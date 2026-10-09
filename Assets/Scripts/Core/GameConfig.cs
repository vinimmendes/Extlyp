namespace CyberExtraction.Core
{
    // Todos os numeros de balanceamento ficam aqui. Ajuste sem medo.
    public static class GameConfig
    {
        // Layer 8 e usada so para paredes (a ferramenta de setup nomeia como "Walls")
        public const int WallLayer = 8;
        public const int WallMask = 1 << WallLayer;

        public const float MatchDuration = 300f;   // 5 minutos
        public const int MatchCapacity = 5;
        public const int MaxConcurrentRooms = 3;
        public const int MatchmakingSeconds = 30;
        public const int ResultsSeconds = 15;

        public const float PlayerSpeed = 4f;
        public const int PlayerMaxHealth = 100;

        // Cone de visao
        public const float VisionRange = 9f;
        public const float VisionAngle = 100f;     // abertura total em graus
        public const float NearRadius = 1.8f;      // raio de percepcao ao redor do jogador

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
