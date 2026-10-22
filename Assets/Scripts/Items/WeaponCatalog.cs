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
        public readonly float WindupSeconds, RecoverySeconds, StaminaCost;
        public bool IsMelee => StaminaCost>0;
        public WeaponDefinition(ItemId item,int damage,float range,float arc,float windup,float recovery,float stamina)
            : this(item,(ItemId)255,0,damage,0,windup+recovery,0,range,arc*.5f,arc*.5f,0,range)
        { WindupSeconds=windup; RecoverySeconds=recovery; StaminaCost=stamina; }
        public WeaponDefinition(ItemId item, ItemId ammo, int capacity, int damage, int pellets,
            float cooldown, float reloadSeconds, float range, float still, float moving,
            float noise, float fullDamageRange, float boltSeconds = 0)
        {
            Item = item; Ammo = ammo; Capacity = capacity; Damage = damage; Pellets = pellets;
            Cooldown = cooldown; ReloadSeconds = reloadSeconds; Range = range;
            StillHalfAngle = still; MovingHalfAngle = moving; NoiseRadius = noise;
            FullDamageRange = fullDamageRange; BoltSeconds = boltSeconds;
        }
        public float HalfAngle(float speed01) => IsMelee ? StillHalfAngle : Mathf.Lerp(StillHalfAngle, MovingHalfAngle, Mathf.Clamp01(speed01)) * .7f;
        // The shotgun retains its standing pellet pattern. Dexterity affects only aiming error.
        public float StructuralHalfAngle => Pellets>1 ? StillHalfAngle*.7f : 0;
        public float PointingHalfAngle(float speed01,float multiplier) => IsMelee ? 0 : Mathf.Max(0,HalfAngle(speed01)-StructuralHalfAngle)*multiplier;
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
        static WeaponDefinition pistol;
        static WeaponDefinition Pistol => pistol ?? (pistol = new WeaponDefinition(ItemId.Pistol, ItemId.PistolAmmo,
            GameplayDefinition.Current.Magazine, GameConfig.ShotDamage, 1, GameConfig.FireCooldown, GameplayDefinition.Current.ReloadSeconds, GameConfig.AimRange,
            GameConfig.AimHalfAngleStill, GameConfig.AimHalfAngleMoving, GameConfig.ShotNoiseRadius, GameConfig.AimRange));
        static readonly WeaponDefinition mosin = new WeaponDefinition(ItemId.Mosin, ItemId.RifleAmmo,
            5, 75, 1, 1.35f, 3.2f, 24f, 1f, 10f, 22f, 24f, 1.35f);
        static readonly WeaponDefinition shotgun = new WeaponDefinition(ItemId.DoubleBarrelShotgun, ItemId.ShotgunShell,
            2, 13, 8, .55f, 2.6f, 9f, 10f, 18f, 20f, 3f);
        // Both sword blade tips reach approximately 2.1 units from the player at impact.
        static readonly WeaponDefinition sword=new WeaponDefinition(ItemId.ShortSword,22,2.1f,70,.25f,.45f,15);
        static readonly WeaponDefinition axe=new WeaponDefinition(ItemId.BattleAxe,32,1.4f,85,.4f,.65f,23);
        static readonly WeaponDefinition spear=new WeaponDefinition(ItemId.Spear,20,2.2f,30,.3f,.5f,14);
        static readonly WeaponDefinition rustySword=new WeaponDefinition(ItemId.RustySword,16,2.1f,65,.3f,.5f,15);
        static readonly WeaponDefinition handCrossbow=new WeaponDefinition(ItemId.ImprovisedHandCrossbow,ItemId.CrossbowBolt,
            1,30,1,1f,2f,12f,3f,10f,4f,8f);
        public static int RecipeOutput(ItemId ammo) => 0; // Firearm recipes are retired; no bolt recipe is introduced here.
        public static WeaponDefinition Get(ItemId item) => ItemCatalog.Available(item) ? LegacyDefinition(item) : null;
        internal static WeaponDefinition LegacyDefinition(ItemId item) => item == ItemId.Pistol ? Pistol :
            item == ItemId.Mosin ? mosin : item == ItemId.DoubleBarrelShotgun ? shotgun :
            item==ItemId.ShortSword ? sword : item==ItemId.BattleAxe ? axe : item==ItemId.Spear ? spear :
            item==ItemId.RustySword ? rustySword : item==ItemId.ImprovisedHandCrossbow ? handCrossbow : null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { pistol = null; }
    }
}
