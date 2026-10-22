using UnityEngine;

namespace CyberExtraction.Items
{
    // Readable provisional tool silhouettes; the current painted item atlas is preserved.
    static class PassageToolArt
    {
        static readonly Sprite[] sprites=new Sprite[7];
        public static Sprite Get(ItemId item)
        {
            var dungeonIcon=DungeonToolArt.GetIcon(item);
            if(dungeonIcon!=null) return dungeonIcon;
            int id=item==ItemId.DungeonCrank ? 5 : item==ItemId.DungeonKey ? 6 : (int)item-(int)ItemId.BoltCutters;
            if(id<0 || id>=sprites.Length) return null;
            if(sprites[id]!=null) return sprites[id];
            const int size=96;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            var pixels=new Color[size*size];
            Color steel=new Color(.65f,.68f,.59f),handle=new Color(.36f,.23f,.12f);
            void Line(int x0,int y0,int x1,int y1,int width,Color color)
            {
                var a=new Vector2(x0,y0); var b=new Vector2(x1,y1); var delta=b-a;
                for(int y=0;y<size;y++) for(int x=0;x<size;x++)
                {
                    float t=Mathf.Clamp01(Vector2.Dot(new Vector2(x,y)-a,delta)/Mathf.Max(1,delta.sqrMagnitude));
                    if(Vector2.Distance(new Vector2(x,y),a+delta*t)<width) pixels[y*size+x]=color;
                }
            }
            if(id==0)
            {
                Line(20,14,46,58,6,steel); Line(76,14,50,58,6,steel);
                Line(20,14,34,39,7,handle); Line(76,14,62,39,7,handle);
                Line(44,56,30,80,7,steel); Line(52,56,66,80,7,steel);
                Line(30,80,43,80,5,steel); Line(66,80,53,80,5,steel);
            }
            else if(id==1) { Line(24,16,68,72,6,steel); Line(68,72,59,84,6,steel); Line(59,84,45,84,5,steel); Line(24,16,14,18,5,steel); }
            else if(id==2)
            {
                Line(18,35,65,54,13,steel); Line(65,54,78,62,9,handle); Line(78,62,70,79,8,handle);
                for(int i=0;i<7;i++) Line(20+i*6,24+i*2,24+i*6,19+i*2,3,steel);
            }
            else if(id==5)
            {
                Line(20,20,20,47,7,handle); Line(20,47,69,47,6,steel); Line(69,47,69,78,6,steel); Line(56,78,80,78,7,handle);
            }
            else
            {
                Color brass=id==3 ? new Color(.63f,.56f,.35f) : new Color(.42f,.58f,.66f);
                for(int y=0;y<size;y++) for(int x=0;x<size;x++)
                { float r=Vector2.Distance(new Vector2(x,y),new Vector2(30,65)); if(r>10 && r<18) pixels[y*size+x]=brass; }
                Line(40,51,73,17,5,brass); Line(65,25,75,35,5,brass); Line(57,33,67,43,5,brass);
            }
            texture.SetPixels(pixels); texture.filterMode=FilterMode.Bilinear; texture.wrapMode=TextureWrapMode.Clamp; texture.Apply();
            sprites[id]=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);
            return sprites[id];
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset()
        {
            for(int i=0;i<sprites.Length;i++) if(sprites[i]!=null)
            { Object.Destroy(sprites[i].texture); Object.Destroy(sprites[i]); sprites[i]=null; }
        }
    }
}
