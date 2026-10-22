using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using CyberExtraction.Camp;
using CyberExtraction.Items;
using CyberExtraction.Players;
using CyberExtraction.Combat;
using CyberExtraction.Match;
namespace CyberExtraction.UI
{
    public class InventoryWindow : MonoBehaviour
    {
        public static InventoryWindow Instance { get; private set; }
        public static bool IsOpen => Instance!=null && Instance.open;
        public static Rect Bounds => UguiBaseHub.Active ? UguiBaseHub.ContentBounds : UguiBaseHub.Compact ? new Rect(16,24,UiLayout.Width-32,552) : new Rect(UiLayout.Width*.06f,UiLayout.Height*.13f,UiLayout.Width*.88f,UiLayout.Height*.72f);
        public static bool ContainsControlPoint(Vector2 point)
        {
            var gui = UiLayout.Point(point);
            return IsOpen || (CampController.Active && !CampController.Instance.HasActivity && !CampController.Instance.ExpeditionPanelOpen && HudLayout.CampInventory.Contains(gui)) || (CanInteract && HudLayout.HitBounds(HudControl.Interaction).Contains(gui));
        }
        static bool CanInteract=>!CampController.Active && !UiLayout.BlocksGameplay && Local()!=null && Local().IsAlive && Local().FishingPhase.Value==0;
        bool open, safeChest, sawOpening;
        bool worldMode;
        public LootPickup Recipient=>worldChest;
        public bool WorldMode=>worldMode;
        public bool LootReady=>worldMode && worldChest!=null && worldChest.HasContents && worldChest.CanInspectContents(Local());
        public bool OrganizationPending=>organizingCamp && CampSession.Pending || Local()!=null && Local().OrganizePending;
        public bool SlotActionsAllowed=>open && !OrganizationPending && (!CampController.Active || !CampSession.Pending) && (!worldMode || LootReady);
        public bool TwoGrids=>(safeChest && CampController.Active) || worldMode;
        public string Notice=>notice;
        public event System.Action Changed;
        public System.Func<bool> CancelSelection { get; set; }
        bool organizingCamp;
        PlayerInventory organizationPlayer;
        public bool CanOrganize(int grid)
        {
            if(!SlotActionsAllowed || grid<0 || grid>1 || grid==1 && (!CampController.Active || !safeChest || worldMode)) return false;
            if(CampController.Active) return CampSession.Synchronized && !CampSession.Pending && CampSession.Progress.ExpeditionState==0 &&
                CampController.Instance!=null && !CampController.Instance.HasActivity;
            var inv=Local(); return grid==0 && inv!=null && inv.CanOrganize;
        }
        public void Organize(int grid)
        {
            if(!CanOrganize(grid) || dragSlot>=0) return;
            notice=null;
            if(CampController.Active)
            {
                organizingCamp=true;
                if(!CampSession.Act(new CampAction {Kind=CampActionKind.Organize,SourceGrid=grid}))
                { organizingCamp=false; notice="Não foi possível organizar. Tente novamente."; }
            }
            else
            {
                BindOrganizationPlayer();
                if(organizationPlayer==null || !organizationPlayer.RequestOrganize()) notice="Não foi possível organizar. Tente novamente.";
            }
            Changed?.Invoke();
        }
        void BindOrganizationPlayer()
        {
            var next=Local(); if(next==organizationPlayer) return;
            if(organizationPlayer!=null) organizationPlayer.OrganizationFinished-=OrganizationCompleted;
            organizationPlayer=next;
            if(organizationPlayer!=null) organizationPlayer.OrganizationFinished+=OrganizationCompleted;
        }
        void OrganizationCompleted(byte result)
        {
            if(!open) return;
            notice=result==1 ? "Mochila organizada" : result==2 ? "Já está organizado" : "Inventário ou estado mudou. Tente organizar novamente.";
            Changed?.Invoke();
        }
        void CampOrganizationChanged()
        {
            if(!organizingCamp || CampSession.Pending) return;
            organizingCamp=false;
            if(!open) return;
            notice=CampSession.Synchronized ? CampSession.Message : "Conecte novamente para atualizar o inventário.";
            Changed?.Invoke();
        }
        public ItemStack Equipped(int slot)
        {
            if(slot<0 || slot>=EquipmentLoadout.Count) return default;
            if(CampController.Active) return CampSession.Progress.Equipment.Slots[slot];
            var inv=Local(); return inv!=null && slot<inv.Equipment.Count ? inv.Equipment[slot] : default;
        }
        public void ExchangeEquipment(int bag,int slot)
        {
            if(!SlotActionsAllowed || bag<0 || bag>=ItemCatalog.BackpackSlots || slot<0 || slot>=EquipmentLoadout.Count) return;
            if(CampController.Active)
            { if(CampSession.Synchronized && !CampSession.Pending) CampSession.Act(new CampAction {Kind=CampActionKind.ExchangeEquipment,Source=bag,Target=slot}); }
            else Local()?.RequestEquipmentExchange(bag,slot);
            Changed?.Invoke();
        }
        public void CopyPresentation(InventoryGrid pack,InventoryGrid other)
        {
            System.Array.Clear(pack.Slots,0,pack.Slots.Length); System.Array.Clear(other.Slots,0,other.Slots.Length);
            if(CampController.Active)
            { System.Array.Copy(CampSession.Progress.Pack.Slots,pack.Slots,ItemCatalog.BackpackSlots); if(safeChest) System.Array.Copy(CampSession.Progress.Chest.Slots,other.Slots,ItemCatalog.SafeChestSlots); }
            else { Local()?.CopyGrid(pack); if(LootReady) worldChest.CopyGrid(other); }
        }
        public void Transfer(int fromGrid,int source,int toGrid,int target,bool tap=false)
        {
            if(!SlotActionsAllowed || source<0 || source>=Capacity(fromGrid) || target<0 || target>=Capacity(toGrid) || fromGrid<0 || fromGrid>1 || toGrid<0 || toGrid>1 || (!TwoGrids && (fromGrid!=0 || toGrid!=0))) return;
            var inv=Local();
            if(CampController.Active) { if(CampSession.Synchronized && !CampSession.Pending) CampSession.Act(new CampAction {Kind=CampActionKind.Transfer,Source=source,Target=target,SourceGrid=fromGrid,TargetGrid=toGrid}); }
            else if(inv!=null && inv.IsAlive)
            {
                if(worldMode && (worldChest==null || !worldChest.IsSpawned || Vector2.Distance(inv.transform.position,worldChest.transform.position)>3)) { Close(); return; }
                if(tap && fromGrid==1) TakeLoot(inv,source);
                else if(fromGrid==0 && toGrid==0) inv.RequestMove(source,target);
                else if(worldChest!=null && fromGrid==1 && toGrid==1) worldChest.RequestReorder(source,target);
                else if(worldChest!=null && fromGrid!=toGrid) worldChest.RequestTransfer(source,target,fromGrid==1);
            }
            Changed?.Invoke();
        }
        public void Discard(int grid,int slot,ItemStack captured)
        {
            if(!SlotActionsAllowed || slot<0 || slot>=Capacity(grid) || grid<0 || grid>1 || (!TwoGrids && grid!=0)) return;
            if(CampController.Active)
            {
                if(!CampSession.Synchronized || CampSession.Pending) return;
                CampSession.Act(new CampAction {Kind=CampActionKind.Discard,Source=slot,SourceGrid=grid,ExpectedItem=captured});
            }
            else if(Local()!=null && Local().IsAlive)
            { if(grid==0) Local().RequestDrop(slot); else if(worldChest!=null && worldChest.IsSpawned) worldChest.RequestDropContents(slot); }
        }
        int dragSlot=-1, dragGrid;
        ItemStack dragItem;
        string notice;
        LootPickup worldChest, pendingChest;
        bool receivedContents;
        float contentsStarted,pendingStarted;
        readonly InventoryGrid packView=new InventoryGrid(ItemCatalog.BackpackSlots), chestView=new InventoryGrid(ItemCatalog.SafeChestSlots);
        public int Capacity(int grid)=>grid==0 ? ItemCatalog.BackpackSlots : CampController.Active && safeChest ? ItemCatalog.SafeChestSlots : ItemCatalog.InventorySlots;
        public static PlayerInventory Local()
        { var nm=NetworkManager.Singleton; var player=nm!=null && nm.IsClient && nm.SpawnManager!=null ? nm.SpawnManager.GetLocalPlayerObject() : null; return player!=null ? player.GetComponent<PlayerInventory>() : null; }
        void Awake() { Instance=this; CampSession.Changed+=CampOrganizationChanged; }
        public void OpenSafe() { Close(); open=true; safeChest=true; notice=null; Changed?.Invoke(); }
        public void OpenBag() { if(UguiBaseHub.Active) { UguiBaseHub.Show(BasePage.Inventory); return; } if(!CampController.Active || CampController.Instance.HasActivity) return; Close(); open=true; safeChest=false; notice=null; Changed?.Invoke(); }
        public void OpenWorld(LootPickup chest)
        {
            if(chest==null || !chest.CanInspectContents(Local())) return;
            Close(); worldChest=chest; worldMode=true; safeChest=false; open=true;
            receivedContents=false; contentsStarted=Time.unscaledTime;
            notice="Carregando conteúdo autorizado...";
            worldChest.Contents.OnListChanged+=WorldContentsChanged;
            worldChest.RefreshContentsView(); RefreshWorldAccess(); Changed?.Invoke();
        }
        void WorldContentsChanged(NetworkListEvent<ItemStack> change)
        { dragSlot=-1; dragItem=default; RefreshWorldAccess(); Changed?.Invoke(); }
        void RefreshWorldAccess()
        {
            var inv=Local();
            if(pendingChest!=null && (!pendingChest.IsSpawned || inv==null || !inv.IsAlive ||
                Vector2.Distance(inv.transform.position,pendingChest.transform.position)>3 ||
                !pendingChest.LockLineClear(inv.transform.position) || Time.unscaledTime-pendingStarted>12))
            { Close(); return; }
            if(!worldMode) return;
            if(worldChest==null || !worldChest.CanInspectContents(inv)) { Close(); return; }
            if(!worldChest.HasContents)
            {
                if(receivedContents || Time.unscaledTime-contentsStarted>10) { Close(); return; }
                return;
            }
            if(!receivedContents) { receivedContents=true; notice=null; Changed?.Invoke(); }
        }
        public void ToggleBag()
        {
            if(UguiBaseHub.Active) { UguiBaseHub.Show(IsOpen ? BasePage.Preparation : BasePage.Inventory); return; }
            if(ExpeditionMenu.IsOpen) return;
            var inv=Local();
            if(LockpickWindow.IsOpen) return;
            if((CampController.Active && (CampController.Instance.HasActivity || CampController.Instance.ExpeditionPanelOpen)) || (inv!=null && inv.FishingPhase.Value!=0)) return;
            if(open) { Close(); return; }
            open=!open; safeChest=worldMode=false; worldChest=null; pendingChest=null; dragSlot=-1; notice=null; Changed?.Invoke();
        }
        public void Close()
        {
            var previous=worldChest;
            if(worldChest!=null) worldChest.Contents.OnListChanged-=WorldContentsChanged;
            if(pendingChest!=null && pendingChest.IsSpawned) pendingChest.RequestCancelOpen();
            if(!CampController.Active) Local()?.CancelEquipmentChange();
            open=worldMode=safeChest=receivedContents=false; dragSlot=-1; dragItem=default; worldChest=null; pendingChest=null; sawOpening=false; notice=null;
            if(previous!=null) previous.ClearContentsView();
            System.Array.Clear(packView.Slots,0,packView.Slots.Length); System.Array.Clear(chestView.Slots,0,chestView.Slots.Length);
            Changed?.Invoke();
        }
        public void Interact()
        {
            var inv=Local(); if(inv==null || !inv.IsAlive) return;
            var chest=LootPickup.NearestChest(inv.transform.position);
            var match=MatchManager.Instance; int action=match!=null ? match.ContextDungeonAction(inv,chest) : -1;
            if(action>=0) { inv.RequestDungeonAction(action); return; }
            var passage=ForestPassage.Nearby(inv);
            if(passage!=null && (chest==null || Vector2.Distance(inv.transform.position,passage.transform.position)<Vector2.Distance(inv.transform.position,chest.transform.position)))
            { MatchManager.Instance.RequestPassage(passage.RouteId); return; }
            var mimic=CyberExtraction.Enemies.Monster.NearestDisguised(inv.transform.position);
            if(mimic!=null && (chest==null || Vector2.Distance(inv.transform.position,mimic.transform.position)<Vector2.Distance(inv.transform.position,chest.transform.position)))
            { mimic.RequestOpenDisguise(); return; }
            if(chest==null) return;
            if(chest.Locked.Value) { Close(); chest.RequestOpen(); return; }
            if(chest.FixtureKind.Value==3) return;
            if(chest.OpenState.Value==2) OpenWorld(chest);
            else { pendingChest=chest; pendingStarted=Time.unscaledTime; sawOpening=false; chest.RequestOpen(); }
        }
        void Update()
        {
            BindOrganizationPlayer();
            RefreshWorldAccess();
            var actor=Local();
            if(actor!=null && actor.DungeonAction.Value>=0 && (open || UiLayout.BlocksGameplay)) actor.RequestDungeonAction(-1);
            if(HudLayoutEditor.IsOpen || HudLayoutEditor.ConsumedBack) return;
            if(ExpeditionMenu.IsOpen) return;
            if(GameOptionsMenu.IsOpen || CampTestPanel.IsOpen || GameOptionsMenu.ConsumedBack || Minimap.ConsumedBack) return;
            if(LockpickWindow.IsOpen || UguiBaseHub.ConsumedBack) return;
            {
                if(!TestLabPanel.IsOpen && GameInput.BagPressed && (!CampController.Active || !CampController.Instance.HasActivity)) ToggleBag();
                if(!UguiBaseHub.Active && GameInput.BackPressed && !(CancelSelection?.Invoke()??false)) Close();
                if(!TestLabPanel.IsOpen && !CampController.Active && !open && GameInput.InteractPressed) Interact();
            }
            if(pendingChest!=null)
            {
                if(pendingChest.OpenState.Value==1) sawOpening=true;
                if(pendingChest.OpenState.Value==2) OpenWorld(pendingChest);
                else if(sawOpening && pendingChest.OpenState.Value==0) pendingChest=null;
            }
            var inv=Local();
            if(!CampController.Active && open && (inv==null || !inv.IsAlive)) Close();
            if(worldChest!=null && inv!=null && Vector2.Distance(inv.transform.position,worldChest.transform.position)>3) Close();
            if(worldMode && (worldChest==null || !worldChest.IsSpawned)) Close();
        }
        Rect SlotRect(int grid,int slot,bool two)
        {
            var bounds=Bounds; float panelWidth=bounds.width/(two ? 2 : 1);
            float size=Mathf.Min(panelWidth*.27f,bounds.height*.24f), x=bounds.x+(two ? grid : 0)*panelWidth+(panelWidth-size*3)/2;
            return new Rect(x+(slot%3)*size,bounds.y+60+(slot/3)*size,size-5,size-5);
        }
        void OnGUI()
        {
            if(UguiGameHud.Active) return;
            if(HudLayoutEditor.IsOpen) return;
            if(ExpeditionMenu.IsOpen) return;
            if(GameOptionsMenu.IsOpen) return;
            using var themeFrame=UiLayout.Begin();
            if(LockpickWindow.IsOpen) return;
            if(TestLabPanel.IsOpen) return;
            var old=GUI.matrix; GUI.matrix=UiLayout.Matrix;
            if(!open)
            {
                if(CampController.Active && (CampController.Instance.HasActivity || CampController.Instance.ExpeditionPanelOpen)) { GUI.matrix=old; return; }
                if(CampController.Active) { if(GUI.Button(HudLayout.CampInventory,"Mochila [I]")) ToggleBag(); }
                else if(CanInteract)
                {
                    if(UguiGameHud.Active) { GUI.matrix=old; return; }
                    var local=Local(); var nearest=LootPickup.NearestChest(local.transform.position);
                    var match=MatchManager.Instance; int mechanism=match!=null ? match.ContextDungeonAction(local,nearest) : -1;
                    if(mechanism>=0)
                    {
                        var interactionRect=HudLayout.Interaction; HudArt.Panel(interactionRect);
                        string text=match.DungeonActionLabel(local,mechanism)+(HudLayout.Touch ? "" : " [E]");
                        GUI.Label(new Rect(interactionRect.x+5,interactionRect.y+2,interactionRect.width-10,interactionRect.height-7),text,HudTheme.Label(12,TextAnchor.MiddleCenter));
                        HudTheme.Meter(new Rect(interactionRect.x+8,interactionRect.yMax-5,interactionRect.width-16,3),local.DungeonActionProgress,HudTheme.Text);
                        if(GUI.Button(HudLayout.HitBounds(HudControl.Interaction),GUIContent.none,HudTheme.InvisibleButton)) Interact();
                        GUI.matrix=old; return;
                    }
                    var mimic=CyberExtraction.Enemies.Monster.NearestDisguised(local.transform.position);
                    string action=nearest!=null ? (nearest.Locked.Value ? "Abrir fechadura" : nearest.OpenState.Value==2 ? "Saquear" : nearest.FixtureKind.Value==3 ? "Abrir porta" : nearest.FixtureKind.Value!=0 ? "Abrir armário" : "Abrir baú") : mimic!=null ? "Abrir baú" : null;
                    var passage=ForestPassage.Nearby(local);
                    if(passage!=null && (nearest==null || Vector2.Distance(local.transform.position,passage.transform.position)<Vector2.Distance(local.transform.position,nearest.transform.position))) action=passage.Route.Action;
                    if(action!=null)
                    {
                        bool container=nearest!=null && !nearest.Locked.Value && nearest.FixtureKind.Value!=3 &&
                            !(passage!=null && Vector2.Distance(local.transform.position,passage.transform.position)<Vector2.Distance(local.transform.position,nearest.transform.position));
                        float progress=container ? nearest.OpenProgress.Value : 0;
                        bool opening=container && nearest.OpenState.Value==1;
                        bool busy=opening && nearest.Opener.Value!=local.OwnerClientId;
                        string interactionLabel=busy ? "Em uso" : opening ? "Abrindo" : action;
                        if(container && nearest.OpenState.Value!=2 && !busy)
                            interactionLabel+=" · "+Mathf.CeilToInt((2+nearest.Rarity.Value*2)*(1-progress))+" s";
                        if(!HudLayout.Touch) interactionLabel+=" [E]";
                        var r=HudLayout.Interaction; HudArt.Panel(r);
                        bool clicked=GUI.Button(HudLayout.HitBounds(HudControl.Interaction),GUIContent.none,HudTheme.InvisibleButton);
                        HudArt.Icon(HudLayout.Detail(HudControl.Interaction,10,11,21,21),HudSymbol.Chest,HudTheme.Text);
                        GUI.Label(HudLayout.Detail(HudControl.Interaction,37,3,139,32),interactionLabel,HudTheme.Label(HudLayout.TextSize(HudControl.Interaction,13),TextAnchor.MiddleCenter));
                        if(container && nearest.OpenState.Value!=2) HudTheme.Meter(new Rect(r.x+8,r.yMax-7,r.width-16,3),progress,HudTheme.Text);
                        if(clicked && !opening) Interact();
                    }
                }
                GUI.matrix=old; return;
            }
            bool camp=CampController.Active, two=(safeChest && camp) || worldChest!=null;
            int firstGrid=0;
            var inv=Local(); CopyPresentation(packView,chestView);
            var pack=camp ? CampSession.Progress.Pack : packView;
            var chest=safeChest ? CampSession.Progress.Chest : chestView;
            Rect bounds=Bounds; GUI.Box(bounds,"");
            var label=new GUIStyle(GUI.skin.label) { fontSize=Mathf.Clamp((int)(UiLayout.Height/32),16,28),alignment=TextAnchor.MiddleCenter,wordWrap=true };
            var cellStyle=new GUIStyle(GUI.skin.box) { fontSize=label.fontSize,alignment=TextAnchor.MiddleCenter,wordWrap=true };
            if(two)
            {
                GUI.Label(new Rect(bounds.x,bounds.y+8,bounds.width/2,40),"MOCHILA",label);
                GUI.Label(new Rect(bounds.center.x,bounds.y+8,bounds.width/2,40),worldChest!=null ? (LootReady ? "SAQUE" : "CARREGANDO SAQUE...") : "BAU SEGURO",label);
            }
            else GUI.Label(new Rect(bounds.x+10,bounds.y+8,bounds.width-100,40),"MOCHILA - 9 SLOTS",label);
            if(GUI.Button(new Rect(bounds.xMax-90,bounds.y+8,75,35),"Fechar")) { Close(); GUI.matrix=old; return; }
            var evt=Event.current;
            var textStyle=new GUIStyle(cellStyle) { fontSize=Mathf.Clamp((int)(SlotRect(0,0,two).height*.12f),11,16) };
            for(int grid=firstGrid;grid<firstGrid+(two ? 2 : 1);grid++) for(int slot=0;slot<Capacity(grid);slot++)
            {
                ItemStack stack=(grid==0 ? pack : chest).Slots[slot]; Rect rect=SlotRect(grid,slot,two);
                GUI.Box(rect,stack.Empty ? "-" : "",cellStyle);
                if(!stack.Empty)
                {
                    float iconHeight=rect.height*.46f;
                    ItemIconGui.Draw(new Rect(rect.x+6,rect.y+4,rect.width-12,iconHeight),stack.Item);
                    string text=ItemCatalog.Name(stack.Item)+" x"+stack.Quantity+(ItemCatalog.Melee(stack.Item) ? "\nCorpo a corpo" : ItemCatalog.Weapon(stack.Item) ? "\n"+stack.Data+"/"+WeaponCatalog.Get(stack.Item).Capacity : stack.Quality>0 ? "\nQualidade "+stack.Quality : "");
                    GUI.Label(new Rect(rect.x+3,rect.y+iconHeight+5,rect.width-6,rect.height-iconHeight-7),text,textStyle);
                }
                if(SlotActionsAllowed && evt.type==EventType.MouseDown && evt.button==0 && rect.Contains(evt.mousePosition) && !stack.Empty)
                { dragSlot=slot; dragGrid=grid; dragItem=stack; evt.Use(); }
            }
            GUI.Label(new Rect(bounds.x+10,bounds.yMax-52,bounds.width-20,44),!string.IsNullOrEmpty(notice) ? notice : worldChest!=null ? "Toque para pegar. Arraste para reorganizar ou para fora para descartar." : "Arraste entre slots. Arraste para fora para jogar no chao. Pilhas: 20.",label);
            if(!SlotActionsAllowed) { dragSlot=-1; dragItem=default; }
            if(dragSlot>=0)
            {
                var dragRect=new Rect(evt.mousePosition.x+12,evt.mousePosition.y+12,90,65);
                GUI.Box(dragRect,"");
                ItemIconGui.Draw(new Rect(dragRect.x+4,dragRect.y+3,55,55),dragItem.Item);
                GUI.Label(new Rect(dragRect.x+58,dragRect.y+20,30,25),"x"+dragItem.Quantity);
                if(evt.type==EventType.MouseUp)
                {
                    bool placed=false;
                    for(int grid=firstGrid;grid<firstGrid+(two ? 2 : 1);grid++) for(int slot=0;slot<Capacity(grid);slot++)
                        if(SlotRect(grid,slot,two).Contains(evt.mousePosition))
                        {
                            if(worldChest!=null && dragGrid==1 && grid==1 && slot==dragSlot) TakeLoot(inv,dragSlot);
                            else if(camp) CampSession.Act(new CampAction { Kind=CampActionKind.Transfer,Source=dragSlot,Target=slot,SourceGrid=dragGrid,TargetGrid=grid });
                            else if(dragGrid==0 && grid==0) inv.RequestMove(dragSlot,slot);
                            else if(worldChest!=null && dragGrid==1 && grid==1) worldChest.RequestReorder(dragSlot,slot);
                            else if(worldChest!=null && dragGrid!=grid) worldChest.RequestTransfer(dragSlot,slot,dragGrid==1);
                            placed=true;
                        }
                    if(!placed && !bounds.Contains(evt.mousePosition))
                    {
                        if(camp)
                        {
                            var drop=new CampAction { Kind=CampActionKind.Discard,Source=dragSlot,SourceGrid=dragGrid };
                            drop.ExpectedItem=dragItem; CampSession.Act(drop);
                        }
                        else if(dragGrid==0) inv.RequestDrop(dragSlot);
                        else if(worldChest!=null) worldChest.RequestDropContents(dragSlot);
                    }
                    dragSlot=-1; evt.Use();
                }
            }
            GUI.matrix=old;
        }
        void TakeLoot(PlayerInventory inv,int source)
        {
            if(!SlotActionsAllowed || inv==null || worldChest==null || source<0 || source>=worldChest.Contents.Count) return;
            var item=worldChest.Contents[source];
            for(int slot=0;slot<inv.Slots.Count;slot++)
            {
                var target=inv.Slots[slot];
                if(target.Empty || (!ItemCatalog.Weapon(item.Item) && target.Same(item) && target.Quantity<ItemCatalog.Limit(item.Item)))
                { worldChest.RequestTransfer(source,slot,true); notice=null; return; }
            }
            notice="Mochila cheia. Abra a mochila para liberar um slot.";
        }
        void OnDestroy()
        {
            CampSession.Changed-=CampOrganizationChanged;
            if(organizationPlayer!=null) organizationPlayer.OrganizationFinished-=OrganizationCompleted;
            if(worldChest!=null) worldChest.Contents.OnListChanged-=WorldContentsChanged;
            if(Instance==this) Instance=null;
        }
    }
}
