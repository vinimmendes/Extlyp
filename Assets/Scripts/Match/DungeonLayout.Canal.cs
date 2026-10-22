using UnityEngine;

namespace CyberExtraction.Match
{
    public static partial class DungeonLayout
    {
        // Authored coordinates relative to the centre of the 128-cell map.
        public static Vector2 CanalPoint(float x,float y)=>new Vector2(x,y)*CellSize;
        static RectInt CanalCells(int x,int y,int width,int height)=>new RectInt(x+64,y+64,width,height);
        public static readonly RectInt CanalArea=CanalCells(30,-64,34,50);
        public static readonly RectInt CanalEastRiver=CanalCells(32,-28,32,8);
        public static readonly RectInt CanalSouthRiver=CanalCells(54,-64,8,44);
        public static readonly RectInt CanalBridge=CanalCells(42,-28,4,8);
        public static readonly RectInt CanalDetour=CanalCells(24,-52,6,40);
        public static readonly RectInt CanalBoarding=CanalCells(54,-51,4,2);
        public static readonly Vector2 CanalControl=CanalPoint(47,-18);
        public static readonly Vector2 CanalDeparture=Vector2.down*(16*CellSize);
        public static bool IsCanalBridge(int x,int y)=>CanalBridge.Contains(new Vector2Int(x,y));
        public static bool CanalReserved(Rect rect)=>rect.Overlaps(WorldRect(CanalArea)) || rect.Overlaps(WorldRect(CanalDetour));

        static void BuildCanalGeometry()
        {
            // Remove the old basin, automatic corridor shortcuts and stale obstacle masks.
            Covers.RemoveAll(r=>r.Overlaps(CanalArea));
            Pillars.RemoveAll(r=>r.Overlaps(CanalArea));
            foreach(var p in CanalArea.allPositionsWithin)
            { int i=Index(p.x,p.y); floor[i]=waterCells[i]=lowCover[i]=false; sectors[i]=4; }
            Paint(CanalDetour,4);
            Paint(CanalCells(24,-16,20,4),4); // northern access to the long gallery
            Paint(CanalCells(40,-20,18,6),4); // dry landing and original canal entrance
            Paint(CanalCells(40,-40,8,12),4); // protected southern approach from the bridge
            Paint(Rooms[13].Cells,4);
            Paint(CanalCells(24,-52,20,4),4); // long gallery returns to the western quay
            foreach(var rect in new[]{CanalEastRiver,CanalSouthRiver})
                foreach(var p in rect.allPositionsWithin)
                { int i=Index(p.x,p.y); floor[i]=lowCover[i]=false; waterCells[i]=true; sectors[i]=4; }
            // Only the fixed moored deck is walkable on the river; it is not a water bypass.
            Paint(CanalBoarding,4);
            AddObstacle(CanalCells(38,-18,2,2),false);
            AddObstacle(CanalCells(40,-35,2,2),false);
            AddObstacle(CanalCells(24,-36,2,2),false);
        }
    }
}
