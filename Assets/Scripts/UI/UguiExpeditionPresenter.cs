using UnityEngine;
using CyberExtraction.Camp;
using CyberExtraction.Match;
using CyberExtraction.Core;

namespace CyberExtraction.UI
{
    public sealed class UguiExpeditionPresenter : MonoBehaviour
    {
        UguiPrefabView view;
        int mode=-1;
        string signature;
        Texture2D forestBackground;
        public void Initialize(UguiPrefabView screen)
        {
            view=screen;
            forestBackground=screen.Backdrop.texture as Texture2D;
            screen.EnvironmentPortraits[0].texture=ExpeditionArtwork.For(ExpeditionMap.Large);
        }
        void Update()
        {
            if(view==null) return;
            bool visible=UiScreenCoordinator.Top==PlayerScreen.Expedition;
            view.gameObject.SetActive(visible); if(!visible) { mode=-1; signature=null; return; }
            var bootstrap=NetworkBootstrap.Instance; if(bootstrap==null) return;
            int next=ExpeditionMenu.ChoosingEnvironment ? 0 : bootstrap.ConnectionBusy ? 1 : bootstrap.ConnectionUnavailable ? 2 : 3;
            float width=UiLayout.Width;
            UguiGeometry.Place(view.Panel,new Rect(0,0,width,600));
            string key=next+"|"+width+"|"+bootstrap.ConnectionMessage+"|"+CampSession.Synchronized+"|"+CampSession.Pending+"|"+HudTheme.Appearance+"|"+bootstrap.IsTestEnvironment+"|"+bootstrap.TestMap+"|"+bootstrap.TestBots;
            if(key==signature) return; signature=key; UguiTheme.Apply(view.gameObject);
            foreach(var b in view.Buttons) { b.gameObject.SetActive(false); b.onClick.RemoveAllListeners(); }
            bool approved=next==0;
            view.EnvironmentArtwork.gameObject.SetActive(approved);
            view.Backdrop.gameObject.SetActive(!approved); view.Surface.gameObject.SetActive(!approved);
            view.EnvironmentSubtitle.gameObject.SetActive(approved);
            var panelFrame=view.Panel.GetComponent<HudFrameGraphic>();
            if(approved)
            {
                panelFrame.sprite=null; panelFrame.type=UnityEngine.UI.Image.Type.Simple;
                panelFrame.color=Color.black; panelFrame.raycastTarget=false;
            }
            else panelFrame.raycastTarget=true;
            view.Heading.gameObject.SetActive(true); view.Detail.gameObject.SetActive(true);
            view.Viewport.parent.gameObject.SetActive(!approved);
            for(int i=0;i<view.Buttons.Length;i++)
            {
                var button=view.Buttons[i];
                button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).gameObject.SetActive(true);
                button.transition=UnityEngine.UI.Selectable.Transition.ColorTint;
                var graphic=button.GetComponent<HudFrameGraphic>();
                graphic.EntryStyle=approved; graphic.Primary=approved && i==0;
                graphic.color=Color.white; graphic.ApplyTheme();
                button.colors=UnityEngine.UI.ColorBlock.defaultColorBlock;
                button.navigation=new UnityEngine.UI.Navigation { mode=UnityEngine.UI.Navigation.Mode.Automatic };
                button.targetGraphic.canvasRenderer.SetColor(Color.white);
            }
            if(approved)
            {
                view.Portrait.gameObject.SetActive(false);
                ShowEntrySelection(bootstrap);
                mode=next;
                return;
            }
            view.Portrait.gameObject.SetActive(false);
            view.Backdrop.texture=ExpeditionArtwork.For(bootstrap.TestMap) ?? forestBackground;
            UguiMenuStyle.Backdrop(view,new Rect(0,0,width,600));
            view.Heading.text=MapBuilder.MapName(bootstrap.TestMap).ToUpperInvariant();
            view.Heading.characterSpacing=0;
            view.Heading.fontSize=bootstrap.TestMap==ExpeditionMap.Dungeon ? 25 : 36;
            view.Heading.alignment=next==0 ? TMPro.TextAlignmentOptions.Center : TMPro.TextAlignmentOptions.MidlineLeft;
            view.Body.fontSize=21; view.Body.richText=next==3;
            view.Body.font=UiFontAssets.Regular;
            view.Body.alignment=next==0 ? TMPro.TextAlignmentOptions.Top : TMPro.TextAlignmentOptions.TopLeft;
            view.Detail.fontSize=15; view.Detail.color=HudTheme.Muted;
            if(view.EnvironmentCards!=null)
                for(int i=0;i<view.EnvironmentCards.Length;i++)
                {
                    var card=view.EnvironmentCards[i]; card.gameObject.SetActive(next==0);
                    float cardWidth=(width-72)/2;
                    UguiGeometry.Place(card,new Rect(24+i*(cardWidth+24),130,cardWidth,250));
                    var labels=card.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                    UguiGeometry.Place(labels[0].rectTransform,new Rect(18,22,cardWidth-36,42));
                    UguiGeometry.Place(labels[1].rectTransform,new Rect(18,78,cardWidth-36,82));
                    labels[0].font=UiFontAssets.Bold; labels[1].font=UiFontAssets.Regular;
                }
            float panelWidth=Mathf.Min(490,width*.48f),x=width-panelWidth-28,w=panelWidth-48;
            UguiGeometry.Place(view.Surface.rectTransform,new Rect(x-24,42,panelWidth,514));
            UguiGeometry.Place(view.Heading.rectTransform,new Rect(x,64,w,66));
            UguiGeometry.Place((RectTransform)view.Viewport.parent,new Rect(x,146,w,154));
            UguiGeometry.Place(view.Viewport,new Rect(0,0,w,154));
            UguiGeometry.Place(view.Content,new Rect(0,0,w,0));
            view.Detail.alignment=TMPro.TextAlignmentOptions.Center;
            UguiGeometry.Place(view.Detail.rectTransform,new Rect(x,506,w,36));
            void Button(int i,string label,System.Action action,int row,bool enabled=true)
            {
                var button=view.Buttons[i]; button.gameObject.SetActive(true); button.interactable=enabled;
                button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).text=label;
                button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).fontSize=18;
                UguiGeometry.Place((RectTransform)button.transform,new Rect(x,328+row*58,w,48));
                UguiMenuStyle.Button(button); button.onClick.AddListener(()=>action());
            }
            if(next==0)
            {
                view.Body.text="O progresso da base e os recursos de teste ficam separados.";
                Button(0,"Entrar na base",()=>bootstrap.ChooseEnvironment(CampEnvironment.Normal),0);
                Button(1,"Entrar para testar",()=>bootstrap.ChooseEnvironment(CampEnvironment.Test),1,GameConfig.AllowQuickTest);
                float cardWidth=(width-72)/2;
                for(int i=0;i<2;i++) UguiGeometry.Place((RectTransform)view.Buttons[i].transform,new Rect(42+i*(cardWidth+24),312,cardWidth-36,50));
            }
            else if(next==1)
            { view.Body.text=bootstrap.Searching ? "Procurando uma sala disponível..." : "Preparando conexão..."; Button(0,"Cancelar",bootstrap.CancelConnectionSearch,0,bootstrap.Searching); }
            else if(next==2)
            { view.Body.text=bootstrap.ConnectionMessage; Button(0,"Tentar novamente",bootstrap.RetryConnection,0); Button(1,"Voltar",ExpeditionMenu.Return,1); }
            else
            {
                view.Body.text=bootstrap.IsTestEnvironment && bootstrap.TestMap==ExpeditionMap.Test
                    ? "Experimente os controles e o combate na área de testes.\n\n<color=#B0A88A>Recursos temporários. Seu progresso real fica preservado.</color>"
                    : "Explore a floresta e alcance uma extração.\n\n<color=#AE5941>Se morrer, perde os itens da mochila.</color>";
                if(bootstrap.TestMap==ExpeditionMap.Dungeon) view.Body.text="Catacumbas dos Afogados\n8 participantes · 10 minutos · PvPvE\n\nExplore, saqueie e prepare uma fuga pela escadaria, pelo poço ou pelo canal. Arte provisória.";
                bool ready=CampSession.Synchronized && !CampSession.Pending;
                Button(0,"Preparar mochila",ExpeditionMenu.PrepareInventory,0,ready); Button(1,"Partir",bootstrap.BeginExpedition,1,ready); Button(2,"Voltar à base",ExpeditionMenu.Return,2);
                Button(3,"Mapa: "+MapBuilder.MapName(bootstrap.TestMap),()=>bootstrap.ConfigureExpedition(bootstrap.IsTestEnvironment ? (ExpeditionMap)(((int)bootstrap.TestMap+1)%3) : bootstrap.TestMap==ExpeditionMap.Large ? ExpeditionMap.Dungeon : ExpeditionMap.Large,bootstrap.TestBots),3,ready);
                if(bootstrap.IsTestEnvironment) Button(4,"Bots: "+bootstrap.TestBots,()=>bootstrap.ConfigureExpedition(bootstrap.TestMap,(bootstrap.TestBots+1)%MapBuilder.CapacityFor(bootstrap.TestMap)),4,ready);
                float actionsY=412;
                UguiGeometry.Place((RectTransform)view.Buttons[3].transform,new Rect(x,316,bootstrap.IsTestEnvironment ? w*.65f-6 : w,42));
                if(bootstrap.IsTestEnvironment) UguiGeometry.Place((RectTransform)view.Buttons[4].transform,new Rect(x+w*.65f,316,w*.35f,42));
                float buttonWidth=(w-12)/2;
                UguiGeometry.Place((RectTransform)view.Buttons[0].transform,new Rect(x,actionsY,buttonWidth,48));
                UguiGeometry.Place((RectTransform)view.Buttons[1].transform,new Rect(x+buttonWidth+12,actionsY,buttonWidth,48));
                UguiGeometry.Place((RectTransform)view.Buttons[2].transform,new Rect(x,actionsY+56,w,42));
                UguiMenuStyle.Button(view.Buttons[1],true);
            }
            view.Detail.text=next==0 ? "Recursos e progresso real não são misturados." : bootstrap.ConnectionMessage;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(view.Content);
            view.Viewport.parent.GetComponent<UnityEngine.UI.ScrollRect>().verticalNormalizedPosition=1;
            if(mode!=next && UnityEngine.EventSystems.EventSystem.current!=null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(view.Buttons[0].gameObject);
            mode=next;
        }
        void ShowEntrySelection(NetworkBootstrap bootstrap)
        {
            // Reference composition is expressed in design units; every live
            // element shares the same uniform scale and safe-area origin.
            const float sourceWidth=1672,sourceHeight=941;
            float scale=Mathf.Min(UiLayout.Width/sourceWidth,600/sourceHeight);
            var origin=new Vector2((UiLayout.Width-sourceWidth*scale)/2,(600-sourceHeight*scale)/2);
            void Place(RectTransform rect,Rect design)=>UguiGeometry.Place(rect,new Rect(origin+design.position*scale,design.size*scale));
            UguiGeometry.Place(view.EnvironmentArtwork.rectTransform,new Rect(0,0,UiLayout.Width,600));
            view.EnvironmentArtwork.color=Color.white;
            Crop(view.EnvironmentArtwork,UiLayout.Width/600);
            view.Heading.text="ESCOLHA SEU AMBIENTE"; view.Heading.font=UiFontAssets.Bold;
            view.Heading.fontSize=68*scale; view.Heading.alignment=TMPro.TextAlignmentOptions.Center;
            view.Heading.characterSpacing=1.2f;
            Place(view.Heading.rectTransform,new Rect(395,67,882,95));
            view.EnvironmentSubtitle.text="Escolha onde começar"; view.EnvironmentSubtitle.font=UiFontAssets.Regular;
            view.EnvironmentSubtitle.fontSize=32*scale; view.EnvironmentSubtitle.alignment=TMPro.TextAlignmentOptions.Center;
            view.EnvironmentSubtitle.color=HudTheme.Muted;
            Place(view.EnvironmentSubtitle.rectTransform,new Rect(450,160,772,52));
            view.Detail.text="Recursos e progresso não são misturados."; view.Detail.font=UiFontAssets.Regular;
            view.Detail.fontSize=22*scale; view.Detail.alignment=TMPro.TextAlignmentOptions.Center;
            view.Detail.color=HudTheme.Muted;
            Place(view.Detail.rectTransform,new Rect(430,844,812,36));
            var cards=new[] {new Rect(353,255,476,529),new Rect(864,266,455,511)};
            var targets=new[] {new Rect(390,679,404,78),new Rect(900,679,384,78)};
            for(int i=0;i<2;i++)
            {
                int index=i;
                var card=view.EnvironmentCards[i]; card.gameObject.SetActive(true); Place(card,cards[i]);
                var title=view.EnvironmentTitles[i]; var description=view.EnvironmentDescriptions[i];
                var picture=view.EnvironmentPortraits[i]; picture.gameObject.SetActive(true);
                UguiGeometry.Place(picture.rectTransform,new Rect(22*scale,22*scale,(cards[i].width-44)*scale,220*scale));
                Crop(picture,(cards[i].width-44)/220);
                UguiGeometry.Place(title.rectTransform,new Rect(18*scale,256*scale,(cards[i].width-36)*scale,52*scale));
                UguiGeometry.Place(description.rectTransform,new Rect(30*scale,330*scale,(cards[i].width-60)*scale,76*scale));
                title.font=UiFontAssets.Bold; title.fontSize=34*scale; title.alignment=TMPro.TextAlignmentOptions.Center;
                description.font=UiFontAssets.Regular; description.fontSize=23*scale; description.alignment=TMPro.TextAlignmentOptions.Center;
                title.text=i==0 ? "BASE NORMAL" : "ACAMPAMENTO DE TESTE";
                description.text=i==0 ? "Sua mochila, seu baú e a Floresta Velha.\nProgresso real." : "Recursos temporários e laboratório.\nSeu progresso real fica preservado.";
                var button=view.Buttons[i]; button.gameObject.SetActive(true);
                button.interactable=i==0 || GameConfig.AllowQuickTest;
                Place((RectTransform)button.transform,targets[i]);
                var caption=button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                caption.text=i==0 ? "Entrar na base" : "Entrar para testar";
                caption.font=UiFontAssets.Regular; caption.fontSize=34*scale;
                caption.color=button.interactable ? HudTheme.Text : HudTheme.Muted;
                var colors=button.colors;
                colors.normalColor=new Color(.86f,.86f,.86f,1);
                colors.highlightedColor=Color.white;
                colors.selectedColor=Color.white;
                colors.pressedColor=new Color(.68f,.65f,.60f,1);
                colors.disabledColor=new Color(.38f,.38f,.38f,.8f);
                colors.fadeDuration=.09f; button.colors=colors;
                button.navigation=new UnityEngine.UI.Navigation {mode=UnityEngine.UI.Navigation.Mode.Explicit,
                    selectOnLeft=i==1 ? view.Buttons[0] : null, selectOnRight=i==0 ? view.Buttons[1] : null};
                button.onClick.AddListener(()=>bootstrap.ChooseEnvironment(index==0 ? CampEnvironment.Normal : CampEnvironment.Test));
            }
            if(mode!=0 && UnityEngine.EventSystems.EventSystem.current!=null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(view.Buttons[0].gameObject);
        }
        static void Crop(UnityEngine.UI.RawImage image,float displayAspect)
        {
            if(image.texture==null) return;
            float aspect=(float)image.texture.width/image.texture.height;
            image.uvRect=aspect>displayAspect ? new Rect((1-displayAspect/aspect)/2,0,displayAspect/aspect,1)
                : new Rect(0,(1-aspect/displayAspect)/2,1,aspect/displayAspect);
        }
    }
}
