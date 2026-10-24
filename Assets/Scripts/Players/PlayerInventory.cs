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
        public NetworkVariable<Unity.Collections.FixedString128Bytes> AccountName=new NetworkVariable<Unity.Collections.FixedString128Bytes>(default);
        public NetworkVariable<int> EquippedSlot = new NetworkVariable<int>(-1,NetworkVariableReadPermission.Owner);
        public NetworkVariable<byte> EquippedItem = new NetworkVariable<byte>(255);
        public NetworkVariable<byte> FishingPhase = new NetworkVariable<byte>(0,NetworkVariableReadPermission.Owner);
        public NetworkVariable<bool> Lockpicking = new NetworkVariable<bool>(false,NetworkVariableReadPermission.Owner);
        public NetworkVariable<float> FishingTension = new NetworkVariable<float>(.4f,NetworkVariableReadPermission.Owner), FishingProgress = new NetworkVariable<float>(0,NetworkVariableReadPermission.Owner);
        public NetworkVariable<float> FishingFish = new NetworkVariable<float>(.5f,NetworkVariableReadPermission.Owner), FishingZone = new NetworkVariable<float>(.28f,NetworkVariableReadPermission.Owner);
        public NetworkVariable<bool> FishingNibble = new NetworkVariable<bool>(false,NetworkVariableReadPermission.Owner);
        public NetworkVariable<float> FishingVelocity = new NetworkVariable<float>(0,NetworkVariableReadPermission.Owner);
        public NetworkList<ItemStack> Slots;
        public NetworkList<ItemStack> Equipment;
        public PlayerStatus CurrentStatus => (PlayerStatus)Status.Value;
        public string DisplayName
        {
            get
            {
                var bot=GetComponent<CyberExtraction.Bots.BotController>();
                return bot!=null ? bot.DisplayName : AccountName.Value.Length>0 ? AccountName.Value.ToString() : "Jogador "+(OwnerClientId+1);
            }
        }
        public bool IsAlive => CurrentStatus == PlayerStatus.Alive && !extractionBanked;
        // Server-local terminal state: never depend on RPC/NetworkVariable publication
        // or a second storage read to decide whether already-banked items may drop.
        bool extractionBanked,extractionStatusPublished,extractionProgressPublished,extractionCampPublished;
        float nextExtractionPublish;
        public float ServerSlowUntil;
        public float ServerMovementMultiplier => Time.time < ServerSlowUntil ? .5f : 1;
        public bool ServerWellFed => WellFed.Value;
        float nextStepNoise;
        Vector2 noisePosition;
        void FixedUpdate()
        {
            if(!IsServer || !IsAlive || MatchManager.Instance==null || MatchManager.Instance.CurrentState!=MatchState.Running) return;
            if(ProgressionActive && health!=null && !health.IsDead) CampRepository.QueueProfile(new ProfileEvent {Session=progressionSession,Expedition=expeditionId,Seconds=Time.fixedDeltaTime},true);
            InspectExits();
            Vector2 point=transform.position;
            if(Time.time>=nextStepNoise && Vector2.Distance(point,noisePosition)>.4f)
            { CyberExtraction.Core.Noise.Emit(point,5); nextStepNoise=Time.time+.5f; noisePosition=point; }
        }
        Health health; SpriteRenderer sr; Collider2D col; VisibilityTarget vis;
        Color aliveColor;
        void Awake() { Slots = new NetworkList<ItemStack>(null,NetworkVariableReadPermission.Owner); Equipment=new NetworkList<ItemStack>(null,NetworkVariableReadPermission.Owner); }
        public override void OnNetworkSpawn()
        {
            All.Add(this); dungeonShooter=GetComponent<PlayerShooter>(); health = GetComponent<Health>(); sr = GetComponent<SpriteRenderer>(); col = GetComponent<Collider2D>(); vis = GetComponent<VisibilityTarget>();
            if(sr!=null) aliveColor=GetComponent<PlayerCharacterVisual>()!=null ? Color.white : sr.color;
            noisePosition=transform.position;
            if (IsServer) { while (Slots.Count < ItemCatalog.BackpackSlots) Slots.Add(default); while(Equipment.Count<EquipmentLoadout.Count) Equipment.Add(default); health.OnDiedServer += HandleDeath; }
            Status.OnValueChanged += OnStatusChanged; ApplyStatus();
            if (IsServer) ServerSyncCamp();
            if(IsServer && NetworkObject.IsPlayerObject && Join()!=null)
                AccountName.Value=new Unity.Collections.FixedString128Bytes(CampRepository.Get(Join().Session).PlayerName ?? "");
            if (IsServer) ServerRefreshAttributes();
        }
        public override void OnNetworkDespawn()
        {
            try
            {
                if(IsServer && MatchManager.Instance!=null) MatchManager.Instance.ServerReleaseDungeonTools(this);
                if (IsServer && IsAlive && MatchManager.Instance != null && MatchManager.Instance.CurrentState == MatchState.Running) DropItems();
            }
            catch(CampLeaseLostException) { Debug.LogWarning("Participante removido sem drops: a sala perdeu a posse do acampamento."); }
            catch(System.Exception error) { Debug.LogError("Falha ao finalizar participante desconectado: "+error); }
            finally
            {
                OrganizePending=false;
                All.Remove(this); Status.OnValueChanged -= OnStatusChanged; if (IsServer && health != null) health.OnDiedServer -= HandleDeath;
            }
        }
        void OnStatusChanged(byte previous, byte current) { ApplyStatus(); }
        void ApplyStatus()
        {
            if(IsAlive) { if(col!=null) col.enabled=true; if(vis!=null) vis.ForceHidden=false; if(sr!=null) sr.color=aliveColor; }
            if (CurrentStatus == PlayerStatus.Extracted || CurrentStatus == PlayerStatus.Dead || CurrentStatus == PlayerStatus.TimeUp) if (col != null) col.enabled = false;
            if (CurrentStatus == PlayerStatus.Extracted && vis != null) vis.ForceHidden = true;
            if (CurrentStatus == PlayerStatus.Dead && sr != null) sr.color = new Color(.25f,.25f,.25f);
        }
        float nextDeathFinalize;
        void HandleDeath(Health h)
        {
            if(!IsAlive) return;
            nextDeathFinalize=Time.unscaledTime+2;
            try
            {
                DropItems(ExpeditionOutcome.Dead);
                Status.Value=(byte)PlayerStatus.Dead;
                CyberExtraction.Enemies.MonsterWorld.AddCorpse(transform.position);
            }
            catch(System.Exception error) { Debug.LogError("Morte pendente de finalização; nova tentativa em até dois segundos: "+error.Message); }
        }
        public InventoryGrid Grid()
        {
            var grid = new InventoryGrid(ItemCatalog.BackpackSlots); for (int i = 0; i < Mathf.Min(ItemCatalog.BackpackSlots, Slots.Count); i++) grid.Slots[i] = Slots[i]; return grid;
        }
        public void CopyGrid(InventoryGrid target)
        { for(int i=0;i<Mathf.Min(target.Slots.Length,ItemCatalog.BackpackSlots);i++) target.Slots[i]=i<Slots.Count ? Slots[i] : default; }
        public int Count(ItemId item) { int total=0; for(int i=0;i<Slots.Count;i++) if(!Slots[i].Empty && Slots[i].Item==item) total+=Slots[i].Quantity; return total; }
        public void ServerSetGrid(InventoryGrid grid)
        {
            if (!IsServer) return;
            if(grid==null || grid.Slots==null || grid.Slots.Length!=ItemCatalog.BackpackSlots) throw new System.InvalidOperationException("Mochila deve conter doze slots.");
            while (Slots.Count < ItemCatalog.BackpackSlots) Slots.Add(default);
            int score = 0;
            for (int i = 0; i < ItemCatalog.BackpackSlots; i++) { if (!Slots[i].Equals(grid.Slots[i])) Slots[i] = grid.Slots[i]; if (!grid.Slots[i].Empty && !ItemCatalog.Weapon(grid.Slots[i].Item) && !ItemCatalog.Ammo(grid.Slots[i].Item)) score += grid.Slots[i].Quantity * ItemCatalog.Score(grid.Slots[i].Item); }
            Wood.Value = grid.Count(ItemId.Wood); Nails.Value = grid.Count(ItemId.Nails); Bandages.Value = grid.Count(ItemId.Bandage); LootValue.Value = score;
            if (!IsWeaponSlot(EquippedSlot.Value)) EquippedSlot.Value = FirstWeaponSlot();
            ServerRefreshEquipped();
        }
        public bool ServerAddStack(ItemStack stack)
        {
            if(stack.Instance.HasIdentity) foreach(var equipped in Equipment) if(!equipped.Empty && equipped.Instance.SameIdentity(stack.Instance)) return false;
            if (!IsServer || !IsAlive || ItemCatalog.ExpeditionOnly(stack.Item) && MapBuilder.Current!=ExpeditionMap.Dungeon) return false; var grid = Grid(); if (!grid.Add(stack)) return false; ServerSetGrid(grid); return true;
        }
        public bool ServerAddItem(ItemId item, int quantity) => ServerAddStack(new ItemStack(item, quantity));
        public void ServerAddLoot(int value) { ServerAddItem(ItemId.Metal, value); }
        public bool ServerRemoveItem(ItemId item, int quantity)
        { if (!IsServer) return false; var grid = Grid(); if (!grid.Remove(item, quantity)) return false; ServerSetGrid(grid); return true; }
        public bool IsWeaponSlot(int slot) => EquipmentLoadout.AcceptsWeapon(slot) && slot < Equipment.Count && !Equipment[slot].Empty && ItemCatalog.Weapon(Equipment[slot].Item);
        int FirstWeaponSlot() { for (int i=EquipmentLoadout.WeaponA;i<Equipment.Count;i++) if (IsWeaponSlot(i)) return i; return -1; }
        public int ActiveWeaponSlot => IsWeaponSlot(EquippedSlot.Value) ? EquippedSlot.Value : FirstWeaponSlot();
        public WeaponDefinition ActiveWeapon => !IsServer && !IsOwner ? WeaponCatalog.Get((ItemId)EquippedItem.Value) : ActiveWeaponSlot >= 0 ? WeaponCatalog.Get(Equipment[ActiveWeaponSlot].Item) : null;
        public NetworkVariable<double> WeaponReadyAt=new NetworkVariable<double>(0);
        bool equipmentInitialized;
        public bool SwitchingWeapon => IsSpawned && NetworkManager.ServerTime.Time<WeaponReadyAt.Value;
        void ServerRefreshEquipped()
        {
            if(!IsServer) return;
            byte next=ActiveWeaponSlot>=0 ? (byte)Equipment[ActiveWeaponSlot].Item : (byte)255;
            if(next!=EquippedItem.Value)
            {
                double duration=equipmentInitialized ? WeaponSwitchTiming.Duration(EquippedItem.Value,next) : 0;
                WeaponReadyAt.Value=NetworkManager.ServerTime.Time+duration;
                EquippedItem.Value=next;
            }
            equipmentInitialized=true;
        }
        public int WeaponRounds => ActiveWeaponSlot >= 0 ? Equipment[ActiveWeaponSlot].Data : 0;
        public int WeaponCount { get { int count=0; for(int i=EquipmentLoadout.WeaponA;i<Equipment.Count;i++) if(IsWeaponSlot(i)) count++; return count; } }
        public void RequestNextWeapon() { if(IsOwner) NextWeaponServerRpc(); }
        [ServerRpc] void NextWeaponServerRpc() { ServerNextWeapon(); }
        public void ServerNextWeapon()
        {
            if (!CanAct || ChangingEquipment || SwitchingWeapon || dungeonShooter!=null && dungeonShooter.MeleeAttacking || FishingPhase.Value != 0 || WeaponCount < 2) return;
            int active = ActiveWeaponSlot;
            for(int offset=1;offset<=Equipment.Count;offset++)
            {
                int slot=(active+offset)%Equipment.Count; if(!IsWeaponSlot(slot)) continue;
                GetComponent<PlayerShooter>()?.ServerCancelReload(); EquippedSlot.Value=slot; ServerRefreshEquipped(); return;
            }
        }
        public void ServerSetRounds(int rounds)
        {
            int slot=ActiveWeaponSlot; if(!IsServer || slot<0) return;
            var stack=Equipment[slot]; int next=Mathf.Clamp(rounds,0,ActiveWeapon.Capacity);
            stack.Instance.RecoveryRounds=Mathf.Max(0,stack.Instance.RecoveryRounds-Mathf.Max(0,stack.Data-next));
            stack.Data=next; Equipment[slot]=stack;
        }
        string expeditionId;
        string testMerchantSession,testMerchantExpedition;
        bool testMerchantCommitted;
        void FinishTestMerchant()
        {
            if(!testMerchantCommitted || string.IsNullOrEmpty(testMerchantSession)) return;
            CampRepository.RestockTestMerchant(testMerchantSession,CyberExtraction.UI.NetworkBootstrap.Instance.LeaseOwner(OwnerClientId),testMerchantExpedition);
            testMerchantCommitted=false;
        }
        public void ServerCommitExpedition() { if(!string.IsNullOrEmpty(testMerchantSession)) testMerchantCommitted=true; if(!extractionBanked && !string.IsNullOrEmpty(progressionSession)) { CampRepository.Commit(progressionSession,expeditionId,(byte)MapBuilder.Current); progressionCommitted=true; } }
        public void ServerLoseExpedition(ExpeditionOutcome outcome=ExpeditionOutcome.Interrupted) { FinishTestMerchant(); if(!extractionBanked && !string.IsNullOrEmpty(progressionSession)) { CampRepository.Lose(progressionSession,expeditionId,outcome); progressionFinished=true; ServerRefreshProgress(); ServerSyncCamp(); } }
        public void ServerRollbackExpedition() { testMerchantCommitted=false; if(!extractionBanked && !string.IsNullOrEmpty(progressionSession)) { CampRepository.Rollback(progressionSession,expeditionId); progressionFinished=true; } }
        void DropItems(ExpeditionOutcome outcome=ExpeditionOutcome.Interrupted)
        {
            if(extractionBanked) return;
            ServerLoseExpedition(outcome);
            var mm = MatchManager.Instance; if (mm == null) return;
            foreach (var stack in Grid().Slots) if (!stack.Empty) mm.ServerDropStack(transform.position + (Vector3)(Random.insideUnitCircle * .7f), RecoveryRules.DeathDrop(stack));
            foreach(var stack in Equipment) if(!stack.Empty) mm.ServerDropStack(transform.position+(Vector3)(Random.insideUnitCircle*.7f),RecoveryRules.DeathDrop(stack));
            ServerSetLoadout(new EquipmentLoadout());
            ServerSetGrid(new InventoryGrid(ItemCatalog.BackpackSlots));
        }
        bool CanAct => IsServer && IsAlive && !Lockpicking.Value && DungeonAction.Value<0 && MatchManager.Instance != null && MatchManager.Instance.CurrentState == MatchState.Running;
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
            if(IsServer && IsAlive && health!=null && health.IsDead && Time.unscaledTime>=nextDeathFinalize) HandleDeath(health);
            StepOrganizationReceipt();
            if(IsServer) PublishBankedExtraction();
            if(IsServer) StepEquipmentChange();
            if(IsServer) StepExplorationExperience();
            if(IsServer && FishingPhase.Value!=0) { FishingPhase.Value=0; FishingNibble.Value=false; }
        }
        public void RequestMove(int source, int target) { if (IsOwner) MoveServerRpc(source, target); }
        [ServerRpc] void MoveServerRpc(int source, int target)
        {
            if (!CanAct || ChangingEquipment) return; var grid=Grid();
            if(!grid.MoveTo(source,grid,target)) return;
            GetComponent<PlayerShooter>()?.ServerCancelReload();
            ServerSetGrid(grid);
        }
        public void RequestDrop(int slot) { if (IsOwner) DropServerRpc(slot); }
        [ServerRpc] void DropServerRpc(int slot)
        {
            if (!CanAct || slot < 0 || slot >= Slots.Count || Slots[slot].Empty || RecoveryRules.Bound(Slots[slot])) return;
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
            equipmentInitialized=false; WeaponReadyAt.Value=0;
            if (!IsServer) return; var join = NetworkObject.IsPlayerObject ? Join() : null;
            extractionBanked=extractionStatusPublished=extractionProgressPublished=extractionCampPublished=false;
            nextExtractionPublish=0;
            testMerchantSession=MatchManager.Instance!=null && MatchManager.Instance.Sandbox && join!=null ? join.Session : null;
            testMerchantExpedition=System.Guid.NewGuid().ToString("N"); testMerchantCommitted=false;
            if(MatchManager.Instance != null && MatchManager.Instance.Sandbox) join=null;
            progressionSession=null; progressionCommitted=progressionFinished=false; visitedExperienceRegions.Clear();
            CharacterLevel.Value=1; ConfirmedExperience.Value=0; AvailableAttributePoints.Value=0;
            PendingExperience.Value=LastConfirmedExperience.Value=0;
            int food = 0; InventoryGrid equipment = InventoryGrid.Starter();
            var loadout=new EquipmentLoadout();
            if(join!=null) { expeditionId=System.Guid.NewGuid().ToString("N"); equipment=CampRepository.Begin(join.Session,expeditionId,out food,out loadout); RecyclerLevel.Value=CampRepository.Get(join.Session).RecyclerLevel; }
            else loadout.FillWeapons(equipment);
            progressionSession=join!=null ? join.Session : null;
            if(join!=null) ServerRefreshProgress();
            AttributeInvestment.Value=join!=null ? CampRepository.Get(join.Session).Character.Invested : default;
            foodHealthBonus=Mathf.Max(0,food); WellFed.Value=food>0;
            ServerSetLoadout(loadout);
            ServerSetGrid(equipment); health.Current.Value=health.Maximum.Value;
            GetComponent<PlayerController>()?.ServerResetStamina(); ServerSyncCamp();
        }
        public bool ServerBankExtraction()
        {
            if(!IsServer) return false;
            if(extractionBanked) { PublishBankedExtraction(); return true; }
            if(CanAct && MatchManager.Instance.Sandbox)
            {
                FinishTestMerchant();
                extractionBanked=true; progressionFinished=true;
                extractionProgressPublished=extractionCampPublished=true;
                PublishBankedExtraction(); return true;
            }
            if (!CanAct || !NetworkObject.IsPlayerObject || Join() == null || string.IsNullOrEmpty(progressionSession)) return false;
            if(ChangingEquipment || !CampRepository.Bank(progressionSession,expeditionId,Grid(),Loadout(),CurrentExtractionExit.Value)) return false;
            // Bank returned success: item/XP ownership is now final, before any
            // network callbacks, profile refresh, or further operation can throw.
            extractionBanked=true; progressionFinished=true;
            PublishBankedExtraction(); return true;
        }
        void PublishBankedExtraction()
        {
            if(!IsServer || !IsSpawned || !extractionBanked ||
                (extractionStatusPublished && extractionProgressPublished && extractionCampPublished) || Time.unscaledTime<nextExtractionPublish) return;
            nextExtractionPublish=Time.unscaledTime+2;
            if(!extractionStatusPublished)
            {
                try { Status.Value=(byte)PlayerStatus.Extracted; ApplyStatus(); extractionStatusPublished=true; }
                catch(System.Exception error) { Debug.LogWarning("Extracao salva; estado sera publicado novamente: "+error.Message); }
            }
            if(!extractionProgressPublished)
            {
                try { ServerRefreshProgress(); extractionProgressPublished=true; }
                catch(System.Exception error) { Debug.LogWarning("Extracao salva; progresso sera atualizado novamente: "+error.Message); }
            }
            if(!extractionCampPublished)
            {
                try { ServerSyncCamp(); extractionCampPublished=true; }
                catch(System.Exception error) { Debug.LogWarning("Extracao salva; acampamento sera sincronizado novamente: "+error.Message); }
            }
        }
        void ServerSyncCamp()
        {
            if (!IsSpawned || !NetworkObject.IsPlayerObject || Join() == null || !NetworkManager.ConnectedClients.ContainsKey(OwnerClientId)) return;
            var profile = CampRepository.Get(Join().Session); profile.Ensure();
            SyncCampClientRpc(JsonUtility.ToJson(profile), new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } } });
        }
        [ClientRpc] void SyncCampClientRpc(string json, ClientRpcParams rpcParams = default) { if (IsOwner) CampSession.Accept(json); }
        public void ServerSetStatus(PlayerStatus s)
        {
            if(!IsServer) return;
            if(extractionBanked) { PublishBankedExtraction(); return; }
            Status.Value=(byte)s;
        }
    }
}
