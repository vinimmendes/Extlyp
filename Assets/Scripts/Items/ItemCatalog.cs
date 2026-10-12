using UnityEngine;
namespace CyberExtraction.Items
{
    public enum ItemId : byte { Wood, Nails, Bandage, Metal, Gunpowder, AmmoComponent, Pistol, PistolAmmo, FishCommon, FishRare, FishLarge, CookedFish, BurntFish, RawFish, Mosin, RifleAmmo, DoubleBarrelShotgun, ShotgunShell, Lockpick, BoltCutters, Crowbar, HandSaw, ServiceKey, DepotKey }
    public static class ItemCatalog
    {
        public const int BandageHeal = 25, StackLimit = 20, InventorySlots = 9;
        public const float BandageCooldown = 3f;
        static readonly string[] icons = { "M", "P", "B", "MT", "PO", "C", "PI", "A", "F", "FR", "FG", "FA", "FQ", "FC", "MO", "AR", "SG", "CA", "GZ", "AL", "PC", "SR", "CS", "CD" };
        static readonly string[] names = { "Madeira", "Pregos", "Bandagem", "Metal", "Polvora", "Componentes", "Pistola", "Municao de pistola", "Peixe comum", "Peixe raro", "Peixe grande", "Peixe assado", "Peixe queimado", "Peixe cru", "Rifle de ferrolho", "Municao de rifle", "Espingarda de dois canos", "Cartucho de espingarda", "Gazua", "Alicate de corte", "Pé de cabra", "Serra manual", "Chave de serviço", "Chave do depósito" };
        public static int Count => names.Length;
        public static bool Valid(ItemId item) => (byte)item < names.Length;
        public static bool Weapon(ItemId item) => item == ItemId.Pistol || item == ItemId.Mosin || item == ItemId.DoubleBarrelShotgun;
        public static bool Ammo(ItemId item) => item == ItemId.PistolAmmo || item == ItemId.RifleAmmo || item == ItemId.ShotgunShell;
        public static bool Fish(ItemId item) => (byte)item >= (byte)ItemId.FishCommon && (byte)item <= (byte)ItemId.FishLarge;
        public static bool PassageTool(ItemId item) => (byte)item>=(byte)ItemId.BoltCutters && (byte)item<=(byte)ItemId.DepotKey;
        public static int Limit(ItemId item) => Weapon(item) || item==ItemId.Lockpick || PassageTool(item) ? 1 : StackLimit;
        public static string Icon(ItemId item) => Valid(item) ? icons[(int)item] : "?";
        public static string Name(ItemId item) => Valid(item) ? names[(int)item] : "?";
        public static int Score(ItemId item) => Weapon(item) ? 12 : item == ItemId.Gunpowder ? 5 : item == ItemId.Bandage ? 3 : 1;
        public static Color Tint(ItemId item) => item == ItemId.Wood ? new Color(.65f,.43f,.23f) : item == ItemId.Nails || item == ItemId.Metal ? new Color(.65f,.75f,.85f) : item == ItemId.Gunpowder ? new Color(.45f,.35f,.65f) : new Color(.93f,.87f,.71f);
    }
}
