using TMPro;
using UnityEngine;

namespace CyberExtraction.UI
{
    // Authored references; runtime presenters never rebuild the visual hierarchy.
    public sealed class UguiPrefabView : MonoBehaviour
    {
        public UguiHudElement[] Elements;
        public RectTransform Panel,Viewport,Content;
        public TextMeshProUGUI Heading,Body,Detail;
        public RectTransform[] EnvironmentCards;
        public UnityEngine.UI.RawImage EnvironmentArtwork;
        public UnityEngine.UI.RawImage[] EnvironmentPortraits;
        public TextMeshProUGUI EnvironmentSubtitle;
        public TextMeshProUGUI[] EnvironmentTitles,EnvironmentDescriptions;
        public UnityEngine.UI.RawImage Backdrop;
        public HudFrameGraphic Surface;
        public RectTransform ResourceStrip;
        public UnityEngine.UI.Image[] ResourceIcons,RecipeIcons;
        public TextMeshProUGUI[] ResourceValues,RecipeTitles,RecipeDetails;
        public RectTransform[] RecipeCards;
        public UnityEngine.UI.Button[] Buttons;
        public UnityEngine.UI.Slider Slider;
        public UnityEngine.UI.RawImage MapImage,Portrait;
        public UnityEngine.UI.Image PlayerMarker;
        public RectTransform[] ExitMarkers;
        public TextMeshProUGUI[] SectorLabels;
        public RectTransform[] Grids;
        public UguiInventorySlot[] Slots;
        public UguiInventorySlot[] BackpackSlots,StorageSlots;
        public UnityEngine.UI.Button DiscardButton,CancelDiscard,SaleSource,UnequipButton;
        public UnityEngine.UI.Button OrganizeBackpack,OrganizeStorage;
        public UnityEngine.UI.Image[] ShopIcons;
        public TextMeshProUGUI[] ShopStock;
        public UguiInventorySlot[] EquipmentSlots;
        public TextMeshProUGUI EquipmentHeading;
        public TextMeshProUGUI CharacterHeading,CharacterBody;
        public UnityEngine.UI.Button CharacterToggle;
        public UnityEngine.UI.Button[] AttributeButtons;
        public UnityEngine.UI.Button CommerceToggle,CommerceMode,TradeConfirm,TradeUnload,TradeLess,TradeMore,TradeAll,RespecButton;
        public UnityEngine.UI.Button[] ShopOffers;
        public TextMeshProUGUI CommerceHeading;
        public RectTransform DragVisual;
        public UnityEngine.UI.Image DragIcon;
        public TextMeshProUGUI DragCaption;
    }
}
