using UnityEngine;

namespace CyberExtraction.Match
{
    public sealed class DungeonLegacySource : MonoBehaviour
    {
        public MapVisualEdit Original;
        [TextArea(2,6)] public string Resolution;
    }
}
