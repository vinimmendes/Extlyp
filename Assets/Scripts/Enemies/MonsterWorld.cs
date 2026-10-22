using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Match;
using CyberExtraction.Core;
using CyberExtraction.Players;
using CyberExtraction.Combat;
using CyberExtraction.UI;
using CyberExtraction.Bots;

namespace CyberExtraction.Enemies
{
    public enum MonsterEffectKind : byte { WebShot, AcidShot, Web, AcidPool, Corpse, Shout, WebWarning }
    [Serializable]
    public struct MonsterEffect : INetworkSerializable, IEquatable<MonsterEffect>
    {
        public int Id;
        public MonsterEffectKind Kind;
        public Vector2 From, To;
        public float Radius;
        public double Start, End;
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        { s.SerializeValue(ref Id); s.SerializeValue(ref Kind); s.SerializeValue(ref From); s.SerializeValue(ref To); s.SerializeValue(ref Radius); s.SerializeValue(ref Start); s.SerializeValue(ref End); }
        public bool Equals(MonsterEffect other) => Id==other.Id && Kind==other.Kind && From==other.From && To==other.To && Radius==other.Radius && Start==other.Start && End==other.End;
    }

    // Shared match-owned effects remain alive when their casting monster dies.
    public static class MonsterWorld
    {
        static int nextId;
        static double lastTick, nextAcidDamage;
        static readonly Dictionary<int,GameObject> visuals=new Dictionary<int,GameObject>();
        static readonly List<int> stale=new List<int>();
        static MatchManager Manager => MatchManager.Instance;
        public static bool Add(MonsterEffectKind kind,Vector2 from,Vector2 to,float radius,float seconds)
        {
            var m=Manager; if(m==null || !m.IsServer || m.CurrentState!=MatchState.Running) return false;
            // Bound long-lived traps/corpses to keep replication finite in long sessions.
            if(m.MonsterEffects.Count>=256) return false;
            double now=m.NetworkManager.ServerTime.Time;
            m.MonsterEffects.Add(new MonsterEffect {Id=++nextId,Kind=kind,From=from,To=to,Radius=radius,Start=now,End=now+seconds});
            return true;
        }
        public static void AddCorpse(Vector2 point) { Add(MonsterEffectKind.Corpse,point,point,.6f,45); }
        public static int CountNear(MonsterEffectKind kind,Vector2 point,float radius)
        { int count=0; if(Manager==null) return count; foreach(var e in Manager.MonsterEffects) if(e.Kind==kind && Vector2.Distance(point,e.To)<radius) count++; return count; }
        public static bool FindCorpse(Vector2 point,float radius,out MonsterEffect corpse)
        {
            corpse=default; bool found=false; if(Manager==null) return false;
            foreach(var e in Manager.MonsterEffects) if(e.Kind==MonsterEffectKind.Corpse && Vector2.Distance(point,e.To)<radius)
            { radius=Vector2.Distance(point,e.To); corpse=e; found=true; } return found;
        }
        public static bool ConsumeCorpse(int id,Vector2 point)
        {
            if(Manager==null || !Manager.IsServer) return false;
            for(int i=0;i<Manager.MonsterEffects.Count;i++)
                if(Manager.MonsterEffects[i].Id==id && Manager.MonsterEffects[i].Kind==MonsterEffectKind.Corpse && Vector2.Distance(point,Manager.MonsterEffects[i].To)<1.5f)
                { Manager.MonsterEffects.RemoveAt(i); return true; }
            return false;
        }
        static Vector2 Position(MonsterEffect e,double now) => Vector2.Lerp(e.From,e.To,(float)((now-e.Start)/Math.Max(.001,e.End-e.Start)));
        public static void ServerTick(MatchManager m)
        {
            double now=m.NetworkManager.ServerTime.Time;
            if(now-lastTick<.1) return;
            double previous=lastTick; lastTick=now;
            bool acidTick=now>=nextAcidDamage; if(acidTick) nextAcidDamage=now+.5;
            for(int i=m.MonsterEffects.Count-1;i>=0;i--)
            {
                var e=m.MonsterEffects[i];
                if(e.Kind==MonsterEffectKind.WebWarning)
                {
                    if(now>=e.End)
                    {
                        e.Kind=MonsterEffectKind.Web; e.Start=now; e.End=now+8;
                        m.MonsterEffects[i]=e;
                    }
                    continue;
                }
                bool shot=e.Kind==MonsterEffectKind.WebShot || e.Kind==MonsterEffectKind.AcidShot;
                if(shot)
                {
                    Vector2 from=Position(e,Math.Max(previous,e.Start)),to=Position(e,now),dir=to-from;
                    bool impact=now>=e.End; Vector2 impactPoint=to;
                    if(dir.sqrMagnitude>.00001f)
                    {
                        foreach(var hit in Physics2D.CircleCastAll(from,e.Radius,dir.normalized,dir.magnitude))
                        {
                            if(hit.collider.isTrigger) continue;
                            var victim=hit.collider.GetComponentInParent<PlayerInventory>();
                            bool wall=(GameConfig.WallMask & (1<<hit.collider.gameObject.layer))!=0;
                            if(!wall && (victim==null || !victim.IsAlive)) continue;
                            impact=true; impactPoint=hit.centroid;
                            if(victim!=null && e.Kind==MonsterEffectKind.AcidShot) victim.GetComponent<Health>().ServerTakeDamage(10,null,DamageKind.Environmental,"Corrosivo");
                            break;
                        }
                    }
                    if(impact)
                    {
                        e.Kind=e.Kind==MonsterEffectKind.WebShot ? MonsterEffectKind.Web : MonsterEffectKind.AcidPool;
                        e.From=e.To=impactPoint; e.Radius=e.Kind==MonsterEffectKind.Web ? 1.3f : 2.3f;
                        e.Start=now; e.End=now+(e.Kind==MonsterEffectKind.Web ? 12 : 8); m.MonsterEffects[i]=e;
                    }
                    continue;
                }
                if(now>=e.End) { m.MonsterEffects.RemoveAt(i); continue; }
                if(e.Kind!=MonsterEffectKind.Web && e.Kind!=MonsterEffectKind.AcidPool) continue;
                foreach(var inv in PlayerInventory.All)
                {
                    if(inv==null || !inv.IsAlive || Vector2.Distance(inv.transform.position,e.To)>e.Radius+.35f ||
                        Physics2D.Linecast(e.To,inv.transform.position,GameConfig.WallMask).collider!=null) continue;
                    if(e.Kind==MonsterEffectKind.Web) inv.ServerSlowUntil=Mathf.Max(inv.ServerSlowUntil,Time.time+1);
                    else if(acidTick) inv.GetComponent<Health>().ServerTakeDamage(4,null,DamageKind.Environmental,"Corrosivo");
                }
            }
        }
        // Web traps can be shot; the first trap in front of a wall/body absorbs one bullet.
        public static bool ShootWeb(Vector2 origin,Vector2 direction,float distance,out Vector2 impact)
        {
            impact=default; var m=Manager; if(m==null || !m.IsServer) return false;
            int index=-1; float nearest=distance;
            for(int i=0;i<m.MonsterEffects.Count;i++)
            {
                var e=m.MonsterEffects[i]; if(e.Kind!=MonsterEffectKind.Web && e.Kind!=MonsterEffectKind.WebWarning) continue;
                Vector2 delta=e.To-origin; float along=Vector2.Dot(delta,direction);
                float perpendicular=delta.sqrMagnitude-along*along;
                if(perpendicular>e.Radius*e.Radius) continue;
                float reach=Mathf.Sqrt(Mathf.Max(0,e.Radius*e.Radius-perpendicular));
                if(along+reach<0) continue;
                float hit=Mathf.Max(0,along-reach);
                if(hit<=nearest) { nearest=hit; index=i; }
            }
            if(index<0) return false;
            impact=origin+direction*nearest; m.MonsterEffects.RemoveAt(index); return true;
        }
        public static void Draw(MatchManager m)
        {
            if(!m.IsClient) return;
            var local=InventoryWindow.Local(); double now=m.NetworkManager.ServerTime.Time;
            stale.Clear(); foreach(var pair in visuals) stale.Add(pair.Key);
            foreach(var e in m.VisibleEffects)
            {
                stale.Remove(e.Id);
                if(!visuals.TryGetValue(e.Id,out var go))
                {
                    go=new GameObject("MonsterEffect_"+e.Kind); visuals.Add(e.Id,go);
                    go.AddComponent<SpriteRenderer>();
                }
                bool shot=e.Kind==MonsterEffectKind.WebShot || e.Kind==MonsterEffectKind.AcidShot;
                Vector2 point=shot ? Position(e,now) : e.To;
                go.transform.position=point;
                bool visible=local!=null && BotController.InVision(local.transform.position,local.transform.eulerAngles.z,point,local) &&
                    Physics2D.Linecast(local.transform.position,point,GameConfig.VisionMask).collider==null;
                go.SetActive(visible);
                var sr=go.GetComponent<SpriteRenderer>();
                Color tint=e.Kind==MonsterEffectKind.Corpse ? new Color(.4f,.08f,.09f) : e.Kind==MonsterEffectKind.Shout ? new Color(1,.2f,.5f,.2f) :
                    e.Kind==MonsterEffectKind.Web || e.Kind==MonsterEffectKind.WebShot ? new Color(.8f,.7f,1,.65f) : new Color(.45f,.9f,.05f,.65f);
                bool warning=e.Kind==MonsterEffectKind.WebWarning;
                bool groundHazard=warning || e.Kind==MonsterEffectKind.Web || e.Kind==MonsterEffectKind.AcidPool;
                var groundColor=warning ? new Color(1,.82f,.52f,.35f+.3f*(float)Mathf.Clamp01((float)((now-e.Start)/.8))) : Color.white;
                SpriteFactory.Setup(sr,groundHazard ? MonsterHazardArt.Get(warning ? MonsterEffectKind.Web : e.Kind) : SpriteFactory.Circle,groundHazard ? groundColor : tint,shot ? 7 : 2);
                float size=shot ? .4f : e.Radius*2;
                if(e.Kind==MonsterEffectKind.Shout) size *= (float)Mathf.Clamp01((float)(now-e.Start));
                go.transform.localScale=Vector3.one*size;
            }
            foreach(int id in stale) { UnityEngine.Object.Destroy(visuals[id]); visuals.Remove(id); }
        }
        public static void ClearVisuals()
        { foreach(var go in visuals.Values) if(go!=null) UnityEngine.Object.Destroy(go); visuals.Clear(); lastTick=nextAcidDamage=0; }
    }
}
