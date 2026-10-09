using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Combat;
using CyberExtraction.Match;
using CyberExtraction.Items;

namespace CyberExtraction.Players
{
    // Arma, municao, ferrolho, recarga, dispersao e dano decididos no servidor.
    public class PlayerShooter : NetworkBehaviour
    {
        PlayerInventory inv;
        readonly LineRenderer[] tracers = new LineRenderer[8];
        float tracerUntil, recycledMetal;
        double nextShotAt;
        int reloadSlot = -1;
        ItemStack reloadStack;
        Vector2 lastPos;
        float serverSpeed;
        public NetworkVariable<double> ReloadEndsAt = new NetworkVariable<double>(0);
        public NetworkVariable<double> BoltEndsAt = new NetworkVariable<double>(0);
        double Now => NetworkManager != null ? NetworkManager.ServerTime.Time : 0;
        public bool Reloading => ReloadEndsAt.Value > Now;
        public bool CyclingBolt => BoltEndsAt.Value > Now;
        public WeaponDefinition Weapon => inv != null ? inv.ActiveWeapon : null;
        public int Magazine => inv != null ? inv.WeaponRounds : 0;
        public int Reserve => Weapon != null ? inv.Count(Weapon.Ammo) : 0;
        public float FireCooldown => Weapon != null ? Weapon.Cooldown : GameConfig.FireCooldown;
        public float Range => Weapon != null ? Weapon.Range : GameConfig.AimRange;

        void Awake()
        {
            inv = GetComponent<PlayerInventory>();
            if (GetComponent<WeaponVisual>() == null) gameObject.AddComponent<WeaponVisual>();
            for (int i = 0; i < tracers.Length; i++)
            {
                var go = new GameObject("Tracer" + i);
                var line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = true; line.positionCount = 2;
                line.startWidth = line.endWidth = .045f;
                line.startColor = line.endColor = new Color(1f, .9f, .5f, .9f);
                line.sortingOrder = 8;
                if (SpriteFactory.Unlit != null) line.sharedMaterial = SpriteFactory.Unlit;
                line.enabled = false; tracers[i] = line;
            }
        }
        public override void OnNetworkSpawn() { lastPos = transform.position; }
        public override void OnDestroy()
        {
            foreach (var line in tracers) if (line != null) Destroy(line.gameObject);
            base.OnDestroy();
        }
        bool CanAct => IsServer && inv != null && inv.IsAlive && Weapon != null &&
            MatchManager.Instance != null && MatchManager.Instance.CurrentState == MatchState.Running;
        bool SameReloadWeapon => inv != null && reloadSlot >= 0 && reloadSlot == inv.ActiveWeaponSlot &&
            reloadSlot < inv.Slots.Count && inv.Slots[reloadSlot].Equals(reloadStack);
        void Update()
        {
            if (Time.time >= tracerUntil) foreach (var line in tracers) line.enabled = false;
            if (!IsServer) return;
            if (ReloadEndsAt.Value > 0)
            {
                if (!CanAct || !SameReloadWeapon) ServerCancelReload();
                else if (Now >= ReloadEndsAt.Value)
                {
                    var weapon = Weapon;
                    int amount = Mathf.Min(weapon.Capacity - Magazine, Reserve);
                    int rounds = Magazine;
                    ServerCancelReload();
                    if (amount > 0 && inv.ServerRemoveItem(weapon.Ammo, amount)) inv.ServerSetRounds(rounds + amount);
                }
            }
            if (!CanAct || Now >= BoltEndsAt.Value) BoltEndsAt.Value = 0;
            Vector2 position = transform.position;
            if (Time.deltaTime > 0) serverSpeed = Mathf.Lerp(serverSpeed, (position - lastPos).magnitude / Time.deltaTime, .2f);
            lastPos = position;
        }
        public void RequestFire(float angleDeg) { if (IsOwner) FireServerRpc(angleDeg); }
        public void RequestReload() { if (IsOwner) ReloadServerRpc(); }
        [ServerRpc] void FireServerRpc(float angleDeg) { ServerFire(angleDeg); }
        [ServerRpc] void ReloadServerRpc() { ServerReload(); }
        public void ServerCancelReload()
        {
            if (!IsServer) return;
            ReloadEndsAt.Value = 0; reloadSlot = -1;
        }
        public void ServerReload()
        {
            if (!CanAct || Magazine >= Weapon.Capacity || Reserve <= 0 || Reloading || CyclingBolt) return;
            reloadSlot = inv.ActiveWeaponSlot; reloadStack = inv.Slots[reloadSlot];
            ReloadEndsAt.Value = Now + Weapon.ReloadSeconds;
        }
        public void ServerFire(float angleDeg)
        {
            if (!CanAct || float.IsNaN(angleDeg) || float.IsInfinity(angleDeg) || Reloading || CyclingBolt || Now < nextShotAt) return;
            if (Magazine <= 0) { ServerReload(); return; }
            var weapon = Weapon;
            nextShotAt = Now + weapon.Cooldown;
            if (weapon.BoltSeconds > 0) BoltEndsAt.Value = Now + weapon.BoltSeconds;
            inv.ServerSetRounds(Magazine - 1);
            int level = Mathf.Clamp(inv.RecyclerLevel.Value, 0, 3);
            recycledMetal += .25f * level * .1f;
            if (recycledMetal >= 1f)
            {
                recycledMetal -= 1f;
                if (!inv.ServerAddItem(ItemId.Metal, 1)) MatchManager.Instance.ServerDropItem(transform.position, ItemId.Metal, 1);
            }
            Vector2 origin = transform.position;
            float half = weapon.HalfAngle(serverSpeed / GameConfig.PlayerSpeed);
            var ends = new Vector2[weapon.Pellets];
            var directions = new Vector2[ends.Length];
            var pelletHits = new RaycastHit2D[ends.Length][];
            // Captura todos os impactos antes de dano/morte desativar colisores.
            for (int pellet = 0; pellet < ends.Length; pellet++)
            {
                float angle = (Mathf.Repeat(angleDeg, 360f) + Random.Range(-half, half)) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                directions[pellet] = dir;
                pelletHits[pellet] = Physics2D.RaycastAll(origin, dir, weapon.Range);
            }
            for (int pellet = 0; pellet < ends.Length; pellet++)
                ends[pellet] = ResolvePellet(origin, directions[pellet], weapon, pelletHits[pellet]);
            Noise.Emit(origin, weapon.NoiseRadius);
            ShotFxClientRpc(origin, ends);
        }
        Vector2 ResolvePellet(Vector2 origin, Vector2 dir, WeaponDefinition weapon, RaycastHit2D[] hits)
        {
            foreach (var hit in hits)
            {
                if (hit.collider.isTrigger || hit.collider.GetComponentInParent<NetworkObject>() == NetworkObject) continue;
                var health = hit.collider.GetComponentInParent<Health>();
                if (health != null)
                {
                    // O corpo continua bloqueando os outros chumbos do mesmo disparo.
                    if (!health.IsDead) health.ServerTakeDamage(weapon.DamageAt(hit.distance));
                    return hit.point;
                }
                return hit.point;
            }
            return origin + dir * weapon.Range;
        }
        [ClientRpc] void ShotFxClientRpc(Vector2 from, Vector2[] ends)
        {
            for (int i = 0; i < tracers.Length; i++)
            {
                tracers[i].enabled = i < ends.Length;
                if (i >= ends.Length) continue;
                tracers[i].SetPosition(0, from); tracers[i].SetPosition(1, ends[i]);
            }
            tracerUntil = Time.time + .08f;
        }
    }
}
