using UnityEngine;

namespace CyberExtraction.Items
{
    // One authoritative simulation is shared by camp and expedition fishing.
    public sealed class FishingGame
    {
        public byte Phase { get; private set; } // waiting, bite, capture, success, escaped
        public float Bar=.5f, Fish=.5f, Progress, Zone=.28f;
        public bool Nibble { get; private set; }
        float biteAt, nextNibble, nibbleUntil, captureAt, inputAt, pressedAt=-1, velocity, seed;
        bool held;
        public float Velocity => velocity;
        ItemId item;
        public void Begin(ItemId caught,float now)
        {
            item=caught; Phase=1; Bar=Fish=.5f; Progress=.35f; velocity=0; held=false; pressedAt=-1; inputAt=now;
            Zone=item==ItemId.FishLarge ? .20f : item==ItemId.FishRare ? .23f : .28f;
            biteAt=now+Random.Range(2.5f,5f); nextNibble=now+.7f; nibbleUntil=0; seed=Random.Range(0f,6f); Nibble=false;
        }
        public void Input(bool value,float now)
        {
            if(value && !held) pressedAt=now;
            held=value; inputAt=now;
        }
        public void Tick(float now,float dt)
        {
            if(Phase==0 || Phase>=4) return;
            bool press=held && now-inputAt<.7f;
            if(Phase==1)
            {
                if(pressedAt>=0 && pressedAt<biteAt) { Phase=5; return; }
                if(now>=nextNibble && now<biteAt) { nibbleUntil=now+.18f; nextNibble=now+Random.Range(.45f,.8f); }
                Nibble=now<nibbleUntil;
                if(now<biteAt) return;
                Phase=2; Nibble=false;
            }
            if(Phase==2)
            {
                // Holding before the bite cannot automatically hook a fish.
                if(pressedAt>=biteAt && pressedAt<=biteAt+1.6f) { Phase=3; captureAt=now; }
                else if(now>biteAt+1.6f) Phase=5;
                return;
            }
            float t=now-captureAt, difficulty=item==ItemId.FishLarge ? 1.5f : item==ItemId.FishRare ? 1.3f : 1;
            float target=.5f+Mathf.Sin(t*(item==ItemId.FishRare ? 2.2f : 1.25f)+seed)*.26f;
            if(item==ItemId.FishLarge) target=Mathf.Clamp01(target+(Mathf.Sin(t*2.8f)>0 ? .18f : -.18f));
            Fish=Mathf.MoveTowards(Fish,target,dt*(item==ItemId.FishLarge ? .7f : .45f));
            MoveBar(ref Bar,ref velocity,Zone,press,dt);
            bool caught=Mathf.Abs(Fish-Bar)<=Zone*.5f;
            Progress=Mathf.Clamp01(Progress+dt*(caught ? .18f/difficulty : -.12f));
            if(Progress>=1) Phase=4;
            else if(Progress<=0 || t>45) Phase=5;
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
