using UnityEngine;
using UnityEngine.InputSystem;
using CyberExtraction.Camp;
using CyberExtraction.Items;
using CyberExtraction.Players;

namespace CyberExtraction.UI
{
    // The client animates time between snapshots; only the server decides quality.
    public sealed class CookingPresentation
    {
        CampSnapshot snapshot;
        float sampleAge;
        double startedAt;
        public bool Active { get; private set; }
        public bool Submitted { get; private set; }
        public float Marker { get; private set; }
        public float Elapsed { get; private set; }
        public double ServerTime { get; private set; }
        public bool Synchronized => sampleAge<=.35f;
        public string Notice { get; private set; }

        public void Reset()
        {
            snapshot=null; Active=false; Submitted=false;
            Marker=Elapsed=sampleAge=0; startedAt=ServerTime=0; Notice=null;
        }

        public void Tick(CampSnapshot view,bool pending,double serverTime,float dt)
        {
            if(view.Activity!=6) { Reset(); return; }
            dt=Mathf.Max(0,dt);
            if(!Active || startedAt!=view.CookStartedAt)
            {
                Active=true; Submitted=false; startedAt=view.CookStartedAt; Notice=null;
            }
            if(!ReferenceEquals(snapshot,view))
            {
                snapshot=view; sampleAge=0;
            }
            else sampleAge+=dt;
            if(Submitted && pending) return;
            if(Submitted) Notice=string.IsNullOrEmpty(view.Message) ? "Tentativa recusada. Tente novamente." : view.Message;
            Submitted=false;
            if(!Synchronized) return;
            ServerTime=serverTime;
            Elapsed=Mathf.Max(0,(float)(serverTime-startedAt));
            // Evaluate the same curve as the server, including both turnaround points.
            // NetworkTime supplies a synchronized clock; snapshots do not drive movement.
            Marker=CookingGame.Marker(Elapsed);
        }

        public bool RetirePressed(bool enabled)
        {
            if(!Active || Submitted || !Synchronized || Elapsed<CookingGame.MinimumInputSeconds || Elapsed>=CookingGame.Seconds || !enabled || !Application.isFocused) return false;
            if(GameInput.HoldPressed) return true;
            Rect bounds=MinigameView.CookingControlBounds;
            var touchscreen=Touchscreen.current;
            if(touchscreen!=null)
                foreach(var touch in touchscreen.touches)
                    if(touch.press.wasPressedThisFrame && bounds.Contains(UiLayout.Point(touch.position.ReadValue()))) return true;
            var mouse=Mouse.current;
            return mouse!=null && mouse.leftButton.wasPressedThisFrame && bounds.Contains(UiLayout.Point(mouse.position.ReadValue()));
        }

        public void Submit() { Submitted=true; Notice=null; }
    }
}
