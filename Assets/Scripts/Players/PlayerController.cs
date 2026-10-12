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

        Vector2 moveInput;
        float facing;
        bool fireHeld;
        float lastFire;
        double lastInputTime;
        Vector2 previousPosition;
        uint inputSequence,lastSequence;
        float nextInputSend;
        Vector2 sentMove;
        float sentFacing;

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
            if (!IsOwner) return;

            cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = GameConfig.ExpeditionCameraSize;
                cam.backgroundColor = Color.black;
                var follow = cam.GetComponent<CameraFollow>();
                if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
                follow.SetTarget(transform);
            }

            gameObject.AddComponent<PlayerVision>();
        }

        private void Update()
        {
            if (!IsOwner) return;

            Vector2 position = transform.position;
            float spd = Time.deltaTime > 0f ? Vector2.Distance(position, previousPosition) /
                (Time.deltaTime * GameConfig.PlayerSpeed) : 0f;
            previousPosition = position;
            Speed01 = Mathf.Lerp(Speed01, Mathf.Clamp01(spd), Time.deltaTime * 10f);

            moveInput = Vector2.zero;
            fireHeld = false;

            var mm = MatchManager.Instance;
            bool canAct = mm != null && mm.CurrentState == MatchState.Running && inv.IsAlive && !CyberExtraction.UI.UiLayout.BlocksGameplay && inv.FishingPhase.Value == 0;
            if (!canAct) return;

            ReadInput();
            if (GameInput.ReloadPressed) shooter.RequestReload();
            if (GameInput.WeaponPressed) inv.RequestNextWeapon();

            if (fireHeld && Time.time - lastFire >= shooter.FireCooldown)
            {
                lastFire = Time.time;
                shooter.RequestFire(facing);
            }
        }

        private void FixedUpdate()
        {
            if(IsOwner && (Time.unscaledTime>=nextInputSend || (moveInput-sentMove).sqrMagnitude>.001f || Mathf.Abs(Mathf.DeltaAngle(facing,sentFacing))>1))
            {
                SubmitInputServerRpc(moveInput,facing,++inputSequence); sentMove=moveInput; sentFacing=facing; nextInputSend=Time.unscaledTime+1f/30;
            }
            if (!IsServer) return;
            var mm = MatchManager.Instance;
            bool canAct = mm != null && mm.CurrentState == MatchState.Running && inv.IsAlive && !inv.Lockpicking.Value && inv.FishingPhase.Value==0;
            rb.linearVelocity = canAct && Time.timeAsDouble - lastInputTime < 0.5
                ? moveInput * GameConfig.PlayerSpeed * inv.ServerMovementMultiplier : Vector2.zero;
            if (canAct) rb.rotation = facing;
        }

        [ServerRpc(Delivery=RpcDelivery.Unreliable)]
        void SubmitInputServerRpc(Vector2 movement, float angle, uint sequence)
        {
            if (float.IsNaN(movement.x) || float.IsInfinity(movement.x) ||
                float.IsNaN(movement.y) || float.IsInfinity(movement.y) ||
                float.IsNaN(angle) || float.IsInfinity(angle)) return;
            if(unchecked((int)(sequence-lastSequence))<=0) return;
            lastSequence=sequence;
            moveInput = Vector2.ClampMagnitude(movement, 1f);
            facing = Mathf.Repeat(angle, 360f);
            lastInputTime = Time.timeAsDouble;
        }

        void ReadInput()
        {
            var tc = TouchControls.Instance;
            if (tc != null && tc.IsTouch)
            {
                moveInput = tc.Move;
                if (tc.AimHeld) facing = Angle(tc.Aim);
                else if (!tc.AimActive && !tc.FireHeld && moveInput.sqrMagnitude > 0.04f)
                    facing = Angle(moveInput);
                fireHeld = tc.FireHeld;
            }
            else
            {
                moveInput=GameInput.Move;
                var stick=GameInput.GamepadAim;
                if(stick.sqrMagnitude>.0625f) facing=Angle(stick);
                fireHeld=GameInput.Fire;
                var ms = Mouse.current;
                if (ms != null && cam != null)
                {
                    Vector2 w = cam.ScreenToWorldPoint(ms.position.ReadValue());
                    Vector2 d = w - (Vector2)transform.position;
                    if (d.sqrMagnitude > 0.01f) facing = Angle(d);
                    fireHeld = GameInput.Fire && !CyberExtraction.UI.UiLayout.ContainsControl(ms.position.ReadValue());
                }
            }
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        }

        static float Angle(Vector2 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
    }
}
