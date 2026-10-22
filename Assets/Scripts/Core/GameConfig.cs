namespace CyberExtraction.Core
{
    // Todos os numeros de balanceamento ficam aqui. Ajuste sem medo.
    public static class GameConfig
    {
        // Shared by connection setup and the generated Windows build manifest.
        public const ushort NetworkProtocol = 41;
        // Layer 8 e usada so para paredes (a ferramenta de setup nomeia como "Walls")
        public const int WallLayer = 8;
        public const int LowObstacleLayer = 9;
        // Physical obstacles include low furniture; sight only stops at tall obstacles.
        public const int VisionMask = 1 << WallLayer;
        public const int WallMask = VisionMask | (1 << LowObstacleLayer);

        public const float MatchDuration = 600f;   // 10 minutos
        public const int MatchCapacity = 5;
        public const int MaxConcurrentRooms = 3;
        public const int MatchmakingSeconds = 30;
        public const int ResultsSeconds = 15;

        public const float PlayerSpeed = 3.3f;
        public const float SprintSpeed = 5.4f;
        public const float PlayerMaxStamina = 100f;
        public const float SprintStartStamina = 10f;
        public const float SprintDrain = 20f;
        public const float StaminaRegen = 15f;
        public const float StaminaRegenDelay = 1.5f;
        public const float ExpeditionCameraSize = 11f;
        public const float DungeonCameraSize = 5.5f;
        public const float CampCameraSize = 9f;
        public static bool AllowQuickTest => UnityEngine.Application.isEditor || UnityEngine.Debug.isDebugBuild || System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-allow-test") >= 0;
        public static bool ShowTemporaryServerRestart => AllowQuickTest;
        public const int PlayerMaxHealth = 100;

        // Cone de visao
        public static float VisionRange => CyberExtraction.Match.MapBuilder.Current==CyberExtraction.Match.ExpeditionMap.Dungeon ? 10f : 16f;
        public static float VisionAngle => CharacterAttributeRules.VisionAngle(1,CyberExtraction.Match.MapBuilder.Current==CyberExtraction.Match.ExpeditionMap.Dungeon);
        public const float NearRadius = 2.4f;      // raio de percepcao ao redor do jogador

        // Rounded flashlight footprint, shared by presentation and authoritative sight.
        public static float VisionRadiusAtAngle(float offsetDegrees,float openingAngle=-1)
        {
            float offset = UnityEngine.Mathf.Abs(offsetDegrees);
            float half = (openingAngle>0 ? openingAngle : VisionAngle) * 0.5f;
            float edge = UnityEngine.Mathf.SmoothStep(0f, 1f,
                UnityEngine.Mathf.InverseLerp(half * 0.55f, half, offset));
            return UnityEngine.Mathf.Lerp(VisionRange, NearRadius, edge);
        }

        // Cone de mira (meio angulo em graus: parado fecha, andando abre)
        public const float AimRange = 10f;
        public const float AimHalfAngleStill = 5f;
        public const float AimHalfAngleMoving = 15f;
        public const float FireCooldown = 0.35f;
        public const int ShotDamage = 20;
        public const float ShotNoiseRadius = 14f;

        public const float ExtractionHoldTime = 3f;
        public const int LootValue = 10;
        public const int MonsterLootValue = 30;
    }
}
