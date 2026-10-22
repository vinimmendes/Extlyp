using UnityEngine;

namespace CyberExtraction.Core
{
    // Pure balance rules, shared by server simulation and presentation.
    public static class CharacterAttributeRules
    {
        public const int MaxInvestment=10, MaxEffective=20;
        public static int Investment(AttributeValues invested,AttributeId id) =>
            id==AttributeId.Intelligence ? 0 : Mathf.Clamp(invested.Get(id),0,MaxInvestment);
        public static int Effective(long total) => (int)System.Math.Max(1,System.Math.Min(MaxEffective,total));
        public static float MeleeMultiplier(int strength) => 1+.02f*(Effective(strength)-1);
        public static float AimMultiplier(int dexterity) => 1-.015f*(Effective(dexterity)-1);
        public static float Stamina(int vigor) => GameConfig.PlayerMaxStamina+3*(Effective(vigor)-1);
        public static int Health(int vitality) => GameConfig.PlayerMaxHealth+Effective(vitality)-1;
        public static float VisionAngle(int perception,bool dungeon) =>
            (dungeon ? 80f : 110f)+30f*(Effective(perception)-1)/19f;
    }
}
