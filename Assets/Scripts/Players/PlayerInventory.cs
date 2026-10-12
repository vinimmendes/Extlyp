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
    public partial class PlayerInventory : NetworkBehaviour
    {
        public static readonly List<PlayerInventory> All = new List<PlayerInventory>();
        public NetworkVariable<int> LootValue = new NetworkVariable<int>(0,NetworkVariableReadPermission.Owner), Wood = new NetworkVariable<int>(0,NetworkVariableReadPermission.Owner), Nails = new NetworkVariable<int>(0,NetworkVariableReadPermission.Owner), Bandages = new NetworkVariable<int>(0,NetworkVariableReadPermission.Owner), RecyclerLevel = new NetworkVariable<int>(0,NetworkVariableReadPermission.Owner);
        public NetworkVariable<double> BandageReadyAt = new NetworkVariable<double>(0,NetworkVariableReadPermission.Owner);
        public NetworkVariable<byte> Status = new NetworkVariable<byte>((byte)PlayerStatus.Alive);
        public NetworkVariable<int> EquippedSlot = new NetworkVariable<int>(-1,NetworkVariableReadPermission.Owner);
        public NetworkVariable<byte> EquippedItem = new NetworkVariable<byte>(255);
        public NetworkVariable<byte> FishingPhase = new NetworkVariable<byte>(0,NetworkVariableReadPermission.Owner);
        public NetworkVariable<bool> Lockpicking = new NetworkVariable<bool>(false,NetworkVariableReadPermission.Owner);
        public NetworkVariable<float> FishingTension = new NetworkVariable<float>(.4f,NetworkVariableReadPermission.Owner), FishingProgress = new NetworkVariable<float>(0,NetworkVariableReadPermission.Owner);
        public NetworkVariable<float> FishingFish = new NetworkVariable<float>(.5f,NetworkVariableReadPermission.Owner), FishingZone = new NetworkVariable<float>(.28f,NetworkVariableReadPermission.Owner);
        public NetworkVariable<bool> FishingNibble = new NetworkVariable<bool>(false,NetworkVariableReadPermission.Owner);
        public NetworkVariable<float> FishingVelocity = new NetworkVariable<float>(0,NetworkVariableReadPermission.Owner);
        public NetworkList<ItemStack> Slots;
        public PlayerStatus CurrentStatus => (PlayerStatus)Status.Value;
        public bool IsAlive => CurrentStatus == PlayerStatus.Alive;
        public float ServerSlowUntil;
        public float ServerMovementMultiplier => Time.time < ServerSlowUntil ? .5f : 1;
        float nextStepNoise;
        Vector2 noisePosition;
        void FixedUpdate()
        {
            if(!IsServer || !IsAlive || MatchManager.Instance==null || MatchManager.Instance.CurrentState!=MatchState.Running) return;
            InspectExits();
            Vector2 point=transform.position;
            if(Time.time>=nextStepNoise && Vector2.Distance(point,noisePosition)>.4f)
            { CyberExtraction.Core.Noise.Emit(point,5); nextStepNoise=Time.time+.5f; noisePosition=point; }
        }
        Health health; SpriteRenderer sr; Collider2D col; VisibilityTarget vis;
        Color aliveColor;
        void Awake() { Slots = new NetworkList<ItemStack>(null,NetworkVariableReadPermission.Owner); }
        public override void OnNetworkSpawn()
        {
            All.Add(this); health = GetComponent<Health>(); sr = GetComponent<SpriteRenderer>(); col = GetComponent<Collider2D>(); vis = GetComponent<VisibilityTarget>();
            if(sr!=null) aliveColor=GetComponent<PlayerCharacterVisual>()!=null ? Color.white : sr.color;
            noisePosition=transform.position;
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
            if(IsAlive) { if(col!=null) col.enabled=true; if(vis!=null) vis.ForceHidden=false; if(sr!=null) sr.color=aliveColor; }
            if (CurrentStatus == PlayerStatus.Extracted || CurrentStatus == PlayerStatus.Dead || CurrentStatus == PlayerStatus.TimeUp) if (col != null) col.enabled = false;
            if (CurrentStatus == PlayerStatus.Extracted && vis != null) vis.ForceHidden = true;
            if (CurrentStatus == PlayerStatus.Dead && sr != null) sr.color = new Color(.25f,.25f,.25f);
        }
        void HandleDeath(Health h) { if (!IsAlive) return; CyberExtraction.Enemies.MonsterWorld.AddCorpse(transform.position); DropItems(); Status.Value = (byte)PlayerStatus.Dead; }
        public InventoryGrid Grid()
        {
            var grid = new InventoryGrid(); for (int i = 0; i < Mathf.Min(9, Slots.Count); i++) grid.Slots[i] = Slots[i]; return grid;
        }
        public void CopyGrid(InventoryGrid target)
        { for(int i=0;i<9;i++) target.Slots[i]=i<Slots.Count ? Slots[i] : default; }
        public int Count(ItemId item) { int total=0; for(int i=0;i<Slots.Count;i++) if(!Slots[i].Empty && Slots[i].Item==item) total+=Slots[i].Quantity; return total; }
        public void ServerSetGrid(InventoryGrid grid)
        {
            if (!IsServer) return;
            while (Slots.Count < 9) Slots.Add(default);
            int active = ActiveWeaponSlot;
            if (active >= 0 && !Slots[active].Equals(grid.Slots[active])) GetComponent<PlayerShooter>()?.ServerCancelReload();
            int score = 0;
            for (int i = 0; i < 9; i++) { if (!Slots[i].Equals(grid.Slots[i])) Slots[i] = grid.Slots[i]; if (!grid.Slots[i].Empty && !ItemCatalog.Weapon(grid.Slots[i].Item) && !ItemCatalog.Ammo(grid.Slots[i].Item)) score += grid.Slots[i].Quantity * ItemCatalog.Score(grid.Slots[i].Item); }
            Wood.Value = grid.Count(ItemId.Wood); Nails.Value = grid.Count(ItemId.Nails); Bandages.Value = grid.Count(ItemId.Bandage); LootValue.Value = score;
            if (!IsWeaponSlot(EquippedSlot.Value)) EquippedSlot.Value = FirstWeaponSlot();
            ServerRefreshEquipped();
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
        public int PistolRounds { get { int slot=PistolSlot(); return slot>=0 ? Slots[slot].Data : 0; } }
        public bool IsWeaponSlot(int slot) => slot >= 0 && slot < Slots.Count && !Slots[slot].Empty && ItemCatalog.Weapon(Slots[slot].Item);
        int FirstWeaponSlot() { for (int i=0;i<Slots.Count;i++) if (IsWeaponSlot(i)) return i; return -1; }
        public int ActiveWeaponSlot => IsWeaponSlot(EquippedSlot.Value) ? EquippedSlot.Value : FirstWeaponSlot();
        public WeaponDefinition ActiveWeapon => !IsServer && !IsOwner ? WeaponCatalog.Get((ItemId)EquippedItem.Value) : ActiveWeaponSlot >= 0 ? WeaponCatalog.Get(Slots[ActiveWeaponSlot].Item) : null;
        void ServerRefreshEquipped() { if(IsServer) EquippedItem.Value=ActiveWeaponSlot>=0 ? (byte)Slots[ActiveWeaponSlot].Item : (byte)255; }
        public int WeaponRounds => ActiveWeaponSlot >= 0 ? Slots[ActiveWeaponSlot].Data : 0;
        public int WeaponCount { get { int count=0; for(int i=0;i<Slots.Count;i++) if(IsWeaponSlot(i)) count++; return count; } }
        public void RequestNextWeapon() { if(IsOwner) NextWeaponServerRpc(); }
        [ServerRpc] void NextWeaponServerRpc() { ServerNextWeapon(); }
        public void ServerNextWeapon()
        {
            if (!CanAct || FishingPhase.Value != 0 || WeaponCount < 2) return;
            int active = ActiveWeaponSlot;
            for(int offset=1;offset<=Slots.Count;offset++)
            {
                int slot=(active+offset)%Slots.Count; if(!IsWeaponSlot(slot)) continue;
                GetComponent<PlayerShooter>()?.ServerCancelReload(); EquippedSlot.Value=slot; ServerRefreshEquipped(); return;
            }
        }
        public void ServerSetRounds(int rounds)
        { int slot = ActiveWeaponSlot; if (!IsServer || slot < 0) return; var stack = Slots[slot]; stack.Data = Mathf.Clamp(rounds, 0, ActiveWeapon.Capacity); Slots[slot] = stack; }
        string expeditionId;
        public void ServerCommitExpedition() { var join=Join(); if(join!=null && !string.IsNullOrEmpty(expeditionId)) CampRepository.Commit(join.Session,expeditionId); }
        public void ServerLoseExpedition() { var join=Join(); if(join!=null && !string.IsNullOrEmpty(expeditionId)) CampRepository.Lose(join.Session,expeditionId); }
        public void ServerRollbackExpedition() { var join=Join(); if(join!=null && !string.IsNullOrEmpty(expeditionId)) CampRepository.Rollback(join.Session,expeditionId); }
        void DropItems()
        {
            var join=NetworkObject.IsPlayerObject ? Join() : null;
            if(join!=null && !string.IsNullOrEmpty(expeditionId)) CampRepository.Lose(join.Session,expeditionId);
            var mm = MatchManager.Instance; if (mm == null) return;
            foreach (var stack in Grid().Slots) if (!stack.Empty) mm.ServerDropStack(transform.position + (Vector3)(Random.insideUnitCircle * .7f), stack);
            ServerSetGrid(new InventoryGrid());
        }
        bool CanAct => IsServer && IsAlive && !Lockpicking.Value && MatchManager.Instance != null && MatchManager.Instance.CurrentState == MatchState.Running;
        // Expedition fishing is disabled. The cabin uses its separate authoritative service.
        // Keep the existing RPC signatures/network fields for scene and diagnostic compatibility.
        public void RequestFishing() { }
        [ServerRpc] void StartFishingServerRpc()
        {
            FishingPhase.Value=0; FishingNibble.Value=false;
        }
        public bool CanFishHere=>false;
        public void SetReel(bool held) { }
        [ServerRpc] void ReelServerRpc(bool held) { }
        public void CancelFishing() { if(IsOwner) CancelFishingServerRpc(); }
        [ServerRpc] void CancelFishingServerRpc() { FishingPhase.Value=0; }
        void Update()
        {
            if(IsServer && FishingPhase.Value!=0) { FishingPhase.Value=0; FishingNibble.Value=false; }
        }
        public void RequestMove(int source, int target) { if (IsOwner) MoveServerRpc(source, target); }
        [ServerRpc] void MoveServerRpc(int source, int target)
        {
            if (!CanAct) return; int active=ActiveWeaponSlot; var grid=Grid();
            if(!grid.MoveTo(source,grid,target)) return;
            GetComponent<PlayerShooter>()?.ServerCancelReload(); ServerSetGrid(grid);
            if(active==source) EquippedSlot.Value=target; else if(active==target) EquippedSlot.Value=source;
            ServerRefreshEquipped();
        }
        public void RequestDrop(int slot) { if (IsOwner) DropServerRpc(slot); }
        [ServerRpc] void DropServerRpc(int slot)
        {
            if (!CanAct || slot < 0 || slot >= Slots.Count || Slots[slot].Empty) return;
            GetComponent<PlayerShooter>()?.ServerCancelReload();
            MatchManager.Instance.ServerDropStack(transform.position + (Vector3)(Random.insideUnitCircle.normalized * 1.4f), Slots[slot], NetworkObjectId);
            Slots[slot] = default; ServerSetGrid(Grid());
        }
        public void RequestBandage() { if (IsOwner) UseBandageServerRpc(); }
        [ServerRpc] void UseBandageServerRpc() { ServerUseBandage(); }
        public bool ServerUseBandage()
        {
            if (!CanAct || Count(ItemId.Bandage) <= 0 || NetworkManager.ServerTime.Time < BandageReadyAt.Value || !health.ServerHeal(ItemCatalog.BandageHeal)) return false;
            ServerRemoveItem(ItemId.Bandage, 1); BandageReadyAt.Value = NetworkManager.ServerTime.Time + ItemCatalog.BandageCooldown; return true;
        }
        CampJoinRequest Join() => NetworkObject.IsPlayerObject && CyberExtraction.UI.NetworkBootstrap.Instance!=null ? CyberExtraction.UI.NetworkBootstrap.Instance.RequestFor(OwnerClientId) : null;
        public void ServerBeginExpedition()
        {
            if (!IsServer) return; var join = NetworkObject.IsPlayerObject ? Join() : null;
            if(MatchManager.Instance != null && MatchManager.Instance.Sandbox) join=null;
            int food = 0; InventoryGrid equipment = InventoryGrid.Starter();
            if(join!=null) { expeditionId=System.Guid.NewGuid().ToString("N"); equipment=CampRepository.Begin(join.Session,expeditionId,out food); RecyclerLevel.Value=CampRepository.Get(join.Session).RecyclerLevel; }
            ServerSetGrid(equipment); health.maxHealth = CyberExtraction.Core.GameConfig.PlayerMaxHealth + food; health.Maximum.Value = health.maxHealth; health.Current.Value = health.maxHealth; ServerSyncCamp();
        }
        public bool ServerBankExtraction()
        {
            if(CanAct && MatchManager.Instance.Sandbox) { Status.Value=(byte)PlayerStatus.Extracted; return true; }
            if (!CanAct || !NetworkObject.IsPlayerObject || Join() == null) return false;
            if(!CampRepository.Bank(Join().Session,expeditionId,Grid())) return false;
            Status.Value=(byte)PlayerStatus.Extracted; ServerSyncCamp(); return true;
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
