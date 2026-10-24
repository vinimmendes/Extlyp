using UnityEngine;

namespace CyberExtraction.Match
{
    public static class DungeonArtLifetime
    {
        // Temporary source art is also built during migration outside Play.
        public static void Release(Object value)
        { if(value==null) return; if(Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
