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
    // Percepcao -> politica (BotDecision) -> plano/destino/mira -> fisica e tiro compartilhado.
    // O bot joga como uma pessoa: tem plano com compromisso (nao troca de ideia a cada decisao),
    // conhece o mapa em graus diferentes, calcula quando precisa sair para extrair e tem erros humanos.
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerInventory), typeof(PlayerShooter))]
    public class BotController : NetworkBehaviour
    {
        public BotDifficulty difficulty = BotDifficulty.Medium;
        public NetworkVariable<byte> State = new NetworkVariable<byte>((byte)BotState.Idle);
        public NetworkVariable<byte> Difficulty = new NetworkVariable<byte>((byte)BotDifficulty.Medium);
        public BotState CurrentState => (BotState)State.Value;

        const float DetourFactor = 1.35f; // caminhos reais sao mais longos que a linha reta

        Rigidbody2D rb;
        PlayerInventory inventory;
        PlayerShooter shooter;
        Health health;
        MonsterNavigation navigation;
        BotProfile profile;
        float bodyRadius;
        Vector2 spawnPosition;

        // Percepcao e memoria
        Health threat;
        Vector2 destination, lastSeen, heardPosition, glancePosition;
        float reactionAt, noiseUntil, memoryUntil, glanceUntil, trackTime;
        bool[] explored;
        float[] pointRetryUntil;
        int[] pointSites;
        bool[] siteKnown;
        float[] sitePreference;
        readonly Dictionary<ulong, KnownLoot> lootMemory = new Dictionary<ulong, KnownLoot>();
        readonly Dictionary<ulong, float> ignoredLoot = new Dictionary<ulong, float>();
        readonly List<ulong> removeKeys = new List<ulong>();
        readonly HashSet<ulong> visibleLoot = new HashSet<ulong>();

        // Plano com compromisso
        int targetPoint = -1;
        float planCommitUntil, stateLockUntil;
        bool hasLootTarget;
        ulong lootTargetId;
        float lootCommitUntil;
        Vector2 chosenExit, avoidExit, wanderDestination;
        float exitRecheckAt, avoidExitUntil, wanderUntil;
        bool exitLatched;
        int chosenExitId = -1;

        // Combate
        float nextThink, nextShot, nextError, aimError, aimNoise, burstPauseUntil, strafeUntil, repositionAt, retreatUntil, coverUntil;
        int burstShots, strafeSign = 1;
        bool strafing;
        Vector2 combatPosition;

        // Travamento
        Vector2 stuckAnchor;
        float stuckCheckAt;

        struct KnownLoot
        {
            public Vector2 position;
            public int value;
            public CyberExtraction.Items.ItemStack stack;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            inventory = GetComponent<PlayerInventory>();
            shooter = GetComponent<PlayerShooter>();
            health = GetComponent<Health>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) { rb.bodyType = RigidbodyType2D.Kinematic; return; }
            Difficulty.Value = (byte)difficulty;
            profile = BotProfile.For(difficulty);
            explored = new bool[MapBuilder.LootPoints.Length];
            pointRetryUntil = new float[explored.Length];
            siteKnown = new bool[MapBuilder.Sites.Length];
            sitePreference = new float[siteKnown.Length];
            pointSites = new int[explored.Length];
            for (int i=0;i<pointSites.Length;i++) pointSites[i]=SiteForPoint(i);
            var circle = GetComponent<CircleCollider2D>();
            bodyRadius = (circle != null ? circle.radius : .5f) * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            navigation = new MonsterNavigation(bodyRadius);
            spawnPosition = rb.position;
            destination = rb.position;
            stuckAnchor = rb.position;
            stuckCheckAt = Time.time + 1.5f;
            nextThink = Time.time + Random.Range(0f, profile.ThinkInterval);

            // Cada bot tem conhecimento e gostos proprios, fixos durante a partida.
            for (int i = 0; i < MapBuilder.Sites.Length; i++)
            {
                bool nearHome = Vector2.Distance(MapBuilder.Sites[i], spawnPosition) < 60f;
                siteKnown[i] = nearHome || Random.value < profile.MapKnowledge;
                sitePreference[i] = Random.Range(0f, 25f) * profile.Whim;
            }
            chosenExit = ChooseExit();
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
            // Jogador experiente vira o rosto na direcao do barulho (so olha, nao descobre quem e).
            if (profile.TurnsToSounds)
            {
                glancePosition = position;
                glanceUntil = Time.time + 1.2f;
            }
        }

        public static bool InVision(Vector2 origin, float facing, Vector2 point)
        {
            Vector2 delta = point - origin;
            float distance = delta.magnitude;
            if (distance > GameConfig.VisionRange) return false;
            if (distance <= GameConfig.NearRadius) return true;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            return distance <= GameConfig.VisionRadiusAtAngle(Mathf.DeltaAngle(facing, angle));
        }

        bool CanSee(Vector2 point)
        {
            return InVision(rb.position, rb.rotation, point) &&
                Physics2D.Linecast(rb.position, point, GameConfig.VisionMask).collider == null;
        }

        // ---------------------------------------------------------------- percepcao

        Health FindThreat()
        {
            Health best = null;
            float bestScore = float.NegativeInfinity;
            foreach (var candidate in Health.All)
            {
                if (candidate == null || candidate == health || candidate.IsDead || !candidate.IsSpawned) continue;
                var monster = candidate.GetComponent<Monster>();
                if (monster != null && monster.Disguised) continue;
                var inv = candidate.GetComponent<PlayerInventory>();
                if (inv != null && !inv.IsAlive) continue;
                Vector2 position = candidate.transform.position;
                if (!CanSee(position)) continue;
                float distance = Vector2.Distance(position, rb.position);
                // Monstro perto tem prioridade: nao deixa melee atacar enquanto mira outro player.
                float score = 12f - distance;
                if ((monster != null || candidate.GetComponent<PlantCreature>() != null) && distance < 4f) score += 5f;
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
                if (loot == null || !loot.IsSpawned || loot.Chest.Value || !CanSee(loot.transform.position)) continue;
                visibleLoot.Add(loot.NetworkObjectId);
                lootMemory[loot.NetworkObjectId] = new KnownLoot { position = loot.transform.position, value = loot.Value.Value,
                    stack = new CyberExtraction.Items.ItemStack((CyberExtraction.Items.ItemId)loot.Item.Value,loot.Quantity.Value,loot.Quality.Value,loot.Data.Value) };
            }
            // Um loot sumir da rede nao revela a coleta atraves da parede.
            // Corrige a memoria apenas quando ve novamente o local onde ele estava.
            removeKeys.Clear();
            foreach (var pair in lootMemory)
                if (CanSee(pair.Value.position) && !visibleLoot.Contains(pair.Key)) removeKeys.Add(pair.Key);
            foreach (var key in removeKeys)
            {
                lootMemory.Remove(key);
                if (hasLootTarget && lootTargetId == key) hasLootTarget = false;
            }
        }

        // Um ponto de loot so e marcado quando o bot esta ao lado dele e o enxerga.
        void MarkExplored()
        {
            for (int i = 0; i < explored.Length; i++)
            {
                if (explored[i]) continue;
                if (Vector2.Distance(rb.position, MapBuilder.LootPoints[i]) < 1.8f && CanSee(MapBuilder.LootPoints[i]))
                    explored[i] = true;
            }
        }

        // Ruinas que estao a vista passam a ser conhecidas, mesmo para quem nao conhece o mapa.
        void DiscoverSites()
        {
            for (int i = 0; i < siteKnown.Length; i++)
                if (!siteKnown[i] && CanSee(MapBuilder.Sites[i]))
                    siteKnown[i] = true;
        }

        // ---------------------------------------------------------------- planos

        bool SelectLoot(out Vector2 position)
        {
            position = rb.position;
            var capacity = inventory.Grid();
            removeKeys.Clear();
            foreach (var pair in ignoredLoot) if (Time.time >= pair.Value) removeKeys.Add(pair.Key);
            foreach (var id in removeKeys) ignoredLoot.Remove(id);
            // Mantem o loot escolhido por um tempo: nao troca de alvo por diferencas pequenas de distancia.
            if (hasLootTarget && Time.time < lootCommitUntil && lootMemory.TryGetValue(lootTargetId, out var current) && capacity.FreeFor(current.stack) >= current.stack.Quantity)
            {
                position = current.position;
                return true;
            }
            float bestScore = float.NegativeInfinity;
            ulong bestId = 0;
            foreach (var pair in lootMemory)
            {
                if (ignoredLoot.TryGetValue(pair.Key, out float until) && Time.time < until) continue;
                var loot = pair.Value;
                if (capacity.FreeFor(loot.stack) < loot.stack.Quantity) continue;
                float distance = Vector2.Distance(rb.position, loot.position);
                float danger = Time.time < noiseUntil ? Mathf.Max(0f, 6f - Vector2.Distance(loot.position, heardPosition)) : 0f;
                float score = loot.value / Mathf.Max(1f, distance) - danger * profile.CoverPreference;
                if (score <= bestScore) continue;
                bestScore = score;
                bestId = pair.Key;
                position = loot.position;
            }
            hasLootTarget = bestScore > float.NegativeInfinity;
            if (hasLootTarget)
            {
                lootTargetId = bestId;
                lootCommitUntil = Time.time + 2f;
            }
            return hasLootTarget;
        }

        bool AllExplored()
        {
            foreach (bool visited in explored) if (!visited) return false;
            return true;
        }

        // Escolhe o proximo local a explorar e mantem a escolha enquanto o compromisso valer.
        int ChooseExplorePoint()
        {
            if (targetPoint >= 0 && !explored[targetPoint] && Time.time >= pointRetryUntil[targetPoint] && Time.time < planCommitUntil) return targetPoint;
            if (targetPoint >= 0 && explored[targetPoint] && Time.time < planCommitUntil)
            {
                int remaining = NextPointInSite(pointSites[targetPoint]);
                if (remaining >= 0) { targetPoint=remaining; return targetPoint; }
            }

            var mm = MatchManager.Instance;
            int seconds = mm != null ? mm.SecondsLeft.Value : 0;
            float elapsed = GameConfig.MatchDuration - seconds;

            for (int attempt = 0; attempt < 2; attempt++)
            {
                int bestSite = -1;
                float bestScore = float.NegativeInfinity;
                for (int i = 0; i < MapBuilder.Sites.Length; i++)
                {
                    if (!siteKnown[i] || NextPointInSite(i) < 0) continue;
                    Vector2 site = MapBuilder.Sites[i];
                    float d = Vector2.Distance(rb.position, site);
                    float score = -d + sitePreference[i];
                    if (Time.time < noiseUntil)
                    {
                        float nd = Vector2.Distance(site, heardPosition);
                        if (nd < 40f) score -= (40f - nd) * profile.CoverPreference;
                    }
                    if (profile.PlanAhead > 0f)
                    {
                        float back = ExitDistance(site);
                        score -= back * 0.35f * profile.PlanAhead;
                        float needed = (d * DetourFactor + back) / GameConfig.PlayerSpeed + profile.ExtractMargin + GameConfig.ExtractionHoldTime;
                        if (needed > seconds - BotDecision.ExtractionUnlockSeconds) score -= 500f; // nao daria tempo de voltar
                    }
                    if (profile.AvoidsSpawns && elapsed < 90f)
                    {
                        foreach (var spawn in MapBuilder.SpawnPoints)
                            if (Vector2.Distance(spawn, spawnPosition) > 5f && Vector2.Distance(site, spawn) < 45f) score -= 60f;
                    }
                    if (score <= bestScore) continue;
                    bestScore = score;
                    bestSite = i;
                }
                if (bestSite >= 0)
                {
                    targetPoint = NextPointInSite(bestSite);
                    planCommitUntil = Time.time + profile.CommitTime;
                    return targetPoint;
                }
                // Nada conhecido para visitar: se o mapa conhecido acabou, recomeca a ronda.
                bool anyKnownLeft = false;
                for (int i = 0; i < siteKnown.Length; i++) if (siteKnown[i]) { anyKnownLeft = true; break; }
                if (anyKnownLeft) System.Array.Clear(explored, 0, explored.Length);
                else break;
            }
            targetPoint = -1;
            return -1;
        }

        // Sem destino conhecido (iniciante): segue numa direcao por um tempo, descobrindo ruinas no caminho.
        Vector2 WanderDestination()
        {
            if (Time.time >= wanderUntil || Vector2.Distance(rb.position, wanderDestination) < 3f)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector2 target = rb.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 40f;
                target.x = Mathf.Clamp(target.x, -MapBuilder.Width / 2 + 5f, MapBuilder.Width / 2 - 5f);
                target.y = Mathf.Clamp(target.y, -MapBuilder.Height / 2 + 5f, MapBuilder.Height / 2 - 5f);
                wanderDestination = target;
                wanderUntil = Time.time + Random.Range(12f, 20f);
            }
            return wanderDestination;
        }

        int SiteForPoint(int point)
        {
            int best = -1; float distance = float.PositiveInfinity;
            for (int i=0;i<MapBuilder.Sites.Length;i++)
            {
                float d=(MapBuilder.LootPoints[point]-MapBuilder.Sites[i]).sqrMagnitude;
                if(d<distance) { distance=d; best=i; }
            }
            return best;
        }
        int NextPointInSite(int site)
        {
            int best=-1; float distance=float.PositiveInfinity;
            for(int i=0;i<explored.Length;i++)
            {
                if(explored[i] || Time.time<pointRetryUntil[i] || pointSites[i]!=site) continue;
                float d=(MapBuilder.LootPoints[i]-rb.position).sqrMagnitude;
                if(d<distance) { distance=d; best=i; }
            }
            return best;
        }
        float ExitDistance(Vector2 from) => MapBuilder.Current==ExpeditionMap.Large && chosenExitId>=0
            ? LargeMapPlan.TravelDistance(from,chosenExitId) : Vector2.Distance(from,chosenExit)*DetourFactor;
        Vector2 ChooseExit()
        {
            if(MapBuilder.Current!=ExpeditionMap.Large) { chosenExitId=-1; return MapBuilder.ExtractionPoints[0]; }
            ushort known=inventory.KnownActiveExits.Value;
            int best=-1; float score=float.NegativeInfinity;
            for(int i=0;i<LargeMapPlan.Exits.Length;i++)
            {
                if((known&(1<<i))==0) continue;
                Vector2 point=LargeMapPlan.Exits[i].Position;
                float candidate=-LargeMapPlan.TravelDistance(rb.position,i);
                if(i==chosenExitId) candidate+=20;
                if(Time.time<avoidExitUntil && (point-avoidExit).sqrMagnitude<1) candidate-=100;
                if(Time.time<noiseUntil) candidate-=Mathf.Max(0,40-Vector2.Distance(point,heardPosition))*profile.PlanAhead;
                if(candidate>score) { score=candidate; best=i; }
            }
            chosenExitId=best;
            // No access to the global active mask: only confirmed/discovered exits.
            return best>=0 ? LargeMapPlan.Exits[best].Position : rb.position;
        }

        // Ponto de espera ~10 m antes da zona; ali o bot aguarda a liberacao da extracao.
        Vector2 Overwatch(Vector2 exit)
        {
            Vector2 from = rb.position - exit;
            if (from.magnitude <= 11f) return rb.position;
            Vector2 point = exit + from.normalized * 10f;
            point.x = Mathf.Clamp(point.x, -MapBuilder.Width / 2 + 2f, MapBuilder.Width / 2 - 2f);
            point.y = Mathf.Clamp(point.y, -MapBuilder.Height / 2 + 2f, MapBuilder.Height / 2 - 2f);
            return point;
        }

        void TryHeal()
        {
            float ratio = (float)health.Current.Value / health.maxHealth;
            bool needs = health.Current.Value <= health.maxHealth - CyberExtraction.Items.ItemCatalog.BandageHeal;
            if (!needs || ratio > profile.HealBelow) return;
            if (threat != null && ratio >= 0.3f) return; // em luta so cura quando esta mal
            inventory.ServerUseBandage();
        }

        static bool Calm(BotState s) => s == BotState.Explore || s == BotState.Collect || s == BotState.Investigate;

        void Think()
        {
            var mm = MatchManager.Instance;
            DiscoverSites();
            MarkExplored();
            ObserveLoot();

            Health observed = FindThreat();
            if (observed != threat)
            {
                threat = observed;
                if (threat != null)
                {
                    reactionAt = Time.time + profile.ReactionTime * Random.Range(0.8f, 1.25f);
                    trackTime = 0f;
                    burstShots = 0;
                }
                repositionAt = 0f;
            }
            if (threat != null)
            {
                lastSeen = threat.transform.position;
                memoryUntil = Time.time + profile.MemoryTime;
            }

            TryHeal();
            if (Time.time >= exitRecheckAt)
            {
                chosenExit = ChooseExit();
                exitRecheckAt = Time.time + 8f;
            }
            // Depois de decidir sair, nao volta atras (o calculo de tempo e uma estimativa).
            float eta = exitLatched ? 9999f :
                ExitDistance(rb.position) / Mathf.Max(.1f,GameConfig.PlayerSpeed * inventory.ServerMovementMultiplier) + profile.ExtractMargin + GameConfig.ExtractionHoldTime;

            bool knownLoot = SelectLoot(out Vector2 lootGoal);
            var situation = new BotSituation
            {
                Active = inventory.IsAlive && mm.CurrentState == MatchState.Running,
                HasThreat = threat != null,
                ThreatIsMonster = threat != null && (threat.GetComponent<Monster>() != null || threat.GetComponent<PlantCreature>() != null),
                ThreatDistance = threat != null ? Vector2.Distance(rb.position, lastSeen) : float.PositiveInfinity,
                HealthRatio = (float)health.Current.Value / health.maxHealth,
                KnownLoot = knownLoot,
                HeardNoise = Time.time < noiseUntil || Time.time < memoryUntil,
                FinishedExploring = AllExplored(),
                Loot = inventory.LootValue.Value,
                SecondsLeft = mm.SecondsLeft.Value,
                ExtractEta = eta
            };
            BotState chosen = BotDecision.Select(situation, profile);

            // Recua por um pequeno periodo, mesmo se o inimigo sair momentaneamente do cone.
            if (CurrentState == BotState.Retreat && threat == null && Time.time < retreatUntil &&
                Vector2.Distance(rb.position, destination) > 0.6f) chosen = BotState.Retreat;
            // Entre tarefas calmas, nao muda de ideia mais de uma vez por segundo.
            if (chosen != CurrentState && Calm(chosen) && Calm(CurrentState) && Time.time < stateLockUntil && (CurrentState != BotState.Collect || knownLoot)) chosen = CurrentState;
            if (chosen == BotState.Extract) exitLatched = true;
            if (chosen != CurrentState)
            {
                State.Value = (byte)chosen;
                stateLockUntil = Time.time + 1f;
            }

            switch (chosen)
            {
                case BotState.Collect:
                    destination = lootGoal;
                    break;
                case BotState.Explore:
                    int point = ChooseExplorePoint();
                    destination = point >= 0 ? MapBuilder.LootPoints[point] : WanderDestination();
                    break;
                case BotState.Investigate:
                    destination = Time.time < memoryUntil ? lastSeen : heardPosition;
                    if (Vector2.Distance(rb.position, destination) < 0.8f)
                    {
                        memoryUntil = noiseUntil = 0f;
                        destination = rb.position;
                        nextThink = 0f; // decide de novo logo, sem ficar parado
                    }
                    break;
                case BotState.Extract:
                    destination = BotDecision.CanExtract(mm.SecondsLeft.Value) ? chosenExit : Overwatch(chosenExit);
                    break;
                case BotState.Retreat:
                    if (Time.time >= repositionAt || Vector2.Distance(rb.position, destination) < 0.6f)
                    {
                        destination = TacticalPosition(lastSeen, true);
                        repositionAt = Time.time + 1.2f;
                        retreatUntil = Time.time + 2f;
                    }
                    break;
                case BotState.Engage:
                    destination = CombatDestination();
                    break;
            }
            nextThink = Time.time + profile.ThinkInterval;
        }

        // ---------------------------------------------------------------- combate

        // Mantem a distancia de luta do perfil, desloca-se de lado entre rajadas e, se experiente
        // e machucado, procura cobertura. Nada de reamostrar posicao aleatoria a cada instante.
        Vector2 CombatDestination()
        {
            Vector2 enemy = lastSeen;
            Vector2 away = rb.position - enemy;
            float distance = away.magnitude;
            Vector2 dir = distance > 0.01f ? away / distance : Vector2.right;

            // Experiente machucado (ou com o inimigo colado) busca cobertura e mantem a escolha por 3 s.
            if (profile.CoverPreference >= 1f)
            {
                if (Time.time < coverUntil && Vector2.Distance(rb.position, combatPosition) > 0.8f) return combatPosition;
                if (Time.time >= coverUntil && (health.Current.Value < health.maxHealth * 0.7f || distance < 3f))
                {
                    combatPosition = TacticalPosition(enemy, false);
                    coverUntil = Time.time + 3f;
                    return combatPosition;
                }
            }

            float range = profile.PreferredRange;
            if (distance > range + 1.5f) return enemy + dir * range;       // aproxima
            if (distance < range - 1.5f) return rb.position + dir * 2f;    // afasta

            if (Time.time >= strafeUntil)
            {
                strafing = Random.value < profile.StrafeChance;
                strafeSign = Random.value < 0.5f ? -1 : 1;
                strafeUntil = Time.time + Random.Range(1.0f, 2.2f);
            }
            if (!strafing) return rb.position;
            Vector2 side = new Vector2(-dir.y, dir.x) * strafeSign;
            if (Physics2D.CircleCast(rb.position, bodyRadius, side, 2f, GameConfig.WallMask).collider != null)
            {
                strafeSign = -strafeSign;
                side = -side;
            }
            return rb.position + side * 2f;
        }

        Vector2 TacticalPosition(Vector2 enemy, bool retreat)
        {
            Vector2 best = rb.position;
            float bestScore = float.NegativeInfinity;
            // Amostra espacos livres. O A* verifica a rota, nao atravessa cobertura.
            for (int ring = 1; ring <= 3; ring++)
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI / 8f;
                Vector2 candidate = rb.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ring * 2f;
                if (Mathf.Abs(candidate.x) > MapBuilder.Width / 2 - .6f || Mathf.Abs(candidate.y) > MapBuilder.Height / 2 - .6f ||
                    Physics2D.OverlapCircle(candidate, bodyRadius + 0.05f, GameConfig.WallMask) != null) continue;
                bool hidden = Physics2D.Linecast(enemy, candidate, GameConfig.VisionMask).collider != null;
                float distance = Vector2.Distance(candidate, enemy);
                float travel = Vector2.Distance(candidate, rb.position);
                bool nearCover = Physics2D.OverlapCircle(candidate, 1.5f, GameConfig.WallMask) != null;
                float score;
                if (retreat)
                    score = distance + (hidden ? 6f * profile.CoverPreference : 0f) - travel * 0.4f
                        - Vector2.Distance(candidate, chosenExit) * 0.01f;
                else
                {
                    if (hidden || distance > GameConfig.VisionRange - 0.5f || distance < 2.5f) continue;
                    score = -Mathf.Abs(distance - profile.PreferredRange) * 2f - travel * 0.35f +
                        (nearCover ? 2f * profile.CoverPreference : 0f);
                }
                if (score <= bestScore) continue;
                bestScore = score;
                best = candidate;
            }
            return best;
        }

        // ---------------------------------------------------------------- travamento

        void GiveUpTarget()
        {
            switch (CurrentState)
            {
                case BotState.Explore:
                    if (targetPoint >= 0) pointRetryUntil[targetPoint] = Time.time + 20f;
                    targetPoint = -1;
                    planCommitUntil = 0f;
                    wanderUntil = 0f;
                    break;
                case BotState.Collect:
                    if (hasLootTarget)
                    {
                        ignoredLoot[lootTargetId] = Time.time + 30f;
                        lootMemory.Remove(lootTargetId);
                        hasLootTarget = false;
                    }
                    break;
                case BotState.Investigate:
                    memoryUntil = noiseUntil = 0f;
                    break;
                case BotState.Extract:
                    avoidExit = chosenExit;
                    avoidExitUntil = Time.time + 20f;
                    exitRecheckAt = 0f;
                    break;
            }
            stateLockUntil = 0f;
            nextThink = 0f;
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

            if (Time.time >= stuckCheckAt)
            {
                bool travelling = Calm(CurrentState) || CurrentState == BotState.Extract;
                if (travelling && Vector2.Distance(rb.position, destination) > 2.5f &&
                    Vector2.Distance(rb.position, stuckAnchor) < 0.5f) GiveUpTarget();
                stuckAnchor = rb.position;
                stuckCheckAt = Time.time + 1.5f;
            }

            Vector2 waypoint = rb.position;
            bool havePath = navigation.TryGetWaypoint(rb.position, destination, out waypoint);
            Vector2 travel = havePath ? waypoint - rb.position : Vector2.zero;
            bool visibleThreat = threat != null && !threat.IsDead && CanSee(threat.transform.position);
            trackTime = visibleThreat ? trackTime + Time.fixedDeltaTime : Mathf.Max(0f, trackTime - Time.fixedDeltaTime * 2f);

            Vector2 look = travel;
            if (visibleThreat) look = (Vector2)threat.transform.position - rb.position;
            else if (Time.time < glanceUntil) look = glancePosition - rb.position;

            if (look.sqrMagnitude < 0.01f)
            {
                // Ao esperar/extrair, verifica o entorno, sem visao de 360 graus.
                rb.rotation += profile.TurnSpeed * 0.12f * Time.fixedDeltaTime;
            }
            else
            {
                if (Time.time >= nextError)
                {
                    // A mira assenta com o tempo no alvo: o erro cai de 100% para 40% do nivel.
                    aimNoise = profile.AimError * Mathf.Lerp(1f, 0.4f, Mathf.Clamp01(trackTime / profile.SettleTime));
                    aimError = Random.Range(-aimNoise, aimNoise);
                    nextError = Time.time + GameConfig.FireCooldown;
                }
                float heading = Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg + (visibleThreat ? aimError : 0f);
                rb.rotation = Mathf.MoveTowardsAngle(rb.rotation, heading, profile.TurnSpeed * Time.fixedDeltaTime);
            }

            bool reactionReady = visibleThreat && Time.time >= reactionAt;
            bool pausing = Time.time < burstPauseUntil;
            // Quem nao atira andando para um instante antes do tiro (o cone abre quando anda).
            bool brace = !profile.ShootWhileMoving && CurrentState == BotState.Engage && reactionReady && !pausing &&
                nextShot - Time.time < 0.15f;
            rb.linearVelocity = brace ? Vector2.zero : travel.normalized *
                Mathf.Min(GameConfig.PlayerSpeed * inventory.ServerMovementMultiplier, travel.magnitude / Time.fixedDeltaTime);

            if (!reactionReady || pausing || Time.time < nextShot) return;
            float targetAngle = Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle(rb.rotation, targetAngle)) > aimNoise + 3f) return;
            int roundsBefore = shooter.Magazine;
            shooter.ServerFire(rb.rotation);
            nextShot = Time.time + shooter.FireCooldown * (difficulty == BotDifficulty.Easy ? 1.5f : 1f);
            if (shooter.Magazine >= roundsBefore) return; // Reload/blocked muzzle did not fire a burst shot.
            if (++burstShots >= profile.BurstSize)
            {
                burstShots = 0;
                burstPauseUntil = Time.time + profile.BurstPause; // reposiciona entre as rajadas
            }
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
