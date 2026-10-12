using UnityEngine;
using CyberExtraction.Camp;
using CyberExtraction.Core;
using CyberExtraction.Players;
namespace CyberExtraction.UI
{
    public class CampTestPanel : MonoBehaviour
    {
        static CampTestPanel instance;
        bool open;
        public static bool Available=>instance!=null && CampController.Active && NetworkBootstrap.Instance!=null && NetworkBootstrap.Instance.InTestCamp;
        public static bool IsOpen=>Available && instance.open;
        public static Rect Bounds=>new Rect((UiLayout.Width-420)/2,130,420,300);
        public static bool Contains(Vector2 screen)=>IsOpen && Bounds.Contains(UiLayout.Point(screen));
        void Awake() { instance=this; }
        void OnDestroy() { if(instance==this) instance=null; }
        public static void Show() { if(Available) instance.open=true; }
        void Update() { if(!Available) open=false; else if(IsOpen && GameInput.BackPressed && !GameOptionsMenu.ConsumedBack) open=false; }
        void OnGUI()
        {
            var camp=CampController.Instance; var bootstrap=NetworkBootstrap.Instance;
            if(!IsOpen || GameOptionsMenu.IsOpen || Application.isBatchMode || camp==null || bootstrap==null || camp.HasActivity || camp.ExpeditionPanelOpen || InventoryWindow.IsOpen) return;
            using var frame=UiLayout.Begin(); var box=Bounds;
            var style=new GUIStyle(GUI.skin.button){fontSize=16,wordWrap=true};
            GUI.Box(box,"");
            GUI.Label(new Rect(box.x+8,box.y+6,box.width-16,32),"ACAMPAMENTO DE TESTE",new GUIStyle(GUI.skin.label){fontSize=17,alignment=TextAnchor.MiddleCenter});
            bool before=GUI.enabled; GUI.enabled=before && !bootstrap.ConnectionBusy && !CampSession.Pending;
            if(GUI.Button(new Rect(box.x+10,box.y+60,box.width-20,42),"Repor recursos e peixes",style)) bootstrap.SendCamp(new CampCommand{Kind=CampCommandKind.TestSupply});
            if(GUI.Button(new Rect(box.x+10,box.y+112,box.width-20,42),"Reiniciar teste (pesca bloqueada)",style)) bootstrap.SendCamp(new CampCommand{Kind=CampCommandKind.TestReset});
            GUI.enabled=before;
            if(ServerDebugPanel.Available) ServerDebugPanel.DrawControl(new Rect(box.x+10,box.y+188,box.width-20,38));
            if(GUI.Button(new Rect(box.x+10,box.yMax-52,box.width-20,40),"Fechar")) open=false;
        }
    }
}
