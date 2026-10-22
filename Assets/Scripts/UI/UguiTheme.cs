using TMPro;
using UnityEngine;
namespace CyberExtraction.UI
{
    public static class UguiTheme
    {
        public static void Apply(GameObject root)
        {
            foreach(var frame in root.GetComponentsInChildren<HudFrameGraphic>(true)) frame.ApplyTheme();
            foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>(true)) text.color=HudTheme.Text;
        }
    }
}
