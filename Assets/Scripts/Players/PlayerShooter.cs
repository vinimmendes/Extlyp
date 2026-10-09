using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Combat;
using CyberExtraction.Match;
using CyberExtraction.Items;

namespace CyberExtraction.Players
{
    // Tiro 100% decidido no servidor: sorteia a direcao dentro do cone de mira e faz o raycast.
    public class PlayerShooter : NetworkBehaviour
    {
        PlayerInventory inv;
        LineRenderer tracer;
        float tracerUntil;
        float lastShot = -10f;
        float recycledMetal;
        public NetworkVariable<double> ReloadEndsAt = new NetworkVariable<double>(0);
        public bool Reloading => ReloadEndsAt.Value > NetworkManager.ServerTime.Time;
        public int Magazine => inv != null ? inv.PistolRounds : 0;
        public int Reserve => inv != null ? inv.Count(ItemId.PistolAmmo) : 0;

        // velocidade medida no servidor (para abrir/fechar o cone)
        Vector2 lastPos;
        float serverSpeed;

        private void Awake()
        {
            inv = GetComponent<PlayerInventory>();

            var go = new GameObject("Tracer");
            tracer = go.AddComponent<LineRenderer>();
            tracer.useWorldSpace = true;
            tracer.positionCount = 2;
            tracer.startWidth = tracer.endWidth = 0.06f;
            tracer.startColor = tracer.endColor = new Color(1f, 0.9f, 0.5f, 0.9f);
            tracer.sortingOrder = 8;
            var m = SpriteFactory.Unlit;
            if (m != null) tracer.sharedMaterial = m;
            tracer.enabled = false;
        }

        public override void OnDestroy()
        {
            if (tracer != null) Destroy(tracer.gameObject);
            base.OnDestroy();
        }

        private void Update()
        {
            if (tracer.enabled && Time.time >= tracerUntil) tracer.enabled = false;

            if (!IsServer) return;
            if (ReloadEndsAt.Value > 0 && NetworkManager.ServerTime.Time >= ReloadEndsAt.Value)
            {
                ReloadEndsAt.Value = 0;
                if (inv.IsAlive && inv.PistolSlot() >= 0 && MatchManager.Instance != null && MatchManager.Instance.CurrentState == MatchState.Running)
                {
                    int amount = Mathf.Min(12 - Magazine, Reserve);
                    if (amount > 0 && inv.ServerRemoveItem(ItemId.PistolAmmo, amount)) inv.ServerSetRounds(Magazine + amount);
                }
            }
            Vector2 p = transform.position;
            float dt = Time.deltaTime;
            if (dt > 0f)
            {
                float inst = (p - lastPos).magnitude / dt;
                serverSpeed = Mathf.Lerp(serverSpeed, inst, 0.2f);
            }
            lastPos = p;
        }

        public void RequestFire(float angleDeg) { FireServerRpc(angleDeg); }
        public void RequestReload() { if (IsOwner) ReloadServerRpc(); }
        [ServerRpc] void ReloadServerRpc() { ServerReload(); }
        public void ServerReload()
        {
            if (!IsServer || !inv.IsAlive || inv.PistolSlot() < 0 || Magazine >= 12 || Reserve <= 0 || Reloading ||
                MatchManager.Instance == null || MatchManager.Instance.CurrentState != MatchState.Running) return;
            ReloadEndsAt.Value = NetworkManager.ServerTime.Time + 1.5;
        }

        [ServerRpc]
        void FireServerRpc(float angleDeg)
        {
            ServerFire(angleDeg);
        }

        // Bots usam a mesma resolucao/cadencia/dano, sem simular entrada do cliente.
        public void ServerFire(float angleDeg)
        {
            if (!IsServer) return;
            if (float.IsNaN(angleDeg) || float.IsInfinity(angleDeg)) return;
            var mm = MatchManager.Instance;
            if (mm == null || mm.CurrentState != MatchState.Running) return;
            if (inv == null || !inv.IsAlive) return;
            if (inv.PistolSlot() < 0 || Reloading) return;
            if (Magazine <= 0) { ServerReload(); return; }
            if (Time.time - lastShot < GameConfig.FireCooldown * 0.9f) return;
            lastShot = Time.time;
            inv.ServerSetRounds(Magazine - 1);
            int level = Mathf.Clamp(inv.RecyclerLevel.Value, 0, 3);
            recycledMetal += 0.25f * level * 0.1f;
            if (recycledMetal >= 1f)
            {
                recycledMetal -= 1f;
                if (!inv.ServerAddItem(ItemId.Metal, 1)) MatchManager.Instance.ServerDropItem(transform.position, ItemId.Metal, 1);
            }

            float speed01 = Mathf.Clamp01(serverSpeed / GameConfig.PlayerSpeed);
            float half = Mathf.Lerp(GameConfig.AimHalfAngleStill, GameConfig.AimHalfAngleMoving, speed01);
            float a = (angleDeg + Random.Range(-half, half)) * Mathf.Deg2Rad;

            Vector2 origin = transform.position;
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Vector2 end = origin + dir * GameConfig.AimRange;

            var hits = Physics2D.RaycastAll(origin, dir, GameConfig.AimRange);
            foreach (var hit in hits)
            {
                // Areas de coleta/extracao nao sao cobertura. Filtro local ao tiro:
                // nao altera a configuracao global de consultas da fisica/visao.
                if (hit.collider.isTrigger) continue;
                if (hit.collider.GetComponentInParent<NetworkObject>() == NetworkObject) continue; // ignora a si mesmo
                var h = hit.collider.GetComponentInParent<Health>();
                if (h != null)
                {
                    if (h.IsDead) continue;
                    h.ServerTakeDamage(GameConfig.ShotDamage);
                    end = hit.point;
                    break;
                }
                end = hit.point; // parede
                break;
            }

            Noise.Emit(origin, GameConfig.ShotNoiseRadius);
            ShotFxClientRpc(origin, end);
        }

        [ClientRpc]
        void ShotFxClientRpc(Vector2 from, Vector2 to)
        {
            tracer.SetPosition(0, from);
            tracer.SetPosition(1, to);
            tracer.enabled = true;
            tracerUntil = Time.time + 0.06f;
        }
    }
}
