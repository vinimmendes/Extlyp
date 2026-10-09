using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Players;
using CyberExtraction.Combat;
using CyberExtraction.Bots;
using CyberExtraction.Items;

namespace CyberExtraction.Match
{
    public enum MatchState : byte { Waiting, Running, Ended }

    // Uma sala por processo: espera, partida e resultados controlados pelo servidor.
    public class MatchManager : NetworkBehaviour
    {
        public static MatchManager Instance { get; private set; }

        public GameObject lootPrefab;
        public GameObject monsterPrefab;
        public GameObject esporiferoPrefab;
        public Vector2 sporeWind = new Vector2(0.65f, 0.15f);
        public GameObject botPrefab;
        public BotDifficulty botDifficulty = BotDifficulty.Medium;
        readonly MatchQueue queue = new MatchQueue();

        public NetworkVariable<byte> State = new NetworkVariable<byte>((byte)MatchState.Waiting);
        public NetworkVariable<int> SecondsLeft = new NetworkVariable<int>((int)GameConfig.MatchDuration);
        public NetworkVariable<int> WaitingSeconds = new NetworkVariable<int>(GameConfig.MatchmakingSeconds);
        public NetworkVariable<int> HumanCount = new NetworkVariable<int>(0);
        public NetworkVariable<int> BotCount = new NetworkVariable<int>(0);
        public NetworkVariable<int> ResultsLeft = new NetworkVariable<int>(0);
        public NetworkVariable<int> RoomNumber = new NetworkVariable<int>(1);

        float timeLeft;

        public MatchState CurrentState => (MatchState)State.Value;

        public override void OnNetworkSpawn()
        {
            Instance = this;
            if (IsServer) RoomNumber.Value = CyberExtraction.UI.NetworkBootstrap.Instance.RoomNumber;
        }
        public override void OnNetworkDespawn() { if (Instance == this) Instance = null; }

        private void Update()
        {
            if (!IsServer) return;

            if (CurrentState == MatchState.Waiting)
            {
                int ready = ReadyPlayers();
                HumanCount.Value = ready;
                WaitingSeconds.Value = queue.SecondsLeft(ready, Time.timeAsDouble);
                if (queue.ShouldStart(ready, Time.timeAsDouble)) StartMatch();
            }
            else if (CurrentState == MatchState.Running)
            {
                timeLeft -= Time.deltaTime;
                int s = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
                if (s != SecondsLeft.Value) SecondsLeft.Value = s;

                if (timeLeft <= 0f || !AnyHumanAlive()) EndMatch();
            }
            else
            {
                timeLeft -= Time.deltaTime;
                ResultsLeft.Value = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
                if (timeLeft <= 0f || NetworkManager.Singleton.ConnectedClientsIds.Count == 0) ResetRoom();
            }
        }

        int ReadyPlayers()
        {
            int ready = 0;
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                if (client.PlayerObject != null && client.PlayerObject.IsSpawned &&
                    CyberExtraction.UI.NetworkBootstrap.Instance.ClientReady(client.ClientId)) ready++;
            return ready;
        }

        bool AnyHumanAlive()
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (!CyberExtraction.UI.NetworkBootstrap.Instance.ClientReady(client.ClientId)) continue;
                var inv = client.PlayerObject != null ? client.PlayerObject.GetComponent<PlayerInventory>() : null;
                if (inv != null && inv.IsAlive) return true;
            }
            return false;
        }

        void StartMatch()
        {
            timeLeft = GameConfig.MatchDuration;
            SecondsLeft.Value = (int)GameConfig.MatchDuration;

            // As vagas humanas sao reservadas pela aprovacao de conexao.
            var occupied = new HashSet<int>();
            var nm = NetworkManager.Singleton;
            var clients = new List<NetworkClient>(nm.ConnectedClientsList);
            foreach (var client in clients)
            {
                if (client.PlayerObject != null && CyberExtraction.UI.NetworkBootstrap.Instance.ClientReady(client.ClientId))
                {
                    occupied.Add(CyberExtraction.UI.NetworkBootstrap.Instance.SlotFor(client.ClientId));
                    client.PlayerObject.GetComponent<PlayerInventory>().ServerBeginExpedition();
                }
                else
                    nm.DisconnectClient(client.ClientId, "A sincronizacao nao terminou a tempo. Inicie uma nova busca.");
            }
            BotCount.Value = 0;
            for (int slot = 0; slot < GameConfig.MatchCapacity; slot++)
            {
                if (occupied.Contains(slot)) continue;
                var bot = Instantiate(botPrefab, MapBuilder.ParticipantSpawn(slot),
                    Quaternion.Euler(0, 0, MapBuilder.ParticipantFacing(slot)));
                bot.GetComponent<BotController>().difficulty = botDifficulty;
                bot.GetComponent<NetworkObject>().Spawn();
                bot.GetComponent<PlayerInventory>().ServerBeginExpedition();
                BotCount.Value++;
            }

            for (int i = 0; i < MapBuilder.LootPoints.Length; i++)
            {
                var item = i == 4 || i == 5 ? ItemId.Bandage : i % 2 == 0 ? ItemId.Wood : ItemId.Nails;
                SpawnItem(MapBuilder.LootPoints[i], item, item == ItemId.Wood ? 6 : item == ItemId.Nails ? 4 : 2);
            }
            if (monsterPrefab != null)
            {
                var m = Instantiate(monsterPrefab, MapBuilder.MonsterSpawn, Quaternion.identity);
                m.GetComponent<NetworkObject>().Spawn();
            }
            if (esporiferoPrefab != null)
            {
                var spores = Instantiate(esporiferoPrefab, MapBuilder.EsporiferoSpawn, Quaternion.identity);
                spores.GetComponent<NetworkObject>().Spawn();
            }
            State.Value = (byte)MatchState.Running;
            Debug.Log($"Partida iniciada: {HumanCount.Value} humanos + {BotCount.Value} bots.");
        }

        void EndMatch()
        {
            foreach (var inv in PlayerInventory.All)
            {
                if (inv != null && inv.IsAlive) inv.ServerSetStatus(PlayerStatus.TimeUp);
            }
            State.Value = (byte)MatchState.Ended;
            timeLeft = GameConfig.ResultsSeconds;
            ResultsLeft.Value = GameConfig.ResultsSeconds;
            if (NetworkManager.Singleton.ConnectedClientsIds.Count == 0) ResetRoom();
        }

        void ResetRoom()
        {
            var nm = NetworkManager.Singleton;
            var clients = new List<ulong>(nm.ConnectedClientsIds);
            foreach (ulong client in clients) nm.DisconnectClient(client, "Partida encerrada. Inicie uma nova busca.");
            var objects = new List<NetworkObject>(nm.SpawnManager.SpawnedObjects.Values);
            foreach (var obj in objects)
                if (obj != NetworkObject && obj != null && obj.IsSpawned) obj.Despawn();
            queue.Reset();
            HumanCount.Value = BotCount.Value = ResultsLeft.Value = 0;
            SecondsLeft.Value = (int)GameConfig.MatchDuration;
            WaitingSeconds.Value = GameConfig.MatchmakingSeconds;
            State.Value = (byte)MatchState.Waiting;
            Debug.Log("Sala pronta para uma nova partida.");
        }

        public void ServerPlayerExtracted(ulong clientId)
        {
            if (!IsServer || CurrentState != MatchState.Running) return;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
            var inv = client.PlayerObject != null ? client.PlayerObject.GetComponent<PlayerInventory>() : null;
            ServerParticipantExtracted(inv);
        }

        public void ServerParticipantExtracted(PlayerInventory inv)
        {
            if (!IsServer || CurrentState != MatchState.Running) return;
            if (inv != null && inv.GetComponent<BotController>() != null &&
                !BotDecision.CanExtract(SecondsLeft.Value)) return;
            if (inv != null && inv.IsSpawned && inv.IsAlive)
            {
                inv.ServerBankExtraction();
                inv.ServerSetStatus(PlayerStatus.Extracted);
            }
        }

        public void ServerDropLoot(Vector2 position, int value)
        {
            if (!IsServer || value <= 0) return;
            SpawnItem(position, ItemId.Nails, Mathf.Max(1, value / 5));
        }

        public void ServerDropItem(Vector2 position, ItemId item, int quantity)
        {
            if (!IsServer || quantity <= 0 || !ItemCatalog.Valid(item)) return;
            Vector2 offset = item == ItemId.Wood ? new Vector2(-0.45f, 0) :
                item == ItemId.Nails ? new Vector2(0.45f, 0) : new Vector2(0, 0.45f);
            SpawnItem(position + offset, item, quantity);
        }

        public void ServerDropStack(Vector2 position, ItemStack stack)
        {
            if (!IsServer || stack.Empty || !ItemCatalog.Valid(stack.Item)) return;
            SpawnStack(position, stack);
        }

        void SpawnItem(Vector2 position, ItemId item, int quantity)
        { SpawnStack(position, new ItemStack(item, quantity)); }

        void SpawnStack(Vector2 position, ItemStack stack)
        {
            if (lootPrefab == null) return;
            var go = Instantiate(lootPrefab, position, Quaternion.identity);
            var pickup = go.GetComponent<LootPickup>();
            pickup.initialItem = stack.Item;
            pickup.initialQuantity = stack.Quantity;
            pickup.initialQuality = stack.Quality; pickup.initialData = stack.Data;
            go.GetComponent<NetworkObject>().Spawn();
        }
    }
}

