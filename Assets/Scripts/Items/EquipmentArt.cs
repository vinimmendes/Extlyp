using UnityEngine;

namespace CyberExtraction.Items
{
    // Original inventory/world silhouettes. A definition may override the generated icon.
    public static class EquipmentArt
    {
        static readonly Sprite[] sprites=new Sprite[EquipmentCatalog.ModelCount];
        static readonly Color Steel=new Color(.65f,.7f,.67f), Leather=new Color(.48f,.35f,.23f),
            Cloth=new Color(.39f,.46f,.35f), Edge=new Color(.88f,.84f,.72f), Dark=new Color(.16f,.19f,.16f);
        public static Sprite Get(ItemId item)
        {
            if(!EquipmentCatalog.IsDefined(item)) return null;
            int index=(int)item-(int)ItemId.WatchmanHood;
            if(sprites[index]!=null) return sprites[index];
            int slot=index/3,variant=index%3;
            var art=new Raster();
            if(slot==0)
            {
                if(variant==0)
                {
                    art.Polygon(Cloth,26,25,19,61,32,99,64,116,96,99,109,61,102,25,81,16,47,16);
                    art.Polygon(Dark,37,34,33,64,43,87,64,98,85,87,95,64,91,34,64,22);
                    art.Line(33,27,24,63,3,Edge); art.Line(104,63,95,27,3,Edge);
                }
                else if(variant==1)
                {
                    art.Polygon(Steel,29,28,23,80,39,104,64,114,89,104,105,80,99,28,78,15,50,15);
                    art.Line(35,69,93,69,12,Dark); art.Line(64,108,64,26,8,Edge);
                    art.Line(32,38,50,28,3,Edge); art.Line(78,28,96,38,3,Edge);
                }
                else
                {
                    art.Line(13,74,115,74,7,Leather);
                    art.Polygon(Leather,30,86,98,86,104,53,64,17,24,53);
                    art.Line(37,70,54,67,9,Dark); art.Line(74,67,91,70,9,Dark);
                    art.Line(64,76,64,35,3,Edge);
                    art.Line(42,50,56,45,2,Edge); art.Line(72,45,86,50,2,Edge);
                }
            }
            else if(slot==1)
            {
                Color material=variant==0 ? Cloth : Steel;
                art.Polygon(material,35,112,16,100,10,68,31,63,33,18,95,18,97,63,118,68,112,100,93,112,79,96,49,96);
                if(variant==0)
                {
                    art.Line(64,93,64,23,4,Leather); art.Line(36,36,92,36,9,Leather);
                    art.Line(40,77,54,63,3,Edge); art.Line(74,63,88,77,3,Edge);
                }
                else if(variant==1)
                {
                    for(int y=30;y<=86;y+=12) for(int x=43;x<=85;x+=14)
                        art.Ring(x+(y%24==6 ? 0 : 4),y,5,2,Dark);
                    art.Line(34,25,94,25,5,Leather);
                }
                else
                {
                    art.Polygon(Edge,43,85,64,93,85,85,89,50,64,39,39,50);
                    art.Line(64,88,64,45,4,Dark); art.Line(38,30,90,30,5,Dark);
                    art.Line(20,87,32,91,8,Edge); art.Line(96,91,108,87,8,Edge);
                }
            }
            else if(slot==2)
            {
                for(int hand=0;hand<2;hand++)
                {
                    int x=hand*58;
                    Color material=variant==1 ? Steel : Leather;
                    art.Polygon(material,x+16,16,x+49,16,x+51,64,x+57,80,x+51,84,x+43,68,x+42,103,x+35,107,x+31,104,x+26,110,x+18,107,x+14,95,x+13,61);
                    art.Line(x+17,31,x+47,31,9,variant==2 ? Cloth : Edge);
                    if(variant==0) { art.Line(x+25,69,x+25,99,3,Edge); art.Line(x+37,69,x+37,98,3,Edge); }
                    else if(variant==1) { art.Line(x+20,57,x+43,57,8,Edge); art.Line(x+22,78,x+40,78,8,Edge); }
                    else { art.Line(x+19,43,x+42,55,3,Edge); art.Line(x+19,55,x+42,43,3,Edge); }
                }
            }
            else if(slot==3)
            {
                for(int foot=0;foot<2;foot++)
                {
                    int x=foot*57;
                    art.Polygon(variant==2 ? Steel : Leather,x+14,112,x+47,112,x+44,52,x+55,29,x+54,16,x+8,16,x+8,29,x+17,51);
                    art.Line(x+13,20,x+52,20,6,Dark);
                    if(variant==0) for(int y=50;y<99;y+=13) art.Line(x+20,y,x+40,y+7,3,Edge);
                    else if(variant==1) { art.Line(x+17,100,x+44,100,10,Cloth); art.Line(x+17,65,x+42,74,5,Edge); }
                    else { art.Line(x+29,104,x+29,49,9,Edge); art.Line(x+18,78,x+41,78,4,Dark); }
                }
            }
            else if(slot==4)
            {
                art.Line(29,113,38,76,4,Leather); art.Line(99,113,90,76,4,Leather);
                art.Line(38,76,64,61,4,Leather); art.Line(90,76,64,61,4,Leather);
                if(variant==0)
                {
                    art.Disc(64,39,25,Steel); art.Ring(64,39,25,3,Edge);
                    art.Polygon(Dark,43,39,54,48,74,48,85,39,74,30,54,30); art.Disc(64,39,6,Edge);
                }
                else if(variant==1)
                {
                    art.Polygon(Cloth,64,71,91,42,64,11,37,42);
                    art.Line(64,22,64,60,4,Edge); art.Line(64,41,49,50,4,Edge); art.Line(64,34,79,43,4,Edge);
                }
                else
                {
                    art.Polygon(Edge,43,60,85,57,80,34,63,13,48,8,57,30);
                    art.Line(47,57,80,54,9,Leather); art.Line(53,52,76,49,3,Steel);
                }
            }
            else
            {
                art.Ring(64,48,31,11,Steel); art.Ring(64,48,35,2,Edge);
                if(variant==0)
                {
                    art.Polygon(Cloth,64,116,84,91,64,70,44,91);
                    art.Line(55,91,73,91,3,Edge); art.Line(64,82,64,101,3,Edge);
                }
                else if(variant==1)
                {
                    art.Polygon(Leather,44,77,84,77,88,105,40,105);
                    art.Line(47,90,55,90,3,Edge); art.Line(55,90,61,99,3,Edge);
                    art.Line(61,99,67,81,3,Edge); art.Line(67,81,73,90,3,Edge); art.Line(73,90,81,90,3,Edge);
                }
                else
                {
                    art.Polygon(Cloth,43,106,85,106,82,85,64,70,46,85);
                    art.Line(64,99,64,81,4,Edge); art.Line(55,91,73,91,4,Edge);
                }
            }
            var texture=new Texture2D(Raster.Size,Raster.Size,TextureFormat.RGBA32,false);
            texture.name="Equipment_"+item; texture.filterMode=FilterMode.Bilinear; texture.wrapMode=TextureWrapMode.Clamp;
            texture.SetPixels(art.Pixels); texture.Apply();
            return sprites[index]=Sprite.Create(texture,new Rect(0,0,Raster.Size,Raster.Size),new Vector2(.5f,.5f),Raster.Size);
        }
        sealed class Raster
        {
            public const int Size=128;
            public readonly Color[] Pixels=new Color[Size*Size];
            void Paint(int x,int y,Color color,float alpha)
            {
                if(alpha<=0) return;
                var prior=Pixels[y*Size+x]; float a=Mathf.Clamp01(alpha)*color.a;
                float combined=a+prior.a*(1-a);
                Pixels[y*Size+x]=new Color((color.r*a+prior.r*prior.a*(1-a))/combined,
                    (color.g*a+prior.g*prior.a*(1-a))/combined,(color.b*a+prior.b*prior.a*(1-a))/combined,combined);
            }
            public void Line(float ax,float ay,float bx,float by,float width,Color color)
            {
                var a=new Vector2(ax,ay); var delta=new Vector2(bx-ax,by-ay);
                for(int y=0;y<Size;y++) for(int x=0;x<Size;x++)
                {
                    var p=new Vector2(x+.5f,y+.5f);
                    float t=Mathf.Clamp01(Vector2.Dot(p-a,delta)/Mathf.Max(1,delta.sqrMagnitude));
                    Paint(x,y,color,width*.5f+.5f-Vector2.Distance(p,a+delta*t));
                }
            }
            public void Disc(float cx,float cy,float radius,Color color) => Circle(cx,cy,radius,0,color);
            public void Ring(float cx,float cy,float radius,float width,Color color) => Circle(cx,cy,radius,width,color);
            void Circle(float cx,float cy,float radius,float width,Color color)
            {
                for(int y=0;y<Size;y++) for(int x=0;x<Size;x++)
                {
                    float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(cx,cy));
                    Paint(x,y,color,width==0 ? radius+.5f-d : width*.5f+.5f-Mathf.Abs(d-radius));
                }
            }
            public void Polygon(Color color,params float[] coords)
            {
                int count=coords.Length/2;
                for(int y=0;y<Size;y++) for(int x=0;x<Size;x++)
                {
                    bool inside=false;
                    for(int i=0,j=count-1;i<count;j=i++)
                    {
                        float ax=coords[i*2],ay=coords[i*2+1],bx=coords[j*2],by=coords[j*2+1];
                        if((ay>y+.5f)!=(by>y+.5f) && x+.5f<(bx-ax)*(y+.5f-ay)/(by-ay)+ax) inside=!inside;
                    }
                    if(inside) Paint(x,y,color,1);
                }
                for(int i=0;i<count;i++) { int next=(i+1)%count; Line(coords[i*2],coords[i*2+1],coords[next*2],coords[next*2+1],1,color); }
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            for(int i=0;i<sprites.Length;i++) if(sprites[i]!=null)
            { Object.Destroy(sprites[i].texture); Object.Destroy(sprites[i]); sprites[i]=null; }
        }
    }
}
