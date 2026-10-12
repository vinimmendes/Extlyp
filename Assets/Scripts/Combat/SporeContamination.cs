using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Enemies;
using CyberExtraction.Match;
using CyberExtraction.Players;

namespace CyberExtraction.Combat
{
    [RequireComponent(typeof(Health), typeof(PlayerInventory))]
    public class SporeContamination : NetworkBehaviour
    {
        public NetworkVariable<float> Level = new NetworkVariable<float>(0);
        Health health;
        PlayerInventory inventory;
        float damageClock, coughClock;

        public override void OnNetworkSpawn()
        {
            health = GetComponent<Health>();
            inventory = GetComponent<PlayerInventory>();
            if (IsServer) ResetExposure();
        }

        void ResetExposure()
        {
            Level.Value = 0;
            damageClock = coughClock = 0;
        }
        public void ServerResetExposure() { if(IsServer) ResetExposure(); }

        void FixedUpdate()
        {
            if (!IsSpawned || !IsServer) return;
            var match = MatchManager.Instance;
            if (match == null || match.CurrentState != MatchState.Running ||
                !inventory.IsAlive || health.IsDead)
            {
                ResetExposure();
                return;
            }
            bool exposed = false;
            foreach (var cloud in SporeCloud.All)
            {
                if (cloud != null && cloud.IsSpawned && cloud.IsServer && cloud.Contains(transform.position))
                { exposed = true; break; }
            }
            // Overlapping clouds do not multiply exposure or damage.
            Level.Value = SporeRules.Exposure(Level.Value, exposed, Time.fixedDeltaTime);
            if (!SporeRules.Harmful(Level.Value, exposed))
            { damageClock = coughClock = 0; return; }
            damageClock += Time.fixedDeltaTime;
            coughClock += Time.fixedDeltaTime;
            if (damageClock >= 1f)
            {
                damageClock -= 1f;
                health.ServerTakeDamage(SporeRules.DamagePerSecond);
            }
            if (coughClock >= 2f && !health.IsDead)
            {
                coughClock -= 2f;
                Noise.Emit(transform.position, 8f);
            }
        }
    }
}
