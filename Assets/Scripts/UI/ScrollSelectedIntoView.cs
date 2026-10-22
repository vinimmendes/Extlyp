using UnityEngine;
using UnityEngine.EventSystems;
namespace CyberExtraction.UI
{
    public sealed class ScrollSelectedIntoView : MonoBehaviour,ISelectHandler
    {
        public void OnSelect(BaseEventData data)
        {
            var scroll=GetComponentInParent<UnityEngine.UI.ScrollRect>();
            if(scroll==null || scroll.content==null || scroll.viewport==null) return;
            Canvas.ForceUpdateCanvases();
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,(RectTransform)transform);
            var visible=scroll.viewport.rect;
            float delta=bounds.min.y<visible.yMin ? visible.yMin-bounds.min.y : bounds.max.y>visible.yMax ? visible.yMax-bounds.max.y : 0;
            scroll.content.anchoredPosition+=new Vector2(0,delta); scroll.StopMovement();
        }
    }
}
