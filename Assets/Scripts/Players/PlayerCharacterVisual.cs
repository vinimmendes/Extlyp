using UnityEngine;
using UnityEngine.Rendering;
using CyberExtraction.Core;
using CyberExtraction.Items;
using CyberExtraction.Match;

namespace CyberExtraction.Players
{
    // Rendered 3D frames follow real 2D displacement on every client.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlayerCharacterVisual : MonoBehaviour
    {
        const int IdleFrame = 8; // Blender frame 9, legs hidden under the torso.
        const float CycleDistance = GameConfig.PlayerSpeed * (32f / 24f);
        const float MinimumSpeed = .05f;
        const float SnapshotGrace = .08f;
        SpriteRenderer spriteRenderer;
        PlayerInventory inventory;
        Rigidbody2D body;
        Vector2 previousPosition;
        Sprite[] currentFrames;
        int currentPose = -1;
        float phase = IdleFrame / 32f;
        float lastDisplacementTime;
        bool initialized;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            inventory = GetComponentInParent<PlayerInventory>();
            body = GetComponentInParent<Rigidbody2D>();
        }

        void OnEnable() { ResetMovement(); }

        void Start()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                enabled = false;
                return;
            }
            currentPose = EquippedPose();
            currentFrames = PlayerCharacterArt.GetFrames(currentPose);
            if (currentFrames == null)
            {
                Debug.LogError("Sprites do personagem ausentes em Resources/PlayerCharacter.", this);
                enabled = false;
                return;
            }
            // All Awake calls have finished; ShapeVisual cannot overwrite the sprite.
            var placeholder = GetComponent<ShapeVisual>();
            if (placeholder != null) placeholder.enabled = false;
            Color tint = inventory != null && !inventory.IsAlive
                ? new Color(.25f, .25f, .25f) : Color.white;
            SpriteFactory.Setup(spriteRenderer, currentFrames[IdleFrame], tint, 5);
            initialized = true;
            ResetMovement();
        }

        void LateUpdate()
        {
            if (!initialized) return;
            int pose = EquippedPose();
            if (pose != currentPose)
            {
                currentPose = pose;
                currentFrames = PlayerCharacterArt.GetFrames(pose);
            }
            Vector2 position = body != null ? (Vector2)body.transform.position : (Vector2)transform.position;
            float distance = Vector2.Distance(position, previousPosition);
            previousPosition = position;
            float delta = Time.deltaTime;
            bool alive = inventory == null || inventory.IsAlive;
            bool inMatch = inventory == null ||
                (inventory.IsSpawned && MatchManager.Instance != null &&
                 MatchManager.Instance.CurrentState == MatchState.Running);
            // Spawn/teleport/correction jumps are not footsteps.
            bool jump = distance > Mathf.Max(1f, GameConfig.PlayerSpeed * delta * 4f);
            bool displaced = delta > 0f && distance > MinimumSpeed * delta && !jump;
            if (alive && inMatch && displaced)
            {
                lastDisplacementTime = Time.time;
                phase = Mathf.Repeat(phase + distance / CycleDistance, 1f);
            }
            else
            {
                // Remote positions can pause between snapshots. Hold briefly without
                // advancing frames. Physical bodies return to idle immediately when stopped.
                bool stoppedBody = body != null && body.bodyType == RigidbodyType2D.Dynamic &&
                    body.linearVelocity.sqrMagnitude < MinimumSpeed * MinimumSpeed;
                if (!alive || !inMatch || jump || stoppedBody ||
                    Time.time - lastDisplacementTime >= SnapshotGrace)
                    phase = IdleFrame / 32f;
            }
            int frame = Mathf.Clamp(Mathf.FloorToInt(phase * 32f), 0, 31);
            if (currentFrames != null && spriteRenderer.sprite != currentFrames[frame])
                spriteRenderer.sprite = currentFrames[frame];
            // VisibilityTarget owns enabled; do not override fog-of-war here.
        }

        void ResetMovement()
        {
            previousPosition = body != null ? (Vector2)body.transform.position : (Vector2)transform.position;
            phase = IdleFrame / 32f;
            lastDisplacementTime = float.NegativeInfinity;
            if (initialized && currentFrames != null) spriteRenderer.sprite = currentFrames[IdleFrame];
        }

        int EquippedPose()
        {
            var weapon = inventory != null && inventory.IsSpawned && inventory.IsAlive
                ? inventory.ActiveWeapon : null;
            if (weapon == null) return 0;
            switch (weapon.Item)
            {
                case ItemId.Pistol: return 1;
                case ItemId.Mosin: return 2;
                case ItemId.DoubleBarrelShotgun: return 3;
                default: return 0;
            }
        }
    }

    // Sprites are cached once and shared by all players, bots and the cabin avatar.
    static class PlayerCharacterArt
    {
        const int CellSize = 256, Columns = 8, FrameCount = 32;
        const float PixelsPerUnit = CellSize / 3.4f;
        static readonly string[] Names = { "Unarmed", "Pistol", "Mosin", "Shotgun" };
        static readonly Sprite[][] Frames = new Sprite[4][];
        static readonly bool[] Loaded = new bool[4];

        public static Sprite[] GetFrames(int pose)
        {
            if (!Loaded[pose])
            {
                Loaded[pose] = true;
                var texture = Resources.Load<Texture2D>("PlayerCharacter/" + Names[pose]);
                if (texture != null && texture.width == Columns * CellSize && texture.height == 4 * CellSize)
                {
                    var frames = new Sprite[FrameCount];
                    for (int i = 0; i < frames.Length; i++)
                    {
                        // PNG rows start above; Unity texture coordinates start below.
                        var rect = new Rect((i % Columns) * CellSize,
                            texture.height - (i / Columns + 1) * CellSize, CellSize, CellSize);
                        frames[i] = Sprite.Create(texture, rect, new Vector2(.5f, .5f),
                            PixelsPerUnit, 0, SpriteMeshType.FullRect);
                        frames[i].name = "Player_" + Names[pose] + "_" + (i + 1).ToString("00");
                    }
                    Frames[pose] = frames;
                }
            }
            return Frames[pose] ?? (pose == 0 ? null : GetFrames(0));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            for (int pose = 0; pose < Frames.Length; pose++)
            {
                if (Frames[pose] != null)
                    foreach (var sprite in Frames[pose]) if (sprite != null) Object.Destroy(sprite);
                Frames[pose] = null;
                Loaded[pose] = false;
            }
        }
    }
}
