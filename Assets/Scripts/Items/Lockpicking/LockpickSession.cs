using System;

namespace CyberExtraction.Lockpicking
{
    // Pure C#: no MonoBehaviour, inventory, networking, scene or asset dependency.
    // The caller must supply server identity, authoritative reach checks and a SERVER clock.
    public sealed class LockpickSession
    {
        readonly LockDefinition definition;
        readonly double[] setPoints;
        readonly bool[] setPins;
        ulong? owner;
        LockpickTool tool;
        Guid attempt;
        long lastSequence;
        double lastTime, pressStarted, depth, tension;
        int activePin=-1, completed;
        bool held, cue, releaseRequired;

        public LockpickState State { get; private set; }=LockpickState.Idle;
        public bool Unlocked => State==LockpickState.Unlocked;
        public event Action<LockpickSignal> Signalled;

        // Stable seed belongs to the lock, not to each attempt. Never replicate it to clients.
        public LockpickSession(LockDefinition definition,int serverSeed)
        {
            this.definition=definition ?? throw new ArgumentNullException(nameof(definition));
            setPoints=new double[definition.PinCount]; setPins=new bool[definition.PinCount];
            var random=new Random(serverSeed);
            for(int i=0;i<setPoints.Length;i++) setPoints[i]=.34+random.NextDouble()*.40;
        }

        public bool TryBegin(ulong actor,LockpickTool serverTool,bool serverCanReach,double serverNow,out Guid token)
        {
            token=Guid.Empty;
            if(Unlocked || owner.HasValue || serverTool==null || serverTool.Broken || !serverCanReach || !ValidTime(serverNow)) return false;
            Array.Clear(setPins,0,setPins.Length); completed=0; activePin=-1;
            depth=tension=0; held=cue=releaseRequired=false;
            owner=actor; tool=serverTool; lastTime=serverNow; lastSequence=0;
            attempt=token=Guid.NewGuid(); State=LockpickState.Picking;
            Emit(LockpickSignal.Started); return true;
        }

        public bool TryPress(ulong actor,Guid token,long sequence,int pin,bool serverCanReach,double serverNow)
        {
            if(!Eligible(actor,token,sequence,serverCanReach,serverNow) || pin<0 || pin>=setPins.Length || setPins[pin]) return false;
            Advance(serverNow);
            if(State!=LockpickState.Picking || held || releaseRequired) return false;
            lastSequence=sequence; activePin=pin; pressStarted=serverNow;
            held=true; depth=tension=0; cue=false; Emit(LockpickSignal.Pressed); return true;
        }

        public bool TryRelease(ulong actor,Guid token,long sequence,bool serverCanReach,double serverNow)
        {
            if(!Eligible(actor,token,sequence,serverCanReach,serverNow)) return false;
            lastSequence=sequence; Advance(serverNow);
            if(State!=LockpickState.Picking) return true; // An overload may have broken the tool.
            if(releaseRequired) { releaseRequired=false; return true; } // Acknowledge physical release, no second wear.
            if(!held) return true;
            held=false;
            if(cue)
            {
                setPins[activePin]=true; completed++; depth=tension=0; cue=false;
                Emit(LockpickSignal.PinSet);
                if(completed==setPins.Length)
                {
                    State=LockpickState.Unlocked; owner=null; attempt=Guid.Empty;
                    Emit(LockpickSignal.Unlocked);
                }
            }
            else Fail(false);
            return true;
        }

        public bool TryCancel(ulong actor,Guid token,long sequence,double serverNow)
        {
            if(!Eligible(actor,token,sequence,true,serverNow)) return false;
            lastSequence=sequence; Advance(serverNow);
            if(State==LockpickState.Picking) End(LockpickState.Cancelled,LockpickSignal.Cancelled);
            return true;
        }

        // Call on the server while an attempt is active. Damage/movement/disconnect are trusted interruption events.
        public void Tick(double serverNow,bool serverCanReach,bool interrupted)
        {
            if(State!=LockpickState.Picking || !ValidTime(serverNow)) return;
            if(interrupted || !serverCanReach) { End(LockpickState.Cancelled,LockpickSignal.Cancelled); return; }
            Advance(serverNow);
        }

        public LockpickView ViewFor(ulong viewer)
        {
            bool owns=owner.HasValue && owner.Value==viewer;
            var pins=new bool[setPins.Length];
            if(owns || Unlocked) Array.Copy(setPins,pins,pins.Length);
            // Other observers see a lock as locked/unlocked, never another player's puzzle progress.
            return new LockpickView(owns ? State : Unlocked ? LockpickState.Unlocked : LockpickState.Idle,
                owns ? attempt : Guid.Empty,owns ? activePin : -1,owns ? completed : Unlocked ? pins.Length : 0,
                owns ? depth : 0,owns ? tension : 0,
                owns && held,owns && cue,owns && releaseRequired,owns && tool!=null && tool.Broken,Array.AsReadOnly(pins));
        }

        bool Eligible(ulong actor,Guid token,long sequence,bool canReach,double now) =>
            State==LockpickState.Picking && owner.HasValue && owner.Value==actor && token==attempt &&
            sequence>lastSequence && canReach && ValidTime(now);
        bool ValidTime(double now) => !double.IsNaN(now) && !double.IsInfinity(now) && now>=0 && now>=lastTime;

        void Advance(double now)
        {
            lastTime=now;
            if(!held || State!=LockpickState.Picking) return;
            double elapsed=now-pressStarted,previousDepth=depth;
            depth=Math.Min(1,elapsed*definition.PressSpeed);
            double target=setPoints[activePin],low=target-definition.Tolerance,high=target+definition.Tolerance;
            cue=depth>=low && depth<=high;
            // Analytic calculation avoids accumulating extra pressure when a render frame stalls.
            tension=Math.Max(0,Math.Min(1,(elapsed-high/definition.PressSpeed)/definition.OverloadSeconds));
            if(previousDepth<low && depth>=low && depth<=high) Emit(LockpickSignal.ClickCue);
            if(tension>=1) Fail(true);
        }
        void Fail(bool requireRelease)
        {
            held=false; cue=false; depth=tension=0; releaseRequired=requireRelease;
            tool.Wear(); Emit(LockpickSignal.Mistake);
            if(tool.Broken) End(LockpickState.PickBroken,LockpickSignal.PickBroken);
        }
        void End(LockpickState state,LockpickSignal signal)
        {
            State=state; owner=null; attempt=Guid.Empty;
            held=cue=releaseRequired=false; depth=tension=0; activePin=-1; completed=0;
            Array.Clear(setPins,0,setPins.Length); Emit(signal);
        }
        void Emit(LockpickSignal signal) { Signalled?.Invoke(signal); }
    }
}
