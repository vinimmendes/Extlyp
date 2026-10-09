using UnityEngine;
namespace CyberExtraction.Items
{
    public enum ItemId : byte { Wood, Nails, Bandage, Metal, Gunpowder, AmmoComponent, Pistol, PistolAmmo, FishCommon, FishRare, FishLarge, CookedFish, BurntFish, RawFish, Mosin, RifleAmmo, DoubleBarrelShotgun, ShotgunShell }
    public static class ItemCatalog
    {
        public const int BandageHeal = 25, StackLimit = 20, InventorySlots = 9;
        public const float BandageCooldown = 3f;
        static readonly string[] icons = { "M", "P", "B", "MT", "PO", "C", "PI", "A", "F", "FR", "FG", "FA", "FQ", "FC", "MO", "AR", "SG", "CA" };
        static readonly string[] names = { "Madeira", "Pregos", "Bandagem", "Metal", "Polvora", "Componentes", "Pistola", "Municao", "Peixe comum", "Peixe raro", "Peixe grande", "Peixe assado", "Peixe queimado", "Peixe cru", "Rifle de ferrolho", "Municao de rifle", "Espingarda de dois canos", "Cartucho de espingarda" };
        public static bool Valid(ItemId item) => (byte)item < names.Length;
        public static bool Weapon(ItemId item) => item == ItemId.Pistol || item == ItemId.Mosin || item == ItemId.DoubleBarrelShotgun;
        public static bool Ammo(ItemId item) => item == ItemId.PistolAmmo || item == ItemId.RifleAmmo || item == ItemId.ShotgunShell;
        public static bool Fish(ItemId item) => item >= ItemId.FishCommon && item <= ItemId.FishLarge;
        public static int Limit(ItemId item) => Weapon(item) ? 1 : StackLimit;
        public static string Icon(ItemId item) => Valid(item) ? icons[(int)item] : "?";
        public static string Name(ItemId item) => Valid(item) ? names[(int)item] : "?";
        public static int Score(ItemId item) => Weapon(item) ? 12 : item == ItemId.Gunpowder ? 5 : item == ItemId.Bandage ? 3 : 1;
        public static Color Tint(ItemId item) => item == ItemId.Wood ? new Color(.65f,.43f,.23f) : item == ItemId.Nails || item == ItemId.Metal ? new Color(.65f,.75f,.85f) : item == ItemId.Gunpowder ? new Color(.45f,.35f,.65f) : new Color(.93f,.87f,.71f);
    }
}
