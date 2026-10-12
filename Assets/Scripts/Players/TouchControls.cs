using UnityEngine;
using UnityEngine.InputSystem;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using CyberExtraction.Core;
using CyberExtraction.Match;
using CyberExtraction.Camp;
using CyberExtraction.UI;

namespace CyberExtraction.Players
{
    // Esquerda: movimento. Direita: mira silenciosa. Botao separado: disparo.
    // Le a entrada antes do PlayerController para aplicar a soltura no mesmo frame.
    [DefaultExecutionOrder(-100)]
    public class TouchControls : MonoBehaviour
    {
        public static TouchControls Instance { get; private set; }

        public bool IsTouch => GameInput.TouchActive;
        public Vector2 Move { get; private set; }
        public Vector2 Aim { get; private set; }
        public bool AimHeld { get; private set; }
        public bool AimActive => aimId >= 0;
        public bool FireHeld { get; private set; }

        int moveId = -1, aimId = -1, fireId = -1;
        Vector2 moveOrigin, moveCur, aimOrigin, aimCur, fireCur;

        float Radius => Screen.height * 0.10f;

        // Coordenadas da tela (origem embaixo), usadas tanto no hit-test como no desenho.
        Rect FireRect
        {
            get
            {
                float size = Mathf.Max(44,Screen.height * 0.15f);
                float margin = Screen.height * 0.04f;
                var safe=UiLayout.SafeScreen; return new Rect(safe.xMax-size-margin,safe.y+margin,size,size);
            }
        }

        bool CanAct
        {
            get
            {
                if (CampController.Active) return CampController.Instance.CanMove;
                if (UiLayout.BlocksGameplay) return false;
                var mm = MatchManager.Instance;
                if (mm == null || mm.CurrentState != MatchState.Running) return false;
                var nm = Unity.Netcode.NetworkManager.Singleton;
                var player = nm != null && nm.SpawnManager != null
                    ? nm.SpawnManager.GetLocalPlayerObject() : null;
                var inv = player != null ? player.GetComponent<PlayerInventory>() : null;
                return inv != null && inv.IsAlive && inv.FishingPhase.Value == 0;
            }
        }

        private void Awake() { Instance = this; }
        private void OnDisable() { ResetInput(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private void OnApplicationFocus(bool hasFocus) { GameInput.SetFocus(hasFocus); if (!hasFocus) ResetInput(); }
        private void OnApplicationPause(bool paused) { GameInput.SetFocus(!paused); if (paused) ResetInput(); }

        void ResetInput()
        {
            moveId = aimId = fireId = -1;
            Move = Aim = Vector2.zero;
            AimHeld = FireHeld = false;
        }

        private void Update()
        {
            var ts = Touchscreen.current;
            if (ts == null || !CanAct) { ResetInput(); return; }

            foreach (var t in ts.touches)
            {
                int id = t.touchId.ReadValue();
                Vector2 pos = t.position.ReadValue();
                var phase = t.phase.ReadValue();

                if (phase == TouchPhase.Began)
                {
                    Vector2 guiPosition = UiLayout.Point(pos);
                    if (UiLayout.ContainsControl(pos) ||
                        (CampController.Active && CampController.InteractionBounds.Contains(guiPosition))) continue;
                    // O botao tem prioridade sobre a metade direita. O dedo conserva
                    // sua funcao ate ser solto, mesmo se atravessar outra area.
                    if (FireRect.Contains(pos))
                    {
                        if (fireId < 0) { fireId = id; fireCur = pos; }
                    }
                    else if (pos.x < Screen.width * 0.5f)
                    {
                        if (moveId < 0) { moveId = id; moveOrigin = moveCur = pos; }
                    }
                    else if (aimId < 0) { aimId = id; aimOrigin = aimCur = pos; }
                }
                else if (phase == TouchPhase.Moved || phase == TouchPhase.Stationary)
                {
                    if (id == moveId) moveCur = pos;
                    if (id == aimId) aimCur = pos;
                    if (id == fireId) fireCur = pos;
                }
                else if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
                {
                    if (id == moveId) moveId = -1;
                    if (id == aimId) aimId = -1;
                    if (id == fireId) fireId = -1;
                }
            }

            Move = moveId >= 0 ? Vector2.ClampMagnitude((moveCur - moveOrigin) / Radius, 1f) : Vector2.zero;
            Aim = aimId >= 0 ? Vector2.ClampMagnitude((aimCur - aimOrigin) / Radius, 1f) : Vector2.zero;
            AimHeld = aimId >= 0 && Aim.magnitude > 0.25f;
            FireHeld = fireId >= 0 && FireRect.Contains(fireCur);
        }

        private void OnGUI()
        {
            if (!IsTouch || !CanAct) return;
            using var themeFrame=UiLayout.Begin();
            // HUD e menu usam escala propria; os controles usam pixels da tela.
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            if (moveId >= 0) DrawStick(moveOrigin, moveCur, HudTheme.Good);
            if (aimId >= 0) DrawStick(aimOrigin, aimCur, HudTheme.Accent);

            if (CampController.Active) { GUI.matrix = oldMatrix; return; }
            Rect rect = FireRect;
            rect.y = Screen.height - rect.y - rect.height;
            var oldColor = GUI.color;
            GUI.color = FireHeld ? HudTheme.Accent : Color.white;
            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(16, Mathf.RoundToInt(Screen.height * 0.03f))
            };
            var texture=SpriteFactory.CircleTexture;
            GUI.color=HudTheme.Panel; GUI.DrawTexture(rect,texture);
            GUI.color=FireHeld ? HudTheme.Accent : HudTheme.Text;
            GUI.Label(rect,"ATIRAR",new GUIStyle(GUI.skin.label) { fontSize=style.fontSize,alignment=TextAnchor.MiddleCenter });
            GUI.color = oldColor;
            GUI.matrix = oldMatrix;
        }

        void DrawStick(Vector2 origin, Vector2 cur, Color c)
        {
            var tex = SpriteFactory.CircleTexture;
            float r = Radius;
            Vector2 knob = origin + Vector2.ClampMagnitude(cur - origin, r);
            var old = GUI.color;
            GUI.color = new Color(c.r, c.g, c.b, c.a * 0.5f);
            GUI.DrawTexture(new Rect(origin.x - r, Screen.height - origin.y - r, r * 2, r * 2), tex);
            GUI.color = c;
            float k = r * 0.45f;
            GUI.DrawTexture(new Rect(knob.x - k, Screen.height - knob.y - k, k * 2, k * 2), tex);
            GUI.color = old;
        }
    }
}
