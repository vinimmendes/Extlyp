using System;
using UnityEditor;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Match;
using CyberExtraction.Players;
using CyberExtraction.Bots;

public static class ServerChecks
{
    [MenuItem("Cyber Extraction/Servidor/Verificar sala e prefabs")]
    public static void Run()
    {
        int checks = 0;
        Action<bool, string> verify = (ok, name) =>
        { if (!ok) throw new Exception("Servidor: " + name); checks++; };
        var queue = new MatchQueue();
        verify(!queue.ShouldStart(0, 100) && queue.SecondsLeft(0, 100) == 30, "Sala vazia nao inicia");
        verify(queue.SecondsLeft(1, 101) == 30, "Primeiro humano inicia espera");
        int partialRoom = GameConfig.MatchCapacity - 1;
        verify(queue.SecondsLeft(partialRoom, 111) == 20, "Entradas posteriores nao reiniciam espera");
        verify(!queue.ShouldStart(partialRoom, 130.9), "Nao iniciar antes de 30 segundos");
        verify(queue.ShouldStart(partialRoom, 131), "Iniciar ao completar 30 segundos");
        queue.Reset();
        verify(queue.ShouldStart(GameConfig.MatchCapacity, 200), "Sala cheia inicia imediatamente");
        queue.Reset();
        queue.SecondsLeft(1, 300);
        queue.SecondsLeft(0, 310);
        verify(queue.SecondsLeft(1, 320) == 30, "Sala vazia cancela contagem anterior");
        for (int humans = 1; humans <= GameConfig.MatchCapacity; humans++)
            verify(humans + MatchQueue.BotsNeeded(humans) == GameConfig.MatchCapacity, "Capacidade com " + humans + " humanos");
        for (int i = 0; i < GameConfig.MatchCapacity; i++)
        for (int j = i + 1; j < GameConfig.MatchCapacity; j++)
            verify(Vector2.Distance(MapBuilder.ParticipantSpawn(i), MapBuilder.ParticipantSpawn(j)) >= 1f,
                "Spawns separados " + i + "/" + j);
        var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        verify(player != null && player.GetComponent<PlayerController>() != null, "Prefab Player valido");
        verify(player.GetComponent<NetworkTransform>().AuthorityMode == NetworkTransform.AuthorityModes.Server,
            "Movimento replicado pelo servidor");
        var bot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bot.prefab");
        verify(bot != null && bot.GetComponent<BotController>() != null && bot.GetComponent<NetworkObject>() != null,
            "Prefab Bot valido");
        BotChecks.Run();
        CampItemChecks.Run();
        Debug.Log($"Servidor: {checks} verificacoes passaram.");
    }
}
