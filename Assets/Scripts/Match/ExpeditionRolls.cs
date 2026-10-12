using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Items;
using CyberExtraction.Enemies;

namespace CyberExtraction.Match
{
    // Call only from server encounter/loot creation. Outcomes are replicated, not rerolled by clients.
    public static class ExpeditionRolls
    {
        static readonly ItemId[] Weapons = { ItemId.Pistol, ItemId.Mosin, ItemId.DoubleBarrelShotgun };
        static readonly int[] WeaponWeights = { 60, 25, 15 };
        static readonly ItemId[] Ammunition = { ItemId.PistolAmmo, ItemId.RifleAmmo, ItemId.ShotgunShell };
        static readonly int[] AmmoWeights = { 50, 30, 20 };
        static readonly ItemId[] Supplies = { ItemId.Wood, ItemId.Nails, ItemId.Metal, ItemId.Gunpowder,
            ItemId.AmmoComponent, ItemId.Bandage, ItemId.Lockpick,ItemId.BoltCutters,ItemId.Crowbar,ItemId.HandSaw,ItemId.ServiceKey,ItemId.DepotKey };
        static readonly int[] SupplyWeights = { 20, 20, 20, 12, 12, 12, 4, 3, 3, 3, 2, 2 };
        static readonly MonsterKind[] Monsters = { MonsterKind.Charger, MonsterKind.Listener, MonsterKind.Shell,
            MonsterKind.Weaver, MonsterKind.Herald, MonsterKind.Acid, MonsterKind.Stalker, MonsterKind.Devourer };
        static readonly int[] MonsterWeights = { 12, 20, 14, 18, 8, 12, 12, 4 };

        public static bool Chance(int percent) => Random.Range(0,100)<Mathf.Clamp(percent,0,100);

        public static byte Rarity()
        {
            int roll=Random.Range(0,100);
            return roll<65 ? (byte)0 : roll<90 ? (byte)1 : (byte)2;
        }

        public static MonsterKind Monster()
        {
            int roll=Random.Range(0,100);
            for(int i=0;i<Monsters.Length;i++) { if(roll<MonsterWeights[i]) return Monsters[i]; roll-=MonsterWeights[i]; }
            return MonsterKind.Charger;
        }

        public static ItemStack[] Loot(int presenceChance,byte rarity=0)
        {
            if(!Chance(presenceChance)) return System.Array.Empty<ItemStack>();
            var config=GameplayDefinition.Current;
            int three=Mathf.Clamp(config.ThreeLootItemsChancePercent,0,100);
            int two=Mathf.Clamp(config.TwoLootItemsChancePercent,0,100-three);
            int amountRoll=Random.Range(0,100);
            int count=amountRoll<three ? 3 : amountRoll<three+two ? 2 : 1;
            var result=new ItemStack[count]; var used=new bool[ItemCatalog.Count];
            int tier=Mathf.Clamp((int)rarity,0,2);
            int weaponChance=Mathf.Clamp(config.LootWeaponChancePercent+tier*5,0,100);
            int ammoChance=Mathf.Clamp(config.LootAmmoChancePercent,0,100-weaponChance);
            for(int i=0;i<count;i++)
            {
                int category=Random.Range(0,100);
                ItemId item=category<weaponChance ? Pick(Weapons,WeaponWeights,used) :
                    category<weaponChance+ammoChance ? Pick(Ammunition,AmmoWeights,used) : Pick(Supplies,SupplyWeights,used);
                used[(int)item]=true;
                int quantity=1,data=0;
                var weapon=WeaponCatalog.Get(item);
                if(weapon!=null) data=Random.Range(0,weapon.Capacity+1);
                else switch(item)
                {
                    case ItemId.Wood: quantity=Random.Range(3,8)+tier; break;
                    case ItemId.Nails: quantity=Random.Range(3,9)+tier; break;
                    case ItemId.Metal: quantity=Random.Range(2,6)+tier; break;
                    case ItemId.Gunpowder: quantity=Random.Range(1,3)+tier; break;
                    case ItemId.AmmoComponent: quantity=Random.Range(1,4); break;
                    case ItemId.Bandage: quantity=Random.Range(1,3); break;
                    case ItemId.PistolAmmo: quantity=Random.Range(6,13)+tier*2; break;
                    case ItemId.RifleAmmo: quantity=Random.Range(3,6)+tier; break;
                    case ItemId.ShotgunShell: quantity=Random.Range(2,5)+tier; break;
                }
                result[i]=new ItemStack(item,Mathf.Min(quantity,ItemCatalog.Limit(item)),0,data);
            }
            return result;
        }

        static ItemId Pick(ItemId[] items,int[] weights,bool[] used)
        {
            int total=0;
            for(int i=0;i<items.Length;i++) if(!used[(int)items[i]]) total+=weights[i];
            int roll=Random.Range(0,total);
            for(int i=0;i<items.Length;i++)
            {
                if(used[(int)items[i]]) continue;
                if(roll<weights[i]) return items[i];
                roll-=weights[i];
            }
            // Every pool has at least three entries and a bundle has at most three draws.
            return items[0];
        }
    }
}
