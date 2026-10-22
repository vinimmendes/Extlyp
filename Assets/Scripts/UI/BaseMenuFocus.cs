using UnityEngine;
using UnityEngine.EventSystems;

namespace CyberExtraction.UI
{
    public sealed class BaseMenuFocus : MonoBehaviour,ISelectHandler,IDeselectHandler,IPointerEnterHandler,IPointerExitHandler
    {
        HudFrameGraphic frame;
        UnityEngine.UI.Selectable control;
        UnityEngine.UI.Image focusMark;
        bool selected,hovered;
        void Awake() { frame=GetComponent<HudFrameGraphic>(); control=GetComponent<UnityEngine.UI.Selectable>(); }
        void OnEnable() { selected=EventSystem.current!=null && EventSystem.current.currentSelectedGameObject==gameObject; Apply(); }
        internal void RefreshStyle()=>Apply();
        void Apply()
        {
            if(frame==null) return;
            frame.BaseFocused=frame.BaseStyle && control!=null && control.IsInteractable() && (selected || hovered);
            bool show=frame.BaseFocused && HudTheme.Somber && !frame.BaseTab;
            if(show && focusMark==null)
            {
                var marker=new GameObject("FocusUnderline",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.Image));
                marker.transform.SetParent(transform,false); focusMark=marker.GetComponent<UnityEngine.UI.Image>(); focusMark.raycastTarget=false;
                var rect=focusMark.rectTransform; rect.anchorMin=Vector2.zero; rect.anchorMax=new Vector2(1,0);
                rect.offsetMin=new Vector2(10,5); rect.offsetMax=new Vector2(-10,7);
            }
            if(focusMark!=null) { focusMark.color=HudTheme.Text; focusMark.enabled=show; }
            frame.SetVerticesDirty();
        }
        public void OnSelect(BaseEventData data) { selected=true; Apply(); }
        public void OnDeselect(BaseEventData data) { selected=false; Apply(); }
        public void OnPointerEnter(PointerEventData data) { hovered=true; Apply(); }
        public void OnPointerExit(PointerEventData data) { hovered=false; Apply(); }
        void OnDisable() { selected=hovered=false; Apply(); }
    }
}
