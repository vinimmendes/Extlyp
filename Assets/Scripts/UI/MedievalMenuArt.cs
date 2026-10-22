using UnityEngine;

namespace CyberExtraction.UI
{
    // Original bitmap surfaces. Nine-slicing preserves the frame at any panel size.
    internal static class MedievalMenuArt
    {
        static readonly Sprite[] sprites=new Sprite[3];
        static readonly bool[] loaded=new bool[3];
        static Sprite backpack;
        public static Sprite Backpack
        {
            get
            {
                if(backpack!=null) return backpack;
                var texture=Resources.Load<Texture2D>("UI/Art/MedievalBackpack");
                if(texture==null) return UiSprites.Icon(HudSymbol.Backpack);
                backpack=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100);
                backpack.name="MedievalBackpack"; backpack.hideFlags=HideFlags.HideAndDontSave;
                return backpack;
            }
        }
        public static Sprite Get(bool button,bool primary)
        {
            int index=button ? (primary ? 2 : 1) : 0;
            if(loaded[index]) return sprites[index];
            loaded[index]=true;
            string name=index==0 ? "MedievalPanel" : index==1 ? "MedievalButton" : "MedievalPrimary";
            var texture=Resources.Load<Texture2D>("UI/Art/"+name);
            if(texture==null) return null;
            // Exclude transparent export margins without modifying the source pixels.
            Rect uv=index==0 ? new Rect(.030f,.027f,.940f,.958f) :
                index==1 ? new Rect(.020f,.170f,.960f,.690f) : new Rect(.014f,.220f,.972f,.592f);
            var rect=new Rect(Mathf.Round(uv.x*texture.width),Mathf.Round(uv.y*texture.height),
                Mathf.Round(uv.width*texture.width),Mathf.Round(uv.height*texture.height));
            float corner=texture.width*(button ? .07f : .15f);
            var border=new Vector4(corner,button ? texture.height*.18f : corner,corner,button ? texture.height*.18f : corner);
            var sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);
            sprite.name=name; sprite.hideFlags=HideFlags.HideAndDontSave;
            return sprites[index]=sprite;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            if(backpack!=null) Object.Destroy(backpack); backpack=null;
            for(int i=0;i<sprites.Length;i++)
            {
                if(sprites[i]!=null) Object.Destroy(sprites[i]);
                sprites[i]=null; loaded[i]=false;
            }
        }
    }
}
