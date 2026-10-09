using System;

namespace CyberExtraction.Enemies
{
    // Pure rules shared by the server simulation and editor checks.
    public static class SporeRules
    {
        public const float DamageFraction = 0.25f;
        public const float WarningSeconds = 1.2f;
        public const float CloudSeconds = 8f;
        public const float CloudRadius = 2.8f;
        public const float ExposurePerSecond = 20f;
        public const float RecoveryPerSecond = 15f;
        public const float HarmThreshold = 40f;
        public const int DamagePerSecond = 4;

        public static int DamageThreshold(int maximum) =>
            Math.Max(1, (int)Math.Ceiling(maximum * (double)DamageFraction));

        public static float Exposure(float current, bool exposed, float seconds) =>
            Math.Max(0f, Math.Min(100f, current +
                (exposed ? ExposurePerSecond : -RecoveryPerSecond) * Math.Max(0f, seconds)));

        public static bool Harmful(float level, bool exposed) => exposed && level >= HarmThreshold;
    }
}
