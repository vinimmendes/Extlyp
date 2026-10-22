using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using CyberExtraction.Camp;

namespace CyberExtraction.UI
{
    public enum PlayerScreen { Gameplay,Organizer,Menu,Inventory,Expedition,Preparation,LegacyActivity,TestTools,Results,Base }
    public static class UiScreenCoordinator
    {
        static readonly List<RaycastResult> hits=new List<RaycastResult>();
        static PointerEventData pointer;
        static EventSystem events;
        static int inputFrame=-1;
        static PlayerScreen inputScreen;
        public static PlayerScreen Top=>HudLayoutEditor.IsOpen ? PlayerScreen.Organizer : LockpickWindow.IsOpen || CampController.Active && CampController.Instance.MinigameActive ? PlayerScreen.LegacyActivity :
            TestLabPanel.IsOpen || CampTestPanel.IsOpen ? PlayerScreen.TestTools : GameOptionsMenu.IsOpen ? PlayerScreen.Menu : InventoryWindow.IsOpen ? PlayerScreen.Inventory :
            ExpeditionMenu.IsOpen ? PlayerScreen.Expedition : CampController.Active && CampController.Instance.HasActivity ? PlayerScreen.Preparation : UguiGameHud.ResultsVisible ? PlayerScreen.Results : UguiBaseHub.Active ? PlayerScreen.Base : PlayerScreen.Gameplay;
        // Freeze input ownership before a handler can open/close another screen this frame.
        public static PlayerScreen InputScreen
        { get { if(inputFrame!=Time.frameCount) { inputFrame=Time.frameCount; inputScreen=Top; } return inputScreen; } }
        public static bool UguiNavigation => InputScreen==Top &&
            (inputScreen==PlayerScreen.Base || inputScreen==PlayerScreen.Preparation || inputScreen==PlayerScreen.Menu || inputScreen==PlayerScreen.Inventory || inputScreen==PlayerScreen.Expedition || inputScreen==PlayerScreen.Organizer || inputScreen==PlayerScreen.Results);
        public static bool PointerOverUi(Vector2 position)
        {
            var current=EventSystem.current; if(current==null) return false;
            if(events!=current) { events=current; pointer=new PointerEventData(current); }
            pointer.position=position; hits.Clear(); current.RaycastAll(pointer,hits); return hits.Count>0;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { pointer=null; events=null; hits.Clear(); inputFrame=-1; }
    }
}
