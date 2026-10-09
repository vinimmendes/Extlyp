using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Players;
using CyberExtraction.Bots;

namespace CyberExtraction.Match
{
    // Humanos e bots usam NetworkObjectId: varios bots podem pertencer ao servidor.
    public class ExtractionZone : MonoBehaviour
    {
        readonly Dictionary<ulong, float> timers = new Dictionary<ulong, float>();
        readonly List<ulong> keys = new List<ulong>();
        static bool IsServer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer) return;
            var inv = other.GetComponentInParent<PlayerInventory>();
            if (inv == null || !inv.IsSpawned || !inv.IsAlive) return;
            timers[inv.NetworkObjectId] = 0f;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsServer) return;
            var inv = other.GetComponentInParent<PlayerInventory>();
            if (inv != null) timers.Remove(inv.NetworkObjectId);
        }

        private void Update()
        {
            if (!IsServer) return;
            var mm = MatchManager.Instance;
            if (mm == null || mm.CurrentState != MatchState.Running) { timers.Clear(); return; }
            keys.Clear();
            keys.AddRange(timers.Keys);
            foreach (var id in keys)
            {
                if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out var obj))
                { timers.Remove(id); continue; }
                var inv = obj.GetComponent<PlayerInventory>();
                if (inv == null || !inv.IsAlive) { timers.Remove(id); continue; }
                if (obj.GetComponent<BotController>() != null && !BotDecision.CanExtract(mm.SecondsLeft.Value))
                { timers[id] = 0f; continue; }
                timers[id] += Time.deltaTime;
                if (timers[id] < GameConfig.ExtractionHoldTime) continue;
                timers.Remove(id);
                mm.ServerParticipantExtracted(inv);
            }
        }
    }
}
