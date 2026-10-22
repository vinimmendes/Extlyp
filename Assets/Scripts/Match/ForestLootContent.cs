using CyberExtraction.Items;

namespace CyberExtraction.Match
{
    public static class ForestLootContent
    {
        // MakeLoot appends one point near each participant spawn after the two points per ruin.
        // Reserve the first two of those existing points; do not add spawns or rely on a roll.
        public static bool TryObjective(int groundSlot,out ItemId item)
        {
            int first=MapBuilder.Sites.Length*2;
            item=groundSlot==first ? ItemId.ServiceKey : ItemId.DepotKey;
            return groundSlot==first || groundSlot==first+1;
        }
    }
}
