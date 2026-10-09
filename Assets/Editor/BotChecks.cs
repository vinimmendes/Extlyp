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
        }
        var easy = BotProfile.For(BotDifficulty.Easy);
        var medium = BotProfile.For(BotDifficulty.Medium);
        var hard = BotProfile.For(BotDifficulty.Hard);
        verify(!BotDecision.CanExtract(61) && BotDecision.CanExtract(60) && BotDecision.CanExtract(59), "Limite da permissao de extracao");
        verify(easy.ReactionTime > medium.ReactionTime && medium.ReactionTime > hard.ReactionTime, "Ordem da reacao");
        verify(easy.AimError > medium.AimError && medium.AimError > hard.AimError, "Ordem da precisao");
        verify(easy.CoverPreference < medium.CoverPreference && medium.CoverPreference < hard.CoverPreference, "Ordem da preferencia por cobertura");
        verify(BotController.InVision(Vector2.zero, 0f, new Vector2(5f, 0f)), "Alvo na frente e visivel geometricamente");
        verify(!BotController.InVision(Vector2.zero, 0f, new Vector2(-5f, 0f)), "Nao detectar alvo atras fora do raio proximo");
        verify(BotController.InVision(Vector2.zero, 0f, new Vector2(-1f, 0f)), "Percepcao proxima igual a do player");
        verify(!BotController.InVision(Vector2.zero, 0f, new Vector2(GameConfig.VisionRange + 0.1f, 0f)), "Dificil nao ganha alcance de visao");
        Debug.Log("CyberExtraction: " + checks + " verificacoes das regras dos bots passaram. Testes de fisica/rede devem ser feitos em Play Mode.");
    }
}
