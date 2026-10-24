using UnityEngine;
using CyberExtraction.Items;
using CyberExtraction.Camp;

namespace CyberExtraction.UI
{
    // IMGUI geometry is shared with input capture through MinigameView.FishingControlBounds.
    public static class FishingPanelView
    {
        static Texture2D water,fish,bobber,spool,ripple;
        static bool loaded;
        static readonly GUIContent empty=GUIContent.none;
        static void Load()
        {
            if(loaded) return; loaded=true;
            water=Resources.Load<Texture2D>("Fishing/WaterPier"); fish=Resources.Load<Texture2D>("Fishing/FishSwim");
            bobber=Resources.Load<Texture2D>("Fishing/Bobber"); spool=Resources.Load<Texture2D>("Fishing/Spool");
            ripple=Resources.Load<Texture2D>("Fishing/Ripple");
        }
        static void Frame(Rect r,Color edge,float line=1)
        {
            HudTheme.Fill(new Rect(r.x,r.y,r.width,line),edge); HudTheme.Fill(new Rect(r.x,r.yMax-line,r.width,line),edge);
            HudTheme.Fill(new Rect(r.x,r.y,line,r.height),edge); HudTheme.Fill(new Rect(r.xMax-line,r.y,line,r.height),edge);
        }
        static void Label(Rect r,string value,int size=16,TextAnchor align=TextAnchor.MiddleLeft)=>GUI.Label(r,value,HudTheme.Label(size,align));
        static bool Send(CampCommandKind kind,bool bait=false)=>NetworkBootstrap.Instance!=null && NetworkBootstrap.Instance.SendCamp(new CampCommand { Kind=kind,UseBait=bait });
        public static bool Draw(FishingPresentation v)
        {
            Load(); Rect p=MinigameView.FishingPanelBounds, control=MinigameView.FishingControlBounds;
            GUI.Box(p,empty); Frame(p,HudTheme.Accent,1.5f);
            Frame(new Rect(p.x+5,p.y+5,p.width-10,p.height-10),Color.Lerp(HudTheme.Panel,HudTheme.Accent,.45f));
            Label(new Rect(p.x+25,p.y+12,140,32),"PESCA",25);
            HudTheme.Fill(new Rect(p.x+24,p.y+54,p.width-48,1),HudTheme.Accent);
            Rect pool=new Rect(p.x+24,p.y+76,p.width*.59f,400);
            Pond(pool,v);
            float x=control.x,w=control.width;
            Rect baitCard=new Rect(x,p.y+76,w,76);
            GUI.Box(baitCard,empty); Frame(baitCard,Color.Lerp(HudTheme.Panel,HudTheme.Accent,.7f));
            ItemIconGui.Draw(new Rect(x+9,baitCard.y+15,38,38),ItemId.Worm);
            int worms=CampSession.Progress.Count(ItemId.Worm);
            Label(new Rect(x+54,baitCard.y+5,w-62,29),v.UseBait ? "MINHOCA · "+worms : "SEM ISCA",18);
            var cfg=FishingDefinition.Current;
            Label(new Rect(x+54,baitCard.y+34,w-62,34),"Peixe raro: "+(v.UseBait ? cfg.BaitRarePercent : cfg.RarePercent)+"%",15);
            string heading=Heading(v);
            Label(new Rect(x,p.y+164,w,42),v.Synchronized ? heading : "Aguardando servidor…",w<250 ? 20 : 23);
            Label(new Rect(x,p.y+214,w,25),"FAIXA DE CAPTURA",16);
            Rect lane=new Rect(x,p.y+246,w,38);
            HudTheme.Fill(lane,Color.Lerp(HudTheme.Panel,Color.black,.35f)); Frame(lane,HudTheme.Accent);
            if(v.Stage==FishingStage.Capture) Track(lane,v);
            Label(new Rect(x,p.y+300,w,25),"CAPTURA",16);
            Rect meter=new Rect(x,p.y+331,w,12); HudTheme.Meter(meter,v.Progress,HudTheme.Good); Frame(meter,HudTheme.Accent);
            bool previous=GUI.enabled;
            GUI.enabled=previous && v.Synchronized && !CampSession.Pending;
            if(v.Ready)
            {
                float hitHeight=Mathf.Max(48,44/UiLayout.Scale);
                Rect choices=new Rect(pool.x+18,pool.yMax-112,pool.width-36,hitHeight);
                float cw=(choices.width-12)*.5f;
                if(GUI.Button(new Rect(choices.x,choices.y,cw,hitHeight),"Sem isca")) Send(CampCommandKind.SelectFishingBait,false);
                GUI.enabled=previous && v.Synchronized && !CampSession.Pending && worms>0;
                if(GUI.Button(new Rect(choices.x+cw+12,choices.y,cw,hitHeight),"Minhoca · "+worms)) Send(CampCommandKind.SelectFishingBait,true);
                Frame(new Rect(v.UseBait ? choices.x+cw+12 : choices.x,choices.y,cw,hitHeight),HudTheme.Good,2);
                GUI.enabled=previous && v.Synchronized && !CampSession.Pending && (!v.UseBait || worms>0);
                if(GUI.Button(control,empty)) Send(CampCommandKind.CastFishing);
            }
            else
            {
                GUI.Box(control,empty);
                if(v.Held && v.Stage==FishingStage.Capture) HudTheme.Fill(new Rect(control.x+3,control.y+3,control.width-6,control.height-6),Color.Lerp(HudTheme.Panel,HudTheme.Good,.3f));
            }
            Frame(control,HudTheme.Accent,1.5f);
            HudTheme.Fill(new Rect(control.x+8,control.y+4,control.width-16,1),Color.Lerp(HudTheme.Text,HudTheme.Accent,.6f));
            if(spool!=null) GUI.DrawTexture(new Rect(control.x+9,control.y+16,32,32),spool,ScaleMode.ScaleToFit,true);
            string action=v.Ready ? v.Stage==FishingStage.Ready ? "LANÇAR ANZOL" : "LANÇAR NOVAMENTE" : v.Stage==FishingStage.Capture ? "SEGURAR" : v.Stage==FishingStage.Bite ? "FISGAR" : "AGUARDE";
            Label(new Rect(control.x+45,control.y,control.width-50,control.height),action,control.width<245 ? 17 : 20,TextAnchor.MiddleCenter);
            GUI.enabled=previous;
            string help=v.Stage==FishingStage.Capture ? "Segure: direita · solte: esquerda" : v.Stage==FishingStage.Bite ? "Pressione agora: ESPAÇO / A / toque" : v.Ready ? (v.UseBait ? "Uma minhoca é usada na mordida." : "Pesca disponível sem isca.") : "Observe a boia. Espere a mordida.";
            Label(new Rect(x,control.yMax+7,w,50),help,15);
            GUI.enabled=previous && !CampSession.Pending;
            float closeHeight=Mathf.Max(40,44/UiLayout.Scale);
            bool cancel=GUI.Button(new Rect(p.xMax-142,p.yMax-12-closeHeight,116,closeHeight),v.Stage==FishingStage.Success || v.Stage==FishingStage.Failed ? "Fechar" : "Cancelar");
            GUI.enabled=previous;
            if(v.Ready && v.Notice!=null) Label(new Rect(pool.x+12,pool.y+12,pool.width-24,70),v.Notice,17);
            else if(v.Ready && !string.IsNullOrEmpty(CampSession.Message) && v.Stage!=FishingStage.Success)
                Label(new Rect(pool.x+12,pool.y+12,pool.width-24,70),CampSession.Message,17);
            Label(new Rect(pool.x,pool.yMax+7,pool.width,34),v.Synchronized ? "" : "Reconectando: aguarde a confirmação do servidor.",14);
            return cancel;
        }
        static string Heading(FishingPresentation v)
        {
            switch(v.Stage)
            {
                case FishingStage.Ready: return "ESCOLHA A ISCA";
                case FishingStage.Cast: return "LANÇANDO";
                case FishingStage.Waiting: return "AGUARDE";
                case FishingStage.Approaching: return "APROXIMANDO";
                case FishingStage.Investigating: return "INVESTIGANDO";
                case FishingStage.Leaving: return "NÃO MORDEU";
                case FishingStage.Bite: return "FISGUE!";
                case FishingStage.Capture: return Mathf.Abs(v.Fish-v.Bar)<=v.Zone*.5f ? "NA FAIXA" : "FORA DA FAIXA";
                case FishingStage.Success: return "CAPTURADO";
                case FishingStage.Failed: return "O PEIXE ESCAPOU";
                default: return "PESCA";
            }
        }
        static void Track(Rect r,FishingPresentation v)
        {
            for(int i=0;i<=10;i++) HudTheme.Fill(new Rect(r.x+i*r.width/10,r.yMax+3,1,i%5==0 ? 5 : 3),HudTheme.Muted);
            bool inside=Mathf.Abs(v.Fish-v.Bar)<=v.Zone*.5f;
            Rect zone=new Rect(r.x+(v.Bar-v.Zone*.5f)*r.width,r.y+3,v.Zone*r.width,r.height-6);
            HudTheme.Fill(zone,Color.Lerp(HudTheme.Panel,HudTheme.Good,inside ? .45f : .20f)); Frame(zone,inside ? HudTheme.Good : HudTheme.Accent,2);
            float x=r.x+v.Fish*r.width,y=r.center.y;
            HudTheme.Fill(new Rect(x-8,y-4,16,8),HudTheme.Text); HudTheme.Fill(new Rect(x-14,y-5,5,10),HudTheme.Text);
            HudTheme.Fill(new Rect(x+4,y-2,2,2),HudTheme.Panel);
        }
        static void Atlas(Texture2D tex,Rect r,int frame,Color tint,float angle=0)
        {
            if(tex==null) return;
            var matrix=GUI.matrix; var color=GUI.color;
            GUI.color=tint;
            if(Mathf.Abs(angle)>.01f) GUIUtility.RotateAroundPivot(angle,r.center);
            GUI.DrawTextureWithTexCoords(r,tex,new Rect((frame%4)*.25f,(2-frame/4)/3f,.25f,1f/3f),true);
            GUI.matrix=matrix; GUI.color=color;
        }
        static void Line(Vector2 a,Vector2 b,Color color,float thickness=1)
        {
            var matrix=GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);
            HudTheme.Fill(new Rect(a.x,a.y,Vector2.Distance(a,b),thickness),color); GUI.matrix=matrix;
        }
        static void Pond(Rect r,FishingPresentation v)
        {
            if(water!=null) GUI.DrawTexture(r,water,ScaleMode.StretchToFill); else HudTheme.Fill(r,new Color(.07f,.20f,.20f));
            Frame(r,HudTheme.Accent);
            bool visible=v.Stage!=FishingStage.Ready && v.Stage!=FishingStage.Success && v.Stage!=FishingStage.Failed;
            var start=new Vector2(r.x+r.width*.11f,r.yMax-r.height*.06f);
            var anchor=new Vector2(r.x+r.width*.56f,r.y+r.height*.45f);
            var floatAt=anchor;
            float q=Mathf.SmoothStep(0,1,v.Fraction),size=v.Item==ItemId.FishLarge ? 1.18f : v.Item==ItemId.FishRare ? 1.03f : .84f;
            float fx=.56f-.07f,fy=.45f,angle=0;
            bool visitor=v.Stage==FishingStage.Approaching || v.Stage==FishingStage.Investigating || v.Stage==FishingStage.Leaving || v.Stage==FishingStage.Bite || v.Stage==FishingStage.Capture;
            if(v.Stage==FishingStage.Cast) floatAt=Vector2.Lerp(start,anchor,q)+Vector2.up*Mathf.Sin(q*Mathf.PI)*r.height*.12f;
            if(v.Stage==FishingStage.Approaching) { fx=Mathf.Lerp(.10f,.49f,q); fy=Mathf.Lerp(v.Visitor%2==0 ? .62f : .28f,.45f,q); }
            if(v.Stage==FishingStage.Investigating) { fx+=Mathf.Sin(v.PhaseAge*7)*.004f; angle=Mathf.Sin(v.PhaseAge*9)*7; }
            if(v.Stage==FishingStage.Leaving) { fx=Mathf.Lerp(.49f,.10f,q); fy=Mathf.Lerp(.45f,.65f,q); angle=Mathf.Lerp(0,180,Mathf.Clamp01(v.Fraction*4)); }
            if(v.Stage==FishingStage.Capture) { fx=.18f+v.Fish*.58f; fy=.45f+Mathf.Sin(v.PhaseAge*3)*.06f; floatAt=new Vector2(r.x+fx*r.width+30*size,r.y+fy*r.height); }
            if(visitor)
            {
                float fade=v.Stage==FishingStage.Approaching ? Mathf.Clamp01(v.Fraction*5) : v.Stage==FishingStage.Leaving ? Mathf.Clamp01((1-v.Fraction)*5) : 1;
                Atlas(fish,new Rect(r.x+fx*r.width-52*size,r.y+fy*r.height-26*size,104*size,52*size),(int)(v.PhaseAge*12)%12,new Color(1,1,1,fade*.85f),angle);
            }
            if(visible)
            {
                Line(start,floatAt,HudTheme.Muted,1);
                for(int i=0;i<3;i++)
                {
                    float wave=Mathf.Repeat(v.PhaseAge*.65f+i/3f,1),rad=8+wave*22;
                    if(ripple!=null)
                    {
                        var old=GUI.color; GUI.color=new Color(1,1,1,(1-wave)*.45f);
                        GUI.DrawTexture(new Rect(floatAt.x-rad,floatAt.y-rad*.65f,rad*2,rad*1.3f),ripple);
                        GUI.color=old;
                    }
                }
                float alpha=v.Stage==FishingStage.Bite ? 1-Mathf.Clamp01(v.PhaseAge/.22f) : 1;
                Atlas(bobber,new Rect(floatAt.x-13,floatAt.y-17,26,34),(int)(v.PhaseAge*12)%12,new Color(1,1,1,alpha));
                if(v.UseBait && !v.BaitConsumed) Line(floatAt+new Vector2(4,5),floatAt+new Vector2(7,11),new Color(.58f,.32f,.21f),2);
            }
            if(v.Stage==FishingStage.Success)
            {
                Rect card=new Rect(r.center.x-135,r.center.y-75,270,150); GUI.Box(card,empty); Frame(card,HudTheme.Accent);
                ItemIconGui.Draw(new Rect(card.x+106,card.y+12,58,44),v.Item);
                Label(new Rect(card.x+8,card.y+60,card.width-16,40),ItemCatalog.Name(v.Item),22,TextAnchor.MiddleCenter);
                Label(new Rect(card.x+8,card.y+105,card.width-16,30),"Guardado na mochila",17,TextAnchor.MiddleCenter);
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { water=fish=bobber=spool=ripple=null; loaded=false; }
    }
}
