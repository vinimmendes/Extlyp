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
        public NetworkVariable<double> WebReleasedAt = new NetworkVariable<double>(-1);
        public NetworkVariable<int> Growth = new NetworkVariable<int>(0);
        public MonsterKind Kind => (MonsterKind)Archetype.Value;
        public string DisplayName => spores!=null ? "Esporífero" : Kind switch
        {
            MonsterKind.Charger => "Investidor",
            MonsterKind.Listener => "Ouvinte",
            MonsterKind.Shell => "Couraçado",
            MonsterKind.Weaver => "Aranha",
            MonsterKind.Herald => "Arauto",
            MonsterKind.Mimic => "Mímico",
            MonsterKind.Acid => "Corrosivo",
            MonsterKind.Stalker => "Espreitador",
            MonsterKind.Devourer => "Devorador",
            _ => "Criatura"
        };
        // Authored reward tier, independent of current/max health and temporary growth.
        public int ExperienceReward => Kind==MonsterKind.Charger || Kind==MonsterKind.Shell || Kind==MonsterKind.Mimic || Kind==MonsterKind.Devourer ? 20 : 10;
        public bool Disguised => Kind == MonsterKind.Mimic && AbilityState.Value == 0;
        float nextAbility;
        public const float AcidWindupSeconds=.8f;
        const float AcidRecoverySeconds=.65f, AcidShotRange=10f, AcidShotSpeed=6f;
        float acidRecoveryUntil;
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
            InitializeWeaver();
            BuildAppearance();
        }

        public void ServerAlert(Vector2 point)
        {
            if (!IsServer || Disguised || returning || WeaverInCeiling) return;
            alertPos = BoundToHome(point); alertUntil = Time.time + 10; searchUntil=0;
            escapeDirection=Vector2.zero; nextPerception=Time.time;
        }

        // Direction is the shooter's location, so rear and side hits bypass the front plates.
        public int ProjectileDamage(int damage, Vector2 source)
        {
            RevealFromDamage(damage);
            if(IsServer && damage>0) ServerAlert(source);
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
        void RevealFromDamage(int damage)
        {
            // Shared by projectile preparation and confirmed Health damage (including melee).
            // Leaving Disguised makes repeated hits/pellets unable to restart this transition.
            if(IsServer && IsSpawned && damage>0 && Disguised) Reveal(null);
        }
        void Reveal(Transform target)
        {
            AbilityState.Value = 1; AbilityEnds.Value = NetworkManager.ServerTime.Time + .75;
            if(target!=null) abilityPoint=target.position;
            abilityTarget = target; nextAbility = Time.time + 4;
            if (bodyCollider != null) bodyCollider.isTrigger = false;
        }

        bool Illuminated(Transform target)
        {
            return target != null && BotController.InVision(target.position, target.eulerAngles.z, transform.position,target.GetComponent<PlayerInventory>()) &&
                Physics2D.Linecast(target.position, transform.position, GameConfig.VisionMask).collider == null;
        }
        Transform ArchetypeTarget() => PerceptionTarget();

        void StepArchetype()
        {
            if(Kind==MonsterKind.Weaver) { StepWeaverCombat(); return; }
            Vector2 pos = rb.position;
            if (Disguised) { rb.linearVelocity = Vector2.zero; return; }
            if(Kind==MonsterKind.Acid && Time.time<acidRecoveryUntil)
            { rb.linearVelocity=Vector2.zero; progressAt=Time.time; return; }
            Transform target = ArchetypeTarget();
            if (AbilityState.Value == 1)
            {
                progressAt=Time.time;
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
                else if (Kind == MonsterKind.Acid)
                {
                    // Aim was committed at preparation start. Travel past the old target
                    // position instead of homing or instantly appearing at its new location.
                    Vector2 direction=(abilityPoint-pos).normalized;
                    MonsterWorld.Add(MonsterEffectKind.AcidShot,
                        pos, pos+direction*AcidShotRange, .25f, AcidShotRange/AcidShotSpeed);
                    acidRecoveryUntil=Time.time+AcidRecoverySeconds;
                }
                else if (Kind == MonsterKind.Mimic && abilityTarget != null) ServerAlert(abilityPoint);
                return;
            }
            if(Kind==MonsterKind.Acid) { StepAcidCombat(target); return; }
            if (target != null && Time.time >= nextAbility)
            {
                if (Kind == MonsterKind.Herald) { Prepare(observedPosition, 2); nextAbility = Time.time + 12; return; }
            }
            if (Kind == MonsterKind.Devourer && target == null && Time.time >= nextAbility && MonsterWorld.FindCorpse(pos, 12, out var corpse) && Vector2.Distance(home,corpse.To)<LeashRadius)
            {
                if (Vector2.Distance(pos, corpse.To) < 1.3f && Physics2D.Linecast(pos,corpse.To,GameConfig.WallMask).collider==null)
                { corpseId = corpse.Id; Prepare(corpse.To, 2.5); nextAbility = Time.time + 4; return; }
                MoveTo(corpse.To, chaseSpeed); return;
            }
            Vector2 dest; float speed;
            if (target != null)
            {
                dest = observedPosition; speed = chaseSpeed;
                float distance = Vector2.Distance(pos, dest);
                if (Kind == MonsterKind.Stalker)
                {
                    bool lit = observedLit;
                    speed = lit ? 1.5f : chaseSpeed;
                    if (!lit && distance > 1.5f) dest = observedPosition - observedFacing * 1.4f;
                }
                if (TryMelee(target)) return;
            }
            else IdleDestination(out dest,out speed);
            MoveTo(dest, speed);
        }
        void StepAcidCombat(Transform target)
        {
            if(target==null)
            { IdleDestination(out var dest,out var speed); MoveTo(dest,speed); return; }
            Vector2 offset=observedPosition-rb.position;
            float distance=offset.magnitude;
            if(distance>8) { MoveTo(observedPosition,chaseSpeed); return; }
            if(distance<5)
            {
                // Retreat only through a clear corridor; try sideways when backed against cover.
                Vector2 away=distance>.01f ? -offset/distance : -(Vector2)transform.right;
                for(int i=0;i<7;i++)
                {
                    float angle=(i==0 ? 0 : ((i+1)/2)*30*(i%2==1 ? 1 : -1))*Mathf.Deg2Rad;
                    Vector2 direction=new Vector2(away.x*Mathf.Cos(angle)-away.y*Mathf.Sin(angle),away.x*Mathf.Sin(angle)+away.y*Mathf.Cos(angle));
                    Vector2 point=rb.position+direction*3;
                    if(Vector2.Distance(home,point)>=LeashRadius ||
                        Physics2D.CircleCast(rb.position,worldRadius+.04f,direction,3,GameConfig.WallMask).collider!=null) continue;
                    MoveTo(point,chaseSpeed*.75f); return;
                }
                // A cornered creature can still defend itself instead of freezing against a wall.
                if(TryMelee(target)) return;
            }
            rb.linearVelocity=Vector2.zero; progressAt=Time.time;
            if(distance>.01f) rb.rotation=Mathf.Atan2(offset.y,offset.x)*Mathf.Rad2Deg;
            if(distance>2 && Time.time>=nextAbility && Time.time>=staggerUntil &&
                Physics2D.Linecast(rb.position,observedPosition,GameConfig.WallMask).collider==null)
            {
                abilityTarget=target; Prepare(observedPosition,AcidWindupSeconds);
                nextAbility=Time.time+AcidWindupSeconds+AcidRecoverySeconds+2;
            }
        }
        void Prepare(Vector2 point, double seconds)
        {
            abilityPoint = point; preparationDamage = health.DamageVersion.Value;
            AbilityState.Value = 1; AbilityEnds.Value = NetworkManager.ServerTime.Time + seconds;
            var dir = point - rb.position; if (dir.sqrMagnitude > .01f) rb.rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            rb.linearVelocity = Vector2.zero;
        }
        void MoveTo(Vector2 dest, float speed) => MoveBehaviour(dest,speed);
    }
}
