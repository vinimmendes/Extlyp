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

            var nose = new GameObject("Nose");
            nose.transform.SetParent(transform, false);
            nose.transform.localPosition = new Vector3(0.5f, 0f, 0f);
            nose.transform.localScale = new Vector3(0.5f, 0.25f, 1f);
            SpriteFactory.Setup(nose.AddComponent<SpriteRenderer>(), SpriteFactory.Square, Color.white, 6);
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
                cam.orthographicSize = 7f;
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
            bool canAct = mm != null && mm.CurrentState == MatchState.Running && inv.IsAlive;
            if (!canAct) return;

            ReadInput();

            if (fireHeld && Time.time - lastFire >= GameConfig.FireCooldown)
            {
                lastFire = Time.time;
                shooter.RequestFire(facing);
            }
        }

        private void FixedUpdate()
        {
            if (IsOwner) SubmitInputServerRpc(moveInput, facing);
            if (!IsServer) return;
            var mm = MatchManager.Instance;
            bool canAct = mm != null && mm.CurrentState == MatchState.Running && inv.IsAlive;
            rb.linearVelocity = canAct && Time.timeAsDouble - lastInputTime < 0.5
                ? moveInput * GameConfig.PlayerSpeed : Vector2.zero;
            if (canAct) rb.rotation = facing;
        }

        [ServerRpc]
        void SubmitInputServerRpc(Vector2 movement, float angle)
        {
            if (float.IsNaN(movement.x) || float.IsInfinity(movement.x) ||
                float.IsNaN(movement.y) || float.IsInfinity(movement.y) ||
                float.IsNaN(angle) || float.IsInfinity(angle)) return;
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
                var kb = Keyboard.current;
                if (kb != null)
                {
                    float x = 0f, y = 0f;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
                    moveInput = new Vector2(x, y);
                }
                var ms = Mouse.current;
                if (ms != null && cam != null)
                {
                    Vector2 w = cam.ScreenToWorldPoint(ms.position.ReadValue());
                    Vector2 d = w - (Vector2)transform.position;
                    if (d.sqrMagnitude > 0.01f) facing = Angle(d);
                    fireHeld = ms.leftButton.isPressed && !CyberExtraction.UI.Quickbar.ContainsScreenPoint(ms.position.ReadValue());
                }
            }
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        }

        static float Angle(Vector2 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
    }
}
