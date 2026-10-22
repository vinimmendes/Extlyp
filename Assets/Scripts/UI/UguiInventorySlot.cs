using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using CyberExtraction.Items;

namespace CyberExtraction.UI
{
    public sealed class UguiInventorySlot : MonoBehaviour,IPointerDownHandler,IPointerClickHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,ISubmitHandler,ICancelHandler
    {
        [SerializeField] UnityEngine.UI.Image icon;
        [SerializeField] TextMeshProUGUI caption;
        [SerializeField] TextMeshProUGUI quantity;
        public int Grid,Slot;
        UguiInventoryPresenter owner;
        bool suppressClick;
        UnityEngine.UI.Outline sourceOutline;
        UnityEngine.UI.ScrollRect activityScroll;
        public System.Action OnTap;
        public void Bind(UguiInventoryPresenter presenter,int grid,int slot) { owner=presenter; Grid=grid; Slot=slot; }
        public void Present(ItemStack stack,bool eligible=true)
        {
            icon.sprite=stack.Empty ? null : ItemIconLibrary.Get(stack.Item); icon.enabled=icon.sprite!=null; icon.color=eligible ? Color.white : new Color(1,1,1,.4f);
            icon.preserveAspect=true; icon.type=UnityEngine.UI.Image.Type.Simple;
            caption.text=stack.Empty ? "" : ItemCatalog.Name(stack.Item)+(ItemCatalog.Melee(stack.Item) ? "\nCorpo a corpo" : ItemCatalog.Weapon(stack.Item) ? "\n"+stack.Data+"/"+WeaponCatalog.Get(stack.Item).Capacity : stack.Quality>0 ? "\nQualidade "+stack.Quality : "");
            caption.fontSize=13; caption.font=UiFontAssets.Regular;
            caption.enableAutoSizing=true; caption.fontSizeMin=9; caption.fontSizeMax=13;
            bool equipment=!stack.Empty && EquipmentCatalog.IsDefined(stack.Item);
            Color textColor=equipment ? EquipmentCatalog.RarityColor(stack.Instance.Rarity) : HudTheme.Text;
            caption.color=eligible ? textColor : HudTheme.Muted;
            Layout(icon.rectTransform,new Vector2(.08f,.29f),new Vector2(.92f,.75f));
            Layout(caption.rectTransform,new Vector2(.04f,.015f),new Vector2(.96f,.27f));
            if(quantity!=null)
            {
                quantity.gameObject.SetActive(!stack.Empty);
                quantity.text=equipment ? (stack.Instance.Rarity==ItemRarity.Common ? "C" : stack.Instance.Rarity==ItemRarity.Uncommon ? "I" : "R") : stack.Quantity.ToString();
                quantity.font=UiFontAssets.Regular; quantity.fontSize=15;
                quantity.enableAutoSizing=true; quantity.fontSizeMin=9; quantity.fontSizeMax=15;
                quantity.textWrappingMode=TextWrappingModes.NoWrap;
                quantity.overflowMode=TextOverflowModes.Truncate;
                quantity.margin=Vector4.zero;
                quantity.color=eligible ? textColor : HudTheme.Muted;
                quantity.alignment=TextAlignmentOptions.MidlineRight;
                Layout(quantity.rectTransform,new Vector2(.08f,.76f),new Vector2(.94f,.97f));
            }
            var frame=GetComponent<HudFrameGraphic>();
            if(frame!=null) { frame.ItemRarityRim=equipment; frame.ItemRimColor=eligible ? textColor : HudTheme.Muted; frame.ApplyTheme(); }
            if(sourceOutline!=null) sourceOutline.effectColor=HudTheme.Accent;
            bool compact=UguiBaseHub.Compact;
            caption.gameObject.SetActive(!compact);
            if(compact)
            {
                // Names and comparisons belong in the readable selection panel,
                // rather than shrinking into a few pixels below every icon.
                Layout(icon.rectTransform,new Vector2(.10f,.12f),new Vector2(.90f,.85f));
                if(quantity!=null)
                {
                    quantity.enableAutoSizing=false; quantity.fontSize=24;
                    quantity.alignment=TextAlignmentOptions.TopRight;
                    Layout(quantity.rectTransform,new Vector2(.06f,.50f),new Vector2(.94f,.98f));
                }
            }
        }
        public void PresentEquipment(ItemStack stack,int slot)
        {
            Present(stack);
            caption.text=EquipmentLoadout.Name(slot);
            caption.fontSizeMax=15;
            if(stack.Empty)
            {
                icon.sprite=EmptyEquipmentSlotArt.Get(slot);
                icon.enabled=icon.sprite!=null;
                icon.color=new Color(.72f,.72f,.72f,UguiBaseHub.Compact ? .45f : .18f);
            }
        }
        static void Layout(RectTransform rect,Vector2 min,Vector2 max)
        { rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=rect.offsetMax=Vector2.zero; }
        public void OnPointerDown(PointerEventData data)=>suppressClick=false;
        public void OnPointerClick(PointerEventData data)
        {
            if(suppressClick || data.button!=PointerEventData.InputButton.Left) return;
            if(OnTap!=null) OnTap(); else owner?.PointerTap(this,data);
        }
        public void OnSubmit(BaseEventData data) { if(OnTap!=null) OnTap(); else owner?.Submit(this); }
        public void OnCancel(BaseEventData data) { if(owner!=null && owner.TryCancelSelection()) data.Use(); }
        public void MarkSource(bool selected)
        {
            if(selected && sourceOutline==null)
            {
                sourceOutline=gameObject.AddComponent<UnityEngine.UI.Outline>();
                sourceOutline.effectDistance=new Vector2(2,-2); sourceOutline.useGraphicAlpha=false;
            }
            if(sourceOutline!=null) { sourceOutline.effectColor=HudTheme.Accent; sourceOutline.enabled=selected; }
        }
        public void OnBeginDrag(PointerEventData data)
        {
            suppressClick=true;
            if(owner!=null) owner.Begin(this,data);
            else { activityScroll=GetComponentInParent<UnityEngine.UI.ScrollRect>(); activityScroll?.OnBeginDrag(data); }
        }
        public void OnDrag(PointerEventData data) { if(owner!=null) owner.Drag(data); else activityScroll?.OnDrag(data); }
        public void OnEndDrag(PointerEventData data)
        { if(owner!=null) owner.Drop(data); else activityScroll?.OnEndDrag(data); activityScroll=null; }
        void OnDisable() { owner?.Cancel(); }
    }
}
