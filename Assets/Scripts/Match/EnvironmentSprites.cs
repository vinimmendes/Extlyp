using UnityEngine;
namespace CyberExtraction.Match
{
    public static class EnvironmentSprites
    {
        static Sprite[] sprites;
        public static Material Material => Resources.Load<Material>("Environment/AbandonedProps");
        public static Sprite Get(string name)
        {
            if(sprites==null) sprites=Resources.LoadAll<Sprite>("Environment/AbandonedProps");
            foreach(var sprite in sprites) if(sprite.name==name) return sprite;
            return null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { sprites=null; }
    }
}
