using UnityEngine;

namespace CyberExtraction.UI
{
    /// <summary>Bitmap silhouettes reserved for empty equipment slots; never item artwork.</summary>
    public static class EmptyEquipmentSlotArt
    {
        static readonly Sprite[] sprites=new Sprite[8];
        static Texture2D atlas;
        static bool loaded;

        public static Sprite Get(int slot)
        {
            if(slot<0 || slot>=sprites.Length) return null;
            // Weapon A has its own bitmap so the other seven atlas icons stay unchanged.
            if(slot==6)
            {
                if(sprites[slot]!=null) return sprites[slot];
                var crossbow=Resources.Load<Texture2D>("UI/Art/EmptyEquipmentHandCrossbow");
                if(crossbow==null) return null;
                var crossbowSprite=Sprite.Create(crossbow,new Rect(0,0,crossbow.width,crossbow.height),
                    new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
                crossbowSprite.name="EmptyEquipmentSlot_HandCrossbow";
                crossbowSprite.hideFlags=HideFlags.HideAndDontSave;
                return sprites[slot]=crossbowSprite;
            }
            if(!loaded)
            {
                atlas=Resources.Load<Texture2D>("UI/Art/EmptyEquipmentSlots");
                loaded=true;
            }
            if(atlas==null) return null;
            if(sprites[slot]!=null) return sprites[slot];
            int column=slot%4,row=1-slot/4;
            int left=Mathf.RoundToInt(column*atlas.width/4f);
            int right=Mathf.RoundToInt((column+1)*atlas.width/4f);
            int bottom=Mathf.RoundToInt(row*atlas.height/2f);
            int top=Mathf.RoundToInt((row+1)*atlas.height/2f);
            var sprite=Sprite.Create(atlas,new Rect(left,bottom,right-left,top-bottom),
                new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            sprite.name="EmptyEquipmentSlot_"+slot;
            sprite.hideFlags=HideFlags.HideAndDontSave;
            return sprites[slot]=sprite;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            for(int i=0;i<sprites.Length;i++)
            {
                if(sprites[i]!=null) Object.Destroy(sprites[i]);
                sprites[i]=null;
            }
            atlas=null;
            loaded=false;
        }
    }
}
