using UnityEngine;
using UnityEngine.InputSystem;
namespace CyberExtraction.Players
{
    // A single action vocabulary; gameplay and camp share bindings, overlays own their context.
    public static class GameInput
    {
        static InputActionMap gameplay, ui;
        static InputAction move, fire, sprint, reload, weapon, interact, bag, hold, bandage, map, back, test, uiNavigate, uiSubmit,uiDiscard;
        static bool focused=true;
        static bool suppressFire;
        static void Context()
        {
            bool active=focused && UI.UiScreenCoordinator.InputScreen==UI.PlayerScreen.Gameplay && !UI.UiLayout.BlocksGameplay;
            if(active && !gameplay.enabled) { gameplay.Enable(); suppressFire=true; }
            else if(!active && gameplay.enabled) gameplay.Disable();
        }
        static void Ensure()
        {
            if(gameplay!=null) { Context(); return; }
            gameplay=new InputActionMap("Gameplay"); ui=new InputActionMap("UI");
            move=gameplay.AddAction("Move",InputActionType.Value); move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/upArrow").With("Down","<Keyboard>/downArrow").With("Left","<Keyboard>/leftArrow").With("Right","<Keyboard>/rightArrow"); move.AddBinding("<Gamepad>/leftStick");
            fire=gameplay.AddAction("Fire",InputActionType.Button,"<Mouse>/leftButton"); fire.AddBinding("<Gamepad>/rightTrigger");
            sprint=gameplay.AddAction("Sprint",InputActionType.Button,"<Keyboard>/leftShift"); sprint.AddBinding("<Keyboard>/rightShift"); sprint.AddBinding("<Gamepad>/leftStickPress");
            reload=gameplay.AddAction("Reload",InputActionType.Button,"<Keyboard>/r"); reload.AddBinding("<Gamepad>/buttonWest");
            reload.AddBinding("<Keyboard>/3");
            weapon=gameplay.AddAction("NextWeapon",InputActionType.Button,"<Keyboard>/q"); weapon.AddBinding("<Keyboard>/2"); weapon.AddBinding("<Gamepad>/rightShoulder");
            interact=gameplay.AddAction("Interact",InputActionType.Button,"<Keyboard>/e"); interact.AddBinding("<Gamepad>/buttonSouth");
            bandage=gameplay.AddAction("Bandage",InputActionType.Button,"<Keyboard>/1");
            bag=ui.AddAction("Inventory",InputActionType.Button,"<Keyboard>/i"); bag.AddBinding("<Gamepad>/start");
            hold=ui.AddAction("Hold",InputActionType.Button,"<Keyboard>/space"); hold.AddBinding("<Gamepad>/buttonSouth");
            map=ui.AddAction("Map",InputActionType.Button,"<Keyboard>/m");
            back=ui.AddAction("Back",InputActionType.Button,"<Keyboard>/escape"); back.AddBinding("<Gamepad>/buttonEast");
            test=ui.AddAction("TestTools",InputActionType.Button,"<Keyboard>/f2"); test.AddBinding("<Gamepad>/select");
            uiNavigate=ui.AddAction("Navigate",InputActionType.Value); uiNavigate.AddBinding("<Gamepad>/dpad"); uiNavigate.AddBinding("<Gamepad>/leftStick");
            uiNavigate.AddCompositeBinding("2DVector").With("Up","<Keyboard>/upArrow").With("Down","<Keyboard>/downArrow").With("Left","<Keyboard>/leftArrow").With("Right","<Keyboard>/rightArrow");
            uiSubmit=ui.AddAction("Submit",InputActionType.Button,"<Keyboard>/enter"); uiSubmit.AddBinding("<Gamepad>/buttonSouth");
            uiDiscard=ui.AddAction("DiscardItem",InputActionType.Button,"<Keyboard>/delete"); uiDiscard.AddBinding("<Gamepad>/buttonWest");
            gameplay.Enable(); ui.Enable(); Context();
        }
        public static void SetFocus(bool value) { focused=value; Ensure(); if(value) { gameplay.Enable(); ui.Enable(); } else { gameplay.Disable(); ui.Disable(); } }
        public static bool TouchActive
        {
            get
            {
                var touch=Touchscreen.current; if(touch==null) return false;
                if(Application.isMobilePlatform) return true;
                foreach(var t in touch.touches) if(t.press.isPressed) return true;
                return TouchControls.Instance!=null && TouchControls.Instance.AimActive;
            }
        }
        public static Vector2 Move { get { Ensure(); return focused && !UI.UiLayout.BlocksGameplay ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(),1) : Vector2.zero; } }
        public static bool Fire { get { Ensure(); if(!fire.IsPressed()) suppressFire=false; return focused && !suppressFire && !UI.UiLayout.BlocksGameplay && fire.IsPressed(); } }
        public static bool GameplayActive { get { Ensure(); return focused && gameplay.enabled; } }
        public static bool FireFromGamepad => Fire && fire.activeControl!=null && fire.activeControl.device is Gamepad;
        public static bool ReloadPressed { get { Ensure(); return reload.WasPressedThisFrame(); } }
        public static bool Sprint { get { Ensure(); return focused && !UI.UiLayout.BlocksGameplay && sprint.IsPressed(); } }
        public static bool WeaponPressed { get { Ensure(); return weapon.WasPressedThisFrame(); } }
        public static bool InteractPressed { get { Ensure(); return focused && gameplay.enabled && UI.UiScreenCoordinator.InputScreen==UI.PlayerScreen.Gameplay && UI.UiScreenCoordinator.Top==UI.PlayerScreen.Gameplay && interact.WasPressedThisFrame(); } }
        public static bool BagPressed { get { Ensure(); return bag.WasPressedThisFrame(); } }
        public static bool BandagePressed { get { Ensure(); return bandage.WasPressedThisFrame(); } }
        public static bool MapPressed { get { Ensure(); return map.WasPressedThisFrame(); } }
        public static bool BackPressed { get { Ensure(); return back.WasPressedThisFrame(); } }
        public static bool TestPressed { get { Ensure(); return test.WasPressedThisFrame(); } }
        public static Vector2 UiNavigate { get { Ensure(); return uiNavigate.ReadValue<Vector2>(); } }
        public static bool UiSubmitPressed { get { Ensure(); return focused && UI.UiScreenCoordinator.InputScreen!=UI.PlayerScreen.Gameplay && UI.UiScreenCoordinator.InputScreen==UI.UiScreenCoordinator.Top && uiSubmit.WasPressedThisFrame(); } }
        public static bool UiDiscardPressed { get { Ensure(); return focused && UI.UiScreenCoordinator.InputScreen==UI.PlayerScreen.Inventory && UI.UiScreenCoordinator.Top==UI.PlayerScreen.Inventory && uiDiscard.WasPressedThisFrame(); } }
        public static bool Hold { get { Ensure(); return focused && hold.IsPressed(); } }
        public static bool HoldPressed { get { Ensure(); return hold.WasPressedThisFrame(); } }
        public static Vector2 GamepadAim=>Gamepad.current!=null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { gameplay?.Dispose(); ui?.Dispose(); gameplay=ui=null; focused=true; }
    }
}
