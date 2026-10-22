using UnityEngine;
using CyberExtraction.Combat;
using CyberExtraction.Core;
using CyberExtraction.Match;

namespace CyberExtraction.Enemies
{
    [DefaultExecutionOrder(20)]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WeaverVisual : MonoBehaviour
    {
        SpriteRenderer visual;
        Monster monster;
        Health health;
        Vector2 previousPosition;
        float phase, lastMove, releaseUntil;
        double lastReleaseAt;
        const float ReleaseDuration=9f/24f;
        const float MinimumSpeed=.05f, SnapshotGrace=.1f;
        Vector3 restPosition,restScale;
        SpriteRenderer tether;
        void Awake()
        {
            visual=GetComponent<SpriteRenderer>(); monster=GetComponentInParent<Monster>();
            health=GetComponentInParent<Health>();
            restPosition=transform.localPosition; restScale=transform.localScale;
            var thread=new GameObject("CeilingSilk"); thread.transform.SetParent(transform.parent,false);
            tether=thread.AddComponent<SpriteRenderer>();
            SpriteFactory.Setup(tether,SpriteFactory.Square,new Color(.72f,.70f,.58f,.7f),visual.sortingOrder);
            thread.SetActive(false);
            monster.GetComponent<VisibilityTarget>()?.Refresh();
        }
        void OnEnable()
        {
            previousPosition=monster!=null ? monster.transform.position : transform.position; phase=0; lastMove=float.NegativeInfinity;
            lastReleaseAt=monster!=null ? monster.WebReleasedAt.Value : -1; releaseUntil=0;
        }
        void LateUpdate()
        {
            if(monster==null || !monster.IsSpawned || !monster.IsClient) return;
            var walk=WeaverArt.Walk; var web=WeaverArt.Web;
            if(walk==null || web==null) return;
            Vector2 position=monster.transform.position;
            float distance=Vector2.Distance(position,previousPosition); previousPosition=position;
            bool active=health!=null && !health.IsDead && MatchManager.Instance!=null && MatchManager.Instance.CurrentState==MatchState.Running;
            transform.localPosition=restPosition; transform.localScale=restScale;
            if(tether!=null) tether.gameObject.SetActive(false);
            if(SpiderAction())
            {
                lastMove=float.NegativeInfinity; releaseUntil=0; lastReleaseAt=monster.WebReleasedAt.Value;
                return;
            }
            bool jump=distance>Mathf.Max(1,5*Time.deltaTime);
            bool moving=active && !jump && Time.deltaTime>0 && distance>MinimumSpeed*Time.deltaTime;
            if(!active || jump)
            {
                releaseUntil=0; phase=0; lastMove=float.NegativeInfinity; lastReleaseAt=monster.WebReleasedAt.Value;
                visual.sprite=web[27]; return;
            }
            double releasedAt=monster.WebReleasedAt.Value;
            if(releasedAt!=lastReleaseAt)
            {
                lastReleaseAt=releasedAt;
                float age=Mathf.Max(0,(float)(monster.NetworkManager.ServerTime.Time-releasedAt));
                releaseUntil=releasedAt>=0 && age<ReleaseDuration ? Time.time+ReleaseDuration-age : 0;
            }
            bool preparing=monster.AbilityState.Value==1 && releaseUntil<=Time.time;
            if(preparing)
            {
                float remaining=(float)(monster.AbilityEnds.Value-monster.NetworkManager.ServerTime.Time);
                float progress=1-Mathf.Clamp01(remaining/.8f);
                visual.sprite=web[Mathf.Clamp(Mathf.FloorToInt(progress*19),0,18)];
                lastMove=float.NegativeInfinity;
            }
            else
            {
                if(moving)
                {
                    float cycleDistance=MonsterMotionArt.WalkCycleDistance(MonsterKind.Weaver)*transform.TransformVector(Vector3.right).magnitude;
                    phase=Mathf.Repeat(phase+distance/Mathf.Max(.001f,cycleDistance),1);
                    lastMove=Time.time; releaseUntil=0;
                }
                float grace=Mathf.Clamp(2f/Mathf.Max(1f,(float)monster.NetworkManager.NetworkConfig.TickRate),SnapshotGrace,.25f);
                bool walking=Time.time-lastMove<grace;
                if(walking) visual.sprite=walk[Mathf.Clamp(Mathf.FloorToInt(phase*walk.Length),0,walk.Length-1)];
                else if(active && Time.time<releaseUntil)
                {
                    int frame=19+Mathf.FloorToInt((ReleaseDuration-(releaseUntil-Time.time))*24);
                    visual.sprite=web[Mathf.Clamp(frame,19,27)];
                }
                else visual.sprite=lastMove>float.NegativeInfinity
                    ? walk[Mathf.Clamp(Mathf.FloorToInt(phase*walk.Length),0,walk.Length-1)] : web[27];
            }
            // VisibilityTarget and DamageFlash own visibility and tint.
        }
        bool SpiderAction()
        {
            if(monster.SpiderPhase==WeaverPhase.Nest)
            { visual.sprite=WeaverArt.Nest; return true; }
            if(monster.SpiderPhase==WeaverPhase.Descending)
            {
                float progress=1-Mathf.Clamp01((float)(monster.WeaverStateEnds.Value-monster.NetworkManager.ServerTime.Time)/Monster.WeaverDescentSeconds);
                var frames=WeaverArt.Descend;
                if(frames!=null) visual.sprite=frames[Mathf.Min(frames.Length-1,Mathf.FloorToInt(progress*frames.Length))];
                float height=1.7f*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01(progress/.8f)));
                transform.localPosition=restPosition+transform.parent.InverseTransformVector(Vector3.up*height);
                if(tether!=null && progress<.92f)
                {
                    tether.gameObject.SetActive(true);
                    Vector3 ceiling=transform.parent.TransformPoint(restPosition)+Vector3.up*2.7f;
                    Vector3 body=transform.position;
                    tether.transform.position=(ceiling+body)*.5f; tether.transform.rotation=Quaternion.identity;
                    var scale=transform.parent.lossyScale;
                    tether.transform.localScale=new Vector3(.018f/Mathf.Max(.001f,Mathf.Abs(scale.x)),
                        (ceiling.y-body.y)/Mathf.Max(.001f,Mathf.Abs(scale.y)),1);
                }
                return true;
            }
            var dash=WeaverArt.Dash;
            if(dash==null) return false;
            switch(monster.SpiderPhase)
            {
                case WeaverPhase.DashWindup: SetAction(dash,0,12,Monster.WeaverWindupSeconds); return true;
                case WeaverPhase.Dashing: SetAction(dash,12,6,Monster.WeaverDashSeconds); return true;
                case WeaverPhase.Recovering: SetAction(dash,18,14,Monster.WeaverRecoverySeconds); return true;
                default: return false;
            }
        }
        void SetAction(Sprite[] frames,int start,int count,float seconds)
        {
            float progress=1-Mathf.Clamp01((float)(monster.WeaverStateEnds.Value-monster.NetworkManager.ServerTime.Time)/seconds);
            visual.sprite=frames[start+Mathf.Min(count-1,Mathf.FloorToInt(progress*count))];
        }
    }

    static class WeaverArt
    {
        const int Cell=256, Columns=8;
        static Sprite[] walk,web,dash,descend,nest;
        static bool loaded;
        public static Sprite[] Walk { get { Load(); return walk; } }
        public static Sprite[] Web { get { Load(); return web; } }
        public static Sprite[] Dash { get { Load(); return dash; } }
        public static Sprite[] Descend { get { Load(); return descend; } }
        public static Sprite Nest { get { Load(); return nest!=null ? nest[0] : null; } }
        static void Load()
        {
            if(loaded) return; loaded=true;
            walk=Slice("Walk",32); web=Slice("Web",28);
            dash=Slice("Dash",32); descend=Slice("Descend",32); nest=Slice("Nest",1);
        }
        static Sprite[] Slice(string name,int count)
        {
            var texture=Resources.Load<Texture2D>("Monsters/Weaver/"+name);
            if(texture==null || texture.width!=(count==1 ? Cell : 2048) || texture.height!=(count==1 ? Cell : 1024))
            { Debug.LogError("Atlas da aranha ausente ou invalido: "+name); return null; }
            var frames=new Sprite[count];
            for(int i=0;i<count;i++)
            {
                var rect=new Rect(i%Columns*Cell,texture.height-(i/Columns+1)*Cell,Cell,Cell);
                frames[i]=Sprite.Create(texture,rect,new Vector2(.5f,.5f),Cell/2.4f,0,SpriteMeshType.FullRect);
                frames[i].name="Weaver_"+name+"_"+i;
            }
            return frames;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            foreach(var frames in new[]{walk,web,dash,descend,nest}) if(frames!=null) foreach(var sprite in frames) if(sprite!=null) Object.Destroy(sprite);
            walk=web=dash=descend=nest=null; loaded=false;
        }
    }
}
