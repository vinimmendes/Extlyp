using System.Collections;
using UnityEngine;
using Unity.Netcode;
using CyberExtraction.Camp;
using CyberExtraction.Combat;
using CyberExtraction.Match;
using CyberExtraction.Players;
using CyberExtraction.UI;

namespace CyberExtraction.Diagnostics
{
    public class ItemSmokeClient : MonoBehaviour
    {
        IEnumerator Start()
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 110;
            while (CampSession.Progress.Wood != 20 || CampSession.Progress.Nails != 10 || CampSession.Progress.Bandages != 1)
            {
                if (Time.realtimeSinceStartupAsDouble > deadline) { Fail("Deposito nao recebeu a extracao."); yield break; }
                yield return null;
            }
            if (!CampSession.Progress.RepairPier() || !CampSession.Progress.Fish()) { Fail("Reparo/pesca locais."); yield break; }
            CampSession.RepairPending = CampSession.FishPending = true;
            CampSession.BringBandages = 1;
            NetworkManager.Singleton.Shutdown();
            yield return new WaitForSeconds(20);
            NetworkBootstrap.Instance.BeginExpedition("127.0.0.1");
            while (MatchManager.Instance == null || MatchManager.Instance.CurrentState != MatchState.Running)
            {
                if (Time.realtimeSinceStartupAsDouble > deadline) { Fail("Segunda expedicao nao iniciou."); yield break; }
                yield return null;
            }
            var player = NetworkManager.Singleton.LocalClient.PlayerObject;
            var hp = player.GetComponent<Health>(); var inv = player.GetComponent<PlayerInventory>();
            yield return new WaitForSeconds(0.2f);
            if (hp.Maximum.Value != 120 || inv.Bandages.Value != 1 || !CampSession.Progress.PierRepaired || CampSession.Progress.Bandages != 0)
            { Fail("Buff, equipamento ou deposito nao sincronizados."); yield break; }
            Debug.Log("CAMP CLIENT PASS: extracao, deposito, reparo, pesca e nova expedicao.");
            NetworkManager.Singleton.Shutdown();
            yield return new WaitForSeconds(0.5f);
            Application.Quit(0);
        }

        static void Fail(string reason) { Debug.LogError("CAMP CLIENT FAIL: " + reason); Application.Quit(1); }
    }
}
