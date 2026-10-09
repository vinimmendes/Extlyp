using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using CyberExtraction.Core;
using CyberExtraction.Match;
using CyberExtraction.Players;

namespace CyberExtraction.Diagnostics
{
    // Diagnostico opt-in de builds locais: -client -smoke-test -expected-humans N.
    public class ServerSmokeTest : MonoBehaviour
    {
        IEnumerator Start()
        {
            int expected = 1;
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-expected-humans") int.TryParse(args[i + 1], out expected);
            double deadline = Time.realtimeSinceStartupAsDouble + 55;
            while (MatchManager.Instance == null || MatchManager.Instance.CurrentState != MatchState.Running ||
                PlayerInventory.All.Count < GameConfig.MatchCapacity)
            {
                if (Time.realtimeSinceStartupAsDouble > deadline)
                { Fail($"A partida nao iniciou com {GameConfig.MatchCapacity} participantes dentro do prazo."); yield break; }
                yield return null;
            }
            var mm = MatchManager.Instance;
            if (mm.HumanCount.Value != expected || mm.BotCount.Value != GameConfig.MatchCapacity - expected)
            { Fail("Quantidade incorreta de humanos/bots."); yield break; }
            var player = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (player == null) { Fail("Jogador local ausente."); yield break; }
            Vector3 before = player.transform.position;
            var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            yield return new WaitForSeconds(0.4f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.2f);
            if (player == null) { Fail("Cliente desconectado durante a partida."); yield break; }
            if (player.transform.position.x < before.x + 0.1f)
            { Fail("Movimento do cliente nao foi replicado pelo servidor."); yield break; }
            foreach (var participant in PlayerInventory.All)
                if (participant.GetComponent<CyberExtraction.Bots.BotController>() != null &&
                    participant.CurrentStatus == PlayerStatus.Extracted && mm.SecondsLeft.Value > 60)
                { Fail("Bot extraiu antes do ultimo minuto."); yield break; }
            Debug.Log($"SMOKE PASS: {expected} humanos, {mm.BotCount.Value} bots, movimento e replicacao.");
            NetworkManager.Singleton.Shutdown();
            yield return new WaitForSeconds(0.5f);
            Application.Quit(0);
        }

        static void Fail(string reason)
        {
            Debug.LogError("SMOKE FAIL: " + reason);
            Application.Quit(1);
        }
    }
}
