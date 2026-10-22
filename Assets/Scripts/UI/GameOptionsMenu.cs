using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using CyberExtraction.Camp;
using CyberExtraction.Players;
using CyberExtraction.Match;
using CyberExtraction.Core;

namespace CyberExtraction.UI
{
    [DefaultExecutionOrder(-150)]
    public class GameOptionsMenu : MonoBehaviour
    {
        static GameOptionsMenu instance;
        bool open, options, help, confirmExit;
        enum OptionCategory { None, Gameplay, Graphics }
        OptionCategory optionCategory;
        int focus, handledBack=-1, handledSubmit=-1;
        float nextNavigation;
        public static bool IsOpen=>instance!=null && instance.open;
        public static int MenuScreen=>!IsOpen ? 0 : instance.options ?
            (instance.optionCategory==OptionCategory.Gameplay ? 5 : instance.optionCategory==OptionCategory.Graphics ? 6 : 2) : instance.help ? 3 : instance.confirmExit ? 4 : 1;
        public static void CloseMenu() { instance?.Close(); }
        public static void ShowHelp() { if(instance!=null) { instance.open=true; instance.options=instance.confirmExit=false; instance.help=true; } }
        public static void AskLeave() { if(instance!=null) { instance.open=true; instance.options=instance.help=false; instance.confirmExit=true; } }
        public static void BackToMenu() { if(instance!=null) instance.options=instance.help=instance.confirmExit=false; }
        public static void ConfirmLeave()=>Leave();
        public static void ShowGameplayOptions()=>SelectCategory(OptionCategory.Gameplay);
        public static void ShowGraphicsOptions()=>SelectCategory(OptionCategory.Graphics);
        public static void BackToOptions()=>SelectCategory(OptionCategory.None);
        static void SelectCategory(OptionCategory category)
        {
            if(instance==null) return;
            instance.open=instance.options=true; instance.help=instance.confirmExit=false;
            instance.optionCategory=category; instance.focus=0;
        }
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
        public static void ShowMenu() { if(instance!=null) instance.Open(); }
        void Open() { if(!UguiBaseHub.Active) InventoryWindow.Instance?.Close(); open=true; options=help=confirmExit=false; optionCategory=OptionCategory.None; focus=0; }
        void Close() { open=options=help=confirmExit=false; }
        void Update()
        {
            if(HudLayoutEditor.IsOpen || HudLayoutEditor.ConsumedBack) return;
            if(Minimap.ConsumedBack) return;
            if(ExpeditionMenu.ChoosingEnvironment || ExpeditionMenu.IsOpen) return;
            if(Application.isBatchMode) return;
            if(UguiBaseHub.Active && !open) return;
            if(GameInput.BackPressed)
            {
                if(open)
                {
                    if(options && optionCategory!=OptionCategory.None) BackToOptions();
                    else if(options || help || confirmExit) { options=help=confirmExit=false; focus=0; }
                    else Close();
                    handledBack=Time.frameCount;
                }
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
            if(UguiGameHud.Active) return;
            if(HudLayoutEditor.IsOpen) return;
            if(ExpeditionMenu.ChoosingEnvironment || ExpeditionMenu.IsOpen) return;
            if(Application.isBatchMode) return;
            var nm=NetworkManager.Singleton; if(nm!=null && nm.IsServer && !nm.IsClient) return;
            using var frame=UiLayout.Begin();
            bool overlay=InventoryWindow.IsOpen || TestLabPanel.IsOpen || CampTestPanel.IsOpen || LockpickWindow.IsOpen ||
                (CampController.Active && (CampController.Instance.HasActivity || CampController.Instance.ExpeditionPanelOpen));
            if(!open) { if(!UguiGameHud.Active && !overlay && HudArt.Button(ButtonBounds,HudSymbol.Menu,"Menu",HudLayout.HitBounds(HudControl.Menu))) Open(); return; }
            float width=Mathf.Min(470,UiLayout.Width-32);
            float height=options ? (optionCategory==OptionCategory.Gameplay ? 520 : optionCategory==OptionCategory.Graphics ? 446 : 340) : 460;
            var panel=new Rect((UiLayout.Width-width)/2,(UiLayout.Height-height)/2,width,height);
            GUI.Box(panel,""); var title=HudTheme.Label(25,TextAnchor.MiddleCenter);
            string optionsTitle=optionCategory==OptionCategory.Gameplay ? "OPÇÕES · JOGABILIDADE" : optionCategory==OptionCategory.Graphics ? "OPÇÕES · GRÁFICOS" : "OPÇÕES";
            GUI.Label(new Rect(panel.x+16,panel.y+16,width-32,40),options ? optionsTitle : help ? "CONTROLES" : confirmExit ? "SAIR DA PARTIDA?" : "MENU",title);
            int count=options ? (optionCategory==OptionCategory.Gameplay ? 7 : optionCategory==OptionCategory.Graphics ? 4 : 3) : help ? 1 : confirmExit ? 2 : 4+(TestLabPanel.Available ? 1 : 0)+(CampTestPanel.Available ? 1 : 0);
            var direction=GameInput.UiNavigate;
            if(direction.sqrMagnitude<.25f) nextNavigation=0;
            else if(Event.current.type==EventType.Repaint && Time.unscaledTime>=nextNavigation) { focus=(focus+(direction.y>0 || direction.x<0 ? -1 : 1)+count)%count; nextNavigation=Time.unscaledTime+.2f; }
            if(focus>=count) focus=0;
            float x=panel.x+20,w=width-40,y=panel.y+70;
            if(options)
            {
                GUI.Label(new Rect(x,y,w,36),"Preferências salvas neste aparelho.",HudTheme.Label(17)); y+=48;
                if(optionCategory==OptionCategory.Gameplay)
                {
                    if(Button(new Rect(x,y,w,44),TouchAimSettings.Label,0)) TouchAimSettings.Toggle();
                    if(Button(new Rect(x,y+52,w,44),TouchAimSettings.SwapLabel,1)) TouchAimSettings.ToggleSwap();
                    if(Button(new Rect(x,y+104,w,44),TouchAimSettings.AutoFireLabel,2)) TouchAimSettings.ToggleAutoFire();
                    if(Button(new Rect(x,y+156,w,44),TouchAimSettings.ScreenAimLabel,3)) TouchAimSettings.ToggleScreenAim();
                    if(Button(new Rect(x,y+208,w,44),TouchAimSettings.AutoReloadLabel,4)) TouchAimSettings.ToggleAutoReload();
                    if(Button(new Rect(x,y+260,w,44),"Organizar botões e tamanhos",5)) { Close(); HudLayoutEditor.Show(); }
                    if(Button(new Rect(x,panel.yMax-65,w,46),"Voltar às categorias",6)) BackToOptions();
                }
                else if(optionCategory==OptionCategory.Graphics)
                {
                    float card=(w-12)/2;
                    if(Button(new Rect(x,y,card,58),"A · SIMPLES"+(HudTheme.Appearance==HudAppearance.Simple ? "\nSelecionado" : ""),0)) HudTheme.Select(HudAppearance.Simple);
                    if(Button(new Rect(x+card+12,y,card,58),"B · SOMBRIA"+(HudTheme.Somber ? "\nSelecionado" : ""),1)) HudTheme.Select(HudAppearance.Somber);
                    if(Button(new Rect(x,y+70,w,44),"Gráficos do cenário: "+SceneGraphics.Name+" · alternar",2)) SceneGraphics.Toggle();
                    if(Button(new Rect(x,panel.yMax-65,w,46),"Voltar às categorias",3)) BackToOptions();
                }
                else
                {
                    if(Button(new Rect(x,y,w,58),"Jogabilidade",0)) ShowGameplayOptions();
                    if(Button(new Rect(x,y+72,w,58),"Gráficos",1)) ShowGraphicsOptions();
                    if(Button(new Rect(x,panel.yMax-65,w,46),"Voltar",2)) { options=false; focus=0; }
                }
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
                    "Sair durante a expedição perde os itens da mochila.\nO baú seguro permanece na base.",HudTheme.Label(20));
                if(Button(new Rect(x,y+130,w,48),"Continuar jogando",0)) confirmExit=false;
                if(Button(new Rect(x,y+190,w,48),"Confirmar saida",1)) Leave();
            }
            else
            {
                int index=0;
                if(Button(new Rect(x,y,w,42),"Continuar",index++)) Close(); y+=50;
                if(Button(new Rect(x,y,w,42),"Opções",index++)) { options=true; focus=0; } y+=50;
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
