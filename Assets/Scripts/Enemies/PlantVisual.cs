using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Bots;
using CyberExtraction.Core;
using CyberExtraction.Players;
using CyberExtraction.Match;

namespace CyberExtraction.Enemies
{
    // Toda animacao e cosmetica; cura e amadurecimento sao controlados por PlantCreature.
    public class PlantVisual : MonoBehaviour
    {
        static readonly Sprite[] sprites = new Sprite[4];
        static bool missingReported;
        PlantCreature creature;
        SpriteRenderer body, glow;
        readonly SpriteRenderer[] sparks = new SpriteRenderer[PlantRules.HealParticleCount];
        readonly LineRenderer[] cracks = new LineRenderer[3];
        PlantKind displayed = (PlantKind)255;
        PlantVisual healTarget;
        double healStartedAt, healUntil, flashStartedAt, flashUntil;
        float burstUntil, pulsePhase;
        double ServerNow => NetworkManager.Singleton != null ? NetworkManager.Singleton.ServerTime.Time : 0;
        public static Sprite SpriteFor(PlantKind kind)
        {
            int index = (int)kind;
            if (index < 0 || index >= sprites.Length) return null;
            if (sprites[index] != null) return sprites[index];
            var texture = Resources.Load<Texture2D>("Plants/PlantMonsterSheet");
            if (texture == null || texture.width != 1306 || texture.height != 1205)
            {
                if (!missingReported) Debug.LogError("Sprite das plantas ausente ou redimensionado. Execute Plantas > Preparar sprites e prefab.");
                missingReported = true; return null;
            }
            // Retangulos do PNG aprovado. Nenhum arquivo de imagem e redesenhado.
            Rect rect = kind == PlantKind.Mother ? new Rect(14, 447, 846, 750) :
                kind == PlantKind.AttackBulb ? new Rect(860, 485, 437, 538) :
                kind == PlantKind.HealingBulb ? new Rect(850, 29, 420, 420) : new Rect(60, 21, 690, 415);
            sprites[index] = Sprite.Create(texture, rect, new Vector2(.5f, .5f), 200f, 0, SpriteMeshType.FullRect);
            sprites[index].name = "Plant_" + kind; return sprites[index];
        }
        void Awake()
        {
            creature = GetComponent<PlantCreature>();
            body = MakeSprite("PlantBody", null, Color.white, 5);
            glow = MakeSprite("PlantGlow", SpriteFactory.Circle, new Color(1, 1, 1, 0), 4);
            for (int i = 0; i < sparks.Length; i++)
            {
                sparks[i] = MakeSprite("PlantSpark" + i, SpriteFactory.Circle, Color.white, 7);
                sparks[i].transform.localScale = Vector3.one * .09f;
                sparks[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < cracks.Length; i++)
            {
                var go = new GameObject("MaturityCrack" + i); go.transform.SetParent(transform, false);
                var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = false;
                line.positionCount = 5; line.startWidth = line.endWidth = .035f;
                line.sortingOrder = 6;
                if (SpriteFactory.Unlit != null) line.sharedMaterial = SpriteFactory.Unlit;
                float angle = (30 + i * 120) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 side = new Vector2(-dir.y, dir.x);
                for (int j = 0; j < 5; j++)
                    line.SetPosition(j, dir * (.06f + .10f * j) + side * (j % 2 == 0 ? -.035f : .035f));
                line.enabled = false; cracks[i] = line;
            }
        }
        SpriteRenderer MakeSprite(string name, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>(); SpriteFactory.Setup(renderer, sprite, color, order);
            return renderer;
        }
        public void BeginHealing(PlantVisual target, double launchedAt)
        {
            healTarget = target; healStartedAt = launchedAt;
            healUntil = launchedAt + PlantRules.HealArrivalSeconds;
        }
        public void FlashHealing(double arrivedAt)
        {
            if (arrivedAt < flashStartedAt) return;
            flashStartedAt = arrivedAt; flashUntil = arrivedAt + .65f;
        }
        public void BeginMaturationBurst() { burstUntil = Time.time + .65f; }
        static bool CanSee(Vector2 point)
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsClient || nm.SpawnManager == null) return false;
            var player = nm.SpawnManager.GetLocalPlayerObject();
            var inv = player != null ? player.GetComponent<PlayerInventory>() : null;
            if (inv == null || !inv.IsAlive) return false;
            return BotController.InVision(player.transform.position, player.transform.eulerAngles.z, point,inv) &&
                Physics2D.Linecast(player.transform.position, point, GameConfig.VisionMask).collider == null;
        }
        void LateUpdate()
        {
            if (creature == null || !creature.IsSpawned) return;
            PlantKind kind = creature.CurrentKind;
            if (displayed != kind)
            {
                body.sprite = SpriteFor(kind); displayed = kind; pulsePhase = 0;
            }
            float scale = body.sprite != null ? PlantRules.SpriteWidth(kind) / body.sprite.bounds.size.x : 1;
            bool running = MatchManager.Instance != null && MatchManager.Instance.CurrentState == MatchState.Running;
            bool visible = running && creature.Alive && CanSee(transform.position);
            // Verificacao propria para linhas/particulas tambem respeitarem paredes e escuridao.
            body.enabled = visible;
            float flash = ServerNow >= flashStartedAt ? Mathf.Clamp01((float)((flashUntil - ServerNow) / .65f)) : 0;
            body.color = Color.Lerp(Color.white, new Color(1f, .78f, .32f), flash * .75f);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = Vector3.one * scale;
            glow.enabled = visible;
            glow.transform.localScale = Vector3.one * (PlantRules.SpriteWidth(kind) * 1.1f);
            glow.color = new Color(1f, .65f, .12f, flash * .23f);
            foreach (var line in cracks) line.enabled = false;
            if (kind == PlantKind.AttackBulb && running)
            {
                float progress = PlantRules.Maturity(creature.NetworkManager.ServerTime.Time, creature.BornAt.Value, creature.MatureAt.Value);
                pulsePhase += Time.deltaTime * PlantRules.PulseFrequency(progress) * Mathf.PI * 2;
                float pulse = (Mathf.Sin(pulsePhase) + 1) * .5f;
                body.transform.localScale *= 1 + pulse * Mathf.Lerp(.025f, .12f, progress);
                body.color = Color.Lerp(Color.white, new Color(1f, .50f, .90f), pulse * progress * .45f);
                glow.color = new Color(.80f, .20f, .75f, (.05f + progress * .18f) * pulse);
                if (progress > .75f)
                    body.transform.localPosition = new Vector3(Mathf.Sin(Time.time * 44f), Mathf.Cos(Time.time * 39f), 0) * (.035f * progress);
                foreach (var line in cracks)
                {
                    line.enabled = visible && progress > .45f;
                    line.startColor = line.endColor = new Color(1f, .25f, .85f, progress * (.4f + .6f * pulse));
                }
            }
            else if (kind == PlantKind.HealingBulb)
            {
                bool healing = ServerNow >= healStartedAt && ServerNow < healUntil && running;
                float pulse = .5f + .5f * Mathf.Sin(Time.time * (healing ? 15 : 3));
                glow.color = new Color(1f, .70f, .10f, healing ? .12f + .23f * pulse : .025f * pulse);
                if (healing) body.transform.localScale *= 1 + .07f * pulse;
            }
            UpdateSparks(visible);
        }
        void UpdateSparks(bool visible)
        {
            bool stream = displayed == PlantKind.HealingBulb && healTarget != null && ServerNow >= healStartedAt && ServerNow < healUntil;
            bool burst = displayed == PlantKind.Minion && Time.time < burstUntil;
            for (int i = 0; i < sparks.Length; i++)
            {
                var spark = sparks[i];
                bool active = visible && (stream || burst);
                if (!active) { spark.gameObject.SetActive(false); continue; }
                Vector2 pos;
                float alpha;
                if (stream)
                {
                    active = healTarget.creature != null && healTarget.creature.Alive && CanSee(healTarget.transform.position);
                    // Mesmo relogio e tempos usados pelo servidor para efetivar a cura.
                    float travel = PlantRules.ParticleTravel(ServerNow, healStartedAt, i);
                    pos = Vector2.Lerp(transform.position, healTarget.transform.position, travel);
                    pos += Vector2.up * Mathf.Sin(travel * Mathf.PI) * (.10f + i * .015f);
                    alpha = Mathf.Sin(travel * Mathf.PI);
                    spark.color = new Color(1f, .78f, .20f, alpha);
                    spark.sprite = SpriteFactory.Circle;
                    spark.transform.localScale = Vector3.one * .09f;
                }
                else
                {
                    float t = Mathf.Clamp01((Time.time - (burstUntil - .65f)) / .65f);
                    float angle = i * Mathf.PI * 2 / sparks.Length;
                    pos = (Vector2)transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (.25f + t * .85f);
                    spark.color = new Color(.45f, .65f, .25f, 1 - t);
                    spark.sprite = SpriteFactory.Square;
                    spark.transform.localScale = new Vector3(.08f, .17f, 1);
                    spark.transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg + t * 180);
                }
                active &= CanSee(pos);
                spark.gameObject.SetActive(active); spark.enabled = active;
                spark.transform.position = pos;
            }
        }
    }
}
