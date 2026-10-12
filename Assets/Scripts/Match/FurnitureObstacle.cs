using UnityEngine;
using CyberExtraction.Combat;

namespace CyberExtraction.Match
{
    // Map geometry persists even when the network container is outside observation.
    public sealed class FurnitureObstacle : MonoBehaviour
    {
        public LootPickup Container;
        public bool BelongsTo(Vector2 position) => ((Vector2)transform.position-position).sqrMagnitude<.01f;
    }
}
