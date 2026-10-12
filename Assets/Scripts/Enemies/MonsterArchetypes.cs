using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Combat;
using CyberExtraction.Players;
using CyberExtraction.Match;
using CyberExtraction.Bots;

namespace CyberExtraction.Enemies
{
    public enum MonsterKind : byte { Charger, Listener, Shell, Weaver, Herald, Mimic, Acid, Stalker, Devourer }

    public partial class Monster
    {
        public static readonly List<Monster> All = new List<Monster>();
        public MonsterKind initialKind;
        public NetworkVariable<byte> Archetype = new NetworkVariable<byte>(0);
        // 0 idle/disguised, 1 preparation/feeding/transformation, 2 revealed mimic.
        public NetworkVariable<byte> AbilityState = new NetworkVariable<byte>(0);
        public NetworkVariable<double> AbilityEnds = new NetworkVariable<double>(0);
        public NetworkVariable<int> Growth = new NetworkVariable<int>(0);
        public MonsterKind Kind => (MonsterKind)Archetype.Value;
        public bool Disguised => Kind == MonsterKind.Mimic && AbilityState.Value == 0;
        float nextAbility, nextTrap;
        int preparationDamage, corpseId;
        Vector2 abilityPoint;
        Transform abilityTarget;

        public void Configure(MonsterKind kind)
        {
            initialKind = kind;
            GetComponent<Health>().maxHealth = StartingHealth(kind);
        }
        static int StartingHealth(MonsterKind kind)
        {
            switch (kind)
            {
                case MonsterKind.Shell: return 220;
                case MonsterKind.Weaver: return 70;
                case MonsterKind.Herald: return 80;
                case MonsterKind.Mimic: return 130;
                case MonsterKind.Acid: return 80;
                case MonsterKind.Stalker: return 90;
                case MonsterKind.Devourer: return 140;
                case MonsterKind.Listener: return 110;
                default: return 150;
            }
        }
        void InitializeArchetype()
        {
            All.Add(this);
            // The imported spore creature owns its sprite and uses only the shared basic movement.
            if (spores != null) return;
            if (IsServer)
            {
                Archetype.Value = (byte)initialKind;
                health.maxHealth = StartingHealth(Kind);
                health.Maximum.Value = health.Current.Value = health.maxHealth;
                if (Kind != MonsterKind.Charger)
                {
                    detectRadius = Kind == MonsterKind.Listener ? 1.5f : Kind == MonsterKind.Stalker ? 14 : Kind == MonsterKind.Acid ? 12 : 9;
                    chaseSpeed = Kind == MonsterKind.Shell ? 1.5f : Kind == MonsterKind.Stalker ? 4.6f : Kind == MonsterKind.Listener ? 3.8f : 3.1f;
                    attackDamage = Kind == MonsterKind.Shell ? 25 : Kind == MonsterKind.Mimic ? 30 : 15;
                }
                nextAbility = Time.time + 2;
            }
            Archetype.OnValueChanged += RebuildAppearance;
            BuildAppearance();
        }

        public void ServerAlert(Vector2 point)
        {
            if (!IsServer || Disguised) return;
            alertPos = point; alertUntil = Time.time + 10;
        }

        // Direction is the shooter's location, so rear and side hits bypass the front plates.
        public int ProjectileDamage(int damage, Vector2 source)
        {
            if (Disguised && IsServer) Reveal(null);
            if (Kind == MonsterKind.Shell)
            {
                Vector2 forward = transform.right;
                Vector2 from = (source - (Vector2)transform.position).normalized;
                if (Vector2.Dot(forward, from) > .5f) return Mathf.Max(1, Mathf.RoundToInt(damage * .25f));
            }
            if (Kind == MonsterKind.Devourer && AbilityState.Value == 1) return damage * 2;
            return damage;
        }

        public static Monster NearestDisguised(Vector2 point)
        {
            Monster best = null; float distance = 3;
            foreach (var m in All)
                if (m != null && m.IsSpawned && m.Disguised && Vector2.Distance(point, m.transform.position) < distance &&
                    Physics2D.Linecast(point, m.transform.position, GameConfig.WallMask).collider == null)
                { best = m; distance = Vector2.Distance(point, m.transform.position); }
            return best;
        }
        public void RequestOpenDisguise() { OpenDisguiseRpc(); }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void OpenDisguiseRpc(RpcParams rpc = default)
        {
            if (!Disguised || MatchManager.Instance == null || MatchManager.Instance.CurrentState != MatchState.Running ||
                !NetworkManager.ConnectedClients.TryGetValue(rpc.Receive.SenderClientId, out var client) || client.PlayerObject == null) return;
            var inv = client.PlayerObject.GetComponent<PlayerInventory>();
            if (inv == null || !inv.IsAlive || Vector2.Distance(inv.transform.position, transform.position) > 3 ||
                Physics2D.Linecast(inv.transform.position, transform.position, GameConfig.WallMask).collider != null) return;
            Reveal(inv.transform);
        }
        void Reveal(Transform target)
        {
            AbilityState.Value = 1; AbilityEnds.Value = NetworkManager.ServerTime.Time + .75;
            abilityTarget = target; nextAbility = Time.time + 4;
            if (bodyCollider != null) bodyCollider.isTrigger = false;
        }

        bool Illuminated(Transform target)
        {
            return target != null && BotController.InVision(target.position, target.eulerAngles.z, transform.position) &&
                Physics2D.Linecast(target.position, transform.position, GameConfig.VisionMask).collider == null;
        }
        Transform ArchetypeTarget()
        {
            if (Kind != MonsterKind.Stalker) return FindTarget(rb.position);
            Transform best = null; float score = float.MinValue;
            foreach (var inv in PlayerInventory.All)
            {
                if (inv == null || !inv.IsAlive) continue;
                float distance = Vector2.Distance(rb.position, inv.transform.position);
                if (distance > detectRadius || Physics2D.Linecast(rb.position, inv.transform.position, GameConfig.VisionMask).collider != null) continue;
                int neighbors = 0;
                foreach (var other in PlayerInventory.All)
                    if (other != null && other != inv && other.IsAlive && Vector2.Distance(other.transform.position, inv.transform.position) < 8) neighbors++;
                float candidate = 30 - distance - neighbors * 12 - (Illuminated(inv.transform) ? 8 : 0);
                if (candidate > score) { score = candidate; best = inv.transform; }
            }
            return best;
        }

        void StepArchetype()
        {
            Vector2 pos = rb.position;
            if (Disguised) { rb.linearVelocity = Vector2.zero; return; }
            Transform target = ArchetypeTarget();
            if (AbilityState.Value == 1)
            {
                rb.linearVelocity = Vector2.zero;
                bool interrupted = (Kind == MonsterKind.Herald || Kind == MonsterKind.Devourer) && health.DamageVersion.Value != preparationDamage;
                if (interrupted) { AbilityState.Value = 0; AbilityEnds.Value = 0; nextAbility = Time.time + 5; return; }
                if (NetworkManager.ServerTime.Time < AbilityEnds.Value) return;
                AbilityState.Value = Kind == MonsterKind.Mimic ? (byte)2 : (byte)0;
                AbilityEnds.Value = 0;
                if (Kind == MonsterKind.Herald)
                {
                    Noise.Emit(pos, 45);
                    foreach (var m in All) if (m != null && m != this && m.IsSpawned && Vector2.Distance(pos, m.transform.position) < 45) m.ServerAlert(abilityPoint);
                    MonsterWorld.Add(MonsterEffectKind.Shout, pos, pos, 3, 1);
                }
                else if (Kind == MonsterKind.Devourer)
                {
                    if (MonsterWorld.ConsumeCorpse(corpseId, pos))
                    {
                        Growth.Value = Mathf.Min(5, Growth.Value + 1);
                        health.maxHealth = StartingHealth(Kind) + Growth.Value * 25;
                        health.Maximum.Value = health.maxHealth; health.ServerHeal(45);
                        attackDamage = 15 + Growth.Value * 4;
                        // Keep collision and navigation clearance fixed as the silhouette grows.
                    }
                }
                else if (Kind == MonsterKind.Weaver || Kind == MonsterKind.Acid)
                {
                    MonsterWorld.Add(Kind == MonsterKind.Weaver ? MonsterEffectKind.WebShot : MonsterEffectKind.AcidShot,
                        pos, abilityPoint, .25f, Mathf.Clamp(Vector2.Distance(pos, abilityPoint) / 10, .2f, 1.5f));
                }
                else if (Kind == MonsterKind.Mimic && abilityTarget != null) ServerAlert(abilityTarget.position);
                return;
            }
            if (target != null && Time.time >= nextAbility)
            {
                if (Kind == MonsterKind.Herald) { Prepare(target.position, 2); nextAbility = Time.time + 12; return; }
                if ((Kind == MonsterKind.Weaver || Kind == MonsterKind.Acid) && Vector2.Distance(pos, target.position) > 2)
                { Prepare(target.position, Kind == MonsterKind.Acid ? 1.2 : .8); nextAbility = Time.time + 6; return; }
            }
            if (Kind == MonsterKind.Devourer && Time.time >= nextAbility && MonsterWorld.FindCorpse(pos, 12, out var corpse))
            {
                if (Vector2.Distance(pos, corpse.To) < 1.3f && Physics2D.Linecast(pos,corpse.To,GameConfig.WallMask).collider==null)
                { corpseId = corpse.Id; Prepare(corpse.To, 2.5); nextAbility = Time.time + 4; return; }
                MoveTo(corpse.To, chaseSpeed); return;
            }
            if (Kind == MonsterKind.Weaver && Time.time >= nextTrap && (target != null || Time.time < alertUntil))
            {
                if (MonsterWorld.CountNear(MonsterEffectKind.Web, pos, 15) < 3)
                    MonsterWorld.Add(MonsterEffectKind.Web, pos, pos, 1.3f, 25);
                nextTrap = Time.time + 9;
            }
            Vector2 dest; float speed;
            if (target != null)
            {
                dest = target.position; speed = chaseSpeed;
                alertPos = dest; alertUntil = Time.time + 6;
                float distance = Vector2.Distance(pos, dest);
                if (Kind == MonsterKind.Stalker)
                {
                    bool lit = Illuminated(target);
                    speed = lit ? 1.5f : chaseSpeed;
                    if (!lit && distance > 1.5f) dest = (Vector2)target.position - (Vector2)target.right * 1.4f;
                }
                if ((Kind == MonsterKind.Acid || Kind == MonsterKind.Weaver) && distance < 4 && distance > attackRange)
                    dest = pos + (pos - (Vector2)target.position).normalized * 3;
                if (distance <= attackRange && Time.time >= nextAttack)
                {
                    nextAttack = Time.time + attackCooldown;
                    int damage = attackDamage;
                    if (Kind == MonsterKind.Stalker && Vector2.Dot((pos - (Vector2)target.position).normalized, target.right) < -.5f) damage = 30;
                    target.GetComponent<Health>()?.ServerTakeDamage(damage);
                }
            }
            else if (Time.time < alertUntil)
            { dest = alertPos; speed = chaseSpeed; if (Vector2.Distance(pos, dest) < .6f) alertUntil = 0; }
            else
            {
                if (Time.time >= nextWander || Vector2.Distance(pos, wanderTarget) < .3f)
                { wanderTarget = home + Random.insideUnitCircle * 4; nextWander = Time.time + Random.Range(3f, 6f); }
                dest = wanderTarget; speed = patrolSpeed;
            }
            MoveTo(dest, speed);
        }
        void Prepare(Vector2 point, double seconds)
        {
            abilityPoint = point; preparationDamage = health.DamageVersion.Value;
            AbilityState.Value = 1; AbilityEnds.Value = NetworkManager.ServerTime.Time + seconds;
            var dir = point - rb.position; if (dir.sqrMagnitude > .01f) rb.rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            rb.linearVelocity = Vector2.zero;
        }
        void MoveTo(Vector2 dest, float speed)
        {
            if (!navigation.TryGetWaypoint(rb.position, dest, out var waypoint)) { rb.linearVelocity = Vector2.zero; return; }
            Vector2 dir = waypoint - rb.position;
            rb.linearVelocity = dir.normalized * Mathf.Min(speed, dir.magnitude / Time.fixedDeltaTime);
            if (dir.sqrMagnitude > .0001f) rb.rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }
    }
}
