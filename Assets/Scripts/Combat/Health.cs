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

        public event Action<Health> OnDiedServer;
        public event Action<Health, int> OnDamagedServer;

        public bool IsDead => Current.Value <= 0;

        public override void OnNetworkSpawn()
        {
            All.Add(this);
            if (IsServer) { Maximum.Value = maxHealth; Current.Value = maxHealth; }
        }

        public override void OnNetworkDespawn() { All.Remove(this); }

        public void ServerTakeDamage(int amount)
        {
            if (!IsServer || IsDead || amount <= 0) return;
            int previous = Current.Value;
            Current.Value = Mathf.Max(0, Current.Value - amount);
            OnDamagedServer?.Invoke(this, previous - Current.Value);
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

