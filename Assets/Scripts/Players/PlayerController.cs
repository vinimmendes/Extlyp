using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using CyberExtraction.Core;
using CyberExtraction.Match;

namespace CyberExtraction.Players
{
    // Cliente envia entrada; servidor simula movimento e replica a posicao.
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : NetworkBehaviour
    {
        Rigidbody2D rb;
        PlayerShooter shooter;
        PlayerInventory inv;
        Camera cam;
        CameraFollow cameraFollow;
        ExpeditionMap cameraMap;
        bool cameraConfigured;
        enum AimDevice { Mouse,Gamepad,Touch }
        AimDevice aimDevice;
        Mouse aimMouse;
        Gamepad aimGamepad;
        Vector2 lastAimPointer,lastAimStick;
        bool aimSampled;

        Vector2 moveInput;
        float facing;
        bool fireHeld;
        float lastFire;
        float nextAutoFireCheck;
        double lastInputTime;
        Vector2 previousPosition;
        uint inputSequence,lastSequence;
        float nextInputSend;
        Vector2 sentMove;
        float sentFacing;
        Vector2 serverMove;
        bool sprintHeld, sentSprint, serverSprint, exhausted;
        double regenAt, sprintBlockedUntil;
        public NetworkVariable<float> Stamina = new NetworkVariable<float>(GameConfig.PlayerMaxStamina, NetworkVariableReadPermission.Owner);
        public NetworkVariable<float> MaxStamina = new NetworkVariable<float>(GameConfig.PlayerMaxStamina, NetworkVariableReadPermission.Owner);
        public NetworkVariable<bool> Sprinting = new NetworkVariable<bool>(false);
        public float ServerFacing => facing;
        public double ServerAimReceivedAt => lastInputTime;
        public bool CanSprint => inv != null && inv.IsAlive && !inv.Lockpicking.Value && inv.FishingPhase.Value==0 &&
            inv.DungeonAction.Value<0 && !inv.ChangingEquipment && !inv.SwitchingWeapon && inv.ExtractionProgress.Value<=0 &&
            shooter!=null && !shooter.MeleeAttacking && !shooter.Reloading && !shooter.CyclingBolt;

        // 0 = parado, 1 = velocidade maxima (controla a abertura do cone de mira)
        public float Speed01 { get; private set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            shooter = GetComponent<PlayerShooter>();
            inv = GetComponent<PlayerInventory>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            GetComponent<NetworkTransform>().AuthorityMode = NetworkTransform.AuthorityModes.Server;
        }

        public override void OnNetworkSpawn()
        {
            rb.bodyType = IsServer ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            facing = transform.eulerAngles.z;
            rb.position = transform.position;
            rb.rotation = facing;
            previousPosition = rb.position;
            aimDevice=AimDevice.Mouse; aimSampled=false; aimMouse=null; aimGamepad=null;
            if (!IsOwner) return;

            cameraConfigured=false;
            RefreshOwnerCamera();

            gameObject.AddComponent<PlayerVision>();
        }

        private void Update()
        {
            if (!IsOwner) return;
            RefreshOwnerCamera();

            Vector2 position = transform.position;
            float spd = Time.deltaTime > 0f ? Vector2.Distance(position, previousPosition) /
                (Time.deltaTime * GameConfig.PlayerSpeed) : 0f;
            previousPosition = position;
            Speed01 = Mathf.Lerp(Speed01, Mathf.Clamp01(spd), Time.deltaTime * 10f);

            moveInput = Vector2.zero;
            fireHeld = false;
            sprintHeld = false;

            var mm = MatchManager.Instance;
            bool canAct = mm != null && mm.CurrentState == MatchState.Running && inv.IsAlive && !CyberExtraction.UI.UiLayout.BlocksGameplay && inv.FishingPhase.Value == 0;
            if (!canAct) { aimSampled=false; return; }

            ReadInput();
            var touch = TouchControls.Instance;
            if (!fireHeld && touch != null && touch.IsTouch && TouchAimSettings.AutoFire &&
                Time.time - lastFire >= shooter.FireCooldown && Time.unscaledTime >= nextAutoFireCheck)
            {
                nextAutoFireCheck = Time.unscaledTime + .08f;
                fireHeld = shooter.HasAutomaticFireTarget(facing);
            }
            sprintHeld = (GameInput.Sprint || touch!=null && touch.SprintEnabled) && !fireHeld;
            if (GameInput.ReloadPressed) shooter.RequestReload();
            else if (touch != null && touch.IsTouch && TouchAimSettings.AutoReload) shooter.RequestAutomaticReload();
            if (GameInput.WeaponPressed) inv.RequestNextWeapon();

            if (fireHeld && Time.time - lastFire >= shooter.FireCooldown)
            {
                lastFire = Time.time;
                shooter.RequestFire(facing);
            }
        }

        void RefreshOwnerCamera()
        {
            if(!IsSpawned || !IsOwner || CyberExtraction.Camp.CampController.Active) return;
            var match=MatchManager.Instance;
            // Player and match objects can arrive in either order. Wait for the
            // replicated selection and its map setup instead of using a stale
            // MapBuilder.Current once at player spawn.
            if(match==null || !match.IsSpawned) return;
            var map=(ExpeditionMap)match.MapSelection.Value;
            if(MapBuilder.Current!=map) return;
            if(cam==null)
            {
                cam=Camera.main; cameraConfigured=false;
                if(cam==null) return;
            }
            if(cameraFollow==null || cameraFollow.gameObject!=cam.gameObject)
            {
                cameraFollow=cam.GetComponent<CameraFollow>();
                if(cameraFollow==null) cameraFollow=cam.gameObject.AddComponent<CameraFollow>();
                cameraConfigured=false;
            }
            float size=map==ExpeditionMap.Dungeon ? GameConfig.DungeonCameraSize : GameConfig.ExpeditionCameraSize;
            if(cameraConfigured && cameraMap==map && cam.orthographic &&
                Mathf.Approximately(cam.orthographicSize,size) && cameraFollow.Follows(transform)) return;
            cam.orthographic=true; cam.orthographicSize=size; cam.backgroundColor=Color.black;
            cameraFollow.SetTarget(transform);
            if(map==ExpeditionMap.Dungeon)
                cameraFollow.SetBounds(new Rect(-MapBuilder.Width/2,-MapBuilder.Height/2,MapBuilder.Width,MapBuilder.Height));
            cameraMap=map; cameraConfigured=true;
        }

        private void FixedUpdate()
        {
            if(IsOwner && (Time.unscaledTime>=nextInputSend || sprintHeld!=sentSprint || (moveInput-sentMove).sqrMagnitude>.001f || Mathf.Abs(Mathf.DeltaAngle(facing,sentFacing))>1))
            {
                SubmitInputServerRpc(moveInput,facing,sprintHeld,++inputSequence); sentMove=moveInput; sentFacing=facing; sentSprint=sprintHeld; nextInputSend=Time.unscaledTime+1f/30;
            }
            if (!IsServer) return;
            var mm = MatchManager.Instance;
            bool canAct = mm != null && mm.CurrentState == MatchState.Running && inv.IsAlive && !inv.Lockpicking.Value && inv.FishingPhase.Value==0;
            bool fresh = Time.timeAsDouble-lastInputTime<.5;
            if(!serverSprint || !fresh || !canAct) exhausted=false;
            bool run = canAct && fresh && serverSprint && serverMove.sqrMagnitude>.001f && CanSprint &&
                Time.timeAsDouble>=sprintBlockedUntil && !exhausted &&
                (Sprinting.Value ? Stamina.Value>0 : Stamina.Value>=GameConfig.SprintStartStamina);
            if(run)
            {
                Stamina.Value=Mathf.Max(0,Stamina.Value-GameConfig.SprintDrain*Time.fixedDeltaTime);
                regenAt=Time.timeAsDouble+GameConfig.StaminaRegenDelay;
                if(Stamina.Value<=0) { exhausted=true; run=false; }
            }
            else if(canAct && Time.timeAsDouble>=regenAt)
                Stamina.Value=Mathf.Min(MaxStamina.Value,Stamina.Value+GameConfig.StaminaRegen*Time.fixedDeltaTime*(inv.ServerWellFed ? 1.15f : 1f));
            Sprinting.Value=run;
            float attackSpeed=shooter!=null && shooter.MeleeAttacking && shooter.Weapon!=null &&
                PlayerMeleeActionArt.Supports(shooter.Weapon.Item) ? .6f : 1f;
            rb.linearVelocity = canAct && fresh
                ? serverMove * (run ? GameConfig.SprintSpeed : GameConfig.PlayerSpeed) * inv.ServerMovementMultiplier * attackSpeed : Vector2.zero;
            if (canAct) rb.rotation = shooter!=null && shooter.MeleeDirectionLocked ? shooter.MeleeAngle.Value : facing;
        }

        [ServerRpc(Delivery=RpcDelivery.Unreliable)]
        void SubmitInputServerRpc(Vector2 movement, float angle, bool sprint, uint sequence)
        {
            if (float.IsNaN(movement.x) || float.IsInfinity(movement.x) ||
                float.IsNaN(movement.y) || float.IsInfinity(movement.y) ||
                float.IsNaN(angle) || float.IsInfinity(angle)) return;
            if(unchecked((int)(sequence-lastSequence))<=0) return;
            lastSequence=sequence;
            serverMove = Vector2.ClampMagnitude(movement, 1f);
            serverSprint = sprint;
            facing = Mathf.Repeat(angle, 360f);
            lastInputTime = Time.timeAsDouble;
        }

        public void ServerResetStamina()
        {
            if(!IsServer) return;
            Stamina.Value=MaxStamina.Value=inv.StaminaCapacity;
            Sprinting.Value=false; serverSprint=exhausted=false; serverMove=Vector2.zero;
            regenAt=sprintBlockedUntil=0;
        }

        public void ServerSetMaxStamina(float maximum)
        {
            if(!IsServer) return;
            MaxStamina.Value=Mathf.Clamp(maximum,GameConfig.PlayerMaxStamina,CharacterAttributeRules.Stamina(CharacterAttributeRules.MaxEffective));
            Stamina.Value=Mathf.Min(Stamina.Value,MaxStamina.Value);
        }

        // Shared by sprint and future melee. Rejected spends never change the resource.
        public bool ServerSpendStamina(float amount, float recoverySeconds=0)
        {
            if(!IsServer || !inv.IsAlive || float.IsNaN(amount) || float.IsInfinity(amount) || amount<=0 ||
                float.IsNaN(recoverySeconds) || float.IsInfinity(recoverySeconds) || recoverySeconds<0 || Stamina.Value<amount) return false;
            Stamina.Value-=amount;
            regenAt=System.Math.Max(regenAt,Time.timeAsDouble+System.Math.Max(GameConfig.StaminaRegenDelay,recoverySeconds));
            ServerStopSprint(recoverySeconds);
            return true;
        }

        public void ServerStopSprint(float seconds)
        {
            if(!IsServer || float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds<0) return;
            Sprinting.Value=false;
            sprintBlockedUntil=System.Math.Max(sprintBlockedUntil,Time.timeAsDouble+seconds);
            rb.linearVelocity=Vector2.ClampMagnitude(rb.linearVelocity,GameConfig.PlayerSpeed*inv.ServerMovementMultiplier);
        }

        void ReadInput()
        {
            if(!GameInput.GameplayActive) { aimSampled=false; return; }
            var tc = TouchControls.Instance;
            if (tc != null && tc.IsTouch)
            {
                aimDevice=AimDevice.Touch; aimSampled=false;
                moveInput = tc.Move;
                if (tc.AimHeld) facing = Angle(tc.Aim);
                else if (TouchAimSettings.FollowMovement && !tc.AimActive && !tc.FireHeld && moveInput.sqrMagnitude > 0.04f)
                    facing = Angle(moveInput);
                fireHeld = tc.FireHeld;
            }
            else
            {
                moveInput=GameInput.Move;
                var stick=GameInput.GamepadAim;
                fireHeld=GameInput.Fire;
                var ms = Mouse.current;
                var pad=Gamepad.current;
                Vector2 pointer=ms!=null ? ms.position.ReadValue() : Vector2.zero;
                bool overHud=ms!=null && (CyberExtraction.UI.UiLayout.ContainsControl(pointer) || tc!=null && tc.ContainsHudPoint(pointer));
                bool stickHeld=stick.sqrMagnitude>.0625f;
                bool stickActivity=stickHeld && (!aimSampled || pad!=aimGamepad || lastAimStick.sqrMagnitude<=.0625f || (stick-lastAimStick).sqrMagnitude>.0004f);
                bool mouseMoved=ms!=null && aimSampled && ms==aimMouse && (pointer-lastAimPointer).sqrMagnitude>=1f;
                bool mousePressed=ms!=null && !overHud && ms.leftButton.wasPressedThisFrame;
                bool padPressed=pad!=null && pad.rightTrigger.wasPressedThisFrame && GameInput.FireFromGamepad;
                // A click wins simultaneous activity; otherwise meaningful stick changes
                // precede mouse movement. Merely retaining a value never takes ownership.
                if(mousePressed) aimDevice=AimDevice.Mouse;
                else if(stickActivity || padPressed) aimDevice=AimDevice.Gamepad;
                else if(mouseMoved && !overHud) aimDevice=AimDevice.Mouse;
                // Accumulate small movements instead of discarding sub-threshold deltas every frame.
                if(!aimSampled || ms!=aimMouse || mouseMoved || mousePressed || overHud) lastAimPointer=pointer;
                if(!aimSampled || pad!=aimGamepad || stickActivity || !stickHeld) lastAimStick=stick;
                aimMouse=ms; aimGamepad=pad; aimSampled=true;
                if(aimDevice==AimDevice.Gamepad && stickHeld) facing=Angle(stick);
                if (aimDevice==AimDevice.Mouse && ms != null && cam != null)
                {
                    Vector2 w = cam.ScreenToWorldPoint(pointer);
                    Vector2 d = w - (Vector2)transform.position;
                    if (d.sqrMagnitude > 0.01f && !overHud) facing = Angle(d);
                }
                fireHeld = GameInput.Fire && (GameInput.FireFromGamepad || !overHud || tc!=null && tc.PointerFireHeld);
            }
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        }

        static float Angle(Vector2 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
    }
}
