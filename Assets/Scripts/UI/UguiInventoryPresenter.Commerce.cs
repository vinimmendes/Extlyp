using UnityEngine;
using CyberExtraction.Camp;
using CyberExtraction.Items;
namespace CyberExtraction.UI
{
    public sealed partial class UguiInventoryPresenter
    {
        bool showCommerce,buyMode=true,respecConfirmation;
        int tradeGrid,tradeSlot=-1,tradeQuantity=1,shopOffer=-1;
        long quoteRevision=-1,respecRevision=-1;
        ItemStack tradeItem;
        string quoteCycle;
        bool TradeReady => CampController.Active && CampSession.Synchronized && !CampSession.Pending && CampSession.Progress.ExpeditionState==0;
        MerchantOffer SelectedOffer => shopOffer>=0 && CampSession.Progress.Merchant!=null && shopOffer<CampSession.Progress.Merchant.Offers.Length ? CampSession.Progress.Merchant.Offers[shopOffer] : null;
        int MaximumTradeQuantity => buyMode ? SelectedOffer!=null ? SelectedOffer.Remaining : 0 : tradeItem.Quantity;
        void InitializeCommerce()
        {
            view.CommerceToggle.onClick.AddListener(()=>SetCommerce(!showCommerce));
            view.CommerceMode.onClick.AddListener(()=> { buyMode=!buyMode; ResetTrade(); dirty=true; });
            view.SaleSource.onClick.AddListener(()=> { tradeGrid=1-tradeGrid; ResetTrade(); dirty=true; });
            view.TradeLess.onClick.AddListener(()=> { tradeQuantity=Mathf.Max(1,tradeQuantity-1); dirty=true; });
            view.TradeMore.onClick.AddListener(()=> { tradeQuantity=Mathf.Min(MaximumTradeQuantity,tradeQuantity+1); dirty=true; });
            view.TradeAll.onClick.AddListener(()=> { tradeQuantity=MaximumTradeQuantity; dirty=true; });
            view.TradeConfirm.onClick.AddListener(ConfirmTrade);
            view.TradeUnload.onClick.AddListener(()=> { if(TradeReady && !buyMode && CurrentQuote()) CampSession.Act(new CampAction {Kind=CampActionKind.Unload,SourceGrid=tradeGrid,Source=tradeSlot,ExpectedItem=tradeItem}); });
            for(int i=0;i<view.ShopOffers.Length;i++)
            {
                int index=i;
                view.ShopOffers[i].onClick.AddListener(()=>
                {
                    if(!TradeReady) return;
                    buyMode=true; shopOffer=index; tradeSlot=-1; tradeQuantity=1;
                    quoteCycle=CampSession.Progress.Merchant.Cycle; quoteRevision=CampSession.Progress.Revision; dirty=true;
                });
            }
            view.RespecButton.onClick.AddListener(()=>
            {
                if(!TradeReady) return;
                if(respecConfirmation && respecRevision==CampSession.Progress.Revision) { CampSession.Act(new CampAction {Kind=CampActionKind.Respec}); respecConfirmation=false; }
                else { respecConfirmation=true; respecRevision=CampSession.Progress.Revision; dirty=true; }
            });
        }
        void ResetTrade() { tradeSlot=shopOffer=-1; quoteRevision=-1; quoteCycle=null; tradeItem=default; tradeQuantity=1; }
        void SetCommerce(bool open)
        {
            showCommerce=open && CampController.Active; showCharacter=false; characterWasShown=true; buyMode=true;
            ResetTrade(); respecConfirmation=discardConfirmation=false; Cancel(); lastBounds=default; dirty=true;
        }
        bool CurrentQuote()
        {
            if(quoteRevision!=CampSession.Progress.Revision) return false;
            if(buyMode) return SelectedOffer!=null && quoteCycle==CampSession.Progress.Merchant.Cycle;
            var grid=tradeGrid==0 ? CampSession.Progress.Pack : CampSession.Progress.Chest;
            return tradeSlot>=0 && tradeSlot<grid.Slots.Length && !tradeItem.Empty && grid.Slots[tradeSlot].Equals(tradeItem);
        }
        void ConfirmTrade()
        {
            if(!TradeReady || !CurrentQuote() || tradeQuantity<1) return;
            if(buyMode) CampSession.Act(new CampAction {Kind=CampActionKind.Buy,Result=shopOffer,Reference=quoteCycle,Quantity=tradeQuantity,ExpectedItem=SelectedOffer.Item});
            else CampSession.Act(new CampAction {Kind=CampActionKind.Sell,SourceGrid=tradeGrid,Source=tradeSlot,Quantity=tradeQuantity,ExpectedItem=tradeItem});
        }
        void SelectSale(UguiInventorySlot slot)
        {
            if(!TradeReady || slot.Grid<0 || slot.Grid>1) return;
            buyMode=false; shopOffer=-1; tradeGrid=slot.Grid; tradeSlot=slot.Slot;
            tradeItem=(tradeGrid==0 ? CampSession.Progress.Pack : CampSession.Progress.Chest).Slots[tradeSlot];
            tradeQuantity=1; quoteRevision=CampSession.Progress.Revision; dirty=true;
        }
        static void TradeButton(UnityEngine.UI.Button button,Rect rect,string label,bool enabled,bool primary=false)
        {
            UguiGeometry.Place((RectTransform)button.transform,rect); UguiMenuStyle.Button(button,primary); button.interactable=enabled;
            var text=button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            text.text=label; text.enableAutoSizing=true; text.fontSizeMin=14; text.fontSizeMax=18;
        }
        bool UpdateCommerce()
        {
            var bounds=InventoryWindow.Bounds; float half=bounds.width/2;
            if(!CurrentQuote()) ResetTrade();
            view.CharacterHeading.gameObject.SetActive(false); view.CharacterBody.gameObject.SetActive(false);
            view.Heading.gameObject.SetActive(true); view.Body.gameObject.SetActive(false);
            view.Heading.text=tradeGrid==0 ? "MOCHILA · 12" : "BAÚ SEGURO · 16";
            UguiGeometry.Place(view.Heading.rectTransform,new Rect(18,12,half-170,40));
            TradeButton(view.SaleSource,new Rect(half-156,8,132,48),tradeGrid==0 ? "Ver baú" : "Ver mochila",TradeReady);
            TradeButton(view.CommerceMode,new Rect(half+18,8,132,48),buyMode ? "Comprar" : "Vender",TradeReady,buyMode);
            UguiGeometry.Place(view.CommerceHeading.rectTransform,new Rect(half+164,12,half-182,40));
            view.CommerceHeading.text="COMERCIANTE · "+CampSession.Progress.Character.Coins+" moedas"; view.CommerceHeading.fontSize=19;
            GridLayout(0,new Rect(18,66,half-42,bounds.height-180),tradeGrid==1 ? 4 : 3,tradeGrid==0);
            GridLayout(1,new Rect(18,66,half-42,bounds.height-180),4,tradeGrid==1);
            float cell=Mathf.Min((half-36)/4,(bounds.height-180)/3),x=half+(half-cell*4)/2;
            var merchant=CampSession.Progress.Merchant;
            for(int i=0;i<view.ShopOffers.Length;i++)
            {
                var offer=merchant!=null && merchant.Offers!=null && i<merchant.Offers.Length ? merchant.Offers[i] : null;
                var button=view.ShopOffers[i]; button.gameObject.SetActive(offer!=null); if(offer==null) continue;
                TradeButton(button,new Rect(x+i%4*cell,66+i/4*cell,cell-6,cell-6),ItemCatalog.Name(offer.Item.Item),TradeReady && offer.Remaining>0,i==shopOffer);
                var frame=button.GetComponent<HudFrameGraphic>();
                frame.BaseStyle=false; frame.SlotStyle=true;
                frame.ItemRarityRim=i==shopOffer; frame.ItemRimColor=HudTheme.Accent; frame.ApplyTheme();
                var caption=button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                caption.gameObject.SetActive(!(UguiBaseHub.Active && UguiBaseHub.Compact));
                caption.rectTransform.anchorMin=new Vector2(.06f,.045f); caption.rectTransform.anchorMax=new Vector2(.94f,.33f); caption.rectTransform.offsetMin=caption.rectTransform.offsetMax=Vector2.zero;
                caption.fontSizeMin=10; caption.fontSizeMax=14; caption.overflowMode=TMPro.TextOverflowModes.Truncate;
                caption.textWrappingMode=TMPro.TextWrappingModes.Normal; caption.alignment=TMPro.TextAlignmentOptions.Center;
                view.ShopIcons[i].sprite=ItemIconLibrary.Get(offer.Item.Item); view.ShopIcons[i].color=offer.Remaining>0 ? Color.white : new Color(1,1,1,.25f);
                view.ShopIcons[i].preserveAspect=true;
                view.ShopIcons[i].rectTransform.anchorMin=new Vector2(.12f,.34f); view.ShopIcons[i].rectTransform.anchorMax=new Vector2(.88f,.78f);
                view.ShopIcons[i].rectTransform.offsetMin=view.ShopIcons[i].rectTransform.offsetMax=Vector2.zero;
                var stock=view.ShopStock[i]; stock.gameObject.SetActive(true); stock.text=offer.Remaining==0 ? "ESGOTADO" : "×"+offer.Remaining+" · "+offer.Price;
                stock.rectTransform.anchorMin=new Vector2(.08f,.81f); stock.rectTransform.anchorMax=new Vector2(.92f,.97f);
                stock.rectTransform.offsetMin=stock.rectTransform.offsetMax=Vector2.zero;
                stock.font=UiFontAssets.Regular; stock.fontSize=15; stock.enableAutoSizing=true; stock.fontSizeMin=10; stock.fontSizeMax=15;
                stock.alignment=TMPro.TextAlignmentOptions.MidlineRight; stock.margin=Vector4.zero;
                stock.textWrappingMode=TMPro.TextWrappingModes.NoWrap; stock.overflowMode=TMPro.TextOverflowModes.Truncate;
                stock.color=offer.Remaining>0 ? HudTheme.Text : HudTheme.Muted;
            }
            bool selected=CurrentQuote(); var current=buyMode ? SelectedOffer?.Item ?? default : tradeItem;
            int price=buyMode ? SelectedOffer?.Price ?? -1 : CampTrade.SellPrice(tradeItem); long total=price<0 ? -1 : (long)price*tradeQuantity;
            float y=bounds.height-106;
            TradeButton(view.TradeLess,new Rect(18,y,48,48),"−",TradeReady && selected && tradeQuantity>1);
            TradeButton(view.TradeMore,new Rect(74,y,48,48),"+",TradeReady && selected && tradeQuantity<MaximumTradeQuantity);
            TradeButton(view.TradeAll,new Rect(130,y,80,48),"Tudo",TradeReady && selected && MaximumTradeQuantity>0);
            var weapon=selected ? WeaponCatalog.Get(current.Item) : null; view.TradeUnload.gameObject.SetActive(!buyMode);
            TradeButton(view.TradeUnload,new Rect(218,y,146,48),"Descarregar",TradeReady && selected && !buyMode && weapon!=null && !weapon.IsMelee && current.Data>0);
            TradeButton(view.TradeConfirm,new Rect(bounds.width-246,y,228,48),(buyMode ? "Comprar" : "Vender")+" ×"+tradeQuantity+" · "+(total<0 ? 0 : total),TradeReady && selected && price>=0 && tradeQuantity>0 && tradeQuantity<=MaximumTradeQuantity && (!buyMode || CampSession.Progress.Character.Coins>=total),true);
            UguiGeometry.Place(view.Detail.rectTransform,new Rect(18,bounds.height-50,bounds.width-36,46));
            view.Detail.text=CampSession.Pending ? "Aguardando confirmação do servidor…" : selected ? Describe(current)+(price<0 ? " · Venda indisponível." : " · Total: "+total+" moedas.") : "Selecione uma mercadoria para comprar ou um item seu para vender.";
            return true;
        }
        void LayoutCompactCommerce(Rect bounds)
        {
            float left=bounds.width*.60f,right=bounds.width-left,inner=right-40;
            view.Heading.text=buyMode ? "MERCADORIAS" : tradeGrid==0 ? "MOCHILA · 12" : "BAÚ SEGURO · 16";
            ReadableText(view.Heading,28);
            UguiGeometry.Place(view.Heading.rectTransform,new Rect(20,10,left-40,36));
            TradeButton(view.CommerceMode,new Rect(20,56,220,64),buyMode ? "Ver venda" : "Ver compra",TradeReady,buyMode);
            view.SaleSource.gameObject.SetActive(!buyMode);
            TradeButton(view.SaleSource,new Rect(252,56,left-272,64),tradeGrid==0 ? "Ver baú" : "Ver mochila",TradeReady);
            GridLayout(0,new Rect(20,132,left-40,bounds.height-144),3,!buyMode && tradeGrid==0);
            GridLayout(1,new Rect(20,132,left-40,bounds.height-144),4,!buyMode && tradeGrid==1);
            var merchant=CampSession.Progress.Merchant;
            int count=merchant!=null && merchant.Offers!=null ? Mathf.Min(view.ShopOffers.Length,merchant.Offers.Length) : 0;
            int rows=Mathf.Max(1,Mathf.CeilToInt(count/4f));
            float cell=Mathf.Min((left-40)/4,(bounds.height-144)/rows),start=20+(left-40-cell*4)/2;
            for(int i=0;i<view.ShopOffers.Length;i++)
            {
                view.ShopOffers[i].gameObject.SetActive(buyMode && i<count);
                if(i>=count) continue;
                var offer=merchant.Offers[i];
                UguiGeometry.Place((RectTransform)view.ShopOffers[i].transform,new Rect(start+i%4*cell,132+i/4*cell,cell-6,cell-6));
                view.ShopIcons[i].rectTransform.anchorMin=new Vector2(.12f,.12f); view.ShopIcons[i].rectTransform.anchorMax=new Vector2(.88f,.82f);
                var stock=view.ShopStock[i]; stock.text=offer.Remaining==0 ? "0" : "×"+offer.Remaining;
                ReadableText(stock,24); stock.alignment=TMPro.TextAlignmentOptions.TopRight;
                stock.rectTransform.anchorMin=new Vector2(.06f,.50f); stock.rectTransform.anchorMax=new Vector2(.94f,.98f);
            }
            ReadableText(view.CommerceHeading,28);
            view.CommerceHeading.text="SALDO · "+CampSession.Progress.Character.Coins+" moedas";
            UguiGeometry.Place(view.CommerceHeading.rectTransform,new Rect(left+20,10,inner,56));
            ReadableText(view.Detail,24); view.Detail.alignment=TMPro.TextAlignmentOptions.TopLeft;
            bool selected=CurrentQuote(); var current=buyMode ? SelectedOffer?.Item ?? default : tradeItem;
            int price=buyMode ? SelectedOffer?.Price ?? -1 : CampTrade.SellPrice(tradeItem);
            if(selected && !CampSession.Pending)
                view.Detail.text=Describe(current)+"\nPreço por unidade: "+(price<0 ? "Venda indisponível" : price+" moedas")+(buyMode ? " · Estoque: "+SelectedOffer.Remaining : "");
            else if(!selected && !CampSession.Pending) view.Detail.text=buyMode ? "Selecione uma mercadoria para ver nome, preço e detalhes." : "Selecione um item da mochila ou do baú para vender.";
            ShowCompactDetails(new Rect(left+20,76,inner,108));
            UguiGeometry.Place((RectTransform)view.TradeLess.transform,new Rect(left+20,196,64,64));
            UguiGeometry.Place((RectTransform)view.TradeMore.transform,new Rect(left+96,196,64,64));
            UguiGeometry.Place((RectTransform)view.TradeAll.transform,new Rect(left+172,196,inner-152,64));
            UguiGeometry.Place((RectTransform)view.TradeUnload.transform,new Rect(left+20,272,inner,64));
            UguiGeometry.Place((RectTransform)view.TradeConfirm.transform,new Rect(left+20,348,inner,72));
            foreach(var button in new[] {view.CommerceMode,view.SaleSource,view.TradeLess,view.TradeMore,view.TradeAll,view.TradeUnload,view.TradeConfirm})
            {
                var text=button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                ReadableText(text,26); text.alignment=TMPro.TextAlignmentOptions.Center;
            }
        }
    }
}
