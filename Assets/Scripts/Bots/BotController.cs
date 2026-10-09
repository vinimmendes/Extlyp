using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Combat;
using CyberExtraction.Enemies;
using CyberExtraction.Match;
using CyberExtraction.Players;

namespace CyberExtraction.Bots
{
    // Bot = participante de extracao. Toda a IA roda no servidor.
    // Percepcao -> politica -> destino/mira -> fisica e tiro compartilhado.
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerInventory), typeof(PlayerShooter))]
    public class BotController : NetworkBehaviour
    {
        public BotDifficulty difficulty = BotDifficulty.Medium;
        public NetworkVariable<byte> State = new NetworkVariable<byte>((byte)BotState.Idle);
        public NetworkVariable<byte> Difficulty = new NetworkVariable<byte>((byte)BotDifficulty.Medium);
        public BotState CurrentState => (BotState)State.Value;

        Rigidbody2D rb;
        PlayerInventory inventory;
        PlayerShooter shooter;
        Health health;
        MonsterNavigation navigation;
        BotProfile profile;
        Health threat;
        Vector2 destination, lastSeen, heardPosition, combatPosition;
        float nextThink, reactionAt, nextShot, noiseUntil, memoryUntil, repositionAt;
        float retreatUntil, nextError, aimError;
        float bodyRadius;
        int routeOffset;
        readonly bool[] explored = new bool[MapBuilder.LootPoints.Length];
        readonly Dictionary<ulong, KnownLoot> lootMemory = new Dictionary<ulong, KnownLoot>();
        readonly List<ulong> removeKeys = new List<ulong>();
        readonly HashSet<ulong> visibleLoot = new HashSet<ulong>();

        struct KnownLoot
        {
            public Vector2 position;
            public int value;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            inventory = GetComponent<PlayerInventory>();
            shooter = GetComponent<PlayerShooter>();
            health = GetComponent<Health>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            // Mesmo marcador de direcao do player; nao instala camera nem controles.
            var nose = new GameObject("Nose");
            nose.transform.SetParent(transform, false);
            nose.transform.localPosition = new Vector3(0.5f, 0f, 0f);
            nose.transform.localScale = new Vector3(0.5f, 0.25f, 1f);
            SpriteFactory.Setup(nose.AddComponent<SpriteRenderer>(), SpriteFactory.Square, Color.white, 6);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) { rb.bodyType = RigidbodyType2D.Kinematic; return; }
            Difficulty.Value = (byte)difficulty;
            profile = BotProfile.For(difficulty);
            var circle = GetComponent<CircleCollider2D>();
            bodyRadius = circle.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            navigation = new MonsterNavigation(bodyRadius);
            destination = rb.position;
            nextThink = Time.time + Random.Range(0f, profile.ThinkInterval);
            routeOffset = Random.Range(0, explored.Length);
            Noise.OnNoise += HearNoise;
            health.Current.OnValueChanged += OnHealthChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;
            Noise.OnNoise -= HearNoise;
            if (health != null) health.Current.OnValueChanged -= OnHealthChanged;
        }

        void OnHealthChanged(int previous, int current)
        {
            if (current < previous) nextThink = 0f;
            // Dano nao informa a posicao do atacante: ainda precisa ver/ouvir.
        }

        void HearNoise(Vector2 position, float radius)
        {
            var mm = MatchManager.Instance;
            if (!inventory.IsAlive || mm == null || mm.CurrentState != MatchState.Running) return;
            float distance = Vector2.Distance(position, rb.position);
            if (distance < 0.5f || distance > radius || Random.value < profile.IgnoreNoiseChance) return;
            heardPosition = position;
            noiseUntil = Time.time + profile.MemoryTime;
        }

        public static bool InVision(Vector2 origin, float facing, Vector2 point)
        {
            Vector2 delta = point - origin;
            float distance = delta.magnitude;
            if (distance > GameConfig.VisionRange) return false;
            if (distance <= GameConfig.NearRadius) return true;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            return Mathf.Abs(Mathf.DeltaAngle(facing, angle)) <= GameConfig.VisionAngle * 0.5f;
        }

        bool CanSee(Vector2 point)
        {
            return InVision(rb.position, rb.rotation, point) &&
                Physics2D.Linecast(rb.position, point, GameConfig.WallMask).collider == null;
        }

        Health FindThreat()
        {
            Health best = null;
            float bestScore = float.NegativeInfinity;
            foreach (var candidate in Health.All)
            {
                if (candidate == null || candidate == health || candidate.IsDead || !candidate.IsSpawned) continue;
                var inv = candidate.GetComponent<PlayerInventory>();
                if (inv != null && !inv.IsAlive) continue;
                Vector2 position = candidate.transform.position;
                if (!CanSee(position)) continue;
                float distance = Vector2.Distance(position, rb.position);
                // Monstro perto tem prioridade: nao deixa melee atacar enquanto mira outro player.
                float score = 12f - distance;
                if (candidate.GetComponent<Monster>() != null && distance < 4f) score += 5f;
                if (candidate == threat) score += 0.75f; // evita trocar de alvo a cada decisao
                if (score <= bestScore) continue;
                bestScore = score;
                best = candidate;
            }
            return best;
        }

        void ObserveLoot()
        {
            visibleLoot.Clear();
            foreach (var loot in LootPickup.All)
            {
                if (loot == null || !loot.IsSpawned || !CanSee(loot.transform.position)) continue;
                visibleLoot.Add(loot.NetworkObjectId);
                lootMemory[loot.NetworkObjectId] = new KnownLoot { position = loot.transform.position, value = loot.Value.Value };
            }
            // Um loot sumir da rede nao revela a coleta atraves da parede.
            // Corrige a memoria apenas quando ve novamente o local onde ele estava.
            removeKeys.Clear();
            foreach (var pair in lootMemory)
                if (CanSee(pair.Value.position) && !visibleLoot.Contains(pair.Key)) removeKeys.Add(pair.Key);
            foreach (var key in removeKeys) lootMemory.Remove(key);
        }

        bool SelectLoot(out Vector2 position)
        {
            position = rb.position;
            float bestScore = float.NegativeInfinity;
            foreach (var pair in lootMemory)
            {
                var loot = pair.Value;
                float distance = Vector2.Distance(rb.position, loot.position);
                float danger = Time.time < noiseUntil ? Mathf.Max(0f, 6f - Vector2.Distance(loot.position, heardPosition)) : 0f;
                float score = loot.value / Mathf.Max(1f, distance) - danger * profile.CoverPreference;
                if (score <= bestScore) continue;
                bestScore = score;
                position = loot.position;
            }
            return bestScore > float.NegativeInfinity;
        }

        bool FinishedExploring()
        {
            foreach (bool visited in explored) if (!visited) return false;
            return lootMemory.Count == 0;
        }

        Vector2 ExploreDestination()
        {
            // Continua patrulhando se ja visitou o mapa antes de liberar a extracao.
            bool allVisited = true;
            foreach (bool visited in explored) if (!visited) { allVisited = false; break; }
            if (allVisited) System.Array.Clear(explored, 0, explored.Length);
            float bestDistance = float.PositiveInfinity;
            Vector2 best = rb.position;
            for (int j = 0; j < explored.Length; j++)
            {
                int i = (j + routeOffset) % explored.Length;
                if (explored[i]) continue;
                float distance = (MapBuilder.LootPoints[i] - rb.position).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = MapBuilder.LootPoints[i];
            }
            return best;
        }

        void Think()
        {
            if (health.Current.Value <= health.maxHealth - CyberExtraction.Items.ItemCatalog.BandageHeal)
                inventory.ServerUseBandage();
            ObserveLoot();
            for (int i = 0; i < explored.Length; i++)
                if (Vector2.Distance(rb.position, MapBuilder.LootPoints[i]) < 1.8f && CanSee(MapBuilder.LootPoints[i]))
                    explored[i] = true;
            Health observed = FindThreat();
            if (observed != threat)
            {
                threat = observed;
                if (threat != null) reactionAt = Time.time + profile.ReactionTime;
                repositionAt = 0f;
            }
            if (threat != null)
            {
                lastSeen = threat.transform.position;
                memoryUntil = Time.time + profile.MemoryTime;
            }
            bool knownLoot = SelectLoot(out Vector2 lootGoal);
            var mm = MatchManager.Instance;
            var situation = new BotSituation
            {
                Active = inventory.IsAlive && mm.CurrentState == MatchState.Running,
                HasThreat = threat != null,
                ThreatIsMonster = threat != null && threat.GetComponent<Monster>() != null,
                ThreatDistance = threat != null ? Vector2.Distance(rb.position, lastSeen) : float.PositiveInfinity,
                HealthRatio = (float)health.Current.Value / health.maxHealth,
                KnownLoot = knownLoot, HeardNoise = Time.time < noiseUntil || Time.time < memoryUntil,
                FinishedExploring = FinishedExploring(), Loot = inventory.LootValue.Value, SecondsLeft = mm.SecondsLeft.Value
            };
            BotState chosen = BotDecision.Select(situation, profile);
            // Recua por um pequeno periodo, mesmo se o inimigo sair momentaneamente do cone.
            if (CurrentState == BotState.Retreat && threat == null && Time.time < retreatUntil &&
                Vector2.Distance(rb.position, destination) > 0.6f) chosen = BotState.Retreat;
            if (chosen != CurrentState) State.Value = (byte)chosen;
            switch (chosen)
            {
                case BotState.Collect: destination = lootGoal; break;
                case BotState.Explore: destination = ExploreDestination(); break;
                case BotState.Investigate:
                    destination = Time.time < memoryUntil ? lastSeen : heardPosition;
                    if (Vector2.Distance(rb.position, destination) < 0.8f)
                    { memoryUntil = noiseUntil = 0f; destination = ExploreDestination(); }
                    break;
                case BotState.Extract: destination = MapBuilder.NearestExtraction(rb.position); break;
                case BotState.Retreat:
                    if (Time.time >= repositionAt || Vector2.Distance(rb.position, destination) < 0.6f)
                    {
                        destination = TacticalPosition(lastSeen, true);
                        repositionAt = Time.time + 1.2f;
                        retreatUntil = Time.time + 2f;
                    }
                    break;
                case BotState.Engage:
                    if (difficulty == BotDifficulty.Easy)
                        destination = situation.ThreatDistance > 5.5f ? lastSeen : rb.position;
                    else if (Time.time >= repositionAt)
                    {
                        combatPosition = TacticalPosition(lastSeen, false);
                        destination = combatPosition;
                        repositionAt = Time.time + 1.5f;
                    }
                    break;
            }
            nextThink = Time.time + profile.ThinkInterval;
        }

        Vector2 TacticalPosition(Vector2 enemy, bool retreat)
        {
            Vector2 best = rb.position;
            float bestScore = float.NegativeInfinity;
            // Amostra espacos livres. O A* verifica a rota, nao atravessa cobertura.
            for (int ring = 0; ring <= 2; ring++)
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI / 8f;
                Vector2 candidate = rb.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ring * 2f;
                if (Mathf.Abs(candidate.x) > MapBuilder.Width / 2 - .6f || Mathf.Abs(candidate.y) > MapBuilder.Height / 2 - .6f ||
                    Physics2D.OverlapCircle(candidate, bodyRadius + 0.05f, GameConfig.WallMask) != null) continue;
                bool hidden = Physics2D.Linecast(enemy, candidate, GameConfig.WallMask).collider != null;
                float distance = Vector2.Distance(candidate, enemy);
                float travel = Vector2.Distance(candidate, rb.position);
                bool nearCover = Physics2D.OverlapCircle(candidate, 1.5f, GameConfig.WallMask) != null;
                float score;
                if (retreat)
                    score = distance + (hidden ? 6f * profile.CoverPreference : 0f) - travel * 0.4f
                        - Vector2.Distance(candidate, MapBuilder.ExtractionCenter) * 0.06f;
                else
                {
                    if (hidden || distance > GameConfig.VisionRange - 0.5f || distance < 2.5f) continue;
                    score = -Mathf.Abs(distance - 5.5f) * 2f - travel * 0.35f +
                        (nearCover ? 2f * profile.CoverPreference : 0f);
                }
                if (score <= bestScore) continue;
                bestScore = score;
                best = candidate;
            }
            return best;
        }

        private void FixedUpdate()
        {
            if (!IsServer) return;
            var mm = MatchManager.Instance;
            if (mm == null || mm.CurrentState != MatchState.Running || !inventory.IsAlive)
            {
                rb.linearVelocity = Vector2.zero;
                if (State.Value != (byte)BotState.Idle) State.Value = (byte)BotState.Idle;
                return;
            }
            if (Time.time >= nextThink) Think();
            Vector2 waypoint = rb.position;
            bool havePath = navigation.TryGetWaypoint(rb.position, destination, out waypoint);
            Vector2 travel = havePath ? waypoint - rb.position : Vector2.zero;
            bool visibleThreat = threat != null && !threat.IsDead && CanSee(threat.transform.position);
            Vector2 look = visibleThreat ? (Vector2)threat.transform.position - rb.position : travel;
            if (look.sqrMagnitude < 0.01f)
            {
                // Ao esperar/extrair, verifica o entorno, sem visao de 360 graus.
                rb.rotation += profile.TurnSpeed * 0.12f * Time.fixedDeltaTime;
            }
            else
            {
                if (Time.time >= nextError)
                {
                    aimError = Random.Range(-profile.AimError, profile.AimError);
                    nextError = Time.time + GameConfig.FireCooldown;
                }
                float heading = Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg + (visibleThreat ? aimError : 0f);
                rb.rotation = Mathf.MoveTowardsAngle(rb.rotation, heading, profile.TurnSpeed * Time.fixedDeltaTime);
            }
            bool brace = CurrentState == BotState.Engage && visibleThreat && Time.time >= reactionAt &&
                nextShot - Time.time < (difficulty == BotDifficulty.Easy ? 0.05f : 0.15f);
            rb.linearVelocity = brace ? Vector2.zero : travel.normalized *
                Mathf.Min(GameConfig.PlayerSpeed, travel.magnitude / Time.fixedDeltaTime);
            if (!visibleThreat || Time.time < reactionAt || Time.time < nextShot) return;
            float targetAngle = Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle(rb.rotation, targetAngle)) > profile.AimError + 3f) return;
            shooter.ServerFire(rb.rotation);
            nextShot = Time.time + GameConfig.FireCooldown * (difficulty == BotDifficulty.Easy ? 1.5f : 1f);
        }

        private void OnDrawGizmosSelected()
        {
            if (navigation == null) return;
            Gizmos.color = Color.yellow;
            navigation.DrawPath();
            Gizmos.DrawWireSphere(destination, 0.25f);
        }
    }
}
