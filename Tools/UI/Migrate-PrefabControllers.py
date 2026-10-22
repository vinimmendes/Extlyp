"""One-time source migration from the runtime prototype to prefab presenters."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
path=ROOT/'Assets/Scripts/UI/UguiGameHud.cs'
source=path.read_text(encoding='utf-8')
def span(text, signature):
    start=text.index(signature); brace=text.index('{',start); depth=1; i=brace+1
    # All removed methods contain ordinary strings but no braces inside literals.
    quoted=False; escape=False
    while depth:
        ch=text[i]
        if quoted:
            if escape: escape=False
            elif ch=='\\': escape=True
            elif ch=='"': quoted=False
        elif ch=='"': quoted=True
        elif ch=='{': depth+=1
        elif ch=='}': depth-=1
        i+=1
    return start,i
def remove(signature):
    global source
    a,b=span(source,signature); source=source[:a]+source[b:]
remove('        sealed class Element')
for signature in ['        static RectTransform Rect(', '        RawImage Art(', '        Text Label(', '        Button MakeButton(', '        void Build(', '        void BuildMap(', '        void BuildMenu(', '        void BuildEditor(', '        void BuildResults(']: remove(signature)
a,b=span(source,'        void Awake()')
awake='''        void Awake()
        {
            if(Application.isBatchMode) { enabled=false; return; }
            var prefab=Resources.Load<PlayerInterfaceRoot>("UI/PlayerInterface");
            if(prefab==null) { Debug.LogError("PlayerInterface prefab ausente."); enabled=false; return; }
            interfaceRoot=Instantiate(prefab,transform,false); interfaceRoot.Initialize();
            instance=this; regular=UiFontAssets.Regular; bold=UiFontAssets.Bold;
            canvas=interfaceRoot.Canvas; scaler=interfaceRoot.Scaler; safe=interfaceRoot.SafeArea;
            foreach(var element in interfaceRoot.Hud.Elements)
            {
                elements.Add(element.Control,element); var control=element.Control;
                element.Button.onClick.AddListener(()=>Click(control));
            }
            mapViewport=interfaceRoot.Hud.Viewport; mapImage=interfaceRoot.Hud.MapImage;
            mapPlayer=interfaceRoot.Hud.PlayerMarker;
            exitMarkers.AddRange(interfaceRoot.Hud.ExitMarkers); sectorLabels.AddRange(interfaceRoot.Hud.SectorLabels);
            for(int i=0;i<exitMarkers.Count;i++) { int id=i; exitMarkers[i].GetComponent<Button>().onClick.AddListener(()=> { if(!HudLayoutEditor.IsOpen && Minimap.IsExpanded && player!=null) player.RequestExitDestination(id); }); }
            var menuView=interfaceRoot.Menus; menu=menuView.Panel; menuTitle=menuView.Heading; menuBody=menuView.Body;
            menuButtons.AddRange(menuView.Buttons);
            for(int i=0;i<menuButtons.Count;i++) { int id=i; menuButtons[i].onClick.AddListener(()=> { if(id<menuActions.Count) menuActions[id](); }); }
            var layout=interfaceRoot.Organizer; editor=layout.Panel; editorTitle=layout.Heading; editorSize=layout.Detail; sizeSlider=layout.Slider;
            layout.Buttons[0].onClick.AddListener(()=>HudLayoutEditor.Selected=(HudControl)(((int)HudLayoutEditor.Selected+15)%16));
            layout.Buttons[1].onClick.AddListener(()=>HudLayoutEditor.Selected=(HudControl)(((int)HudLayoutEditor.Selected+1)%16));
            layout.Buttons[2].onClick.AddListener(()=>HudLayout.ResetPlacement(HudLayoutEditor.Selected));
            layout.Buttons[3].onClick.AddListener(HudLayoutEditor.Commit); layout.Buttons[4].onClick.AddListener(HudLayoutEditor.Discard); layout.Buttons[5].onClick.AddListener(HudLayout.RestoreDefaults);
            toolbarToggle=layout.Buttons[6]; toolbarToggle.onClick.AddListener(()=>toolsVisible=!toolsVisible);
            sizeSlider.onValueChanged.AddListener(value=> { if(!HudLayoutEditor.IsOpen) return; var c=HudLayoutEditor.Selected; var p=HudLayout.Placement(c); var box=HudLayout.Bounds(c); p.Size=HudLayout.DefaultSize(c)*value; p.Offset+=(box.size-p.Size)*.5f; HudLayout.SetPlacement(c,p); });
            results=interfaceRoot.Results.Panel; resultText=interfaceRoot.Results.Body; resultBack=interfaceRoot.Results.Buttons[0];
            resultBack.onClick.AddListener(()=> { NetworkManager.Singleton?.Shutdown(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); });
            if(FindFirstObjectByType<EventSystem>()==null)
            {
                var events=new GameObject("Player UI EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)); events.transform.SetParent(transform,false);
                ownedEvents=events.GetComponent<EventSystem>(); events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            else
            {
                var events=FindFirstObjectByType<EventSystem>();
                if(events.GetComponent<InputSystemUIInputModule>()==null) { var previous=events.GetComponent<StandaloneInputModule>(); if(previous!=null) previous.enabled=false; events.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions(); }
            }
            gameObject.AddComponent<UguiInventoryPresenter>().Initialize(interfaceRoot.Inventory);
            gameObject.AddComponent<UguiExpeditionPresenter>().Initialize(interfaceRoot.Expedition);
            gameObject.AddComponent<UguiCampPresenter>().Initialize(interfaceRoot.Cabin,interfaceRoot.Activities);
            ready=true;
        }'''
source=source[:a]+awake+source[b:]
source=source.replace('public sealed class UguiGameHud','public sealed partial class UguiGameHud')
source=source.replace('using UnityEngine.UI;','using UnityEngine.UI;\nusing Text=TMPro.TextMeshProUGUI;\nusing Font=TMPro.TMP_FontAsset;\nusing Element=CyberExtraction.UI.UguiHudElement;')
source=source.replace('RawImage mapImage,mapPlayer;', 'RawImage mapImage;\n        Image mapPlayer;\n        PlayerInterfaceRoot interfaceRoot;\n        Button toolbarToggle;')
source=source.replace('n.Background.texture=Circle(n.Control) ? HudArt.CanvasCircle(r.width*UiLayout.Scale,n.Control==HudControl.Fire) : HudArt.CanvasPanel(r.size);','n.Background.Circle=Circle(n.Control); n.Background.Prominent=n.Control==HudControl.Fire; n.Background.ApplyTheme();')
source=source.replace('n.InnerRing.texture=HudArt.CanvasCircle(r.width*.42f*UiLayout.Scale);','n.InnerRing.Circle=true; n.InnerRing.ApplyTheme();')
source=source.replace('.Icon.texture=HudArt.IconTexture(','.Icon.sprite=UiSprites.Icon(')
source=source.replace('TextAnchor.MiddleLeft','TMPro.TextAlignmentOptions.MidlineLeft')
source=source.replace('mapPlayer.color=HudTheme.Text','mapPlayer.color=HudTheme.Text')
source=source.replace('marker.GetComponent<RawImage>()','marker.GetComponent<Image>()')
source=source.replace('b.GetComponent<RawImage>().texture=HudArt.CanvasPanel(((RectTransform)b.transform).sizeDelta);','b.GetComponent<HudFrameGraphic>().ApplyTheme();')
source=source.replace('menu.GetComponent<RawImage>().texture=HudArt.CanvasPanel(menu.sizeDelta);','menu.GetComponent<HudFrameGraphic>().ApplyTheme();')
source=source.replace('editor.GetComponent<RawImage>().texture=HudArt.CanvasPanel(editor.sizeDelta);','editor.GetComponent<HudFrameGraphic>().ApplyTheme();')
source=source.replace('button.GetComponent<RawImage>().texture=HudArt.CanvasPanel(((RectTransform)button.transform).sizeDelta);','button.GetComponent<HudFrameGraphic>().ApplyTheme();')
source=source.replace('results.GetComponent<RawImage>().texture=HudArt.CanvasPanel(results.sizeDelta);','results.GetComponent<HudFrameGraphic>().ApplyTheme();')
source=source.replace('resultBack.GetComponent<RawImage>().texture=HudArt.CanvasPanel(((RectTransform)resultBack.transform).sizeDelta);','resultBack.GetComponent<HudFrameGraphic>().ApplyTheme();')
source=source.replace('var toggle=safe.Find("Layout toolbar toggle");','var toggle=toolbarToggle.transform;')
source=source.replace('n.Icon.texture=HudArt.IconTexture(', 'n.Icon.sprite=UiSprites.Icon(')
source=source.replace('bool overlay=ExpeditionMenu.IsOpen || GameOptionsMenu.IsOpen || InventoryWindow.IsOpen || LockpickWindow.IsOpen || TestLabPanel.IsOpen || CampTestPanel.IsOpen;', 'bool overlay=UiScreenCoordinator.Top!=PlayerScreen.Gameplay && UiScreenCoordinator.Top!=PlayerScreen.Organizer;')
source=source.replace('if(n.Control==HudControl.Interaction) show&=edit || interactionAvailable;', 'if(n.Control==HudControl.Interaction) show&=edit || interactionAvailable;\n                if(n.Control==HudControl.Move) show|=!overlay && CampController.Active && CampController.Instance.CanMove;')
source=source.replace('if(UiLayout.BlocksGameplay || player==null || !player.IsAlive) return;', 'if(c==HudControl.Backpack && CampController.Active && !UiLayout.BlocksGameplay) { InventoryWindow.Instance?.ToggleBag(); return; }\n            if(UiLayout.BlocksGameplay || player==null || !player.IsAlive) return;')
for name, methods in [('Geometry',['        static void Place(', '        static bool Circle(', '        void Geometry(', '        static HudSymbol Symbol(']),('Map',['        Rect ExpandedMap(', '        void RefreshMap(', '        static string WindName(']),('Menus',['        void Choice(', '        void RefreshMenu(', '        void RefreshResults(']),('Organizer',['        void RefreshEditor('])]:
    parts=[]
    for signature in methods:
        # expression-bodied helpers end at the first semicolon
        start=source.index(signature); tail=source[start:source.index('\n',start)]
        if '=>' in tail and '{' not in tail:
            end=source.index(';',start)+1
        else:
            start,end=span(source,signature)
        parts.append(source[start:end]); source=source[:start]+source[end:]
    header='using System;\nusing UnityEngine;\nusing UnityEngine.UI;\nusing UnityEngine.EventSystems;\nusing CyberExtraction.Items;\nusing CyberExtraction.Match;\nusing CyberExtraction.Players;\nusing CyberExtraction.Camp;\nusing Text=TMPro.TextMeshProUGUI;\nnamespace CyberExtraction.UI\n{\n    public sealed partial class UguiGameHud\n    {\n'
    (path.parent/f'UguiGameHud.{name}.cs').write_text(header+'\n'.join(parts)+'\n    }\n}\n',encoding='utf-8')
path.write_text(source,encoding='utf-8')
print('Prefab controller bindings and partial presenters written.')
