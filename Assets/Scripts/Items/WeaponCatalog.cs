using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Items
{
    public sealed class WeaponDefinition
    {
        public readonly ItemId Item, Ammo;
        public readonly int Capacity, Damage, Pellets;
        public readonly float Cooldown, ReloadSeconds, Range, StillHalfAngle, MovingHalfAngle, NoiseRadius, FullDamageRange;
        public readonly float BoltSeconds;
        public WeaponDefinition(ItemId item, ItemId ammo, int capacity, int damage, int pellets,
            float cooldown, float reloadSeconds, float range, float still, float moving,
            float noise, float fullDamageRange, float boltSeconds = 0)
        {
            Item = item; Ammo = ammo; Capacity = capacity; Damage = damage; Pellets = pellets;
            Cooldown = cooldown; ReloadSeconds = reloadSeconds; Range = range;
            StillHalfAngle = still; MovingHalfAngle = moving; NoiseRadius = noise;
            FullDamageRange = fullDamageRange; BoltSeconds = boltSeconds;
        }
        public float HalfAngle(float speed01) => Mathf.Lerp(StillHalfAngle, MovingHalfAngle, Mathf.Clamp01(speed01));
        public int DamageAt(float distance)
        {
            if (distance > Range) return 0;
            if (distance <= FullDamageRange || FullDamageRange >= Range) return Damage;
            float factor = Mathf.Lerp(1f, .25f, Mathf.Clamp01((distance - FullDamageRange) / (Range - FullDamageRange)));
            return Mathf.Max(1, Mathf.RoundToInt(Damage * factor));
        }
    }
    public static class WeaponCatalog
    {
        static readonly WeaponDefinition pistol = new WeaponDefinition(ItemId.Pistol, ItemId.PistolAmmo,
            12, GameConfig.ShotDamage, 1, GameConfig.FireCooldown, 1.5f, GameConfig.AimRange,
            GameConfig.AimHalfAngleStill, GameConfig.AimHalfAngleMoving, GameConfig.ShotNoiseRadius, GameConfig.AimRange);
        static readonly WeaponDefinition mosin = new WeaponDefinition(ItemId.Mosin, ItemId.RifleAmmo,
            5, 75, 1, 1.35f, 3.2f, 24f, 1f, 10f, 22f, 24f, 1.35f);
        static readonly WeaponDefinition shotgun = new WeaponDefinition(ItemId.DoubleBarrelShotgun, ItemId.ShotgunShell,
            2, 13, 8, .55f, 2.6f, 9f, 10f, 18f, 20f, 3f);
        public static WeaponDefinition Get(ItemId item) => item == ItemId.Pistol ? pistol :
            item == ItemId.Mosin ? mosin : item == ItemId.DoubleBarrelShotgun ? shotgun : null;
    }
}
