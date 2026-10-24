using UnityEngine;

namespace CyberExtraction.Match
{
    public enum DungeonElementKind { Visual, Point, Region, Room, FunctionalVisual }
    public sealed class DungeonAuthoringElement : MonoBehaviour
    {
        public DungeonElementKind Kind;
        public string Id,AssetId,Group;
        public string Family;
        public int Variant=1;
        public bool Hidden,Carrier;
        public int Room=-1;
        public bool CellCoordinates;
        public Vector2 Size=Vector2.one;
        [HideInInspector] public Vector2 ImportedSpriteSize;
        public int Sector;
        public string RoomName;
        void OnDrawGizmosSelected()
        {
            if(Kind==DungeonElementKind.Visual) return;
            Gizmos.color=Kind==DungeonElementKind.Point ? Color.cyan : Color.yellow;
            if(Kind==DungeonElementKind.Point) Gizmos.DrawWireSphere(transform.position,.4f);
            else { Gizmos.matrix=transform.localToWorldMatrix; Gizmos.DrawWireCube(Vector3.zero,Size); }
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position,Id);
            #endif
        }
    }
}
