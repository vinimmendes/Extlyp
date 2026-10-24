using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Match;
using CyberExtraction.Players;

namespace CyberExtraction.Enemies
{
    public partial class Monster
    {
        public NetworkVariable<double> MeleeEnds = new NetworkVariable<double>(0);
        public NetworkVariable<float> MeleeAim = new NetworkVariable<float>(0);
        public NetworkVariable<float> MeleeReach = new NetworkVariable<float>(0);
        const float PerceptionInterval=.125f, MeleeWarning=.3f, MeleeHalfAngle=65f;
        const int MeleeWarningSegments=64;
        PlayerInventory perceivedTarget, meleeTarget;
        Vector2 observedPosition, observedFacing, escapeDirection, previousObservation, observedVelocity;
        bool observedLit, returning, nearbyPlayers;
        float nextPerception, nextActivation, previousObservationAt, targetLockUntil, ignoreTargetsUntil;
        float searchUntil, nextSearchPoint, progressAt, recoveryUntil, lastMoveFailure;
        Vector2 searchOrigin, searchPoint, progressAnchor, recoveryPoint;
        int failedMoves, patrolSite=-1;
        float staggerUntil;
        void HandleStagger(CyberExtraction.Combat.Health source,int damage)
        {
            if(damage<=0) return;
            RevealFromDamage(damage);
            if(source.IsDead) return;
            if(Kind==MonsterKind.Weaver && SpiderPhase==WeaverPhase.DashWindup) RecoverWeaver();
            staggerUntil=Time.time+.1f; // Refresh, never add per pellet/impact.
            // Active charge owns velocity; damage must not stop or shorten it.
            if(ChargeState.Value!=2) rb.linearVelocity=Vector2.zero;
        }
        float LeashRadius => Kind==MonsterKind.Stalker ? 75 : Kind==MonsterKind.Listener ? 55 : 34;
        void InitializeBehaviour()
        {
            nextPerception=Time.time+Random.Range(0,PerceptionInterval);
            nextActivation=Time.time+Random.Range(0,.5f);
            progressAnchor=rb.position; progressAt=Time.time;
            if(Kind==MonsterKind.Stalker || (Kind==MonsterKind.Listener && Random.value<.35f))
            {
                float best=50;
                for(int i=0;i<MapBuilder.Sites.Length;i++)
                {
                    float distance=Vector2.Distance(home,MapBuilder.Sites[i]);
                    if(distance>8 && distance<best) { best=distance; patrolSite=i; }
                }
            }
        }
        bool HasNearbyPlayers()
        {
            if(Time.time<nextActivation) return nearbyPlayers;
            nextActivation=Time.time+.5f; nearbyPlayers=false;
            foreach(var inv in PlayerInventory.All)
                if(inv!=null && inv.IsAlive && (inv.transform.position-transform.position).sqrMagnitude<65*65)
                { nearbyPlayers=true; break; }
            return nearbyPlayers;
        }
        bool Perceives(PlayerInventory inv)
        {
            if(inv==null || !inv.IsSpawned || !inv.IsAlive) return false;
            Vector2 offset=(Vector2)inv.transform.position-rb.position;
            if(offset.sqrMagnitude>detectRadius*detectRadius || Vector2.Distance(home,inv.transform.position)>LeashRadius) return false;
            if(offset.sqrMagnitude>Mathf.Min(2,detectRadius)*Mathf.Min(2,detectRadius) &&
                Mathf.Abs(Mathf.DeltaAngle(rb.rotation,Mathf.Atan2(offset.y,offset.x)*Mathf.Rad2Deg))>70) return false;
            return Physics2D.Linecast(rb.position,inv.transform.position,GameConfig.VisionMask).collider==null;
        }
        Transform PerceptionTarget()
        {
            if(Time.time>=nextPerception)
            {
                nextPerception=Time.time+PerceptionInterval;
                PlayerInventory best=null; float bestScore=float.NegativeInfinity;
                if(!returning && Time.time>=ignoreTargetsUntil && !Disguised)
                foreach(var inv in PlayerInventory.All)
                {
                    if(!Perceives(inv)) continue;
                    float score=-Vector2.Distance(rb.position,inv.transform.position);
                    if(Kind==MonsterKind.Stalker)
                    {
                        int neighbors=0;
                        foreach(var other in PlayerInventory.All)
                            if(other!=inv && Perceives(other) && Vector2.Distance(other.transform.position,inv.transform.position)<8) neighbors++;
                        score-=neighbors*12+(Illuminated(inv.transform) ? 8 : 0);
                    }
                    if(inv==perceivedTarget) score+=Time.time<targetLockUntil ? 8 : 3;
                    if(score>bestScore) { best=inv; bestScore=score; }
                }
                if(best!=perceivedTarget) { targetLockUntil=Time.time+1; previousObservationAt=0; observedVelocity=Vector2.zero; }
                perceivedTarget=best;
                if(best!=null)
                {
                    observedPosition=best.transform.position; observedFacing=best.transform.right;
                    observedLit=Kind==MonsterKind.Stalker && Illuminated(best.transform);
                    if(previousObservationAt>0 && Time.time>previousObservationAt)
                    {
                        Vector2 delta=observedPosition-previousObservation;
                        float elapsed=Time.time-previousObservationAt;
                        observedVelocity=delta.sqrMagnitude>.0001f && delta.magnitude<=GameConfig.PlayerSpeed*elapsed*1.5f
                            ? Vector2.Lerp(observedVelocity,Vector2.ClampMagnitude(delta/elapsed,GameConfig.PlayerSpeed),.65f) : Vector2.zero;
                        if(delta.sqrMagnitude>.01f) escapeDirection=delta.normalized;
                    }
                    previousObservation=observedPosition; previousObservationAt=Time.time;
                    alertPos=observedPosition; alertUntil=Time.time+6; searchUntil=0;
                }
            }
            return perceivedTarget!=null && perceivedTarget.IsSpawned && perceivedTarget.IsAlive ? perceivedTarget.transform : null;
        }
        bool ReturnIfNeeded()
        {
            if(Vector2.Distance(rb.position,home)>LeashRadius) BeginReturn();
            if(!returning) return false;
            if(Vector2.Distance(rb.position,home)<3) { returning=false; ignoreTargetsUntil=Time.time+1; return false; }
            MoveTo(home,chaseSpeed*.7f); return true;
        }
        void BeginReturn()
        {
            returning=true; perceivedTarget=null; alertUntil=searchUntil=0;
            ChargeState.Value=0; ChargeReach.Value=0; chargeTarget=null; RestoreMonsterCollisions();
            CancelMelee(); AbilityState.Value=Kind==MonsterKind.Mimic ? (byte)2 : (byte)0;
            CancelWeaverCombat();
            AbilityEnds.Value=0;
        }
        Vector2 BoundToHome(Vector2 point) => home+Vector2.ClampMagnitude(point-home,LeashRadius-2);
        void IdleDestination(out Vector2 dest,out float speed)
        {
            speed=chaseSpeed*.7f;
            if(Time.time<alertUntil)
            {
                dest=BoundToHome(alertPos);
                if(Vector2.Distance(rb.position,dest)>.8f) return;
                alertUntil=0; searchOrigin=dest; searchUntil=Time.time+3; nextSearchPoint=0;
            }
            if(Time.time<searchUntil)
            {
                if(Time.time>=nextSearchPoint || Vector2.Distance(rb.position,searchPoint)<.6f)
                {
                    Vector2 direction=escapeDirection.sqrMagnitude>.1f ? escapeDirection : (Vector2)transform.right;
                    searchPoint=BoundToHome(searchOrigin+direction*2+Random.insideUnitCircle*1.5f);
                    nextSearchPoint=Time.time+1;
                }
                dest=searchPoint; return;
            }
            if(searchUntil>0) { searchUntil=0; BeginReturn(); dest=home; return; }
            speed=patrolSpeed;
            if(Time.time>=nextWander || Vector2.Distance(rb.position,wanderTarget)<.6f)
            {
                if(patrolSite>=0) wanderTarget=Vector2.Distance(rb.position,home)<5 ? MapBuilder.Sites[patrolSite] : home;
                else wanderTarget=home+Random.insideUnitCircle*4;
                nextWander=Time.time+(patrolSite>=0 ? 45 : Random.Range(3f,6f));
            }
            dest=BoundToHome(wanderTarget);
        }
        bool TryMelee(Transform target)
        {
            if(target==null || Time.time<nextAttack || Vector2.Distance(rb.position,observedPosition)>attackRange ||
                Physics2D.Linecast(rb.position,observedPosition,GameConfig.WallMask).collider!=null) return false;
            meleeTarget=target.GetComponent<PlayerInventory>();
            MeleeAim.Value=Mathf.Atan2(observedPosition.y-rb.position.y,observedPosition.x-rb.position.x)*Mathf.Rad2Deg;
            MeleeReach.Value=attackRange; MeleeEnds.Value=NetworkManager.ServerTime.Time+MeleeWarning;
            rb.rotation=MeleeAim.Value; rb.linearVelocity=Vector2.zero; return true;
        }
        bool StepMelee()
        {
            if(MeleeEnds.Value<=0) return false;
            rb.linearVelocity=Vector2.zero; rb.rotation=MeleeAim.Value; progressAt=Time.time;
            if(NetworkManager.ServerTime.Time<MeleeEnds.Value) return true;
            if(meleeTarget!=null && meleeTarget.IsSpawned && meleeTarget.IsAlive)
            {
                Vector2 offset=(Vector2)meleeTarget.transform.position-rb.position;
                if(offset.magnitude<=MeleeReach.Value && Mathf.Abs(Mathf.DeltaAngle(MeleeAim.Value,Mathf.Atan2(offset.y,offset.x)*Mathf.Rad2Deg))<=MeleeHalfAngle &&
                    MeleeObstacle(rb.position,meleeTarget.transform.position).collider==null && MeleeWarningContains(rb.position,offset))
                {
                    int damage=attackDamage;
                    if(Kind==MonsterKind.Stalker && Vector2.Dot(-offset.normalized,meleeTarget.transform.right)<-.5f) damage=30;
                    meleeTarget.GetComponent<CyberExtraction.Combat.Health>()?.ServerTakeDamage(damage,creatureName:DisplayName,origin:CyberExtraction.Combat.DamageOrigin.Creature(CyberExtraction.Combat.DamageOrigin.Identify(gameObject)));
                }
            }
            CancelMelee(); nextAttack=Time.time+attackCooldown; return true;
        }
        void CancelMelee() { MeleeEnds.Value=0; meleeTarget=null; }
        static RaycastHit2D MeleeObstacle(Vector2 origin,Vector2 point) => Physics2D.Linecast(origin,point,GameConfig.WallMask);
        Vector2 MeleeWarningPoint(Vector2 origin,int index)
        {
            float angle=(MeleeAim.Value+Mathf.Lerp(-MeleeHalfAngle,MeleeHalfAngle,(float)index/MeleeWarningSegments))*Mathf.Deg2Rad;
            Vector2 direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            float reach=MeleeReach.Value/Mathf.Cos(MeleeHalfAngle/MeleeWarningSegments*Mathf.Deg2Rad);
            var wall=MeleeObstacle(origin,origin+direction*reach);
            return direction*(wall.collider!=null ? Vector2.Distance(origin,wall.point) : reach);
        }
        bool MeleeWarningContains(Vector2 origin,Vector2 offset)
        {
            if(offset.sqrMagnitude<.00000001f) return true;
            float relative=Mathf.DeltaAngle(MeleeAim.Value,Mathf.Atan2(offset.y,offset.x)*Mathf.Rad2Deg);
            int index=Mathf.Clamp(Mathf.FloorToInt((relative+MeleeHalfAngle)/(2*MeleeHalfAngle)*MeleeWarningSegments),0,MeleeWarningSegments-1);
            Vector2 a=MeleeWarningPoint(origin,index), b=MeleeWarningPoint(origin,index+1);
            Vector2 edge=b-a, point=offset-a;
            // The same clipped triangle edge used by the warning bounds the impact.
            return edge.x*point.y-edge.y*point.x>=0;
        }
        bool CanStartCharge(Vector2 pos)
        {
            float distance=Vector2.Distance(pos,observedPosition);
            return distance>=5 && distance<=chargeSpeed*chargeDuration &&
                Physics2D.CircleCast(pos,worldRadius,(observedPosition-pos).normalized,distance,GameConfig.WallMask).collider==null;
        }
        bool TryTrackChargeAim(Vector2 pos,Vector2 point,out float aim)
        {
            aim=ChargeAim.Value;
            Vector2 direction=point-pos;
            if(direction.sqrMagnitude<=.0001f || Vector2.Distance(home,point)>LeashRadius ||
                Physics2D.Linecast(pos,point,GameConfig.VisionMask).collider!=null) return false;
            aim=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg; return true;
        }
        Vector2 Separate(Vector2 direction)
        {
            Vector2 push=Vector2.zero;
            foreach(var other in All)
            {
                if(other==null || other==this || !other.IsSpawned || other.Disguised) continue;
                Vector2 delta=rb.position-(Vector2)other.transform.position;
                float desired=worldRadius+other.worldRadius+.35f;
                if(delta.sqrMagnitude>=desired*desired) continue;
                if(delta.sqrMagnitude<.001f) delta=NetworkObjectId<other.NetworkObjectId ? Vector2.up : Vector2.down;
                push+=delta.normalized*(1-Mathf.Clamp01(delta.magnitude/desired));
            }
            return (direction.normalized+Vector2.ClampMagnitude(push,.6f)).normalized;
        }
        void MoveBehaviour(Vector2 dest,float speed)
        {
            // Only ordinary navigation pauses. Ability/mêlée/charge state machines still tick.
            if(Time.time<staggerUntil)
            { rb.linearVelocity=Vector2.zero; progressAt=Time.time; return; }
            if(Time.time<recoveryUntil) dest=recoveryPoint;
            bool travelling=Vector2.Distance(rb.position,dest)>1;
            if((rb.position-progressAnchor).sqrMagnitude>.16f || !travelling)
            { progressAnchor=rb.position; progressAt=Time.time; if(Time.time-lastMoveFailure>8) failedMoves=0; }
            if(travelling && Time.time-progressAt>2)
            {
                failedMoves++; lastMoveFailure=progressAt=Time.time; navigation=new MonsterNavigation(worldRadius);
                perceivedTarget=null; ignoreTargetsUntil=Time.time+2;
                if(failedMoves>=2) BeginReturn();
                Vector2 side=new Vector2(-transform.right.y,transform.right.x)*(Random.value<.5f ? -1 : 1);
                recoveryPoint=BoundToHome(rb.position+side*2); recoveryUntil=Time.time+1;
                dest=recoveryPoint;
            }
            if(!navigation.TryGetWaypoint(rb.position,dest,out var waypoint)) { rb.linearVelocity=Vector2.zero; return; }
            Vector2 delta=waypoint-rb.position;
            Vector2 direction=Separate(delta);
            float step=Mathf.Min(speed,delta.magnitude/Time.fixedDeltaTime);
            if(Physics2D.CircleCast(rb.position,worldRadius,direction,step*Time.fixedDeltaTime,GameConfig.WallMask).collider!=null) direction=delta.normalized;
            rb.linearVelocity=direction*step;
            if(Kind==MonsterKind.Acid && perceivedTarget!=null)
            {
                // Backpedal without turning the perception cone away from the observed target.
                Vector2 aim=observedPosition-rb.position;
                if(aim.sqrMagnitude>.001f) rb.rotation=Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg;
            }
            else if(delta.sqrMagnitude>.001f) rb.rotation=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
        }
    }
}
