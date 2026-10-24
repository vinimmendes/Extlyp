using System;
using UnityEngine;

namespace CyberExtraction.Match
{
    // Compatibility for the original generated vigil compositions. The recess
    // already contains its stone rim; the added full slab covered that rim.
    // Projected hanging cloth, like the candles, must remain camera-facing.
    public static class DungeonVotivePresentation
    {
        public static bool NeedsLegacyFix(DungeonMapDocument data)
        {
            foreach(var v in data.visuals)
                if(!v.hidden && ObsoleteLedge(v.id,v.group,v.asset,v.size)) return true;
            return false;
        }
        static bool Vigil(string group)=>group!=null && group.EndsWith("_Vigilia",StringComparison.Ordinal);
        public static bool ObsoleteLedge(string id,string group,string asset,Vector2 size)=>
            Vigil(group) && id!=null && id.EndsWith("_FrontLedge",StringComparison.Ordinal) &&
            asset=="EmptyFlatShelf" && Mathf.Abs(size.y-.28f)<.001f;
        public static bool ProjectedCloth(string id,string group,string asset)=>
            Vigil(group) && id!=null && id.EndsWith("_DrapedRemnant",StringComparison.Ordinal) && asset=="HangingMourningCloth";
        public static bool GeneratedNorthCloth(string id,string group,string asset)=>
            ProjectedCloth(id,group,asset) && id.IndexOf("_NorthVigil_0_",StringComparison.Ordinal)>=0;
        public static bool DuplicateNorthCloth(DungeonVisualRecord v,DungeonMapDocument data)=>
            GeneratedNorthCloth(v.id,v.group,v.asset) && Array.Exists(data.visuals,o=>
                o.id=="R"+v.room.ToString("00")+"_MourningCloth" && o.asset=="HangingMourningCloth" && !o.hidden);
    }
}
