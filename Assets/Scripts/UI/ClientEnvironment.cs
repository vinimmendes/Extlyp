using UnityEngine;

namespace CyberExtraction.UI
{
    public enum CampEnvironment { None, Normal, Test }

    // Process-local selection survives scene reloads, never changes a saved profile.
    public static class ClientEnvironment
    {
        public static CampEnvironment Selected { get; private set; }
        public static bool Chosen => Selected != CampEnvironment.None;
        public static void Select(CampEnvironment environment) { Selected=environment; }
        public static void ReturnToStart() { Selected=CampEnvironment.None; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Selected=CampEnvironment.None; }
    }
}
