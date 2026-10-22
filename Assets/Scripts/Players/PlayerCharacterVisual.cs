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
        // Preserve the authored stride length when movement balance changes (originally 4 units/s).
        const float CycleDistance = 4f * (32f / 24f);
        const float MinimumSpeed = .05f;
        const float SnapshotGrace = .08f;
        SpriteRenderer spriteRenderer;
        PlayerInventory inventory;
        PlayerShooter shooter;
        Rigidbody2D body;
        Vector2 previousPosition;
        Sprite[] currentFrames;
        int currentPose = -1;
        float phase = IdleFrame / 32f;
        float lastDisplacementTime;
        bool initialized;
        SpriteRenderer switchUpperBody;
        Sprite[] switchFrames, legFrames;
        float switchStartedAt=-1;
        int switchFirstFrame;
        bool wasInMatch;
        double observedReloadEnd;
        int reloadPose;
        Sprite[] reloadFrames, partialReloadFrames;
        uint observedShot;
        float crossbowShotStartedAt=-1;
        Sprite[] crossbowShotFrames;
        bool observedCrossbowEmpty;
        const float SwitchFps=24f;
        public bool MeleePresentationActive { get; private set; }

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            inventory = GetComponentInParent<PlayerInventory>();
            shooter = GetComponentInParent<PlayerShooter>();
            body = GetComponentInParent<Rigidbody2D>();
            var upper=new GameObject("WeaponSwitchUpperBody");
            upper.transform.SetParent(transform,false);
            upper.layer=gameObject.layer;
            switchUpperBody=upper.AddComponent<SpriteRenderer>();
            upper.SetActive(false);
            GetComponent<CyberExtraction.Combat.VisibilityTarget>()?.Refresh();
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
            legFrames=PlayerCharacterArt.GetFrames(5);
            observedShot=shooter!=null ? shooter.ShotSequence.Value : 0;
            ResetMovement();
        }

        void LateUpdate()
        {
            MeleePresentationActive=false;
            if (!initialized) return;
            int pose = EquippedPose();
            bool alive = inventory == null || inventory.IsAlive;
            bool inMatch = inventory == null ||
                (inventory.IsSpawned && MatchManager.Instance != null &&
                 MatchManager.Instance.CurrentState == MatchState.Running);
            if (pose != currentPose)
            {
                int previousPose=currentPose;
                CancelSwitch();
                crossbowShotStartedAt=-1;
                currentPose = pose;
                observedCrossbowEmpty=false;
                currentFrames = PlayerCharacterArt.GetFrames(pose);
                if(previousPose>=0 && alive && inMatch && wasInMatch)
                {
                    switchFrames=PlayerActionArt.Transition(previousPose,pose,out switchFirstFrame);
                    if(switchFrames!=null && legFrames!=null)
                    {
                        switchStartedAt=inventory!=null ? (float)(inventory.WeaponReadyAt.Value-(switchFrames.Length-switchFirstFrame)/SwitchFps) : Time.time;
                        switchUpperBody.gameObject.SetActive(true);
                        SpriteFactory.Setup(switchUpperBody,switchFrames[switchFirstFrame],spriteRenderer.color,spriteRenderer.sortingOrder+1);
                    }
                }
            }
            wasInMatch=inMatch;
            if(shooter!=null && shooter.ShotSequence.Value!=observedShot)
            {
                observedShot=shooter.ShotSequence.Value;
                // Firing remains authoritative and can interrupt a cosmetic draw.
                CancelSwitch();
                if(pose==6 && alive && inMatch)
                {
                    observedCrossbowEmpty=true;
                    crossbowShotFrames=PlayerActionArt.Get("ShotCrossbow",8);
                    crossbowShotStartedAt=Time.time;
                }
            }
            Vector2 position = body != null ? (Vector2)body.transform.position : (Vector2)transform.position;
            float distance = Vector2.Distance(position, previousPosition);
            previousPosition = position;
            float delta = Time.deltaTime;
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
            if(!alive || !inMatch || jump) CancelSwitch();
            if(alive && inMatch && !jump && legFrames!=null &&
                PlayerMeleeActionArt.TrySample(shooter,out var meleeSprite,out _))
            {
                CancelSwitch();
                crossbowShotStartedAt=-1;
                observedReloadEnd=0;
                ShowUpper(meleeSprite,frame,shooter.Weapon.Item==ItemId.BattleAxe || shooter.Weapon.Item==ItemId.Spear);
                MeleePresentationActive=true;
                return;
            }
            if(alive && inMatch && !jump && UpdateReload(pose,frame))
            {
                crossbowShotStartedAt=-1;
                return;
            }
            if(pose==6 && observedReloadEnd>0 && shooter!=null && shooter.IsSpawned &&
                shooter.NetworkManager.ServerTime.Time>=observedReloadEnd)
                observedCrossbowEmpty=false;
            observedReloadEnd=0;
            if(pose==6 && alive && inMatch && !jump && crossbowShotStartedAt>=0 && crossbowShotFrames!=null)
            {
                int shotFrame=Mathf.FloorToInt((Time.time-crossbowShotStartedAt)*SwitchFps);
                if(shotFrame<crossbowShotFrames.Length)
                {
                    ShowUpper(crossbowShotFrames[Mathf.Max(0,shotFrame)],frame);
                    return;
                }
                crossbowShotStartedAt=-1;
            }
            if(switchStartedAt>=0)
            {
                float clock=inventory!=null && inventory.IsSpawned ? (float)inventory.NetworkManager.ServerTime.Time : Time.time;
                int switchFrame=switchFirstFrame+Mathf.FloorToInt((clock-switchStartedAt)*SwitchFps);
                if(switchFrame<switchFrames.Length)
                {
                    ShowUpper(switchFrames[Mathf.Clamp(switchFrame,0,switchFrames.Length-1)],frame);
                    return;
                }
                CancelSwitch();
            }
            switchUpperBody.gameObject.SetActive(false);
            if(pose==6)
            {
                // Other clients cannot read owner-only equipment/ammunition lists.
                bool loaded=inventory.IsOwner || inventory.IsServer ? shooter!=null && shooter.Magazine>0 : !observedCrossbowEmpty;
                currentFrames=PlayerCharacterArt.GetFrames(loaded ? 6 : 7);
            }
            if (currentFrames != null && spriteRenderer.sprite != currentFrames[frame])
                spriteRenderer.sprite = currentFrames[frame];
            // VisibilityTarget owns enabled; do not override fog-of-war here.
        }

        bool UpdateReload(int pose,int legFrame)
        {
            if(shooter==null || !shooter.IsSpawned || !shooter.Reloading || legFrames==null) return false;
            var weapon=shooter.Weapon;
            if(weapon==null || (pose!=6 && (pose<1 || pose>3))) return false;
            CancelSwitch();
            if(observedReloadEnd!=shooter.ReloadEndsAt.Value || reloadPose!=pose)
            {
                observedReloadEnd=shooter.ReloadEndsAt.Value; reloadPose=pose;
                reloadFrames=PlayerActionArt.Reload(pose);
                partialReloadFrames=null;
                int rounds=shooter.ReloadRounds.Value;
                if(pose==3 && rounds==1) partialReloadFrames=PlayerActionArt.Get("ReloadShotgunSingle",42);
                else if(pose==2 && rounds>=1 && rounds<5) partialReloadFrames=PlayerActionArt.Get("ReloadRifleRounds"+rounds,23);
            }
            if(reloadFrames==null) return false;
            double remaining=shooter.ReloadEndsAt.Value-shooter.NetworkManager.ServerTime.Time;
            float progress=1-Mathf.Clamp01((float)(remaining/weapon.ReloadSeconds));
            int frame=Mathf.Clamp(Mathf.FloorToInt(progress*(reloadFrames.Length-1)),0,reloadFrames.Length-1);
            var sprite=reloadFrames[frame];
            int first=pose==3 ? 13 : 28;
            if(partialReloadFrames!=null && frame>=first && frame<first+partialReloadFrames.Length)
                sprite=partialReloadFrames[frame-first];
            ShowUpper(sprite,legFrame);
            return true;
        }

        void ShowUpper(Sprite sprite,int legFrame,bool weaponBehindHands=false)
        {
            spriteRenderer.sprite=legFrames[legFrame];
            switchUpperBody.gameObject.SetActive(true);
            switchUpperBody.sprite=sprite;
            switchUpperBody.sharedMaterial=spriteRenderer.sharedMaterial;
            switchUpperBody.sortingLayerID=spriteRenderer.sortingLayerID;
            switchUpperBody.sortingOrder=spriteRenderer.sortingOrder+(weaponBehindHands ? 3 : 1);
            switchUpperBody.color=spriteRenderer.color;
            switchUpperBody.enabled=spriteRenderer.enabled;
        }

        void ResetMovement()
        {
            CancelSwitch();
            wasInMatch=false;
            observedReloadEnd=0;
            crossbowShotStartedAt=-1;
            previousPosition = body != null ? (Vector2)body.transform.position : (Vector2)transform.position;
            phase = IdleFrame / 32f;
            lastDisplacementTime = float.NegativeInfinity;
            if (initialized && currentFrames != null) spriteRenderer.sprite = currentFrames[IdleFrame];
        }

        void OnDisable() { MeleePresentationActive=false; CancelSwitch(); }
        void CancelSwitch()
        {
            switchStartedAt=-1;
            if(switchUpperBody!=null) switchUpperBody.gameObject.SetActive(false);
        }

        int EquippedPose()
        {
            var weapon = inventory != null && inventory.IsSpawned && inventory.IsAlive
                ? inventory.ActiveWeapon : null;
            if (weapon == null) return 0;
            switch (weapon.Item)
            {
                case ItemId.ImprovisedHandCrossbow: return 6;
                default: return 0;
            }
        }
    }

    // Sprites are cached once and shared by all players, bots and the cabin avatar.
    static class PlayerCharacterArt
    {
        const int CellSize = 256, Columns = 8, FrameCount = 32;
        const float PixelsPerUnit = CellSize / 3.4f;
        static readonly string[] Names = { "Unarmed", "Pistol", "Mosin", "Shotgun", "SwitchShotgun", "Legs", "HandCrossbow", "HandCrossbowEmpty" };
        static readonly Sprite[][] Frames = new Sprite[8][];
        static readonly bool[] Loaded = new bool[8];

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
            return Frames[pose] ?? (pose == 0 || pose==4 || pose==5 ? null : GetFrames(0));
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
