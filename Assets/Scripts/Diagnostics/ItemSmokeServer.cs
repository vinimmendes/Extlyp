using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using CyberExtraction.Camp;
using CyberExtraction.Combat;
using CyberExtraction.Items;
using CyberExtraction.Match;
using CyberExtraction.Players;
using CyberExtraction.UI;

namespace CyberExtraction.Diagnostics
{
    // So ativado pelo argumento explicito -items-smoke-server em testes locais.
    public class ItemSmokeServer : MonoBehaviour
    {
        IEnumerator Start()
        {
            while (MatchManager.Instance == null || MatchManager.Instance.CurrentState != MatchState.Running) yield return null;
            var player = NetworkManager.Singleton.ConnectedClientsList[0].PlayerObject;
            var inv = player.GetComponent<PlayerInventory>();
            var hp = player.GetComponent<Health>();
            var mm = MatchManager.Instance;
            inv.ServerAddItem(ItemId.Wood, 20); inv.ServerAddItem(ItemId.Nails, 10); inv.ServerAddItem(ItemId.Bandage, 3);
            Check(!inv.ServerUseBandage() && inv.Bandages.Value == 3, "Vida cheia nao consome bandagem");
            hp.ServerTakeDamage(40);
            Check(inv.ServerUseBandage() && hp.Current.Value == 85 && inv.Bandages.Value == 2, "Bandagem cura e consome uma unidade");
            Check(!inv.ServerUseBandage() && hp.Current.Value == 85 && inv.Bandages.Value == 2, "Cooldown bloqueia uso repetido");
            inv.BandageReadyAt.Value = 0;
            Check(inv.ServerUseBandage() && hp.Current.Value == 100 && inv.Bandages.Value == 1, "Cura limitada pela vida maxima");
            mm.ServerParticipantExtracted(inv); mm.ServerParticipantExtracted(inv);
            var request = NetworkBootstrap.Instance.RequestFor(inv.OwnerClientId);
            var profile = CampRepository.Get(request.Session);
            Check(profile.Wood == 20 && profile.Nails == 10 && profile.Bandages == 1, "Extracao credita uma vez no deposito");
            Check(!inv.ServerUseBandage(), "Extraido nao pode usar bandagem");
            int pickupsBefore = LootPickup.All.Count;
            foreach (var bot in PlayerInventory.All)
            {
                if (bot.NetworkObject.IsPlayerObject || !bot.IsAlive) continue;
                bot.ServerAddItem(ItemId.Wood, 4); bot.ServerAddItem(ItemId.Nails, 3); bot.ServerAddItem(ItemId.Bandage, 1);
                bot.GetComponent<Health>().ServerTakeDamage(1000);
                Check(LootPickup.All.Count == pickupsBefore + 3 && bot.Wood.Value == 0 && bot.Nails.Value == 0 && bot.Bandages.Value == 0,
                    "Morte solta os tres tipos de itens e esvazia a mochila");
                break;
            }
            Debug.Log("ITEMS SERVER PASS: cura, consumo, cooldown, extracao e deposito.");
            yield return new WaitForSeconds(2);
            foreach (var participant in PlayerInventory.All)
                if (participant.IsAlive) participant.ServerSetStatus(PlayerStatus.TimeUp);
            while (mm.CurrentState != MatchState.Waiting) yield return null;
            while (mm.CurrentState != MatchState.Running) yield return null;
            player = NetworkManager.Singleton.ConnectedClientsList[0].PlayerObject;
            inv = player.GetComponent<PlayerInventory>(); hp = player.GetComponent<Health>();
            request = NetworkBootstrap.Instance.RequestFor(inv.OwnerClientId);
            profile = CampRepository.Get(request.Session);
            Check(profile.PierRepaired && !profile.WellFed && profile.Wood == 0 && profile.Nails == 0 && profile.Bandages == 0,
                "Servidor valida reparo e desconta equipamento");
            Check(hp.Current.Value == 120 && hp.Maximum.Value == 120 && inv.Bandages.Value == 1,
                "Buff e bandagem equipados na proxima expedicao");
            Debug.Log("CAMP SERVER PASS: reparo, pesca, equipamento e buff validados.");
        }

        static void Check(bool condition, string name)
        {
            if (!condition) { Debug.LogError("ITEMS FAIL: " + name); throw new Exception(name); }
        }
    }
}
