using UnityEngine;
namespace CyberExtraction.UI
{
    public static class UguiGeometry
    {
        public static void Place(RectTransform rect,Rect bounds) { rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(bounds.x,-bounds.y); rect.sizeDelta=bounds.size; }
        public static UnityEngine.UI.ScrollRect CreateVerticalScroll(Transform parent,string name)
        {
            var root=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.ScrollRect)); root.transform.SetParent(parent,false);
            var viewport=new GameObject("Viewport",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.RectMask2D));
            viewport.transform.SetParent(root.transform,false);
            var image=viewport.GetComponent<UnityEngine.UI.Image>(); image.color=Color.clear; image.canvasRenderer.cullTransparentMesh=false;
            var content=new GameObject("Content",typeof(RectTransform)); content.transform.SetParent(viewport.transform,false);
            var scroll=root.GetComponent<UnityEngine.UI.ScrollRect>();
            scroll.viewport=(RectTransform)viewport.transform; scroll.content=(RectTransform)content.transform;
            scroll.horizontal=false; scroll.vertical=true; scroll.scrollSensitivity=28;
            scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;
            return scroll;
        }
    }
}
