using UnityEngine;

namespace CyberExtraction.Items
{
    // Server-owned simulation. Presentation only predicts the movement of the control window.
    public sealed class FishingGame
    {
        public FishingStage Stage { get; private set; }
        public byte Phase => Stage==FishingStage.None ? (byte)0 : Stage==FishingStage.Bite ? (byte)2 : Stage==FishingStage.Capture ? (byte)3 : Stage==FishingStage.Success ? (byte)4 : Stage==FishingStage.Failed ? (byte)5 : (byte)1;
        public float Bar=.5f, Fish=.5f, Progress, Zone=.28f;
        public bool Nibble => Stage==FishingStage.Investigating;
        public bool UseBait { get; private set; }
        public bool BaitConsumed { get; private set; }
        public bool Done => Stage==FishingStage.Success || Stage==FishingStage.Failed;
        public int Visitor { get; private set; }
        public ItemId Item { get; private set; }
        public string Notice { get; private set; }
        public float StageStarted { get; private set; }
        public float Duration { get; private set; }
        float attemptAt, captureAt, inputAt, velocity, seed;
        bool held, willBite;
        FishingDefinition Definition=>FishingDefinition.Current;
        public float Velocity => velocity;
        public void Open(float now)
        {
            UseBait=BaitConsumed=held=false; Visitor=0; Notice=null; Progress=0; Bar=Fish=.5f; velocity=0;
            Set(FishingStage.Ready,now,0);
        }
        public void Close() { Stage=FishingStage.None; held=false; }
        public bool SelectBait(bool value)
        { if(Stage!=FishingStage.Ready && !Done) return false; UseBait=value; return true; }
        public bool Cast(bool bait,float now)
        {
            if(Stage!=FishingStage.Ready && !Done) return false;
            UseBait=bait; BaitConsumed=held=false; Visitor=0; Notice=null; Progress=0; Bar=Fish=.5f; velocity=0;
            attemptAt=inputAt=now; Set(FishingStage.Cast,now,Mathf.Max(.1f,Definition.CastSeconds)); return true;
        }
        public void ConsumeBait()=>BaitConsumed=true;
        public void Fail(string message,float now) { Notice=message; Set(FishingStage.Failed,now,0); held=false; }
        void Set(FishingStage stage,float now,float duration) { Stage=stage; StageStarted=now; Duration=duration; }
        void Wait(float now)=>Set(FishingStage.Waiting,now,Random.Range(Definition.WaitMin,Definition.WaitMax));
        public void Hook(float now)
        {
            if(Stage==FishingStage.Bite)
            {
                if(now>StageStarted+Duration) { Fail("O peixe escapou. A fisgada chegou tarde.",now); return; }
                if(UseBait && !BaitConsumed) return;
                Bar=Fish=.5f; Progress=.20f; velocity=0; held=false; captureAt=inputAt=now;
                Set(FishingStage.Capture,now,Definition.CaptureSeconds);
            }
            else if(Stage==FishingStage.Approaching || Stage==FishingStage.Investigating)
                Set(FishingStage.Leaving,now,Definition.LeaveSeconds);
        }
        public void Input(bool value,float now)
        {
            held=value; inputAt=now;
        }
        public void Tick(float now,float dt)
        {
            if(Stage==FishingStage.None || Stage==FishingStage.Ready || Done) return;
            if(now-attemptAt>=Definition.AttemptSeconds) { Fail("Pesca encerrada pelo limite de tempo.",now); return; }
            float age=now-StageStarted;
            if(Stage!=FishingStage.Capture)
            {
                if(age<Duration) return;
                switch(Stage)
                {
                    case FishingStage.Cast: case FishingStage.Leaving: Wait(now); break;
                    case FishingStage.Waiting:
                        Item=Definition.Roll(UseBait); Visitor++; Zone=Definition.For(Item).Zone;
                        seed=Random.Range(0f,6f); willBite=Random.Range(0,100)<Definition.BitePercent;
                        Set(FishingStage.Approaching,now,Definition.ApproachSeconds); break;
                    case FishingStage.Approaching: Set(FishingStage.Investigating,now,Random.Range(Definition.InvestigateMin,Definition.InvestigateMax)); break;
                    case FishingStage.Investigating: Set(willBite ? FishingStage.Bite : FishingStage.Leaving,now,willBite ? Definition.HookSeconds : Definition.LeaveSeconds); break;
                    case FishingStage.Bite: Fail("O peixe escapou. Espere a mordida e pressione para fisgar.",now); break;
                }
                return;
            }
            var profile=Definition.For(Item);
            float t=now-captureAt, target=.5f+Mathf.Sin(t*profile.Frequency+seed)*.26f;
            if(Item==ItemId.FishLarge) target=Mathf.Clamp01(target+Mathf.Sin(t*2.8f)*.12f);
            Fish=Mathf.MoveTowards(Fish,target,Mathf.Max(0,dt)*profile.Speed);
            bool press=held && now-inputAt<.7f;
            MoveBar(ref Bar,ref velocity,Zone,press,dt);
            bool caught=Mathf.Abs(Fish-Bar)<=Zone*.5f;
            Progress=Mathf.Clamp01(Progress+dt*(caught ? profile.Gain : -Definition.Loss));
            if(Progress>=1) { Set(FishingStage.Success,now,0); held=false; }
            else if(Progress<=0 || t>=Definition.CaptureSeconds) Fail(Progress<=0 ? "O peixe escapou da faixa." : "O peixe escapou pelo limite da disputa.",now);
        }
        // Shared motion only. Presentation never awards progress or a catch.
        public static void MoveBar(ref float bar,ref float speed,float zone,bool press,float dt)
        {
            speed=Mathf.MoveTowards(speed,press ? .65f : -.65f,dt*2.5f);
            bar=Mathf.Clamp(bar+speed*dt,zone*.5f,1-zone*.5f);
            if(bar<=zone*.5f || bar>=1-zone*.5f) speed=0;
        }
    }
    public static class CookingGame
    {
        public const float Seconds=8f, TraverseSeconds=1.2f, MinimumInputSeconds=.1f;
        public const float GoodStart=.46f, PerfectStart=.59f, PerfectEnd=.67f, GoodEnd=.80f;
        // A quick timing challenge: one traversal takes 1.2 seconds, then reverses.
        public static float Marker(float elapsed) => Mathf.PingPong(Mathf.Max(0,elapsed)/TraverseSeconds,1);
        public static int Quality(float elapsed)
        {
            float marker=Marker(elapsed);
            return marker<GoodStart ? 0 : marker>GoodEnd ? 1 : marker>=PerfectStart && marker<=PerfectEnd ? 3 : 2;
        }
    }
}
