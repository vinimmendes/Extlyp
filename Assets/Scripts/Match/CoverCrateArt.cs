using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Match
{
    // Visual-only child: the existing parent collider, footprint and layer stay intact.
    public static class CoverCrateArt
    {
        static Sprite sprite;
        static bool attempted;
        public static void Apply(GameObject root,int order)
        {
            if(Application.isBatchMode) return;
            if(!attempted)
            {
                attempted=true;
                var texture=Resources.Load<Texture2D>("Environment/CoverCrate-v1");
                if(texture!=null)
                {
                    // Crop only the generator's transparent padding in sprite UV space.
                    // Normalized coordinates also work with platform texture downscaling.
                    var rect=new Rect(texture.width*77f/1254,texture.height*81f/1254,
                        texture.width*1102f/1254,texture.height*1091f/1254);
                    sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),rect.width,0,SpriteMeshType.FullRect);
                    sprite.name="Weathered cover crate";
                }
            }
            if(sprite==null) return;
            var art=new GameObject("CrateArt"); art.transform.SetParent(root.transform,false);
            art.transform.localScale=new Vector3(1/sprite.bounds.size.x,1/sprite.bounds.size.y,1);
            SpriteFactory.Setup(art.AddComponent<SpriteRenderer>(),sprite,Color.white,order);
            root.GetComponent<SpriteRenderer>().enabled=false;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset()
        { if(sprite!=null) Object.Destroy(sprite); sprite=null; attempted=false; }
    }
}
