using Unity.Netcode;
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
    public class Monster : NetworkBehaviour
    {
        public float patrolSpeed = 1.2f;
        public float chaseSpeed = 3.4f;
        public float detectRadius = 6f;
        public float attackRange = 1.2f;
        public float attackCooldown = 0.8f;
        public int attackDamage = 15;

        Rigidbody2D rb;
        Health health;
        MonsterNavigation navigation;
        Vector2 home, wanderTarget, alertPos;
        float nextWander, alertUntil, nextAttack;
        Esporifero spores;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            spores = GetComponent<Esporifero>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                return;
            }
            home = transform.position;
            wanderTarget = home;
            var circle = GetComponent<CircleCollider2D>();
            float worldRadius = circle != null
                ? circle.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y))
                : 0.55f;
            navigation = new MonsterNavigation(worldRadius);
            Noise.OnNoise += HandleNoise;
            health.OnDiedServer += HandleDeath;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;
            Noise.OnNoise -= HandleNoise;
            if (health != null) health.OnDiedServer -= HandleDeath;
        }

        void HandleNoise(Vector2 pos, float radius)
        {
            if (Vector2.Distance(pos, rb.position) > radius) return;
            alertPos = pos;
            alertUntil = Time.time + 6f;
        }

        void HandleDeath(Health h)
        {
            if (MatchManager.Instance != null)
                MatchManager.Instance.ServerDropLoot(transform.position, GameConfig.MonsterLootValue);
            NetworkObject.Despawn();
        }

        private void FixedUpdate()
        {
            if (!IsServer || health == null || health.IsDead) return;
            if (MatchManager.Instance == null || MatchManager.Instance.CurrentState != MatchState.Running ||
                (spores != null && spores.IsPreparing))
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 pos = rb.position;
            Transform target = FindTarget(pos);
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
                if (Physics2D.Linecast(pos, p, GameConfig.WallMask)) continue;
                bestD = d;
                best = inv.transform;
            }
            return best;
        }
    }
}

