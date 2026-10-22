using System;
using UnityEngine;
using CyberExtraction.Match;

namespace CyberExtraction.Items
{
    public static class LootTables
    {
        const int SourceCount=5;
        static LootTableDefinition[] tables;
        public static ExpeditionMap CanonicalMap(ExpeditionMap map)
        {
            if(map==ExpeditionMap.Test) return ExpeditionMap.Large;
            if(map!=ExpeditionMap.Large && map!=ExpeditionMap.Dungeon) throw new ArgumentException("Mapa de loot invalido.");
            return map;
        }
        public static LootTableDefinition Get(LootContext context)
        {
            var map=CanonicalMap(context.Map);
            if((byte)context.Source>=SourceCount) throw new ArgumentException("Cadaveres de participantes nao usam tabelas de loot.");
            if(tables==null)
            {
                var loaded=new LootTableDefinition[SourceCount*2];
                foreach(var table in Resources.LoadAll<LootTableDefinition>("Items/Loot/Tables"))
                {
                    Validate(table);
                    int index=((int)table.Map-1)*SourceCount+(int)table.Source;
                    if(loaded[index]!=null) throw new InvalidOperationException("Tabela de loot duplicada: "+table.name);
                    loaded[index]=table;
                }
                for(int i=0;i<loaded.Length;i++) if(loaded[i]==null) throw new InvalidOperationException("Tabela de loot ausente: "+i);
                tables=loaded;
            }
            return tables[((int)map-1)*SourceCount+(int)context.Source];
        }
        public static bool CategoryAllows(LootCategory category,ItemId item,ExpeditionMap map)
        {
            if(!ItemCatalog.Available(item)) return false;
            switch(category)
            {
                case LootCategory.Materials: return item==ItemId.Wood || item==ItemId.Nails || item==ItemId.Metal || item==ItemId.Gunpowder || item==ItemId.AmmoComponent;
                case LootCategory.Supplies: return item==ItemId.Bandage || ItemCatalog.Ammo(item) || item==ItemId.Lockpick ||
                    map==ExpeditionMap.Large && (item==ItemId.BoltCutters || item==ItemId.Crowbar || item==ItemId.HandSaw);
                case LootCategory.Equipment: return EquipmentCatalog.IsDefined(item);
                case LootCategory.Weapons: return ItemCatalog.Weapon(item);
                case LootCategory.Treasures: return TreasureCatalog.IsDefined(item) && TreasureCatalog.MapFor(item)==map;
                default: return false;
            }
        }
        public static void Validate(LootTableDefinition table)
        {
            if(table==null || table.Map!=ExpeditionMap.Large && table.Map!=ExpeditionMap.Dungeon || (byte)table.Source>=SourceCount ||
                table.PresencePercent<0 || table.PresencePercent>100 || table.MinimumStacks<1 || table.MaximumStacks<table.MinimumStacks ||
                table.MaximumStacks>ItemCatalog.InventorySlots || table.Categories==null || table.Categories.Length!=5 || table.Entries==null || table.Rarities==null || table.Rarities.Length!=3)
                throw new InvalidOperationException("Tabela de loot invalida: "+(table!=null ? table.name : "null"));
            var categories=new bool[5]; int total=0;
            foreach(var category in table.Categories)
            {
                if((byte)category.Category>=5 || categories[(int)category.Category] || category.Weight<0 || category.Weight>10000)
                    throw new InvalidOperationException("Pesos de categoria invalidos: "+table.name);
                categories[(int)category.Category]=true; total+=category.Weight;
            }
            if(total==0) throw new InvalidOperationException("Tabela sem categorias: "+table.name);
            var used=new bool[ItemCatalog.Count]; bool equipment=false;
            foreach(var entry in table.Entries)
            {
                if(!ItemCatalog.Valid(entry.Item) || !CategoryAllows(entry.Category,entry.Item,table.Map) || used[(int)entry.Item] ||
                    entry.Weight<1 || entry.Weight>10000 || entry.Minimum<1 || entry.Maximum<entry.Minimum || entry.Maximum>ItemCatalog.Limit(entry.Item) ||
                    entry.Themes==0 || (byte)entry.Themes>(byte)LootTheme.All)
                    throw new InvalidOperationException("Entrada de loot invalida: "+table.name+" / "+entry.Item);
                used[(int)entry.Item]=true; equipment|=entry.Category==LootCategory.Equipment;
            }
            if(table.GuaranteeEquipment && !equipment) throw new InvalidOperationException("Garantia sem equipamento elegivel: "+table.name);
            foreach(var row in table.Rarities)
                if(row.Common<0 || row.Uncommon<0 || row.Rare<0 || row.Common>100 || row.Uncommon>100 || row.Rare>100 || row.Common+row.Uncommon+row.Rare!=100)
                    throw new InvalidOperationException("Raridades invalidas: "+table.name);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { tables=null; }
    }
}
