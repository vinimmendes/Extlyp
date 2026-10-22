using UnityEngine;

namespace CyberExtraction.Items
{
    // Original code-drawn silhouettes, shared by inventory, world loot and held weapons.
    public static class MeleeWeaponArt
    {
        static readonly Sprite[] sprites=new Sprite[3];
        public static Sprite Get(ItemId item)
        {
            if(!ItemCatalog.Melee(item)) return null;
            if(item==ItemId.RustySword) return ItemVisualCatalog.World(item) ?? ItemVisualCatalog.Interface(item);
            int index=(int)item-(int)ItemId.ShortSword;
            if(sprites[index]!=null) return sprites[index];
            const int size=128;
            var pixels=new Color[size*size];
            var steel=new Color(.73f,.77f,.73f); var edge=new Color(.92f,.91f,.82f);
            var wood=new Color(.4f,.27f,.15f); var binding=new Color(.64f,.49f,.27f);
            void Line(Vector2 a,Vector2 b,float width,Color color)
            {
                Vector2 delta=b-a;
                for(int y=0;y<size;y++) for(int x=0;x<size;x++)
                {
                    var point=new Vector2(x+.5f,y+.5f);
                    float t=Mathf.Clamp01(Vector2.Dot(point-a,delta)/Mathf.Max(1,delta.sqrMagnitude));
                    float alpha=Mathf.Clamp01(width*.5f+.5f-Vector2.Distance(point,a+delta*t));
                    if(alpha>0) pixels[y*size+x]=Color.Lerp(pixels[y*size+x],color,alpha);
                }
            }
            if(item==ItemId.ShortSword)
            {
                Line(new Vector2(14,64),new Vector2(47,64),9,wood);
                Line(new Vector2(47,46),new Vector2(47,82),6,binding);
                Line(new Vector2(53,64),new Vector2(105,64),14,steel);
                Line(new Vector2(105,64),new Vector2(119,64),7,edge);
                Line(new Vector2(55,67),new Vector2(106,67),2,edge);
            }
            else if(item==ItemId.BattleAxe)
            {
                Line(new Vector2(14,64),new Vector2(106,64),8,wood);
                Line(new Vector2(94,43),new Vector2(94,86),16,steel);
                Line(new Vector2(103,38),new Vector2(103,91),9,edge);
                Line(new Vector2(28,59),new Vector2(28,69),4,binding);
                Line(new Vector2(37,59),new Vector2(37,69),4,binding);
            }
            else
            {
                Line(new Vector2(8,64),new Vector2(96,64),5,wood);
                Line(new Vector2(94,64),new Vector2(116,64),10,steel);
                Line(new Vector2(110,64),new Vector2(123,64),4,edge);
                Line(new Vector2(83,60),new Vector2(83,68),3,binding);
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            texture.name="Melee_"+item; texture.filterMode=FilterMode.Bilinear; texture.wrapMode=TextureWrapMode.Clamp;
            texture.SetPixels(pixels); texture.Apply();
            return sprites[index]=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            for(int i=0;i<sprites.Length;i++) if(sprites[i]!=null)
            { Object.Destroy(sprites[i].texture); Object.Destroy(sprites[i]); sprites[i]=null; }
        }
    }
}
