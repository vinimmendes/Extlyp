using System;
using System.Collections.Generic;

namespace CyberExtraction.Lockpicking
{
    public enum LockGrade { Simple, Common, Reinforced }
    public enum LockpickState { Idle, Picking, Cancelled, PickBroken, Unlocked }
    public enum LockpickSignal { Started, Pressed, ClickCue, PinSet, Mistake, PickBroken, Cancelled, Unlocked }

    public sealed class LockDefinition
    {
        public LockGrade Grade { get; }
        public int PinCount { get; }
        public double Tolerance { get; }
        public double PressSpeed { get; }
        public double OverloadSeconds { get; }

        public LockDefinition(LockGrade grade)
        {
            if(!Enum.IsDefined(typeof(LockGrade),grade)) throw new ArgumentOutOfRangeException(nameof(grade));
            Grade=grade;
            PinCount=grade==LockGrade.Simple ? 2 : grade==LockGrade.Common ? 3 : 4;
            Tolerance=grade==LockGrade.Simple ? .10 : grade==LockGrade.Common ? .075 : .055;
            PressSpeed=.28; OverloadSeconds=.30;
        }
    }

    // One canonical server-owned instance per physical tool. Cancellation never repairs it.
    public sealed class LockpickBreakage
    {
        public double InitialChance { get; }
        public double ChanceIncrease { get; }
        public double MaximumChance { get; }
        public LockpickBreakage(double initialChance=.10,double chanceIncrease=.15,double maximumChance=.95)
        {
            if(double.IsNaN(initialChance) || initialChance<0 || initialChance>=1) throw new ArgumentOutOfRangeException(nameof(initialChance));
            if(double.IsNaN(chanceIncrease) || double.IsInfinity(chanceIncrease) || chanceIncrease<=0) throw new ArgumentOutOfRangeException(nameof(chanceIncrease));
            if(double.IsNaN(maximumChance) || maximumChance<initialChance || maximumChance>=1) throw new ArgumentOutOfRangeException(nameof(maximumChance));
            InitialChance=initialChance; ChanceIncrease=chanceIncrease; MaximumChance=maximumChance;
        }
        public double ChanceFor(int accumulatedErrors) => Math.Min(MaximumChance,InitialChance+ChanceIncrease*accumulatedErrors);
    }

    public sealed class LockpickTool
    {
        readonly Func<double> serverRandom;
        readonly LockpickBreakage breakage;
        public int AccumulatedErrors { get; private set; }
        public bool Broken { get; private set; }
        public double ChanceOnNextError => breakage.ChanceFor(AccumulatedErrors);
        // Supply a SERVER random stream separate from the lock's solution seed.
        public LockpickTool(Func<double> serverRandom,LockpickBreakage breakage=null,int accumulatedErrors=0,bool broken=false)
        {
            this.serverRandom=serverRandom ?? throw new ArgumentNullException(nameof(serverRandom));
            this.breakage=breakage ?? new LockpickBreakage();
            if(accumulatedErrors<0) throw new ArgumentOutOfRangeException(nameof(accumulatedErrors));
            AccumulatedErrors=accumulatedErrors; Broken=broken;
        }
        internal void Wear()
        {
            if(Broken) return;
            double chance=ChanceOnNextError,roll=serverRandom();
            if(double.IsNaN(roll) || roll<0 || roll>=1) throw new InvalidOperationException("Server random must return a value in [0,1).");
            if(AccumulatedErrors<int.MaxValue) AccumulatedErrors++;
            Broken=roll<chance;
        }
    }

    public readonly struct LockpickView
    {
        public readonly LockpickState State;
        public readonly Guid Attempt;
        public readonly int ActivePin, CompletedPins;
        public readonly double Depth, Tension;
        public readonly bool Held, ClickCue, ReleaseRequired, ToolBroken;
        public readonly IReadOnlyList<bool> SetPins;
        public LockpickView(LockpickState state,Guid attempt,int activePin,int completed,
            double depth,double tension,bool held,bool cue,bool releaseRequired,bool toolBroken,IReadOnlyList<bool> setPins)
        {
            State=state; Attempt=attempt; ActivePin=activePin; CompletedPins=completed;
            Depth=depth; Tension=tension;
            Held=held; ClickCue=cue; ReleaseRequired=releaseRequired; ToolBroken=toolBroken; SetPins=setPins;
        }
    }
}
