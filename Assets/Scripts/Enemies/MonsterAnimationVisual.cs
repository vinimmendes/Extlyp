using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Combat;
using CyberExtraction.Match;

namespace CyberExtraction.Enemies
{
    [DefaultExecutionOrder(20)]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MonsterAnimationVisual : MonoBehaviour
    {
        SpriteRenderer visual;
        Monster monster;
        Health health;
        Sprite[] walk,action;
        Vector2 previousPosition;
        float phase,lastMove,releaseStarted=-1;
        double preparingEnd;
        bool wasPreparing;
        MonsterKind kind;
        const float MinimumSpeed=.05f, SnapshotGrace=.1f;
        void Awake()
        {
            visual=GetComponent<SpriteRenderer>(); monster=GetComponentInParent<Monster>();
            health=GetComponentInParent<Health>();
        }
        public void Configure(MonsterKind value)
        {
            kind=value; walk=MonsterMotionArt.Get(kind,false); action=MonsterMotionArt.Get(kind,true);
            ResetMotion();
        }
        void OnEnable() { ResetMotion(); }
        void ResetMotion()
        {
            previousPosition=monster!=null ? (Vector2)monster.transform.position : (Vector2)transform.position;
            phase=0; lastMove=float.NegativeInfinity; releaseStarted=-1; preparingEnd=0; wasPreparing=false;
        }
        void LateUpdate()
        {
            if(monster==null || !monster.IsSpawned || !monster.IsClient || walk==null || action==null) return;
            Vector2 position=monster.transform.position;
            float distance=Vector2.Distance(position,previousPosition); previousPosition=position;
            bool active=health!=null && !health.IsDead && MatchManager.Instance!=null && MatchManager.Instance.CurrentState==MatchState.Running;
            bool jump=distance>Mathf.Max(1,monster.chaseSpeed*Time.deltaTime*4);
            bool moving=active && !jump && Time.deltaTime>0 && distance>MinimumSpeed*Time.deltaTime;
            double now=monster.NetworkManager.ServerTime.Time;
            bool preparing=active && !jump && (kind==MonsterKind.Acid ? monster.AbilityState.Value==1 : monster.MeleeEnds.Value>now);
            // Shell's frame 7 (zero-based) is the cut crossing the authoritative melee tick.
            int lastPreparation=kind==MonsterKind.Acid ? 28 : kind==MonsterKind.Shell ? 6 : 11;
            float duration=kind==MonsterKind.Acid ? Monster.AcidWindupSeconds : .3f;
            if(!active || jump) { ResetMotion(); visual.sprite=action[action.Length-1]; return; }
            if(moving)
            {
                // Use the rendered displacement and sprite scale, including on remote clients.
                float cycleDistance=MonsterMotionArt.WalkCycleDistance(kind)*transform.TransformVector(Vector3.right).magnitude;
                phase=Mathf.Repeat(phase+distance/Mathf.Max(.001f,cycleDistance),1);
                lastMove=Time.time;
            }
            if(preparing)
            {
                preparingEnd=kind==MonsterKind.Acid ? monster.AbilityEnds.Value : monster.MeleeEnds.Value;
                float progress=1-Mathf.Clamp01((float)(preparingEnd-now)/duration);
                // Preparation stops before the impact frame; release starts on the server's damage tick.
                visual.sprite=action[Mathf.Clamp(Mathf.FloorToInt(progress*(lastPreparation+1)),0,lastPreparation)];
                releaseStarted=-1;
                lastMove=float.NegativeInfinity;
            }
            else
            {
                // Returning home/staggering cancels preparation; it must not play a fake attack.
                if(wasPreparing && preparingEnd>0 && now>=preparingEnd-.02) releaseStarted=Time.time;
                preparingEnd=0;
                float grace=Mathf.Clamp(2f/Mathf.Max(1f,(float)monster.NetworkManager.NetworkConfig.TickRate),SnapshotGrace,.25f);
                bool walking=Time.time-lastMove<grace;
                int release=releaseStarted<0 ? action.Length : lastPreparation+1+Mathf.FloorToInt((Time.time-releaseStarted)*24);
                int minimumRelease=kind==MonsterKind.Acid || kind==MonsterKind.Shell ? 5 : 2;
                if(release<action.Length && (!walking || release<lastPreparation+1+minimumRelease)) visual.sprite=action[release];
                else if(walking)
                {
                    releaseStarted=-1; visual.sprite=walk[Mathf.Clamp(Mathf.FloorToInt(phase*walk.Length),0,walk.Length-1)];
                }
                else
                {
                    // Keep the planted pose and phase when movement stops; a pause
                    // between physics/network updates must not restart the stride.
                    releaseStarted=-1;
                    visual.sprite=lastMove>float.NegativeInfinity
                        ? walk[Mathf.Clamp(Mathf.FloorToInt(phase*walk.Length),0,walk.Length-1)]
                        : action[action.Length-1];
                }
            }
            wasPreparing=preparing;
            // Fog visibility and damage flash retain ownership of enabled/tint.
        }
    }

    static class MonsterMotionArt
    {
        const int Cell=256, Columns=8;
        static readonly Dictionary<string,Sprite[]> Cache=new Dictionary<string,Sprite[]>();
        public static float WalkCycleDistance(MonsterKind kind)
        {
            // Foot travel during ground contact / fraction of the cycle in contact,
            // converted from the source camera framing to the runtime sprite size.
            // CorrosivePouch/build_corrosive.py: .60 / .60, framing 2.4 -> 2.4.
            // StalkerHoodedV3/animate_model.py: .40 / .60, framing 3.2 -> 3.2.
            // SpiderPale/build_spider.py: .40 / .50, framing 3.65 -> 2.4.
            // Shell source is external: approximate 23px support travel over half
            // the 32-frame atlas cycle, at 256px / 2.2 units. Tune visually in game.
            switch(kind)
            {
                case MonsterKind.Acid: return .60f/.60f;
                case MonsterKind.Stalker: return .40f/.60f;
                case MonsterKind.Weaver: return (.40f/.50f)*(2.4f/3.65f);
                case MonsterKind.Shell: return (23f/256f*2.2f)/.50f;
                default: return 1;
            }
        }
        public static Sprite[] Get(MonsterKind kind,bool action)
        {
            if(kind!=MonsterKind.Acid && kind!=MonsterKind.Stalker && kind!=MonsterKind.Shell) return null;
            string folder=kind==MonsterKind.Acid ? "Acid" : kind==MonsterKind.Shell ? "Shell" : "Stalker";
            string name=action ? (kind==MonsterKind.Acid ? "Spit" : kind==MonsterKind.Shell ? "ArcAttack" : "ClawAttack") : "Walk";
            string path=folder+"/"+name;
            if(Cache.TryGetValue(path,out var frames)) return frames;
            int count=action ? (kind==MonsterKind.Acid ? 45 : kind==MonsterKind.Shell ? 27 : 20) : 32;
            var texture=Resources.Load<Texture2D>("Monsters/"+path);
            if(texture==null || texture.width!=2048 || texture.height!=((count+7)/8)*Cell)
            { Cache[path]=null; Debug.LogError("Atlas de monstro ausente/invalido: "+path); return null; }
            frames=new Sprite[count];
            for(int i=0;i<count;i++)
            {
                var rect=new Rect(i%Columns*Cell,texture.height-(i/Columns+1)*Cell,Cell,Cell);
                // Match the fixed orthographic framing of each Blender export.
                frames[i]=Sprite.Create(texture,rect,new Vector2(.5f,.5f),Cell/(kind==MonsterKind.Acid ? 2.4f : kind==MonsterKind.Shell ? 2.2f : 3.2f),0,SpriteMeshType.FullRect);
                frames[i].name=path+"_"+(i+1);
            }
            Cache[path]=frames; return frames;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            foreach(var frames in Cache.Values) if(frames!=null) foreach(var sprite in frames) if(sprite!=null) Object.Destroy(sprite);
            Cache.Clear();
        }
    }
}
