using TMPro;
using UnityEngine;

namespace CyberExtraction.UI
{
    public sealed class PlayerProfileView : MonoBehaviour
    {
        public RectTransform Panel;
        public TextMeshProUGUI Title,Hint,Favorite,Body;
        public TMP_InputField NameInput;
        public UnityEngine.UI.Button Save;
        public UnityEngine.UI.Button[] Sections;
        public UnityEngine.UI.Image WeaponIcon;
        public UnityEngine.UI.ScrollRect Scroll;
    }
}
