using System;
using CyberExtraction.Items;
class NewWeaponsRulesProbe
{
    static int checks;
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
    static void Main()
    {
        var bow=WeaponCatalog.Get(ItemId.ImprovisedHandCrossbow);var sword=WeaponCatalog.Get(ItemId.RustySword);
        Check(bow!=null && !bow.IsMelee && bow.Ammo==ItemId.CrossbowBolt,"crossbow ranged and bolt ammunition");
        Check(bow.Capacity==1 && bow.ReloadSeconds==2 && bow.Cooldown==1,"crossbow magazine/reload/cooldown");
        Check(bow.DamageAt(2)==30 && bow.DamageAt(8)==30,"crossbow full damage");
        Check(bow.DamageAt(10)<30 && bow.DamageAt(12)>0 && bow.DamageAt(12.01f)==0,"crossbow falloff and range boundary");
        Check(bow.PointingHalfAngle(1,1)>bow.PointingHalfAngle(0,1),"moving increases crossbow dispersion");
        Check(sword!=null && sword.IsMelee && sword.Damage==16 && sword.StaminaCost==15,"rusty sword damage/stamina");
        Check(sword.WindupSeconds==.3f && sword.RecoverySeconds==.5f && Math.Abs(sword.Cooldown-.8f)<.001f,"sword windup and recovery");
        Check(sword.Range==1.4f && sword.StillHalfAngle==32.5f,"sword range and arc");
        foreach(var id in new[]{ItemId.Pistol,ItemId.Mosin,ItemId.DoubleBarrelShotgun,ItemId.PistolAmmo,ItemId.RifleAmmo,ItemId.ShotgunShell})
            Check(!ItemCatalog.Available(id) && WeaponCatalog.Get(id)==null,"retired rejected "+id);
        Console.WriteLine("WEAPON RULES PASS "+checks+" checks; real ItemCatalog/WeaponCatalog, engine/domain collaborators stubbed. No RPC/input/physics validation.");
    }
}
namespace UnityEngine
{
    public struct Color {public Color(float r,float g,float b){}}
    public enum RuntimeInitializeLoadType {SubsystemRegistration}
    public class RuntimeInitializeOnLoadMethodAttribute:Attribute {public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType x){}}
    public static class Mathf
    {
        public static float Clamp01(float x)=>Math.Clamp(x,0,1);
        public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static int Max(int a,int b)=>Math.Max(a,b);
        public static int RoundToInt(float x)=>(int)Math.Round(x);
    }
}
namespace CyberExtraction.Core
{
    public class GameplayDefinition {public static GameplayDefinition Current=new GameplayDefinition();public int Magazine=12;public float ReloadSeconds=2;}
    public static class GameConfig {public const int ShotDamage=20;public const float FireCooldown=.3f,AimRange=12,AimHalfAngleStill=3,AimHalfAngleMoving=10,ShotNoiseRadius=15;}
}
namespace CyberExtraction.Items
{
    public enum EquipmentSlot {None,Head,Torso,Hands,Feet,Amulet,Ring}
    public class StubEntry {public EquipmentSlot Slot;public string DisplayName;}
    public static class EquipmentCatalog {public static bool IsDefined(ItemId i)=>false;public static StubEntry Get(ItemId i)=>new StubEntry();}
    public static class TreasureCatalog {public static bool IsDefined(ItemId i)=>false;public static StubEntry Get(ItemId i)=>new StubEntry();}
    public static class EquipmentLoadout {public static string Name(int i)=>"";}
}
