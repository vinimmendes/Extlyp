using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Combat;
using CyberExtraction.Match;

namespace CyberExtraction.Enemies
{
    [RequireComponent(typeof(Health), typeof(Monster))]
    public class Esporifero : NetworkBehaviour
    {
        public SporeCloud cloudPrefab;
        public NetworkVariable<double> ReleaseAt = new NetworkVariable<double>(0);
        Health health;
        int accumulatedDamage;
        public bool IsPreparing => ReleaseAt.Value > 0;

        public override void OnNetworkSpawn()
        {
            health = GetComponent<Health>();
            if (IsServer) health.OnDamagedServer += OnDamage;
        }

        public override void OnNetworkDespawn()
        {
            if (health != null) health.OnDamagedServer -= OnDamage;
        }

        void OnDamage(Health source, int damage)
        {
            // A lethal hit cancels the warning; a burst never queues multiple clouds.
            if (source.IsDead || IsPreparing || cloudPrefab == null) return;
            accumulatedDamage += damage;
            if (accumulatedDamage < SporeRules.DamageThreshold(source.Maximum.Value)) return;
            accumulatedDamage = 0;
            ReleaseAt.Value = NetworkManager.ServerTime.Time + SporeRules.WarningSeconds;
        }

        void Update()
        {
            if (!IsSpawned || !IsServer || !IsPreparing) return;
            var match = MatchManager.Instance;
            if (health.IsDead || match == null || match.CurrentState != MatchState.Running)
            {
                ReleaseAt.Value = 0;
                return;
            }
            if (NetworkManager.ServerTime.Time < ReleaseAt.Value) return;
            ReleaseAt.Value = 0;
            Vector2 origin = transform.position;
            var cloud = Instantiate(cloudPrefab, origin, Quaternion.identity);
            cloud.ServerInitialize(origin, MapBuilder.IsSheltered(origin) ? Vector2.zero : match.sporeWind);
            cloud.NetworkObject.Spawn();
        }
    }
}
