using TMPro;
using UnityEngine;

namespace CyberExtraction.UI
{
    [System.Serializable]
    public sealed class UguiHudElement
    {
        public HudControl Control;
        public RectTransform Root,Visual;
        public HudFrameGraphic Background,InnerRing;
        public UnityEngine.UI.Image Icon,Bar,BarTrack,Knob,Hit;
        public UnityEngine.UI.Outline Selection;
        public TextMeshProUGUI Title,Main,Secondary,Note;
        public UnityEngine.UI.Button Button;
        [System.NonSerialized] public Rect Previous;
        [System.NonSerialized] public UnityEngine.UI.Image Wait;
    }
}
