using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using CyberExtraction.Camp;
using CyberExtraction.Players;
using CyberExtraction.Match;

namespace CyberExtraction.UI
{
    [DefaultExecutionOrder(-150)]
    public class GameOptionsMenu : MonoBehaviour
    {
        static GameOptionsMenu instance;
        bool open, options, help, confirmExit;
        int focus, handledBack=-1, handledSubmit=-1;
        float nextNavigation;
        public static bool IsOpen=>instance!=null && instance.open;
        public static bool ConsumedBack=>instance!=null && instance.handledBack==Time.frameCount;
        public static Rect ButtonBounds=>HudLayout.Menu;
        void Awake()
        {
            instance=this;
            if(Application.isMobilePlatform)
            {
                Screen.autorotateToPortrait=Screen.autorotateToPortraitUpsideDown=false;
                Screen.autorotateToLandscapeLeft=Screen.autorotateToLandscapeRight=true;
                Screen.orientation=ScreenOrientation.AutoRotation;
            }
        }
        void OnDestroy() { if(instance==this) instance=null; }
        public static void ShowOptions() { if(instance!=null) { instance.Open(); instance.options=true; } }
        void Open() { InventoryWindow.Instance?.Close(); open=true; options=help=confirmExit=false; focus=0; }
        void Close() { open=options=help=confirmExit=false; }
        void Update()
        {
            if(ExpeditionMenu.ChoosingEnvironment || ExpeditionMenu.IsOpen) return;
            if(Application.isBatchMode) return;
            if(GameInput.BackPressed)
            {
                if(open) { if(options || help || confirmExit) options=help=confirmExit=false; else Close(); handledBack=Time.frameCount; }
                else if(!InventoryWindow.IsOpen && !TestLabPanel.IsOpen && !CampTestPanel.IsOpen && !LockpickWindow.IsOpen &&
                    !(CampController.Active && (CampController.Instance.HasActivity || CampController.Instance.ExpeditionPanelOpen)))
                { Open(); handledBack=Time.frameCount; }
            }
        }
        bool Button(Rect rect,string text,int index)
        {
            GUI.SetNextControlName("GameMenu"+index);
            if(index==focus) GUI.FocusControl("GameMenu"+index);
            if(GUI.Button(rect,text)) { focus=index; return true; }
            bool pressed=index==focus && GameInput.UiSubmitPressed && handledSubmit!=Time.frameCount;
            if(pressed) handledSubmit=Time.frameCount;
            return pressed;
        }
        void OnGUI()
        {
            if(ExpeditionMenu.ChoosingEnvironment || ExpeditionMenu.IsOpen) return;
            if(Application.isBatchMode) return;
            var nm=NetworkManager.Singleton; if(nm!=null && nm.IsServer && !nm.IsClient) return;
            using var frame=UiLayout.Begin();
            bool overlay=InventoryWindow.IsOpen || TestLabPanel.IsOpen || CampTestPanel.IsOpen || LockpickWindow.IsOpen ||
                (CampController.Active && (CampController.Instance.HasActivity || CampController.Instance.ExpeditionPanelOpen));
            if(!open) { if(!overlay && GUI.Button(ButtonBounds,"Menu")) Open(); return; }
            float width=Mathf.Min(470,UiLayout.Width-32);
            var panel=new Rect((UiLayout.Width-width)/2,70,width,460);
            GUI.Box(panel,""); var title=HudTheme.Label(25,TextAnchor.MiddleCenter);
            GUI.Label(new Rect(panel.x+16,panel.y+16,width-32,40),options ? "OPCOES · HUD" : help ? "CONTROLES" : confirmExit ? "SAIR DA PARTIDA?" : "MENU",title);
            int count=options ? 3 : help ? 1 : confirmExit ? 2 : 4+(TestLabPanel.Available ? 1 : 0)+(CampTestPanel.Available ? 1 : 0);
            var direction=GameInput.UiNavigate;
            if(direction.sqrMagnitude<.25f) nextNavigation=0;
            else if(Event.current.type==EventType.Repaint && Time.unscaledTime>=nextNavigation) { focus=(focus+(direction.y>0 || direction.x<0 ? -1 : 1)+count)%count; nextNavigation=Time.unscaledTime+.2f; }
            if(focus>=count) focus=0;
            float x=panel.x+20,w=width-40,y=panel.y+70;
            if(options)
            {
                GUI.Label(new Rect(x,y,w,36),"Escolha o visual. A preferencia fica salva neste aparelho.",HudTheme.Label(17)); y+=48;
                if(Button(new Rect(x,y,w,58),(HudTheme.Appearance==HudAppearance.Simple ? "✓ " : "")+"A · SIMPLES\nPaineis limpos e destaque verde",0)) HudTheme.Select(HudAppearance.Simple); y+=72;
                if(Button(new Rect(x,y,w,58),(HudTheme.Somber ? "✓ " : "")+"B · SOMBRIA\nVerde escuro, marfim e cobre envelhecido",1)) HudTheme.Select(HudAppearance.Somber);
                GUI.Label(new Rect(x,y+78,w,60),"Celular: somente na horizontal.\nVida, botoes e regras iguais nos dois temas.",HudTheme.Label(17));
                if(Button(new Rect(x,panel.yMax-65,w,46),"Voltar",2)) options=false;
            }
            else if(help)
            {
                GUI.Label(new Rect(x,y,w,265),GameHelp.Controls, HudTheme.Label(18));
                if(Button(new Rect(x,panel.yMax-65,w,46),"Voltar",0)) help=false;
            }
            else if(confirmExit)
            {
                GUI.Label(new Rect(x,y,w,110),CampController.Active ? "Voltar à tela inicial? Seu progresso permanece salvo." :
                    NetworkBootstrap.Instance!=null && NetworkBootstrap.Instance.IsTestEnvironment ? "Voltar à tela inicial? Os itens desta expedição são temporários." :
                    "Sair durante a expedição perde os itens da mochila.\nO baú seguro permanece na cabana.",HudTheme.Label(20));
                if(Button(new Rect(x,y+130,w,48),"Continuar jogando",0)) confirmExit=false;
                if(Button(new Rect(x,y+190,w,48),"Confirmar saida",1)) Leave();
            }
            else
            {
                int index=0;
                if(Button(new Rect(x,y,w,42),"Continuar",index++)) Close(); y+=50;
                if(Button(new Rect(x,y,w,42),"Opcoes · tema da HUD",index++)) { options=true; focus=0; } y+=50;
                if(Button(new Rect(x,y,w,42),"Ajuda e controles",index++)) { help=true; focus=0; } y+=50;
                if(TestLabPanel.Available) { if(Button(new Rect(x,y,w,42),"Laboratorio [F2]",index++)) { Close(); TestLabPanel.Show(); } y+=50; }
                if(CampTestPanel.Available) { if(Button(new Rect(x,y,w,42),"Ferramentas de teste",index++)) { Close(); CampTestPanel.Show(); } y+=50; }
                if(Button(new Rect(x,y,w,42),"Sair para a tela inicial",index)) { confirmExit=true; focus=0; }
                GUI.Label(new Rect(x,panel.yMax-55,w,38),"Partidas online continuam com o menu aberto.",HudTheme.Label(15,TextAnchor.MiddleCenter));
            }
        }
        static void Leave()
        {
            ClientEnvironment.ReturnToStart();
            PlayerPrefs.DeleteKey("connection_message");
            NetworkManager.Singleton?.Shutdown(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
