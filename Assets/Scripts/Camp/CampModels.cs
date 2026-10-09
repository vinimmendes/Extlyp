using System;
using System.Collections.Generic;
using CyberExtraction.Items;
namespace CyberExtraction.Camp
{
    public enum CampActionKind : byte { Repair, CraftAmmo, UpgradeRecycler, Transfer, Discard, CatchFish, CookFish, Eat }
    [Serializable] public class CampAction
    {
        public string Id = Guid.NewGuid().ToString("N");
        public CampActionKind Kind;
        public int Source, Target, SourceGrid, TargetGrid, Result;
        public float Duration;
    }
    [Serializable]
    public class CampProgress
    {
        public const int PierWoodCost = 20, PierNailsCost = 10, WellFedHealth = 20;
        public int Wood, Nails, Bandages, RecyclerLevel, FoodHealth;
        public bool PierRepaired, WellFed;
        public InventoryGrid Chest = new InventoryGrid(), Pack = InventoryGrid.Starter();
        public List<string> AppliedActions = new List<string>();
        public void Ensure() { if (Chest == null) Chest = new InventoryGrid(); if (Pack == null) Pack = new InventoryGrid(); if (AppliedActions == null) AppliedActions = new List<string>(); Sync(); }
        public void Sync() { Wood = Count(ItemId.Wood); Nails = Count(ItemId.Nails); Bandages = Count(ItemId.Bandage); }
        public int Count(ItemId item) => Chest.Count(item) + Pack.Count(item);
        public bool Take(ItemId item, int amount)
        {
            if (amount < 0 || Count(item) < amount) return false;
            int fromChest = Math.Min(Chest.Count(item), amount); Chest.Remove(item, fromChest); Pack.Remove(item, amount - fromChest); Sync(); return true;
        }
        public void Deposit(int wood, int nails, int bandages)
        { Chest.Add(new ItemStack(ItemId.Wood, wood)); Chest.Add(new ItemStack(ItemId.Nails, nails)); Chest.Add(new ItemStack(ItemId.Bandage, bandages)); Sync(); }
        public bool RepairPier()
        {
            if (PierRepaired || Count(ItemId.Wood) < 20 || Count(ItemId.Nails) < 10) return false;
            Take(ItemId.Wood, 20); Take(ItemId.Nails, 10); PierRepaired = true; return true;
        }
        public bool CraftAmmo()
        {
            if (Count(ItemId.Metal) < 3 || Count(ItemId.Gunpowder) < 1 || Count(ItemId.AmmoComponent) < 1) return false;
            var chestBefore = Chest.Copy(); var packBefore = Pack.Copy();
            Take(ItemId.Metal, 3); Take(ItemId.Gunpowder, 1); Take(ItemId.AmmoComponent, 1);
            var ammo = new ItemStack(ItemId.PistolAmmo, 12);
            if (!Chest.Add(ammo) && !Pack.Add(ammo)) { Chest = chestBefore; Pack = packBefore; Sync(); return false; }
            Sync(); return true;
        }
        public bool UpgradeRecycler()
        {
            int cost = (RecyclerLevel + 1) * 10;
            if (RecyclerLevel >= 3 || Count(ItemId.Metal) < cost) return false;
            Take(ItemId.Metal, cost); RecyclerLevel++; return true;
        }
        public bool Fish() => false; // A pesca agora entrega um item; o buff vem da refeicao.
        public int TakeBandages(int amount) { int taken = Math.Min(Bandages, Math.Max(0, Math.Min(3, amount))); Take(ItemId.Bandage, taken); return taken; }
        public int ConsumeFoodBuff() { int health = WellFed ? (FoodHealth > 0 ? FoodHealth : WellFedHealth) : 0; WellFed = false; FoodHealth = 0; return health; }
        public CampProgress Copy() => UnityEngine.JsonUtility.FromJson<CampProgress>(UnityEngine.JsonUtility.ToJson(this));
        public bool Apply(CampAction action)
        {
            Ensure();
            if (action == null || !Guid.TryParseExact(action.Id, "N", out _) || AppliedActions.Contains(action.Id)) return false;
            bool success = false;
            InventoryGrid source = action.SourceGrid == 0 ? Pack : Chest;
            InventoryGrid target = action.TargetGrid == 0 ? Pack : Chest;
            if (action.Kind == CampActionKind.Repair) success = RepairPier();
            else if (action.Kind == CampActionKind.CraftAmmo) success = CraftAmmo();
            else if (action.Kind == CampActionKind.UpgradeRecycler) success = UpgradeRecycler();
            else if (action.Kind == CampActionKind.Transfer) success = source.MoveTo(action.Source, target, action.Target);
            else if (action.Kind == CampActionKind.Discard && action.Source >= 0 && action.Source < 9) { source.Slots[action.Source] = default; success = true; }
            else if (action.Kind == CampActionKind.CatchFish && PierRepaired && action.Duration >= 4 && action.Duration <= 120)
            {
                var random = new Random(StableSeed(action.Id)); int roll = random.Next(100);
                ItemId item = roll < 8 ? ItemId.AmmoComponent : roll < 15 ? ItemId.Metal : roll < 25 ? ItemId.FishRare : roll < 40 ? ItemId.FishLarge : ItemId.FishCommon;
                success = Pack.Add(new ItemStack(item, 1));
            }
            else if (action.Kind == CampActionKind.CookFish && action.Source >= 0 && action.Source < 9 && action.Duration >= 6 && action.Duration <= 90)
            {
                var fish = source.Slots[action.Source];
                if (!fish.Empty && ItemCatalog.Fish(fish.Item))
                {
                    var before = source.Copy(); fish.Quantity--; source.Slots[action.Source] = fish.Empty ? default : fish;
                    ItemId result = action.Result == 0 ? ItemId.RawFish : action.Result == 1 ? ItemId.BurntFish : ItemId.CookedFish;
                    byte quality = (byte)Math.Min(3, Math.Max(0, action.Result));
                    success = source.Add(new ItemStack(result, 1, quality, (int)fish.Item));
                    if (!success) source.Slots = before.Slots;
                }
            }
            else if (action.Kind == CampActionKind.Eat && action.Source >= 0 && action.Source < 9)
            {
                var food = source.Slots[action.Source];
                if (!food.Empty && food.Item == ItemId.CookedFish)
                {
                    food.Quantity--; source.Slots[action.Source] = food.Empty ? default : food;
                    WellFed = true; FoodHealth = food.Quality >= 3 ? 25 : 20; success = true;
                }
            }
            if (success) { AppliedActions.Add(action.Id); if (AppliedActions.Count > 256) AppliedActions.RemoveAt(0); Sync(); }
            return success;
        }
        static int StableSeed(string id) { unchecked { int hash = 17; foreach (char c in id) hash = hash * 31 + c; return hash; } }
    }
}
