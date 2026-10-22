using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace CyberExtraction.UI
{
    // Gate navigation immediately before EventSystem dispatch, independent of Update order.
    public sealed class ContextUiInputModule : InputSystemUIInputModule
    {
        public static void Install(EventSystem events)
        {
            if(events.GetComponent<ContextUiInputModule>()!=null) return;
            var previous=events.GetComponent<InputSystemUIInputModule>();
            var module=events.gameObject.AddComponent<ContextUiInputModule>();
            module.enabled=false;
            if(previous!=null)
            {
                module.actionsAsset=previous.actionsAsset;
                module.point=previous.point; module.leftClick=previous.leftClick;
                module.rightClick=previous.rightClick; module.middleClick=previous.middleClick;
                module.scrollWheel=previous.scrollWheel; module.move=previous.move;
                module.submit=previous.submit; module.cancel=previous.cancel;
                module.trackedDevicePosition=previous.trackedDevicePosition;
                module.trackedDeviceOrientation=previous.trackedDeviceOrientation;
                module.xrTrackingOrigin=previous.xrTrackingOrigin;
                module.pointerBehavior=previous.pointerBehavior;
                module.cursorLockBehavior=previous.cursorLockBehavior;
                module.deselectOnBackgroundClick=previous.deselectOnBackgroundClick;
                module.moveRepeatDelay=previous.moveRepeatDelay; module.moveRepeatRate=previous.moveRepeatRate;
            }
            else module.AssignDefaultActions();
            foreach(var old in events.GetComponents<BaseInputModule>()) if(old!=module) old.enabled=false;
            module.enabled=true;
        }
        public override void Process()
        {
            if(!UguiGameHud.Active) { base.Process(); return; }
            bool allow=UiScreenCoordinator.UguiNavigation;
            var root=allow ? UguiGameHud.NavigationRoot(UiScreenCoordinator.InputScreen) : null;
            var selected=eventSystem.currentSelectedGameObject;
            if(selected!=null && (!allow || root==null || !selected.transform.IsChildOf(root)))
                eventSystem.SetSelectedGameObject(null);
            if(allow && root!=null && eventSystem.currentSelectedGameObject==null)
                foreach(var selectable in root.GetComponentsInChildren<UnityEngine.UI.Selectable>())
                    if(selectable.IsActive() && selectable.IsInteractable() && selectable.navigation.mode!=UnityEngine.UI.Navigation.Mode.None)
                    { eventSystem.SetSelectedGameObject(selectable.gameObject); break; }
            bool previous=eventSystem.sendNavigationEvents;
            eventSystem.sendNavigationEvents=previous && allow;
            try { base.Process(); }
            finally { eventSystem.sendNavigationEvents=previous; }
        }
    }
}
