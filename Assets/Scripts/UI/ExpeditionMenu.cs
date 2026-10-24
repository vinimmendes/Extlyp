using UnityEngine;
using Unity.Netcode;
using CyberExtraction.Camp;
using CyberExtraction.Core;
using CyberExtraction.Match;
using CyberExtraction.Players;

namespace CyberExtraction.UI
{
    [DefaultExecutionOrder(-120)]
    public sealed class ExpeditionMenu : MonoBehaviour
    {
        static ExpeditionMenu instance;
        bool returnFromBag;
        int focus, submittedFrame=-1;
        float nextNavigation;
        Vector2 messageScroll;
        string lastMessage;
        NetworkBootstrap Bootstrap=>NetworkBootstrap.Instance;
        static readonly ExpeditionMap[] mapOrder={ExpeditionMap.Dungeon,ExpeditionMap.Large,ExpeditionMap.Test};
        public static ExpeditionMap NextMap(ExpeditionMap current,bool testing,int direction=1)
        {
            int count=testing ? mapOrder.Length : 2;
            int index=System.Array.IndexOf(mapOrder,current);
            if(index<0 || index>=count) return mapOrder[0];
            return mapOrder[(index+direction+count)%count];
        }
        public static bool ChoosingEnvironment => NetworkBootstrap.Instance!=null && NetworkBootstrap.Instance.ReadyForMenus && !NetworkBootstrap.Instance.CheckingTestAccess && !ClientEnvironment.Chosen;
        public static bool IsOpen => instance!=null && instance.Visible;
        bool Visible
        {
            get
            {
                var bootstrap=Bootstrap;
                if(bootstrap==null || !bootstrap.ReadyForMenus || Application.isBatchMode || HudLayoutEditor.IsOpen || GameOptionsMenu.IsOpen || InventoryWindow.IsOpen) return false;
                if(!ClientEnvironment.Chosen) return true;
                if(UguiBaseHub.Active) return false;
                return bootstrap.ConnectionBusy || bootstrap.ConnectionUnavailable || bootstrap.CampConnection && !CampSession.Synchronized ||
                    CampController.Active && CampController.Instance.ExpeditionPanelOpen;
            }
        }
        void Awake() { instance=this; }
        void OnDestroy() { if(instance==this) instance=null; }
        void Update()
        {
            if(returnFromBag && !InventoryWindow.IsOpen)
            {
                returnFromBag=false;
                if(CampController.Active) CampController.Instance.ExpeditionPanelOpen=true;
                focus=0;
            }
            if(!Visible) return;
            if(UguiGameHud.Active) return;
            if(GameInput.BackPressed && !ChoosingEnvironment)
            {
                if(Bootstrap.ConnectionBusy) Bootstrap.CancelConnectionSearch();
                else Back();
            }
            int count=ChoosingEnvironment ? 2 : Bootstrap.ConnectionBusy ? 1 : Bootstrap.ConnectionUnavailable ? 2 : Bootstrap.IsTestEnvironment ? 5 : 4;
            Vector2 navigation=GameInput.UiNavigate;
            if(navigation.sqrMagnitude<.25f) nextNavigation=0;
            else if(Time.unscaledTime>=nextNavigation)
            {
                if(Bootstrap.InTestCamp && !ChoosingEnvironment && !Bootstrap.ConnectionBusy && !Bootstrap.ConnectionUnavailable && focus<2 && Mathf.Abs(navigation.x)>.5f)
                {
                    int direction=navigation.x>0 ? 1 : -1;
                    Bootstrap.ConfigureTestExpedition(focus==0 ? NextMap(Bootstrap.TestMap,true,direction) : Bootstrap.TestMap,
                        focus==1 ? Mathf.Clamp(Bootstrap.TestBots+direction,0,MapBuilder.CapacityFor(Bootstrap.TestMap)-1) : Bootstrap.TestBots);
                }
                else focus=(focus+(navigation.y>0 || navigation.x<0 ? -1 : 1)+count)%count;
                nextNavigation=Time.unscaledTime+.2f;
            }
            focus=Mathf.Clamp(focus,0,count-1);
        }
        bool Button(Rect rect,string text,int index,bool enabled=true)
        {
            bool before=GUI.enabled; GUI.enabled=before && enabled;
            GUI.SetNextControlName("ExpeditionAction"+index);
            if(index==focus && enabled) GUI.FocusControl("ExpeditionAction"+index);
            bool clicked=GUI.Button(rect,text);
            if(enabled && index==focus && GameInput.UiSubmitPressed && submittedFrame!=Time.frameCount)
            { clicked=true; submittedFrame=Time.frameCount; }
            GUI.enabled=before;
            if(clicked) focus=index;
            return clicked;
        }
        void Back()
        {
            if(Bootstrap.ConnectionUnavailable || !CampSession.Synchronized) Bootstrap.ReturnToEnvironmentSelection();
            else if(CampController.Instance!=null) CampController.Instance.ExpeditionPanelOpen=false;
        }
        public static void Return() { instance?.Back(); }
        public static void PrepareInventory() { instance?.PrepareBag(); }
        void PrepareBag()
        {
            CampController.Instance.ExpeditionPanelOpen=false;
            InventoryWindow.Instance.OpenBag();
            returnFromBag=true;
        }
        void DrawMessage(Rect rect,string text)
        {
            if(text!=lastMessage) { lastMessage=text; messageScroll=Vector2.zero; }
            if(string.IsNullOrEmpty(text)) return;
            var style=HudTheme.Label(16);
            float width=rect.width-20,height=Mathf.Max(rect.height,style.CalcHeight(new GUIContent(text),width)+4);
            messageScroll=GUI.BeginScrollView(rect,messageScroll,new Rect(0,0,width,height));
            GUI.Label(new Rect(0,0,width,height),text,style); GUI.EndScrollView();
        }
        void OnGUI()
        {
            if(UguiGameHud.Active) return;
            if(!Visible) return;
            using var frame=UiLayout.Begin();
            HudTheme.Fill(new Rect(0,0,UiLayout.Width,UiLayout.Height),new Color(0,0,0,.7f));
            float width=Mathf.Min(960,UiLayout.Width-48);
            var panel=new Rect((UiLayout.Width-width)/2,70,width,460); GUI.Box(panel,"");
            if(ChoosingEnvironment)
            {
                GUI.Label(new Rect(panel.x+24,panel.y+24,width-48,44),"ESCOLHA SEU AMBIENTE",HudTheme.Label(27,TextAnchor.MiddleCenter));
                GUI.Label(new Rect(panel.x+24,panel.y+76,width-48,38),"O progresso da base e os recursos de teste ficam separados.",HudTheme.Label(17,TextAnchor.MiddleCenter));
                float cardWidth=(width-72)/2;
                var normal=new Rect(panel.x+24,panel.y+130,cardWidth,250);
                var test=new Rect(normal.xMax+24,normal.y,cardWidth,250);
                GUI.Box(normal,""); GUI.Box(test,"");
                GUI.Label(new Rect(normal.x+18,normal.y+22,cardWidth-36,42),"BASE NORMAL",HudTheme.Label(24,TextAnchor.MiddleCenter));
                GUI.Label(new Rect(normal.x+18,normal.y+78,cardWidth-36,82),"Sua mochila, seu baú e as Catacumbas dos Afogados. Progresso real.",HudTheme.Label(18));
                if(Button(new Rect(normal.x+18,normal.yMax-68,cardWidth-36,50),"Entrar na base",0)) Bootstrap.ChooseEnvironment(CampEnvironment.Normal);
                GUI.Label(new Rect(test.x+18,test.y+22,cardWidth-36,42),"ACAMPAMENTO DE TESTE",HudTheme.Label(24,TextAnchor.MiddleCenter));
                GUI.Label(new Rect(test.x+18,test.y+78,cardWidth-36,82),"Recursos temporários, mapas de teste e laboratório. Sem alterar seu progresso real.",HudTheme.Label(18));
                if(Button(new Rect(test.x+18,test.yMax-68,cardWidth-36,50),"Entrar para testar",1,Bootstrap.CanEnterTests)) Bootstrap.ChooseEnvironment(CampEnvironment.Test);
                return;
            }
            float leftWidth=(width-72)*.43f;
            var image=new Rect(panel.x+24,panel.y+24,leftWidth,340);
            GUI.Box(image,"");
            var artwork=ExpeditionArtwork.For(Bootstrap.TestMap);
            if(artwork!=null) GUI.DrawTexture(new Rect(image.x+5,image.y+5,image.width-10,image.height-10),artwork,ScaleMode.ScaleAndCrop,true);
            GUI.Label(new Rect(image.x,image.yMax+18,leftWidth,44),Bootstrap.TestEnvironmentSelected ? "EXPEDIÇÃO DE TESTE" : "EXPEDIÇÃO",HudTheme.Label(19,TextAnchor.MiddleCenter));
            float x=image.xMax+24,w=panel.xMax-24-x;
            GUI.Label(new Rect(x,panel.y+24,w,44),MapBuilder.MapName(Bootstrap.TestMap).ToUpperInvariant(),HudTheme.Label(Bootstrap.TestMap==ExpeditionMap.Dungeon ? 22 : 30));
            if(Bootstrap.ConnectionBusy)
            {
                GUI.Label(new Rect(x,panel.y+96,w,95),Bootstrap.CheckingTestAccess ? "Verificando acesso ao servidor..." : Bootstrap.Searching ? "Procurando uma sala disponível..." : "Preparando conexão...",HudTheme.Label(22));
                if(Button(new Rect(x,panel.y+220,w,52),"Cancelar",0,Bootstrap.Searching)) Bootstrap.CancelConnectionSearch();
                return;
            }
            if(Bootstrap.ConnectionUnavailable)
            {
                DrawMessage(new Rect(x,panel.y+90,w,140),Bootstrap.ConnectionMessage);
                if(Button(new Rect(x,panel.y+252,w,52),"Tentar novamente",0)) Bootstrap.RetryConnection();
                if(Button(new Rect(x,panel.y+316,w,48),"Voltar",1)) Back();
                return;
            }
            GUI.Label(new Rect(x,panel.y+78,w,66),Bootstrap.TestMap==ExpeditionMap.Dungeon ? "Explore as catacumbas, encontre suprimentos e prepare uma das três fugas." : "Explore a floresta e suas ruínas. Encontre suprimentos e alcance uma extração.",HudTheme.Label(19));
            GUI.Label(new Rect(x,panel.y+149,w,56),"Você leva sua mochila. Se morrer, perde os itens carregados.",HudTheme.Label(17));
            bool testing=Bootstrap.IsTestEnvironment;
            float y=panel.y+222;
            if(testing)
            {
                GUI.SetNextControlName("ExpeditionAction0"); if(focus==0) GUI.FocusControl("ExpeditionAction0");
                int map=GUI.SelectionGrid(new Rect(x,y,w,36),System.Array.IndexOf(mapOrder,Bootstrap.TestMap),new[]{"Catacumbas","Floresta Velha","Área de testes"},3);
                GUI.SetNextControlName("ExpeditionAction1"); if(focus==1) GUI.FocusControl("ExpeditionAction1");
                var labels=new string[MapBuilder.CapacityFor(Bootstrap.TestMap)];
                for(int i=0;i<labels.Length;i++) labels[i]=i==0 ? "0 bots" : i.ToString();
                int bots=GUI.SelectionGrid(new Rect(x,y+42,w,32),Bootstrap.TestBots,labels,labels.Length);
                Bootstrap.ConfigureTestExpedition(mapOrder[map],bots); y+=84;
            }
            if(!testing)
            {
                if(Button(new Rect(x,y,w,36),"Mapa: "+MapBuilder.MapName(Bootstrap.TestMap),3)) Bootstrap.ConfigureExpedition(Bootstrap.TestMap==ExpeditionMap.Large ? ExpeditionMap.Dungeon : ExpeditionMap.Large,0);
                y+=44;
            }
            bool ready=CampSession.Synchronized && !CampSession.Pending;
            float buttonWidth=(w-12)/2;
            int first=testing ? 2 : 0;
            if(Button(new Rect(x,y,buttonWidth,48),"Preparar mochila",first,ready)) PrepareBag();
            if(Button(new Rect(x+buttonWidth+12,y,buttonWidth,48),"Partir",first+1,ready)) Bootstrap.BeginExpedition();
            if(Button(new Rect(x,y+56,w,42),"Voltar à base",first+2)) Back();
            string status=ready ? Bootstrap.ConnectionMessage : "Aguarde a sincronização do acampamento.";
            if(ready && string.IsNullOrEmpty(status)) status="Conectado ao servidor. Pronto para partir.";
            DrawMessage(new Rect(x,panel.yMax-48,w,36),status);
        }
    }
}
