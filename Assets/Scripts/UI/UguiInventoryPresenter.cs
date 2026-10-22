using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;
using CyberExtraction.Items;
using CyberExtraction.Players;
using CyberExtraction.Combat;
using CyberExtraction.Camp;
using CyberExtraction.Match;
using CyberExtraction.Core;

namespace CyberExtraction.UI
{
    public sealed partial class UguiInventoryPresenter : MonoBehaviour
    {
        UguiPrefabView view;
        InventoryWindow window;
        PlayerInventory player;
        LootPickup recipient;
        readonly InventoryGrid pack=new InventoryGrid(ItemCatalog.BackpackSlots),other=new InventoryGrid(ItemCatalog.SafeChestSlots);
        UguiInventorySlot source;
        ItemStack captured;
        int pointerId;
        bool dirty=true;
        Rect lastBounds;
        bool lastTwo;
        bool lastWorld;
        HudAppearance theme=(HudAppearance)(-1);
        bool wasPending;
        bool wasOrganizationPending,wasOrganizationReady;
        int selectedBag=-1;
        ItemStack selectedItem;
        string equipmentNotice;
        public void Initialize(UguiPrefabView screen)
        {
            view=screen; window=InventoryWindow.Instance; window.Changed+=Changed; CampSession.Changed+=Changed;
            window.CancelSelection=TryCancelSelection;
            view.Buttons[0].onClick.AddListener(()=>window.Close());
            for(int i=0;i<view.BackpackSlots.Length;i++) view.BackpackSlots[i].Bind(this,0,i);
            for(int i=0;i<view.StorageSlots.Length;i++) view.StorageSlots[i].Bind(this,1,i);
            for(int i=0;i<view.EquipmentSlots.Length;i++) view.EquipmentSlots[i].Bind(this,2,i);
            InitializeProgression();
            InitializeCommerce();
            InitializeDiscard();
            view.OrganizeBackpack.onClick.AddListener(()=>OrganizeGrid(0));
            view.OrganizeStorage.onClick.AddListener(()=>OrganizeGrid(1));
        }
        public void ShowBasePage(BasePage page)
        {
            selectionSlot=-1; discardConfirmation=false;
            SetCommerce(page==BasePage.Commerce);
            showCharacter=page==BasePage.Character; characterWasShown=!showCharacter;
            dirty=true; lastBounds=default;
        }
        void Changed()
        {
            dirty=true; discardConfirmation=false; selectionSlot=-1;
            Cancel(); selectedBag=-1; selectedItem=default; equipmentNotice=null;
            if(!InventoryWindow.IsOpen || window!=null && !window.SlotActionsAllowed)
            { System.Array.Clear(pack.Slots,0,pack.Slots.Length); System.Array.Clear(other.Slots,0,other.Slots.Length); }
            if(!InventoryWindow.IsOpen) { Cancel(); selectedBag=-1; equipmentNotice=null; showCommerce=showCharacter=false; characterWasShown=true; lastBounds=default; ResetTrade(); respecConfirmation=false; }
        }
        void EquipmentTimeChanged(double before,double after)=>Changed();
        void AttributesChanged(AttributeValues before,AttributeValues after)=>Changed();
        void Items(NetworkListEvent<ItemStack> change)=>Changed();
        void Update()
        {
            if(view==null) return;
            var next=InventoryWindow.Local(); var chest=window.Recipient;
            if(next!=player || chest!=recipient)
            {
                if(player!=null) { player.Slots.OnListChanged-=Items; player.Equipment.OnListChanged-=Items; player.EquipmentChangeEnds.OnValueChanged-=EquipmentTimeChanged; player.AttributeInvestment.OnValueChanged-=AttributesChanged; }
                if(recipient!=null) recipient.Contents.OnListChanged-=Items;
                BindProgression(player,false); player=next; recipient=chest; BindProgression(player,true); Cancel(); dirty=true;
                if(player!=null) { player.Slots.OnListChanged+=Items; player.Equipment.OnListChanged+=Items; player.EquipmentChangeEnds.OnValueChanged+=EquipmentTimeChanged; player.AttributeInvestment.OnValueChanged+=AttributesChanged; }
                if(recipient!=null) recipient.Contents.OnListChanged+=Items;
            }
            bool visible=UiScreenCoordinator.Top==PlayerScreen.Inventory;
            if(view.gameObject.activeSelf!=(visible))
            {
                view.gameObject.SetActive(visible); dirty=true; Cancel();
                if(visible && EventSystem.current!=null)
                {
                    if(!UguiBaseHub.Active) EventSystem.current.SetSelectedGameObject(view.Buttons[0].gameObject);
                    else if(EventSystem.current.currentSelectedGameObject==null || !EventSystem.current.currentSelectedGameObject.activeInHierarchy)
                        EventSystem.current.SetSelectedGameObject((showCommerce ? view.ShopOffers[0].gameObject : view.BackpackSlots[0].gameObject));
                }
            }
            if(!visible) return;
            if(GameInput.UiDiscardPressed) DiscardControllerSelection();
            if(wasPending!=CampSession.Pending) { wasPending=CampSession.Pending; dirty=true; }
            bool organizing=window.OrganizationPending,organizeReady=window.CanOrganize(0);
            if(wasOrganizationPending!=organizing || wasOrganizationReady!=organizeReady)
            { wasOrganizationPending=organizing; wasOrganizationReady=organizeReady; dirty=true; }
            if(theme!=HudTheme.Appearance) { theme=HudTheme.Appearance; dirty=true; }
            bool liveWorld=window.WorldMode || !CampController.Active && player!=null && MatchManager.Instance!=null;
            if(lastWorld!=liveWorld) { lastWorld=liveWorld; dirty=true; }
            window.CopyPresentation(pack,other);
            LayoutInventory();
        }
        public void Tap(UguiInventorySlot slot)
        {
            ClearControllerSelection();
            if(!window.SlotActionsAllowed || source!=null || UiScreenCoordinator.Top!=PlayerScreen.Inventory) return;
            if(showCommerce) { SelectSale(slot); return; }
            window.CopyPresentation(pack,other);
            SelectItem(slot);
            if(slot.Grid==1 && window.WorldMode) { window.Transfer(1,slot.Slot,1,slot.Slot,true); return; }
            if(slot.Grid==0)
            {
                selectedBag=pack.Slots[slot.Slot].Empty ? -1 : slot.Slot;
                selectedItem=selectedBag>=0 ? pack.Slots[selectedBag] : default;
                equipmentNotice=selectedBag<0 ? null : Describe(pack.Slots[selectedBag])+(showCharacter ? " · Selecione o slot para equipar." : "");
                if(selectedBag>=0)
                {
                    var candidate=pack.Slots[selectedBag];
                    if(ItemCatalog.Weapon(candidate.Item) || ItemCatalog.Equipment(candidate.Item)) equipmentNotice+=" · Duplo toque/clique para equipar.";
                    int comparisonSlot=ComparisonSlot(candidate);
                    if(showCharacter && comparisonSlot>=0) equipmentNotice+="\nEquipado: "+Describe(window.Equipped(comparisonSlot),false);
                }
            }
            if(slot.Grid==2)
            {
                int bag=selectedBag;
                if(bag<0 && window.Equipped(slot.Slot).Empty)
                { equipmentNotice=EquipmentLoadout.Name(slot.Slot)+" vazio. Selecione um item compatível na mochila."; dirty=true; return; }
                if(bag>=0 && !pack.Slots[bag].Equals(selectedItem))
                { selectedBag=-1; equipmentNotice="O item mudou; selecione novamente."; dirty=true; return; }
                if(bag<0) { equipmentNotice=Describe(window.Equipped(slot.Slot))+" · Duplo toque/clique para desequipar."; dirty=true; return; }
                if(bag<0) equipmentNotice="Mochila cheia: libere um espaço para guardar a peça.";
                else if(!EquipmentLoadout.Accepts(slot.Slot,pack.Slots[bag])) equipmentNotice="Este item não é compatível com "+EquipmentLoadout.Name(slot.Slot)+".";
                else { equipmentNotice=Describe(window.Equipped(slot.Slot))+" → "+Describe(pack.Slots[bag]); window.ExchangeEquipment(bag,slot.Slot); selectedBag=-1; }
            }
            dirty=true;
        }
        string AttributeSummary(CyberExtraction.Core.CharacterProgress character)
        {
            string text="";
            string[] names={"FOR","DES","VIG","PER","VIT","INT"};
            for(int i=0;i<6;i++)
            {
                var id=(AttributeId)i;
                long total=PresentedTotal(id);
                int basis=PresentedBase(id);
                long bonus=total-basis;
                text+=(i>0 && i%2==0 ? "\n" : i>0 ? "  " : "")+names[i]+" "+
                    (id==AttributeId.Intelligence ? "1 (futuro)" : basis+(bonus!=0 ? (bonus>0 ? "+" : "")+bonus+"="+CharacterAttributeRules.Effective(total) : "")+
                    (total>20 ? " ("+(total-20)+" exc.)" : ""));
            }
            return text;
        }
        int PresentedBase(AttributeId id) => player!=null && !CampController.Active ? player.BaseAttribute(id) :
            CampSession.Progress.Character!=null ? CampSession.Progress.Character.BaseAttribute(id) : 1;
        long PresentedTotal(AttributeId id)
        {
            if(id==AttributeId.Intelligence) return 1;
            long total=PresentedBase(id);
            for(int i=0;i<EquipmentLoadout.WeaponA;i++)
            { var stack=window.Equipped(i); if(!stack.Empty) total+=stack.Instance.Bonuses.Get(id); }
            return total;
        }
        float PresentedPhysicalProtection() => Mathf.Clamp(EquipmentCatalog.PhysicalProtection(window.Equipped(0))+
            EquipmentCatalog.PhysicalProtection(window.Equipped(1)),0,EquipmentCatalog.MaximumPhysicalProtection);
        int ComparisonSlot(ItemStack item)
        {
            if(item.Empty) return -1;
            for(int i=0;i<EquipmentLoadout.Count;i++)
            {
                var equipped=window.Equipped(i);
                if(!equipped.Empty && item.Instance.HasIdentity && equipped.Instance.SameIdentity(item.Instance)) return i;
            }
            if(!ItemCatalog.Weapon(item.Item)) return ItemCatalog.Equipment(item.Item) ? (int)ItemCatalog.EquipmentSlotFor(item.Item)-1 : -1;
            if(window.Equipped(EquipmentLoadout.WeaponA).Empty) return EquipmentLoadout.WeaponA;
            if(window.Equipped(EquipmentLoadout.WeaponB).Empty) return EquipmentLoadout.WeaponB;
            int active=CampController.Active ? CampSession.Progress.Equipment.ActiveWeaponSlot : player!=null ? player.ActiveWeaponSlot : EquipmentLoadout.WeaponA;
            return EquipmentLoadout.AcceptsWeapon(active) ? active : EquipmentLoadout.WeaponA;
        }
        static string ComparisonBadge(float difference,bool percentage=false)
        {
            if(Mathf.Abs(difference)<.0001f) difference=0;
            string color=difference>0 ? "#72D68B" : difference<0 ? "#F07878" : "#FFFFFF";
            string value=(difference>0 ? "+" : "")+difference.ToString("0.##")+(percentage ? " p.p." : "");
            return " <color="+color+">("+value+")</color>";
        }
        string Describe(ItemStack item,bool compare=true)
        {
            if(item.Empty) return "Vazio";
            if(TreasureCatalog.IsDefined(item.Item)) return ItemCatalog.Name(item.Item)+" · Tesouro · Valor de venda: "+TreasureCatalog.Get(item.Item).SellPrice+" moedas/un. · Pilha máxima: 5";
            string text=ItemCatalog.Name(item.Item)+" ["+EquipmentCatalog.RarityName(item.Instance.Rarity)+"]";
            if(RecoveryRules.Bound(item)) text+=" · Recuperação: vinculado até extrair";
            int sale=CampTrade.SellPrice(item);
            if(sale>=0) text+=" · Venda: "+sale+" moedas/un.";
            string[] names={"FOR","DES","VIG","PER","VIT","INT"};
            var model=EquipmentCatalog.Get(item.Item);
            int slot=compare ? ComparisonSlot(item) : -1;
            var outgoing=slot>=0 ? window.Equipped(slot) : default;
            if(slot>=0) text+="\nComparado com: "+(outgoing.Empty ? EquipmentLoadout.Name(slot)+" vazio" : ItemCatalog.Name(outgoing.Item));
            for(int i=0;i<names.Length;i++)
            {
                var id=(AttributeId)i; int bonus=item.Instance.Bonuses.Get(id);
                int previous=outgoing.Empty ? 0 : outgoing.Instance.Bonuses.Get(id);
                if(bonus==0 && previous==0 && (model==null || model.PrimaryAttribute!=id)) continue;
                text+="\n"+(model!=null && model.PrimaryAttribute==id ? "Principal: " : bonus!=0 && model!=null ? "Secundário: " : "")+names[i]+" "+(bonus>=0 ? "+" : "")+bonus;
                if(slot>=0)
                {
                    long before=PresentedTotal(id);
                    long after=slot<EquipmentLoadout.WeaponA && id!=AttributeId.Intelligence ? before-previous+bonus : before;
                    text+=ComparisonBadge(CharacterAttributeRules.Effective(after)-CharacterAttributeRules.Effective(before));
                    if(after>CharacterAttributeRules.MaxEffective && previous!=bonus) text+=" · limite efetivo "+CharacterAttributeRules.MaxEffective;
                }
            }
            float protection=EquipmentCatalog.PhysicalProtection(item);
            if(protection>0 || EquipmentCatalog.PhysicalProtection(outgoing)>0)
            {
                text+="\nProteção física "+(protection*100).ToString("0")+"%";
                if(slot>=0)
                {
                    float before=PresentedPhysicalProtection();
                    float after=Mathf.Clamp(before-EquipmentCatalog.PhysicalProtection(outgoing)+protection,0,EquipmentCatalog.MaximumPhysicalProtection);
                    text+=ComparisonBadge((after-before)*100,true);
                }
            }
            if(ItemCatalog.Melee(item.Item)) text+=" · "+WeaponCatalog.Get(item.Item).Damage+" dano · "+WeaponCatalog.Get(item.Item).Range.ToString("0.0")+" u · "+WeaponCatalog.Get(item.Item).StaminaCost+" stamina";
            return text;
        }
        public void Begin(UguiInventorySlot slot,PointerEventData data)
        {
            lastPointerSlot=null;
            if(!window.SlotActionsAllowed || showCommerce || source!=null || UiScreenCoordinator.Top!=PlayerScreen.Inventory || CampController.Active && (!CampSession.Synchronized || CampSession.Pending)) return;
            ClearControllerSelection(); dirty=true;
            window.CopyPresentation(pack,other); var item=slot.Grid==2 ? window.Equipped(slot.Slot) : (slot.Grid==0 ? pack : other).Slots[slot.Slot];
            if(item.Empty) return; source=slot; captured=item; pointerId=data.pointerId;
            if(view.DragVisual!=null)
            { view.DragVisual.gameObject.SetActive(true); view.DragIcon.sprite=ItemIconLibrary.Get(item.Item); view.DragIcon.preserveAspect=true; view.DragCaption.text="×"+item.Quantity; Drag(data); }
        }
        public void Drag(PointerEventData data) { if(source==null || pointerId!=data.pointerId || view.DragVisual==null) return; UguiGeometry.Place(view.DragVisual,new Rect(UiLayout.Point(data.position)-lastBounds.position+new Vector2(12,12),new Vector2(80,60))); }
        public void Drop(PointerEventData data)
        {
            if(source==null || pointerId!=data.pointerId) return;
            if(!window.SlotActionsAllowed || UiScreenCoordinator.Top!=PlayerScreen.Inventory) { Cancel(); return; }
            window.CopyPresentation(pack,other); var current=source.Grid==2 ? window.Equipped(source.Slot) : (source.Grid==0 ? pack : other).Slots[source.Slot];
            if(!current.Equals(captured)) { Cancel(); return; }
            var target=data.pointerCurrentRaycast.gameObject!=null ? data.pointerCurrentRaycast.gameObject.GetComponentInParent<UguiInventorySlot>() : null;
            if(target!=null && source.Grid==0 && target.Grid==2) window.ExchangeEquipment(source.Slot,target.Slot);
            else if(target!=null && source.Grid==2 && target.Grid==0) window.ExchangeEquipment(target.Slot,source.Slot);
            else if(target!=null && source.Grid!=2 && target.Grid!=2) window.Transfer(source.Grid,source.Slot,target.Grid,target.Slot);
            else if(!CampController.Active && source.Grid!=2 && !RectTransformUtility.RectangleContainsScreenPoint(view.Panel,data.position,null)) window.Discard(source.Grid,source.Slot,captured);
            Cancel();
        }
        public void Cancel() { dirty=true; lastPointerSlot=null; source=null; captured=default; ClearControllerSelection(); if(view!=null && view.DragVisual!=null) view.DragVisual.gameObject.SetActive(false); }
        void OrganizeGrid(int grid)
        {
            if(showCommerce || showCharacter || source!=null || controllerSource!=null || discardConfirmation || UiScreenCoordinator.Top!=PlayerScreen.Inventory || !window.CanOrganize(grid)) return;
            window.Organize(grid);
        }
        void OnApplicationFocus(bool focus) { if(!focus) Cancel(); }
        void OnDestroy()
        {
            BindProgression(player,false);
            if(window!=null) { window.Changed-=Changed; window.CancelSelection=null; } CampSession.Changed-=Changed;
            if(player!=null) { player.Slots.OnListChanged-=Items; player.Equipment.OnListChanged-=Items; player.EquipmentChangeEnds.OnValueChanged-=EquipmentTimeChanged; player.AttributeInvestment.OnValueChanged-=AttributesChanged; } if(recipient!=null) recipient.Contents.OnListChanged-=Items;
        }
    }
}
