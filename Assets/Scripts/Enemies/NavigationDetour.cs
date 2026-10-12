using System;
using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Core;
namespace CyberExtraction.Enemies
{
    // Rare fallback for obstacles longer than the local A* window. Nodes are clearance
    // corners of solid walls; only a bounded set of nearest visible edges is expanded.
    public static class NavigationDetour
    {
        public static bool Find(Vector2 from,Vector2 goal,float radius,List<Vector2> output,Func<Vector2,Vector2,bool> clear)
        {
            var nodes=new List<Vector2> { from,goal };
            foreach(var wall in UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
            {
                if(!wall.enabled || wall.isTrigger || (GameConfig.WallMask&(1<<wall.gameObject.layer))==0) continue;
                var b=wall.bounds; float margin=radius+.08f;
                foreach(var point in new[] { new Vector2(b.min.x-margin,b.min.y-margin),new Vector2(b.min.x-margin,b.max.y+margin),new Vector2(b.max.x+margin,b.min.y-margin),new Vector2(b.max.x+margin,b.max.y+margin) })
                {
                    if(Physics2D.OverlapCircle(point,radius,GameConfig.WallMask)!=null) continue;
                    if(nodes.Count<1024) nodes.Add(point);
                }
            }
            int count=nodes.Count; var cost=new float[count]; var parent=new int[count]; var closed=new bool[count];
            var nearest=new int[16]; var distance=new float[16];
            for(int i=0;i<count;i++) { cost[i]=float.PositiveInfinity; parent[i]=-1; } cost[0]=0;
            for(int iteration=0;iteration<Mathf.Min(count,512);iteration++)
            {
                int current=-1; float score=float.PositiveInfinity;
                for(int i=0;i<count;i++) { float f=cost[i]+Vector2.Distance(nodes[i],goal); if(!closed[i] && f<score) { score=f; current=i; } }
                if(current<0) return false;
                if(current==1)
                {
                    output.Clear(); for(int n=1;n>0;n=parent[n]) output.Add(nodes[n]); output.Reverse(); return output.Count>0;
                }
                closed[current]=true;
                if(clear(nodes[current],goal)) { float candidate=cost[current]+Vector2.Distance(nodes[current],goal); if(candidate<cost[1]) { cost[1]=candidate; parent[1]=current; } }
                for(int j=0;j<nearest.Length;j++) { nearest[j]=-1; distance[j]=float.PositiveInfinity; }
                for(int i=2;i<count;i++)
                {
                    if(closed[i]) continue; float d=(nodes[i]-nodes[current]).sqrMagnitude;
                    for(int j=0;j<nearest.Length;j++) if(d<distance[j])
                    { for(int k=nearest.Length-1;k>j;k--) { nearest[k]=nearest[k-1]; distance[k]=distance[k-1]; } nearest[j]=i; distance[j]=d; break; }
                }
                foreach(int neighbor in nearest)
                {
                    if(neighbor<0 || !clear(nodes[current],nodes[neighbor])) continue;
                    float candidate=cost[current]+Vector2.Distance(nodes[current],nodes[neighbor]);
                    if(candidate<cost[neighbor]) { cost[neighbor]=candidate; parent[neighbor]=current; }
                }
            }
            return false;
        }
    }
}
