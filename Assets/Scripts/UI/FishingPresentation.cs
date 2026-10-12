using UnityEngine;
using UnityEngine.InputSystem;
using CyberExtraction.Items;
using CyberExtraction.Players;

namespace CyberExtraction.UI
{
    // One instance per screen. All animation advances in Update, never in OnGUI.
    public sealed class FishingPresentation
    {
        public float Bar=.5f, Fish=.5f, Progress, Zone=.28f;
        public bool Held { get; private set; }
        public float PhaseAge { get; private set; }
        public int Phase { get; private set; }
        float speed, lastBar, lastFish, lastProgress, sampleAge, correctionDelay;
        float nextSend;
        bool sentHeld, sent, mouseCaptured;
        int touchId=-1;

        public void Reset()
        {
            Phase=0; PhaseAge=0; Held=false; sent=false; mouseCaptured=false; touchId=-1;
            speed=sampleAge=correctionDelay=0;
        }

        public void ReadInput(bool enabled)
        {
            bool value=false;
            if(enabled && Application.isFocused)
            {
                Rect bounds=MinigameView.FishingControlBounds;
                var touchscreen=Touchscreen.current;
                bool touchHeld=false;
                if(touchscreen!=null)
                {
                    foreach(var touch in touchscreen.touches)
                    {
                        int id=touch.touchId.ReadValue();
                        if(touchId<0 && touch.press.wasPressedThisFrame && bounds.Contains(UiLayout.Point(touch.position.ReadValue()))) touchId=id;
                        if(id==touchId && touch.press.isPressed) touchHeld=true;
                    }
                }
                if(!touchHeld) touchId=-1;
                var mouse=Mouse.current;
                if(mouse!=null)
                {
                    if(mouse.leftButton.wasPressedThisFrame && bounds.Contains(UiLayout.Point(mouse.position.ReadValue()))) mouseCaptured=true;
                    if(!mouse.leftButton.isPressed) mouseCaptured=false;
                }
                else mouseCaptured=false;
                value=touchHeld || mouseCaptured || GameInput.Hold;
            }
            else { mouseCaptured=false; touchId=-1; }
            if(value!=Held) correctionDelay=.12f;
            Held=value;
        }

        public bool NeedsSend(float now) => !sent || Held!=sentHeld || now>=nextSend;
        public void Sent(float now) { sent=true; sentHeld=Held; nextSend=now+.2f; }

        public void Tick(int phase,float bar,float fish,float zone,float progress,float velocity,float dt)
        {
            dt=Mathf.Clamp(dt,0,.05f);
            Zone=zone;
            if(phase!=Phase)
            {
                Phase=phase; PhaseAge=0; Bar=bar; Fish=fish; Progress=progress;
                speed=velocity; sampleAge=0; lastBar=bar; lastFish=fish; lastProgress=progress;
                if(phase==2) MinigameView.BiteCue();
            }
            PhaseAge+=dt;
            if(phase!=3) return;
            if(bar!=lastBar || fish!=lastFish || progress!=lastProgress)
            { sampleAge=0; lastBar=bar; lastFish=fish; lastProgress=progress; }
            else sampleAge+=dt;
            float blend=1-Mathf.Exp(-dt/ .055f);
            Fish=Mathf.Lerp(Fish,fish,blend);
            Progress=Mathf.Lerp(Progress,progress,blend);
            // Bound anticipation. A stalled connection must not keep simulating a catch.
            if(sampleAge<.35f)
            {
                FishingGame.MoveBar(ref Bar,ref speed,Zone,Held,dt);
                correctionDelay=Mathf.Max(0,correctionDelay-dt);
                if(correctionDelay<=0)
                {
                    float target=Mathf.Clamp(bar+velocity*Mathf.Min(sampleAge,.1f),zone*.5f,1-zone*.5f);
                    Bar=Mathf.Lerp(Bar,target,1-Mathf.Exp(-dt/ .12f));
                }
                Bar=Mathf.Clamp(Bar,Mathf.Max(zone*.5f,bar-.16f),Mathf.Min(1-zone*.5f,bar+.16f));
            }
            else { Bar=Mathf.Lerp(Bar,bar,blend); speed=velocity; }
        }
    }
}
