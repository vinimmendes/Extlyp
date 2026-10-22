using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

namespace CyberExtraction.Combat
{
    public enum DamageKind : byte { Physical, Poison, Environmental }

    public partial class Health : NetworkBehaviour
    {
        public static readonly List<Health> All = new List<Health>();
        public int maxHealth = 100;
        public NetworkVariable<int> Current = new NetworkVariable<int>(100);
        public NetworkVariable<int> Maximum = new NetworkVariable<int>(100);
        public NetworkVariable<int> DamageVersion = new NetworkVariable<int>(0);
        // Captured on the server at the fatal hit; survives the attacker's despawn.
        // Legacy field name retained: also carries player and bot names.
        public NetworkVariable<FixedString64Bytes> KillerCreature = new NetworkVariable<FixedString64Bytes>(default);
        public string DeathMessage => KillerCreature.Value.Length==0 ? "Você morreu" : "Você morreu\nMorto por: "+KillerCreature.Value.ToString();

        public event Action<Health> OnDiedServer;
        public event Action<Health,int> OnDamagedServer;

        public bool IsDead => Current.Value <= 0;
        // Local to a test session; never serialized into a camp profile.
        public bool TestInvulnerable;
        CyberExtraction.Players.PlayerInventory armorReceiver;
        CyberExtraction.Enemies.Monster monsterReceiver;

        void Awake()
        {
            armorReceiver=GetComponent<CyberExtraction.Players.PlayerInventory>();
            monsterReceiver=GetComponent<CyberExtraction.Enemies.Monster>();
            CyberExtraction.Match.ServerVisibility.Install(GetComponent<NetworkObject>());
            if(GetComponent<DamageFlash>()==null) gameObject.AddComponent<DamageFlash>();
        }
        public override void OnNetworkSpawn()
        {
            if(armorReceiver==null) armorReceiver=GetComponent<CyberExtraction.Players.PlayerInventory>();
            All.Add(this);
            if(IsServer) ResetExperienceTracking();
            if (IsServer) { KillerCreature.Value = default; Maximum.Value = maxHealth; Current.Value = maxHealth; }
        }

        public override void OnNetworkDespawn() { All.Remove(this); }

        public void ServerTakeDamage(int amount,CyberExtraction.Players.PlayerInventory source=null,DamageKind kind=DamageKind.Physical,string creatureName=null)
        {
            if (!IsServer || IsDead) return;
            if(TestInvulnerable && CyberExtraction.Match.MatchManager.Instance != null && CyberExtraction.Match.MatchManager.Instance.Sandbox) return;
            if (amount <= 0) return;
            if(monsterReceiver!=null && monsterReceiver.ServerInterceptWeaverDamage(amount,source)) return;
            if(kind==DamageKind.Physical && armorReceiver!=null)
                amount=Mathf.Max(1,Mathf.RoundToInt(amount*(1-armorReceiver.PhysicalDamageReduction)));
            CyberExtraction.Diagnostics.MatchTelemetry.Record("damage",NetworkObjectId,amount);
            DamageVersion.Value++;
            int previous = Current.Value;
            // Clear previous deaths on the next accepted hit, including after a test revival.
            string killerName=amount>=previous ? (source!=null ? source.DisplayName : creatureName) : null;
            KillerCreature.Value = !string.IsNullOrEmpty(killerName) ? new FixedString64Bytes(killerName) : default;
            Current.Value = Mathf.Max(0, Current.Value - amount);
            TrackExperienceDamage(source,previous-Current.Value);
            if(Current.Value<=0) AwardDeathExperience(source);
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
