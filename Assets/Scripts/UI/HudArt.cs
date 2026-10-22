using System.Collections.Generic;
using UnityEngine;

namespace CyberExtraction.UI
{
    public enum HudSymbol { Heal, Swap, Ammo, Backpack, Bullet, Aim, Map, Menu, Food, Spores, Chest, Exit, Arrow, Pistol, Rifle, Shotgun, Wind, Sprint }

    // Original, tintable silhouettes. Text remains live and independent of artwork.
    public static class HudArt
    {
        static readonly Dictionary<int,Texture2D> icons=new Dictionary<int,Texture2D>();
        static readonly Dictionary<int,Texture2D> rings=new Dictionary<int,Texture2D>();
        static readonly Queue<int> ringOrder=new Queue<int>();
        static readonly Dictionary<Vector2Int,Texture2D> panels=new Dictionary<Vector2Int,Texture2D>();
        static readonly Queue<Vector2Int> panelOrder=new Queue<Vector2Int>();
        static Texture2D knob;
        static readonly Dictionary<Vector3Int,Texture2D> canvasPanels=new Dictionary<Vector3Int,Texture2D>();
        static readonly List<Vector3Int> canvasPanelOrder=new List<Vector3Int>();
        static readonly Dictionary<int,Texture2D> canvasRings=new Dictionary<int,Texture2D>();
        static readonly List<int> canvasRingOrder=new List<int>();
        public static Texture2D IconTexture(HudSymbol symbol)
        {
            int key=(int)symbol+(HudTheme.Somber ? 128 : 0);
            if(!icons.TryGetValue(key,out var texture)) { texture=MakeIcon(symbol); icons.Add(key,texture); }
            return texture;
        }
        public static Texture2D CanvasPanel(Vector2 size)
        {
            var key=new Vector3Int(Mathf.Clamp(Mathf.CeilToInt(size.x*2),16,2048),Mathf.Clamp(Mathf.CeilToInt(size.y*2),16,2048),HudTheme.Somber ? 1 : 0);
            if(!canvasPanels.TryGetValue(key,out var texture)) { texture=MakePanel(key.x,key.y); canvasPanels.Add(key,texture); }
            canvasPanelOrder.Remove(key); canvasPanelOrder.Add(key);
            if(canvasPanelOrder.Count>48) { var expired=canvasPanelOrder[0]; canvasPanelOrder.RemoveAt(0); Object.Destroy(canvasPanels[expired]); canvasPanels.Remove(expired); }
            return texture;
        }
        public static Texture2D CanvasCircle(float diameter,bool prominent=false)
        {
            int size=Mathf.Clamp(Mathf.CeilToInt(diameter*2),32,1024);
            int key=size*8+(HudTheme.Somber ? 1 : 0)+(prominent ? 2 : 0);
            if(!canvasRings.TryGetValue(key,out var texture)) { texture=MakeRing(size,Mathf.Max(1,diameter),prominent,false); canvasRings.Add(key,texture); }
            canvasRingOrder.Remove(key); canvasRingOrder.Add(key);
            if(canvasRingOrder.Count>48) { int expired=canvasRingOrder[0]; canvasRingOrder.RemoveAt(0); Object.Destroy(canvasRings[expired]); canvasRings.Remove(expired); }
            return texture;
        }
        public static Texture2D CanvasKnob()
        {
            if(knob!=null) return knob;
            const int n=128; var pixels=new Color[n*n];
            for(int y=0;y<n;y++) for(int x=0;x<n;x++)
            {
                float d=Vector2.Distance(new Vector2(x,y),new Vector2(63.5f,63.5f));
                float shade=HudTheme.Somber ? .88f+(Mathf.PerlinNoise(x*.4f,y*.4f)-.5f)*.13f : .92f;
                pixels[y*n+x]=new Color(shade,shade,shade,Mathf.Clamp01(63-d)*(d>60 ? .9f : .65f));
            }
            knob=Texture("Joystick thumb",n,pixels); return knob;
        }
        public static void Icon(Rect rect,HudSymbol symbol,Color color,float angle=0)
        {
            int key=(int)symbol+(HudTheme.Somber ? 128 : 0);
            if(!icons.TryGetValue(key,out var texture)) { texture=MakeIcon(symbol); icons.Add(key,texture); }
            var matrix=GUI.matrix; var tint=GUI.color;
            if(angle!=0)
                GUI.matrix=matrix*Matrix4x4.Translate(rect.center)*Matrix4x4.Rotate(Quaternion.Euler(0,0,angle))*Matrix4x4.Translate(-rect.center);
            GUI.color=color; GUI.DrawTexture(rect,texture,ScaleMode.ScaleToFit,true);
            GUI.color=tint; GUI.matrix=matrix;
        }
        public static void Panel(Rect rect)
        {
            if(!HudTheme.Somber) { GUI.Box(rect,GUIContent.none); return; }
            // Generate at the panel's own aspect ratio: wear and corners never stretch.
            var key=new Vector2Int(Mathf.Clamp(Mathf.CeilToInt(rect.width*2),16,2048),Mathf.Clamp(Mathf.CeilToInt(rect.height*2),16,2048));
            if(!panels.TryGetValue(key,out var panel))
            {
                panel=MakePanel(key.x,key.y); panels.Add(key,panel); panelOrder.Enqueue(key);
                if(panelOrder.Count>16) { var expired=panelOrder.Dequeue(); Object.Destroy(panels[expired]); panels.Remove(expired); }
            }
            var old=GUI.color; GUI.color=Color.white;
            GUI.DrawTexture(rect,panel,ScaleMode.StretchToFill,true);
            GUI.color=old;
        }
        public static void Circle(Rect rect,bool prominent=false,bool held=false)
        {
            float scale=new Vector2(GUI.matrix.m00,GUI.matrix.m10).magnitude;
            float diameter=Mathf.Max(1,Mathf.Min(rect.width,rect.height)*scale);
            int size=Mathf.Clamp(Mathf.CeilToInt(diameter*2),32,1024);
            int key=size*8+(HudTheme.Somber ? 1 : 0)+(prominent ? 2 : 0)+(held ? 4 : 0);
            if(!rings.TryGetValue(key,out var texture))
            {
                texture=MakeRing(size,diameter,prominent,held); rings.Add(key,texture); ringOrder.Enqueue(key);
                if(ringOrder.Count>32) { int expired=ringOrder.Dequeue(); Object.Destroy(rings[expired]); rings.Remove(expired); }
            }
            var old=GUI.color; GUI.color=Color.white;
            GUI.DrawTexture(rect,texture,ScaleMode.ScaleToFit,true); GUI.color=old;
        }
        public static void Knob(Rect rect,bool held)
        {
            if(knob==null)
            {
                const int n=128; var pixels=new Color[n*n];
                for(int y=0;y<n;y++) for(int x=0;x<n;x++)
                { float d=Vector2.Distance(new Vector2(x,y),new Vector2(63.5f,63.5f)); float shade=.88f+(Mathf.PerlinNoise(x*.4f,y*.4f)-.5f)*.13f;
                  pixels[y*n+x]=new Color(shade,shade,shade,Mathf.Clamp01(63-d)*(d>60 ? .9f : .65f)); }
                knob=Texture("Worn joystick thumb",n,pixels);
            }
            var old=GUI.color; GUI.color=held ? HudTheme.Text : HudTheme.Muted;
            GUI.DrawTexture(rect,knob,ScaleMode.ScaleToFit,true); GUI.color=old;
        }
        public static bool Button(Rect rect,HudSymbol symbol,string text,Rect? hit=null)
        {
            bool clicked=GUI.Button(hit ?? rect,GUIContent.none,HudTheme.InvisibleButton);
            Panel(rect);
            float scale=rect.height/26;
            Icon(new Rect(rect.x+9*scale,rect.center.y-11*scale,22*scale,22*scale),symbol,HudTheme.Text);
            GUI.Label(new Rect(rect.x+37*scale,rect.y,rect.width-41*scale,rect.height),text,HudTheme.Label(Mathf.Clamp(Mathf.RoundToInt(14*scale),8,40),TextAnchor.MiddleCenter));
            return clicked;
        }
        static Texture2D Texture(string name,int size,Color[] pixels)
            =>Texture(name,size,size,pixels);
        static Texture2D Texture(string name,int width,int height,Color[] pixels)
        {
            var t=new Texture2D(width,height,TextureFormat.RGBA32,false) {name=name,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            t.SetPixels(pixels); t.Apply(false,true); return t;
        }
        static float Stroke(float distance,float center,float halfWidth)=>Mathf.Clamp01(halfWidth+.5f-Mathf.Abs(distance-center));
        static Texture2D MakePanel(int width,int height)
        {
            var px=new Color[width*height];
            for(int y=0;y<height;y++) for(int x=0;x<width;x++)
            {
                const float radius=5;
                float qx=Mathf.Abs(x+.5f-width*.5f)-(width*.5f-1.5f-radius);
                float qy=Mathf.Abs(y+.5f-height*.5f)-(height*.5f-1.5f-radius);
                float outside=Mathf.Sqrt(Mathf.Max(qx,0)*Mathf.Max(qx,0)+Mathf.Max(qy,0)*Mathf.Max(qy,0))+Mathf.Min(Mathf.Max(qx,qy),0)-radius;
                float inside=-outside,coverage=Mathf.Clamp01(.5f-outside);
                float fine=HudTheme.Somber ? Mathf.PerlinNoise(x*.43f+9,y*.43f+27)-.5f : 0;
                float patina=HudTheme.Somber ? Mathf.PerlinNoise(x*.055f+31,y*.075f+17) : 1;
                float fiber=HudTheme.Somber ? Mathf.PerlinNoise(x*.31f,y*.013f)-.5f : 0;
                var fill=HudTheme.Panel; fill.a=.74f;
                float shading=HudTheme.Somber ? fine*.008f+fiber*.004f-((1-patina)*.007f) : 0;
                fill.r+=shading; fill.g+=shading; fill.b+=shading*.8f;
                // Thin irregular metal edge, with sparse abrasions rather than uniform grain.
                float rim=Stroke(inside,1+fine*.22f,.75f);
                float chip=HudTheme.Somber ? Mathf.Clamp01((Mathf.PerlinNoise(x*.21f+51,y*.23f+63)-.64f)*3.2f) : 0;
                var edge=Color.Lerp(HudTheme.Muted,HudTheme.Accent,.30f);
                edge*=.76f+patina*.28f; edge.a=(.58f+patina*.16f)*(1-chip*.55f);
                var c=Color.Lerp(fill,edge,rim);
                float inset=Stroke(inside,3.6f,.18f)*(.08f+patina*.10f);
                c=Color.Lerp(c,HudTheme.Muted,inset);
                // A few short exposed marks on the corner joins, never on the text surface.
                float corner=Mathf.Min(Mathf.Min(x,width-1-x),Mathf.Min(y,height-1-y));
                if(inside<2.4f && corner<12 && patina>.46f) c=Color.Lerp(c,HudTheme.Text,rim*.16f);
                c.a*=coverage; px[y*width+x]=c;
            }
            return Texture("Somber tailored frame",width,height,px);
        }
        static Texture2D MakeRing(int n,float diameter,bool prominent,bool held)
        {
            var px=new Color[n*n];
            float pixelsPerScreen=n/diameter;
            float outer=n*.5f-pixelsPerScreen;
            // The outline must cover a real pixel on small buttons, not 5/512 of their diameter.
            float rim=(prominent ? 1.85f : 1.35f)*pixelsPerScreen;
            float artScale=512f/n;
            for(int y=0;y<n;y++) for(int x=0;x<n;x++)
            {
                float dx=x+.5f-n*.5f,dy=y+.5f-n*.5f,d=Mathf.Sqrt(dx*dx+dy*dy),angle=Mathf.Atan2(dy,dx);
                float noise=Mathf.PerlinNoise(x*artScale*.23f+3,y*artScale*.23f+7)-.5f;
                float patina=Mathf.PerlinNoise(x*artScale*.039f+19,y*artScale*.039f+41);
                var fill=HudTheme.Panel; fill.a=held ? .91f : .63f;
                float fiber=Mathf.PerlinNoise(x*artScale*.18f,y*artScale*.009f)-.5f;
                if(HudTheme.Somber) { fill.r+=noise*.008f+fiber*.006f; fill.g+=noise*.008f+fiber*.006f; fill.b+=noise*.006f+fiber*.005f; }
                var edge=prominent ? (HudTheme.Somber ? Color.Lerp(HudTheme.Accent,new Color(.82f,.49f,.30f),.35f) : HudTheme.Accent) : HudTheme.Muted;
                float abrasion=HudTheme.Somber ? Mathf.Clamp01((patina-.58f)*2.3f) : 0;
                edge.a=(prominent ? .95f : .82f)*(1-abrasion*.20f);
                float bevel=Mathf.Clamp01((rim*.5f-Mathf.Abs(d-(outer-rim*.5f)))/pixelsPerScreen+.5f);
                var c=held ? Color.Lerp(fill,HudTheme.Accent,.22f) : fill;
                c=Color.Lerp(c,edge,bevel);
                var highlight=Color.Lerp(edge,HudTheme.Text,prominent ? .13f : .07f);
                c=Color.Lerp(c,highlight,bevel*.16f*(1-abrasion));
                // Partial companion arc with two deliberately worn breaks, not a perfect second ring.
                if(prominent && HudTheme.Somber)
                {
                    float arc=(angle>-.08f && angle<2.88f && Mathf.Abs(angle-1.65f)>.055f) ? 1 : 0;
                    float companion=Mathf.Clamp01((.35f*pixelsPerScreen-Mathf.Abs(d-(outer-3.4f*pixelsPerScreen)))/pixelsPerScreen+.5f);
                    c=Color.Lerp(c,edge,companion*arc*(.54f+patina*.22f));
                }
                c.a*=Mathf.Clamp01((outer-d)/pixelsPerScreen+.5f); px[y*n+x]=c;
            }
            return Texture("HUD action ring",n,px);
        }
        sealed class Ink
        {
            const int N=256; readonly float[] alpha=new float[N*N];
            public void Line(float x,float y,float u,float v,float width=3)
                =>PaintLine(x,y,u,v,width,false);
            public void Etch(float x,float y,float u,float v,float width=3)
                =>PaintLine(x,y,u,v,width,true);
            void PaintLine(float x,float y,float u,float v,float width,bool engrave)
            {
                x*=2; y*=2; u*=2; v*=2; width*=2;
                var a=new Vector2(x,y); var b=new Vector2(u,v); var delta=b-a;
                int left=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(x,u)-width)),right=Mathf.Min(N-1,Mathf.CeilToInt(Mathf.Max(x,u)+width));
                int top=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(y,v)-width)),bottom=Mathf.Min(N-1,Mathf.CeilToInt(Mathf.Max(y,v)+width));
                for(int py=top;py<=bottom;py++) for(int px=left;px<=right;px++)
                { var p=new Vector2(px+.5f,py+.5f); float t=delta.sqrMagnitude>0 ? Mathf.Clamp01(Vector2.Dot(p-a,delta)/delta.sqrMagnitude) : 0;
                  int i=(N-1-py)*N+px; float coverage=Mathf.Clamp01(width*.5f+.5f-Vector2.Distance(p,a+delta*t));
                  alpha[i]=engrave ? alpha[i]*(1-coverage*.78f) : Mathf.Max(alpha[i],coverage); }
            }
            public void Path(float width,params float[] points)
            { for(int i=2;i<points.Length;i+=2) Line(points[i-2],points[i-1],points[i],points[i+1],width); }
            public void Box(float x,float y,float w,float h,float stroke=3)
            { Path(stroke,x,y,x+w,y,x+w,y+h,x,y+h,x,y); }
            public void Ring(float x,float y,float radius,float stroke=3)
            { for(int i=0;i<64;i++) { float a=i*Mathf.PI/32,b=(i+1)*Mathf.PI/32; Line(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius,stroke); } }
            public void Solid(params float[] p)
            {
                for(int y=0;y<N;y++) for(int x=0;x<N;x++)
                { bool inside=false; for(int i=0,j=p.Length-2;i<p.Length;j=i,i+=2)
                  if((p[i+1]>(y+.5f)*.5f)!=(p[j+1]>(y+.5f)*.5f) && (x+.5f)*.5f<(p[j]-p[i])*((y+.5f)*.5f-p[i+1])/(p[j+1]-p[i+1])+p[i]) inside=!inside;
                  if(inside) alpha[(N-1-y)*N+x]=1; }
            }
            public void Cut(float x,float y,float w,float h)
            { for(int py=Mathf.Max(0,(int)(y*2));py<Mathf.Min(N,(y+h)*2);py++) for(int px=Mathf.Max(0,(int)(x*2));px<Mathf.Min(N,(x+w)*2);px++) alpha[(N-1-py)*N+px]=0; }
            public Texture2D Finish(HudSymbol symbol)
            {
                var pixels=new Color[N*N];
                for(int i=0;i<pixels.Length;i++)
                {
                    float grain=HudTheme.Somber ? Mathf.PerlinNoise(i%N*.28f+11,i/N*.28f+17) : 1;
                    float paper=HudTheme.Somber ? .91f+grain*.09f : 1;
                    pixels[i]=new Color(paper,paper,paper,alpha[i]*(.965f+grain*.035f));
                }
                return Texture("HUD "+symbol,N,pixels);
            }
        }
        static Texture2D MakeIcon(HudSymbol symbol)
        {
            var p=new Ink();
            switch(symbol)
            {
                case HudSymbol.Sprint:
                    p.Ring(79,23,8,7);
                    p.Path(10,69,41,56,66,79,78,96,103);
                    p.Path(9,57,66,43,88,21,100);
                    p.Path(8,67,44,88,58,107,53);
                    p.Path(8,66,45,47,42,32,58); break;
                case HudSymbol.Heal:
                    p.Solid(28,37,100,37,104,42,104,98,100,102,28,102,24,98,24,42); p.Box(48,23,32,13,4);
                    p.Cut(57,50,14,41); p.Cut(44,63,40,14); p.Cut(28,40,4,8); p.Cut(96,40,4,8);
                    p.Etch(31,93,49,93,2); p.Etch(83,45,96,45,2); p.Line(51,23,77,23,2); break;
                case HudSymbol.Swap:
                    p.Line(22,43,101,43,7); p.Solid(86,27,110,43,86,59); p.Line(27,84,106,84,7); p.Solid(42,68,18,84,42,100); break;
                case HudSymbol.Ammo:
                    for(int x=30;x<=90;x+=30) { p.Line(x,31,x,105,4);p.Solid(x,18,x-9,38,x+9,38);
                        p.Path(4,x-8,87,x,95,x+8,87);p.Path(4,x-8,97,x,105,x+8,97); } break;
                case HudSymbol.Backpack:
                    p.Box(48,18,32,16,5); p.Solid(39,35,28,49,25,98,34,109,94,109,103,98,100,49,89,35);
                    p.Cut(37,71,54,28); p.Box(39,73,50,23,3); p.Cut(38,53,52,4); p.Cut(56,39,16,12); p.Box(59,42,10,6,2);
                    p.Etch(34,61,32,93,3); p.Etch(94,61,96,93,3); p.Line(43,79,85,79,2); p.Line(78,79,78,84,3);
                    p.Etch(42,102,86,102,2); p.Etch(40,43,49,43,2); p.Etch(79,43,88,43,2); break;
                case HudSymbol.Bullet:
                    p.Line(30,99,91,38,6);p.Solid(110,18,79,31,98,50);
                    p.Path(5,21,77,34,90,47,103);p.Path(5,20,89,33,102,46,115);break;
                case HudSymbol.Aim:
                    p.Ring(64,64,34,3); p.Line(64,18,64,43,3); p.Line(64,85,64,110,3); p.Line(18,64,43,64,3); p.Line(85,64,110,64,3); p.Ring(64,64,2,3); break;
                case HudSymbol.Map:
                    p.Path(4,16,31,46,20,80,32,111,20,111,96,80,108,46,96,16,108,16,31); p.Line(46,20,46,96,4); p.Line(80,32,80,108,4); break;
                case HudSymbol.Menu:
                    p.Line(22,32,106,32,6); p.Line(22,64,106,64,6); p.Line(22,96,106,96,6); break;
                case HudSymbol.Food:
                    p.Path(4,27,79,36,97,92,97,101,79,27,79); p.Line(20,105,108,105,4);
                    for(int x=42;x<=86;x+=22) p.Path(4,x,66,x-7,55,x+3,43,x-3,27,x+2,16); break;
                case HudSymbol.Spores:
                    p.Solid(14,62,20,39,40,23,66,18,91,28,107,46,112,62); p.Solid(54,67,72,67,82,110,44,110);
                    p.Cut(35,39,7,5); p.Cut(66,29,6,6); p.Cut(87,44,7,5);
                    p.Etch(26,56,103,56,2); p.Etch(58,75,54,99,2); p.Line(49,109,76,109,2); break;
                case HudSymbol.Chest:
                    p.Solid(20,54,108,54,108,96,104,100,24,100,20,96); p.Solid(20,49,27,31,100,31,108,49);
                    p.Cut(35,32,7,67); p.Cut(86,32,7,67); p.Etch(24,69,103,69,2); p.Etch(25,86,102,86,2);
                    p.Cut(54,53,20,24); p.Box(57,56,14,18,3); p.Cut(62,61,4,7);
                    p.Line(25,37,32,37,2); p.Line(96,37,102,37,2); p.Cut(25,59,3,3); p.Cut(99,91,3,3); break;
                case HudSymbol.Exit:
                    p.Path(5,21,111,21,44,43,26,85,26,107,44,107,111,21,111); p.Line(38,44,90,44,4); p.Path(4,49,101,49,57,78,57,78,101); p.Line(64,14,64,31,5); p.Line(53,19,75,19,5); break;
                case HudSymbol.Arrow: p.Solid(64,16,105,107,64,88,23,107); break;
                case HudSymbol.Pistol:
                    // Legacy enum value retained; the active weapon symbol is a crossbow.
                    p.Line(64,24,64,108,10);p.Path(7,16,48,39,31,64,25,89,31,112,48);
                    p.Path(3,16,48,64,90,112,48);break;
                case HudSymbol.Rifle:
                case HudSymbol.Shotgun:
                    p.Solid(12,82,39,58,69,48,76,57,50,70,37,95); p.Line(62,51,113,25,symbol==HudSymbol.Shotgun ? 10 : 6); p.Path(4,61,62,74,66,82,58); break;
                case HudSymbol.Wind:
                    for(int y=40;y<=88;y+=24) p.Path(4,14,y,32,y+5,53,y-7,72,y-7,91,y+4,113,y-2); break;
            }
            return p.Finish(symbol);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset()
        { foreach(var t in icons.Values) Object.Destroy(t); icons.Clear(); foreach(var t in rings.Values) Object.Destroy(t); rings.Clear(); ringOrder.Clear(); foreach(var t in panels.Values) Object.Destroy(t); panels.Clear(); panelOrder.Clear(); foreach(var t in canvasPanels.Values) Object.Destroy(t); canvasPanels.Clear(); canvasPanelOrder.Clear(); foreach(var t in canvasRings.Values) Object.Destroy(t); canvasRings.Clear(); canvasRingOrder.Clear(); if(knob!=null) Object.Destroy(knob); knob=null; }
    }
}
