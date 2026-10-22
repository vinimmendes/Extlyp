using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Combat
{
    public partial class LootPickup
    {
        // The current chest art faces north at zero rotation. Free chests face south.
        public NetworkVariable<float> ChestFacing=new NetworkVariable<float>(180);
        BoxCollider2D chestCollider;

        void InitializeChestCollision()
        {
            // Keep the pickup trigger separate. Low obstacles block movement, not vision.
            // Parent scale gives this unit box the same 1.5 x 1.2 footprint as the body;
            // the animated lid does not change the solid area or chest interaction.
            if(chestCollider==null)
            {
                var body=new GameObject("ChestCollision");
                body.layer=GameConfig.LowObstacleLayer;
                body.transform.SetParent(transform,false);
                chestCollider=body.AddComponent<BoxCollider2D>();
                chestCollider.size=Vector2.one;
                chestCollider.isTrigger=false;
            }
            chestCollider.enabled=true;
        }

        void InitializeChestFacing()
        {
            if(!Chest.Value || FixtureKind.Value!=0) return;
            if(IsServer)
            {
                // Map generation can create/move wall colliders in the same frame.
                Physics2D.SyncTransforms();
                ChestFacing.Value=ChestOrientation.AwayFromSupport(transform);
            }
            ChestFacing.OnValueChanged+=ChestFacingChanged;
            ChestFacingChanged(ChestFacing.Value,ChestFacing.Value);
        }
        void ChestFacingChanged(float previous,float current)
        { transform.rotation=Quaternion.Euler(0,0,current); }
    }

    internal static class ChestOrientation
    {
        // Depth is 1.2 world units. A support must be within 0.2 of its back edge;
        // nearby objects across an aisle are not treated as attached walls.
        const float SupportReach=.6f+.2f;
        public static float AwayFromSupport(Transform chest)
        {
            Vector2 center=chest.position;
            float nearest=SupportReach,bestAngle=180;
            bool found=false;
            foreach(var obstacle in Physics2D.OverlapCircleAll(center,SupportReach,GameConfig.WallMask))
            {
                if(obstacle.isTrigger || obstacle.transform==chest || obstacle.transform.IsChildOf(chest)) continue;
                if(obstacle.attachedRigidbody!=null && obstacle.attachedRigidbody.bodyType!=RigidbodyType2D.Static) continue;
                Vector2 away=center-obstacle.ClosestPoint(center);
                float distance=away.magnitude;
                if(distance<.001f || distance>SupportReach) continue;
                float angle=Mathf.Repeat(Mathf.Atan2(away.y,away.x)*Mathf.Rad2Deg-90,360);
                // Stable tie-break at corners, independent of Physics query ordering.
                if(!found || distance<nearest-.0001f || Mathf.Abs(distance-nearest)<=.0001f && angle<bestAngle)
                { found=true; nearest=distance; bestAngle=angle; }
            }
            return bestAngle;
        }
    }
}
