using UnityEngine;

namespace CyberExtraction.UI
{
    internal static class UguiMenuStyle
    {
        public static void Backdrop(UguiPrefabView view,Rect bounds)
        {
            UguiGeometry.Place(view.Backdrop.rectTransform,new Rect(-bounds.x,-bounds.y,UiLayout.Width,600));
            float aspect=(float)view.Backdrop.texture.width/view.Backdrop.texture.height,display=UiLayout.Width/600;
            view.Backdrop.uvRect=aspect>display ? new Rect((1-display/aspect)/2,0,display/aspect,1) : new Rect(0,(1-aspect/display)/2,1,aspect/display);
            view.Backdrop.color=new Color(.7f,.7f,.7f,1);
            UguiGeometry.Place(view.Surface.rectTransform,new Rect(0,0,bounds.width,bounds.height));
            view.Surface.MenuStyle=true; view.Surface.ApplyTheme();
        }
        public static void Button(UnityEngine.UI.Button button,bool primary=false)
        {
            var frame=button.GetComponent<HudFrameGraphic>();
            frame.EntryStyle=false; frame.MenuStyle=true; frame.Primary=primary && (!frame.BaseStyle || button.name=="Depart"); frame.color=Color.white; frame.ApplyTheme();
            button.transition=UnityEngine.UI.Selectable.Transition.ColorTint;
            var colors=UnityEngine.UI.ColorBlock.defaultColorBlock;
            colors.normalColor=new Color(.88f,.88f,.88f,1); colors.highlightedColor=colors.selectedColor=Color.white;
            colors.pressedColor=new Color(.65f,.65f,.65f,1); colors.disabledColor=new Color(.38f,.38f,.38f,.8f);
            colors.fadeDuration=.09f; button.colors=colors;
            var text=button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            text.font=UiFontAssets.Regular; text.fontSize=18; text.color=HudTheme.Text;
        }
    }
}
