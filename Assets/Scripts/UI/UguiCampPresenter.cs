using UnityEngine;
using CyberExtraction.Camp;
using CyberExtraction.Items;
using CyberExtraction.Core;

namespace CyberExtraction.UI
{
    public sealed class UguiCampPresenter : MonoBehaviour
    {
        UguiPrefabView cabin,activities;
        int lastActivity=-1;
        string last;
        readonly int[] fishSources=new int[ItemCatalog.BackpackSlots];
        bool preparationDirty=true;
        float lastWidth,lastBaseTop;
        bool lastPending,lastCompact;
        UnityEngine.UI.ScrollRect compactScroll;
        readonly System.Collections.Generic.Dictionary<Transform,Transform> compactParents=new System.Collections.Generic.Dictionary<Transform,Transform>();
        float activityScrollPosition=1;
        void PreparationChanged()=>preparationDirty=true;
        public void Initialize(UguiPrefabView baseView,UguiPrefabView preparation)
        {
            cabin=baseView; activities=preparation;
            CampSession.Changed+=PreparationChanged;
            cabin.Buttons[0].onClick.AddListener(()=>CampController.Instance.RequestInteraction());
            cabin.Buttons[1].onClick.AddListener(()=>InventoryWindow.Instance.ToggleBag());
            // Firearm recipes are retired. These serialized controls remain hidden.
            activities.Buttons[3].onClick.AddListener(()=>CampController.Instance.UpgradeRecycler());
            activities.Buttons[4].onClick.AddListener(()=>CampController.Instance.BuyComponent());
            activities.Buttons[5].onClick.AddListener(()=>CampController.Instance.ClosePreparation());
            for(int i=0;i<activities.Slots.Length;i++) { int slot=i; activities.Slots[i].OnTap=()=>CampController.Instance.ChooseFish(fishSources[slot]); }
        }
        void Update()
        {
            if(cabin==null) return;
            var camp=CampController.Instance;
            bool baseVisible=!UguiBaseHub.Active && CampController.Active && UiScreenCoordinator.Top==PlayerScreen.Gameplay;
            bool prepare=CampController.Active && UiScreenCoordinator.Top==PlayerScreen.Preparation && camp!=null && !camp.MinigameActive;
            cabin.gameObject.SetActive(baseVisible); activities.gameObject.SetActive(prepare);
            if(!baseVisible && !prepare) { lastActivity=-1; preparationDirty=true; return; }
            var profile=CampSession.Progress;
            string key=profile.Wood+"|"+profile.Nails+"|"+profile.Bandages+"|"+profile.RecyclerLevel+"|"+CampSession.Message+"|"+CampSession.Pending+"|"+CampSession.Synchronized+"|"+camp.NearbyInteraction+"|"+camp.PresentationActivity+"|"+HudTheme.Appearance;
            // Slots are small; activity selection also observes server snapshots without predicting rewards.
            if(last!=key) { last=key; preparationDirty=true; UguiTheme.Apply(cabin.gameObject); UguiTheme.Apply(activities.gameObject); }
            UguiGeometry.Place(cabin.Panel,new Rect(0,0,UiLayout.Width,600));
            UguiGeometry.Place(cabin.Heading.rectTransform,new Rect((UiLayout.Width-320)/2,16,320,38));
            cabin.Heading.text="CABANA"; cabin.Body.text="M "+profile.Wood+"   P "+profile.Nails+"   B "+profile.Bandages+(profile.WellFed ? "\nBem alimentado pronto" : ""); cabin.Detail.text=CampSession.Message;
            int near=camp.NearbyInteraction;
            cabin.Buttons[0].interactable=near!=0 && CampSession.Synchronized && !CampSession.Pending;
            cabin.Buttons[0].GetComponentInChildren<TMPro.TextMeshProUGUI>(true).text=near==1 ? "Abrir baú" : near==2 ? "Preparar expedição" : near==3 ? profile.PierRepaired ? "Pescar" : "Habilitar pesca: M 20 + P 10" : near==4 ? "Bancada" : near==5 ? "Fogão a lenha" : "Caminhe até uma instalação";
            UguiGeometry.Place((RectTransform)cabin.Buttons[0].transform,CampController.InteractionBounds);
            UguiGeometry.Place((RectTransform)cabin.Buttons[1].transform,HudLayout.CampInventory);
            if(!prepare) { lastActivity=-1; return; }
            int activity=camp.PresentationActivity; bool stove=activity==5;
            float baseTop=UguiBaseHub.Active ? UguiBaseHub.ContentBounds.y : 0;
            if(lastActivity==activity && lastWidth==UiLayout.Width && lastBaseTop==baseTop && lastCompact==UguiBaseHub.Compact && lastPending==CampSession.Pending && !preparationDirty) return;
            if(compactScroll!=null)
            {
                activityScrollPosition=compactScroll.verticalNormalizedPosition;
                foreach(var pair in compactParents) pair.Key.SetParent(pair.Value,false);
                compactScroll.gameObject.SetActive(false);
            }
            preparationDirty=false; lastWidth=UiLayout.Width; lastBaseTop=baseTop; lastCompact=UguiBaseHub.Compact; lastPending=CampSession.Pending;
            BaseMenuStyle.Scope(activities.gameObject,UguiBaseHub.Active);
            int fishCount=0;
            for(int i=0;i<profile.Pack.Slots.Length;i++)
            { var item=profile.Pack.Slots[i].Item; if(ItemCatalog.Fish(item) || item==ItemId.RawFish || item==ItemId.CookedFish) fishCount++; }
            int rows=Mathf.Max(1,Mathf.CeilToInt(fishCount/3f));
            float width=Mathf.Min(stove ? 760 : 900,UiLayout.Width-64);
            float height=UguiBaseHub.Active ? 432 : stove ? Mathf.Min(500,210+rows*150) : 510;
            var bounds=new Rect((UiLayout.Width-width)/2,UguiBaseHub.Active ? baseTop : (600-height)/2,width,height);
            UguiGeometry.Place(activities.Panel,bounds); UguiMenuStyle.Backdrop(activities,bounds);
            activities.Backdrop.gameObject.SetActive(!UguiBaseHub.Active);
            UguiGeometry.Place(activities.Heading.rectTransform,new Rect(24,12,width-48,40));
            activities.Heading.fontSize=26;
            activities.Heading.text=stove ? "ESCOLHA UM PEIXE" : "BANCADA";
            for(int i=0;i<5;i++) activities.Buttons[i].gameObject.SetActive(!stove);
            for(int i=0;i<5;i++) activities.Buttons[i].interactable=CampSession.Synchronized && !CampSession.Pending;
            foreach(var button in activities.Buttons) UguiMenuStyle.Button(button);
            for(int i=0;i<3;i++) activities.RecipeCards[i].gameObject.SetActive(!stove);
            activities.ResourceStrip.gameObject.SetActive(!stove);
            activities.Grids[0].gameObject.SetActive(stove);
            activities.Body.gameObject.SetActive(stove);
            activities.Body.font=UiFontAssets.Regular; activities.Body.fontSize=18;
            UguiGeometry.Place(activities.Body.rectTransform,new Rect(24,62,width-48,44));
            if(stove)
            {
                activities.Body.text=fishCount==0 ? "Você não tem peixes disponíveis na mochila." : "Escolha um peixe da mochila para cozinhar ou comer.";
                float size=Mathf.Min(180,Mathf.Min((width-80)/3,(height-214)/rows));
                var group=activities.Grids[0].GetComponent<UnityEngine.UI.GridLayoutGroup>(); group.cellSize=new Vector2(size-6,size-6);
                UguiGeometry.Place(activities.Grids[0],new Rect((width-size*3)/2,114,size*3,size*rows));
                int visible=0;
                for(int i=0;i<profile.Pack.Slots.Length;i++)
                {
                    var stack=profile.Pack.Slots[i];
                    if(stack.Empty || !(ItemCatalog.Fish(stack.Item) || stack.Item==ItemId.RawFish || stack.Item==ItemId.CookedFish)) continue;
                    fishSources[visible]=i; var slot=activities.Slots[visible++]; slot.gameObject.SetActive(true);
                    slot.Present(stack);
                    slot.GetComponent<UnityEngine.UI.Button>().interactable=CampSession.Synchronized && !CampSession.Pending;
                }
                for(int i=visible;i<activities.Slots.Length;i++) activities.Slots[i].gameObject.SetActive(false);
                activities.Detail.text="Peixe cru: cozinhar · Peixe cozido: comer";
                UguiGeometry.Place((RectTransform)activities.Buttons[5].transform,new Rect((width-200)/2,height-56,200,48));
            }
            else
            {
                var ammo=new[] {ItemId.CrossbowBolt,ItemId.CrossbowBolt,ItemId.CrossbowBolt};
                var resources=new[] {ItemId.Metal,ItemId.Gunpowder,ItemId.AmmoComponent};
                var recipe=GameplayDefinition.Current;
                string cost=recipe.MetalRecipeCost+" metal + "+recipe.PowderRecipeCost+" pólvora + "+recipe.ComponentRecipeCost+" componente";
                UguiGeometry.Place(activities.ResourceStrip,new Rect(26,60,width-52,50));
                float resourceWidth=(width-52)/3,cardWidth=(width-64)/3;
                for(int i=0;i<3;i++)
                {
                    activities.ResourceIcons[i].sprite=ItemIconLibrary.Get(resources[i]);
                    activities.ResourceIcons[i].preserveAspect=true;
                    UguiGeometry.Place(activities.ResourceIcons[i].rectTransform,new Rect(i*resourceWidth+12,4,42,42));
                    UguiGeometry.Place(activities.ResourceValues[i].rectTransform,new Rect(i*resourceWidth+62,0,resourceWidth-70,50));
                    activities.ResourceValues[i].font=UiFontAssets.Regular; activities.ResourceValues[i].fontSize=17;
                    activities.ResourceValues[i].text=ItemCatalog.Name(resources[i])+"\n"+profile.Count(resources[i]);
                    UguiGeometry.Place(activities.RecipeCards[i],new Rect(24+i*(cardWidth+8),112,cardWidth,230));
                    activities.RecipeIcons[i].sprite=ItemIconLibrary.Get(ammo[i]);
                    activities.RecipeIcons[i].preserveAspect=true;
                    UguiGeometry.Place(activities.RecipeIcons[i].rectTransform,new Rect(24,52,cardWidth-48,70));
                    UguiGeometry.Place(activities.RecipeTitles[i].rectTransform,new Rect(10,8,cardWidth-20,44));
                    activities.RecipeTitles[i].font=UiFontAssets.Regular; activities.RecipeTitles[i].fontSize=18; activities.RecipeTitles[i].text=ItemCatalog.Name(ammo[i]);
                    UguiGeometry.Place(activities.RecipeDetails[i].rectTransform,new Rect(8,126,cardWidth-16,42));
                    activities.RecipeDetails[i].font=UiFontAssets.Regular; activities.RecipeDetails[i].fontSize=14;
                    activities.RecipeDetails[i].text="Produz "+WeaponCatalog.RecipeOutput(ammo[i])+"\n"+cost;
                    UguiGeometry.Place((RectTransform)activities.Buttons[i].transform,new Rect(10,174,cardWidth-20,48));
                    activities.Buttons[i].GetComponentInChildren<TMPro.TextMeshProUGUI>(true).text="Fabricar";
                    UguiMenuStyle.Button(activities.Buttons[i],true);
                    activities.RecipeCards[i].gameObject.SetActive(false);
                    activities.Buttons[i].gameObject.SetActive(false);
                }
                activities.Buttons[3].GetComponentInChildren<TMPro.TextMeshProUGUI>(true).text="Reciclador nível "+profile.RecyclerLevel+" · Melhorar ("+((profile.RecyclerLevel+1)*10)+" metais)";
                activities.Buttons[4].GetComponentInChildren<TMPro.TextMeshProUGUI>(true).text="Adquirir componente · "+recipe.ComponentPrice+" metais";
                UguiGeometry.Place((RectTransform)activities.Buttons[3].transform,new Rect(24,140,width*.48f-30,48));
                UguiGeometry.Place((RectTransform)activities.Buttons[4].transform,new Rect(width*.48f,140,width*.32f-8,48));
                UguiGeometry.Place((RectTransform)activities.Buttons[5].transform,new Rect(width*.80f,140,width*.20f-24,48));
                activities.Detail.text=CampSession.Pending ? "Aguardando confirmação do servidor..." : CampSession.Message;
            }
            activities.Detail.fontSize=15; activities.Detail.color=HudTheme.Muted;
            activities.Detail.overflowMode=TMPro.TextOverflowModes.Ellipsis;
            UguiGeometry.Place(activities.Detail.rectTransform,new Rect(24,stove ? height-88 : 405,width-48,28));
            if(UguiBaseHub.Compact) LayoutCompactActivity(width,height,stove,rows);
            if(lastActivity!=activity && UnityEngine.EventSystems.EventSystem.current!=null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(activities.Buttons[5].gameObject);
            lastActivity=activity;
        }
        void MoveToActivityScroll(Transform target)
        {
            if(!compactParents.ContainsKey(target)) compactParents.Add(target,target.parent);
            target.SetParent(compactScroll.content,false);
        }
        static void ActivityText(TMPro.TextMeshProUGUI text,float size)
        {
            text.font=UiFontAssets.Regular; text.fontSize=size; text.enableAutoSizing=false;
            text.textWrappingMode=TMPro.TextWrappingModes.Normal; text.overflowMode=TMPro.TextOverflowModes.Ellipsis;
        }
        void LayoutCompactActivity(float width,float height,bool stove,int rows)
        {
            if(compactScroll==null) compactScroll=UguiGeometry.CreateVerticalScroll(activities.Panel,"CompactActivityContent");
            compactScroll.gameObject.SetActive(true);
            float inner=width-48,viewportHeight=height-152;
            UguiGeometry.Place((RectTransform)compactScroll.transform,new Rect(24,64,inner,viewportHeight));
            UguiGeometry.Place(compactScroll.viewport,new Rect(0,0,inner,viewportHeight));
            ActivityText(activities.Heading,30);
            MoveToActivityScroll(activities.Detail.transform);
            float bottom;
            if(stove)
            {
                MoveToActivityScroll(activities.Body.transform);
                ActivityText(activities.Body,24);
                UguiGeometry.Place(activities.Body.rectTransform,new Rect(0,0,inner,64));
                MoveToActivityScroll(activities.Grids[0]);
                float size=Mathf.Min(148,inner/3);
                var group=activities.Grids[0].GetComponent<UnityEngine.UI.GridLayoutGroup>();
                group.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount; group.constraintCount=3;
                group.cellSize=new Vector2(size-6,size-6); group.spacing=new Vector2(6,6);
                UguiGeometry.Place(activities.Grids[0],new Rect((inner-size*3)/2,76,size*3,size*rows));
                bottom=88+size*rows;
            }
            else
            {
                MoveToActivityScroll(activities.ResourceStrip);
                UguiGeometry.Place(activities.ResourceStrip,new Rect(0,0,inner,76));
                for(int i=0;i<3;i++)
                {
                    float third=inner/3;
                    UguiGeometry.Place(activities.ResourceIcons[i].rectTransform,new Rect(i*third,16,40,40));
                    UguiGeometry.Place(activities.ResourceValues[i].rectTransform,new Rect(i*third+48,0,third-56,76));
                    ActivityText(activities.ResourceValues[i],24);
                    MoveToActivityScroll(activities.RecipeCards[i]);
                    UguiGeometry.Place(activities.RecipeCards[i],new Rect(0,88+i*140,inner,128));
                    UguiGeometry.Place(activities.RecipeIcons[i].rectTransform,new Rect(12,28,72,72));
                    UguiGeometry.Place(activities.RecipeTitles[i].rectTransform,new Rect(100,8,inner-312,36));
                    ActivityText(activities.RecipeTitles[i],28);
                    UguiGeometry.Place(activities.RecipeDetails[i].rectTransform,new Rect(100,44,inner-312,80));
                    ActivityText(activities.RecipeDetails[i],24);
                    UguiGeometry.Place((RectTransform)activities.Buttons[i].transform,new Rect(inner-196,32,184,64));
                }
                MoveToActivityScroll(activities.Buttons[3].transform); MoveToActivityScroll(activities.Buttons[4].transform);
                UguiGeometry.Place((RectTransform)activities.Buttons[3].transform,new Rect(0,100,inner,64));
                UguiGeometry.Place((RectTransform)activities.Buttons[4].transform,new Rect(0,176,inner,64));
                bottom=252;
            }
            ActivityText(activities.Detail,24);
            float detailHeight=Mathf.Max(64,activities.Detail.GetPreferredValues(activities.Detail.text,inner,float.PositiveInfinity).y);
            activities.Detail.overflowMode=TMPro.TextOverflowModes.Overflow;
            UguiGeometry.Place(activities.Detail.rectTransform,new Rect(0,bottom,inner,detailHeight));
            UguiGeometry.Place(compactScroll.content,new Rect(0,0,inner,bottom+detailHeight));
            compactScroll.StopMovement();
            compactScroll.verticalNormalizedPosition=lastActivity==CampController.Instance.PresentationActivity ? Mathf.Clamp01(activityScrollPosition) : 1;
            foreach(var button in activities.Buttons)
            {
                var text=button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                ActivityText(text,26); text.alignment=TMPro.TextAlignmentOptions.Center;
                if(button.GetComponent<ScrollSelectedIntoView>()==null) button.gameObject.AddComponent<ScrollSelectedIntoView>();
            }
            foreach(var slot in activities.Slots)
                if(slot.GetComponent<ScrollSelectedIntoView>()==null) slot.gameObject.AddComponent<ScrollSelectedIntoView>();
            UguiGeometry.Place((RectTransform)activities.Buttons[5].transform,new Rect(24,height-76,inner,64));
        }
        void OnDestroy()=>CampSession.Changed-=PreparationChanged;
    }
}
