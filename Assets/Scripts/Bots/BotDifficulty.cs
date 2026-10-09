namespace CyberExtraction.Bots
{
    public enum BotDifficulty : byte { Easy, Medium, Hard }
    public enum BotState : byte { Idle, Explore, Collect, Investigate, Engage, Retreat, Extract }

    // Dificuldade muda habilidade e cautela, nunca vida, dano, velocidade ou visao.
    public sealed class BotProfile
    {
        public readonly float ThinkInterval, ReactionTime, AimError, TurnSpeed;
        public readonly float RetreatHealth, IgnoreNoiseChance, MemoryTime, CoverPreference;
        public readonly int LootGoal;

        BotProfile(float think, float reaction, float error, float turn, float retreat,
            float ignoreNoise, float memory, float cover, int lootGoal)
        {
            ThinkInterval = think; ReactionTime = reaction; AimError = error; TurnSpeed = turn;
            RetreatHealth = retreat; IgnoreNoiseChance = ignoreNoise; MemoryTime = memory;
            CoverPreference = cover; LootGoal = lootGoal;
        }

        public static BotProfile For(BotDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BotDifficulty.Easy: return new BotProfile(0.35f, 0.9f, 14f, 140f, 0.20f, 0.65f, 2f, 0.2f, 10);
                case BotDifficulty.Hard: return new BotProfile(0.12f, 0.25f, 3f, 300f, 0.45f, 0.10f, 6f, 1f, 30);
                default: return new BotProfile(0.22f, 0.55f, 7f, 220f, 0.35f, 0.35f, 4f, 0.65f, 20);
            }
        }
    }

    // Politica pura: separada da percepcao, da fisica e da rede para verificacao.
    public struct BotSituation
    {
        public bool Active, HasThreat, ThreatIsMonster, KnownLoot, HeardNoise, FinishedExploring;
        public float HealthRatio, ThreatDistance;
        public int Loot, SecondsLeft;
    }

    public static class BotDecision
    {
        public const int ExtractionUnlockSeconds = 60;

        public static bool CanExtract(int secondsLeft) => secondsLeft <= ExtractionUnlockSeconds;

        public static BotState Select(BotSituation s, BotProfile p)
        {
            if (!s.Active) return BotState.Idle;
            if (s.HasThreat && (s.HealthRatio <= p.RetreatHealth ||
                (s.ThreatIsMonster && s.ThreatDistance < 3.5f))) return BotState.Retreat;
            if (CanExtract(s.SecondsLeft) && (!s.HasThreat || s.ThreatDistance > 4f)) return BotState.Extract;
            if (s.HasThreat) return BotState.Engage;
            if (s.KnownLoot) return BotState.Collect;
            if (s.HeardNoise && s.Loot == 0) return BotState.Investigate;
            return BotState.Explore;
        }
    }
}
