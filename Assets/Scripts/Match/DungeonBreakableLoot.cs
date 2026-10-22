using System;
using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Items;

namespace CyberExtraction.Match
{
    // Configuration only. Rolled rewards are stored on the server's object.
    [CreateAssetMenu(fileName="BreakableLoot",menuName="Cyber Extraction/Dungeon Breakable Loot")]
    public sealed class DungeonBreakableLoot : ScriptableObject
    {
        [Serializable] public struct Entry
        {
            public ItemId Item;
            [Min(1)] public int Weight,Minimum,Maximum;
        }
        [Range(0,100)] public int ChancePercent=10;
        public Entry[] Entries=Array.Empty<Entry>();
        public bool Valid()
        {
            if(ChancePercent<0 || ChancePercent>100 || Entries==null || Entries.Length!=8) return false;
            var seen=new HashSet<ItemId>();
            foreach(var entry in Entries)
                if(!Common(entry.Item) || !seen.Add(entry.Item) || entry.Weight<1 || entry.Weight>10000 ||
                    entry.Minimum<1 || entry.Maximum<entry.Minimum || entry.Maximum>ItemCatalog.Limit(entry.Item)) return false;
            return true;
        }
        public static bool Common(ItemId item) => item==ItemId.Wood || item==ItemId.Nails || item==ItemId.Metal ||
            item==ItemId.CrossbowBolt || item==ItemId.Gunpowder || item==ItemId.AmmoComponent || item==ItemId.Bandage;
        // Caller validates the configuration once before spawning the batch.
        public ItemStack Roll(System.Random random)
        {
            if(random.Next(100)>=ChancePercent) return default;
            int total=0; foreach(var entry in Entries) total+=entry.Weight;
            int roll=random.Next(total);
            foreach(var entry in Entries)
            {
                if(roll<entry.Weight) return new ItemStack(entry.Item,random.Next(entry.Minimum,entry.Maximum+1));
                roll-=entry.Weight;
            }
            return default;
        }
    }
}
