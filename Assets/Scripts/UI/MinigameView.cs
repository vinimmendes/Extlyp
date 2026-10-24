using UnityEngine;
using CyberExtraction.Items;

namespace CyberExtraction.UI
{
    public static class MinigameView
    {
        static AudioClip biteSound;
        public static Rect FishingPanelBounds
        {
            get { float width=Mathf.Min(1020,UiLayout.Width-32); return new Rect((UiLayout.Width-width)*.5f,32,width,536); }
        }
        public static Rect FishingControlBounds
        {
            get { var p=FishingPanelBounds; return new Rect(p.x+p.width*.65f,p.y+352,p.width*.32f,66); }
        }
        public static Rect CookingPanelBounds
        {
            get
            {
                float width=Mathf.Min(520,UiLayout.Width-32);
                float buttonHeight=Mathf.Max(44,44/UiLayout.Scale);
                float height=214+buttonHeight;
                return new Rect((UiLayout.Width-width)*.5f,(UiLayout.Height-height)*.5f,width,height);
            }
        }
        public static Rect CookingControlBounds
        {
            get
            {
                var panel=CookingPanelBounds;
                float width=Mathf.Min(230,(panel.width-60)*.56f);
                return new Rect(panel.xMax-24-width,panel.y+182,width,panel.height-214);
            }
        }
        public static bool CookingPanel(CookingPresentation view)
        {
            Rect panel=CookingPanelBounds;
            GUI.Box(panel,GUIContent.none);
            GUI.Label(new Rect(panel.x+52,panel.y+18,panel.width-154,28),"FOGUEIRA",HudTheme.Label(20));
            GUI.Label(new Rect(panel.xMax-84,panel.y+18,60,28),Mathf.CeilToInt(Mathf.Max(0,CookingGame.Seconds-view.Elapsed))+" s",HudTheme.Label(16,TextAnchor.MiddleRight));
            HudTheme.Fill(new Rect(panel.x+24,panel.y+60,panel.width-48,1),Color.Lerp(HudTheme.Panel,HudTheme.Accent,.35f));
            float x=panel.x+34,y=panel.y+32;
            float flicker=Mathf.Sin(Time.unscaledTime*5)*1.2f;
            var old=GUI.color;
            GUI.color=HudTheme.Accent;
            GUI.DrawTexture(new Rect(x-8,y-12-flicker,16,22+flicker),Core.SpriteFactory.CircleTexture);
            GUI.color=Color.Lerp(HudTheme.Accent,HudTheme.Text,.4f);
            GUI.DrawTexture(new Rect(x-4,y-3-flicker,8,12+flicker),Core.SpriteFactory.CircleTexture);
            GUI.color=old;
            string status=view.Submitted ? "Confirmando tentativa..." : !view.Synchronized ? "Aguardando conexao..." : view.Notice ?? "Pare na faixa clara";
            GUI.Label(new Rect(panel.x+24,panel.y+78,panel.width-48,40),status,HudTheme.Label(view.Notice==null ? 17 : 14,TextAnchor.MiddleCenter));
            Rect meter=new Rect(panel.x+24,panel.y+132,panel.width-48,22);
            Cooking(meter,view.Marker);
            Rect control=CookingControlBounds;
            GUI.Box(control,GUIContent.none);
            if(view.Submitted) HudTheme.Fill(new Rect(control.x+3,control.y+3,control.width-6,control.height-6),Color.Lerp(HudTheme.Panel,HudTheme.Accent,.25f));
            GUI.Label(control,view.Submitted ? "ENVIANDO..." : Players.GameInput.TouchActive ? "PARAR" : "PARAR [ESPACO / A]",HudTheme.Label(16,TextAnchor.MiddleCenter));
            bool enabled=GUI.enabled; GUI.enabled=enabled && !view.Submitted && !Camp.CampSession.Pending;
            bool cancel=GUI.Button(new Rect(panel.x+24,control.y,Mathf.Min(132,control.x-panel.x-36),control.height),"Cancelar");
            GUI.enabled=enabled;
            return cancel;
        }
        public static bool FishingPanel(FishingPresentation view,bool nibble)
        {
            return FishingPanelView.Draw(view);
        }
        public static void BiteCue()
        {
            if(Application.isBatchMode || Camera.main==null) return;
            if(biteSound==null)
            {
                const int rate=44100; var samples=new float[4410];
                for(int i=0;i<samples.Length;i++) samples[i]=Mathf.Sin(i*2*Mathf.PI*650/rate)*Mathf.Sin(i*Mathf.PI/samples.Length)*.2f;
                biteSound=AudioClip.Create("FishingBite",samples.Length,1,rate,false); biteSound.SetData(samples,0);
            }
            AudioSource.PlayClipAtPoint(biteSound,Camera.main.transform.position,.7f);
        }
        public static void Bobber(Rect area,bool nibble,bool bitten,float phaseAge=1)
        {
            var old=GUI.color; GUI.color=Color.Lerp(HudTheme.Panel,new Color(.12f,.3f,.36f),.5f); GUI.DrawTexture(area,Texture2D.whiteTexture);
            float y=area.center.y+(bitten ? area.height*.22f*Mathf.SmoothStep(0,1,phaseAge/.18f) : nibble ? Mathf.Sin(Time.unscaledTime*30)*5 : Mathf.Sin(Time.unscaledTime*2)*3);
            for(int i=0;i<4;i++)
            {
                float wave=Mathf.Repeat(Time.unscaledTime*.35f+i*.25f,1);
                GUI.color=new Color(.6f,.75f,.7f,(1-wave)*.22f);
                GUI.DrawTexture(new Rect(area.center.x-18-wave*35,area.center.y+16+wave*15,36+wave*70,2),Texture2D.whiteTexture);
            }
            GUI.color=HudTheme.Muted; GUI.DrawTexture(new Rect(area.center.x-1,area.y,2,Mathf.Max(0,y-area.y-10)),Texture2D.whiteTexture);
            GUI.color=Color.white; GUI.DrawTexture(new Rect(area.center.x-12,y-12,24,24),CyberExtraction.Core.SpriteFactory.CircleTexture);
            GUI.color=Color.red; GUI.DrawTexture(new Rect(area.center.x-9,y-7,18,14),CyberExtraction.Core.SpriteFactory.CircleTexture);
            GUI.color=old;
        }
        public static void Fishing(Rect area,float bar,float fish,float zone,float progress)
        {
            GUI.Box(new Rect(area.x-6,area.y-6,area.width+12,area.height+12),GUIContent.none);
            HudTheme.Fill(area,Color.Lerp(HudTheme.Panel,new Color(.1f,.29f,.34f),.4f));
            for(int i=1;i<12;i++)
                HudTheme.Fill(new Rect(area.x+3,area.y+area.height*i/12,8,1),HudTheme.Muted*.6f);
            bool inside=Mathf.Abs(fish-bar)<=zone*.5f;
            Rect capture=new Rect(area.x+3,area.y+(1-bar-zone*.5f)*area.height,area.width-6,zone*area.height);
            Color edge=inside ? Color.Lerp(HudTheme.Good,HudTheme.Text,.3f) : HudTheme.Good;
            HudTheme.Fill(capture,Color.Lerp(HudTheme.Panel,HudTheme.Good,inside ? .7f : .38f));
            HudTheme.Fill(new Rect(capture.x,capture.y,capture.width,3),edge);
            HudTheme.Fill(new Rect(capture.x,capture.yMax-3,capture.width,3),edge);
            HudTheme.Fill(new Rect(capture.x,capture.y,2,capture.height),edge);
            HudTheme.Fill(new Rect(capture.xMax-2,capture.y,2,capture.height),edge);
            // Original geometric fish, with a dark outline, tail, dorsal fin and eye.
            float x=area.center.x, y=area.y+(1-fish)*area.height;
            Color fishColor=HudTheme.Somber ? new Color(.88f,.72f,.43f) : new Color(.98f,.83f,.42f);
            HudTheme.Fill(new Rect(x-16,y-8,29,16),HudTheme.Panel);
            HudTheme.Fill(new Rect(x-24,y-8,9,16),fishColor);
            HudTheme.Fill(new Rect(x-14,y-6,25,12),fishColor);
            HudTheme.Fill(new Rect(x-5,y-10,9,4),fishColor);
            HudTheme.Fill(new Rect(x+6,y-3,3,3),HudTheme.Panel);
            Rect meter=new Rect(area.xMax+20,area.y,18,area.height);
            GUI.Box(new Rect(meter.x-4,meter.y-4,meter.width+8,meter.height+8),GUIContent.none);
            HudTheme.Fill(meter,HudTheme.Panel);
            float fill=Mathf.Clamp01(progress)*meter.height;
            HudTheme.Fill(new Rect(meter.x,meter.yMax-fill,meter.width,fill),progress<.2f ? HudTheme.Danger : HudTheme.Good);
            GUI.Label(new Rect(area.x-12,area.yMax+12,area.width+64,28),Mathf.RoundToInt(progress*100)+"%",HudTheme.Label(18,TextAnchor.MiddleCenter));
        }
        public static void Cooking(Rect area,float marker)
        {
            HudTheme.Fill(new Rect(area.x-1,area.y-1,area.width+2,area.height+2),Color.Lerp(HudTheme.Panel,HudTheme.Muted,.55f));
            HudTheme.Fill(area,Color.Lerp(HudTheme.Panel,HudTheme.Muted,.2f));
            HudTheme.Fill(new Rect(area.x+area.width*CookingGame.GoodStart,area.y,area.width*(CookingGame.GoodEnd-CookingGame.GoodStart),area.height),Color.Lerp(HudTheme.Panel,HudTheme.Good,.7f));
            Rect perfect=new Rect(area.x+area.width*CookingGame.PerfectStart,area.y,area.width*(CookingGame.PerfectEnd-CookingGame.PerfectStart),area.height);
            HudTheme.Fill(perfect,Color.Lerp(HudTheme.Accent,HudTheme.Text,.35f));
            HudTheme.Fill(new Rect(perfect.x,perfect.y,perfect.width,1),HudTheme.Text);
            HudTheme.Fill(new Rect(perfect.x,perfect.yMax-1,perfect.width,1),HudTheme.Text);
            HudTheme.Fill(new Rect(area.x+area.width*CookingGame.GoodEnd,area.y,area.width*(1-CookingGame.GoodEnd),area.height),HudTheme.Danger);
            // Marker center uses the exact same normalized track as the quality zones.
            float cursor=area.x+Mathf.Clamp01(marker)*area.width;
            HudTheme.Fill(new Rect(cursor-2,area.y-5,4,area.height+10),HudTheme.Panel);
            HudTheme.Fill(new Rect(cursor-1,area.y-4,2,area.height+8),HudTheme.Text);
            HudTheme.Fill(new Rect(cursor-4,area.y-7,8,3),HudTheme.Text);
        }
    }
}
