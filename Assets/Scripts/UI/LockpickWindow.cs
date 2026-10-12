using UnityEngine;
using CyberExtraction.Combat;
using CyberExtraction.Players;
namespace CyberExtraction.UI
{
    [DefaultExecutionOrder(250)] public class LockpickWindow : MonoBehaviour
    {
        static LockpickWindow instance;
        LootPickup target; LockpickSnapshot view;
        string notice=""; float noticeUntil,nextNav; int selected; long sequence;
        string ignoredToken;
        bool pointerHeld,sentHold,previousCue;
        AudioSource sound; AudioClip click;
        public static bool IsOpen=>instance!=null && instance.view!=null && instance.view.Active && instance.target!=null && instance.target.IsSpawned;
        public static void Receive(LootPickup source,LockpickSnapshot snapshot) { if(instance!=null && snapshot!=null) instance.Accept(source,snapshot); }
        public static void CancelCurrent() { if(instance!=null) instance.Cancel(); }
        void Awake()
        {
            instance=this; sound=gameObject.AddComponent<AudioSource>(); sound.playOnAwake=false; sound.spatialBlend=0;
            var samples=new float[2205]; for(int i=0;i<samples.Length;i++) samples[i]=Mathf.Sin(i*2*Mathf.PI*740/44100)*.12f*(1-i/(float)samples.Length);
            click=AudioClip.Create("LockpickClick",samples.Length,1,44100,false); click.SetData(samples,0);
        }
        void OnDestroy() { if(instance==this) instance=null; if(click!=null) Destroy(click); }
        void Accept(LootPickup source,LockpickSnapshot snapshot)
        {
            if(snapshot.Active && source==target && snapshot.Token==ignoredToken) return;
            if(snapshot.Active && (view==null || view.Token!=snapshot.Token || target!=source))
            { target=source; selected=0; sequence=0; pointerHeld=sentHold=previousCue=false; InventoryWindow.Instance?.Close(); }
            if(snapshot.Active && snapshot.Cue && !previousCue) sound.PlayOneShot(click);
            previousCue=snapshot.Cue; view=snapshot;
            if(!snapshot.Active)
            {
                pointerHeld=sentHold=false; notice=snapshot.Message; noticeUntil=Time.unscaledTime+5;
                if(snapshot.Unlocked && source.FixtureKind.Value!=3) InventoryWindow.Instance?.OpenWorld(source);
            }
        }
        void Cancel()
        {
            if(IsOpen) { ignoredToken=view.Token; target.RequestCancelLock(view.Token,++sequence); }
            if(view!=null) view.Active=false; pointerHeld=sentHold=false;
        }
        void OnApplicationFocus(bool focused) { if(!focused) Cancel(); }
        void Update()
        {
            if(GameOptionsMenu.IsOpen || GameOptionsMenu.ConsumedBack) return;
            if(!IsOpen) return;
            if(GameInput.BackPressed || GameInput.TestPressed) { Cancel(); return; }
            var inv=InventoryWindow.Local(); if(inv==null || !inv.IsAlive) { Cancel(); return; }
            bool desired=GameInput.Hold || pointerHeld;
            if(desired && !sentHold) { target.RequestPin(view.Token,++sequence,selected,true); sentHold=true; }
            else if(!desired && sentHold) { target.RequestPin(view.Token,++sequence,selected,false); sentHold=false; }
            var direction=GameInput.UiNavigate;
            if(!sentHold && direction.sqrMagnitude>.25f && Time.unscaledTime>=nextNav)
            {
                int step=direction.x<0 || direction.y>0 ? -1 : 1;
                for(int i=1;i<=view.PinCount;i++) { int at=(selected+step*i+view.PinCount*2)%view.PinCount; if(!view.SetPins[at]) { selected=at; break; } }
                nextNav=Time.unscaledTime+.2f;
            }
        }
        static void Fill(Rect rect,Color color) { var old=GUI.color; GUI.color=color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=old; }
        void OnGUI()
        {
            if(GameOptionsMenu.IsOpen) return;
            if(Application.isBatchMode) return;
            using var frame=UiLayout.Begin();
            if(!IsOpen)
            {
                if(Time.unscaledTime<noticeUntil && !string.IsNullOrEmpty(notice) && !Camp.CampController.Active)
                    GUI.Label(new Rect(UiLayout.Width*.15f,UiLayout.Height-85,UiLayout.Width*.7f,35),notice,new GUIStyle(GUI.skin.label){fontSize=18,alignment=TextAnchor.MiddleCenter,wordWrap=true});
                return;
            }
            float width=Mathf.Min(460,UiLayout.Width-32); var panel=new Rect((UiLayout.Width-width)/2,105,width,385);
            Fill(panel,HudTheme.Panel); GUI.Box(panel,"");
            var label=new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true,alignment=TextAnchor.MiddleCenter};
            var small=new GUIStyle(label){fontSize=14}; var button=new GUIStyle(GUI.skin.button){fontSize=17,wordWrap=true};
            string title=target.FixtureKind.Value==1 ? "ARMARIO" : target.FixtureKind.Value==2 ? "GAVETA" : target.FixtureKind.Value==3 ? "PORTA" : "BAU";
            GUI.Label(new Rect(panel.x+14,panel.y+12,width-100,30),title+" — TRANCADO",label);
            if(GUI.Button(new Rect(panel.xMax-76,panel.y+10,62,30),"Sair",button)) { Cancel(); return; }
            GUI.Label(new Rect(panel.x+14,panel.y+45,width-28,24),"Fechadura "+(target.LockGrade.Value==0 ? "simples" : target.LockGrade.Value==1 ? "comum" : "reforcada"),small);
            float spacing=(width-48)/view.PinCount;
            for(int i=0;i<view.PinCount;i++)
            {
                float x=panel.x+24+i*spacing; var cell=new Rect(x,panel.y+80,spacing-7,155);
                Fill(cell,selected==i ? new Color(.24f,.25f,.18f) : new Color(.12f,.14f,.10f));
                var shaft=new Rect(cell.center.x-14,cell.y+12,28,99); Fill(shaft,new Color(.055f,.06f,.045f));
                float depth=view.SetPins[i] ? .55f : view.ActivePin==i ? view.Depth : 0;
                bool cue=view.ActivePin==i && view.Cue; float shake=cue ? Mathf.Sin(Time.unscaledTime*80)*1.2f : 0;
                for(int j=0;j<5;j++) Fill(new Rect(shaft.x+5,shaft.y+5+j*7,18,2),new Color(.40f,.42f,.32f));
                Fill(new Rect(shaft.x+4+shake,shaft.y+23+depth*47,20,34),cue ? new Color(.80f,.71f,.43f) : view.SetPins[i] ? new Color(.60f,.69f,.47f) : new Color(.55f,.53f,.43f));
                GUI.Label(new Rect(cell.x,cell.y+117,cell.width,27),view.SetPins[i] ? "Encaixado" : "Pino "+(i+1),small);
                if(!sentHold && !view.SetPins[i] && GUI.Button(cell,GUIContent.none,GUIStyle.none)) selected=i;
            }
            GUI.Label(new Rect(panel.x+16,panel.y+242,width-32,40),view.Message,small);
            Fill(new Rect(panel.x+20,panel.y+288,width-40,5),new Color(.20f,.23f,.16f));
            Fill(new Rect(panel.x+20,panel.y+288,(width-40)*view.Tension,5),new Color(.70f,.43f,.24f));
            GUI.Label(new Rect(panel.x+20,panel.y+294,width-40,22),"Tensao da gazua",small);
            bool held=GUI.RepeatButton(new Rect(panel.x+20,panel.y+320,width-40,44),"Segurar / soltar [Espaco]",button);
            if(Event.current.type==EventType.Repaint) pointerHeld=held;
        }
    }
}
