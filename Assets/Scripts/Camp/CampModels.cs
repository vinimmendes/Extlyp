using System;
using System.Collections.Generic;
using CyberExtraction.Items;
namespace CyberExtraction.Camp
{
    public enum CampActionKind : byte { Repair, CraftAmmo, UpgradeRecycler, Transfer, Discard, CatchFish, CookFish, Eat, RecoverDrop, BuyComponent }
    [Serializable] public class CampGroundItem { public string Id; public ItemStack Stack; }
    [Serializable] public class CampAction
    {
        public string Id = Guid.NewGuid().ToString("N");
        public long Sequence;
        public string Reference;
        public CampActionKind Kind;
        public int Source, Target, SourceGrid, TargetGrid, Result;
        public float Duration;
        // Server-only completion feedback; not part of the serialized client command.
        internal string CookingStorage;
    }
    [Serializable]
    public class CampProgress
    {
        public const int PierWoodCost = 20, PierNailsCost = 10, WellFedHealth = 20;
        public int Wood, Nails, Bandages, RecyclerLevel, FoodHealth;
        public long Revision;
        public string ExpeditionId;
        public InventoryGrid Escrow;
        public int EscrowFood;
        public byte ExpeditionState;
        public bool PierRepaired, WellFed;
        public InventoryGrid Chest = new InventoryGrid(), Pack = InventoryGrid.Starter();
        public List<string> AppliedActions = new List<string>();
        public List<CampGroundItem> Ground = new List<CampGroundItem>();
        public void Ensure() { if (Chest == null) Chest = new InventoryGrid(); if (Pack == null) Pack = new InventoryGrid(); if (AppliedActions == null) AppliedActions = new List<string>(); if(Ground==null) Ground=new List<CampGroundItem>(); Sync(); }
        public void Sync() { Wood = Count(ItemId.Wood); Nails = Count(ItemId.Nails); Bandages = Count(ItemId.Bandage); }
        public int Count(ItemId item) => Chest.Count(item) + Pack.Count(item);
        public bool HasAnyWeapon
        {
            get
            {
                foreach(var stack in Pack.Slots) if(!stack.Empty && ItemCatalog.Weapon(stack.Item)) return true;
                foreach(var stack in Chest.Slots) if(!stack.Empty && ItemCatalog.Weapon(stack.Item)) return true;
                foreach(var drop in Ground) if(!drop.Stack.Empty && ItemCatalog.Weapon(drop.Stack.Item)) return true;
                return false;
            }
        }
        public void PrepareRecoveryEquipment()
        {
            Ensure();
            if(HasAnyWeapon) return;
            AddRecoveryStack(new ItemStack(ItemId.Pistol,1,0,CyberExtraction.Core.GameplayDefinition.Current.Magazine));
            AddRecoveryStack(new ItemStack(ItemId.PistolAmmo,WeaponCatalog.RecipeOutput(ItemId.PistolAmmo)));
            Sync();
        }
        void AddRecoveryStack(ItemStack stack)
        {
            // Preserve displaced items in the safe chest or camp ground, never delete loot.
            while(Pack.FreeFor(stack)<stack.Quantity)
            {
                int slot=Array.FindLastIndex(Pack.Slots,item=>!item.Empty && !ItemCatalog.Weapon(item.Item) && !item.Same(stack));
                if(slot<0) throw new InvalidOperationException("Nao foi possivel preparar o equipamento basico.");
                var displaced=Pack.Slots[slot];
                if(!Chest.Add(displaced)) Ground.Add(new CampGroundItem { Id=Guid.NewGuid().ToString("N"),Stack=displaced });
                Pack.Slots[slot]=default;
            }
            if(!Pack.Add(stack)) throw new InvalidOperationException("Nao foi possivel entregar o equipamento basico.");
        }
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
        public bool CraftAmmo(ItemId ammoItem = ItemId.PistolAmmo)
        {
            if (!ItemCatalog.Ammo(ammoItem)) return false;
            var recipe=CyberExtraction.Core.GameplayDefinition.Current;
            if (Count(ItemId.Metal) < recipe.MetalRecipeCost || Count(ItemId.Gunpowder) < recipe.PowderRecipeCost || Count(ItemId.AmmoComponent) < recipe.ComponentRecipeCost) return false;
            var chestBefore = Chest.Copy(); var packBefore = Pack.Copy();
            Take(ItemId.Metal, recipe.MetalRecipeCost); Take(ItemId.Gunpowder, recipe.PowderRecipeCost); Take(ItemId.AmmoComponent, recipe.ComponentRecipeCost);
            var ammo = new ItemStack(ammoItem, WeaponCatalog.RecipeOutput(ammoItem));
            if (!Chest.Add(ammo) && !Pack.Add(ammo)) { Chest = chestBefore; Pack = packBefore; Sync(); return false; }
            Sync(); return true;
        }
        public bool UpgradeRecycler()
        {
            int cost = (RecyclerLevel + 1) * 10;
            if (RecyclerLevel >= 3 || Count(ItemId.Metal) < cost) return false;
            Take(ItemId.Metal, cost); RecyclerLevel++; return true;
        }
        public bool BuyComponent()
        {
            int price=CyberExtraction.Core.GameplayDefinition.Current.ComponentPrice;
            if(Count(ItemId.Metal)<price) return false;
            var chestBefore=Chest.Copy(); var packBefore=Pack.Copy(); Take(ItemId.Metal,price);
            var component=new ItemStack(ItemId.AmmoComponent,1);
            if(!Chest.Add(component) && !Pack.Add(component)) { Chest=chestBefore; Pack=packBefore; Sync(); return false; }
            Sync(); return true;
        }
        public bool Fish() => false; // A pesca agora entrega um item; o buff vem da refeicao.
        public int TakeBandages(int amount) { int taken = Math.Min(Bandages, Math.Max(0, Math.Min(3, amount))); Take(ItemId.Bandage, taken); return taken; }
        public int ConsumeFoodBuff() { int health = WellFed ? (FoodHealth > 0 ? FoodHealth : WellFedHealth) : 0; WellFed = false; FoodHealth = 0; return health; }
        public CampProgress Copy() => UnityEngine.JsonUtility.FromJson<CampProgress>(UnityEngine.JsonUtility.ToJson(this));
        public bool Apply(CampAction action) => ApplyCommand(action, false);
        internal bool CompleteCooking(CampAction action) => ApplyCommand(action, true);
        public bool CompleteFishing(ItemId item)
        {
            if (!PierRepaired || !(ItemCatalog.Fish(item) || item == ItemId.Metal || item == ItemId.AmmoComponent) || !Pack.Add(new ItemStack(item, 1))) return false;
            Revision++; Sync(); return true;
        }
        bool ApplyCommand(CampAction action, bool serverActivity)
        {
            Ensure();
            if (action == null || !Guid.TryParseExact(action.Id, "N", out _) || action.Sequence != Revision + 1 ||
                action.SourceGrid < 0 || action.SourceGrid > 1 || action.TargetGrid < 0 || action.TargetGrid > 1 ||
                !Enum.IsDefined(typeof(CampActionKind), action.Kind) || ExpeditionState != 0) return false;
            // Completion is produced by the server's activity state machine, never a client command.
            if (action.Kind == CampActionKind.CatchFish || (action.Kind == CampActionKind.CookFish && !serverActivity)) return false;
            bool success = false;
            InventoryGrid source = action.SourceGrid == 0 ? Pack : Chest;
            InventoryGrid target = action.TargetGrid == 0 ? Pack : Chest;
            if (action.Kind == CampActionKind.Repair) success = RepairPier();
            else if (action.Kind == CampActionKind.CraftAmmo) success = CraftAmmo(action.Result == 0 ? ItemId.PistolAmmo : (ItemId)action.Result);
            else if (action.Kind == CampActionKind.UpgradeRecycler) success = UpgradeRecycler();
            else if (action.Kind == CampActionKind.BuyComponent) success = BuyComponent();
            else if (action.Kind == CampActionKind.Transfer) success = source.MoveTo(action.Source, target, action.Target);
            else if (action.Kind == CampActionKind.Discard && action.Source >= 0 && action.Source < 9 && !source.Slots[action.Source].Empty)
            { Ground.Add(new CampGroundItem { Id=action.Id,Stack=source.Slots[action.Source] }); source.Slots[action.Source] = default; success = true; }
            else if (action.Kind == CampActionKind.RecoverDrop)
            {
                var drop=Ground.Find(item=>item.Id==action.Reference);
                if(drop!=null && Pack.Add(drop.Stack)) { Ground.Remove(drop); success=true; }
            }
            else if (action.Kind == CampActionKind.CookFish && action.Source >= 0 && action.Source < 9 && action.Duration >= .1f && action.Duration <= 90 && action.Result >= 0 && action.Result <= 3)
            {
                var fish = source.Slots[action.Source];
                if (!fish.Empty && (ItemCatalog.Fish(fish.Item) || fish.Item==ItemId.RawFish))
                {
                    fish.Quantity--; source.Slots[action.Source] = fish.Empty ? default : fish;
                    ItemId result = action.Result == 0 ? ItemId.RawFish : action.Result == 1 ? ItemId.BurntFish : ItemId.CookedFish;
                    byte quality = (byte)Math.Min(3, Math.Max(0, action.Result));
                    var cooked=new ItemStack(result,1,quality,fish.Item==ItemId.RawFish ? fish.Data : (int)fish.Item);
                    // A completed camp QTE must not fail because its new quality/species
                    // needs another slot. Use the existing camp overflow destinations.
                    if(source.Add(cooked)) action.CookingStorage=action.SourceGrid==0 ? "mochila" : "bau seguro";
                    else if(Chest.Add(cooked)) action.CookingStorage="bau seguro";
                    else
                    {
                        Ground.Add(new CampGroundItem { Id=action.Id,Stack=cooked });
                        action.CookingStorage="chao do acampamento";
                    }
                    success=true;
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
            if (success) { Revision = action.Sequence; AppliedActions.Add(action.Id); if (AppliedActions.Count > 256) AppliedActions.RemoveAt(0); Sync(); }
            return success;
        }
        static int StableSeed(string id) { unchecked { int hash = 17; foreach (char c in id) hash = hash * 31 + c; return hash; } }
        public static ItemId CatchItem(string id)
        {
            int roll=new Random(StableSeed(id)).Next(100);
            return roll<8 ? ItemId.AmmoComponent : roll<15 ? ItemId.Metal : roll<25 ? ItemId.FishRare : roll<40 ? ItemId.FishLarge : ItemId.FishCommon;
        }
    }
}
