using System;
using UnityEditor;
using UnityEngine;
using CyberExtraction.Bots;
using CyberExtraction.Core;

// Verificacoes de contratos da IA, sem dependencias de NUnit ou de Play Mode.
public static class BotChecks
{
    [MenuItem("Cyber Extraction/Verificar regras dos bots")]
    public static void Run()
    {
        int checks = 0;
        Action<bool, string> verify = (ok, name) =>
        {
            if (!ok) throw new Exception("Bot checks: " + name);
            checks++;
        };
        foreach (BotDifficulty difficulty in Enum.GetValues(typeof(BotDifficulty)))
        {
            var p = BotProfile.For(difficulty);
            var s = new BotSituation { Active = true, HealthRatio = 1f, SecondsLeft = 180 };
            verify(BotDecision.Select(s, p) == BotState.Explore, "Sem informacao deve explorar: " + difficulty);
            s.HasThreat = true; s.ThreatDistance = 6f;
            verify(BotDecision.Select(s, p) == BotState.Engage, "Encontro visivel deve permitir combate: " + difficulty);
            s.HealthRatio = 0.1f;
            verify(BotDecision.Select(s, p) == BotState.Retreat, "Baixa vida com inimigo exige recuo: " + difficulty);
            s.HealthRatio = 1f; s.ThreatIsMonster = true; s.ThreatDistance = 2f;
            verify(BotDecision.Select(s, p) == BotState.Retreat, "Evitar melee do monstro: " + difficulty);
            s.ThreatIsMonster = false; s.ThreatDistance = 7f; s.SecondsLeft = 20;
            verify(BotDecision.Select(s, p) == BotState.Extract, "Prazo vence combate distante: " + difficulty);
            s.HasThreat = false; s.SecondsLeft = 180; s.Loot = p.LootGoal; s.KnownLoot = true;
            verify(BotDecision.Select(s, p) == BotState.Collect, "Meta de loot nao libera extracao antecipada: " + difficulty);
            s.Loot = 0;
            verify(BotDecision.Select(s, p) == BotState.Collect, "Loot observado tem prioridade sobre explorar: " + difficulty);
            s.KnownLoot = false; s.HeardNoise = true;
            verify(BotDecision.Select(s, p) == BotState.Investigate, "Som aceito leva a investigar, nao a tiro cego: " + difficulty);
            s.HeardNoise = false; s.FinishedExploring = true;
            verify(BotDecision.Select(s, p) == BotState.Explore, "Mapa visitado continua patrulhando antes do ultimo minuto: " + difficulty);
            s.HealthRatio = 0.1f; s.SecondsLeft = 61;
            verify(BotDecision.Select(s, p) != BotState.Extract, "Baixa vida nao libera extracao com 61 segundos: " + difficulty);
            s.HealthRatio = 1f; s.Loot = p.LootGoal; s.KnownLoot = true;
            verify(BotDecision.Select(s, p) == BotState.Collect, "Com 61 segundos ainda coleta: " + difficulty);
            s.SecondsLeft = 60;
            verify(BotDecision.Select(s, p) == BotState.Extract, "Extracao liberada exatamente com 60 segundos: " + difficulty);
            s.SecondsLeft = 59;
            verify(BotDecision.Select(s, p) == BotState.Extract, "Extracao liberada com 59 segundos: " + difficulty);
            s.Active = false; s.HasThreat = true;
            verify(BotDecision.Select(s, p) == BotState.Idle, "Morto/extraido/partida encerrada nao pode agir: " + difficulty);

            // Planejamento da volta: sai cedo quando a extracao esta longe.
            var t = new BotSituation { Active = true, HealthRatio = 1f, ExtractEta = 100f, SecondsLeft = 160 };
            verify(BotDecision.Select(t, p) == BotState.Extract, "Sai rumo a extracao quando o tempo de caminhada exige: " + difficulty);
            t.SecondsLeft = 161;
            verify(BotDecision.Select(t, p) == BotState.Explore, "Ainda ha tempo: continua o plano: " + difficulty);
            t.SecondsLeft = 100; t.HasThreat = true; t.ThreatDistance = 3f;
            verify(BotDecision.Select(t, p) == BotState.Engage, "Inimigo colado vence a viagem para a extracao: " + difficulty);
            t.ThreatDistance = 8f;
            verify(BotDecision.Select(t, p) == BotState.Extract, "Inimigo distante nao impede a viagem: " + difficulty);
        }

        var easy = BotProfile.For(BotDifficulty.Easy);
        var medium = BotProfile.For(BotDifficulty.Medium);
        var hard = BotProfile.For(BotDifficulty.Hard);
        verify(!BotDecision.CanExtract(61) && BotDecision.CanExtract(60) && BotDecision.CanExtract(59), "Limite da permissao de extracao");
        verify(easy.ReactionTime > medium.ReactionTime && medium.ReactionTime > hard.ReactionTime, "Ordem da reacao");
        verify(easy.AimError > medium.AimError && medium.AimError > hard.AimError, "Ordem da precisao");
        verify(easy.CoverPreference < medium.CoverPreference && medium.CoverPreference < hard.CoverPreference, "Ordem da preferencia por cobertura");

        // Conhecimento e disciplina crescem com a experiencia.
        verify(easy.MapKnowledge < medium.MapKnowledge && medium.MapKnowledge < hard.MapKnowledge, "Ordem do conhecimento do mapa");
        verify(easy.CommitTime < medium.CommitTime && medium.CommitTime < hard.CommitTime, "Ordem do compromisso com o plano");
        verify(easy.ExtractMargin < medium.ExtractMargin && medium.ExtractMargin < hard.ExtractMargin, "Ordem da folga para extrair");
        verify(easy.PlanAhead < medium.PlanAhead && medium.PlanAhead < hard.PlanAhead, "Ordem do planejamento");
        verify(easy.PreferredRange < medium.PreferredRange && medium.PreferredRange < hard.PreferredRange, "Ordem da distancia de luta");
        verify(easy.StrafeChance < medium.StrafeChance && medium.StrafeChance < hard.StrafeChance, "Ordem do deslocamento lateral");
        verify(easy.SettleTime > medium.SettleTime && medium.SettleTime > hard.SettleTime, "Ordem do tempo de assentar a mira");
        verify(easy.ShootWhileMoving && !medium.ShootWhileMoving && !hard.ShootWhileMoving, "Iniciante atira andando; os demais param");
        verify(!easy.TurnsToSounds && medium.TurnsToSounds && hard.TurnsToSounds, "Quem ouve vira para o som");
        verify(!easy.AvoidsSpawns && !medium.AvoidsSpawns && hard.AvoidsSpawns, "So o experiente evita nascimentos rivais");
        verify(easy.HealBelow < medium.HealBelow, "Iniciante cura mais tarde");
        verify(BotProfile.DisplayName(BotDifficulty.Easy) == "Iniciante" &&
               BotProfile.DisplayName(BotDifficulty.Medium) == "Mediano" &&
               BotProfile.DisplayName(BotDifficulty.Hard) == "Experiente", "Nomes dos niveis");

        // Experiente nao investiga tiro ouvido machucado; iniciante vai de qualquer jeito.
        var hurt = new BotSituation { Active = true, HealthRatio = 0.5f, SecondsLeft = 180, HeardNoise = true };
        verify(BotDecision.Select(hurt, hard) == BotState.Explore, "Experiente machucado nao investiga som");
        verify(BotDecision.Select(hurt, easy) == BotState.Investigate, "Iniciante machucado ainda investiga som");

        verify(BotController.InVision(Vector2.zero, 0f, new Vector2(5f, 0f)), "Alvo na frente e visivel geometricamente");
        verify(!BotController.InVision(Vector2.zero, 0f, new Vector2(-5f, 0f)), "Nao detectar alvo atras fora do raio proximo");
        verify(BotController.InVision(Vector2.zero, 0f, new Vector2(-1f, 0f)), "Percepcao proxima igual a do player");
        verify(!BotController.InVision(Vector2.zero, 0f, new Vector2(GameConfig.VisionRange + 0.1f, 0f)), "Dificil nao ganha alcance de visao");
        Debug.Log("CyberExtraction: " + checks + " verificacoes das regras dos bots passaram. Testes de fisica/rede devem ser feitos em Play Mode.");
    }
}
