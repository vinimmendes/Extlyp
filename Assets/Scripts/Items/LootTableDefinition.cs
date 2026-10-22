using System;
using UnityEngine;
using CyberExtraction.Match;

namespace CyberExtraction.Items
{
    public enum LootSource : byte { Ground, Supplies, CommonContainer, ValuableContainer, Monster, ParticipantCorpse }
    public enum LootRisk : byte { Low, Medium, High }
    public enum LootCategory : byte { Materials, Supplies, Equipment, Weapons, Treasures }
    [Flags] public enum LootTheme : byte { General=1, Armored=2, Organic=4, Corrosive=8, All=15 }

    public readonly struct LootContext
    {
        public readonly ExpeditionMap Map;
        public readonly LootSource Source;
        public readonly LootRisk Risk;
        public readonly LootTheme Theme;
        public LootContext(ExpeditionMap map,LootSource source,LootRisk risk=LootRisk.Low,LootTheme theme=LootTheme.General)
        { Map=map; Source=source; Risk=risk; Theme=theme; }
    }
    [Serializable] public struct LootCategoryWeight
    {
        public LootCategory Category;
        [Min(0)] public int Weight;
    }
    [Serializable] public struct LootEntry
    {
        public ItemId Item;
        public LootCategory Category;
        [Min(1)] public int Weight,Minimum,Maximum;
        public LootTheme Themes;
    }
    [Serializable] public struct LootRarityWeights
    {
        [Range(0,100)] public int Common,Uncommon,Rare;
    }
    [CreateAssetMenu(fileName="LootTable",menuName="Cyber Extraction/Loot Table")]
    public sealed class LootTableDefinition : ScriptableObject
    {
        public ExpeditionMap Map;
        public LootSource Source;
        [Range(0,100)] public int PresencePercent;
        [Range(1,9)] public int MinimumStacks=1,MaximumStacks=1;
        public bool GuaranteeEquipment;
        public LootCategoryWeight[] Categories=Array.Empty<LootCategoryWeight>();
        public LootEntry[] Entries=Array.Empty<LootEntry>();
        // Indexed by LootRisk. Container opening grade never indexes these rows.
        public LootRarityWeights[] Rarities=Array.Empty<LootRarityWeights>();
    }
}
