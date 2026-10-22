using TMPro;
using UnityEngine;

namespace CyberExtraction.UI
{
    public sealed class BaseHubView : MonoBehaviour
    {
        public RectTransform Panel,Preparation,Activities,Equipment,Destination,Scenery;
        public TextMeshProUGUI Heading,Resources,Message,EquipmentText,DestinationText,MapTitle;
        public UnityEngine.UI.RawImage Artwork;
        public UnityEngine.UI.Button[] Tabs,ActivityButtons;
        public UnityEngine.UI.Button CharacterShortcut;
        public UnityEngine.UI.Button Options,Organize,Depart,Cancel,Retry,Leave,Map,Bots;
        public UnityEngine.UI.Image[] EquipmentIcons;
        public CanvasGroup PreparationFade;
        public TextMeshProUGUI Brand,PreparationsTitle,BackpackLabel,BackpackCount,StatusText,DestinationLabel,ExpeditionTitle,ConnectionText,RiskText;
        public TextMeshProUGUI[] SupplyLabels,SupplyValues,ResourceLabels,ResourceValues;
        public UnityEngine.UI.Image[] SupplyIcons,ResourceIcons;
        public UnityEngine.UI.Image BackpackIcon,CapacityTrack,CapacityFill,HeaderShade,FooterShade,BackdropShade,TitleShade;
    }
}
