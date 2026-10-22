using UnityEngine;
using CyberExtraction.Players;

namespace CyberExtraction.UI
{
    [DefaultExecutionOrder(-200)]
    public sealed class HudLayoutEditor : MonoBehaviour
    {
        static HudLayoutEditor instance;
        bool open,dragging,showTools=true;
        string original;
        HudControl selected=HudControl.Fire;
        Vector2 dragOffset;
        int handledBack=-1;
        public static bool IsOpen=>instance!=null && instance.open;
        public static bool ConsumedBack=>instance!=null && instance.handledBack==Time.frameCount;
        public static HudControl Selected { get=>instance!=null ? instance.selected==HudControl.Map ? HudControl.Minimap : instance.selected : HudControl.Fire; set { if(instance!=null) instance.selected=value==HudControl.Map ? HudControl.Minimap : value; } }
        public static void SelectNext(int direction)
        {
            int next=(int)Selected;
            do { next=(next+direction+HudLayout.Controls.Length)%HudLayout.Controls.Length; } while((HudControl)next==HudControl.Map);
            Selected=(HudControl)next;
        }
        public static string SelectedName=>HudLayout.EditingControl(Selected)==HudControl.Health ? "Vida e stamina · grupo" : names[(int)Selected];
        public static void Commit() { if(instance!=null) { HudLayout.Save(); instance.open=instance.dragging=false; } }
        public static void Discard() { instance?.Cancel(); }
        static readonly string[] names={"Vida","Bem alimentado","Esporos","Tempo / extração","Mapa","Menu","Arma e munição","Curar","Trocar","Recarregar","Mochila","Mover","Mirar","Atirar","Interação / baú","Minimapa","Correr","Stamina"};
        void Awake() { instance=this; }
        void OnDestroy() { if(open) HudLayout.Restore(original); if(instance==this) instance=null; }
        public static void Show()
        {
            if(instance==null) return;
            instance.original=HudLayout.Capture(); instance.open=true; instance.dragging=false; instance.showTools=true;
        }
        void Update()
        {
            if(open && GameInput.BackPressed) { Cancel(); handledBack=Time.frameCount; }
        }
        void Cancel() { HudLayout.Restore(original); open=dragging=false; }
        static void Outline(Rect r,Color color)
        {
            HudTheme.Fill(new Rect(r.x,r.y,r.width,1),color); HudTheme.Fill(new Rect(r.x,r.yMax-1,r.width,1),color);
            HudTheme.Fill(new Rect(r.x,r.y,1,r.height),color); HudTheme.Fill(new Rect(r.xMax-1,r.y,1,r.height),color);
        }
        void OnGUI()
        {
            if(UguiGameHud.Active) return;
            if(!open || Application.isBatchMode) return;
            using var frame=UiLayout.Begin();
            HudTheme.Fill(new Rect(0,0,UiLayout.Width,UiLayout.Height),new Color(0,0,0,.62f));
            foreach(var control in HudLayout.Controls) if(control!=HudControl.Map) Preview(control,HudLayout.Bounds(control));
            var rect=HudLayout.Bounds(selected); Outline(rect,HudTheme.Accent);
            var toolbar=new Rect((UiLayout.Width-350)/2,210,350,182);
            var toggle=new Rect((UiLayout.Width-150)/2,174,150,30);
            bool inTools=toggle.Contains(Event.current.mousePosition) || showTools && toolbar.Contains(Event.current.mousePosition);
            var evt=Event.current;
            if(evt.type==EventType.MouseDown && evt.button==0 && !inTools)
            {
                for(int i=HudLayout.Controls.Length-1;i>=0;i--)
                {
                    var control=HudLayout.Controls[i]; var bounds=HudLayout.Bounds(control);
                    if(control==HudControl.Map) continue;
                    if(!bounds.Contains(evt.mousePosition)) continue;
                    selected=control; dragOffset=evt.mousePosition-bounds.position; dragging=true; evt.Use(); break;
                }
            }
            if(evt.type==EventType.MouseDrag && dragging)
            {
                MoveTo(evt.mousePosition-dragOffset); evt.Use();
            }
            if(evt.type==EventType.MouseUp && dragging) { dragging=false; evt.Use(); }
            if(GUI.Button(toggle,showTools ? "Ocultar ajustes" : "Mostrar ajustes")) showTools=!showTools;
            if(!showTools) return;
            HudArt.Panel(toolbar);
            float x=toolbar.x+12,w=toolbar.width-24,y=toolbar.y+10;
            GUI.Label(new Rect(x,y,w,20),"ORGANIZAR HUD · "+names[(int)selected],HudTheme.Label(16,TextAnchor.MiddleCenter));
            GUI.Label(new Rect(x,y+22,w,25),"Arraste os elementos · Conteúdo ilustrativo\nA partida online continua.",HudTheme.Label(11,TextAnchor.MiddleCenter));
            if(GUI.Button(new Rect(x,y+48,36,28),"‹")) SelectNext(-1);
            GUI.Label(new Rect(x+41,y+49,w-82,25),names[(int)selected],HudTheme.Label(14,TextAnchor.MiddleCenter));
            if(GUI.Button(new Rect(x+w-36,y+48,36,28),"›")) SelectNext(1);
            var target=HudLayout.EditingControl(selected);
            var p=HudLayout.Placement(target); var baseSize=HudLayout.DefaultSize(target);
            float scale=p.Size.x/baseSize.x;
            GUI.Label(new Rect(x,y+83,86,22),"Tamanho "+Mathf.RoundToInt(scale*100)+"%",HudTheme.Label(12));
            float changed=GUI.HorizontalSlider(new Rect(x+91,y+87,w-91,20),scale,.6f,2f);
            if(Mathf.Abs(changed-scale)>.001f)
            {
                HudLayout.ResizeForEditing(selected,changed);
            }
            if(GUI.Button(new Rect(x,y+113,w,25),"Restaurar este elemento")) HudLayout.ResetPlacement(target);
            float button=(w-12)/3;
            if(GUI.Button(new Rect(x,y+144,button,27),"Salvar")) { HudLayout.Save(); open=dragging=false; }
            if(GUI.Button(new Rect(x+button+6,y+144,button,27),"Cancelar")) Cancel();
            if(GUI.Button(new Rect(x+2*(button+6),y+144,button,27),"Padrão")) HudLayout.RestoreDefaults();
        }
        void MoveTo(Vector2 position)
        {
            HudLayout.MoveForEditing(selected,position);
        }
        static void Preview(HudControl c,Rect r)
        {
            if(c==HudControl.Move || c==HudControl.Aim || c==HudControl.Fire || c==HudControl.Heal || c==HudControl.Swap || c==HudControl.Reload || c==HudControl.Backpack || c==HudControl.Sprint)
            {
                HudArt.Circle(r,c==HudControl.Fire);
                var symbol=c==HudControl.Sprint ? HudSymbol.Sprint : c==HudControl.Aim ? HudSymbol.Aim : c==HudControl.Fire ? HudSymbol.Bullet : c==HudControl.Heal ? HudSymbol.Heal :
                    c==HudControl.Swap ? HudSymbol.Swap : c==HudControl.Reload ? HudSymbol.Ammo : HudSymbol.Backpack;
                if(c==HudControl.Move) HudArt.Knob(new Rect(r.center.x-r.width*.14f,r.center.y-r.height*.14f,r.width*.28f,r.height*.28f),false);
                else if(c==HudControl.Fire)
                {
                    HudArt.Icon(new Rect(r.x+r.width*.28f,r.y+r.height*.20f,r.width*.44f,r.height*.44f),symbol,HudTheme.Text);
                    GUI.Label(new Rect(r.x+6,r.y+r.height*.67f,r.width-12,r.height*.22f),"ATIRAR",HudTheme.Label(Mathf.RoundToInt(r.width*.145f),TextAnchor.MiddleCenter)); return;
                }
                else HudArt.Icon(new Rect(r.x+r.width*.22f,r.y+r.height*.22f,r.width*.56f,r.height*.56f),symbol,HudTheme.Text);
                if(c!=HudControl.Aim)
                {
                    float scale=r.width/HudLayout.DefaultSize(c).x;
                    GUI.Label(new Rect(r.center.x-38*scale,r.yMax+3,76*scale,40*scale),c==HudControl.Heal ? "Curar\n×3" : names[(int)c],HudTheme.Label(HudLayout.TextSize(c,c==HudControl.Move ? 14 : 13),TextAnchor.UpperCenter));
                }
                return;
            }
            HudArt.Panel(r);
            if(c==HudControl.Health || c==HudControl.Stamina)
            { GUI.Label(HudLayout.Detail(c,12,5,206,18),c==HudControl.Stamina ? "STAMINA  70 / 100" : "VIDA  86 / 120",HudTheme.Label(HudLayout.TextSize(c,14))); HudTheme.Meter(HudLayout.Detail(c,12,24,206,10),.72f,HudTheme.Good); }
            else if(c==HudControl.Clock)
            {
                GUI.Label(HudLayout.Detail(c,5,4,122,30),"03:42",HudTheme.Label(HudLayout.TextSize(c,26),TextAnchor.MiddleCenter));
                GUI.Label(HudLayout.Detail(c,9,35,57,18),"Extração",HudTheme.Label(HudLayout.TextSize(c,12)));
                HudArt.Icon(HudLayout.Detail(c,64,35,15,15),HudSymbol.Arrow,HudTheme.Text,45);
                GUI.Label(HudLayout.Detail(c,82,35,46,18),"280 u",HudTheme.Label(HudLayout.TextSize(c,12)));
            }
            else if(c==HudControl.Weapon)
            {
                HudArt.Icon(HudLayout.Detail(c,7,17,34,34),HudSymbol.Pistol,HudTheme.Text,-20);
                GUI.Label(HudLayout.Detail(c,47,4,93,16),"BESTA",HudTheme.Label(HudLayout.TextSize(c,12)));
                GUI.Label(HudLayout.Detail(c,47,19,93,26),"1 / 1",HudTheme.Label(HudLayout.TextSize(c,22)));
                GUI.Label(HudLayout.Detail(c,47,46,93,16),"Reserva 5",HudTheme.Label(HudLayout.TextSize(c,12)));
            }
            else if(c==HudControl.Minimap)
            { GUI.Label(new Rect(r.x,r.y+4,r.width,20),"N",HudTheme.Label(13,TextAnchor.MiddleCenter)); HudArt.Icon(new Rect(r.center.x-8,r.center.y-10,16,20),HudSymbol.Arrow,HudTheme.Text); }
            else
            {
                var symbol=c==HudControl.Map ? HudSymbol.Map : c==HudControl.Menu ? HudSymbol.Menu : c==HudControl.Food ? HudSymbol.Food : c==HudControl.Spores ? HudSymbol.Spores : HudSymbol.Chest;
                float scale=r.height/HudLayout.DefaultSize(c).y;
                HudArt.Icon(new Rect(r.x+7*scale,r.center.y-10*scale,20*scale,20*scale),symbol,HudTheme.Text);
                GUI.Label(new Rect(r.x+32*scale,r.y+2*scale,r.width-36*scale,r.height-4*scale),c==HudControl.Interaction ? "Abrir baú · 2 s" : c==HudControl.Spores ? "Esporos 24%" : names[(int)c],HudTheme.Label(HudLayout.TextSize(c,12),TextAnchor.MiddleCenter));
            }
        }
    }
}
