using UnityEngine;
using CyberExtraction.Camp;
using CyberExtraction.Items;

namespace CyberExtraction.UI
{
    public sealed partial class UguiInventoryPresenter
    {
        int selectionGrid,selectionSlot=-1;
        ItemStack selectionItem;
        bool discardConfirmation;
        UnityEngine.UI.ScrollRect compactDetailScroll;
        Transform detailParent;
        string compactDetailText;
        float compactDetailPosition=1;
        bool wasCompact;
        void SelectItem(UguiInventorySlot slot)
        {
            selectionGrid=slot.Grid; selectionSlot=slot.Slot;
            selectionItem=slot.Grid==2 ? window.Equipped(slot.Slot) : (slot.Grid==0 ? pack : other).Slots[slot.Slot];
            equipmentNotice=selectionItem.Empty ? null : Describe(selectionItem);
            discardConfirmation=false; dirty=true;
        }
        bool CurrentSelection()
        {
            if(selectionSlot<0 || selectionGrid<0 || selectionGrid>1 || selectionItem.Empty) return false;
            var grid=selectionGrid==0 ? pack : other;
            return selectionSlot<window.Capacity(selectionGrid) && grid.Slots[selectionSlot].Equals(selectionItem);
        }
        void InitializeDiscard()
        {
            view.UnequipButton.onClick.AddListener(()=>
            {
                if(!showCharacter || selectionGrid!=2 || selectionSlot<0 || !window.SlotActionsAllowed) return;
                window.CopyPresentation(pack,other);
                int empty=System.Array.FindIndex(pack.Slots,item=>item.Empty);
                if(empty<0) equipmentNotice="Mochila cheia: libere um espaço para desequipar.";
                else window.ExchangeEquipment(empty,selectionSlot);
                dirty=true;
            });
            view.DiscardButton.onClick.AddListener(()=>
            {
                if(!discardConfirmation) { AskDiscard(); return; }
                if(TradeReady && CurrentSelection() && !RecoveryRules.Bound(selectionItem)) window.Discard(selectionGrid,selectionSlot,selectionItem);
                discardConfirmation=false; dirty=true;
            });
            view.CancelDiscard.onClick.AddListener(()=> { discardConfirmation=false; dirty=true; });
        }
        void AskDiscard()
        {
            if(TradeReady && CurrentSelection() && !RecoveryRules.Bound(selectionItem)) { discardConfirmation=true; dirty=true; }
        }
        void GridLayout(int grid,Rect area,int rows,bool visible)
        {
            var slots=grid==0 ? view.BackpackSlots : view.StorageSlots;
            var data=grid==0 ? pack : other;
            var root=view.Grids[grid]; root.gameObject.SetActive(visible);
            if(!visible) return;
            int columns=grid==1 && window.WorldMode ? 3 : 4;
            float size=Mathf.Min(area.width/columns,area.height/rows);
            int occupiedRows=Mathf.CeilToInt((float)window.Capacity(grid)/columns);
            UguiGeometry.Place(root,new Rect(area.x+(area.width-size*columns)/2,area.y+(area.height-size*occupiedRows)/2,size*columns,size*occupiedRows));
            var group=root.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            group.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount; group.constraintCount=columns;
            group.cellSize=new Vector2(size-6,size-6); group.spacing=new Vector2(6,6);
            int capacity=window.Capacity(grid);
            for(int i=0;i<slots.Length;i++)
            {
                slots[i].gameObject.SetActive(i<capacity);
                if(i<capacity) slots[i].Present(data.Slots[i]);
            }
        }
        void LayoutInventory()
        {
            var bounds=InventoryWindow.Bounds;
            bool compact=UguiBaseHub.Compact;
            if(wasCompact!=compact) { wasCompact=compact; dirty=true; }
            if(bounds!=lastBounds) { lastBounds=bounds; dirty=true; }
            if(!dirty) return;
            if(compactDetailScroll!=null)
            {
                compactDetailPosition=compactDetailScroll.verticalNormalizedPosition;
                compactDetailScroll.gameObject.SetActive(compact);
                if(!compact || showCommerce)
                { view.Detail.transform.SetParent(detailParent,false); compactDetailScroll.gameObject.SetActive(false); }
            }
            foreach(var label in compactAttributeLabels) if(label!=null) label.gameObject.SetActive(false);
            if(expeditionCharacterSection!=null && (!compact || !showCharacter || UguiBaseHub.Active))
                expeditionCharacterSection.gameObject.SetActive(false);
            BaseMenuStyle.Scope(view.gameObject,UguiBaseHub.Active);
            bool world=window.WorldMode || !CampController.Active && player!=null;
            if(discardConfirmation && !CurrentSelection()) discardConfirmation=false;
            UguiGeometry.Place(view.Panel,bounds); UguiMenuStyle.Backdrop(view,bounds);
            view.Backdrop.gameObject.SetActive(!world && !UguiBaseHub.Active);
            view.Surface.FillOpacity=world ? compact ? .94f : .24f : 1;
            view.Buttons[0].gameObject.SetActive(!UguiBaseHub.Active);
            UguiGeometry.Place((RectTransform)view.Buttons[0].transform,new Rect(bounds.width-106,8,90,48));
            UguiMenuStyle.Button(view.Buttons[0]);
            view.CharacterToggle.gameObject.SetActive(!UguiBaseHub.Active && !window.WorldMode);
            view.CommerceToggle.gameObject.SetActive(false);
            UguiGeometry.Place((RectTransform)view.CharacterToggle.transform,new Rect(bounds.width-260,8,140,48));
            UguiMenuStyle.Button(view.CharacterToggle);
            view.CharacterToggle.GetComponentInChildren<TMPro.TextMeshProUGUI>().text=showCharacter ? "Mochila" : "Personagem";
            view.EquipmentHeading.gameObject.SetActive(false);
            foreach(var slot in view.EquipmentSlots) slot.gameObject.SetActive(showCharacter);
            view.CharacterHeading.gameObject.SetActive(showCharacter); view.CharacterBody.gameObject.SetActive(showCharacter);
            foreach(var b in view.AttributeButtons) b.gameObject.SetActive(showCharacter);
            view.RespecButton.gameObject.SetActive(showCharacter && CampController.Active);
            view.CommerceHeading.gameObject.SetActive(showCommerce);
            foreach(var b in view.ShopOffers) b.gameObject.SetActive(showCommerce);
            foreach(var b in new[] {view.CommerceMode,view.SaleSource,view.TradeConfirm,view.TradeLess,view.TradeMore,view.TradeAll}) b.gameObject.SetActive(showCommerce);
            view.TradeUnload.gameObject.SetActive(showCommerce);
            if(dirty) UguiTheme.Apply(view.gameObject);
            view.Heading.fontSize=view.Body.fontSize=23;
            view.Heading.alignment=view.Body.alignment=TMPro.TextAlignmentOptions.MidlineLeft;
            if(showCommerce) UpdateCommerce();
            else if(showCharacter) UpdateCharacterSheet();
            else
            {
                float half=bounds.width/2;
                view.Heading.gameObject.SetActive(true); view.Body.gameObject.SetActive(window.TwoGrids);
                view.Heading.text="MOCHILA · 12";
                view.Body.text=window.WorldMode ? "SAQUE" : "BAÚ SEGURO · 16";
                UguiGeometry.Place(view.Heading.rectTransform,new Rect(24,14,half-48,40));
                UguiGeometry.Place(view.Body.rectTransform,new Rect(half+24,14,half-48,40));
                float available=bounds.height-164;
                GridLayout(0,new Rect(24,66,window.TwoGrids ? half-48 : bounds.width-48,available),window.TwoGrids && !window.WorldMode ? 4 : 3,true);
                GridLayout(1,new Rect(half+24,66,half-48,available),window.WorldMode ? 3 : 4,window.TwoGrids);
            }
            if(UguiBaseHub.Active)
            {
                view.Heading.font=view.Body.font=UiFontAssets.Title;
                view.Heading.enableAutoSizing=view.Body.enableAutoSizing=true;
                view.Heading.fontSizeMin=view.Body.fontSizeMin=17;
                view.Heading.fontSizeMax=view.Body.fontSizeMax=23;
            }
            else { view.Heading.font=UiFontAssets.Bold; view.Body.font=UiFontAssets.Regular; }
            if(!showCommerce)
            {
                float reserved=CampController.Active || showCharacter ? 218 : 0;
                UguiGeometry.Place(view.Detail.rectTransform,new Rect(24,bounds.height-90,bounds.width-48-reserved,76));
                view.Detail.text=window.OrganizationPending || CampSession.Pending && CampController.Active ? "Aguardando confirmação do servidor…" : equipmentNotice ?? window.Notice ?? (showCharacter ? "Selecione um item da mochila para comparar e equipar." : "Selecione um item para ver os detalhes.");
            }
            bool characterIdle=showCharacter && (selectionSlot<0 || selectionItem.Empty);
            view.RespecButton.gameObject.SetActive(characterIdle && CampController.Active);
            view.DiscardButton.gameObject.SetActive(CampController.Active && !showCommerce && !characterIdle);
            bool unequip=showCharacter && selectionGrid==2 && selectionSlot>=0 && !selectionItem.Empty;
            view.UnequipButton.gameObject.SetActive(unequip);
            if(unequip)
            {
                view.DiscardButton.gameObject.SetActive(false);
                TradeButton(view.UnequipButton,new Rect(bounds.width-212,bounds.height-68,188,48),"Desequipar",!CampController.Active || TradeReady);
            }
            view.CancelDiscard.gameObject.SetActive(discardConfirmation && !showCommerce);
            if(CampController.Active && !showCommerce)
            {
                TradeButton(view.DiscardButton,new Rect(bounds.width-212,bounds.height-68,188,48),discardConfirmation ? "Confirmar descarte" : "Descartar",TradeReady && CurrentSelection() && !RecoveryRules.Bound(selectionItem));
                if(discardConfirmation)
                {
                    view.Detail.text="Descartar "+ItemCatalog.Name(selectionItem.Item)+" ×"+selectionItem.Quantity+"? O item será destruído.";
                    TradeButton(view.CancelDiscard,new Rect(bounds.width-212,bounds.height-124,188,48),"Cancelar",true);
                }
            }
            view.Detail.alignment=TMPro.TextAlignmentOptions.TopLeft;
            view.Detail.richText=true;
            view.Detail.fontSize=18; view.Detail.enableAutoSizing=true; view.Detail.fontSizeMin=14; view.Detail.fontSizeMax=18;
            view.Detail.overflowMode=TMPro.TextOverflowModes.Ellipsis;
            view.Detail.color=UguiBaseHub.Active ? BaseMenuStyle.Secondary : HudTheme.Muted;
            if(UguiBaseHub.Active) view.Detail.fontSizeMin=18;
            if(compact) FinishCompactInventory(bounds);
            LayoutOrganization(bounds);
            dirty=false;
        }
        void LayoutOrganization(Rect bounds)
        {
            bool visible=!showCommerce && !showCharacter;
            view.OrganizeBackpack.gameObject.SetActive(visible);
            view.OrganizeStorage.gameObject.SetActive(visible && CampController.Active && window.TwoGrids && !window.WorldMode);
            if(!visible) return;
            bool compact=UguiBaseHub.Compact,expeditionCompact=compact && !UguiBaseHub.Active;
            float margin=expeditionCompact ? 20 : 24,half=bounds.width/2;
            float width=window.TwoGrids ? half-margin*2 : bounds.width-margin*2;
            float top=expeditionCompact ? 88 : 62,height=compact ? 56 : 48,gridTop=top+height+8;
            float available=Mathf.Max(0,bounds.height-gridTop-(expeditionCompact ? 112 : 98));
            bool idle=source==null && controllerSource==null && !discardConfirmation;
            TradeButton(view.OrganizeBackpack,new Rect(margin,top,Mathf.Min(width,compact ? 180 : 144),height),"Organizar",idle && window.CanOrganize(0));
            if(view.OrganizeStorage.gameObject.activeSelf)
                TradeButton(view.OrganizeStorage,new Rect(half+margin,top,Mathf.Min(width,compact ? 180 : 144),height),"Organizar",idle && window.CanOrganize(1));
            foreach(var button in new[] {view.OrganizeBackpack,view.OrganizeStorage})
            {
                var caption=button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                if(compact) ReadableText(caption,26);
            }
            GridLayout(0,new Rect(margin,gridTop,width,available),window.TwoGrids && !window.WorldMode ? 4 : 3,true);
            GridLayout(1,new Rect(half+margin,gridTop,half-margin*2,available),window.WorldMode ? 3 : 4,window.TwoGrids);
        }
        void FinishCompactInventory(Rect bounds)
        {
            if(!showCharacter)
            {
                ReadableText(view.Heading,28); ReadableText(view.Body,28);
            }
            if(showCommerce) { LayoutCompactCommerce(bounds); return; }
            if(!UguiBaseHub.Active) LayoutCompactExpeditionControls(bounds);
            if(showCharacter)
            {
                ReadableText(view.Body,26);
                foreach(var button in view.AttributeButtons)
                {
                    var text=button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                    ReadableText(text,26); text.alignment=TMPro.TextAlignmentOptions.Center;
                }
                var toggle=view.CharacterToggle.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                ReadableText(toggle,26); toggle.alignment=TMPro.TextAlignmentOptions.Center;
            }
            bool hasAction=view.DiscardButton.gameObject.activeSelf || view.UnequipButton.gameObject.activeSelf || view.RespecButton.gameObject.activeSelf;
            float reserved=hasAction ? 288 : 0;
            if(discardConfirmation) reserved=500;
            var actionBounds=new Rect(bounds.width-284,bounds.height-76,264,64);
            foreach(var button in new[] {view.DiscardButton,view.UnequipButton,view.RespecButton})
            {
                if(!button.gameObject.activeSelf) continue;
                UguiGeometry.Place((RectTransform)button.transform,actionBounds);
                var text=button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                ReadableText(text,26); text.alignment=TMPro.TextAlignmentOptions.Center;
            }
            if(discardConfirmation)
            {
                UguiGeometry.Place((RectTransform)view.CancelDiscard.transform,new Rect(bounds.width-496,bounds.height-76,200,64));
                var text=view.CancelDiscard.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                ReadableText(text,26); text.alignment=TMPro.TextAlignmentOptions.Center;
            }
            if(showCharacter && compactAttributes)
            {
                // Attribute controls own this view; equipment actions only belong
                // to the equipment view, even if a former selection survives a snapshot.
                view.UnequipButton.gameObject.SetActive(false); view.DiscardButton.gameObject.SetActive(false);
                view.CancelDiscard.gameObject.SetActive(false);
            }
            var detailBounds=new Rect(20,bounds.height-92,bounds.width-40-reserved,80);
            ReadableText(view.Detail,24); view.Detail.alignment=TMPro.TextAlignmentOptions.TopLeft;
            view.Detail.color=BaseMenuStyle.Secondary;
            ShowCompactDetails(detailBounds);
        }
        void LayoutCompactExpeditionControls(Rect bounds)
        {
            TradeButton(view.Buttons[0],new Rect(bounds.width-120,8,100,64),"Fechar",true);
            var closeText=view.Buttons[0].GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            ReadableText(closeText,26); closeText.alignment=TMPro.TextAlignmentOptions.Center;
            if(showCharacter)
            {
                if(expeditionCharacterSection!=null)
                {
                    var section=expeditionCharacterSection.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                    ReadableText(section,26); section.alignment=TMPro.TextAlignmentOptions.Center;
                }
                return;
            }
            float half=bounds.width/2;
            UguiGeometry.Place(view.Heading.rectTransform,new Rect(20,12,window.TwoGrids ? half-40 : bounds.width-400,64));
            if(window.TwoGrids)
                UguiGeometry.Place(view.Body.rectTransform,new Rect(half+20,12,half-160,64));
            if(!window.WorldMode)
            {
                TradeButton(view.CharacterToggle,new Rect(bounds.width-360,8,228,64),"Personagem",true);
                var text=view.CharacterToggle.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                ReadableText(text,26); text.alignment=TMPro.TextAlignmentOptions.Center;
            }
            float available=bounds.height-200;
            GridLayout(0,new Rect(20,88,window.TwoGrids ? half-40 : bounds.width-40,available),3,true);
            GridLayout(1,new Rect(half+20,88,half-40,available),window.WorldMode ? 3 : 4,window.TwoGrids);
        }
        void ShowCompactDetails(Rect bounds)
        {
            if(compactDetailScroll==null)
            {
                detailParent=view.Detail.transform.parent;
                var root=new GameObject("CompactItemDetails",typeof(RectTransform),typeof(UnityEngine.UI.ScrollRect));
                root.transform.SetParent(detailParent,false);
                var viewport=new GameObject("Viewport",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.RectMask2D));
                viewport.transform.SetParent(root.transform,false);
                var image=viewport.GetComponent<UnityEngine.UI.Image>(); image.color=Color.clear; image.canvasRenderer.cullTransparentMesh=false;
                compactDetailScroll=root.GetComponent<UnityEngine.UI.ScrollRect>();
                compactDetailScroll.viewport=(RectTransform)viewport.transform;
                compactDetailScroll.content=view.Detail.rectTransform;
                compactDetailScroll.horizontal=false; compactDetailScroll.vertical=true;
                compactDetailScroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;
                compactDetailScroll.scrollSensitivity=28;
            }
            compactDetailScroll.gameObject.SetActive(true);
            view.Detail.transform.SetParent(compactDetailScroll.viewport,false);
            view.Detail.raycastTarget=false;
            UguiGeometry.Place((RectTransform)compactDetailScroll.transform,bounds);
            UguiGeometry.Place(compactDetailScroll.viewport,new Rect(0,0,bounds.width,bounds.height));
            view.Detail.overflowMode=TMPro.TextOverflowModes.Overflow;
            float height=Mathf.Max(bounds.height,view.Detail.GetPreferredValues(view.Detail.text,bounds.width,float.PositiveInfinity).y);
            UguiGeometry.Place(view.Detail.rectTransform,new Rect(0,0,bounds.width,height));
            // Preserve scroll position for unchanged content; a new selection starts at its name.
            compactDetailScroll.StopMovement();
            compactDetailScroll.verticalNormalizedPosition=compactDetailText==view.Detail.text ? Mathf.Clamp01(compactDetailPosition) : 1;
            compactDetailText=view.Detail.text;
        }
    }
}
