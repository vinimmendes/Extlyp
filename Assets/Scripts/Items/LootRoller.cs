using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random=UnityEngine.Random;

namespace CyberExtraction.Items
{
    public static class LootRoller
    {
        public static ItemStack[] Roll(LootContext context) => Roll(LootTables.Get(context),context);
        public static ItemStack[] Roll(LootTableDefinition table,LootContext context)
        {
            if(NetworkManager.Singleton==null || !NetworkManager.Singleton.IsServer) throw new InvalidOperationException("Loot so pode ser sorteado pelo servidor.");
            if(table==null || table.Map!=LootTables.CanonicalMap(context.Map) || table.Source!=context.Source || (byte)context.Risk>2 ||
                context.Theme!=LootTheme.General && context.Theme!=LootTheme.Armored && context.Theme!=LootTheme.Organic && context.Theme!=LootTheme.Corrosive)
                throw new ArgumentException("Contexto nao corresponde a tabela de loot.");
            // Referenced authoring assets are validated too, even when they bypass the registry.
            LootTables.Validate(table);
            if(Random.Range(0,100)>=table.PresencePercent) return Array.Empty<ItemStack>();
            int count=Random.Range(table.MinimumStacks,table.MaximumStacks+1);
            var result=new List<ItemStack>(count); var used=new bool[ItemCatalog.Count];
            bool weapon=false,equipment=false;
            if(table.GuaranteeEquipment)
            {
                if(!TryPick(table,LootCategory.Equipment,context.Theme,used,out var guaranteed))
                    throw new InvalidOperationException("Garantia sem equipamento para o tema: "+table.name);
                Add(guaranteed);
            }
            while(result.Count<count)
            {
                int total=0;
                foreach(var category in table.Categories) if(Eligible(category.Category)) total+=category.Weight;
                if(total==0) break;
                int roll=Random.Range(0,total); LootCategory selected=default;
                foreach(var category in table.Categories)
                {
                    if(!Eligible(category.Category)) continue;
                    if(roll<category.Weight) { selected=category.Category; break; }
                    roll-=category.Weight;
                }
                if(!TryPick(table,selected,context.Theme,used,out var entry)) break;
                Add(entry);
            }
            return result.ToArray();

            bool Eligible(LootCategory category) => !(category==LootCategory.Weapons && weapon || category==LootCategory.Equipment && equipment) &&
                HasEntry(table,category,context.Theme,used);
            void Add(LootEntry entry)
            {
                used[(int)entry.Item]=true;
                weapon|=entry.Category==LootCategory.Weapons; equipment|=entry.Category==LootCategory.Equipment;
                if(entry.Category==LootCategory.Equipment)
                {
                    var row=table.Rarities[(int)context.Risk]; int rarity=Random.Range(0,100);
                    result.Add(EquipmentCatalog.Create(entry.Item,rarity<row.Common ? ItemRarity.Common : rarity<row.Common+row.Uncommon ? ItemRarity.Uncommon : ItemRarity.Rare));
                }
                else
                {
                    var definition=WeaponCatalog.Get(entry.Item);
                    int rounds=definition!=null && !definition.IsMelee ? Random.Range(0,definition.Capacity+1) : 0;
                    result.Add(new ItemStack(entry.Item,Random.Range(entry.Minimum,entry.Maximum+1),0,rounds));
                }
            }
        }
        static bool HasEntry(LootTableDefinition table,LootCategory category,LootTheme theme,bool[] used)
        {
            foreach(var entry in table.Entries) if(entry.Category==category && !used[(int)entry.Item] && (entry.Themes&theme)!=0) return true;
            return false;
        }
        static bool TryPick(LootTableDefinition table,LootCategory category,LootTheme theme,bool[] used,out LootEntry selected)
        {
            int total=0;
            foreach(var entry in table.Entries) if(entry.Category==category && !used[(int)entry.Item] && (entry.Themes&theme)!=0) total+=entry.Weight;
            if(total>0)
            {
                int roll=Random.Range(0,total);
                foreach(var entry in table.Entries)
                {
                    if(entry.Category!=category || used[(int)entry.Item] || (entry.Themes&theme)==0) continue;
                    if(roll<entry.Weight) { selected=entry; return true; }
                    roll-=entry.Weight;
                }
            }
            selected=default; return false;
        }
    }
}
