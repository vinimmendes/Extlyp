using UnityEngine;
using CyberExtraction.Match;
using CyberExtraction.Players;
using CyberExtraction.Items;

namespace CyberExtraction.UI
{
    [DefaultExecutionOrder(200)]
    public class TestLabPanel : MonoBehaviour
    {
        static TestLabPanel instance;
        bool open, initialized;
        int tab, weapon, monster, item, lockKind, lockGrade;
        Vector2 scroll;
        int focus=2, controlCount, drawnControls;
        float nextNavigation;
        Camera dockedCamera;
        Rect cameraRect;
        bool submit, focusMoved, inScroll;
        readonly string[] itemNames=new string[ItemCatalog.Count];
        public static bool Available => NetworkBootstrap.Instance!=null && NetworkBootstrap.Instance.IsTestEnvironment && MatchManager.Instance!=null && MatchManager.Instance.Sandbox && MatchManager.Instance.CurrentState==MatchState.Running && InventoryWindow.Local()!=null;
        public static bool IsOpen => Available && instance!=null && instance.open;
        public static int CurrentTab => instance!=null ? instance.tab : 0;
        public static Rect ButtonBounds => new Rect(UiLayout.Width-164,276,150,42);
        public static Rect Bounds { get { float width=HudLayout.Touch ? Mathf.Min(760,UiLayout.Width-32) : Mathf.Min(500,UiLayout.Width*.48f); return new Rect(HudLayout.Touch ? (UiLayout.Width-width)/2 : UiLayout.Width-width-16,70,width,480); } }
        public static bool Contains(Vector2 point) => Available && (IsOpen || ButtonBounds.Contains(UiLayout.Point(point)));
        void Awake() { instance=this; for(int i=0;i<itemNames.Length;i++) itemNames[i]=ItemCatalog.Name((ItemId)i); }
        void OnDestroy() { RestoreCamera(); if(instance==this) instance=null; }
        public static void Show() { if(Available && instance!=null) instance.Open(); }
        void Update()
        {
            if(GameOptionsMenu.IsOpen || GameOptionsMenu.ConsumedBack) return;
            if(!Available) { open=initialized=false; RestoreCamera(); return; }
            if(!initialized) initialized=true;
            if(GameInput.TestPressed) { if(open) open=false; else Open(); }
            if(open && GameInput.BackPressed) open=false;
            if(open && !HudLayout.Touch && Camera.main!=null)
            {
                if(dockedCamera==null) { dockedCamera=Camera.main; cameraRect=dockedCamera.rect; }
                float fraction=Mathf.Clamp((UiLayout.SafeScreen.x+Bounds.x*UiLayout.Scale)/Screen.width,.3f,1);
                dockedCamera.rect=new Rect(cameraRect.x,cameraRect.y,cameraRect.width*fraction,cameraRect.height);
            }
            else RestoreCamera();
            if(open)
            {
                var nav=GameInput.UiNavigate;
                if(nav.sqrMagnitude<.25f) nextNavigation=0;
                else if(Time.unscaledTime>=nextNavigation && controlCount>0)
                {
                    int step=Mathf.Abs(nav.y)>Mathf.Abs(nav.x) ? (nav.y>0 ? -1 : 1) : (nav.x>0 ? 1 : -1);
                    focus=(focus+step+controlCount)%controlCount; focusMoved=true; nextNavigation=Time.unscaledTime+.2f;
                }
                submit=GameInput.UiSubmitPressed;
            }
        }
        void RestoreCamera() { if(dockedCamera!=null) dockedCamera.rect=cameraRect; dockedCamera=null; }
        void Open() { LockpickWindow.CancelCurrent(); InventoryWindow.Instance?.Close(); open=true; scroll=Vector2.zero; }
        void Send(TestAction action,int value=0) { MatchManager.Instance.RequestTest(action,value); }
        bool LabButton(Rect rect,string text,GUIStyle style=null)
        {
            int id=drawnControls++; GUI.SetNextControlName("LabControl"+id);
            var old=GUI.backgroundColor; if(id==focus) GUI.backgroundColor=new Color(.35f,.8f,.68f);
            bool pressed=style==null ? GUI.Button(rect,text) : GUI.Button(rect,text,style);
            GUI.backgroundColor=old;
            if(id==focus && focusMoved)
            {
                GUI.FocusControl("LabControl"+id);
                if(inScroll) scroll.y=Mathf.Max(0,rect.y-100);
                focusMoved=false;
            }
            if(submit && id==focus) { submit=false; pressed=true; }
            return pressed;
        }
        int LabGrid(Rect rect,int selected,string[] labels,int columns,GUIStyle style)
        {
            int rows=(labels.Length+columns-1)/columns;
            for(int i=0;i<labels.Length;i++)
            {
                var old=GUI.color; if(i==selected) GUI.color=new Color(.65f,1,.82f);
                if(LabButton(new Rect(rect.x+i%columns*rect.width/columns,rect.y+i/columns*rect.height/rows,rect.width/columns-3,rect.height/rows-3),labels[i],style)) selected=i;
                GUI.color=old;
            }
            return selected;
        }
        void OnGUI()
        {
            if(GameOptionsMenu.IsOpen) return;
            if(!Available || Application.isBatchMode) return;
            drawnControls=0; inScroll=false;
            using var frame=UiLayout.Begin();
            if(LabButton(ButtonBounds,"Ferramentas [F2]")) { if(open) open=false; else Open(); }
            if(!open) return;
            var panel=Bounds;
            var oldColor=GUI.color; GUI.color=HudTheme.Panel; GUI.DrawTexture(panel,Texture2D.whiteTexture); GUI.color=oldColor;
            GUI.Box(panel,"");
            var label=new GUIStyle(GUI.skin.label) { fontSize=17,wordWrap=true };
            var button=new GUIStyle(GUI.skin.button) { fontSize=16,wordWrap=true };
            GUI.Label(new Rect(panel.x+16,panel.y+10,panel.width-115,26),"LABORATORIO DE TESTES",label);
            if(LabButton(new Rect(panel.xMax-88,panel.y+8,72,30),"Fechar",button)) { open=false; return; }
            GUI.SetNextControlName("LabTabs");
            int selected=LabGrid(new Rect(panel.x+16,panel.y+45,panel.width-32,36),tab,new[] {"Equipar","Monstros","Sessao","Fechaduras"},4,button);
            if(selected!=tab) { tab=selected; scroll=Vector2.zero; }
            Rect view=new Rect(panel.x+16,panel.y+90,panel.width-32,310);
            float width=view.width-22;
            float itemGridHeight=width<550 ? 385 : 266;
            scroll=GUI.BeginScrollView(view,scroll,new Rect(0,0,width,tab==0 ? 227+itemGridHeight : tab==1 ? 398 : 390));
            inScroll=true;
            if(tab==0)
            {
                GUI.Label(new Rect(0,0,width,24),"Escolha a arma (trocar substitui a mochila de teste):",label);
                weapon=LabGrid(new Rect(0,30,width,44),weapon,new[] {"Pistola","Rifle de ferrolho","Espingarda"},3,button);
                if(LabButton(new Rect(0,82,width,36),"Equipar arma + municao + bandagens",button)) Send(TestAction.Weapon,weapon);
                GUI.Label(new Rect(0,130,width,24),"Entregar item para testar inventario, loot e pesca:",label);
                item=LabGrid(new Rect(0,162,width,itemGridHeight),item,itemNames,3,button);
                if(LabButton(new Rect(0,174+itemGridHeight,width,38),"Entregar item escolhido (mochila cheia: cai no chao)",button)) Send(TestAction.Item,item);
            }
            else if(tab==1)
            {
                GUI.Label(new Rect(0,0,width,40),"Cria perto do jogador, em espaco livre, preferindo a frente. Bulbos e servo incluem uma planta-mae.",label);
                monster=LabGrid(new Rect(0,47,width,224),monster,MatchManager.TestMonsters,2,button);
                if(LabButton(new Rect(0,278,width*.49f,30),"Criar escolhido",button)) Send(TestAction.Monster,monster);
                if(LabButton(new Rect(width*.51f,278,width*.49f,30),"Remover todos os monstros",button)) Send(TestAction.ClearEnemies);
                if(LabButton(new Rect(0,318,width*.49f,34),"Criar teia",button)) Send(TestAction.Web);
                if(LabButton(new Rect(width*.51f,318,width*.49f,34),"Criar area de acido",button)) Send(TestAction.Acid);
                if(LabButton(new Rect(0,361,width,34),"Limpar teias, acido e contaminacao",button)) Send(TestAction.ClearEffects);
            }
            else if(tab==2)
            {
                GUI.Label(new Rect(0,0,width,25),"Bots — trocar recria os bots com equipamento inicial:",label);
                int bots=LabGrid(new Rect(0,31,width,38),MatchManager.Instance.QuickBots.Value,new[] {"Sem bots","1 bot","2 bots","3 bots","4 bots"},5,button);
                if(bots!=MatchManager.Instance.QuickBots.Value) Send(TestAction.Bots,bots);
                if(LabButton(new Rect(0,85,width,35),MatchManager.Instance.TestProtection.Value ? "Protecao LIGADA — desligar" : "Protecao DESLIGADA — ligar",button)) Send(TestAction.Invulnerable,MatchManager.Instance.TestProtection.Value ? 0 : 1);
                if(LabButton(new Rect(0,136,width,38),"Restaurar vida, municao e remover status",button)) Send(TestAction.Restore);
                if(LabButton(new Rect(0,184,width,38),"Voltar ao abrigo e reviver",button)) Send(TestAction.ReturnToShelter);
                GUI.Label(new Rect(0,239,width,26),"Ir para uma area do mapa:",label);
                if(LabButton(new Rect(0,274,width/3-4,44),"Combate",button)) Send(TestAction.Teleport,0);
                if(LabButton(new Rect(width/3,274,width/3-4,44),"Primeira ruina",button)) Send(TestAction.Teleport,1);
                if(LabButton(new Rect(width*2/3,274,width/3-4,44),"Extracao",button)) Send(TestAction.Teleport,2);
                if(ServerDebugPanel.Available) ServerDebugPanel.DrawControl(new Rect(0,335,width,38));
                else GUI.Label(new Rect(0,335,width,50),"O teste continua durante o menu. Use protecao para observar ataques.",label);
            }
            else
            {
                GUI.Label(new Rect(0,0,width,38),"Gazuas e objetos trancados para teste rapido:",label);
                if(LabButton(new Rect(0,44,width,40),"Kit de gazuas (substitui a mochila de teste)",button)) Send(TestAction.LockKit);
                lockKind=LabGrid(new Rect(0,103,width,44),lockKind,new[]{"Bau","Armario","Gaveta","Porta"},4,button);
                lockGrade=LabGrid(new Rect(0,159,width,40),lockGrade,new[]{"Simples · 2 pinos","Comum · 3 pinos","Reforcada · 4 pinos"},3,button);
                if(LabButton(new Rect(0,216,width,42),"Criar fechadura escolhida perto de mim",button)) Send(TestAction.CreateLock,lockKind*3+lockGrade);
                if(LabButton(new Rect(0,273,width,36),"Remover fechaduras de teste",button)) Send(TestAction.ClearLocks);
                GUI.Label(new Rect(0,327,width,60),"Feche o painel e use E / Interagir. Selecione um pino, segure Espaco e solte no clique. Erros podem quebrar a mesma gazua.",label);
            }
            GUI.EndScrollView();
            inScroll=false; controlCount=drawnControls; if(focus>=controlCount) focus=2;
            GUI.Label(new Rect(panel.x+16,panel.y+411,panel.width-32,40),MatchManager.Instance.TestMessage.Value.ToString(),label);
            GUI.Label(new Rect(panel.x+16,panel.y+455,panel.width-32,22),"Itens temporarios. Acampamento preservado.",new GUIStyle(label) {fontSize=14});
        }
    }
}
