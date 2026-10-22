using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Items
{
    public enum ItemCategory : byte { Material, Supply, Weapon, Equipment, Treasure, Objective }

    // Shared authoring data. Rolled bonuses and ownership live in ItemInstance, never here.
    [CreateAssetMenu(fileName="ItemDefinition",menuName="Cyber Extraction/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        public ItemId Id;
        public string DisplayName;
        public ItemCategory Category;
        public EquipmentSlot Slot;
        public AttributeId PrimaryAttribute;
        public AttributeId[] SecondaryPool=System.Array.Empty<AttributeId>();
        public Sprite Icon;
        [Min(0)] public int SellPrice;
    }
}
