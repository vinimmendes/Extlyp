using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Text=TMPro.TextMeshProUGUI;
using Font=TMPro.TMP_FontAsset;
using Element=CyberExtraction.UI.UguiHudElement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using CyberExtraction.Combat;
using CyberExtraction.Core;
using CyberExtraction.Items;
using CyberExtraction.Match;
using CyberExtraction.Players;
using CyberExtraction.Camp;

namespace CyberExtraction.UI
{
    // Retained Canvas UI. The existing HUD placement data remains the source of truth.
    public sealed partial class UguiGameHud : MonoBehaviour
    {

        static UguiGameHud instance;
        public static bool Active=>instance!=null && instance.ready;
        public static bool ResultsVisible=>Active && instance.results!=null && instance.results.gameObject.activeInHierarchy;
        internal static Transform NavigationRoot(PlayerScreen screen)
        {
            if(!Active) return null;
            if(UguiBaseHub.Active && (screen==PlayerScreen.Base || screen==PlayerScreen.Inventory || screen==PlayerScreen.Preparation)) return instance.safe;
            switch(screen)
            {
                case PlayerScreen.Menu:return instance.menu;
                case PlayerScreen.Inventory:return instance.interfaceRoot.Inventory.Panel;
                case PlayerScreen.Expedition:return instance.interfaceRoot.Expedition.Panel;
                case PlayerScreen.Organizer:return instance.editor;
                case PlayerScreen.Results:return instance.results;
                default:return null;
            }
        }
        readonly Dictionary<HudControl,Element> elements=new Dictionary<HudControl,Element>();
        readonly List<Button> menuButtons=new List<Button>();
        readonly List<Action> menuActions=new List<Action>();
        readonly List<RectTransform> exitMarkers=new List<RectTransform>();
        readonly List<Text> sectorLabels=new List<Text>();
        readonly List<Image> exitArtwork=new List<Image>();
        readonly List<Text> exitMapLabels=new List<Text>();
        readonly List<Text> exitChoiceLabels=new List<Text>();
        readonly List<Button> exitChoiceButtons=new List<Button>();
        readonly List<Image> exitChoiceImages=new List<Image>();
        bool mapWasExpanded;
        Canvas canvas;
        CanvasScaler scaler;
        RectTransform safe,menu,editor,results,mapViewport;
        Text menuTitle,menuBody,editorTitle,editorSize,resultText;
        Slider sizeSlider;
        RawImage mapImage;
        Image mapPlayer;
        ScrollRect mapExitList;
        RectTransform mapExitContent;
        Image menuDimmer;
        PlayerInterfaceRoot interfaceRoot;
        Button toolbarToggle;
        Button legacyLayoutButton;
        Button resultBack;
        Font regular,bold;
        EventSystem ownedEvents;
        PlayerInventory player;
        Health health;
        SporeContamination spores;
        PlayerShooter shooter;
        PlayerController movement;
        MatchManager match;
        bool ready,dirty=true,toolsVisible=true,wasEditor,lastHudCompact;
        float refreshAt,arrow,arrowVelocity,mapHeading,mapVelocity;
        bool directionReady;
        Rect lastSafe;
        HudAppearance lastTheme=(HudAppearance)(-1);
        int lastMenu=-1;
        void Awake()
        {
            if(Application.isBatchMode) { enabled=false; return; }
            var asset=Resources.Load<GameObject>("UI/Prefabs/PlayerInterface");
            var prefab=asset!=null ? asset.GetComponent<PlayerInterfaceRoot>() : null;
            if(prefab==null) { Debug.LogError("PlayerInterface prefab ausente."); enabled=false; return; }
            interfaceRoot=Instantiate(prefab,transform,false); interfaceRoot.Initialize();
            instance=this; regular=UiFontAssets.Regular; bold=UiFontAssets.Bold;
            canvas=interfaceRoot.Canvas; scaler=interfaceRoot.Scaler; safe=interfaceRoot.SafeArea;
            foreach(var element in interfaceRoot.Hud.Elements)
            {
                elements.Add(element.Control,element); var control=element.Control;
                element.Button.navigation=new Navigation {mode=Navigation.Mode.None};
                element.Button.onClick.AddListener(()=>Click(control));
            }
            mapViewport=interfaceRoot.Hud.Viewport; mapImage=interfaceRoot.Hud.MapImage;
            mapPlayer=interfaceRoot.Hud.PlayerMarker;
            var exitListObject=new GameObject("Map exit choices",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));
            exitListObject.transform.SetParent(elements[HudControl.Minimap].Visual,false);
            var listRect=(RectTransform)exitListObject.transform; listRect.anchorMin=listRect.anchorMax=listRect.pivot=new Vector2(0,1);
            exitListObject.GetComponent<Image>().color=Color.clear;
            mapExitList=exitListObject.GetComponent<ScrollRect>(); mapExitList.horizontal=true; mapExitList.vertical=false; mapExitList.movementType=ScrollRect.MovementType.Clamped; mapExitList.viewport=listRect;
            var contentObject=new GameObject("Exit choices content",typeof(RectTransform)); contentObject.transform.SetParent(listRect,false);
            mapExitContent=(RectTransform)contentObject.transform; mapExitContent.anchorMin=mapExitContent.anchorMax=mapExitContent.pivot=new Vector2(0,1); mapExitList.content=mapExitContent;
            exitMarkers.AddRange(interfaceRoot.Hud.ExitMarkers); sectorLabels.AddRange(interfaceRoot.Hud.SectorLabels);
            foreach(var marker in exitMarkers)
            {
                var originalLabel=marker.GetComponentInChildren<Text>(true); exitChoiceLabels.Add(originalLabel);
                exitChoiceButtons.Add(marker.GetComponent<Button>()); exitChoiceImages.Add(marker.GetComponent<Image>());
                var go=new GameObject("Exit symbol",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                go.transform.SetParent(marker,false); go.transform.SetAsFirstSibling();
                var image=go.GetComponent<Image>(); image.raycastTarget=false; image.preserveAspect=true;
                image.rectTransform.anchorMin=image.rectTransform.anchorMax=image.rectTransform.pivot=new Vector2(.5f,.5f);
                exitArtwork.Add(image);
                var badge=Instantiate(originalLabel,image.transform,false); badge.name="Geographic exit ID"; badge.raycastTarget=false;
                exitMapLabels.Add(badge);
            }
            while(sectorLabels.Count<DungeonLayout.SectorNames.Length)
                sectorLabels.Add(Instantiate(sectorLabels[0],sectorLabels[0].transform.parent));
            for(int i=0;i<exitMarkers.Count;i++) { int id=i; var button=exitChoiceButtons[i]; button.navigation=new Navigation {mode=Navigation.Mode.None}; button.onClick.AddListener(()=> { if(!HudLayoutEditor.IsOpen && Minimap.IsExpanded && player!=null) player.RequestExitDestination(id); }); }
            var menuView=interfaceRoot.Menus; menu=menuView.Panel; menuTitle=menuView.Heading; menuBody=menuView.Body;
            menuButtons.AddRange(menuView.Buttons);
            for(int i=0;i<menuButtons.Count;i++) { int id=i; menuButtons[i].onClick.AddListener(()=> { if(id<menuActions.Count) menuActions[id](); }); }
            var layout=interfaceRoot.Organizer; editor=layout.Panel; editorTitle=layout.Heading; editorSize=layout.Detail; sizeSlider=layout.Slider;
            layout.Buttons[0].onClick.AddListener(()=>HudLayoutEditor.SelectNext(-1));
            layout.Buttons[1].onClick.AddListener(()=>HudLayoutEditor.SelectNext(1));
            layout.Buttons[2].onClick.AddListener(()=>HudLayout.ResetPlacement(HudLayout.EditingControl(HudLayoutEditor.Selected)));
            layout.Buttons[3].onClick.AddListener(HudLayoutEditor.Commit); layout.Buttons[4].onClick.AddListener(HudLayoutEditor.Discard); layout.Buttons[5].onClick.AddListener(HudLayout.RestoreDefaults);
            toolbarToggle=layout.Buttons[6]; toolbarToggle.onClick.AddListener(()=>toolsVisible=!toolsVisible);
            legacyLayoutButton=Instantiate(layout.Buttons[5],editor);
            legacyLayoutButton.name="RestoreLegacyLayout"; legacyLayoutButton.onClick.RemoveAllListeners();
            legacyLayoutButton.GetComponentInChildren<Text>(true).text="Restaurar organização anterior";
            legacyLayoutButton.onClick.AddListener(HudLayout.RestoreLegacy);
            sizeSlider.onValueChanged.AddListener(value=> { if(HudLayoutEditor.IsOpen) HudLayout.ResizeForEditing(HudLayoutEditor.Selected,value); });
            results=interfaceRoot.Results.Panel; resultText=interfaceRoot.Results.Body; resultBack=interfaceRoot.Results.Buttons[0];
            resultBack.onClick.AddListener(()=> { NetworkManager.Singleton?.Shutdown(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); });
            if(FindFirstObjectByType<EventSystem>()==null)
            {
                var events=new GameObject("Player UI EventSystem",typeof(EventSystem)); events.transform.SetParent(transform,false);
                ownedEvents=events.GetComponent<EventSystem>(); ContextUiInputModule.Install(ownedEvents);
            }
            else
            {
                var events=FindFirstObjectByType<EventSystem>();
                ContextUiInputModule.Install(events);
            }
            var inventoryPresenter=gameObject.AddComponent<UguiInventoryPresenter>();
            inventoryPresenter.Initialize(interfaceRoot.Inventory);
            gameObject.AddComponent<UguiBaseHub>().Initialize(interfaceRoot.BaseHub,inventoryPresenter);
            gameObject.AddComponent<UguiExpeditionPresenter>().Initialize(interfaceRoot.Expedition);
            gameObject.AddComponent<UguiCampPresenter>().Initialize(interfaceRoot.Cabin,interfaceRoot.Activities);
            ready=true;
        }

        void Unbind()
        {
            if(movement!=null) { movement.Stamina.OnValueChanged-=FloatChanged; movement.MaxStamina.OnValueChanged-=FloatChanged; }
            if(health!=null) { health.Current.OnValueChanged-=IntChanged; health.Maximum.OnValueChanged-=IntChanged; }
            if(spores!=null) spores.Level.OnValueChanged-=FloatChanged;
            if(player!=null) { player.Bandages.OnValueChanged-=IntChanged; player.Status.OnValueChanged-=ByteChanged; player.EquippedSlot.OnValueChanged-=IntChanged; player.ExtractionProgress.OnValueChanged-=FloatChanged; player.Slots.OnListChanged-=ItemsChanged; player.Equipment.OnListChanged-=ItemsChanged; }
            if(match!=null) { match.SecondsLeft.OnValueChanged-=IntChanged; match.State.OnValueChanged-=ByteChanged; }
        }
        void Bind(PlayerInventory next,MatchManager nextMatch)
        {
            Unbind(); player=next; match=nextMatch; health=player!=null ? player.GetComponent<Health>() : null;
            spores=player!=null ? player.GetComponent<SporeContamination>() : null; shooter=player!=null ? player.GetComponent<PlayerShooter>() : null;
            movement=player!=null ? player.GetComponent<PlayerController>() : null;
            if(movement!=null) { movement.Stamina.OnValueChanged+=FloatChanged; movement.MaxStamina.OnValueChanged+=FloatChanged; }
            if(health!=null) { health.Current.OnValueChanged+=IntChanged; health.Maximum.OnValueChanged+=IntChanged; }
            if(spores!=null) spores.Level.OnValueChanged+=FloatChanged;
            if(player!=null) { player.Bandages.OnValueChanged+=IntChanged; player.Status.OnValueChanged+=ByteChanged; player.EquippedSlot.OnValueChanged+=IntChanged; player.ExtractionProgress.OnValueChanged+=FloatChanged; player.Slots.OnListChanged+=ItemsChanged; player.Equipment.OnListChanged+=ItemsChanged; }
            if(match!=null) { match.SecondsLeft.OnValueChanged+=IntChanged; match.State.OnValueChanged+=ByteChanged; }
            dirty=true; directionReady=false;
        }
        void IntChanged(int before,int after)=>dirty=true;
        void FloatChanged(float before,float after)=>dirty=true;
        void ByteChanged(byte before,byte after)=>dirty=true;
        void ItemsChanged(NetworkListEvent<ItemStack> change)=>dirty=true;
        void Update()
        {
            if(!ready) return;
            var next=InventoryWindow.Local(); if(next!=player || MatchManager.Instance!=match) Bind(next,MatchManager.Instance);
            scaler.scaleFactor=UiLayout.Scale;
            bool style=lastTheme!=HudTheme.Appearance || lastSafe!=UiLayout.SafeScreen || wasEditor!=HudLayoutEditor.IsOpen || lastHudCompact!=UguiBaseHub.Compact;
            if(style) { lastTheme=HudTheme.Appearance; lastSafe=UiLayout.SafeScreen; lastHudCompact=UguiBaseHub.Compact; dirty=true; }
            safe.anchorMin=new Vector2(lastSafe.x/Mathf.Max(1,Screen.width),lastSafe.y/Mathf.Max(1,Screen.height));
            safe.anchorMax=new Vector2(lastSafe.xMax/Mathf.Max(1,Screen.width),lastSafe.yMax/Mathf.Max(1,Screen.height)); safe.offsetMin=safe.offsetMax=Vector2.zero;
            bool edit=HudLayoutEditor.IsOpen;
            var screen=UiScreenCoordinator.Top;
            bool overlay=screen!=PlayerScreen.Gameplay && screen!=PlayerScreen.Organizer;
            bool expedition=!CampController.Active && match!=null && NetworkManager.Singleton!=null && NetworkManager.Singleton.IsConnectedClient;
            bool running=expedition && player!=null && player.IsAlive && match.CurrentState==MatchState.Running && player.FishingPhase.Value==0;
            foreach(var n in elements.Values)
            {
                bool show=edit || !overlay && (n.Control==HudControl.Menu ? !ExpeditionMenu.IsOpen : expedition &&
                    (n.Control==HudControl.Health || n.Control==HudControl.Clock || running && !Minimap.IsExpanded));
                if(n.Control==HudControl.Minimap || n.Control==HudControl.Map) show=edit || !overlay && expedition && MapBuilder.Current!=ExpeditionMap.Test;
                if(n.Control==HudControl.Map) show=false;
                if(n.Control==HudControl.Clock && Minimap.IsExpanded && !edit) show=false;
                if(n.Control==HudControl.Food) show&=edit || player!=null && player.WellFed.Value;
                if(n.Control==HudControl.Spores) show&=edit || spores!=null && spores.Level.Value>0;
                if(n.Control==HudControl.Interaction) show&=edit || interactionAvailable;
                if(n.Control==HudControl.Weapon) show&=edit || shooter!=null && shooter.Weapon!=null;
                if(n.Control==HudControl.Move) show|=!overlay && CampController.Active && CampController.Instance.CanMove;
                n.Root.gameObject.SetActive(show);
                n.Selection.enabled=edit && HudLayout.EditingControl(HudLayoutEditor.Selected)==HudLayout.EditingControl(n.Control) || !edit && n.Button.interactable && EventSystem.current!=null && EventSystem.current.currentSelectedGameObject==n.Root.gameObject; n.Selection.effectColor=HudTheme.Accent;
                var box=HudLayout.Bounds(n.Control);
                if(n.Control==HudControl.Minimap && Minimap.IsExpanded && !edit) box=ExpandedMap();
                if(style || n.Previous!=box) Geometry(n,box);
                n.Hit.raycastTarget=edit || HudLayout.Interactive(n.Control);
            }
            wasEditor=edit;
            if(dirty || Time.unscaledTime>=refreshAt || edit) { RefreshData(edit); refreshAt=Time.unscaledTime+.1f; dirty=false; }
            // Results owns input while visible; it must not suppress its own next refresh.
            Animate(); RefreshMap(edit); RefreshMenu(style); RefreshEditor(edit); RefreshResults((!overlay || screen==PlayerScreen.Results) && !edit && expedition);
        }
        void Set(HudControl c,string title,string main="",string detail="")
        { var n=elements[c]; if(n.Title.text!=title) n.Title.text=title; if(n.Main.text!=main) n.Main.text=main; if(n.Secondary.text!=detail) n.Secondary.text=detail; }
        void Enabled(HudControl c,bool enabled)
        {
            var n=elements[c]; bool available=enabled || HudLayoutEditor.IsOpen;
            n.Button.interactable=available;
            var tint=available ? HudTheme.Text : Color.Lerp(HudTheme.Text,new Color(.47f,.47f,.45f,1),UguiBaseHub.Compact ? .35f : 1);
            n.Icon.color=n.Title.color=tint;
            n.Background.Unavailable=!available; n.Background.SetVerticesDirty();
        }
        void Bar(HudControl c,float value,Color color)
        {
            var n=elements[c]; var basis=n.Visual.sizeDelta;
            float u=HudMetrics.Unit;
            var box=c==HudControl.Health || c==HudControl.Stamina ? new Rect(6*u,22*u,basis.x-12*u,4*u) : c==HudControl.Clock ? new Rect(6*u,n.Background.rectTransform.sizeDelta.y-4*u,basis.x-12*u,2*u) : new Rect(8*u,basis.y-6*u,basis.x-16*u,2*u);
            Place(n.BarTrack.rectTransform,box);
            n.BarTrack.color=new Color(.4f,.42f,.36f,.35f); n.BarTrack.gameObject.SetActive(n.Bar.gameObject.activeSelf);
            box.width*=Mathf.Clamp01(value);
            Place(n.Bar.rectTransform,box); n.Bar.color=color;
        }
        bool interactionAvailable;
        void RefreshData(bool edit)
        {
            bool compact=true;
            string staminaValue=edit ? "70 / 100" : movement!=null ? Mathf.CeilToInt(movement.Stamina.Value)+" / "+Mathf.CeilToInt(movement.MaxStamina.Value) : "—";
            Set(HudControl.Stamina,compact ? "STAMINA" : "STAMINA   "+staminaValue,compact ? staminaValue : "");
            Bar(HudControl.Stamina,edit ? .7f : movement!=null ? movement.Stamina.Value/Mathf.Max(1,movement.MaxStamina.Value) : 0,HudTheme.Accent);
            Set(HudControl.Sprint,"CORRER");
            // Keep the hold target active at exhaustion so releasing still clears the pointer.
            Enabled(HudControl.Sprint,edit || movement!=null && movement.CanSprint);
            string healthValue=edit ? "86 / 120" : health!=null ? health.Current.Value+" / "+health.Maximum.Value : "—";
            Set(HudControl.Health,compact ? "VIDA" : "VIDA   "+healthValue,compact ? healthValue : "");
            HudLayout.HealthContentWidth=Mathf.Max(144,64+elements[HudControl.Health].Main.GetPreferredValues(healthValue).x/HudMetrics.Unit);
            Bar(HudControl.Health,edit ? .72f : health!=null ? (float)health.Current.Value/Mathf.Max(1,health.Maximum.Value) : 0,HudTheme.Somber || health!=null && health.Current.Value<health.Maximum.Value*.3f ? HudTheme.Danger : HudTheme.Good);
            Set(HudControl.Food,UguiBaseHub.Compact ? "Alimentado" : "Bem alimentado"); elements[HudControl.Food].Title.color=elements[HudControl.Food].Icon.color=HudTheme.Good;
            Set(HudControl.Spores,"Esporos "+(edit ? 24 : spores!=null ? Mathf.CeilToInt(spores.Level.Value) : 0)+"%");
            int seconds=match!=null ? match.SecondsLeft.Value : 222;
            Set(HudControl.Clock,"Extração",edit ? "03:42" : (seconds/60).ToString("00")+":"+(seconds%60).ToString("00"));
            var clock=elements[HudControl.Clock];
            clock.Main.fontSize=HudMetrics.Dp(22);
            string note="";
            if(player!=null && !edit)
            {
                int exit=player.SelectedExit.Value;
                if(MapBuilder.Current==ExpeditionMap.Large && exit>=0 && exit<LargeMapPlan.Exits.Length && player.ExitKnowledge(exit)!="Confirmada") note=LargeMapPlan.Exits[exit].Name+" · "+player.ExitKnowledge(exit);
                if(player.ExtractionProgress.Value>0)
                {
                    int extracting=player.CurrentExtractionExit.Value;
                    note+=(note.Length>0 ? "\n" : "")+(extracting>=0 && extracting<LargeMapPlan.Exits.Length && LargeMapPlan.Exits[extracting].Transport ? "Embarcando" : "Deixando a floresta");
                }
            }
            if(!edit && MapBuilder.Current==ExpeditionMap.Dungeon && player!=null)
            {
                int selected=player.SelectedExit.Value;
                note="";
                if(selected>=0 && selected<DungeonLayout.ExitPositions.Length)
                {
                    Vector2 position=player.transform.position;
                    float rangeSquared=DungeonContent.InteractionRange*DungeonContent.InteractionRange;
                    bool nearby=(position-DungeonContent.Mechanism(selected)).sqrMagnitude<=rangeSquared ||
                        (position-DungeonLayout.ExitPositions[selected]).sqrMagnitude<=rangeSquared;
                    if(nearby)
                    {
                        note=(selected+1)+" · "+DungeonContent.ExitStatus(selected);
                        if(player.Count(ItemId.DungeonCrank)>0 || player.Count(ItemId.DungeonKey)>0)
                            note+="\n"+(player.Count(ItemId.DungeonCrank)>0 ? "Manivela " : "")+(player.Count(ItemId.DungeonKey)>0 ? "Chave " : "");
                    }
                }
                if(player.ExtractionProgress.Value>0) note="Saindo: "+MapBuilder.ExitName(player.CurrentExtractionExit.Value);
            }
            elements[HudControl.Clock].Note.text=note;
            if(compact) CompactClockContext(clock);
            elements[HudControl.Clock].Bar.gameObject.SetActive(!edit && player!=null && player.ExtractionProgress.Value>0);
            Bar(HudControl.Clock,player!=null ? player.ExtractionProgress.Value : 0,HudTheme.Good);
            Set(HudControl.Menu,"Menu"); Set(HudControl.Move,"Mover"); Set(HudControl.Aim,""); Set(HudControl.Fire,"ATIRAR");
            var weapon=shooter!=null ? shooter.Weapon : null;
            bool melee=weapon!=null && weapon.IsMelee && !edit;
            Set(HudControl.Weapon,"",edit ? "12" : weapon!=null && !melee ? shooter.Magazine.ToString() : "",
                edit ? "24" : weapon!=null && !melee ? shooter.Reserve.ToString() : "");
            var weaponDisplay=elements[HudControl.Weapon];
            HudLayout.WeaponContentWidth=Mathf.Max(104,65+Mathf.Max(weaponDisplay.Main.GetPreferredValues(weaponDisplay.Main.text).x,weaponDisplay.Secondary.GetPreferredValues(weaponDisplay.Secondary.text).x)/HudMetrics.Unit);
            weaponDisplay.Main.color=!edit && !melee && shooter!=null && shooter.Magazine==0 ? HudTheme.Danger : HudTheme.Text;
            var displayedItem=weapon!=null ? weapon.Item : ItemId.ImprovisedHandCrossbow;
            var replacement=ItemVisualCatalog.Interface(displayedItem);
            elements[HudControl.Weapon].Icon.sprite=replacement!=null ? replacement : RefinedHudArt.Weapon(displayedItem);
            elements[HudControl.Weapon].Icon.color=replacement!=null ? Color.white : HudTheme.Text;
            elements[HudControl.Weapon].Icon.gameObject.SetActive(edit || weapon!=null);
            if(melee)
            {
                Set(HudControl.Fire,compact ? "ATACAR" : shooter.MeleeAttacking ? "GOLPE" : "ATACAR");
                // Attack is a functional control symbol; only the item display uses the new catalog.
                elements[HudControl.Fire].Icon.sprite=RefinedHudArt.Weapon(weapon.Item);
                elements[HudControl.Fire].Icon.rectTransform.localEulerAngles=Vector3.zero;
                var displaySize=weaponDisplay.Visual.sizeDelta;
                Place(weaponDisplay.Icon.rectTransform,new Rect((displaySize.x-HudMetrics.Dp(44))/2,HudMetrics.Dp(7),HudMetrics.Dp(44),HudMetrics.Dp(26)));
            }
            else
            {
                Place(weaponDisplay.Icon.rectTransform,new Rect(HudMetrics.Dp(8),HudMetrics.Dp(7),HudMetrics.Dp(44),HudMetrics.Dp(26)));
                elements[HudControl.Fire].Icon.sprite=RefinedHudArt.Icon(HudSymbol.Bullet);
                elements[HudControl.Fire].Icon.rectTransform.localEulerAngles=Vector3.zero;
            }
            double cooldown=player!=null && NetworkManager.Singleton!=null ? Math.Max(0,player.BandageReadyAt.Value-NetworkManager.Singleton.ServerTime.Time) : 0;
            int bandages=edit ? 3 : player!=null ? player.Bandages.Value : 0;
            string healValue=cooldown>0 && !edit ? Math.Ceiling(cooldown)+" s" : bandages>99 ? "99+" : bandages.ToString();
            Set(HudControl.Heal,compact ? healValue : "Curar\n"+healValue);
            var heal=elements[HudControl.Heal]; if(heal.Wait!=null) { heal.Wait.gameObject.SetActive(cooldown>0 && !edit); heal.Wait.fillAmount=Mathf.Clamp01((float)cooldown/Mathf.Max(.01f,ItemCatalog.BandageCooldown)); }
            var reload=elements[HudControl.Reload];
            if(reload.Wait!=null)
            {
                bool waiting=!edit && shooter!=null && shooter.Reloading && weapon!=null;
                double remaining=waiting ? Math.Max(0,shooter.ReloadEndsAt.Value-NetworkManager.Singleton.ServerTime.Time) : 0;
                reload.Wait.gameObject.SetActive(waiting); reload.Wait.fillAmount=waiting ? Mathf.Clamp01((float)remaining/Mathf.Max(.01f,weapon.ReloadSeconds)) : 0;
                reload.Title.gameObject.SetActive(waiting); reload.Title.font=bold; reload.Title.fontSize=HudMetrics.Dp(14);
                float rw=reload.Visual.sizeDelta.x,rh=reload.Visual.sizeDelta.y,u=HudMetrics.Unit;
                Place(reload.Title.rectTransform,new Rect(0,rh-21*u,rw,18*u));
                Place(reload.Icon.rectTransform,waiting ? new Rect(rw*.27f,7*u,rw*.46f,rh*.46f) : new Rect(rw*.22f,rh*.22f,rw*.56f,rh*.56f));
            }
            Set(HudControl.Swap,"Trocar"); Set(HudControl.Reload,shooter!=null && !edit && shooter.Reloading ? "Recarga..." : shooter!=null && !edit && shooter.CyclingBolt ? "Ferrolho" : UguiBaseHub.Compact ? "Recarga" : "Recarregar"); Set(HudControl.Backpack,"Mochila");
            if(reload.Wait!=null && reload.Wait.gameObject.activeSelf) reload.Title.text=Math.Ceiling(Math.Max(0,shooter.ReloadEndsAt.Value-NetworkManager.Singleton.ServerTime.Time))+" s";
            Enabled(HudControl.Heal,edit || player!=null && health!=null && player.Bandages.Value>0 && health.Current.Value<health.Maximum.Value && cooldown<=0);
            Enabled(HudControl.Swap,edit || player!=null && player.WeaponCount>1 && shooter!=null && !shooter.MeleeAttacking);
            Enabled(HudControl.Reload,edit || shooter!=null && weapon!=null && !weapon.IsMelee && !shooter.MeleeAttacking && !shooter.Reloading && !shooter.CyclingBolt && shooter.Reserve>0 && shooter.Magazine<weapon.Capacity);
            // An empty crossbow can still act: ServerFire starts reload when reserve exists.
            Enabled(HudControl.Fire,edit || shooter!=null && weapon!=null && !shooter.Reloading && (weapon.IsMelee || shooter.Magazine>0 || shooter.Reserve>0));
            RefreshInteraction(edit);
        }
        void RefreshInteraction(bool edit)
        {
            interactionAvailable=false;
            if(edit) { Set(HudControl.Interaction,"Abrir baú · 2 s"); Bar(HudControl.Interaction,0,HudTheme.Text); return; }
            if(player==null || !player.IsAlive) return;
            var nearest=LootPickup.NearestChest(player.transform.position);
            int mechanism=match!=null ? match.ContextDungeonAction(player,nearest) : -1;
            if(mechanism>=0)
            {
                interactionAvailable=true; Set(HudControl.Interaction,match.DungeonActionLabel(player,mechanism));
                Bar(HudControl.Interaction,player.DungeonActionProgress,HudTheme.Text);
                elements[HudControl.Interaction].Button.interactable=player.DungeonAction.Value>=0 || match.CanOperateDungeon(player,mechanism); return;
            }
            var passage=ForestPassage.Nearby(player); var mimic=CyberExtraction.Enemies.Monster.NearestDisguised(player.transform.position);
            bool usePassage=passage!=null && (nearest==null || Vector2.Distance(player.transform.position,passage.transform.position)<Vector2.Distance(player.transform.position,nearest.transform.position));
            string action=usePassage ? passage.Route.Action : nearest!=null ? nearest.Locked.Value ? "Abrir fechadura" : nearest.OpenState.Value==2 ? "Saquear" : nearest.FixtureKind.Value==3 ? "Abrir porta" : nearest.FixtureKind.Value!=0 ? "Abrir armário" : "Abrir baú" : mimic!=null ? "Abrir baú" : null;
            if(action==null)
            {
                if(MapBuilder.Current==ExpeditionMap.Dungeon && match!=null && match.DungeonShortcut.Value &&
                    Vector2.Distance(player.transform.position,DungeonContent.ShortcutControl)<=DungeonContent.InteractionRange)
                {
                    interactionAvailable=true;
                    Set(HudControl.Interaction,"Passagem para o embarcadouro liberada");
                    Bar(HudControl.Interaction,1,HudTheme.Text);
                    elements[HudControl.Interaction].Button.interactable=false;
                }
                return;
            }
            interactionAvailable=true;
            bool container=!usePassage && nearest!=null && !nearest.Locked.Value && nearest.FixtureKind.Value!=3;
            bool opening=container && nearest.OpenState.Value==1;
            bool busy=opening && nearest.Opener.Value!=player.OwnerClientId;
            if(opening) action=busy ? "Em uso" : "Abrindo";
            if(container && nearest.OpenState.Value!=2 && !busy) action+=" · "+Mathf.CeilToInt((2+nearest.Rarity.Value*2)*(1-nearest.OpenProgress.Value))+" s";
            Set(HudControl.Interaction,action); Bar(HudControl.Interaction,container ? nearest.OpenProgress.Value : 0,HudTheme.Text);
            elements[HudControl.Interaction].Button.interactable=!opening;
        }
        void Click(HudControl c)
        {
            if(HudLayoutEditor.IsOpen) { HudLayoutEditor.Selected=c; return; }
            if(c==HudControl.Menu) { GameOptionsMenu.ShowMenu(); return; }
            if(c==HudControl.Minimap) { Minimap.Toggle(); return; }
            if(c==HudControl.Backpack && CampController.Active && !UiLayout.BlocksGameplay) { InventoryWindow.Instance?.ToggleBag(); return; }
            if(UiLayout.BlocksGameplay || player==null || !player.IsAlive) return;
            if(c==HudControl.Heal) player.RequestBandage();
            else if(c==HudControl.Swap) player.RequestNextWeapon();
            else if(c==HudControl.Reload) shooter?.RequestReload();
            else if(c==HudControl.Backpack) InventoryWindow.Instance?.ToggleBag();
            else if(c==HudControl.Interaction) InventoryWindow.Instance?.Interact();
        }
        void Animate()
        {
            bool edit=HudLayoutEditor.IsOpen;
            if(player!=null && !edit)
            {
                int exit=player.SelectedExit.Value;
                var destination=exit>=0 && exit<MapBuilder.ExitCount ? MapBuilder.ExitPosition(exit) : MapBuilder.Current==ExpeditionMap.Test ? MapBuilder.ExtractionPoints[0] : (Vector2)player.transform.position;
                var d=destination-(Vector2)player.transform.position;
                float target=90-Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg;
                if(!directionReady) { arrow=target; mapHeading=90-player.transform.eulerAngles.z; directionReady=true; }
                else if(d.sqrMagnitude>.01f) arrow=Mathf.SmoothDampAngle(arrow,target,ref arrowVelocity,.16f,Mathf.Infinity,Time.unscaledDeltaTime);
                elements[HudControl.Clock].Secondary.text=Mathf.RoundToInt(d.magnitude)+" u";
                mapHeading=Mathf.SmoothDampAngle(mapHeading,90-player.transform.eulerAngles.z,ref mapVelocity,.12f,Mathf.Infinity,Time.unscaledDeltaTime);
            }
            else if(edit) elements[HudControl.Clock].Secondary.text="280 u";
            elements[HudControl.Clock].Icon.gameObject.SetActive(true);
            elements[HudControl.Clock].Icon.rectTransform.localEulerAngles=new Vector3(0,0,edit ? -45 : -arrow);
            var move=elements[HudControl.Move]; var tc=TouchControls.Instance; var input=tc!=null && tc.IsTouch ? tc.Move : GameInput.Move;
            if(!edit && (tc!=null && tc.SprintEnabled || movement!=null && movement.Sprinting.Value))
                elements[HudControl.Sprint].Icon.color=elements[HudControl.Sprint].Title.color=HudTheme.Accent;
            var size=move.Visual.sizeDelta; var center=size*.5f+new Vector2(input.x,-input.y)*size.x*.29f;
            Place(move.Knob.rectTransform,new Rect(center-Vector2.one*size.x*.14f,Vector2.one*size.x*.28f)); move.Knob.color=Color.white;
        }

        void OnDestroy()
        { Unbind(); ready=false; if(instance==this) instance=null; if(canvas!=null) Destroy(canvas.gameObject); if(ownedEvents!=null) Destroy(ownedEvents.gameObject); }
    }
}
