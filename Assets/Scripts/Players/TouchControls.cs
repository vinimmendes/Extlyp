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

        public bool IsTouch => GameInput.TouchActive || UguiGameHud.Active && (moveId>=0 || aimId>=0 || fireId>=0);
        public Vector2 Move { get; private set; }
        public Vector2 Aim { get; private set; }
        public bool AimHeld { get; private set; }
        public bool AimActive => aimId >= 0 || screenAimId >= 0;
        public bool FireHeld { get; private set; }
        public bool SprintEnabled => CanAct && sprintEnabled;
        public bool PointerFireHeld=>!IsTouch && CanAct && GameInput.Fire && Mouse.current!=null && FireRect.Contains(Mouse.current.position.ReadValue());
        public bool ContainsHudPoint(Vector2 point)
        { return !CampController.Active && CanAct && (HudLayout.ScreenBounds(HudControl.Move).Contains(point) || HudLayout.ScreenBounds(HudControl.Aim).Contains(point) || HudLayout.ScreenBounds(HudControl.Sprint).Contains(point) || FireRect.Contains(point)); }

        int moveId = -1, aimId = -1, fireId = -1, sprintId = -1;
        bool sprintEnabled;
        // IDs crus do Touchscreen sao separados dos IDs de ponteiro do uGUI.
        int screenAimId = -1;
        Vector2 moveOrigin, moveCur, aimOrigin, aimCur, fireCur;

        float Radius => HudLayout.Bounds(HudControl.Move).width*UiLayout.Scale*.5f;

        // Coordenadas da tela (origem embaixo), usadas tanto no hit-test como no desenho.
        Rect FireRect
        {
            get
            {
                return HudLayout.ScreenBounds(HudControl.Fire);
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
            moveId = aimId = fireId = sprintId = screenAimId = -1;
            Move = Aim = Vector2.zero;
            AimHeld = FireHeld = false;
            sprintEnabled = false;
        }

        private void Update()
        {
            if(UguiGameHud.Active)
            {
                if(!CanAct) { ResetInput(); return; }
                ReadScreenAim();
                ReadHeldPointers(); return;
            }
            var ts = Touchscreen.current;
            if (ts == null || !CanAct) { ResetInput(); return; }

            foreach (var t in ts.touches)
            {
                int id = t.touchId.ReadValue();
                Vector2 pos = t.position.ReadValue();
                var phase = t.phase.ReadValue();

                if (phase == TouchPhase.Began)
                {
                    if(TouchAimSettings.AimAnywhere && !UiLayout.SafeScreen.Contains(pos)) continue;
                    Vector2 guiPosition = UiLayout.Point(pos);
                    if (UiLayout.ContainsControl(pos) ||
                        (CampController.Active && CampController.InteractionBounds.Contains(guiPosition))) continue;
                    // O botao tem prioridade sobre a metade direita. O dedo conserva
                    // sua funcao ate ser solto, mesmo se atravessar outra area.
                    if (HudLayout.ScreenBounds(HudControl.Sprint).Contains(pos))
                    { if(sprintId<0) { sprintId=id; sprintEnabled=!sprintEnabled; } }
                    else if (FireRect.Contains(pos))
                    {
                        if (fireId < 0) { fireId = id; fireCur = pos; }
                    }
                    else if (HudLayout.ScreenBounds(HudControl.Move).Contains(pos) ||
                        ((CampController.Active || !TouchAimSettings.AimAnywhere) && !HudLayout.ScreenBounds(HudControl.Aim).Contains(pos) && pos.x < Screen.width * 0.5f))
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
                    if (id == sprintId) sprintId = -1;
                }
            }

            ReadHeldPointers();
        }
        void ReadScreenAim()
        {
            var touchscreen=Touchscreen.current;
            if(!TouchAimSettings.AimAnywhere || CampController.Active || touchscreen==null)
            { screenAimId=-1; return; }

            bool tracked=false;
            foreach(var touch in touchscreen.touches)
            {
                int id=touch.touchId.ReadValue();
                var phase=touch.phase.ReadValue();
                Vector2 point=touch.position.ReadValue();
                if(screenAimId>=0 && id==screenAimId)
                {
                    tracked=true;
                    if(phase==TouchPhase.Ended || phase==TouchPhase.Canceled || !touch.press.isPressed)
                        screenAimId=-1;
                    else aimCur=point;
                }
                else if(phase==TouchPhase.Began && screenAimId<0 && aimId<0 &&
                    UiLayout.SafeScreen.Contains(point) && !UiLayout.ContainsControl(point) && !ContainsHudPoint(point))
                {
                    screenAimId=id; aimOrigin=aimCur=point; tracked=true;
                }
            }
            // Cancelamento ou desconexao sem fase Ended nao deixa a mira presa.
            if(!tracked) screenAimId=-1;
        }
        public void BeginPointer(HudControl control,int id,Vector2 point)
        {
            if(!CanAct) return;
            id&=int.MaxValue;
            if(control==HudControl.Move && moveId<0) { moveId=id; moveOrigin=moveCur=point; }
            else if(control==HudControl.Aim && aimId<0 && screenAimId<0) { aimId=id; aimOrigin=aimCur=point; }
            else if(control==HudControl.Fire && fireId<0) { fireId=id; fireCur=point; }
            else if(control==HudControl.Sprint && sprintId<0) { sprintId=id; sprintEnabled=!sprintEnabled; }
            ReadHeldPointers();
        }
        public void MovePointer(int id,Vector2 point)
        { id&=int.MaxValue; if(id==moveId) moveCur=point; if(id==aimId) aimCur=point; if(id==fireId) fireCur=point; ReadHeldPointers(); }
        public void EndPointer(int id)
        { id&=int.MaxValue; if(id==moveId) moveId=-1; if(id==aimId) aimId=-1; if(id==fireId) fireId=-1; if(id==sprintId) sprintId=-1; ReadHeldPointers(); }
        void ReadHeldPointers()
        {
            Move = moveId >= 0 ? Vector2.ClampMagnitude((moveCur - moveOrigin) / Mathf.Max(1,Radius), 1f) : Vector2.zero;
            float aimRadius=HudLayout.Bounds(HudControl.Aim).width*UiLayout.Scale*.5f;
            Aim = AimActive ? Vector2.ClampMagnitude((aimCur - aimOrigin) / Mathf.Max(1,aimRadius), 1f) : Vector2.zero;
            AimHeld = AimActive && Aim.magnitude > 0.25f;
            FireHeld = fireId >= 0 && FireRect.Contains(fireCur);
        }

        private void OnGUI()
        {
            if(UguiGameHud.Active) return;
            if (!CanAct) return;
            using var themeFrame=UiLayout.Begin();
            if(CampController.Active)
            {
                if(!IsTouch) return;
                var oldMatrix=GUI.matrix; GUI.matrix=Matrix4x4.identity;
                if(moveId>=0) DrawStick(moveOrigin,moveCur,HudTheme.Good);
                GUI.matrix=oldMatrix; return;
            }
            var move=HudLayout.Bounds(HudControl.Move); HudArt.Circle(move);
            float outer=move.width*.5f;
            var movement=IsTouch ? Move : GameInput.Move;
            var c=move.center+new Vector2(movement.x,-movement.y)*outer*.58f;
            float inner=move.width*.42f;
            HudArt.Circle(new Rect(move.center.x-inner/2,move.center.y-inner/2,inner,inner));
            float knob=move.width*.28f;
            HudArt.Knob(new Rect(c.x-knob/2,c.y-knob/2,knob,knob),moveId>=0);
            var muted=HudTheme.Muted; muted.a=.55f;
            for(int i=0;i<4;i++)
            {
                float a=i*Mathf.PI*.5f;
                var point=move.center+new Vector2(Mathf.Sin(a),-Mathf.Cos(a))*outer*.87f;
                HudArt.Icon(new Rect(point.x-4,point.y-4,8,8),HudSymbol.Arrow,muted,i*90);
            }
            GUI.Label(new Rect(move.x,move.yMax+6,move.width,40),"Mover",HudTheme.Label(HudLayout.TextSize(HudControl.Move,14),TextAnchor.UpperCenter));
            var aim=HudLayout.Bounds(HudControl.Aim); HudArt.Circle(aim,held:AimHeld);
            HudArt.Icon(new Rect(aim.x+aim.width*.2f,aim.y+aim.height*.2f,aim.width*.6f,aim.height*.6f),HudSymbol.Aim,AimHeld ? HudTheme.Accent : HudTheme.Text);
            var fire=HudLayout.Bounds(HudControl.Fire); HudArt.Circle(fire,true,FireHeld || PointerFireHeld);
            var activeWeapon=InventoryWindow.Local()?.ActiveWeapon;
            var fireIcon=new Rect(fire.x+fire.width*.28f,fire.y+fire.height*.20f,fire.width*.44f,fire.height*.44f);
            if(activeWeapon!=null && activeWeapon.IsMelee) GUI.DrawTexture(fireIcon,Items.MeleeWeaponArt.Get(activeWeapon.Item).texture,ScaleMode.ScaleToFit,true);
            else HudArt.Icon(fireIcon,HudSymbol.Bullet,HudTheme.Text);
            GUI.Label(new Rect(fire.x+6,fire.y+fire.height*.67f,fire.width-12,fire.height*.22f),activeWeapon!=null && activeWeapon.IsMelee ? "ATACAR" : "ATIRAR",HudTheme.Label(Mathf.RoundToInt(fire.width*.145f),TextAnchor.MiddleCenter));
            var sprint=HudLayout.Bounds(HudControl.Sprint); HudArt.Circle(sprint,held:SprintEnabled);
            HudArt.Icon(new Rect(sprint.x+sprint.width*.25f,sprint.y+sprint.height*.25f,sprint.width*.5f,sprint.height*.5f),HudSymbol.Sprint,HudTheme.Text);
            GUI.Label(new Rect(sprint.x,sprint.yMax+4,sprint.width,24),"CORRER",HudTheme.Label(HudLayout.TextSize(HudControl.Sprint,13),TextAnchor.UpperCenter));
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
