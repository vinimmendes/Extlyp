using System;

namespace CyberExtraction.Enemies
{
    public enum PlantKind : byte { Mother, AttackBulb, HealingBulb, Minion }

    // Regras sem dependencia da cena, reutilizadas nas verificacoes do editor.
    public static class PlantRules
    {
        public const int MotherHealth = 300, AttackBulbHealth = 30, HealingBulbHealth = 40, MinionHealth = 35;
        public const int MaxAttackChildren = 3, MaxHealers = 2, HealAmount = 12, AttackDamage = 10;
        public const float MaturationSeconds = 9f, SpawnInterval = 6f, HealInterval = 2.5f;
        public const int HealParticleCount = 10;
        public const float HealTravelSeconds = .65f, HealStaggerSeconds = .02f;
        public const float HealArrivalSeconds = HealTravelSeconds + (HealParticleCount - 1) * HealStaggerSeconds;
        public static float ParticleTravel(double now, double launchedAt, int index) =>
            (float)Math.Max(0, Math.Min(1, (now - launchedAt - index * HealStaggerSeconds) / HealTravelSeconds));
        public const float ActivationRange = 12f, HealRange = 6f, MinionDetectRange = 9f;
        public const float LeashRange = 14f, MinionSpeed = 3.2f, AttackRange = 1.1f, AttackCooldown = 1.2f;
        public static int MaxHealth(PlantKind kind) => kind == PlantKind.Mother ? MotherHealth :
            kind == PlantKind.AttackBulb ? AttackBulbHealth : kind == PlantKind.HealingBulb ? HealingBulbHealth : MinionHealth;
        public static float Radius(PlantKind kind) => kind == PlantKind.Mother ? 1.25f : .42f;
        public static float SpriteWidth(PlantKind kind) => kind == PlantKind.Mother ? 4.2f : kind == PlantKind.Minion ? 1.8f : 1.5f;
        public static bool IsAttacker(PlantKind kind) => kind == PlantKind.AttackBulb || kind == PlantKind.Minion;
        public static bool CanSpawn(int attackChildren, int healers, PlantKind kind) =>
            attackChildren >= 0 && healers >= 0 && (kind == PlantKind.AttackBulb ? attackChildren < MaxAttackChildren :
            kind == PlantKind.HealingBulb && healers < MaxHealers);
        public static bool CanHeal(bool motherAlive, int current, int maximum, float distance, bool clearLine) =>
            motherAlive && current > 0 && current < maximum && distance <= HealRange && clearLine;
        public static float Maturity(double now, double bornAt, double matureAt)
        {
            if (matureAt <= bornAt) return 0;
            return (float)Math.Max(0, Math.Min(1, (now - bornAt) / (matureAt - bornAt)));
        }
        public static float PulseFrequency(float progress) => .8f + 4.2f * Math.Max(0, Math.Min(1, progress));
    }
}
