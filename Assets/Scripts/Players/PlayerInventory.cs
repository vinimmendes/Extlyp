using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Combat;
using CyberExtraction.Match;
using CyberExtraction.Items;
using CyberExtraction.Camp;
namespace CyberExtraction.Players
{
    public enum PlayerStatus : byte { Alive, Extracted, Dead, TimeUp }
    public class PlayerInventory : NetworkBehaviour
    {
        public static readonly List<PlayerInventory> All = new List<PlayerInventory>();
        public NetworkVariable<int> LootValue = new NetworkVariable<int>(0), Wood = new NetworkVariable<int>(0), Nails = new NetworkVariable<int>(0), Bandages = new NetworkVariable<int>(0), RecyclerLevel = new NetworkVariable<int>(0);
        public NetworkVariable<double> BandageReadyAt = new NetworkVariable<double>(0);
        public NetworkVariable<byte> Status = new NetworkVariable<byte>((byte)PlayerStatus.Alive);
        public NetworkList<ItemStack> Slots;
        public PlayerStatus CurrentStatus => (PlayerStatus)Status.Value;
        public bool IsAlive => CurrentStatus == PlayerStatus.Alive;
        Health health; SpriteRenderer sr; Collider2D col; VisibilityTarget vis;
        void Awake() { Slots = new NetworkList<ItemStack>(); }
        public override void OnNetworkSpawn()
        {
            All.Add(this); health = GetComponent<Health>(); sr = GetComponent<SpriteRenderer>(); col = GetComponent<Collider2D>(); vis = GetComponent<VisibilityTarget>();
            if (IsServer) { while (Slots.Count < 9) Slots.Add(default); health.OnDiedServer += HandleDeath; }
            Status.OnValueChanged += OnStatusChanged; ApplyStatus();
            if (IsServer) ServerSyncCamp();
        }
        public override void OnNetworkDespawn()
        {
            if (IsServer && IsAlive && MatchManager.Instance != null && MatchManager.Instance.CurrentState == MatchState.Running) DropItems();
            All.Remove(this); Status.OnValueChanged -= OnStatusChanged; if (IsServer && health != null) health.OnDiedServer -= HandleDeath;
        }
        void OnStatusChanged(byte previous, byte current) { ApplyStatus(); }
        void ApplyStatus()
        {
            if (CurrentStatus == PlayerStatus.Extracted || CurrentStatus == PlayerStatus.Dead || CurrentStatus == PlayerStatus.TimeUp) if (col != null) col.enabled = false;
            if (CurrentStatus == PlayerStatus.Extracted && vis != null) vis.ForceHidden = true;
            if (CurrentStatus == PlayerStatus.Dead && sr != null) sr.color = new Color(.25f,.25f,.25f);
        }
        void HandleDeath(Health h) { if (!IsAlive) return; DropItems(); Status.Value = (byte)PlayerStatus.Dead; }
        public InventoryGrid Grid()
        {
            var grid = new InventoryGrid(); for (int i = 0; i < Mathf.Min(9, Slots.Count); i++) grid.Slots[i] = Slots[i]; return grid;
        }
        public int Count(ItemId item) => Grid().Count(item);
        public void ServerSetGrid(InventoryGrid grid)
        {
            if (!IsServer) return;
            while (Slots.Count < 9) Slots.Add(default);
            int score = 0;
            for (int i = 0; i < 9; i++) { if (!Slots[i].Equals(grid.Slots[i])) Slots[i] = grid.Slots[i]; if (!grid.Slots[i].Empty && !ItemCatalog.Weapon(grid.Slots[i].Item) && grid.Slots[i].Item != ItemId.PistolAmmo) score += grid.Slots[i].Quantity * ItemCatalog.Score(grid.Slots[i].Item); }
            Wood.Value = grid.Count(ItemId.Wood); Nails.Value = grid.Count(ItemId.Nails); Bandages.Value = grid.Count(ItemId.Bandage); LootValue.Value = score;
        }
        public bool ServerAddStack(ItemStack stack)
        {
            if (!IsServer || !IsAlive) return false; var grid = Grid(); if (!grid.Add(stack)) return false; ServerSetGrid(grid); return true;
        }
        public bool ServerAddItem(ItemId item, int quantity) => ServerAddStack(new ItemStack(item, quantity));
        public void ServerAddLoot(int value) { ServerAddItem(ItemId.Metal, value); }
        public bool ServerRemoveItem(ItemId item, int quantity)
        { if (!IsServer) return false; var grid = Grid(); if (!grid.Remove(item, quantity)) return false; ServerSetGrid(grid); return true; }
        public int PistolSlot() { for (int i = 0; i < Slots.Count; i++) if (!Slots[i].Empty && Slots[i].Item == ItemId.Pistol) return i; return -1; }
        public int PistolRounds => PistolSlot() >= 0 ? Slots[PistolSlot()].Data : 0;
        public void ServerSetRounds(int rounds)
        { int slot = PistolSlot(); if (!IsServer || slot < 0) return; var stack = Slots[slot]; stack.Data = Mathf.Clamp(rounds, 0, 12); Slots[slot] = stack; }
        void DropItems()
        {
            var mm = MatchManager.Instance; if (mm == null) return;
            foreach (var stack in Grid().Slots) if (!stack.Empty) mm.ServerDropStack(transform.position + Random.insideUnitCircle * .7f, stack);
            ServerSetGrid(new InventoryGrid());
        }
        bool CanAct => IsServer && IsAlive && MatchManager.Instance != null && MatchManager.Instance.CurrentState == MatchState.Running;
        public void RequestMove(int source, int target) { if (IsOwner) MoveServerRpc(source, target); }
        [ServerRpc] void MoveServerRpc(int source, int target) { if (!CanAct) return; var grid = Grid(); if (grid.MoveTo(source, grid, target)) ServerSetGrid(grid); }
        public void RequestDrop(int slot) { if (IsOwner) DropServerRpc(slot); }
        [ServerRpc] void DropServerRpc(int slot)
        {
            if (!CanAct || slot < 0 || slot >= Slots.Count || Slots[slot].Empty) return;
            MatchManager.Instance.ServerDropStack(transform.position + (Vector3)(Random.insideUnitCircle * .7f), Slots[slot]);
            Slots[slot] = default; ServerSetGrid(Grid());
        }
        public void RequestBandage() { if (IsOwner) UseBandageServerRpc(); }
        [ServerRpc] void UseBandageServerRpc() { ServerUseBandage(); }
        public bool ServerUseBandage()
        {
            if (!CanAct || Count(ItemId.Bandage) <= 0 || NetworkManager.ServerTime.Time < BandageReadyAt.Value || !health.ServerHeal(ItemCatalog.BandageHeal)) return false;
            ServerRemoveItem(ItemId.Bandage, 1); BandageReadyAt.Value = NetworkManager.ServerTime.Time + ItemCatalog.BandageCooldown; return true;
        }
        CampJoinRequest Join() => CyberExtraction.UI.NetworkBootstrap.Instance.RequestFor(OwnerClientId);
        public void ServerBeginExpedition()
        {
            if (!IsServer) return; var join = NetworkObject.IsPlayerObject ? Join() : null;
            int food = 0; InventoryGrid equipment = InventoryGrid.Starter();
            if (join != null) equipment = CampRepository.Edit(join.Session, profile => { profile.Ensure(); food = profile.ConsumeFoodBuff(); var pack = profile.Pack.Copy(); profile.Pack = new InventoryGrid(); profile.Sync(); RecyclerLevel.Value = profile.RecyclerLevel; return pack; });
            ServerSetGrid(equipment); health.maxHealth = CyberExtraction.Core.GameConfig.PlayerMaxHealth + food; health.Maximum.Value = health.maxHealth; health.Current.Value = health.maxHealth; ServerSyncCamp();
        }
        public void ServerBankExtraction()
        {
            if (!CanAct || !NetworkObject.IsPlayerObject || Join() == null) return;
            var returned = Grid(); CampRepository.Edit(Join().Session, profile => { profile.Pack = returned; profile.Sync(); return 0; }); ServerSyncCamp();
        }
        void ServerSyncCamp()
        {
            if (!NetworkObject.IsPlayerObject || Join() == null) return;
            var profile = CampRepository.Get(Join().Session); profile.Ensure();
            SyncCampClientRpc(JsonUtility.ToJson(profile), new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } } });
        }
        [ClientRpc] void SyncCampClientRpc(string json, ClientRpcParams rpcParams = default) { if (IsOwner) CampSession.Accept(json); }
        public void ServerSetStatus(PlayerStatus s) { if (IsServer) Status.Value = (byte)s; }
    }
}
