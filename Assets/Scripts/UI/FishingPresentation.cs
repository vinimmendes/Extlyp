using UnityEngine;
using UnityEngine.InputSystem;
using CyberExtraction.Items;
using CyberExtraction.Players;
using CyberExtraction.Camp;

namespace CyberExtraction.UI
{
    // One instance per screen. All animation advances in Update, never in OnGUI.
    public sealed class FishingPresentation
    {
        public float Bar=.5f, Fish=.5f, Progress, Zone=.28f;
        public bool Held { get; private set; }
        public bool Pressed { get; private set; }
        public FishingStage Stage { get; private set; }
        public int Visitor { get; private set; }
        public ItemId Item { get; private set; }
        public bool UseBait { get; private set; }
        public bool BaitConsumed { get; private set; }
        public string Notice { get; private set; }
        public float Duration { get; private set; }
        public bool Ready=>Stage==FishingStage.Ready || Stage==FishingStage.Success || Stage==FishingStage.Failed;
        public bool Synchronized=>lastSnapshot!=null && Time.unscaledTime-sampledAt<.7f && CampSession.Synchronized;
        public float Fraction=>Duration>0 ? Mathf.Clamp01(PhaseAge/Duration) : 0;
        CampSnapshot lastSnapshot;
        float sampledAt, stageAge;
        public float PhaseAge { get; private set; }
        public int Phase { get; private set; }
        float speed, lastBar, lastFish, lastProgress, sampleAge, correctionDelay;
        float nextSend;
        bool sentHeld, sent, mouseCaptured,awaitRelease=true;
        int touchId=-1;

        public void Reset()
        {
            Phase=0; PhaseAge=0; Held=false; sent=false; mouseCaptured=false; touchId=-1;
            Stage=FishingStage.None; Pressed=false; lastSnapshot=null; awaitRelease=true;
            speed=sampleAge=correctionDelay=0;
        }

        public void ReadInput(bool enabled)
        {
            bool value=false;
            if(enabled && Synchronized && Application.isFocused)
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
                if(awaitRelease) { if(!value) awaitRelease=false; value=false; }
            }
            else { mouseCaptured=false; touchId=-1; awaitRelease=true; }
            if(value!=Held) correctionDelay=.12f;
            Pressed=value && !Held; Held=value;
        }

        public bool NeedsSend(float now) => !sent || Held!=sentHeld || now>=nextSend;
        public void Sent(float now) { sent=true; sentHeld=Held; nextSend=now+.2f; }

        public void Tick(CampSnapshot view,float dt)
        {
            int phase=view.FishingStage==FishingStage.Bite ? 2 : view.FishingStage==FishingStage.Capture ? 3 : 1;
            float bar=view.FishingBar,fish=view.Fish,zone=view.Zone,progress=view.Progress,velocity=view.BarVelocity;
            if(view!=lastSnapshot)
            {
                if(Stage!=view.FishingStage) { PhaseAge=0; if(view.FishingStage==FishingStage.Bite) MinigameView.BiteCue(); }
                Stage=view.FishingStage; Visitor=view.FishingVisitor; Item=view.FishingItem; Duration=view.FishingDuration;
                UseBait=view.FishingBait; BaitConsumed=view.FishingConsumed; Notice=view.FishingMessage;
                stageAge=view.FishingAge; sampledAt=Time.unscaledTime; lastSnapshot=view;
            }
            dt=Mathf.Clamp(dt,0,.05f);
            Zone=zone;
            if(phase!=Phase)
            {
                Phase=phase; PhaseAge=0; Bar=bar; Fish=fish; Progress=progress;
                speed=velocity; sampleAge=0; lastBar=bar; lastFish=fish; lastProgress=progress;
            }
            PhaseAge=stageAge+Mathf.Min(.35f,Time.unscaledTime-sampledAt);
            if(phase!=3) { Progress=progress; return; }
            if(bar!=lastBar || fish!=lastFish || progress!=lastProgress)
            { sampleAge=0; lastBar=bar; lastFish=fish; lastProgress=progress; }
            else sampleAge+=dt;
            float blend=1-Mathf.Exp(-dt/ .055f);
            Fish=Mathf.Lerp(Fish,fish,blend);
            Progress=Mathf.Lerp(Progress,progress,blend);
            // Bound anticipation. A stalled connection must not keep simulating a catch.
            if(Synchronized && Time.unscaledTime-sampledAt<.35f)
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
