using UnityEngine;
using UnityEngine.InputSystem;

namespace CyberExtraction.UI
{
    public class ExpeditionFishingUI : MonoBehaviour
    {
        // Compatibility for older scenes: fishing now belongs exclusively to the cabin.
        void Awake() { enabled=false; }
        readonly FishingPresentation fishing=new FishingPresentation();
        void Update()
        {
            var inv = InventoryWindow.Local();
            if (inv == null || inv.FishingPhase.Value == 0) { fishing.Reset(); return; }
            fishing.ReadInput(!GameOptionsMenu.IsOpen && !TestLabPanel.IsOpen);
            if (fishing.NeedsSend(Time.unscaledTime))
            {
                inv.SetReel(fishing.Held);
                fishing.Sent(Time.unscaledTime);
            }
            fishing.Tick(inv.FishingPhase.Value,inv.FishingTension.Value,inv.FishingFish.Value,
                inv.FishingZone.Value,inv.FishingProgress.Value,inv.FishingVelocity.Value,Time.unscaledDeltaTime);
        }
        void OnGUI()
        {
            if(GameOptionsMenu.IsOpen || TestLabPanel.IsOpen) return;
            using var themeFrame=UiLayout.Begin();
            var inv = InventoryWindow.Local(); if (inv == null || inv.FishingPhase.Value == 0) return;
            if(MinigameView.FishingPanel(fishing,inv.FishingNibble.Value)) inv.CancelFishing();
        }
        void OnDisable()
        {
            var inv=InventoryWindow.Local();
            if(inv!=null && inv.FishingPhase.Value!=0) inv.SetReel(false);
            fishing.Reset();
        }
    }
}
