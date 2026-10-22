using System;
using UnityEngine;
using CyberExtraction.Match;

namespace CyberExtraction.Items
{
    public static class TreasureCatalog
    {
        static ItemDefinition[] definitions;
        public static bool IsDefined(ItemId item) => item>=ItemId.AncientCoin && item<=ItemId.CollectorsDocument;
        public static ExpeditionMap MapFor(ItemId item) => item<=ItemId.FuneraryIdol ? ExpeditionMap.Dungeon : ExpeditionMap.Large;
        public static ItemDefinition Get(ItemId item)
        {
            if(!IsDefined(item)) return null;
            if(definitions==null)
            {
                var loaded=new ItemDefinition[6];
                foreach(var definition in Resources.LoadAll<ItemDefinition>("Items/Treasure/Definitions"))
                {
                    if(!IsDefined(definition.Id) || definition.Category!=ItemCategory.Treasure || definition.Slot!=EquipmentSlot.None ||
                        string.IsNullOrEmpty(definition.DisplayName) || definition.SellPrice<=0 || definition.SecondaryPool==null || definition.SecondaryPool.Length!=0)
                        throw new InvalidOperationException("Tesouro invalido: "+definition.name);
                    int index=(int)definition.Id-(int)ItemId.AncientCoin;
                    if(loaded[index]!=null) throw new InvalidOperationException("Tesouro duplicado: "+definition.name);
                    loaded[index]=definition;
                }
                for(int i=0;i<loaded.Length;i++) if(loaded[i]==null) throw new InvalidOperationException("Tesouro ausente: "+i);
                definitions=loaded;
            }
            return definitions[(int)item-(int)ItemId.AncientCoin];
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { definitions=null; }
    }
}
