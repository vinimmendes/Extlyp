using UnityEngine;

namespace CyberExtraction.UI
{
    public static class UiSprites
    {
        static readonly Sprite[] surfaces=new Sprite[4];
        static readonly Sprite[] sprintIcons=new Sprite[2];
        public static Sprite Icon(HudSymbol symbol)
        {
            if(symbol!=HudSymbol.Sprint) return Resources.Load<Sprite>("UI/Art/"+symbol);
            int index=HudTheme.Somber ? 1 : 0;
            if(sprintIcons[index]!=null) return sprintIcons[index];
            var texture=HudArt.IconTexture(symbol);
            var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100);
            sprite.name="Sprint"; sprite.hideFlags=HideFlags.HideAndDontSave;
            return sprintIcons[index]=sprite;
        }
        public static Sprite Panel=>Resources.Load<Sprite>("UI/Art/"+(HudTheme.Somber ? "PanelSomber" : "PanelSimple"));
        public static Sprite Knob=>Resources.Load<Sprite>("UI/Art/Knob");
        public static Sprite Surface(bool button)
        {
            int index=(HudTheme.Somber ? 2 : 0)+(button ? 1 : 0);
            if(surfaces[index]!=null) return surfaces[index];
            var texture=HudTheme.Surface(button);
            var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(8,8,8,8));
            sprite.name=(HudTheme.Somber ? "Sombria" : "Simples")+(button ? " · Botao" : " · Painel");
            sprite.hideFlags=HideFlags.HideAndDontSave;
            return surfaces[index]=sprite;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            for(int i=0;i<sprintIcons.Length;i++) { if(sprintIcons[i]!=null) Object.Destroy(sprintIcons[i]); sprintIcons[i]=null; }
            for(int i=0;i<surfaces.Length;i++) { if(surfaces[i]!=null) Object.Destroy(surfaces[i]); surfaces[i]=null; }
        }
    }
}
