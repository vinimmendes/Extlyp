using UnityEngine;
using UnityEngine.Tilemaps;

namespace CyberExtraction.Match
{
    [RequireComponent(typeof(Tilemap))]
    public sealed class DungeonTerrainSource : MonoBehaviour
    {
        public int Priority;
        public bool VisualSurface;
        public int SurfaceOrder=-7;
        public string SurfaceFamily="";
    }
}
