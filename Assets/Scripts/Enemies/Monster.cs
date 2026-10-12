using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Combat;
using CyberExtraction.Match;
using CyberExtraction.Players;

namespace CyberExtraction.Enemies
{
    // IA simples, so no servidor: fica perto de casa, investiga tiros (barulho) e persegue quem ve.
    // Navegacao A* contorna paredes e caixas, executada apenas no servidor.
    [RequireComponent(typeof(Rigidbody2D))]
    public partial class Monster : NetworkBehaviour
    {
        public float patrolSpeed = 1.2f;
        public float chaseSpeed = 3.4f;
        public float detectRadius = 6f;
        public float attackRange = 1.2f;
        public float attackCooldown = 0.8f;
        public int attackDamage = 15;
        public float chargeAimSeconds = 3f, chargeSpeed = 38f, chargeDuration = .5f, chargeRecovery = 2f, chargeCooldown = 8f;
        public int chargeDamage = 40;
        [InspectorName("Mostrar area da investida")]
        [Tooltip("Mostra uma faixa vermelha de preparacao e avanco. Pode ser desativada sem mudar o dano.")]
        public bool showChargeIndicator = true;
        public NetworkVariable<byte> ChargeState = new NetworkVariable<byte>(0);
        public NetworkVariable<float> ChargeAim = new NetworkVariable<float>(0);
        public NetworkVariable<float> ChargeReach = new NetworkVariable<float>(0);
        public NetworkVariable<bool> ChargeIndicatorEnabled = new NetworkVariable<bool>(true);
        float chargeUntil, nextCharge;
        Vector2 chargeDirection;
        PlayerInventory chargeTarget;
        CircleCollider2D bodyCollider;
        float worldRadius;
        SpriteRenderer indicatorBody, indicatorStart, indicatorEnd;
        GameObject indicatorRoot;
        readonly List<long> ignoredPairs = new List<long>();
        class CollisionPair { public Collider2D A, B; public int Users; public bool PreviouslyIgnored; }
        static readonly Dictionary<long, CollisionPair> collisionPairs = new Dictionary<long, CollisionPair>();

        Rigidbody2D rb;
        Health health;
        Esporifero spores;
        MonsterNavigation navigation;
        Vector2 home, wanderTarget, alertPos;
        float nextWander, alertUntil, nextAttack;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            spores = GetComponent<Esporifero>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            bodyCollider = GetComponent<CircleCollider2D>();
            worldRadius = bodyCollider != null ? bodyCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y)) : .55f;
        }

        public override void OnNetworkSpawn()
        {
            InitializeArchetype();
            if (!IsServer)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                return;
            }
            home = transform.position;
            wanderTarget = home;
            var circle = GetComponent<CircleCollider2D>();
            worldRadius = circle != null
                ? circle.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y))
                : 0.55f;
            navigation = new MonsterNavigation(worldRadius);
            ChargeIndicatorEnabled.Value = showChargeIndicator;
            Noise.OnNoise += HandleNoise;
            health.OnDiedServer += HandleDeath;
        }

        public override void OnNetworkDespawn()
        {
            All.Remove(this);
            Archetype.OnValueChanged -= RebuildAppearance;
            if (!IsServer) return;
            RestoreMonsterCollisions();
            Noise.OnNoise -= HandleNoise;
            if (health != null) health.OnDiedServer -= HandleDeath;
        }

        void HandleNoise(Vector2 pos, float radius)
        {
            if (Kind == MonsterKind.Mimic && AbilityState.Value == 0) return;
            if (Vector2.Distance(pos, rb.position) > (Kind == MonsterKind.Listener ? Mathf.Min(50, radius * 2.5f) : radius)) return;
            alertPos = pos;
            alertUntil = Time.time + 6f;
        }

        void HandleDeath(Health h)
        {
            RestoreMonsterCollisions();
            if (MatchManager.Instance != null)
            {
                MonsterWorld.AddCorpse(transform.position);
                MatchManager.Instance.ServerDropLoot(transform.position, GameConfig.MonsterLootValue);
            }
            NetworkObject.Despawn();
        }

        private void FixedUpdate()
        {
            if (!IsServer || health == null || health.IsDead) return;
            ChargeIndicatorEnabled.Value = showChargeIndicator;
            if (MatchManager.Instance == null || MatchManager.Instance.CurrentState != MatchState.Running)
            { rb.linearVelocity = Vector2.zero; ChargeState.Value = 0; ChargeReach.Value = 0; chargeTarget = null; RestoreMonsterCollisions(); return; }
            if (spores != null && spores.IsPreparing) { rb.linearVelocity = Vector2.zero; return; }

            bool nearby = false;
            foreach (var participant in PlayerInventory.All)
                if (participant != null && participant.IsAlive && Vector2.Distance(participant.transform.position,rb.position)<65) { nearby=true; break; }
            if (!nearby && ChargeState.Value == 0) { rb.linearVelocity=Vector2.zero; return; }

            if (Kind != MonsterKind.Charger) { StepArchetype(); return; }

            Vector2 pos = rb.position;
            if (ChargeState.Value == 1)
            {
                rb.linearVelocity=Vector2.zero;
                if (chargeTarget == null || !chargeTarget.IsSpawned || !chargeTarget.IsAlive) { Recover(); return; }
                // Mantem o alvo original mesmo fora do raio de aquisicao. Paredes ainda bloqueiam a visao.
                if (Physics2D.Linecast(pos,chargeTarget.transform.position,GameConfig.WallMask).collider == null)
                {
                    Vector2 direction=(Vector2)chargeTarget.transform.position-pos;
                    if(direction.sqrMagnitude>.0001f) ChargeAim.Value=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
                }
                rb.rotation=ChargeAim.Value;
                if (Time.time>=chargeUntil)
                {
                    float radians=ChargeAim.Value*Mathf.Deg2Rad;
                    chargeDirection=new Vector2(Mathf.Cos(radians),Mathf.Sin(radians));
                    ChargeState.Value=2; chargeUntil=Time.time+Mathf.Max(0,chargeDuration);
                    IgnoreMonsterCollisions();
                    StepCharge(pos);
                }
                return;
            }
            if (ChargeState.Value == 2)
            {
                StepCharge(pos);
                return;
            }
            if (ChargeState.Value == 3)
            { rb.linearVelocity=Vector2.zero; if(Time.time>=chargeUntil) ChargeState.Value=0; return; }
            Transform target = FindTarget(pos);
            if (spores==null && target!=null && Time.time>=nextCharge)
            {
                chargeTarget=target.GetComponent<PlayerInventory>();
                Vector2 direction=(Vector2)target.position-pos;
                ChargeAim.Value=direction.sqrMagnitude>.0001f ? Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg : rb.rotation;
                rb.rotation=ChargeAim.Value; ChargeReach.Value=Mathf.Max(0,chargeSpeed)*Mathf.Max(0,chargeDuration);
                ChargeState.Value=1; chargeUntil=Time.time+Mathf.Max(0,chargeAimSeconds); rb.linearVelocity=Vector2.zero; return;
            }
            Vector2 dest;
            float speed;

            if (target != null)
            {
                dest = target.position;
                speed = chaseSpeed;
                // Ao perder a linha de visao numa quina, contorna ate a ultima
                // posicao vista, sem conhecer a posicao escondida do jogador.
                alertPos = dest;
                alertUntil = Time.time + 6f;
                if (Vector2.Distance(pos, dest) <= attackRange && Time.time >= nextAttack)
                {
                    nextAttack = Time.time + attackCooldown;
                    var th = target.GetComponent<Health>();
                    if (th != null) th.ServerTakeDamage(attackDamage);
                }
            }
            else if (Time.time < alertUntil)
            {
                dest = alertPos;
                speed = chaseSpeed * 0.7f;
                if (Vector2.Distance(pos, dest) < 0.6f) alertUntil = 0f;
            }
            else
            {
                if (Time.time >= nextWander || Vector2.Distance(pos, wanderTarget) < 0.3f)
                {
                    wanderTarget = home + Random.insideUnitCircle * 3f;
                    nextWander = Time.time + Random.Range(3f, 6f);
                }
                dest = wanderTarget;
                speed = patrolSpeed;
            }

            if (navigation.TryGetWaypoint(pos, dest, out Vector2 waypoint))
            {
                Vector2 to = waypoint - pos;
                float distance = to.magnitude;
                rb.linearVelocity = to.normalized * Mathf.Min(speed, distance / Time.fixedDeltaTime);
                if (distance > 0.01f) rb.rotation = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        void StepCharge(Vector2 pos)
        {
            float remaining=chargeUntil-Time.time;
            if(remaining<=0) { Recover(); return; }
            float step=Mathf.Max(0,chargeSpeed)*Mathf.Min(Time.fixedDeltaTime,remaining);
            ChargeReach.Value=Mathf.Max(0,chargeSpeed)*remaining;
            var hits=Physics2D.CircleCastAll(pos,worldRadius,chargeDirection,step);
            foreach(var hit in hits)
            {
                if(hit.collider.isTrigger || hit.collider.GetComponentInParent<NetworkObject>()==NetworkObject) continue;
                if(hit.collider.GetComponentInParent<Monster>()!=null || hit.collider.GetComponentInParent<PlantCreature>()!=null) continue;
                var victim=hit.collider.GetComponentInParent<PlayerInventory>();
                if(victim!=null && !victim.IsAlive) continue;
                bool obstacle=(GameConfig.WallMask & (1<<hit.collider.gameObject.layer)) != 0;
                if(victim==null && !obstacle) continue;
                rb.position=pos+chargeDirection*Mathf.Max(0,hit.distance-.04f);
                if(victim!=null)
                {
                    var victimHealth=victim.GetComponent<Health>();
                    if(victimHealth!=null) victimHealth.ServerTakeDamage(chargeDamage);
                }
                Recover(); return;
            }
            rb.linearVelocity=chargeDirection*(step/Time.fixedDeltaTime);
        }

        void Recover()
        {
            rb.linearVelocity=Vector2.zero; ChargeState.Value=3; ChargeReach.Value=0; chargeTarget=null;
            chargeUntil=Time.time+Mathf.Max(0,chargeRecovery); nextCharge=Time.time+Mathf.Max(0,chargeCooldown);
            RestoreMonsterCollisions();
        }

        void IgnoreMonsterCollisions()
        {
            if(bodyCollider==null) return;
            foreach(var candidate in Health.All)
            {
                if(candidate==null || candidate==health || (candidate.GetComponent<Monster>()==null && candidate.GetComponent<PlantCreature>()==null)) continue;
                var other=candidate.GetComponent<Collider2D>(); if(other==null) continue;
                int a=bodyCollider.GetInstanceID(), b=other.GetInstanceID();
                long key=((long)Mathf.Min(a,b)<<32) | (uint)Mathf.Max(a,b);
                if(!collisionPairs.TryGetValue(key,out var pair))
                {
                    pair=new CollisionPair { A=bodyCollider,B=other,PreviouslyIgnored=Physics2D.GetIgnoreCollision(bodyCollider,other) };
                    collisionPairs.Add(key,pair);
                }
                pair.Users++; ignoredPairs.Add(key); Physics2D.IgnoreCollision(bodyCollider,other,true);
            }
        }
        void RestoreMonsterCollisions()
        {
            foreach(long key in ignoredPairs)
            {
                if(!collisionPairs.TryGetValue(key,out var pair)) continue;
                pair.Users--;
                if(pair.Users<=0)
                {
                    if(pair.A!=null && pair.B!=null) Physics2D.IgnoreCollision(pair.A,pair.B,pair.PreviouslyIgnored);
                    collisionPairs.Remove(key);
                }
            }
            ignoredPairs.Clear();
        }

        void LateUpdate()
        {
            if(IsSpawned) UpdateAppearance();
            bool visible=IsSpawned && ChargeIndicatorEnabled.Value && (ChargeState.Value==1 || ChargeState.Value==2) && !health.IsDead;
            var nm=Unity.Netcode.NetworkManager.Singleton;
            var local=nm!=null && nm.IsClient && nm.SpawnManager!=null ? nm.SpawnManager.GetLocalPlayerObject() : null;
            visible=visible && local!=null && CyberExtraction.Bots.BotController.InVision(local.transform.position,local.transform.eulerAngles.z,transform.position) &&
                Physics2D.Linecast(local.transform.position,transform.position,GameConfig.VisionMask).collider==null;
            if(!visible) { if(indicatorRoot!=null) indicatorRoot.SetActive(false); return; }
            if(indicatorRoot==null)
            {
                indicatorRoot=new GameObject("ChargeArea");
                indicatorBody=IndicatorSprite("Trajectory",SpriteFactory.Square);
                indicatorStart=IndicatorSprite("Start",SpriteFactory.Circle);
                indicatorEnd=IndicatorSprite("End",SpriteFactory.Circle);
            }
            indicatorRoot.SetActive(true);
            float radians=ChargeAim.Value*Mathf.Deg2Rad;
            var direction=new Vector2(Mathf.Cos(radians),Mathf.Sin(radians)); var origin=(Vector2)transform.position;
            float length=ChargeReach.Value;
            var wall=Physics2D.CircleCast(origin,worldRadius,direction,length,GameConfig.WallMask);
            if(wall.collider!=null) length=Mathf.Max(0,wall.distance-.04f);
            indicatorBody.transform.position=origin+direction*length*.5f;
            indicatorBody.transform.rotation=Quaternion.Euler(0,0,ChargeAim.Value);
            indicatorBody.transform.localScale=new Vector3(length,worldRadius*2,1);
            indicatorStart.transform.position=origin; indicatorEnd.transform.position=origin+direction*length;
            indicatorStart.transform.localScale=indicatorEnd.transform.localScale=new Vector3(worldRadius*2,worldRadius*2,1);
        }
        SpriteRenderer IndicatorSprite(string name,Sprite sprite)
        {
            var go=new GameObject(name); go.transform.SetParent(indicatorRoot.transform,false);
            var renderer=go.AddComponent<SpriteRenderer>(); SpriteFactory.Setup(renderer,sprite,new Color(1,.18f,.08f,.36f),4); return renderer;
        }
        public override void OnDestroy()
        {
            RestoreMonsterCollisions(); if(indicatorRoot!=null) Destroy(indicatorRoot); base.OnDestroy();
        }

        void OnGUI()
        {
            DrawAbilityWarning();
            if(IsServer || !IsSpawned || ChargeState.Value==0 || Camera.main==null) return;
            var local=NetworkManager.SpawnManager.GetLocalPlayerObject();
            if(local==null || !CyberExtraction.Bots.BotController.InVision(local.transform.position,local.transform.eulerAngles.z,transform.position) ||
                Physics2D.Linecast(local.transform.position,transform.position,GameConfig.VisionMask).collider!=null) return;
            var point=Camera.main.WorldToScreenPoint(transform.position); var old=GUI.matrix; GUI.matrix=Matrix4x4.identity;
            var style=new GUIStyle(GUI.skin.box) { fontSize=20,alignment=TextAnchor.MiddleCenter };
            GUI.Box(new Rect(point.x-65,Screen.height-point.y-60,130,32),ChargeState.Value==1 ? "INVESTIDA!" : ChargeState.Value==3 ? "VULNERAVEL" : "AVANCANDO",style); GUI.matrix=old;
        }

        private void OnDrawGizmosSelected()
        {
            if (navigation == null) return;
            Gizmos.color = Color.cyan;
            navigation.DrawPath();
        }

        Transform FindTarget(Vector2 pos)
        {
            Transform best = null;
            float bestD = detectRadius;
            foreach (var inv in PlayerInventory.All)
            {
                if (inv == null || !inv.IsAlive) continue;
                Vector2 p = inv.transform.position;
                float d = Vector2.Distance(pos, p);
                if (d > bestD) continue;
                if (Physics2D.Linecast(pos, p, GameConfig.VisionMask)) continue;
                bestD = d;
                best = inv.transform;
            }
            return best;
        }
    }
}
