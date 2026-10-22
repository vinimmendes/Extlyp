using UnityEngine;

namespace CyberExtraction.UI
{
    // Legacy vector paths remain available. The refined expedition HUD uses
    // layered raster sprites while preserving these prefab bindings and IDs.
    public sealed class HudActionIconGraphic : UnityEngine.UI.Image
    {
        public HudSymbol Symbol;
        [System.NonSerialized] public bool RasterArtwork;
        public override Texture mainTexture=>RasterArtwork ? base.mainTexture : Texture2D.whiteTexture;
        UnityEngine.UI.VertexHelper mesh;
        Vector2 center;
        float unit,fringe;

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper helper)
        {
            if(RasterArtwork) { base.OnPopulateMesh(helper); return; }
            helper.Clear(); mesh=helper;
            var rect=GetPixelAdjustedRect(); center=rect.center;
            unit=Mathf.Min(rect.width,rect.height)/64f;
            if(unit<=0) return;
            float scale=canvas!=null ? canvas.scaleFactor*Mathf.Abs(rectTransform.lossyScale.x)/Mathf.Max(.001f,Mathf.Abs(canvas.transform.lossyScale.x)) : 1;
            fringe=Mathf.Min(2,.75f/Mathf.Max(.01f,unit*scale));
            switch(Symbol)
            {
                case HudSymbol.Menu:
                    // Eight teeth and a central ring, both native vector paths.
                    Vector2 previous=Vector2.zero;
                    for(int i=0;i<=32;i++)
                    {
                        int phase=i%4; float radius=phase==1 || phase==2 ? 25 : 19;
                        float angle=i*Mathf.PI/16;
                        var point=new Vector2(32+Mathf.Cos(angle)*radius,32+Mathf.Sin(angle)*radius);
                        if(i>0) Line(previous.x,previous.y,point.x,point.y);
                        previous=point;
                    }
                    for(int i=0;i<24;i++)
                    {
                        float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;
                        Line(32+Mathf.Cos(a)*8,32+Mathf.Sin(a)*8,32+Mathf.Cos(b)*8,32+Mathf.Sin(b)*8);
                    }
                    break;
                case HudSymbol.Heal:
                    Path(24,22,24,14,40,14,40,22);
                    Path(10,22,54,22,54,53,10,53,10,22);
                    Line(25,37,39,37); Line(32,30,32,44);
                    break;
                case HudSymbol.Swap:
                    Path(10,21,53,21,43,11); Line(53,21,43,31);
                    Path(54,43,11,43,21,33); Line(11,43,21,53);
                    break;
                case HudSymbol.Ammo:
                    for(int i=0;i<3;i++)
                    {
                        float x=12+i*16;
                        Line(x+4,12,x+4,52);
                        Path(x,20,x+4,10,x+8,20);
                        Path(x,43,x+4,48,x+8,43);
                        Path(x,49,x+4,54,x+8,49);
                    }
                    break;
                case HudSymbol.Backpack:
                    Path(24,16,24,9,40,9,40,16);
                    Path(17,17,47,17,52,25,52,54,12,54,12,25,17,17);
                    Path(20,38,44,38,44,49,20,49,20,38);
                    Line(20,29,44,29); Line(38,29,38,33);
                    break;
            }
        }
        void Path(params float[] points)
        {
            for(int i=2;i<points.Length;i+=2) Line(points[i-2],points[i-1],points[i],points[i+1]);
        }
        Vector2 Point(float x,float y)=>center+new Vector2(x-32,32-y)*unit;
        void Line(float x1,float y1,float x2,float y2)
        {
            var a=Point(x1,y1); var b=Point(x2,y2); var direction=(b-a).normalized;
            var normal=new Vector2(-direction.y,direction.x); float half=1.5f*unit;
            var n=normal*half; var aa=normal*(half+fringe*unit);
            var clear=color; clear.a=0;
            Quad(a-n,b-n,b+n,a+n,color,color,color,color);
            Quad(a+n,b+n,b+aa,a+aa,color,color,clear,clear);
            Quad(a-aa,b-aa,b-n,a-n,clear,clear,color,color);
        }
        void Quad(Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color ca,Color cb,Color cc,Color cd)
        {
            int start=mesh.currentVertCount;
            mesh.AddVert(a,ca,Vector2.zero); mesh.AddVert(b,cb,Vector2.zero);
            mesh.AddVert(c,cc,Vector2.zero); mesh.AddVert(d,cd,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2); mesh.AddTriangle(start,start+2,start+3);
        }
    }
}
