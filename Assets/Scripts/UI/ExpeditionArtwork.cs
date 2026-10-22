using UnityEngine;
using CyberExtraction.Match;

namespace CyberExtraction.UI
{
    internal static class ExpeditionArtwork
    {
        static Texture2D forest,catacombs,workshop;
        public static Texture2D For(ExpeditionMap map)
        {
            switch(map)
            {
                case ExpeditionMap.Dungeon:
                    return catacombs!=null ? catacombs : catacombs=Resources.Load<Texture2D>("UI/Art/MedievalCatacombsBackdrop");
                case ExpeditionMap.Test:
                    return workshop!=null ? workshop : workshop=Resources.Load<Texture2D>("UI/Art/EntryTestWorkshop");
                default:
                    return forest!=null ? forest : forest=Resources.Load<Texture2D>("UI/Art/ExpeditionForestBackdrop");
            }
        }
        public static void Apply(UnityEngine.UI.RawImage image,ExpeditionMap map)
        {
            image.texture=For(map);
            if(image.texture==null) return;
            var size=image.rectTransform.rect.size;
            if(size.x<=0 || size.y<=0) return;
            float aspect=(float)image.texture.width/image.texture.height,display=size.x/size.y;
            image.uvRect=aspect>display ? new Rect((1-display/aspect)/2,0,display/aspect,1) : new Rect(0,(1-aspect/display)/2,1,aspect/display);
        }
    }
}
