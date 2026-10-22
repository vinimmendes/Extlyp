using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Combat;
using CyberExtraction.Core;
using CyberExtraction.Items;
using CyberExtraction.Match;
using CyberExtraction.Players;

namespace CyberExtraction.Enemies
{
    [RequireComponent(typeof(NetworkObject), typeof(Health), typeof(Rigidbody2D))]
    public class PlantCreature : NetworkBehaviour
    {
        public static readonly List<PlantCreature> All = new List<PlantCreature>();
        public NetworkVariable<byte> Kind = new NetworkVariable<byte>((byte)PlantKind.Mother);
        public NetworkVariable<NetworkObjectReference> Mother = new NetworkVariable<NetworkObjectReference>();
        public NetworkVariable<double> BornAt = new NetworkVariable<double>(0);
        public NetworkVariable<double> MatureAt = new NetworkVariable<double>(0);
        public PlantKind CurrentKind => (PlantKind)Kind.Value;
        public Health Health { get; private set; }
        public bool Alive => IsSpawned && Health != null && !Health.IsDead;
        public PlantCreature MotherCreature
        {
            get
            {
                if (!Mother.Value.TryGet(out NetworkObject obj, NetworkManager) || obj == null) return null;
                return obj.GetComponent<PlantCreature>();
            }
        }
        PlantKind initialKind;
        PlantCreature initialMother;
        NetworkObjectReference selfReference;
        Rigidbody2D body;
        CircleCollider2D circle;
        MonsterNavigation navigation;
        PlantVisual visual;
        double nextSpawn, nextHeal, nextAttack, pendingHealAt;
        double staggerUntil;
        NetworkObjectReference pendingHealMother;
        Vector2 lastSeen;
        double investigateUntil;
        double Now => NetworkManager.ServerTime.Time;
        bool Running => MatchManager.Instance != null && MatchManager.Instance.CurrentState == MatchState.Running;

        void Awake()
        {
            Health = GetComponent<Health>(); body = GetComponent<Rigidbody2D>();
            circle = GetComponent<CircleCollider2D>(); visual = GetComponent<PlantVisual>();
            body.gravityScale = 0; body.freezeRotation = true;
        }
        // Configura a vida ANTES de Health.OnNetworkSpawn inicializa-la.
        public void Configure(PlantKind kind, PlantCreature mother = null)
        {
            if (IsSpawned) return;
            initialKind = kind; initialMother = mother;
            GetComponent<Health>().maxHealth = PlantRules.MaxHealth(kind);
        }
        public override void OnNetworkSpawn()
        {
            All.Add(this);
            Kind.OnValueChanged += KindChanged;
            if (IsServer)
            {
                selfReference = new NetworkObjectReference(NetworkObject);
                Kind.Value = (byte)initialKind;
                if (initialMother != null) Mother.Value = new NetworkObjectReference(initialMother.NetworkObject);
                BornAt.Value = Now;
                MatureAt.Value = initialKind == PlantKind.AttackBulb ? Now + PlantRules.MaturationSeconds : 0;
                nextSpawn = Now + 2; nextHeal = Now + PlantRules.HealInterval;
                Health.OnDiedServer += Died;
                Health.OnDamagedServer += MinionStagger;
            }
            ApplyKind();
        }
        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                Health.OnDiedServer -= Died;
                Health.OnDamagedServer -= MinionStagger;
                if (CurrentKind == PlantKind.Mother) ClearChildren();
            }
            Kind.OnValueChanged -= KindChanged; All.Remove(this);
        }
        void KindChanged(byte previous, byte current)
        {
            ApplyKind();
            if (IsClient && previous == (byte)PlantKind.AttackBulb && current == (byte)PlantKind.Minion)
                visual?.BeginMaturationBurst();
        }
        void ApplyKind()
        {
            if (circle != null) circle.radius = PlantRules.Radius(CurrentKind);
            body.linearVelocity = Vector2.zero;
            body.bodyType = IsServer && CurrentKind == PlantKind.Minion ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            if (IsServer && CurrentKind == PlantKind.Minion) navigation = new MonsterNavigation(PlantRules.Radius(CurrentKind));
        }
        void FixedUpdate()
        {
            if (!IsServer || !Alive) return;
            if (!Running) { body.linearVelocity = Vector2.zero; pendingHealAt = 0; return; }
            if (CurrentKind == PlantKind.Mother) { GrowBulbs(); return; }
            var mother = MotherCreature;
            if (mother == null || !mother.Alive)
            {
                NetworkObject.Despawn(); return;
            }
            if (CurrentKind == PlantKind.AttackBulb)
            {
                if (Now >= MatureAt.Value) Mature();
            }
            else if (CurrentKind == PlantKind.HealingBulb) HealMother(mother);
            else MoveAndAttack(mother);
        }
        void GrowBulbs()
        {
            if (Now < nextSpawn) return;
            nextSpawn = Now + PlantRules.SpawnInterval;
            if (FindPlayer(PlantRules.ActivationRange) == null) return;
            int attackers = 0, healers = 0;
            foreach (var child in All)
            {
                if (child == null || !child.Alive || child.MotherCreature != this) continue;
                if (PlantRules.IsAttacker(child.CurrentKind)) attackers++;
                else if (child.CurrentKind == PlantKind.HealingBulb) healers++;
            }
            PlantKind kind = healers == 0 || (attackers >= 2 && healers < PlantRules.MaxHealers)
                ? PlantKind.HealingBulb : PlantKind.AttackBulb;
            if (!PlantRules.CanSpawn(attackers, healers, kind))
            {
                kind = kind == PlantKind.AttackBulb ? PlantKind.HealingBulb : PlantKind.AttackBulb;
                if (!PlantRules.CanSpawn(attackers, healers, kind)) return;
            }
            for (int attempt = 0; attempt < 20; attempt++)
            {
                Vector2 direction = Random.insideUnitCircle;
                if (direction.sqrMagnitude < .01f) continue;
                Vector2 pos = (Vector2)transform.position + direction.normalized * Random.Range(2.3f, 4f);
                if (!PlantEncounter.ClearSpawn(pos, PlantRules.Radius(kind))) continue;
                if (Physics2D.Linecast(transform.position, pos, GameConfig.WallMask).collider != null) continue;
                PlantEncounter.Spawn(kind, pos, this); return;
            }
        }
        void Mature()
        {
            // Nao cria outro objeto: preserva identidade, dano recebido e limite da colonia.
            int previousMaximum = Health.Maximum.Value;
            Health.maxHealth = PlantRules.MinionHealth;
            Health.Maximum.Value = Health.maxHealth;
            Health.Current.Value = Mathf.Max(1, Mathf.CeilToInt((float)Health.Current.Value / previousMaximum * Health.maxHealth));
            Kind.Value = (byte)PlantKind.Minion;
        }
        bool CanHealMother(PlantCreature mother)
        {
            if (mother == null || !mother.Alive) return false;
            float distance = Vector2.Distance(transform.position, mother.transform.position);
            bool clear = Physics2D.Linecast(transform.position, mother.transform.position, GameConfig.WallMask).collider == null;
            return PlantRules.CanHeal(mother.Alive, mother.Health.Current.Value, mother.Health.Maximum.Value, distance, clear);
        }
        void HealMother(PlantCreature mother)
        {
            if (pendingHealAt > 0)
            {
                if (Now < pendingHealAt) return;
                pendingHealAt = 0;
                // Revalida no impacto: bulbo/mae vivos, vinculo, alcance, parede e vida faltando.
                if (Mother.Value.Equals(pendingHealMother) && CanHealMother(mother) && mother.Health.ServerHeal(PlantRules.HealAmount))
                    HealArrivedClientRpc(Mother.Value, Now);
                return;
            }
            if (Now < nextHeal) return;
            nextHeal = Now + PlantRules.HealInterval;
            if (!CanHealMother(mother)) return;
            double launchedAt = Now;
            pendingHealMother = Mother.Value;
            pendingHealAt = launchedAt + PlantRules.HealArrivalSeconds;
            LaunchHealClientRpc(Mother.Value, launchedAt);
        }
        [ClientRpc] void LaunchHealClientRpc(NetworkObjectReference reference, double launchedAt)
        {
            if (!reference.TryGet(out NetworkObject obj, NetworkManager) || obj == null) return;
            visual?.BeginHealing(obj.GetComponent<PlantVisual>(), launchedAt);
        }
        [ClientRpc] void HealArrivedClientRpc(NetworkObjectReference reference, double arrivedAt)
        {
            if (!reference.TryGet(out NetworkObject obj, NetworkManager) || obj == null) return;
            obj.GetComponent<PlantVisual>()?.FlashHealing(arrivedAt);
        }
        PlayerInventory FindPlayer(float range)
        {
            PlayerInventory nearest = null; float best = range;
            foreach (var player in PlayerInventory.All)
            {
                if (player == null || !player.IsSpawned || !player.IsAlive) continue;
                float distance = Vector2.Distance(transform.position, player.transform.position);
                if (distance > best || Physics2D.Linecast(transform.position, player.transform.position, GameConfig.VisionMask).collider != null) continue;
                best = distance; nearest = player;
            }
            return nearest;
        }
        void MoveAndAttack(PlantCreature mother)
        {
            Vector2 pos = body.position;
            var target = FindPlayer(PlantRules.MinionDetectRange);
            Vector2 motherPos = mother.transform.position;
            if (target != null && Vector2.Distance(target.transform.position, motherPos) > PlantRules.LeashRange) target = null;
            Vector2 goal = motherPos;
            if (target != null)
            {
                goal = target.transform.position; lastSeen = goal; investigateUntil = Now + 3;
                if (Vector2.Distance(pos, goal) <= PlantRules.AttackRange && Now >= nextAttack)
                {
                    nextAttack = Now + PlantRules.AttackCooldown;
                    target.GetComponent<Health>()?.ServerTakeDamage(PlantRules.AttackDamage,creatureName:
                        CurrentKind==PlantKind.AttackBulb ? "Bulbo de ataque" : "Servo vegetal");
                }
            }
            else if (Now < investigateUntil) goal = lastSeen;
            else if (Vector2.Distance(pos, motherPos) < 2.2f) { body.linearVelocity = Vector2.zero; return; }
            if(Now<staggerUntil) { body.linearVelocity=Vector2.zero; return; }
            if (navigation == null) navigation = new MonsterNavigation(PlantRules.Radius(CurrentKind));
            if (!navigation.TryGetWaypoint(pos, goal, out Vector2 waypoint)) { body.linearVelocity = Vector2.zero; return; }
            Vector2 delta = waypoint - pos;
            body.linearVelocity = delta.normalized * Mathf.Min(PlantRules.MinionSpeed, delta.magnitude / Time.fixedDeltaTime);
            if (delta.sqrMagnitude > .001f)
                body.rotation = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f; // sprite aponta para cima
        }
        void Died(Health ignored)
        {
            if (!IsServer || !IsSpawned) return;
            if(CurrentKind==PlantKind.Mother || CurrentKind==PlantKind.Minion) MonsterWorld.AddCorpse(transform.position);
            if (CurrentKind == PlantKind.Mother)
            {
                ClearChildren();
                MatchManager.Instance?.ServerDropLoot(transform.position, GameConfig.MonsterLootValue,CyberExtraction.Items.LootTheme.Organic);
            }
            NetworkObject.Despawn();
        }
        void MinionStagger(Health source,int damage)
        {
            if(CurrentKind!=PlantKind.Minion || damage<=0 || source.IsDead) return;
            staggerUntil=Now+.1; body.linearVelocity=Vector2.zero;
        }
        void ClearChildren()
        {
            var children = new List<PlantCreature>(All);
            foreach (var child in children)
                if (child != null && child != this && child.IsSpawned && child.Mother.Value.Equals(selfReference))
                    child.NetworkObject.Despawn();
        }
    }
}
