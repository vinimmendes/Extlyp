using UnityEngine;

namespace CyberExtraction.Items
{
    // Original small silhouettes: coin, reliquary, idol, salvage, watch and document.
    public static class TreasureArt
    {
        static readonly Sprite[] sprites=new Sprite[6];
        public static Sprite Get(ItemId item)
        {
            if(!TreasureCatalog.IsDefined(item)) return null;
            int index=(int)item-(int)ItemId.AncientCoin;
            if(sprites[index]!=null) return sprites[index];
            const int size=96;
            var pixels=new Color[size*size];
            var metal=new Color(.62f,.67f,.62f); var bronze=new Color(.57f,.41f,.25f);
            var light=new Color(.87f,.82f,.68f); var dark=new Color(.2f,.24f,.19f);
            void Paint(int x,int y,Color color,float alpha)
            {
                float a=Mathf.Clamp01(alpha); if(a<=0) return;
                var prior=pixels[y*size+x]; float combined=a+prior.a*(1-a);
                pixels[y*size+x]=new Color((color.r*a+prior.r*prior.a*(1-a))/combined,
                    (color.g*a+prior.g*prior.a*(1-a))/combined,(color.b*a+prior.b*prior.a*(1-a))/combined,combined);
            }
            void Line(float ax,float ay,float bx,float by,float width,Color color)
            {
                var a=new Vector2(ax,ay); var delta=new Vector2(bx-ax,by-ay);
                for(int y=0;y<size;y++) for(int x=0;x<size;x++)
                {
                    var p=new Vector2(x+.5f,y+.5f); float t=Mathf.Clamp01(Vector2.Dot(p-a,delta)/Mathf.Max(1,delta.sqrMagnitude));
                    Paint(x,y,color,width*.5f+.5f-Vector2.Distance(p,a+delta*t));
                }
            }
            void Circle(float cx,float cy,float radius,float width,Color color)
            {
                for(int y=0;y<size;y++) for(int x=0;x<size;x++)
                {
                    float distance=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(cx,cy));
                    Paint(x,y,color,width==0 ? radius+.5f-distance : width*.5f+.5f-Mathf.Abs(distance-radius));
                }
            }
            if(index==0)
            {
                Circle(48,48,32,0,bronze); Circle(48,48,28,3,light); Circle(48,48,20,2,dark);
                Line(48,31,48,65,5,light); Line(38,58,48,65,4,light); Line(38,33,59,33,4,light);
            }
            else if(index==1)
            {
                Line(48,77,48,82,9,bronze); Circle(48,80,7,3,metal);
                Line(28,32,28,65,18,bronze); Line(48,25,48,70,22,bronze); Line(68,32,68,65,18,bronze);
                Line(24,27,72,27,4,light); Line(24,69,72,69,4,light);
                Circle(48,48,13,0,dark); Circle(48,48,9,3,light); Line(48,40,48,56,3,light);
            }
            else if(index==2)
            {
                Circle(48,69,15,0,metal); Line(48,26,48,49,26,metal); Line(26,16,70,16,12,bronze);
                Line(30,49,25,28,10,metal); Line(66,49,71,28,10,metal);
                Circle(42,72,3,0,dark); Circle(54,72,3,0,dark); Line(44,62,52,62,3,dark);
                Line(41,29,55,29,4,light);
            }
            else if(index==3)
            {
                for(int i=0;i<8;i++)
                {
                    float angle=i*Mathf.PI/4; var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                    Line(42+direction.x*21,52+direction.y*21,42+direction.x*29,52+direction.y*29,11,metal);
                }
                Circle(42,52,23,12,metal); Circle(42,52,13,3,bronze);
                Line(58,20,76,43,11,bronze); Line(68,40,80,30,7,light);
            }
            else if(index==4)
            {
                Circle(48,80,8,4,bronze); Circle(48,44,31,0,bronze); Circle(48,44,25,0,light);
                Circle(48,44,23,2,dark); Line(48,44,48,61,3,dark); Line(48,44,62,35,3,dark);
                for(int i=0;i<4;i++)
                { float a=i*Mathf.PI/2; Line(48+Mathf.Cos(a)*18,44+Mathf.Sin(a)*18,48+Mathf.Cos(a)*21,44+Mathf.Sin(a)*21,3,dark); }
            }
            else
            {
                Line(25,19,25,77,18,light); Line(43,19,43,77,18,light); Line(61,19,61,77,18,light);
                Line(18,80,69,80,8,bronze); Line(18,15,69,15,8,bronze);
                for(int y=36;y<=67;y+=9) Line(25,y,60-(y%3)*4,y,2,dark);
                Circle(64,28,10,0,bronze); Line(64,24,64,32,3,light);
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            texture.name="Treasure_"+item; texture.filterMode=FilterMode.Bilinear; texture.wrapMode=TextureWrapMode.Clamp;
            texture.SetPixels(pixels); texture.Apply();
            return sprites[index]=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset()
        {
            for(int i=0;i<sprites.Length;i++) if(sprites[i]!=null)
            { Object.Destroy(sprites[i].texture); Object.Destroy(sprites[i]); sprites[i]=null; }
        }
    }
}
