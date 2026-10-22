using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Combat;
using CyberExtraction.Match;
using CyberExtraction.Players;
using CyberExtraction.Core;

namespace CyberExtraction.Enemies
{
    public enum WeaverPhase : byte { Nest, Descending, Grounded, DashWindup, Dashing, Recovering }

    public partial class Monster
    {
        public const float WeaverDescentSeconds=1.5f, WeaverWindupSeconds=.7f, WeaverDashSeconds=.25f, WeaverRecoverySeconds=.6f;
        const float WeaverDashDistance=3f, WeaverDashCooldown=5f, WeaverRingRadius=3.3f;
        public NetworkVariable<byte> WeaverState=new NetworkVariable<byte>((byte)WeaverPhase.Nest);
        public NetworkVariable<double> WeaverStateEnds=new NetworkVariable<double>(0);
        public NetworkVariable<int> WeaverNestHealth=new NetworkVariable<int>(18);
        public WeaverPhase SpiderPhase => (WeaverPhase)WeaverState.Value;
        public bool WeaverInCeiling => Kind==MonsterKind.Weaver && (SpiderPhase==WeaverPhase.Nest || SpiderPhase==WeaverPhase.Descending);
        float nextWeaverDash, weaverDashLeft;
        Vector2 weaverAlertPoint;
        bool weaverHasAlert;
        readonly Vector2[] weaverRingPoints=new Vector2[3];

        void InitializeWeaver()
        {
            if(Kind!=MonsterKind.Weaver) return;
            if(IsServer)
            {
                WeaverState.Value=(byte)WeaverPhase.Nest; WeaverStateEnds.Value=0; WeaverNestHealth.Value=18;
                ChargeState.Value=0; ChargeReach.Value=0;
            }
            ApplyWeaverCollider();
        }
        void ApplyWeaverCollider()
        {
            if(Kind==MonsterKind.Weaver && bodyCollider!=null)
                bodyCollider.enabled=SpiderPhase!=WeaverPhase.Descending;
        }
        // Health calls this before monster damage/XP: a broken nest releases its occupant, never kills it.
        public bool ServerInterceptWeaverDamage(int amount,PlayerInventory source)
        {
            if(!IsServer || !IsSpawned || Kind!=MonsterKind.Weaver || !WeaverInCeiling) return false;
            if(SpiderPhase==WeaverPhase.Nest)
            {
                WeaverNestHealth.Value=Mathf.Max(0,WeaverNestHealth.Value-amount);
                if(WeaverNestHealth.Value==0)
                {
                    CancelMelee(); AbilityState.Value=0; AbilityEnds.Value=0;
                    WeaverState.Value=(byte)WeaverPhase.Descending;
                    WeaverStateEnds.Value=NetworkManager.ServerTime.Time+WeaverDescentSeconds;
                    weaverHasAlert=source!=null;
                    if(weaverHasAlert) weaverAlertPoint=source.transform.position;
                    rb.linearVelocity=Vector2.zero; ApplyWeaverCollider();
                }
            }
            return true;
        }
        bool StepWeaverEntry()
        {
            if(!WeaverInCeiling) return false;
            rb.linearVelocity=Vector2.zero; progressAt=Time.time;
            if(SpiderPhase==WeaverPhase.Descending && NetworkManager.ServerTime.Time>=WeaverStateEnds.Value)
            {
                // An occupied landing must not keep the spider suspended until the player moves.
                // Find nearby clearance before restoring its solid body and combat state.
                if(!TryWeaverLanding(out var landing)) return true;
                rb.position=landing;
                WeaverState.Value=(byte)WeaverPhase.Grounded; WeaverStateEnds.Value=0;
                nextWeaverDash=Time.time+.8f; nextAbility=Time.time+1.2f; nextPerception=0;
                ApplyWeaverCollider();
                if(weaverHasAlert) ServerAlert(weaverAlertPoint);
                weaverHasAlert=false;
            }
            return true;
        }
        bool TryWeaverLanding(out Vector2 landing)
        {
            Vector2 origin=rb.position;
            landing=origin;
            if(WeaverLandingClear(origin)) return true;
            // Search nearest rings first, staying on this side of walls and within the territory.
            for(int ring=1;ring<=10;ring++)
                for(int i=0;i<16;i++)
                {
                    float angle=i*Mathf.PI/8;
                    Vector2 offset=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(ring*.25f);
                    Vector2 candidate=origin+offset;
                    if(Vector2.Distance(home,candidate)>=LeashRadius ||
                        Physics2D.CircleCast(origin,worldRadius+.04f,offset.normalized,offset.magnitude,GameConfig.WallMask).collider!=null ||
                        !WeaverLandingClear(candidate)) continue;
                    landing=candidate; return true;
                }
            return false;
        }
        bool WeaverLandingClear(Vector2 point)
        {
            if(Physics2D.OverlapCircle(point,worldRadius+.04f,GameConfig.WallMask)!=null) return false;
            foreach(var inv in PlayerInventory.All)
            {
                if(inv==null || !inv.IsSpawned || !inv.IsAlive || inv.gameObject.scene!=gameObject.scene) continue;
                var collider=inv.GetComponent<CircleCollider2D>();
                float radius=collider!=null ? collider.radius*Mathf.Max(Mathf.Abs(inv.transform.lossyScale.x),Mathf.Abs(inv.transform.lossyScale.y)) : .5f;
                float clearance=worldRadius+radius+.04f;
                if(((Vector2)inv.transform.position-point).sqrMagnitude<clearance*clearance) return false;
            }
            foreach(var other in All)
            {
                if(other==null || other==this || !other.IsSpawned || other.health.IsDead || other.gameObject.scene!=gameObject.scene ||
                    other.bodyCollider==null || !other.bodyCollider.enabled || other.bodyCollider.isTrigger) continue;
                float clearance=worldRadius+other.worldRadius+.04f;
                if(((Vector2)other.transform.position-point).sqrMagnitude<clearance*clearance) return false;
            }
            return true;
        }
        void RecoverWeaver()
        {
            rb.linearVelocity=Vector2.zero; ChargeState.Value=3; ChargeReach.Value=0;
            WeaverState.Value=(byte)WeaverPhase.Recovering;
            WeaverStateEnds.Value=NetworkManager.ServerTime.Time+WeaverRecoverySeconds;
            nextWeaverDash=Time.time+WeaverDashCooldown; nextAttack=Time.time+WeaverRecoverySeconds;
            nextAbility=Mathf.Max(nextAbility,Time.time+WeaverRecoverySeconds+1);
        }
        void CancelWeaverCombat()
        {
            if(Kind!=MonsterKind.Weaver || WeaverInCeiling) return;
            WeaverState.Value=(byte)WeaverPhase.Grounded; WeaverStateEnds.Value=0;
            AbilityState.Value=0; AbilityEnds.Value=0;
            ChargeState.Value=0; ChargeReach.Value=0; weaverDashLeft=0;
            nextWeaverDash=Time.time+WeaverDashCooldown;
        }
        bool StepWeaverDash()
        {
            if(Kind!=MonsterKind.Weaver) return false;
            double now=NetworkManager.ServerTime.Time;
            if(SpiderPhase==WeaverPhase.DashWindup)
            {
                progressAt=Time.time; rb.linearVelocity=Vector2.zero; rb.rotation=ChargeAim.Value;
                if(now>=WeaverStateEnds.Value)
                {
                    WeaverState.Value=(byte)WeaverPhase.Dashing; ChargeState.Value=2;
                    WeaverStateEnds.Value=now+WeaverDashSeconds; weaverDashLeft=WeaverDashDistance;
                }
                return true;
            }
            if(SpiderPhase==WeaverPhase.Recovering)
            {
                progressAt=Time.time; rb.linearVelocity=Vector2.zero;
                if(now>=WeaverStateEnds.Value) { WeaverState.Value=(byte)WeaverPhase.Grounded; WeaverStateEnds.Value=0; ChargeState.Value=0; }
                return true;
            }
            if(SpiderPhase!=WeaverPhase.Dashing) return false;
            progressAt=Time.time; rb.linearVelocity=Vector2.zero;
            if(now>=WeaverStateEnds.Value || weaverDashLeft<=.001f) { RecoverWeaver(); return true; }
            float angle=ChargeAim.Value*Mathf.Deg2Rad;
            Vector2 direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)), origin=rb.position;
            float step=Mathf.Min(weaverDashLeft,WeaverDashDistance/WeaverDashSeconds*Time.fixedDeltaTime);
            RaycastHit2D nearest=default; float hitDistance=float.PositiveInfinity;
            foreach(var hit in Physics2D.CircleCastAll(origin,worldRadius,direction,step))
            {
                if(hit.collider.isTrigger || hit.collider.GetComponentInParent<NetworkObject>()==NetworkObject) continue;
                var victim=hit.collider.GetComponentInParent<PlayerInventory>();
                if(victim!=null && !victim.IsAlive) continue;
                if(victim==null && hit.collider.GetComponentInParent<Monster>()==null &&
                    hit.collider.GetComponentInParent<PlantCreature>()==null && (GameConfig.WallMask&(1<<hit.collider.gameObject.layer))==0) continue;
                if(hit.distance<hitDistance) { nearest=hit; hitDistance=hit.distance; }
            }
            if(nearest.collider!=null)
            {
                rb.position=origin+direction*Mathf.Max(0,hitDistance-.04f);
                var victim=nearest.collider.GetComponentInParent<PlayerInventory>();
                if(victim!=null && Physics2D.Linecast(origin,victim.transform.position,GameConfig.WallMask).collider==null)
                    victim.GetComponent<Health>()?.ServerTakeDamage(attackDamage,creatureName:DisplayName);
                RecoverWeaver(); return true;
            }
            rb.position=origin+direction*step; weaverDashLeft-=step; ChargeReach.Value=weaverDashLeft;
            return true;
        }
        bool WeaverPackCanAttack()
        {
            foreach(var other in All)
            {
                if(other==null || other==this || !other.IsSpawned || other.Kind!=MonsterKind.Weaver || other.health.IsDead ||
                    ((Vector2)other.transform.position-rb.position).sqrMagnitude>18*18) continue;
                if(other.SpiderPhase==WeaverPhase.DashWindup || other.SpiderPhase==WeaverPhase.Dashing ||
                    other.SpiderPhase==WeaverPhase.Recovering || other.AbilityState.Value==1) return false;
            }
            return true;
        }
        bool PlanWeaverRing(Vector2 center)
        {
            // A real free corridor, including player clearance, defines the opening of the horseshoe.
            Vector2 preferred=observedVelocity.sqrMagnitude>.2f ? observedVelocity.normalized : (center-rb.position).normalized;
            if(preferred.sqrMagnitude<.1f) preferred=Vector2.right;
            for(int candidate=0;candidate<8;candidate++)
            {
                float heading=Mathf.Atan2(preferred.y,preferred.x)+candidate*Mathf.PI/4;
                Vector2 opening=new Vector2(Mathf.Cos(heading),Mathf.Sin(heading));
                if(Physics2D.CircleCast(center,.55f,opening,WeaverRingRadius+1.6f,GameConfig.WallMask).collider!=null) continue;
                if(!WeaverOpeningClear(center,opening)) continue;
                bool clear=true;
                for(int i=0;i<3;i++)
                {
                    float angle=heading+(i+1)*Mathf.PI/2;
                    var point=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*WeaverRingRadius;
                    weaverRingPoints[i]=point;
                    if(Physics2D.OverlapCircle(point,1.4f,GameConfig.WallMask)!=null ||
                        Physics2D.Linecast(rb.position,point,GameConfig.WallMask).collider!=null ||
                        MonsterWorld.CountNear(MonsterEffectKind.Web,point,3)>0 ||
                        MonsterWorld.CountNear(MonsterEffectKind.WebWarning,point,3)>0) { clear=false; break; }
                }
                if(clear) return true;
            }
            return false;
        }
        bool WeaverOpeningClear(Vector2 center,Vector2 direction)
        {
            foreach(var effect in MatchManager.Instance.MonsterEffects)
            {
                if(effect.Kind!=MonsterEffectKind.Web && effect.Kind!=MonsterEffectKind.WebWarning && effect.Kind!=MonsterEffectKind.AcidPool) continue;
                float along=Mathf.Clamp(Vector2.Dot(effect.To-center,direction),0,WeaverRingRadius+1.6f);
                if((effect.To-center-direction*along).sqrMagnitude<(effect.Radius+.55f)*(effect.Radius+.55f)) return false;
            }
            return true;
        }
        void StepWeaverCombat()
        {
            var target=PerceptionTarget();
            if(AbilityState.Value==1)
            {
                progressAt=Time.time; rb.linearVelocity=Vector2.zero;
                if(NetworkManager.ServerTime.Time<AbilityEnds.Value) return;
                AbilityState.Value=0; AbilityEnds.Value=0;
                if(target!=null && target==abilityTarget && Time.time-previousObservationAt<=.2f && PlanWeaverRing(observedPosition) &&
                    MatchManager.Instance.MonsterEffects.Count<=253)
                {
                    bool emitted=false;
                    for(int i=0;i<3;i++) emitted|=MonsterWorld.Add(MonsterEffectKind.WebWarning,weaverRingPoints[i],weaverRingPoints[i],1.3f,.8f);
                    if(emitted) WebReleasedAt.Value=NetworkManager.ServerTime.Time;
                }
                nextWeaverDash=Mathf.Max(nextWeaverDash,Time.time+.8f); return;
            }
            if(target!=null)
            {
                float distance=Vector2.Distance(rb.position,observedPosition);
                if(Time.time>=nextWeaverDash && Time.time>=staggerUntil && distance>=1.5f && distance<=5 && WeaverPackCanAttack() &&
                    Physics2D.CircleCast(rb.position,worldRadius,(observedPosition-rb.position).normalized,Mathf.Min(distance,WeaverDashDistance),GameConfig.WallMask).collider==null &&
                    Vector2.Distance(home,rb.position+(observedPosition-rb.position).normalized*WeaverDashDistance)<LeashRadius)
                {
                    CancelMelee(); ChargeAim.Value=Mathf.Atan2(observedPosition.y-rb.position.y,observedPosition.x-rb.position.x)*Mathf.Rad2Deg;
                    rb.rotation=ChargeAim.Value; ChargeReach.Value=WeaverDashDistance; ChargeState.Value=1;
                    WeaverState.Value=(byte)WeaverPhase.DashWindup; WeaverStateEnds.Value=NetworkManager.ServerTime.Time+WeaverWindupSeconds;
                    rb.linearVelocity=Vector2.zero; return;
                }
                if(Time.time>=nextAbility && distance>2 && WeaverPackCanAttack() &&
                    MonsterWorld.CountNear(MonsterEffectKind.Web,observedPosition,12)+MonsterWorld.CountNear(MonsterEffectKind.WebWarning,observedPosition,12)==0 &&
                    PlanWeaverRing(observedPosition))
                {
                    abilityTarget=target; Prepare(observedPosition,.8); nextAbility=Time.time+6; return;
                }
                if(TryMelee(target)) return;
                MoveTo(observedPosition,chaseSpeed); return;
            }
            IdleDestination(out var destination,out var speed); MoveTo(destination,speed);
        }
    }
}
