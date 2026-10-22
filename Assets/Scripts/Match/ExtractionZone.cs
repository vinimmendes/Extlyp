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
        public int ExitId=-1;
        bool Active => ExitId<0 || MatchManager.Instance!=null && (MatchManager.Instance.ActiveExtractions.Value&(1<<ExitId))!=0;
        readonly Dictionary<ulong, float> timers = new Dictionary<ulong, float>();
        readonly List<ulong> keys = new List<ulong>();
        static bool IsServer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer || !Active) return;
            var inv = other.GetComponentInParent<PlayerInventory>();
            if (inv == null || !inv.IsSpawned || !inv.IsAlive) return;
            timers[inv.NetworkObjectId] = 0f;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // A preparation can complete while someone is already on the platform.
            if(!IsServer || !Active || MapBuilder.Current!=ExpeditionMap.Dungeon) return;
            var inv=other.GetComponentInParent<PlayerInventory>();
            if(inv!=null && inv.IsSpawned && inv.IsAlive && !timers.ContainsKey(inv.NetworkObjectId)) timers[inv.NetworkObjectId]=0;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsServer) return;
            var inv = other.GetComponentInParent<PlayerInventory>();
            if (inv != null)
            {
                timers.Remove(inv.NetworkObjectId);
                if(inv.IsSpawned && inv.CurrentExtractionExit.Value==ExitId) { inv.ExtractionProgress.Value=0; inv.CurrentExtractionExit.Value=-1; }
            }
        }

        private void Update()
        {
            if (!IsServer) return;
            var mm = MatchManager.Instance;
            if (mm == null || mm.CurrentState != MatchState.Running || !Active) { ClearTimers(); return; }
            keys.Clear();
            keys.AddRange(timers.Keys);
            foreach (var id in keys)
            {
                if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out var obj))
                { timers.Remove(id); continue; }
                var inv = obj.GetComponent<PlayerInventory>();
                if (inv == null || !inv.IsAlive) { if(inv!=null) ResetProgress(inv); timers.Remove(id); continue; }
                if(inv.ChangingEquipment || obj.GetComponent<PlayerShooter>() is PlayerShooter attacker && attacker.MeleeAttacking) { timers[id]=0; ResetProgress(inv); continue; }
                if(MapBuilder.Current==ExpeditionMap.Dungeon && (inv.Lockpicking.Value || inv.DungeonAction.Value>=0))
                { timers[id]=0; ResetProgress(inv); continue; }
                if (obj.GetComponent<BotController>() != null && !BotDecision.CanExtract(mm.SecondsLeft.Value))
                { timers[id] = 0f; continue; }
                timers[id] += Time.deltaTime;
                inv.CurrentExtractionExit.Value=ExitId;
                inv.ExtractionProgress.Value=Mathf.Clamp01(timers[id]/GameConfig.ExtractionHoldTime);
                if (timers[id] < GameConfig.ExtractionHoldTime) continue;
                timers.Remove(id);
                mm.ServerParticipantExtracted(inv);
                inv.ExtractionProgress.Value=0; inv.CurrentExtractionExit.Value=-1;
            }
        }
        void ResetProgress(PlayerInventory inv)
        { if(inv.IsSpawned && inv.CurrentExtractionExit.Value==ExitId) { inv.ExtractionProgress.Value=0; inv.CurrentExtractionExit.Value=-1; } }
        void ClearTimers()
        {
            var manager=NetworkManager.Singleton;
            if(manager==null || manager.SpawnManager==null) { timers.Clear(); return; }
            foreach(var id in timers.Keys) if(manager.SpawnManager.SpawnedObjects.TryGetValue(id,out var obj))
            { var inv=obj.GetComponent<PlayerInventory>(); if(inv!=null) ResetProgress(inv); }
            timers.Clear();
        }
    }
}
