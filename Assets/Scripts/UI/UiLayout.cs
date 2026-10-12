using UnityEngine;
namespace CyberExtraction.UI
{
    public static class UiLayout
    {
        public readonly struct Frame : System.IDisposable
        {
            readonly Matrix4x4 previous;
            readonly GUISkin previousSkin;
            public Frame(Matrix4x4 previous) { this.previous=previous; previousSkin=GUI.skin; GUI.skin=HudTheme.Skin(previousSkin); GUI.matrix=Matrix; }
            public void Dispose() { GUI.matrix=previous; GUI.skin=previousSkin; }
        }
        public static Frame Begin()=>new Frame(GUI.matrix);
        public static Rect SafeScreen => Screen.safeArea.width>0 ? Screen.safeArea : new Rect(0,0,Screen.width,Screen.height);
        public static float Scale => Mathf.Max(.1f,SafeScreen.height/600f);
        public static float Width => SafeScreen.width/Scale;
        public static float Height => 600;
        public static Matrix4x4 Matrix => Matrix4x4.TRS(new Vector3(SafeScreen.x,Screen.height-SafeScreen.yMax,0),Quaternion.identity,Vector3.one*Scale);
        public static Vector2 Point(Vector2 screenPoint) => new Vector2((screenPoint.x-SafeScreen.x)/Scale,(SafeScreen.yMax-screenPoint.y)/Scale);
        public static Rect Region(float x,float y,float width,float height)=>new Rect(Width*x,Height*y,Width*width,Height*height);
        public static bool BlocksGameplay => ExpeditionMenu.IsOpen || GameOptionsMenu.IsOpen || CampTestPanel.IsOpen || LockpickWindow.IsOpen || TestLabPanel.IsOpen || InventoryWindow.IsOpen || (Camp.CampController.Active && (Camp.CampController.Instance.HasActivity || Camp.CampController.Instance.ExpeditionPanelOpen));
        public static bool ContainsControl(Vector2 point)=>ExpeditionMenu.IsOpen || GameOptionsMenu.IsOpen || GameOptionsMenu.ButtonBounds.Contains(Point(point)) || LockpickWindow.IsOpen || CampTestPanel.Contains(point) || TestLabPanel.Contains(point) || Quickbar.ContainsScreenPoint(point);
    }
}
