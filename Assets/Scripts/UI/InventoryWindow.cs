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
        public static Rect Bounds => new Rect(UiLayout.Width*.06f,UiLayout.Height*.13f,UiLayout.Width*.88f,UiLayout.Height*.72f);
        public static bool ContainsControlPoint(Vector2 point)
        {
            var gui = UiLayout.Point(point);
            return IsOpen || (CampController.Active && !CampController.Instance.HasActivity && !CampController.Instance.ExpeditionPanelOpen && HudLayout.CampInventory.Contains(gui)) || (CanInteract && HudLayout.Interaction.Contains(gui));
        }
        static bool CanInteract=>!CampController.Active && !UiLayout.BlocksGameplay && Local()!=null && Local().IsAlive && Local().FishingPhase.Value==0;
        bool open, safeChest, sawOpening;
        int dragSlot=-1, dragGrid;
        ItemStack dragItem;
        string notice;
        LootPickup worldChest, pendingChest;
        readonly InventoryGrid packView=new InventoryGrid(), chestView=new InventoryGrid();
        public static PlayerInventory Local()
        { var nm=NetworkManager.Singleton; var player=nm!=null && nm.IsClient && nm.SpawnManager!=null ? nm.SpawnManager.GetLocalPlayerObject() : null; return player!=null ? player.GetComponent<PlayerInventory>() : null; }
        void Awake() { Instance=this; }
        public void OpenSafe() { open=true; safeChest=true; worldChest=null; notice=null; }
        public void OpenBag() { if(!CampController.Active || CampController.Instance.HasActivity) return; open=true; safeChest=false; worldChest=null; pendingChest=null; dragSlot=-1; notice=null; }
        public void OpenWorld(LootPickup chest) { if(chest==null || chest.FixtureKind.Value==3) return; worldChest=chest; safeChest=false; open=true; pendingChest=null; notice=null; }
        public void ToggleBag()
        {
            if(ExpeditionMenu.IsOpen) return;
            var inv=Local();
            if(LockpickWindow.IsOpen) return;
            if((CampController.Active && (CampController.Instance.HasActivity || CampController.Instance.ExpeditionPanelOpen)) || (inv!=null && inv.FishingPhase.Value!=0)) return;
            open=!open; safeChest=false; worldChest=null; pendingChest=null; dragSlot=-1; notice=null;
        }
        public void Close() { if(pendingChest!=null) pendingChest.RequestCancelOpen(); open=false; dragSlot=-1; worldChest=null; pendingChest=null; sawOpening=false; }
        public void Interact()
        {
            var inv=Local(); if(inv==null || !inv.IsAlive) return;
            var chest=LootPickup.NearestChest(inv.transform.position);
            var passage=ForestPassage.Nearby(inv);
            if(passage!=null && (chest==null || Vector2.Distance(inv.transform.position,passage.transform.position)<Vector2.Distance(inv.transform.position,chest.transform.position)))
            { MatchManager.Instance.RequestPassage(passage.RouteId); return; }
            var mimic=CyberExtraction.Enemies.Monster.NearestDisguised(inv.transform.position);
            if(mimic!=null && (chest==null || Vector2.Distance(inv.transform.position,mimic.transform.position)<Vector2.Distance(inv.transform.position,chest.transform.position)))
            { mimic.RequestOpenDisguise(); return; }
            if(chest==null) return;
            if(chest.Locked.Value) { Close(); chest.RequestOpen(); return; }
            if(chest.FixtureKind.Value==3) return;
            if(chest.OpenState.Value==2) { worldChest=chest; safeChest=false; open=true; }
            else { pendingChest=chest; sawOpening=false; chest.RequestOpen(); }
        }
        void Update()
        {
            if(ExpeditionMenu.IsOpen) return;
            if(GameOptionsMenu.IsOpen || CampTestPanel.IsOpen || GameOptionsMenu.ConsumedBack) return;
            if(LockpickWindow.IsOpen) return;
            {
                if(!TestLabPanel.IsOpen && GameInput.BagPressed && (!CampController.Active || !CampController.Instance.HasActivity)) ToggleBag();
                if(GameInput.BackPressed) Close();
                if(!TestLabPanel.IsOpen && !CampController.Active && !open && GameInput.InteractPressed) Interact();
            }
            if(pendingChest!=null)
            {
                if(pendingChest.OpenState.Value==1) sawOpening=true;
                if(pendingChest.OpenState.Value==2) { worldChest=pendingChest; safeChest=false; open=true; pendingChest=null; }
                else if(sawOpening && pendingChest.OpenState.Value==0) pendingChest=null;
            }
            var inv=Local();
            if(!CampController.Active && open && (inv==null || !inv.IsAlive)) Close();
            if(worldChest!=null && inv!=null && Vector2.Distance(inv.transform.position,worldChest.transform.position)>3) Close();
        }
        Rect SlotRect(int grid,int slot,bool two)
        {
            var bounds=Bounds; float panelWidth=bounds.width/(two ? 2 : 1);
            float size=Mathf.Min(panelWidth*.27f,bounds.height*.24f), x=bounds.x+(two ? grid : 0)*panelWidth+(panelWidth-size*3)/2;
            return new Rect(x+(slot%3)*size,bounds.y+60+(slot/3)*size,size-5,size-5);
        }
        void OnGUI()
        {
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
                    var local=Local(); var nearest=LootPickup.NearestChest(local.transform.position);
                    var mimic=CyberExtraction.Enemies.Monster.NearestDisguised(local.transform.position);
                    string action=nearest!=null ? (nearest.Locked.Value ? "Abrir fechadura" : nearest.OpenState.Value==2 ? "Saquear" : nearest.FixtureKind.Value==3 ? "Abrir porta" : nearest.FixtureKind.Value!=0 ? "Abrir armário" : "Abrir bau") : mimic!=null ? "Abrir bau" : null;
                    var passage=ForestPassage.Nearby(local);
                    if(passage!=null && (nearest==null || Vector2.Distance(local.transform.position,passage.transform.position)<Vector2.Distance(local.transform.position,nearest.transform.position))) action=passage.Route.Action;
                    if(action!=null && GUI.Button(HudLayout.Interaction,action+(HudLayout.Touch ? "" : " [E]"))) Interact();
                }
                GUI.matrix=old; return;
            }
            bool camp=CampController.Active, two=safeChest && camp;
            int firstGrid=worldChest!=null ? 1 : 0;
            var inv=Local(); if(inv!=null && !camp) inv.CopyGrid(packView); if(worldChest!=null) worldChest.CopyGrid(chestView);
            var pack=camp ? CampSession.Progress.Pack : packView;
            var chest=safeChest ? CampSession.Progress.Chest : chestView;
            Rect bounds=Bounds; GUI.Box(bounds,"");
            var label=new GUIStyle(GUI.skin.label) { fontSize=Mathf.Clamp((int)(UiLayout.Height/32),16,28),alignment=TextAnchor.MiddleCenter,wordWrap=true };
            var cellStyle=new GUIStyle(GUI.skin.box) { fontSize=label.fontSize,alignment=TextAnchor.MiddleCenter,wordWrap=true };
            GUI.Label(new Rect(bounds.x+10,bounds.y+8,bounds.width-100,40),two ? "MOCHILA                  BAU SEGURO" : worldChest!=null ? "SAQUE · toque em um item para pegar" : "MOCHILA - 9 SLOTS",label);
            if(GUI.Button(new Rect(bounds.xMax-90,bounds.y+8,75,35),"Fechar")) { Close(); GUI.matrix=old; return; }
            var evt=Event.current;
            var textStyle=new GUIStyle(cellStyle) { fontSize=Mathf.Clamp((int)(SlotRect(0,0,two).height*.12f),11,16) };
            for(int grid=firstGrid;grid<firstGrid+(two ? 2 : 1);grid++) for(int slot=0;slot<9;slot++)
            {
                ItemStack stack=(grid==0 ? pack : chest).Slots[slot]; Rect rect=SlotRect(grid,slot,two);
                GUI.Box(rect,stack.Empty ? "-" : "",cellStyle);
                if(!stack.Empty)
                {
                    float iconHeight=rect.height*.46f;
                    ItemIconGui.Draw(new Rect(rect.x+6,rect.y+4,rect.width-12,iconHeight),stack.Item);
                    string text=ItemCatalog.Name(stack.Item)+" x"+stack.Quantity+(ItemCatalog.Weapon(stack.Item) ? "\n"+stack.Data+"/"+WeaponCatalog.Get(stack.Item).Capacity : stack.Quality>0 ? "\nQualidade "+stack.Quality : "");
                    GUI.Label(new Rect(rect.x+3,rect.y+iconHeight+5,rect.width-6,rect.height-iconHeight-7),text,textStyle);
                }
                if(evt.type==EventType.MouseDown && evt.button==0 && rect.Contains(evt.mousePosition) && !stack.Empty)
                { dragSlot=slot; dragGrid=grid; dragItem=stack; evt.Use(); }
            }
            GUI.Label(new Rect(bounds.x+10,bounds.yMax-52,bounds.width-20,44),!string.IsNullOrEmpty(notice) ? notice : worldChest!=null ? "Toque para pegar. Arraste para reorganizar ou para fora para descartar." : "Arraste entre slots. Arraste para fora para jogar no chao. Pilhas: 20.",label);
            if(dragSlot>=0)
            {
                var dragRect=new Rect(evt.mousePosition.x+12,evt.mousePosition.y+12,90,65);
                GUI.Box(dragRect,"");
                ItemIconGui.Draw(new Rect(dragRect.x+4,dragRect.y+3,55,55),dragItem.Item);
                GUI.Label(new Rect(dragRect.x+58,dragRect.y+20,30,25),"x"+dragItem.Quantity);
                if(evt.type==EventType.MouseUp)
                {
                    bool placed=false;
                    for(int grid=firstGrid;grid<firstGrid+(two ? 2 : 1);grid++) for(int slot=0;slot<9;slot++)
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
                            if(CampSession.Act(drop)) CampController.Instance.SpawnDrop(drop.Id,dragItem);
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
            if(inv==null || source>=worldChest.Contents.Count) return;
            var item=worldChest.Contents[source];
            for(int slot=0;slot<inv.Slots.Count;slot++)
            {
                var target=inv.Slots[slot];
                if(target.Empty || (!ItemCatalog.Weapon(item.Item) && target.Same(item) && target.Quantity<ItemCatalog.Limit(item.Item)))
                { worldChest.RequestTransfer(source,slot,true); notice=null; return; }
            }
            notice="Mochila cheia. Abra a mochila para liberar um slot.";
        }
        void OnDestroy() { if(Instance==this) Instance=null; }
    }
}
