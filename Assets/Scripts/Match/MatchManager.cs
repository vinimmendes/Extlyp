using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Players;
using CyberExtraction.Combat;
using CyberExtraction.Bots;
using CyberExtraction.Items;
using CyberExtraction.Enemies;

namespace CyberExtraction.Match
{
    public enum MatchState : byte { Waiting, Running, Ended }

    // Uma sala por processo: espera, partida e resultados controlados pelo servidor.
    public partial class MatchManager : NetworkBehaviour
    {
        public static MatchManager Instance { get; private set; }

        public GameObject lootPrefab;
        public GameObject monsterPrefab;
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
        public NetworkVariable<byte> MapSelection = new NetworkVariable<byte>(0);
        public NetworkVariable<int> LayoutSeed = new NetworkVariable<int>(43127);
        public NetworkVariable<bool> QuickTest = new NetworkVariable<bool>(false);
        public NetworkVariable<int> QuickBots = new NetworkVariable<int>(1);

        float timeLeft;
        public NetworkList<MonsterEffect> MonsterEffects;
        public readonly List<MonsterEffect> VisibleEffects=new List<MonsterEffect>();
        void Awake() { MonsterEffects = new NetworkList<MonsterEffect>(null,NetworkVariableReadPermission.Owner); }
        void LateUpdate() { if(IsSpawned) MonsterWorld.Draw(this); }

        public MatchState CurrentState => (MatchState)State.Value;

        public override void OnNetworkSpawn()
        {
            Instance = this;
            MapSelection.OnValueChanged += OnMapChanged;
            LayoutSeed.OnValueChanged += OnLayoutChanged;
            ActiveExtractions.OnValueChanged+=ExitStateChanged;
            OpenedPassages.OnValueChanged+=PassageStateChanged;
            MapBuilder.Configure((ExpeditionMap)MapSelection.Value,LayoutSeed.Value);
            if (IsServer) RoomNumber.Value = CyberExtraction.UI.NetworkBootstrap.Instance.RoomNumber;
        }
        public override void OnNetworkDespawn() { MapSelection.OnValueChanged -= OnMapChanged; LayoutSeed.OnValueChanged -= OnLayoutChanged; ActiveExtractions.OnValueChanged-=ExitStateChanged; OpenedPassages.OnValueChanged-=PassageStateChanged; MonsterWorld.ClearVisuals(); if (Instance == this) Instance = null; }
        void OnMapChanged(byte previous,byte current) { MapBuilder.Configure((ExpeditionMap)current,LayoutSeed.Value); }
        void OnLayoutChanged(int previous,int current) { MapBuilder.Configure((ExpeditionMap)MapSelection.Value,current); }

        private void Update()
        {
            if (!IsServer) return;

            if (CurrentState == MatchState.Waiting)
            {
                int ready = ReadyPlayers();
                HumanCount.Value = ready;
                if (QuickTest.Value)
                {
                    WaitingSeconds.Value = 0;
                    if (ready > 0) StartMatch();
                    return;
                }
                WaitingSeconds.Value = queue.SecondsLeft(ready, Time.timeAsDouble);
                if (queue.ShouldStart(ready, Time.timeAsDouble, MapBuilder.Capacity)) StartMatch();
            }
            else if (CurrentState == MatchState.Running)
            {
                MonsterWorld.ServerTick(this);
                TickDungeon();
                timeLeft -= Time.deltaTime;
                int s = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
                if (s != SecondsLeft.Value) SecondsLeft.Value = s;

                if (!Sandbox && (timeLeft <= 0f || !AnyHumanAlive())) EndMatch();
                if (Sandbox && timeLeft <= 0) timeLeft = GameConfig.MatchDuration;
                if (Sandbox && ReadyPlayers()==0) ResetRoom();
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
            try { StartMatchCore(); }
            catch(System.Exception error)
            {
                foreach(var inv in PlayerInventory.All) if(inv!=null && inv.NetworkObject.IsPlayerObject) inv.ServerRollbackExpedition();
                Debug.LogError("Inicio de partida revertido: "+error.Message); ResetRoom();
            }
        }
        void StartMatchCore()
        {
            var spawnClock=System.Diagnostics.Stopwatch.StartNew();
            OpenedPassages.Value=0;
            ResetDungeon();
            LayoutSeed.Value=Random.Range(1,int.MaxValue);
            MapBuilder.Configure((ExpeditionMap)MapSelection.Value,LayoutSeed.Value);
            ActiveExtractions.Value=MapBuilder.Current==ExpeditionMap.Large ? LargeMapPlan.ChooseExits(LayoutSeed.Value,MapBuilder.SpawnPoints) : MapBuilder.Current==ExpeditionMap.Dungeon ? (ushort)0 : (ushort)1;
            timeLeft = GameConfig.MatchDuration;
            DungeonBreakables.ServerSpawn();
            SecondsLeft.Value = (int)GameConfig.MatchDuration;

            // As vagas humanas sao reservadas pela aprovacao de conexao.
            var occupied = new HashSet<int>();
            var nm = NetworkManager.Singleton;
            var clients = new List<NetworkClient>(nm.ConnectedClientsList);
            foreach (var client in clients)
            {
                if(CyberExtraction.UI.NetworkBootstrap.Instance.IsCampClient(client.ClientId)) continue;
                if (client.PlayerObject != null && CyberExtraction.UI.NetworkBootstrap.Instance.ClientReady(client.ClientId))
                {
                    occupied.Add(CyberExtraction.UI.NetworkBootstrap.Instance.SlotFor(client.ClientId));
                    client.PlayerObject.GetComponent<PlayerInventory>().ServerBeginExpedition();
                    client.PlayerObject.GetComponent<PlayerInventory>().ServerInitializeExits();
                }
                else
                    nm.DisconnectClient(client.ClientId, "A sincronizacao nao terminou a tempo. Inicie uma nova busca.");
            }
            BotCount.Value = 0;
            int botsRequired = QuickTest.Value ? Mathf.Clamp(QuickBots.Value,0,MapBuilder.Capacity-1) : MapBuilder.Capacity - occupied.Count;
            var freeSlots=new List<int>();
            for(int slot=0;slot<MapBuilder.Capacity;slot++) if(!occupied.Contains(slot)) freeSlots.Add(slot);
            while(freeSlots.Count>0 && BotCount.Value<botsRequired)
            {
                int pick=Random.Range(0,freeSlots.Count),slot=freeSlots[pick];
                freeSlots.RemoveAt(pick);
                var bot = Instantiate(botPrefab, MapBuilder.ParticipantSpawn(slot),
                    Quaternion.Euler(0, 0, MapBuilder.ParticipantFacing(slot)));
                bot.GetComponent<BotController>().difficulty = botDifficulty;
                bot.GetComponent<NetworkObject>().Spawn();
                bot.GetComponent<PlayerInventory>().ServerBeginExpedition();
                bot.GetComponent<PlayerInventory>().ServerInitializeExits();
                BotCount.Value++;
            }

            for (int i = 0; i < (MapBuilder.Current==ExpeditionMap.Dungeon ? 0 : MapBuilder.LootPoints.Length); i++)
            {
                // These two existing slots carry authored passage keys, outside random loot.
                if(MapBuilder.Current==ExpeditionMap.Large && ForestLootContent.TryObjective(i,out var objective))
                    SpawnItem(MapBuilder.LootPoints[i],objective,1);
                else SpawnRolledLoot(MapBuilder.LootPoints[i],new LootContext(MapBuilder.Current,LootSource.Ground));
            }
            for (int i=0;i<(MapBuilder.Current==ExpeditionMap.Dungeon ? 0 : MapBuilder.Sites.Length);i++)
            {
                // One server-side roll per chest slot each match, not a fixed map pattern.
                if(monsterPrefab!=null && Random.Range(0,100)<Mathf.Clamp(GameplayDefinition.Current.MimicChestChancePercent,0,100) &&
                    SpawnMonster(MapBuilder.Current==ExpeditionMap.Large ? MapBuilder.RuinPoint(i,new Vector2(0,4)) : MapBuilder.Sites[i]+new Vector2(0,4),MonsterKind.Mimic))
                {
                    continue;
                }
                var chest=Instantiate(lootPrefab,MapBuilder.Current==ExpeditionMap.Large ? MapBuilder.RuinPoint(i,new Vector2(0,4)) : MapBuilder.Sites[i]+new Vector2(0,4),Quaternion.identity);
                var loot=chest.GetComponent<LootPickup>(); loot.initialChest=true; loot.initialRarity=ExpeditionRolls.ContainerGrade(); loot.initialQuantity=0;
                loot.initialLocked=loot.initialRarity>0 || ExpeditionRolls.Chance(30); loot.initialLockGrade=loot.initialRarity;
                loot.ConfigureLoot(new LootContext(MapBuilder.Current,loot.initialLocked ? LootSource.ValuableContainer : LootSource.CommonContainer,
                    loot.initialLocked ? LootRisk.Medium : LootRisk.Low));
                chest.GetComponent<NetworkObject>().Spawn();
            }
            foreach(var fixture in MapBuilder.FurniturePoints)
            {
                var container=Instantiate(lootPrefab,fixture.Position,Quaternion.Euler(0,0,fixture.Angle));
                var loot=container.GetComponent<LootPickup>();
                loot.initialChest=true; loot.initialQuantity=0; loot.initialFixtureKind=fixture.Kind;
                loot.initialRarity=ExpeditionRolls.ContainerGrade();
                loot.initialLocked=ExpeditionRolls.Chance(30); loot.initialLockGrade=loot.initialRarity;
                loot.ConfigureLoot(new LootContext(MapBuilder.Current,loot.initialLocked ? LootSource.ValuableContainer : fixture.Source,
                    loot.initialLocked ? LootRisk.Medium : fixture.Risk));
                container.GetComponent<NetworkObject>().Spawn();
            }
            if (monsterPrefab != null && MapBuilder.Current!=ExpeditionMap.Dungeon)
            {
                var positions = MapBuilder.Current == ExpeditionMap.Test ? new[] { MapBuilder.MonsterSpawn } : MapBuilder.Sites;
                for(int i=0;i<positions.Length;i++)
                {
                    var site=positions[i];
                    if(MapBuilder.Current==ExpeditionMap.Test)
                    {
                        if(ExpeditionRolls.Chance(GameplayDefinition.Current.MonsterSpawnChancePercent)) SpawnMonster(site,ExpeditionRolls.Monster());
                        continue;
                    }
                    Vector2[] offsets={new Vector2(12,10),new Vector2(12,-8),new Vector2(-9,-8)};
                    for(int j=0;j<offsets.Length;j++)
                        if(ExpeditionRolls.Chance(GameplayDefinition.Current.MonsterSpawnChancePercent))
                            SpawnMonster(site+offsets[j]+Random.insideUnitCircle*3,ExpeditionRolls.Monster());
                }
            }
            if(MapBuilder.Current==ExpeditionMap.Large) { PlantEncounter.ServerSpawnMothers(); SporeEncounter.Spawn(); }
            if(MapBuilder.Current==ExpeditionMap.Large && monsterPrefab!=null)
                foreach(var route in LargeMapPlan.Routes)
                    if(route.Condition==RouteCondition.Horde)
                        for(int member=0;member<3;member++) SpawnMonster(route.Gate+new Vector2(member*2-2,2),member==0 ? MonsterKind.Shell : MonsterKind.Listener);
            if(MapBuilder.Current==ExpeditionMap.Dungeon) SpawnDungeonContent();
            foreach(var inv in PlayerInventory.All) if(inv!=null && inv.NetworkObject.IsPlayerObject) inv.ServerCommitExpedition();
            State.Value = (byte)MatchState.Running;
            spawnClock.Stop(); CyberExtraction.Diagnostics.MatchTelemetry.Record("spawn-ms",0,(int)spawnClock.ElapsedMilliseconds);
            CyberExtraction.Diagnostics.MatchTelemetry.Record("match-start",0,HumanCount.Value);
            Debug.Log($"Partida iniciada: {HumanCount.Value} humanos + {BotCount.Value} bots.");
        }

        bool SpawnMonster(Vector2 point,MonsterKind kind)
        {
            if(monsterPrefab==null) return false;
            bool free=false;
            Vector2 origin=point;
            for(int attempt=0;attempt<25;attempt++)
            {
                point=origin+(attempt==0 ? Vector2.zero : new Vector2(Mathf.Cos(attempt*2.4f),Mathf.Sin(attempt*2.4f))*(1+attempt*.35f));
                if(Mathf.Abs(point.x)>MapBuilder.Width/2-2 || Mathf.Abs(point.y)>MapBuilder.Height/2-2 || Physics2D.OverlapCircle(point,.85f,GameConfig.WallMask)!=null) continue;
                bool safe=true;
                foreach(var spawn in MapBuilder.SpawnPoints) if(Vector2.Distance(point,spawn)<(MapBuilder.Current==ExpeditionMap.Large ? 18 : 10)) safe=false;
                foreach(var exit in MapBuilder.ExtractionPoints) if(Vector2.Distance(point,exit)<8) safe=false;
                foreach(var other in Monster.All) if(other!=null && Vector2.Distance(point,other.transform.position)<2) safe=false;
                if(safe) { free=true; break; }
            }
            if(!free) return false;
            var go=Instantiate(monsterPrefab,point,Quaternion.identity);
            go.GetComponent<Monster>().Configure(kind); go.GetComponent<NetworkObject>().Spawn();
            return true;
        }

        void EndMatch()
        {
            foreach (var inv in PlayerInventory.All)
            {
                if (inv != null && inv.IsAlive) { if(inv.NetworkObject.IsPlayerObject) { var join=CyberExtraction.UI.NetworkBootstrap.Instance.RequestFor(inv.OwnerClientId); if(join!=null) inv.ServerLoseExpedition(CyberExtraction.Camp.ExpeditionOutcome.TimeUp); } inv.ServerSetStatus(PlayerStatus.TimeUp); }
            }
            State.Value = (byte)MatchState.Ended;
            timeLeft = GameConfig.ResultsSeconds;
            ResultsLeft.Value = GameConfig.ResultsSeconds;
            if (NetworkManager.Singleton.ConnectedClientsIds.Count == 0) ResetRoom();
        }

        void ResetRoom()
        {
            ResetDungeon(true);
            var nm = NetworkManager.Singleton;
            var clients = new List<ulong>(nm.ConnectedClientsIds);
            foreach (ulong client in clients) if(!CyberExtraction.UI.NetworkBootstrap.Instance.IsCampClient(client)) nm.DisconnectClient(client, "Partida encerrada. Inicie uma nova busca.");
            var objects = new List<NetworkObject>(nm.SpawnManager.SpawnedObjects.Values);
            foreach (var obj in objects)
                if (obj != NetworkObject && obj != null && obj.IsSpawned) obj.Despawn();
            MonsterEffects.Clear(); MonsterWorld.ClearVisuals();
            nextTestCommand.Clear(); TestProtection.Value=false; TestMessage.Value=default;
            queue.Reset();
            HumanCount.Value = BotCount.Value = ResultsLeft.Value = 0;
            SecondsLeft.Value = (int)GameConfig.MatchDuration;
            WaitingSeconds.Value = GameConfig.MatchmakingSeconds;
            State.Value = (byte)MatchState.Waiting;
            ActiveExtractions.Value=0; OpenedPassages.Value=0;
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
                CyberExtraction.Diagnostics.MatchTelemetry.Record("extraction",inv.NetworkObjectId,SecondsLeft.Value);
                if(MapBuilder.Current==ExpeditionMap.Dungeon && (inv.CurrentExtractionExit.Value<0 || !DungeonReady(inv.CurrentExtractionExit.Value) || inv.ExtractionProgress.Value<1 || inv.Lockpicking.Value || inv.DungeonAction.Value>=0 || Vector2.Distance(inv.transform.position,DungeonLayout.ExitPositions[inv.CurrentExtractionExit.Value])>2.2f)) return;
                if(inv.NetworkObject.IsPlayerObject && !inv.ServerBankExtraction()) return;
                ServerReleaseDungeonTools(inv);
                inv.ServerSetStatus(PlayerStatus.Extracted);
            }
        }

        public void ServerDropLoot(Vector2 position, int value,LootTheme theme=LootTheme.General)
        {
            if (!IsServer || value <= 0) return;
            SpawnRolledLoot(position,new LootContext(MapBuilder.Current,LootSource.Monster,LootRisk.Low,theme));
        }

        void SpawnRolledLoot(Vector2 position,LootContext context)
        {
            if(!IsServer) return;
            var stacks=LootRoller.Roll(context);
            float angle=Random.Range(0f,Mathf.PI*2);
            for(int i=0;i<stacks.Length;i++)
            {
                float a=angle+i*Mathf.PI*2/stacks.Length;
                var offset=stacks.Length==1 ? Vector2.zero : new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.85f;
                SpawnStack(position+offset,stacks[i]);
            }
        }

        public void ServerDropItem(Vector2 position, ItemId item, int quantity)
        {
            if (!IsServer || quantity <= 0 || !ItemCatalog.Valid(item)) return;
            Vector2 offset = item == ItemId.Wood ? new Vector2(-0.45f, 0) :
                item == ItemId.Nails ? new Vector2(0.45f, 0) : new Vector2(0, 0.45f);
            SpawnItem(position + offset, item, quantity);
        }

        public void ServerDropStack(Vector2 position, ItemStack stack, ulong ignoreObject = 0)
        {
            if (!IsServer || stack.Empty || !ItemCatalog.Valid(stack.Item) || !EquipmentCatalog.ValidInstance(stack) || RecoveryRules.Bound(stack)) return;
            SpawnStack(position, stack, ignoreObject);
            if(ignoreObject!=0) Noise.Emit(position,10);
        }

        void SpawnItem(Vector2 position, ItemId item, int quantity)
        { SpawnStack(position, new ItemStack(item, quantity)); }

        void SpawnStack(Vector2 position, ItemStack stack, ulong ignoreObject = 0)
        {
            if (lootPrefab == null) return;
            position = new Vector2(Mathf.Clamp(position.x,-MapBuilder.Width/2+1,MapBuilder.Width/2-1),Mathf.Clamp(position.y,-MapBuilder.Height/2+1,MapBuilder.Height/2-1));
            if (Physics2D.OverlapCircle(position,.3f,GameConfig.WallMask)!=null)
            {
                for(int i=0;i<16;i++)
                {
                    float angle=i*Mathf.PI/8; var candidate=position+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*1.5f;
                    if(Physics2D.OverlapCircle(candidate,.3f,GameConfig.WallMask)==null) { position=candidate; break; }
                }
            }
            var go = Instantiate(lootPrefab, position, Quaternion.identity);
            var pickup = go.GetComponent<LootPickup>();
            pickup.initialItem = stack.Item;
            pickup.initialQuantity = stack.Quantity;
            pickup.initialQuality = stack.Quality; pickup.initialData = stack.Data;
            pickup.initialInstance=stack.Instance;
            pickup.ignoreObject = ignoreObject; pickup.ignoreUntil = Time.time + 3;
            go.GetComponent<NetworkObject>().Spawn();
        }
    }
}
