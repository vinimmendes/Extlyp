using UnityEngine;
using UnityEngine.EventSystems;
using CyberExtraction.Camp;
using CyberExtraction.Core;
using CyberExtraction.Items;
using CyberExtraction.Match;
using CyberExtraction.Players;

namespace CyberExtraction.UI
{
    public enum BasePage { Preparation,Inventory,Character,Commerce,Activities,Profile }

    [DefaultExecutionOrder(-100)]
    public sealed class UguiBaseHub : MonoBehaviour
    {
        static UguiBaseHub instance;
        BaseHubView view;
        UguiInventoryPresenter inventory;
        UguiProfilePresenter profilePresenter;
        bool profilePrompted;
        BasePage page;
        bool dirty=true,wasVisible;
        (bool busy,bool unavailable,string message,bool pending,bool synchronized,string campMessage,bool test,ExpeditionMap map,int bots) connectionState;
        float width=-1,fade;
        HudAppearance theme=(HudAppearance)(-1);
        int consumedBack=-1;
        bool compactDestination,compactLayout;
        bool confirmSeparateProfile;
        int selectedResource=-1;
        readonly UnityEngine.UI.Button[] resourceButtons=new UnityEngine.UI.Button[3];
        static bool WantsCompactLayout => Application.isMobilePlatform || UnityEngine.InputSystem.Touchscreen.current!=null || UiLayout.Width<1280;
        public static bool Compact => WantsCompactLayout;
        Rect safeBounds;
        public static bool Active => instance!=null && NetworkBootstrap.Instance!=null &&
            NetworkBootstrap.Instance.ReadyForMenus && ClientEnvironment.Chosen &&
            (CampController.Active || NetworkBootstrap.Instance.ConnectionBusy || NetworkBootstrap.Instance.ConnectionUnavailable);
        public static BasePage Page => instance!=null ? instance.page : BasePage.Preparation;
        public static bool ConsumedBack => instance!=null && instance.consumedBack==Time.frameCount;
        public static Rect ContentBounds => new Rect((UiLayout.Width-Mathf.Min(1200,UiLayout.Width-32))/2,WantsCompactLayout ? 128 : 116,Mathf.Min(1200,UiLayout.Width-32),432);
        public static void Show(BasePage next) { if(Active) instance.Navigate(next); }
        public void Initialize(BaseHubView screen,UguiInventoryPresenter items)
        {
            instance=this; view=screen; inventory=items;
            profilePresenter=gameObject.AddComponent<UguiProfilePresenter>(); profilePresenter.Initialize(view);
            InitializeResourceControls();
            for(int i=0;i<view.Tabs.Length;i++) { var target=(BasePage)i; view.Tabs[i].onClick.AddListener(()=>Navigate(target)); }
            view.Options.onClick.AddListener(GameOptionsMenu.ShowOptions);
            view.CharacterShortcut.onClick.AddListener(()=>Navigate(BasePage.Character));
            view.Organize.onClick.AddListener(()=>Navigate(BasePage.Inventory));
            view.Depart.onClick.AddListener(()=> { if(Ready) NetworkBootstrap.Instance.BeginExpedition(); });
            view.Cancel.onClick.AddListener(()=>NetworkBootstrap.Instance.CancelConnectionSearch());
            view.Retry.onClick.AddListener(()=>
            {
                var net=NetworkBootstrap.Instance;
                if(!net.DuplicateIdentity) { net.RetryConnection(); return; }
                if(!confirmSeparateProfile) { confirmSeparateProfile=true; dirty=true; return; }
                net.CreateSeparateProfileAndRetry(); confirmSeparateProfile=false; dirty=true;
            });
            view.Leave.onClick.AddListener(()=>
            {
                if(confirmSeparateProfile) { confirmSeparateProfile=false; dirty=true; NetworkBootstrap.Instance.RetryConnection(); }
                else NetworkBootstrap.Instance.ReturnToEnvironmentSelection();
            });
            view.Map.onClick.AddListener(()=>
            {
                var net=NetworkBootstrap.Instance;
                if(!Ready) return;
                net.ConfigureExpedition(ExpeditionMenu.NextMap(net.TestMap,net.IsTestEnvironment),net.TestBots);
            });
            view.Bots.onClick.AddListener(()=> { var net=NetworkBootstrap.Instance; if(Ready && net.IsTestEnvironment) net.ConfigureExpedition(net.TestMap,(net.TestBots+1)%MapBuilder.CapacityFor(net.TestMap)); });
            view.ActivityButtons[0].onClick.AddListener(()=>CampController.Instance.OpenBench());
            view.ActivityButtons[1].onClick.AddListener(()=>CampController.Instance.OpenKitchen());
            view.ActivityButtons[2].onClick.AddListener(()=>CampController.Instance.OpenFishing());
            CampSession.Changed+=Changed;
            BaseMenuStyle.Scope(view.gameObject,true);
            view.Panel.GetComponent<HudFrameGraphic>().FillOpacity=0;
            foreach(var tab in view.Tabs) tab.GetComponent<HudFrameGraphic>().BaseTab=true;
            ExpeditionArtwork.Apply(view.Artwork,ExpeditionMap.Dungeon);
            view.gameObject.SetActive(false);
        }
        bool Ready => CampController.Active && CampSession.Synchronized && !CampSession.Pending &&
            !NetworkBootstrap.Instance.ConnectionBusy && CampSession.Progress.ExpeditionState==0;
        void Changed()=>dirty=true;
        void Navigate(BasePage next)
        {
            if(NetworkBootstrap.Instance.ConnectionBusy || CampSession.Pending || CampController.Instance!=null && CampController.Instance.MinigameActive) return;
            if(next!=BasePage.Preparation && !CampSession.Synchronized) return;
            CampController.Instance?.ClosePreparation();
            InventoryWindow.Instance.Close();
            page=next; dirty=true; fade=0;
            if(next==BasePage.Inventory || next==BasePage.Character || next==BasePage.Commerce)
            { InventoryWindow.Instance.OpenSafe(); inventory.ShowBasePage(next); }
            if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(view.Tabs[(int)next].gameObject);
        }
        void Update()
        {
            if(view==null) return;
            if(!Active) { view.gameObject.SetActive(false); wasVisible=false; return; }
            var net=NetworkBootstrap.Instance;
            if(confirmSeparateProfile && !net.DuplicateIdentity) { confirmSeparateProfile=false; dirty=true; }
            bool overlay=GameOptionsMenu.IsOpen || HudLayoutEditor.IsOpen || CampTestPanel.IsOpen || TestLabPanel.IsOpen;
            bool minigame=CampController.Instance!=null && CampController.Instance.MinigameActive;
            bool visible=!overlay && !minigame;
            // Keep the destination behind the modal; its full-screen scrim owns pointer input.
            view.gameObject.SetActive(visible || GameOptionsMenu.IsOpen);
            if(!wasVisible && visible)
            {
                dirty=true; fade=0;
                if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(view.Tabs[(int)page].gameObject);
            }
            wasVisible=visible;
            if(!visible) return;
            if(!profilePrompted && Ready && !net.IsTestEnvironment)
            { profilePrompted=true; if(string.IsNullOrEmpty(CampSession.Progress.PlayerName)) Navigate(BasePage.Profile); }
            if(net.ConnectionBusy || net.ConnectionUnavailable)
            { if(page!=BasePage.Preparation) { InventoryWindow.Instance.Close(); CampController.Instance?.ClosePreparation(); page=BasePage.Preparation; dirty=true; } }
            if(GameInput.BackPressed && !GameOptionsMenu.ConsumedBack && !HudLayoutEditor.ConsumedBack)
            {
                consumedBack=Time.frameCount;
                if(net.ConnectionBusy) net.CancelConnectionSearch();
                else if(InventoryWindow.IsOpen && (InventoryWindow.Instance.CancelSelection?.Invoke()??false)) { }
                else if(CampController.Instance!=null && CampController.Instance.HasActivity) CampController.Instance.ClosePreparation();
                else if(page!=BasePage.Preparation) Navigate(BasePage.Preparation);
                else GameOptionsMenu.ShowMenu();
            }
            if((page==BasePage.Inventory || page==BasePage.Character || page==BasePage.Commerce) && !InventoryWindow.IsOpen)
            { page=BasePage.Preparation; dirty=true; }
            bool preparing=page==BasePage.Preparation;
            view.Preparation.gameObject.SetActive(preparing);
            view.Activities.gameObject.SetActive(page==BasePage.Activities && !CampController.Instance.HasActivity);
            profilePresenter.Show(page==BasePage.Profile);
            if(width!=UiLayout.Width || safeBounds!=UiLayout.SafeScreen || compactLayout!=WantsCompactLayout)
            { width=UiLayout.Width; safeBounds=UiLayout.SafeScreen; compactLayout=WantsCompactLayout; Layout(); dirty=true; }
            var nextState=(net.ConnectionBusy,net.ConnectionUnavailable,net.ConnectionMessage,CampSession.Pending,CampSession.Synchronized,CampSession.Message,net.IsTestEnvironment,net.TestMap,net.TestBots);
            if(!nextState.Equals(connectionState)) { connectionState=nextState; dirty=true; }
            if(theme!=HudTheme.Appearance) { theme=HudTheme.Appearance; dirty=true; }
            if(dirty) { dirty=false; Refresh(); }
            fade=Mathf.Min(1,fade+Time.unscaledDeltaTime/.15f); view.PreparationFade.alpha=fade;
        }
        void Layout()
        {
            UguiGeometry.Place(view.Panel,new Rect(0,0,width,600));
            var box=ContentBounds; float w=box.width;
            float scale=UiLayout.Scale;
            var background=new Rect(-safeBounds.x/scale,-(Screen.height-safeBounds.yMax)/scale,Screen.width/scale,Screen.height/scale);
            UguiGeometry.Place(view.Artwork.rectTransform,background);
            UguiGeometry.Place(view.BackdropShade.rectTransform,background);
            UguiGeometry.Place(view.HeaderShade.rectTransform,new Rect(background.x,background.y,background.width,108-background.y));
            UguiGeometry.Place(view.FooterShade.rectTransform,new Rect(background.x,550,background.width,background.y+background.height-550));
            UguiGeometry.Place(view.Brand.rectTransform,new Rect(box.x,6,170,20));
            UguiGeometry.Place(view.Heading.rectTransform,new Rect(box.x,28,170,32));
            Place(view.Options,new Rect(box.x+w-112,8,112,48));
            float resourceX=box.x+180,resourceWidth=(w-304)/3;
            for(int i=0;i<3;i++)
            {
                float x=resourceX+i*resourceWidth;
                UguiGeometry.Place(view.ResourceIcons[i].rectTransform,new Rect(x,17,24,28));
                UguiGeometry.Place(view.ResourceLabels[i].rectTransform,new Rect(x+30,8,resourceWidth-34,20));
                UguiGeometry.Place(view.ResourceValues[i].rectTransform,new Rect(x+30,30,resourceWidth-34,26));
            }
            for(int i=0;i<view.Tabs.Length;i++) Place(view.Tabs[i],new Rect(box.x+i*w/view.Tabs.Length,60,w/view.Tabs.Length,48));
            UguiGeometry.Place(view.Message.rectTransform,new Rect(box.x+8,554,w-16,38));
            UguiGeometry.Place(view.Preparation,box); UguiGeometry.Place(view.Activities,box);
            float available=w-24,left=Mathf.Max(240,available*.24f),right=Mathf.Max(280,available*.30f),middle=w-left-right-24;
            UguiGeometry.Place(view.Equipment,new Rect(0,0,left,432));
            UguiGeometry.Place(view.Scenery,new Rect(left+12,0,Mathf.Max(0,middle),432));
            compactDestination=middle<260;
            view.Scenery.gameObject.SetActive(!compactDestination);
            UguiGeometry.Place(view.Destination,new Rect(w-right,0,right,432));
            UguiGeometry.Place(view.PreparationsTitle.rectTransform,new Rect(20,18,left-40,32));
            UguiGeometry.Place(view.BackpackIcon.rectTransform,new Rect(20,76,56,66));
            UguiGeometry.Place(view.BackpackLabel.rectTransform,new Rect(90,78,left-110,24));
            UguiGeometry.Place(view.BackpackCount.rectTransform,new Rect(90,108,left-110,36));
            UguiGeometry.Place(view.CapacityTrack.rectTransform,new Rect(20,156,left-40,11));
            for(int i=0;i<2;i++)
            {
                UguiGeometry.Place(view.SupplyIcons[i].rectTransform,new Rect(20,188+i*40,26,26));
                UguiGeometry.Place(view.SupplyLabels[i].rectTransform,new Rect(54,188+i*40,left-116,28));
                UguiGeometry.Place(view.SupplyValues[i].rectTransform,new Rect(left-58,188+i*40,38,28));
            }
            UguiGeometry.Place(view.StatusText.rectTransform,new Rect(20,274,left-40,32));
            Place(view.Organize,new Rect(20,316,left-40,48));
            Place(view.CharacterShortcut,new Rect(20,374,left-40,48));
            UguiGeometry.Place(view.ExpeditionTitle.rectTransform,new Rect(20,18,right-40,32));
            view.MapTitle.rectTransform.SetParent(compactDestination ? view.Destination : view.Scenery,false);
            UguiGeometry.Place(view.MapTitle.rectTransform,compactDestination ? new Rect(20,58,right-40,62) : new Rect(16,86,middle-32,108));
            UguiGeometry.Place(view.DestinationLabel.rectTransform,new Rect(16,48,Mathf.Max(0,middle-32),26));
            UguiGeometry.Place(view.TitleShade.rectTransform,new Rect(0,38,Mathf.Max(0,middle),168));
            float top=compactDestination ? 132 : 74;
            Place(view.Map,new Rect(20,top,right-40,48));
            UguiGeometry.Place(view.ConnectionText.rectTransform,new Rect(20,top+68,right-40,30));
            UguiGeometry.Place(view.RiskText.rectTransform,new Rect(20,top+110,right-40,compactDestination ? 74 : 100));
            Place(view.Bots,new Rect(20,310,right-40,48));
            foreach(var b in new[] {view.Depart,view.Cancel,view.Retry}) Place(b,new Rect(20,374,right-40,48));
            Place(view.Leave,new Rect(20,310,right-40,48));
            for(int i=0;i<3;i++) Place(view.ActivityButtons[i],new Rect(24+i*(w-48)/3,115,(w-72)/3,150));
            view.Brand.gameObject.SetActive(!compactLayout);
            view.Heading.gameObject.SetActive(!compactLayout);
            view.ExpeditionTitle.gameObject.SetActive(!compactLayout);
            view.CharacterShortcut.gameObject.SetActive(!compactLayout);
            for(int i=0;i<3;i++)
            {
                view.ResourceLabels[i].gameObject.SetActive(!compactLayout);
                resourceButtons[i].gameObject.SetActive(compactLayout);
            }
            if(compactLayout) LayoutCompact(box,background);
            profilePresenter.Layout();
        }
        void LayoutCompact(Rect box,Rect background)
        {
            float w=box.width,panelWidth=(w-16)/2,inner=panelWidth-40;
            compactDestination=true;
            view.Scenery.gameObject.SetActive(false);
            UguiGeometry.Place(view.HeaderShade.rectTransform,new Rect(background.x,background.y,background.width,120-background.y));
            UguiGeometry.Place(view.FooterShade.rectTransform,new Rect(background.x,562,background.width,background.y+background.height-562));
            UguiGeometry.Place(view.Message.rectTransform,new Rect(box.x+8,564,w-16,36));
            Place(view.Options,new Rect(box.x+w-136,0,136,64));
            float resourceWidth=(w-148)/3;
            for(int i=0;i<3;i++)
            {
                float x=box.x+i*resourceWidth;
                Place(resourceButtons[i],new Rect(x,0,resourceWidth-12,64));
                UguiGeometry.Place(view.ResourceIcons[i].rectTransform,new Rect(x+8,16,32,32));
                UguiGeometry.Place(view.ResourceValues[i].rectTransform,new Rect(x+48,0,resourceWidth-64,64));
            }
            for(int i=0;i<view.Tabs.Length;i++) Place(view.Tabs[i],new Rect(box.x+i*w/view.Tabs.Length,64,w/view.Tabs.Length,56));
            UguiGeometry.Place(view.Equipment,new Rect(0,0,panelWidth,432));
            UguiGeometry.Place(view.Destination,new Rect(panelWidth+16,0,panelWidth,432));
            UguiGeometry.Place(view.PreparationsTitle.rectTransform,new Rect(20,12,inner,40));
            UguiGeometry.Place(view.BackpackIcon.rectTransform,new Rect(20,70,56,66));
            UguiGeometry.Place(view.BackpackLabel.rectTransform,new Rect(92,66,panelWidth-112,32));
            UguiGeometry.Place(view.BackpackCount.rectTransform,new Rect(92,100,panelWidth-112,40));
            UguiGeometry.Place(view.CapacityTrack.rectTransform,new Rect(20,154,inner,14));
            for(int i=0;i<2;i++)
            {
                UguiGeometry.Place(view.SupplyIcons[i].rectTransform,new Rect(20,190+i*44,32,32));
                UguiGeometry.Place(view.SupplyLabels[i].rectTransform,new Rect(64,188+i*44,panelWidth-152,36));
                UguiGeometry.Place(view.SupplyValues[i].rectTransform,new Rect(panelWidth-76,188+i*44,56,36));
            }
            UguiGeometry.Place(view.StatusText.rectTransform,new Rect(20,280,inner,60));
            Place(view.Organize,new Rect(20,348,inner,64));
            // The destination itself is the heading in the compact composition.
            view.ExpeditionTitle.gameObject.SetActive(false);
            view.MapTitle.rectTransform.SetParent(view.Destination,false);
            UguiGeometry.Place(view.MapTitle.rectTransform,new Rect(20,12,inner,76));
            Place(view.Map,new Rect(20,100,inner,64));
            UguiGeometry.Place(view.ConnectionText.rectTransform,new Rect(20,176,inner,32));
            UguiGeometry.Place(view.RiskText.rectTransform,new Rect(20,212,inner,124));
            // Test settings share the destination row without reducing touch height.
            float botsWidth=112;
            Place(view.Bots,new Rect(panelWidth-botsWidth-20,100,botsWidth,64));
            if(NetworkBootstrap.Instance.IsTestEnvironment) Place(view.Map,new Rect(20,100,inner-botsWidth-12,64));
            foreach(var button in new[] {view.Depart,view.Cancel,view.Retry}) Place(button,new Rect(20,348,inner,72));
            Place(view.Leave,new Rect(20,272,inner,64));
            if(NetworkBootstrap.Instance.ConnectionUnavailable)
            {
                UguiGeometry.Place(view.ConnectionText.rectTransform,new Rect(20,100,inner,32));
                UguiGeometry.Place(view.RiskText.rectTransform,new Rect(20,144,inner,116));
            }
        }
        static void Place(UnityEngine.UI.Button button,Rect bounds)=>UguiGeometry.Place((RectTransform)button.transform,bounds);
        static void Caption(UnityEngine.UI.Button button,string text)=>button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).text=text;
        void Refresh()
        {
            // Connection/test state can change without changing the screen dimensions.
            Layout();
            UguiTheme.Apply(view.gameObject);
            var net=NetworkBootstrap.Instance; var profile=CampSession.Progress;
            view.Heading.text=net.IsTestEnvironment ? "BASE · TESTE" : string.IsNullOrEmpty(profile.PlayerName) ? "BASE" : profile.PlayerName;
            view.Heading.richText=false;
            view.Message.text=CampSession.Pending ? "Aguardando confirmação do servidor…" : !CampSession.Synchronized ? net.ConnectionMessage : CampSession.Message;
            view.Message.color=BaseMenuStyle.Secondary;
            view.FooterShade.gameObject.SetActive(!string.IsNullOrWhiteSpace(view.Message.text));
            for(int i=0;i<view.Tabs.Length;i++)
            {
                var tab=view.Tabs[i]; tab.interactable=!net.ConnectionBusy && !CampSession.Pending && (i==0 || CampSession.Synchronized);
                UguiMenuStyle.Button(tab);
                var frame=tab.GetComponent<HudFrameGraphic>(); frame.BaseSelected=(int)page==i; frame.SetVerticesDirty();
                var label=tab.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                label.font=(int)page==i ? UiFontAssets.Bold : UiFontAssets.Regular;
                label.fontSize=20; label.color=(int)page==i ? HudTheme.Text : BaseMenuStyle.Secondary;
            }
            foreach(var button in new[] {view.Options,view.Organize,view.Depart,view.Cancel,view.Retry,view.Leave,view.Map,view.Bots}) UguiMenuStyle.Button(button,button==view.Depart);
            profilePresenter.Refresh();
            foreach(var icon in view.EquipmentIcons) icon.gameObject.SetActive(false);
            int used=0; foreach(var item in profile.Pack.Slots) if(!item.Empty) used++;
            view.BackpackCount.text=CampSession.Synchronized ? used+" / "+profile.Pack.Slots.Length+" espaços" : "Sincronizando…";
            view.CapacityFill.rectTransform.anchorMin=Vector2.zero;
            view.CapacityFill.rectTransform.anchorMax=new Vector2(CampSession.Synchronized ? Mathf.Clamp01((float)used/profile.Pack.Slots.Length) : 0,1);
            view.CapacityFill.rectTransform.sizeDelta=Vector2.zero; view.CapacityFill.rectTransform.anchoredPosition=Vector2.zero;
            view.CapacityFill.rectTransform.pivot=Vector2.one*.5f;
            view.CapacityTrack.color=HudTheme.Somber ? new Color(.20f,.21f,.17f) : new Color(.18f,.27f,.22f);
            view.CapacityFill.color=HudTheme.Accent;
            if(view.CapacityTrack is CapacitySegmentsGraphic segments)
            {
                segments.Occupied=CampSession.Synchronized ? used : 0; segments.SetVerticesDirty();
                view.CapacityFill.gameObject.SetActive(false);
            }
            view.SupplyValues[0].text=CampSession.Synchronized ? profile.Pack.Count(ItemId.Bandage).ToString() : "—";
            view.SupplyValues[1].text=CampSession.Synchronized ? profile.Pack.Count(ItemId.CrossbowBolt).ToString() : "—";
            view.StatusText.text=CampSession.Synchronized && profile.WellFed ? "Bem alimentado pronto" : "";
            view.StatusText.color=HudTheme.Text;
            view.BackpackIcon.sprite=MedievalMenuArt.Backpack; view.BackpackIcon.color=Color.white; view.BackpackIcon.preserveAspect=true;
            view.SupplyIcons[0].sprite=ItemIconLibrary.Get(ItemId.Bandage); view.SupplyIcons[1].sprite=ItemIconLibrary.Get(ItemId.CrossbowBolt);
            var resourceItems=new[] {ItemId.AncientCoin,ItemId.Wood,ItemId.Nails};
            string[] resourceValues={profile.Character.Coins.ToString("N0"),profile.Wood.ToString("N0"),profile.Nails.ToString("N0")};
            for(int i=0;i<3;i++)
            {
                view.ResourceValues[i].text=CampSession.Synchronized ? resourceValues[i] : "—";
                view.ResourceIcons[i].sprite=ItemIconLibrary.Get(resourceItems[i]); view.ResourceIcons[i].color=Color.white;
                view.ResourceIcons[i].preserveAspect=true;
                Label(view.ResourceLabels[i],16,false); view.ResourceLabels[i].color=BaseMenuStyle.Secondary;
                Label(view.ResourceValues[i],20,true);
            }
            for(int i=0;i<2;i++)
            {
                view.SupplyIcons[i].preserveAspect=true; view.SupplyIcons[i].color=Color.white;
                Label(view.SupplyLabels[i],18,false); Label(view.SupplyValues[i],20,true);
                view.SupplyValues[i].alignment=TMPro.TextAlignmentOptions.MidlineRight;
            }
            Label(view.Brand,14,false); view.Brand.characterSpacing=4; view.Brand.color=BaseMenuStyle.Secondary;
            Label(view.Heading,28,true); Label(view.PreparationsTitle,24,true); Label(view.ExpeditionTitle,24,true);
            view.Heading.font=view.PreparationsTitle.font=view.ExpeditionTitle.font=UiFontAssets.Title;
            view.PreparationsTitle.enableAutoSizing=view.ExpeditionTitle.enableAutoSizing=true;
            view.PreparationsTitle.fontSizeMin=view.ExpeditionTitle.fontSizeMin=19;
            view.PreparationsTitle.fontSizeMax=view.ExpeditionTitle.fontSizeMax=24;
            Label(view.BackpackLabel,18,false); view.BackpackLabel.color=BaseMenuStyle.Secondary;
            Label(view.BackpackCount,25,true);
            Caption(view.Organize,"Organizar mochila"); Caption(view.CharacterShortcut,"Personagem");
            UguiMenuStyle.Button(view.CharacterShortcut); view.CharacterShortcut.interactable=Ready;
            view.MapTitle.text=MapBuilder.MapName(net.TestMap).ToUpperInvariant();
            Label(view.MapTitle,compactDestination ? 24 : 32,true); view.MapTitle.textWrappingMode=TMPro.TextWrappingModes.Normal;
            view.MapTitle.font=UiFontAssets.Title;
            view.MapTitle.enableAutoSizing=true; view.MapTitle.fontSizeMin=compactDestination ? 19 : 24; view.MapTitle.fontSizeMax=compactDestination ? 24 : 34;
            Label(view.DestinationLabel,16,false); view.DestinationLabel.color=BaseMenuStyle.Secondary;
            ExpeditionArtwork.Apply(view.Artwork,net.TestMap);
            view.Artwork.color=Color.white;
            var panel=HudTheme.Panel; view.HeaderShade.color=new Color(panel.r,panel.g,panel.b,.97f);
            view.FooterShade.color=new Color(panel.r,panel.g,panel.b,.96f);
            view.BackdropShade.color=new Color(0,0,0,page==BasePage.Preparation ? .18f : .42f);
            view.TitleShade.color=new Color(.025f,.03f,.02f,.42f);
            view.ConnectionText.text=net.ConnectionBusy ? net.Searching ? "Buscando sala…" : "Conectando…" : net.ConnectionUnavailable ? "Sem conexão" : !CampSession.Synchronized ? "Sincronizando…" : CampSession.Pending ? "Aguardando servidor…" : "Conectado";
            Label(view.ConnectionText,20,false);
            view.ConnectionText.color=net.ConnectionUnavailable ? Color.Lerp(HudTheme.Danger,HudTheme.Text,.4f) : CampSession.Synchronized && !net.ConnectionBusy ? Color.Lerp(HudTheme.Good,HudTheme.Text,.35f) : HudTheme.Text;
            view.RiskText.text=net.ConnectionUnavailable ? net.ConnectionMessage : "Se morrer, perde os itens da mochila.";
            if(net.DuplicateIdentity && confirmSeparateProfile)
                view.RiskText.text="Este celular começará um perfil novo. O perfil anterior continua salvo. Confirmar ou tentar usar o perfil atual?";
            bool canDepart=CampSession.Synchronized && profile.CanPrepareRecoveryEquipment();
            if(Ready && !canDepart) view.RiskText.text="Libere espaço na mochila ou no baú para receber o equipamento básico.";
            Label(view.RiskText,20,false); view.RiskText.color=BaseMenuStyle.Secondary;
            view.RiskText.textWrappingMode=TMPro.TextWrappingModes.Normal;
            view.Depart.gameObject.SetActive(!net.ConnectionBusy && !net.ConnectionUnavailable); view.Depart.interactable=Ready && canDepart;
            view.Cancel.gameObject.SetActive(net.ConnectionBusy); view.Cancel.interactable=net.Searching;
            view.Retry.gameObject.SetActive(net.ConnectionUnavailable); view.Retry.interactable=!net.ConnectionBusy;
            view.Leave.gameObject.SetActive(net.ConnectionUnavailable);
            Caption(view.Retry,net.DuplicateIdentity ? confirmSeparateProfile ? "Confirmar novo perfil" : "Criar perfil neste celular" : "Tentar novamente");
            Caption(view.Leave,confirmSeparateProfile ? "Usar perfil atual" : "Voltar");
            view.Map.gameObject.SetActive(!net.ConnectionBusy && !net.ConnectionUnavailable); view.Map.interactable=Ready;
            Caption(view.Map,"Alterar destino"); Caption(view.Depart,"PARTIR");
            var departText=view.Depart.GetComponentInChildren<TMPro.TextMeshProUGUI>(true); departText.font=UiFontAssets.Bold; departText.fontSize=23;
            view.Bots.gameObject.SetActive(net.IsTestEnvironment && !net.ConnectionBusy && !net.ConnectionUnavailable); view.Bots.interactable=Ready; Caption(view.Bots,"Bots: "+net.TestBots);
            view.Organize.interactable=Ready;
            foreach(var button in view.ActivityButtons) { UguiMenuStyle.Button(button); button.interactable=Ready; }
            Caption(view.ActivityButtons[0],"BANCADA\nMunições e reciclador");
            Caption(view.ActivityButtons[1],"COZINHA\nPreparar e consumir peixe");
            Caption(view.ActivityButtons[2],profile.PierRepaired ? "PESCA\nIniciar atividade" : "HABILITAR PESCA\n20 madeira + 10 pregos");
            if(compactLayout) ApplyCompactPresentation();
            FitDestinationTitle();
            RefreshNavigation();
        }
        void FitDestinationTitle()
        {
            var title=view.MapTitle;
            // The shared label style uses ellipsis. Destination names instead own
            // enough height for all lines at the chosen readable font size.
            title.enableAutoSizing=false; title.fontSize=36;
            title.textWrappingMode=TMPro.TextWrappingModes.Normal;
            title.overflowMode=TMPro.TextOverflowModes.Overflow;
            title.alignment=TMPro.TextAlignmentOptions.TopLeft;
            var rect=title.rectTransform;
            float height=Mathf.Max(compactLayout ? 44 : rect.sizeDelta.y,Mathf.Ceil(title.GetPreferredValues(title.text,rect.sizeDelta.x,float.PositiveInfinity).y)+8);
            rect.sizeDelta=new Vector2(rect.sizeDelta.x,height);
            if(!compactLayout) return;
            // Move the following controls with the title so revealing its second
            // line never covers the destination button or connection status.
            float top=24+height;
            void Move(RectTransform target,float y)
            { target.anchoredPosition=new Vector2(target.anchoredPosition.x,-y); }
            Move((RectTransform)view.Map.transform,top);
            Move((RectTransform)view.Bots.transform,top);
            bool unavailable=NetworkBootstrap.Instance.ConnectionUnavailable;
            Move(view.ConnectionText.rectTransform,unavailable ? top : top+76);
            float riskTop=unavailable ? top+44 : top+112;
            Move(view.RiskText.rectTransform,riskTop);
            view.RiskText.rectTransform.sizeDelta=new Vector2(view.RiskText.rectTransform.sizeDelta.x,Mathf.Max(0,(unavailable ? 260 : 336)-riskTop));
        }
        void ApplyCompactPresentation()
        {
            var names=new[] {"Moedas","Madeira","Pregos"};
            for(int i=0;i<3;i++)
            {
                var text=view.ResourceValues[i];
                if(selectedResource==i) text.text=names[i]+"\n"+text.text;
                Label(text,26,true);
                text.textWrappingMode=TMPro.TextWrappingModes.Normal;
                text.color=selectedResource==i ? HudTheme.Accent : HudTheme.Text;
            }
            foreach(var button in view.Tabs) CompactCaption(button,22);
            foreach(var button in view.ActivityButtons) CompactCaption(button,26);
            foreach(var button in new[] {view.Options,view.Organize,view.Map,view.Bots,view.Cancel,view.Retry,view.Leave}) CompactCaption(button,26);
            CompactCaption(view.Depart,30);
            view.Depart.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).font=UiFontAssets.Bold;
            Label(view.PreparationsTitle,30,true); view.PreparationsTitle.font=UiFontAssets.Title;
            Label(view.MapTitle,30,true); view.MapTitle.font=UiFontAssets.Title;
            view.MapTitle.textWrappingMode=TMPro.TextWrappingModes.Normal;
            Label(view.BackpackLabel,24,false);
            Label(view.BackpackCount,30,true);
            for(int i=0;i<2;i++)
            {
                Label(view.SupplyLabels[i],24,false); Label(view.SupplyValues[i],26,true);
                view.SupplyValues[i].alignment=TMPro.TextAlignmentOptions.MidlineRight;
            }
            Label(view.StatusText,24,false); view.StatusText.textWrappingMode=TMPro.TextWrappingModes.Normal;
            Label(view.Message,24,false);
            var connectionColor=view.ConnectionText.color;
            Label(view.ConnectionText,24,false); view.ConnectionText.color=connectionColor;
            Label(view.RiskText,24,false); view.RiskText.textWrappingMode=TMPro.TextWrappingModes.Normal;
            view.RiskText.color=BaseMenuStyle.Secondary;
            if(CampSession.Pending && !NetworkBootstrap.Instance.ConnectionUnavailable)
                view.RiskText.text="Aguarde a confirmação do servidor para partir.";
            // Keep the decorative panel artwork, with its existing opaque backing.
            // Content text never auto-shrinks to accommodate the compact layout.
        }
        static void CompactCaption(UnityEngine.UI.Button button,float size)
        {
            var text=button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            var font=text.font; var color=text.color;
            Label(text,size,false); text.font=font; text.color=color;
            text.alignment=TMPro.TextAlignmentOptions.Center;
            text.textWrappingMode=TMPro.TextWrappingModes.Normal;
        }
        void InitializeResourceControls()
        {
            for(int i=0;i<3;i++)
            {
                int index=i;
                var target=new GameObject("ResourceInfo"+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button),typeof(EventTrigger));
                target.transform.SetParent(view.Panel,false);
                var image=target.GetComponent<UnityEngine.UI.Image>(); image.color=Color.clear;
                image.canvasRenderer.cullTransparentMesh=false;
                var button=target.GetComponent<UnityEngine.UI.Button>(); resourceButtons[i]=button;
                button.targetGraphic=image; button.transition=UnityEngine.UI.Selectable.Transition.None;
                view.ResourceIcons[i].raycastTarget=false; view.ResourceValues[i].raycastTarget=false; view.ResourceLabels[i].raycastTarget=false;
                button.onClick.AddListener(()=>
                {
                    if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(target);
                    selectedResource=index; dirty=true;
                });
                var trigger=target.GetComponent<EventTrigger>();
                void Listen(EventTriggerType kind,System.Action action)
                {
                    var entry=new EventTrigger.Entry {eventID=kind};
                    entry.callback.AddListener(_=>action()); trigger.triggers.Add(entry);
                }
                Listen(EventTriggerType.Select,()=> { selectedResource=index; dirty=true; });
                Listen(EventTriggerType.Deselect,()=> { if(selectedResource==index) { selectedResource=-1; dirty=true; } });
                Listen(EventTriggerType.PointerEnter,()=> { selectedResource=index; dirty=true; });
                Listen(EventTriggerType.PointerExit,()=>
                {
                    if(selectedResource==index && (EventSystem.current==null || EventSystem.current.currentSelectedGameObject!=target))
                    { selectedResource=-1; dirty=true; }
                });
            }
        }
        void RefreshNavigation()
        {
            var buttons=view.GetComponentsInChildren<UnityEngine.UI.Button>();
            foreach(var button in buttons)
            {
                if(!button.IsActive() || !button.IsInteractable()) continue;
                var nav=new UnityEngine.UI.Navigation {mode=UnityEngine.UI.Navigation.Mode.Automatic};
                // Other pages include inventory controls outside this hierarchy;
                // leave their existing automatic cross-panel navigation available.
                if(page==BasePage.Preparation)
                {
                    nav.mode=UnityEngine.UI.Navigation.Mode.Explicit;
                    nav.selectOnLeft=Neighbor(button,Vector2.left,buttons);
                    nav.selectOnRight=Neighbor(button,Vector2.right,buttons);
                    nav.selectOnUp=Neighbor(button,Vector2.up,buttons);
                    nav.selectOnDown=Neighbor(button,Vector2.down,buttons);
                }
                button.navigation=nav;
            }
            if(EventSystem.current==null) return;
            var selected=EventSystem.current.currentSelectedGameObject;
            var control=selected!=null ? selected.GetComponent<UnityEngine.UI.Selectable>() : null;
            if(selected==null || !selected.activeInHierarchy || control!=null && !control.IsInteractable())
            {
                UnityEngine.UI.Button fallback=view.Tabs[(int)page];
                if(!fallback.IsActive() || !fallback.IsInteractable())
                {
                    fallback=null;
                    foreach(var candidate in buttons)
                        if(candidate.IsActive() && candidate.IsInteractable()) { fallback=candidate; break; }
                }
                EventSystem.current.SetSelectedGameObject(fallback!=null ? fallback.gameObject : null);
            }
        }
        static UnityEngine.UI.Button Neighbor(UnityEngine.UI.Button source,Vector2 direction,UnityEngine.UI.Button[] buttons)
        {
            var rect=(RectTransform)source.transform;
            Vector2 center=rect.TransformPoint(rect.rect.center);
            UnityEngine.UI.Button best=null; float score=float.PositiveInfinity;
            foreach(var candidate in buttons)
            {
                if(candidate==source || !candidate.IsActive() || !candidate.IsInteractable()) continue;
                var other=(RectTransform)candidate.transform;
                Vector2 delta=(Vector2)other.TransformPoint(other.rect.center)-center;
                float forward=Vector2.Dot(direction,delta); if(forward<=.01f) continue;
                float distance=delta.sqrMagnitude/forward;
                if(distance<score) { score=distance; best=candidate; }
            }
            return best;
        }
        static void Label(TMPro.TextMeshProUGUI text,float size,bool bold)
        {
            text.font=bold ? UiFontAssets.Bold : UiFontAssets.Regular; text.fontSize=size; text.enableAutoSizing=false;
            text.color=HudTheme.Text; text.textWrappingMode=TMPro.TextWrappingModes.NoWrap;
            text.overflowMode=TMPro.TextOverflowModes.Ellipsis; text.alignment=TMPro.TextAlignmentOptions.MidlineLeft;
        }
        void OnDestroy() { CampSession.Changed-=Changed; if(instance==this) instance=null; }
    }
}
