using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Players;
using CyberExtraction.Items;
using CyberExtraction.Bots;
using CyberExtraction.Match;
namespace CyberExtraction.Combat
{
    public partial class LootPickup : NetworkBehaviour
    {
        public static readonly List<LootPickup> All = new List<LootPickup>();
        public int initialValue = GameConfig.LootValue, initialQuantity = 1, initialData;
        public ItemId initialItem = ItemId.Wood;
        public byte initialQuality, initialRarity;
        public ItemInstance initialInstance;
        public NetworkVariable<ItemInstance> Instance=new NetworkVariable<ItemInstance>();
        public bool initialChest;
        public LootTableDefinition initialLootTable;
        public LootRisk initialLootRisk;
        public LootTheme initialLootTheme=LootTheme.General;
        bool lootInitialized;
        public void ConfigureLoot(LootContext context)
        {
            if(IsSpawned || lootInitialized) throw new System.InvalidOperationException("Loot ja foi inicializado.");
            initialLootTable=LootTables.Get(context); initialLootRisk=context.Risk; initialLootTheme=context.Theme;
        }
        public ulong ignoreObject;
        public float ignoreUntil;
        public NetworkVariable<byte> Item = new NetworkVariable<byte>(0), Quality = new NetworkVariable<byte>(0), Rarity = new NetworkVariable<byte>(0), OpenState = new NetworkVariable<byte>(0);
        public NetworkVariable<int> Quantity = new NetworkVariable<int>(1), Data = new NetworkVariable<int>(0), Value = new NetworkVariable<int>(1);
        public NetworkVariable<bool> Chest = new NetworkVariable<bool>(false);
        public NetworkVariable<float> OpenProgress = new NetworkVariable<float>(0);
        public NetworkVariable<ulong> Opener = new NetworkVariable<ulong>(0);
        public LootContents Contents { get; private set; }
        int openingDamage; float openingStarted;
        void Awake() { Contents = new LootContents(()=>contentsDirty=true); ServerVisibility.Install(GetComponent<NetworkObject>()); }
        public override void OnNetworkSpawn()
        {
            All.Add(this);
            if(!IsServer) ClearContentsView();
            if(IsServer && !lootInitialized)
            {
                lootInitialized=true;
                Item.Value=(byte)initialItem; Quantity.Value=initialQuantity; Data.Value=initialData; Quality.Value=initialQuality;
                Instance.Value=initialInstance.HasIdentity ? initialInstance :
                    initialQuantity>0 && !initialChest && EquipmentCatalog.IsDefined(initialItem) ? EquipmentCatalog.RollInstance(initialItem,ItemRarity.Common) :
                    initialQuantity>0 && !initialChest && (ItemCatalog.Weapon(initialItem) || ItemCatalog.Equipment(initialItem)) ? ItemInstance.Create() : default;
                Value.Value=initialQuantity*ItemCatalog.Score(initialItem); Chest.Value=initialChest; Rarity.Value=initialRarity;
                if(initialChest)
                {
                    var grid=new InventoryGrid();
                    if(initialLabLock)
                    {
                        grid.Add(new ItemStack(ItemId.Metal,3)); grid.Add(new ItemStack(ItemId.CrossbowBolt,5));
                        grid.Add(new ItemStack(ItemId.Lockpick,1));
                    }
                    else if(initialFixtureKind!=3)
                    {
                        var context=new LootContext(MapBuilder.Current,initialLootTable!=null ? initialLootTable.Source : LootSource.CommonContainer,initialLootRisk,initialLootTheme);
                        var table=initialLootTable!=null ? initialLootTable : LootTables.Get(context);
                        foreach(var stack in LootRoller.Roll(table,context))
                            if(!grid.Add(stack)) throw new System.InvalidOperationException("Bundle de loot nao cabe no recipiente.");
                    }
                    foreach(var stack in grid.Slots) Contents.Add(stack);
                }
            }
            InitializeLock();
            InitializeChestFacing();
            GetComponent<SpriteRenderer>().color=ItemCatalog.Tint((ItemId)Item.Value);
            if(Chest.Value)
            {
                transform.localScale=new Vector3(1.5f,1.2f,1); GetComponent<SpriteRenderer>().color=Color.clear;
                if(FixtureKind.Value==0)
                {
                    InitializeChestCollision();
                    CyberExtraction.Enemies.ChestAppearance.Build(transform,Rarity.Value==2 ? new Color(.65f,.4f,.8f) : new Color(.5f,.35f,.2f));
                }
                else BuildFixtureAppearance();
            }
            else if(!Application.isBatchMode) ItemIconLibrary.ApplyWorld(gameObject,(ItemId)Item.Value);
            if(!IsServer && Chest.Value) RequestContents();
        }
        public override void OnNetworkDespawn()
        {
            contentReaders.Clear();
            contentTokens.Clear(); ClearContentsView();
            ChestFacing.OnValueChanged-=ChestFacingChanged;
            if(chestCollider!=null) chestCollider.enabled=false;
            CancelLock(false); Locked.OnValueChanged-=LockChanged; OpenState.OnValueChanged-=FixtureOpened; All.Remove(this);
            // Visibility hide/show can respawn the same pickup; do not retain visual subscriptions.
            var root=transform.Find("ChestAppearance");
            if(root!=null) { root.name="DespawnedChestAppearance"; root.gameObject.SetActive(false); Destroy(root.gameObject); }
        }
        void OnTriggerEnter2D(Collider2D other)
        {
            if(!IsServer || !IsSpawned || Chest.Value) return;
            var inv=other.GetComponentInParent<PlayerInventory>();
            if(inv!=null && inv.NetworkObjectId==ignoreObject && Time.time<ignoreUntil) return;
            var stack=new ItemStack { Item=(ItemId)Item.Value,Quantity=Quantity.Value,Quality=Quality.Value,Data=Data.Value,Instance=Instance.Value };
            if(inv!=null && inv.IsAlive && inv.ServerAddStack(stack)) { CyberExtraction.Diagnostics.MatchTelemetry.Record("pickup",inv.NetworkObjectId,Quantity.Value); NetworkObject.Despawn(); }
        }
        public static LootPickup NearestChest(Vector2 point)
        {
            LootPickup best=null; float distance=3;
            foreach(var loot in All) if(loot!=null && loot.IsSpawned && loot.Chest.Value && Vector2.Distance(point,loot.transform.position)<distance)
            { best=loot; distance=Vector2.Distance(point,loot.transform.position); } return best;
        }
        PlayerInventory Sender(ulong client)
        {
            if(!NetworkManager.ConnectedClients.TryGetValue(client,out var connection) || connection.PlayerObject==null) return null;
            var inv=connection.PlayerObject.GetComponent<PlayerInventory>();
            if(inv!=null && inv.GetComponent<PlayerShooter>() is PlayerShooter attacker && attacker.MeleeAttacking) return null;
            return inv!=null && inv.IsAlive && inv.DungeonAction.Value<0 && MatchManager.Instance!=null && MatchManager.Instance.CurrentState==MatchState.Running && Vector2.Distance(inv.transform.position,transform.position)<=3 && LockLineClear(inv.transform.position) ? inv : null;
        }
        public void RequestOpen() { OpenRpc(); }
        public void RequestCancelOpen() { CancelOpenRpc(); }
        [Rpc(SendTo.Server, InvokePermission=RpcInvokePermission.Everyone)] void CancelOpenRpc(RpcParams rpc=default)
        { if(Chest.Value && OpenState.Value==1 && Opener.Value==rpc.Receive.SenderClientId) { OpenState.Value=0; OpenProgress.Value=0; } }
        [Rpc(SendTo.Server, InvokePermission=RpcInvokePermission.Everyone)] void OpenRpc(RpcParams rpc=default)
        {
            if(!Chest.Value || OpenState.Value!=0) return;
            var inv=Sender(rpc.Receive.SenderClientId); if(inv==null) return;
            if(Locked.Value) { BeginLock(inv,rpc.Receive.SenderClientId); return; }
            if(FixtureKind.Value==3) return;
            Opener.Value=rpc.Receive.SenderClientId; openingDamage=inv.GetComponent<Health>().DamageVersion.Value;
            openingStarted=Time.time; OpenState.Value=1; OpenProgress.Value=0;
            Noise.Emit(transform.position,12);
        }
        void Update()
        {
            TickLock();
            if(!IsServer || !IsSpawned) return;
            if(OpenState.Value==1)
            {
                var inv=Sender(Opener.Value);
                if(inv==null || inv.GetComponent<Health>().DamageVersion.Value!=openingDamage) { OpenState.Value=0; OpenProgress.Value=0; }
                else
                {
                    OpenProgress.Value=Mathf.Clamp01((Time.time-openingStarted)/(2+Rarity.Value*2));
                    if(OpenProgress.Value>=1) OpenState.Value=2;
                }
            }
            TickContentsAccess();
        }
        public InventoryGrid Grid() { var grid=new InventoryGrid(); for(int i=0;i<Mathf.Min(9,Contents.Count);i++) grid.Slots[i]=Contents[i]; return grid; }
        public void CopyGrid(InventoryGrid target) { for(int i=0;i<9;i++) target.Slots[i]=i<Contents.Count ? Contents[i] : default; }
        public void RequestTransfer(int source,int target,bool fromChest) { TransferRpc(source,target,fromChest); }
        [Rpc(SendTo.Server, InvokePermission=RpcInvokePermission.Everyone)] void TransferRpc(int source,int target,bool fromChest,RpcParams rpc=default)
        {
            var inv=Sender(rpc.Receive.SenderClientId); if(inv==null || !CanReadContents(rpc.Receive.SenderClientId)) return;
            var chest=Grid(); var pack=inv.Grid();
            if(source<0 || source>=(fromChest ? ItemCatalog.InventorySlots : ItemCatalog.BackpackSlots) || target<0 || target>=(fromChest ? ItemCatalog.BackpackSlots : ItemCatalog.InventorySlots) ||
                RecoveryRules.Bound((fromChest ? chest : pack).Slots[source]) || RecoveryRules.Bound((fromChest ? pack : chest).Slots[target])) return;
            if(!(fromChest ? chest.MoveTo(source,pack,target) : pack.MoveTo(source,chest,target))) return;
            for(int i=0;i<9;i++) Contents[i]=chest.Slots[i]; inv.ServerSetGrid(pack);
        }
        public void RequestDropContents(int slot) { DropContentsRpc(slot); }
        public void RequestReorder(int source,int target) { ReorderRpc(source,target); }
        [Rpc(SendTo.Server, InvokePermission=RpcInvokePermission.Everyone)] void ReorderRpc(int source,int target,RpcParams rpc=default)
        {
            if(!CanReadContents(rpc.Receive.SenderClientId)) return;
            var grid=Grid(); if(!grid.MoveTo(source,grid,target)) return;
            for(int i=0;i<9;i++) Contents[i]=grid.Slots[i];
        }
        [Rpc(SendTo.Server, InvokePermission=RpcInvokePermission.Everyone)] void DropContentsRpc(int slot,RpcParams rpc=default)
        {
            if(!CanReadContents(rpc.Receive.SenderClientId) || slot<0 || slot>=Contents.Count || Contents[slot].Empty || RecoveryRules.Bound(Contents[slot])) return;
            MatchManager.Instance.ServerDropStack(transform.position+(Vector3)(Random.insideUnitCircle*.8f),Contents[slot]); Contents[slot]=default;
        }
    }
}
