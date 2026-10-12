namespace CyberExtraction.Bots
{
    // Nomes no jogo: Easy = Iniciante, Medium = Mediano, Hard = Experiente.
    public enum BotDifficulty : byte { Easy, Medium, Hard }
    public enum BotState : byte { Idle, Explore, Collect, Investigate, Engage, Retreat, Extract }

    // Dificuldade muda conhecimento, disciplina e habilidade. Nunca muda vida, dano, velocidade ou visao.
    //  - Iniciante: conhece pouco do mapa, vaga sem plano, atira andando, nao usa cobertura e
    //    sai tarde para a extracao (pode nao chegar).
    //  - Mediano: conhece o mapa, escolhe locais proximos, para para mirar, recua com pouca vida.
    //  - Experiente: planeja a rota de volta, evita os pontos de nascimento rivais no inicio,
    //    atira em rajadas curtas, se desloca entre elas, usa cobertura e chega cedo a extracao.
    public sealed class BotProfile
    {
        public readonly string Name;
        public readonly float ThinkInterval, ReactionTime, AimError, TurnSpeed;
        public readonly float RetreatHealth, IgnoreNoiseChance, MemoryTime, CoverPreference;
        public readonly int LootGoal;

        // Conhecimento e planejamento
        public readonly float MapKnowledge;      // fracao dos locais de loot conhecidos desde o inicio
        public readonly float CommitTime;        // tempo minimo mantendo o mesmo destino (evita zigue-zague)
        public readonly float Whim;              // peso das preferencias pessoais por local (cada bot tem as suas)
        public readonly float PlanAhead;         // 0..1: quanto considera a volta a extracao ao escolher rota
        public readonly float ExtractMargin;     // folga, em segundos, ao calcular quando sair para extrair
        public readonly bool AvoidsSpawns;       // evita nascimentos rivais no inicio da partida
        public readonly bool TurnsToSounds;      // olha na direcao de um tiro ouvido
        public readonly float InvestigateHealth; // so investiga som com vida acima disso
        public readonly float HealBelow;         // usa bandagem quando a vida cai abaixo disso e esta seguro

        // Combate
        public readonly float PreferredRange;    // distancia em que tenta lutar
        public readonly float StrafeChance;      // chance de se deslocar lateralmente na luta
        public readonly float SettleTime;        // segundos mirando no alvo ate a mira estabilizar
        public readonly int BurstSize;           // tiros antes de uma pausa para reposicionar
        public readonly float BurstPause;
        public readonly bool ShootWhileMoving;

        BotProfile(string name, float think, float reaction, float error, float turn, float retreat,
            float ignoreNoise, float memory, float cover, int lootGoal,
            float mapKnowledge, float commit, float whim, float planAhead, float extractMargin,
            bool avoidsSpawns, bool turnsToSounds, float investigateHealth, float healBelow,
            float range, float strafe, float settle, int burstSize, float burstPause, bool shootWhileMoving)
        {
            Name = name;
            ThinkInterval = think; ReactionTime = reaction; AimError = error; TurnSpeed = turn;
            RetreatHealth = retreat; IgnoreNoiseChance = ignoreNoise; MemoryTime = memory;
            CoverPreference = cover; LootGoal = lootGoal;
            MapKnowledge = mapKnowledge; CommitTime = commit; Whim = whim; PlanAhead = planAhead;
            ExtractMargin = extractMargin; AvoidsSpawns = avoidsSpawns; TurnsToSounds = turnsToSounds;
            InvestigateHealth = investigateHealth; HealBelow = healBelow;
            PreferredRange = range; StrafeChance = strafe; SettleTime = settle;
            BurstSize = burstSize; BurstPause = burstPause; ShootWhileMoving = shootWhileMoving;
        }

        public static string DisplayName(BotDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BotDifficulty.Easy: return "Iniciante";
                case BotDifficulty.Hard: return "Experiente";
                default: return "Mediano";
            }
        }

        public static BotProfile For(BotDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BotDifficulty.Easy:
                    return new BotProfile("Iniciante", 0.35f, 0.9f, 14f, 140f, 0.20f, 0.65f, 2f, 0.2f, 10,
                        0.30f, 2.0f, 1.0f, 0f, 5f, false, false, 0.30f, 0.40f,
                        4.0f, 0f, 2.5f, 99, 0f, true);
                case BotDifficulty.Hard:
                    return new BotProfile("Experiente", 0.12f, 0.25f, 3f, 300f, 0.45f, 0.10f, 6f, 1f, 30,
                        1.00f, 8.0f, 0.2f, 1f, 30f, true, true, 0.60f, 0.75f,
                        6.5f, 1f, 0.8f, 3, 0.8f, false);
                default:
                    return new BotProfile("Mediano", 0.22f, 0.55f, 7f, 220f, 0.35f, 0.35f, 4f, 0.65f, 20,
                        0.85f, 5.0f, 0.5f, 0.5f, 15f, false, true, 0.50f, 0.70f,
                        5.5f, 0.5f, 1.5f, 4, 0.6f, false);
            }
        }
    }

    // Politica pura: separada da percepcao, da fisica e da rede para verificacao.
    public struct BotSituation
    {
        public bool Active, HasThreat, ThreatIsMonster, KnownLoot, HeardNoise, FinishedExploring;
        public float HealthRatio, ThreatDistance;
        // Segundos que o bot leva para chegar a extracao (caminho + folga). 0 = desconhecido.
        public float ExtractEta;
        // Compatibility with existing diagnostics; new plans include their own margin in ExtractEta.
        public float ExtractionTravelSeconds;
        public int Loot, SecondsLeft;
    }

    public static class BotDecision
    {
        public const int ExtractionUnlockSeconds = 60;

        public static bool CanExtract(int secondsLeft) => secondsLeft <= ExtractionUnlockSeconds;

        // Hora de sair rumo a extracao: a extracao so abre aos 60 s, mas o bot precisa estar la quando abrir.
        public static bool TimeToLeave(int secondsLeft, float extractEta) => secondsLeft <= ExtractionUnlockSeconds + extractEta;

        public static BotState Select(BotSituation s, BotProfile p)
        {
            if (!s.Active) return BotState.Idle;
            if (s.HasThreat && (s.HealthRatio <= p.RetreatHealth ||
                (s.ThreatIsMonster && s.ThreatDistance < 3.5f))) return BotState.Retreat;
            if (TimeToLeave(s.SecondsLeft, s.ExtractEta > 0 ? s.ExtractEta : s.ExtractionTravelSeconds) && (!s.HasThreat || s.ThreatDistance > 4f)) return BotState.Extract;
            if (s.HasThreat) return BotState.Engage;
            if (s.KnownLoot) return BotState.Collect;
            if (s.HeardNoise && s.Loot < p.LootGoal && s.HealthRatio >= p.InvestigateHealth) return BotState.Investigate;
            return BotState.Explore;
        }
    }
}
