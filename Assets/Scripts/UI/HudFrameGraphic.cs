using UnityEngine;

namespace CyberExtraction.UI
{
    // Shared legacy/menu geometry; the refined HUD opts into normal Image sprites.
    public sealed class HudFrameGraphic : UnityEngine.UI.Image
    {
        public bool Circle,Prominent;
        public bool EntryStyle,Primary;
        public bool MenuStyle,SlotStyle;
        public bool HudStyle,Joystick,Unavailable,HealthTrack;
        public bool BarStyle;
        public bool BaseStyle,BaseTab,BaseSelected,BaseVeil;
        [System.NonSerialized] public bool BaseFocused;
        [Range(0,1)] public float FillOpacity=1;
        [System.NonSerialized] public bool ItemRarityRim;
        [System.NonSerialized] public Color ItemRimColor;
        [System.NonSerialized] public bool RasterArtwork;
        bool UsesMenuArtwork=>BaseStyle && !BaseTab && !BaseVeil && FillOpacity>0 && HudTheme.Somber && sprite!=null;
        public override Texture mainTexture=>RasterArtwork || UsesMenuArtwork ? base.mainTexture : BaseStyle || Circle || EntryStyle || MenuStyle || SlotStyle || HudStyle || BarStyle ? Texture2D.whiteTexture : base.mainTexture;
        public void ApplyTheme()
        {
            if(BaseStyle && !BaseTab && !BaseVeil && FillOpacity>0 && HudTheme.Somber)
            {
                bool button=GetComponent<UnityEngine.UI.Button>()!=null;
                sprite=MedievalMenuArt.Get(button,Primary);
                type=Type.Sliced;
                if(sprite!=null) pixelsPerUnitMultiplier=sprite.border.x/(button ? 12 : 24);
            }
            if(!BaseStyle && !Circle && !EntryStyle && !MenuStyle && !SlotStyle && !HudStyle && !BarStyle)
            {
                sprite=Application.isPlaying ? UiSprites.Surface(GetComponent<UnityEngine.UI.Button>()!=null) : UiSprites.Panel;
                type=Type.Sliced; pixelsPerUnitMultiplier=1; color=Color.white;
            }
            GetComponent<BaseMenuFocus>()?.RefreshStyle();
            SetVerticesDirty();
            SetMaterialDirty();
        }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            if(RasterArtwork) { base.OnPopulateMesh(mesh); return; }
            if(BaseStyle) { PopulateBase(mesh); return; }
            if(BarStyle)
            {
                mesh.Clear(); var bounds=GetPixelAdjustedRect();
                if(bounds.width<=.01f || bounds.height<=.01f) return;
                float barScale=canvas!=null ? canvas.scaleFactor*Mathf.Abs(rectTransform.lossyScale.x)/Mathf.Max(.001f,Mathf.Abs(canvas.transform.lossyScale.x)) : 1;
                RoundedHud(mesh,bounds,1.2f,0,.65f/Mathf.Max(.1f,barScale),Color.white,Color.white);
                return;
            }
            if(EntryStyle || MenuStyle || SlotStyle) { PopulateEntry(mesh); return; }
            if(HudStyle && !Circle) { PopulateHudPanel(mesh); return; }
            if(!Circle) { base.OnPopulateMesh(mesh); return; }
            mesh.Clear(); var rect=GetPixelAdjustedRect(); var center=rect.center;
            float scale=canvas!=null ? canvas.scaleFactor*Mathf.Abs(rectTransform.lossyScale.x)/Mathf.Max(.001f,Mathf.Abs(canvas.transform.lossyScale.x)) : 1;
            float radius=Mathf.Min(rect.width,rect.height)*.5f-1/Mathf.Max(.1f,scale);
            float rim=(Prominent ? 1.85f : 1.35f)/Mathf.Max(.1f,scale);
            var edge=Prominent ? HudTheme.Accent : HudTheme.Muted; edge.a=.82f;
            var fill=HudTheme.Panel; fill.a=.63f;
            if(Unavailable) { edge=new Color(.42f,.42f,.40f,.8f); fill=new Color(.075f,.075f,.07f,.6f); }
            edge*=color; fill*=color;
            const int segments=128;
            float feather=Mathf.Min(.6f/Mathf.Max(.1f,scale),rim*.35f);
            mesh.AddVert(center,fill,Vector2.zero);
            for(int i=0;i<=segments;i++)
            {
                float angle=i*Mathf.PI*2/segments; var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                var tint=edge; if(HudTheme.Somber) tint.a*=.92f+.08f*Mathf.Sin(angle*7+1)*Mathf.Sin(angle*3);
                mesh.AddVert(center+direction*Mathf.Max(0,radius-rim),fill,Vector2.zero);
                var transparent=tint; transparent.a=0;
                mesh.AddVert(center+direction*Mathf.Max(0,radius-rim),transparent,Vector2.zero);
                mesh.AddVert(center+direction*Mathf.Max(0,radius-rim+feather),tint,Vector2.zero);
                mesh.AddVert(center+direction*Mathf.Max(0,radius-feather),tint,Vector2.zero);
                mesh.AddVert(center+direction*Mathf.Max(0,radius),transparent,Vector2.zero);
                if(i==0) continue;
                int start=1+(i-1)*5,next=1+i*5;
                mesh.AddTriangle(0,start,next);
                for(int band=1;band<4;band++)
                { mesh.AddTriangle(start+band,start+band+1,next+band+1); mesh.AddTriangle(start+band,next+band+1,next+band); }
            }
            if(Joystick)
                for(int i=0;i<4;i++)
                {
                    float a=i*Mathf.PI/2; var direction=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                    var across=new Vector2(-direction.y,direction.x); int first=mesh.currentVertCount;
                    var tint=HudTheme.Muted; tint.a=.55f;
                    mesh.AddVert(center+direction*(radius-9),tint,Vector2.zero);
                    mesh.AddVert(center+direction*(radius-16)+across*4,tint,Vector2.zero);
                    mesh.AddVert(center+direction*(radius-16)-across*4,tint,Vector2.zero);
                    mesh.AddTriangle(first,first+1,first+2);
                }
            if(Prominent)
                for(int i=0;i<32;i++)
                {
                    float a=.1f+i*.65f*Mathf.PI/32,b=.1f+(i+1)*.65f*Mathf.PI/32;
                    float outer=radius-3.5f/Mathf.Max(.1f,scale),inner=outer-.65f/Mathf.Max(.1f,scale);
                    int first=mesh.currentVertCount;
                    var tint=edge; tint.a*=.5f;
                    mesh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*inner,tint,Vector2.zero);
                    mesh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*outer,tint,Vector2.zero);
                    mesh.AddVert(center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*outer,tint,Vector2.zero);
                    mesh.AddVert(center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*inner,tint,Vector2.zero);
                    mesh.AddTriangle(first,first+1,first+2); mesh.AddTriangle(first,first+2,first+3);
                }
        }
        void PopulateBase(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear(); var rect=GetPixelAdjustedRect();
            if(rect.width<=0 || rect.height<=0) return;
            if(FillOpacity<=0) return;
            if(UsesMenuArtwork)
            {
                base.OnPopulateMesh(mesh);
                return;
            }
            if(BaseVeil)
            {
                float feather=Mathf.Min(12,Mathf.Min(rect.width,rect.height)/2);
                float[] xs={rect.xMin,rect.xMin+feather,rect.xMax-feather,rect.xMax};
                float[] ys={rect.yMin,rect.yMin+feather,rect.yMax-feather,rect.yMax};
                for(int y=0;y<4;y++) for(int x=0;x<4;x++)
                {
                    var tint=color; if(x==0 || x==3 || y==0 || y==3) tint.a=0;
                    mesh.AddVert(new Vector3(xs[x],ys[y]),tint,Vector2.zero);
                    if(x==0 || y==0) continue;
                    int i=y*4+x; mesh.AddTriangle(i-5,i-1,i); mesh.AddTriangle(i-5,i,i-4);
                }
                return;
            }
            var control=GetComponent<UnityEngine.UI.Button>(); bool button=control!=null;
            bool focused=BaseFocused && (control==null || control.IsInteractable());
            var fill=HudTheme.Somber ? new Color(.055f,.067f,.049f,.97f) : new Color(.075f,.12f,.098f,.98f);
            var edge=HudTheme.Somber ? new Color(.45f,.41f,.31f,.65f) : new Color(.38f,.58f,.46f,.65f);
            if(BaseTab)
            {
                if(focused) Quad(mesh,rect,new Color(.20f,.22f,.17f,.85f));
                if(BaseSelected) Quad(mesh,new Rect(rect.x+10,rect.y,rect.width-20,2),HudTheme.Accent);
                if(focused) Rim(mesh,new Rect(rect.x+1,rect.y+1,rect.width-2,rect.height-2),1,HudTheme.Text);
                return;
            }
            if(button && Primary) fill=HudTheme.Somber ? new Color(.34f,.20f,.10f,1) : new Color(.12f,.28f,.20f,1);
            fill.a*=FillOpacity;
            Quad(mesh,rect,fill);
            if(button) Rim(mesh,rect,focused ? 1.5f : 1,focused ? HudTheme.Text : edge);
            else
            {
                // Quiet corner marks, no repeating scratches behind lettering.
                for(int i=0;i<4;i++)
                {
                    float x=i%2==0 ? rect.xMin : rect.xMax-12;
                    float y=i<2 ? rect.yMin : rect.yMax-.65f;
                    Quad(mesh,new Rect(x,y,12,.65f),edge);
                }
            }
        }
        void PopulateHudPanel(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear(); var rect=GetPixelAdjustedRect();
            float scale=canvas!=null ? canvas.scaleFactor*Mathf.Abs(rectTransform.lossyScale.x)/Mathf.Max(.001f,Mathf.Abs(canvas.transform.lossyScale.x)) : 1;
            float rim=1.1f/Mathf.Max(.1f,scale);
            var fill=HudTheme.Panel; fill.a=HudTheme.Somber ? .72f : .88f;
            var edge=HudTheme.Somber ? new Color(.58f,.52f,.40f,.72f) : HudTheme.Accent;
            RoundedHud(mesh,rect,HudTheme.Somber ? 3 : 5,rim,.8f/Mathf.Max(.1f,scale),fill,edge);
            if(HealthTrack) RoundedHud(mesh,new Rect(rect.xMin+11,rect.yMax-35,rect.width-22,12),1.5f,rim*.8f,.65f/Mathf.Max(.1f,scale),Color.clear,edge);
            if(HudTheme.Somber)
                for(int i=0;i<16;i++)
                {
                    float x=rect.xMin+2+(i*29%97)/97f*Mathf.Max(0,rect.width-6);
                    float y=rect.yMin+2+(i*43%89)/89f*Mathf.Max(0,rect.height-6);
                    Quad(mesh,new Rect(x,y,1.5f+(i%3),.45f),new Color(.65f,.59f,.43f,.045f));
                }
        }
        void RoundedHud(UnityEngine.UI.VertexHelper mesh,Rect rect,float radius,float stroke,float feather,Color fill,Color edge)
        {
            const int count=48;
            int first=mesh.currentVertCount;
            mesh.AddVert(rect.center,fill*color,Vector2.zero);
            var inner=new Rect(rect.x+stroke,rect.y+stroke,rect.width-2*stroke,rect.height-2*stroke);
            var outer=new Rect(rect.x-feather,rect.y-feather,rect.width+2*feather,rect.height+2*feather);
            var clear=edge; clear.a=0;
            for(int i=0;i<=count;i++)
            {
                var inside=RoundedPoint(inner,Mathf.Max(0,radius-stroke),i%count);
                var tone=fill; float light=(inside.y-rect.yMin)/Mathf.Max(1,rect.height);
                tone.r+=light*.012f; tone.g+=light*.012f; tone.b+=light*.01f;
                mesh.AddVert(inside,tone*color,Vector2.zero);
                mesh.AddVert(inside,edge*color,Vector2.zero);
                mesh.AddVert(RoundedPoint(rect,radius,i%count),edge*color,Vector2.zero);
                mesh.AddVert(RoundedPoint(outer,radius+feather,i%count),clear*color,Vector2.zero);
                if(i==0) continue;
                int a=first+1+(i-1)*4,b=first+1+i*4;
                mesh.AddTriangle(first,a,b);
                for(int band=1;band<3;band++)
                { mesh.AddTriangle(a+band,a+band+1,b+band+1); mesh.AddTriangle(a+band,b+band+1,b+band); }
            }
        }
        static Vector2 RoundedPoint(Rect rect,float radius,int index)
        {
            radius=Mathf.Min(radius,Mathf.Min(rect.width,rect.height)/2);
            int corner=index/12; float angle=(corner*90+(index%12)*7.5f)*Mathf.Deg2Rad;
            var center=new Vector2(corner==0 || corner==3 ? rect.xMax-radius : rect.xMin+radius,
                corner<2 ? rect.yMax-radius : rect.yMin+radius);
            return center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
        }
        void PopulateEntry(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear(); var rect=GetPixelAdjustedRect();
            float screenScale=canvas!=null ? canvas.scaleFactor*Mathf.Abs(rectTransform.lossyScale.x)/Mathf.Max(.001f,Mathf.Abs(canvas.transform.lossyScale.x)) : 1;
            float rim=1.25f/Mathf.Max(.1f,screenScale);
            bool button=GetComponent<UnityEngine.UI.Button>()!=null;
            Color fill=HudTheme.Somber ? (button && Primary ? new Color(.21f,.135f,.075f,.96f) : new Color(.075f,.085f,.065f,.94f)) : HudTheme.Panel;
            if(MenuStyle || SlotStyle) fill=HudTheme.Somber ? (button && Primary ? new Color(.25f,.14f,.075f,1) : new Color(.047f,.058f,.043f,.985f)) : HudTheme.Panel;
            fill.a*=FillOpacity;
            Color edge=HudTheme.Somber ? new Color(.58f,.38f,.20f,Primary ? .95f : .75f) : HudTheme.Accent;
            if(SlotStyle && ItemRarityRim) edge=ItemRimColor;
            Quad(mesh,rect,fill);
            // Restrained wear is native geometry, independent from the photo and text.
            if(HudTheme.Somber && !SlotStyle)
                for(int i=0;i<52;i++)
                {
                    float x=rect.xMin+4+(i*37%101)/101f*Mathf.Max(0,rect.width-8);
                    float y=rect.yMin+4+(i*61%97)/97f*Mathf.Max(0,rect.height-8);
                    Quad(mesh,new Rect(x,y,2+(i%4)*1.7f,.55f),new Color(.62f,.46f,.28f,.045f));
                }
            Rim(mesh,rect,rim,edge);
            if(Primary) Rim(mesh,new Rect(rect.x+3,rect.y+3,rect.width-6,rect.height-6),rim*.5f,new Color(edge.r,edge.g,edge.b,.28f));
            if(EntryStyle && !button)
            {
                float unit=rect.width/(Primary ? 476 : 455);
                Rim(mesh,new Rect(rect.xMin+22*unit,rect.yMax-242*unit,rect.width-44*unit,220*unit),rim*.65f,edge);
                Quad(mesh,new Rect(rect.center.x-55*unit,rect.yMax-318*unit,110*unit,rim*.65f),edge);
            }
        }
        void Quad(UnityEngine.UI.VertexHelper mesh,Rect rect,Color tint)
        {
            tint*=color; int i=mesh.currentVertCount;
            mesh.AddVert(new Vector3(rect.xMin,rect.yMin),tint,Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin,rect.yMax),tint,Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax,rect.yMax),tint,Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax,rect.yMin),tint,Vector2.zero);
            mesh.AddTriangle(i,i+1,i+2); mesh.AddTriangle(i,i+2,i+3);
        }
        void Rim(UnityEngine.UI.VertexHelper mesh,Rect rect,float width,Color tint)
        {
            // Subpixel strips touching the clip edge disappear at fractional Canvas scales.
            float pixel=1/Mathf.Max(.1f,canvas!=null ? canvas.scaleFactor : 1);
            width=Mathf.Max(pixel,width);
            rect=new Rect(rect.x+pixel*.5f,rect.y+pixel*.5f,rect.width-pixel,rect.height-pixel);
            if(rect.width<=width*2 || rect.height<=width*2) return;
            Quad(mesh,new Rect(rect.xMin,rect.yMin,rect.width,width),tint);
            Quad(mesh,new Rect(rect.xMin,rect.yMax-width,rect.width,width),tint);
            Quad(mesh,new Rect(rect.xMin,rect.yMin+width,width,rect.height-width*2),tint);
            Quad(mesh,new Rect(rect.xMax-width,rect.yMin+width,width,rect.height-width*2),tint);
        }
    }
}
