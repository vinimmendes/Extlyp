using UnityEngine;

namespace CyberExtraction.UI
{
    internal static class BaseMenuStyle
    {
        public static Color Secondary => HudTheme.Somber ? new Color(.75f,.73f,.65f) : new Color(.75f,.84f,.78f);
        public static void Scope(GameObject root,bool enabled)
        {
            foreach(var frame in root.GetComponentsInChildren<HudFrameGraphic>(true))
            {
                if(frame.SlotStyle || frame.Circle || frame.BarStyle) continue;
                frame.BaseStyle=enabled;
                if(!enabled) frame.BaseFocused=false;
                if(enabled && frame.GetComponent<UnityEngine.UI.Button>()!=null && frame.GetComponent<BaseMenuFocus>()==null)
                    frame.gameObject.AddComponent<BaseMenuFocus>();
                frame.ApplyTheme();
            }
        }
    }
}
