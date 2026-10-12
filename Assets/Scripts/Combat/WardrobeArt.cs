using UnityEngine;

namespace CyberExtraction.Combat
{
    static class WardrobeArt
    {
        static Sprite closed,opened;
        public static Sprite Get(bool open)
        {
            if(closed==null)
            {
                var texture=Resources.Load<Texture2D>("Environment/WardrobeStates");
                if(texture==null || texture.width!=1536 || texture.height!=1024) return null;
                // Identical frame/pivot preserves body placement when the doors open.
                closed=Sprite.Create(texture,new Rect(0,224,768,544),new Vector2(.51f,.625f),100,0,SpriteMeshType.FullRect);
                opened=Sprite.Create(texture,new Rect(768,224,768,544),new Vector2(.51f,.625f),100,0,SpriteMeshType.FullRect);
            }
            return open ? opened : closed;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            if(closed!=null) Object.Destroy(closed);
            if(opened!=null) Object.Destroy(opened);
            closed=opened=null;
        }
    }
}
