using UnityEngine;
namespace CyberExtraction.Items
{
    public enum ItemId : byte { Wood, Nails, Bandage, Metal, Gunpowder, AmmoComponent, Pistol, PistolAmmo, FishCommon, FishRare, FishLarge, CookedFish, BurntFish, RawFish, Mosin, RifleAmmo, DoubleBarrelShotgun, ShotgunShell, Lockpick, BoltCutters, Crowbar, HandSaw, ServiceKey, DepotKey, DungeonCrank, DungeonKey, TrainingHead, TrainingTorso, TrainingHands, TrainingFeet, TrainingAmulet, TrainingRing, ShortSword, BattleAxe, Spear, WatchmanHood, GuardianHelm, MarksmanMask, ScoutJerkin, FighterMail, GuardianCuirass, MarksmanGloves, FighterGauntlets, ExplorerGloves, WandererBoots, DuelistBoots, GuardianGreaves, EyeMedallion, BreathTalisman, FighterFang, SteadyHandRing, StrongPulseRing, PersistenceRing, AncientCoin, Reliquary, FuneraryIdol, ValuableScrap, AntiqueObject, CollectorsDocument, ImprovisedHandCrossbow, RustySword, CrossbowBolt, Worm }
    public static class ItemCatalog
    {
        public const int BandageHeal = 25, StackLimit = 20, InventorySlots = 9, BackpackSlots = 12, SafeChestSlots = 16;
        public const float BandageCooldown = 3f;
        static readonly string[] icons = { "M", "P", "B", "MT", "PO", "C", "PI", "A", "F", "FR", "FG", "FA", "FQ", "FC", "MO", "AR", "SG", "CA", "GZ", "AL", "PC", "SR", "CS", "CD", "MV", "CC" };
        static readonly string[] names = { "Madeira", "Pregos", "Bandagem", "Metal", "Polvora", "Componentes", "Pistola", "Municao de pistola", "Peixe comum", "Peixe raro", "Peixe grande", "Peixe assado", "Peixe queimado", "Peixe cru", "Rifle de ferrolho", "Municao de rifle", "Espingarda de dois canos", "Cartucho de espingarda", "Gazua", "Alicate de corte", "Pé de cabra", "Serra manual", "Chave de serviço", "Chave do depósito", "Manivela dos coveiros", "Chave do canal" };
        public static int Count => (int)ItemId.Worm+1;
        public static bool Valid(ItemId item) => (byte)item < Count;
        // Retired IDs remain reserved so historical profiles never change item identity.
        public static bool Retired(ItemId item) => item==ItemId.Pistol || item==ItemId.Mosin || item==ItemId.DoubleBarrelShotgun || item==ItemId.PistolAmmo || item==ItemId.RifleAmmo || item==ItemId.ShotgunShell;
        public static bool Available(ItemId item) => Valid(item) && !Retired(item);
        public static bool Equipment(ItemId item) => EquipmentCatalog.IsDefined(item) || item>=ItemId.TrainingHead && item<=ItemId.TrainingRing;
        public static EquipmentSlot EquipmentSlotFor(ItemId item) => EquipmentCatalog.IsDefined(item) ? EquipmentCatalog.Get(item).Slot : Equipment(item) ? (EquipmentSlot)((int)item-(int)ItemId.TrainingHead+1) : EquipmentSlot.None;
        public static bool Melee(ItemId item) => item==ItemId.RustySword || item>=ItemId.ShortSword && item<=ItemId.Spear;
        public static bool Weapon(ItemId item) => Melee(item) || item == ItemId.Pistol || item == ItemId.Mosin || item == ItemId.DoubleBarrelShotgun || item==ItemId.ImprovisedHandCrossbow;
        public static bool Ammo(ItemId item) => item == ItemId.PistolAmmo || item == ItemId.RifleAmmo || item == ItemId.ShotgunShell || item==ItemId.CrossbowBolt;
        public static bool Fish(ItemId item) => (byte)item >= (byte)ItemId.FishCommon && (byte)item <= (byte)ItemId.FishLarge;
        public static bool PassageTool(ItemId item) => (byte)item>=(byte)ItemId.BoltCutters && (byte)item<=(byte)ItemId.DepotKey;
        public static bool ExpeditionOnly(ItemId item) => item==ItemId.DungeonCrank || item==ItemId.DungeonKey;
        public static int Limit(ItemId item) => TreasureCatalog.IsDefined(item) ? 5 : Weapon(item) || Equipment(item) || PassageTool(item) || ExpeditionOnly(item) ? 1 : StackLimit;
        public static string Icon(ItemId item) => item==ItemId.Worm ? "MI" : item==ItemId.ImprovisedHandCrossbow ? "BI" : item==ItemId.RustySword ? "EF" : item==ItemId.CrossbowBolt ? "VI" : TreasureCatalog.IsDefined(item) ? "TS" : Melee(item) ? (item==ItemId.ShortSword ? "EC" : item==ItemId.BattleAxe ? "MX" : "LN") : Equipment(item) ? "EQ" : Valid(item) ? icons[(int)item] : "?";
        public static string Name(ItemId item) => item==ItemId.Worm ? "Minhoca" : item==ItemId.ImprovisedHandCrossbow ? "Besta de mão improvisada" : item==ItemId.RustySword ? "Espada enferrujada" : item==ItemId.CrossbowBolt ? "Virotes" : TreasureCatalog.IsDefined(item) ? TreasureCatalog.Get(item).DisplayName : EquipmentCatalog.IsDefined(item) ? EquipmentCatalog.Get(item).DisplayName : Melee(item) ? (item==ItemId.ShortSword ? "Espada curta" : item==ItemId.BattleAxe ? "Machado" : "Lança") : Equipment(item) ? EquipmentLoadout.Name((int)EquipmentSlotFor(item)-1)+" · teste" : Valid(item) ? names[(int)item] : "?";
        public static int Score(ItemId item) => ExpeditionOnly(item) ? 0 : Weapon(item) ? 12 : item == ItemId.Gunpowder ? 5 : item == ItemId.Bandage ? 3 : 1;
        public static Color Tint(ItemId item) => item == ItemId.Wood ? new Color(.65f,.43f,.23f) : item == ItemId.Nails || item == ItemId.Metal ? new Color(.65f,.75f,.85f) : item == ItemId.Gunpowder ? new Color(.45f,.35f,.65f) : new Color(.93f,.87f,.71f);
    }
}
