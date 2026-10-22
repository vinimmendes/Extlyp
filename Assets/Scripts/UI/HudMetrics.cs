using UnityEngine;

namespace CyberExtraction.UI
{
    public enum HudSizeProfile { Small, Standard, Large }
    // Only the expedition HUD uses dp. UiLayout and menu scaling remain unchanged.
    public static class HudMetrics
    {
        static int cachedFrame=-1;
        static float density=1;
        static Vector2 logical;
        static Vector2 simulation;
        static Rect previousSafe;
        static int previousWidth,previousHeight;
        public static void Simulate(Vector2 logicalSize) { simulation=logicalSize; cachedFrame=-1; previousSafe=Rect.zero; }
        public static float Density { get { Refresh(); return density; } }
        public static Vector2 LogicalSize { get { Refresh(); return logical; } }
        public static float Unit=>Density/UiLayout.Scale;
        public static float Dp(float value)=>value*Unit;
        public static HudSizeProfile Profile=>LogicalSize.x>=800 && LogicalSize.y>=480 ? HudSizeProfile.Large : LogicalSize.x<640 || LogicalSize.y<360 ? HudSizeProfile.Small : HudSizeProfile.Standard;
        public static string ProfileName=>Profile==HudSizeProfile.Small ? "Pequeno" : Profile==HudSizeProfile.Large ? "Amplo" : "Padrão";
        public static float Action=>Profile==HudSizeProfile.Small ? 48 : Profile==HudSizeProfile.Large ? 56 : 52;
        public static float Fire=>Profile==HudSizeProfile.Small ? 72 : Profile==HudSizeProfile.Large ? 88 : 76;
        public static float Move=>Profile==HudSizeProfile.Small ? 96 : Profile==HudSizeProfile.Large ? 112 : 104;
        public static Vector2 Map=>Profile==HudSizeProfile.Small ? new Vector2(88,80) : Profile==HudSizeProfile.Large ? new Vector2(112,104) : new Vector2(96,88);
        static void Refresh()
        {
            if(cachedFrame==Time.frameCount) return;
            cachedFrame=Time.frameCount;
            var safe=UiLayout.SafeScreen;
            if(safe==previousSafe && previousWidth==Screen.width && previousHeight==Screen.height && logical.y>0) return;
            previousSafe=safe; previousWidth=Screen.width; previousHeight=Screen.height;
#if UNITY_ANDROID && !UNITY_EDITOR
            // Resources.getDisplayMetrics uses logical Android density, never Screen.dpi.
            try
            {
                using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
                using var resources=activity.Call<AndroidJavaObject>("getResources");
                using var metrics=resources.Call<AndroidJavaObject>("getDisplayMetrics");
                density=Mathf.Max(.1f,metrics.Get<float>("density"));
                // Account for Unity rendering below the Android window resolution.
                density*=Mathf.Max(1,Screen.width)/(float)Mathf.Max(1,metrics.Get<int>("widthPixels"));
            }
            catch(System.Exception) { density=Mathf.Max(.1f,safe.height/360f); }
#else
            // Desktop previews use actual small logical sizes; large Game views
            // simulate a standard 360dp-high phone unless explicitly overridden.
            density=simulation.y>0 ? safe.height/simulation.y : safe.height<=480 ? 1 : safe.height/360f;
#endif
            logical=new Vector2(safe.width/density,safe.height/density);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { cachedFrame=-1; simulation=logical=Vector2.zero; previousSafe=Rect.zero; }
    }
}
