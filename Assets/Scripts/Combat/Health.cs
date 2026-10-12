using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CyberExtraction.Combat
{
    public class Health : NetworkBehaviour
    {
        public static readonly List<Health> All = new List<Health>();
        public int maxHealth = 100;
        public NetworkVariable<int> Current = new NetworkVariable<int>(100);
        public NetworkVariable<int> Maximum = new NetworkVariable<int>(100);
        public NetworkVariable<int> DamageVersion = new NetworkVariable<int>(0);

        public event Action<Health> OnDiedServer;
        public event Action<Health,int> OnDamagedServer;

        public bool IsDead => Current.Value <= 0;
        // Local to a test session; never serialized into a camp profile.
        public bool TestInvulnerable;

        void Awake()
        {
            CyberExtraction.Match.ServerVisibility.Install(GetComponent<NetworkObject>());
            if(GetComponent<DamageFlash>()==null) gameObject.AddComponent<DamageFlash>();
        }
        public override void OnNetworkSpawn()
        {
            All.Add(this);
            if (IsServer) { Maximum.Value = maxHealth; Current.Value = maxHealth; }
        }

        public override void OnNetworkDespawn() { All.Remove(this); }

        public void ServerTakeDamage(int amount)
        {
            if (!IsServer || IsDead) return;
            if(TestInvulnerable && CyberExtraction.Match.MatchManager.Instance != null && CyberExtraction.Match.MatchManager.Instance.Sandbox) return;
            if (amount <= 0) return;
            CyberExtraction.Diagnostics.MatchTelemetry.Record("damage",NetworkObjectId,amount);
            DamageVersion.Value++;
            int previous = Current.Value;
            Current.Value = Mathf.Max(0, Current.Value - amount);
            OnDamagedServer?.Invoke(this,previous-Current.Value);
            if (Current.Value <= 0) OnDiedServer?.Invoke(this);
        }

        public bool ServerHeal(int amount)
        {
            if (!IsServer || IsDead || amount <= 0 || Current.Value >= maxHealth) return false;
            Current.Value = Mathf.Min(maxHealth, Current.Value + amount);
            return true;
        }
    }
}
