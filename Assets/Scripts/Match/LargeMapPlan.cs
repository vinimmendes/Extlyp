using System;
using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Items;

namespace CyberExtraction.Match
{
    public enum RouteCondition : byte { Free, Blocked, Horde, Narrow, Gated }
    public sealed class ForestRoute
    {
        public int Id, From, To;
        public float Width, Length;
        public Vector2[] Points;
        public RouteCondition Condition;
        public ItemId RequiredItem;
        public Vector2 Gate => Points[2];
        public float Angle => Mathf.Atan2(Points[3].y-Points[1].y,Points[3].x-Points[1].x)*Mathf.Rad2Deg;
        public bool Accessible => Condition!=RouteCondition.Blocked &&
            (Condition!=RouteCondition.Gated || MatchManager.Instance!=null && (MatchManager.Instance.OpenedPassages.Value & (1u<<Id))!=0);
        public string Action => RequiredItem==ItemId.BoltCutters ? "Cortar cerca" : RequiredItem==ItemId.Crowbar ? "Remover tábuas" :
            RequiredItem==ItemId.HandSaw ? "Cortar tronco" : "Destrancar passagem";
    }
    public readonly struct ForestSector
    {
        public readonly string Name;
        public readonly Vector2 Center;
        public ForestSector(string name,float x,float y) { Name=name; Center=new Vector2(x,y); }
    }
    public readonly struct ForestExit
    {
        public readonly string Name;
        public readonly Vector2 Position;
        public readonly int Sector;
        public readonly bool Transport;
        public ForestExit(string name,float x,float y,int sector,bool transport)
        { Name=name; Position=new Vector2(x,y); Sector=sector; Transport=transport; }
    }

    // Fixed geography, deterministic conditions. Only the server changes passage/exit state.
    public static class LargeMapPlan
    {
        public static readonly ForestSector[] Sectors={new ForestSector("Serraria",-40,190),new ForestSector("Depósito",120,130),
            new ForestSector("Vilarejo",-120,35),new ForestSector("Lago",110,-110),new ForestSector("Fazenda",-45,-200),new ForestSector("Clareira",0,0)};
        public static readonly ForestExit[] Exits={new ForestExit("Estrada das Toras",-50,275,0,false),new ForestExit("Caminhão Madeireiro",30,260,0,true),
            new ForestExit("Vagonete de Manutenção",175,165,1,true),new ForestExit("Portão de Serviço",175,95,1,false),
            new ForestExit("Ponte Velha",-175,60,2,false),new ForestExit("Trilha dos Peregrinos",-175,-10,2,false),
            new ForestExit("Barco do Ancoradouro",170,-100,3,true),new ForestExit("Canoa dos Juncos",145,-190,3,true),
            new ForestExit("Carroça do Portão",-100,-270,4,true),new ForestExit("Passagem do Gado",-10,-270,4,false)};
        public static readonly List<ForestRoute> Routes=new List<ForestRoute>();
        static readonly int[,] Connections={{0,1},{0,2},{1,3},{2,4},{4,3},{0,5},{1,5},{2,5},{3,5},{4,5}};
        public static int GeometryVersion { get; private set; }
        static readonly float[][] distanceCache=new float[6][];
        static readonly ForestRoute[][] routeCache=new ForestRoute[6][];
        static int cachedVersion=-1;
        public static void PassageChanged() { GeometryVersion++; }
        public static void Build(int seed)
        {
            Routes.Clear(); GeometryVersion++;
            var random=new System.Random(seed);
            for(int link=0;link<Connections.GetLength(0);link++)
            {
                int clear=random.Next(2), count=link==0 || link==3 ? 3 : 2;
                for(int branch=0;branch<count;branch++)
                {
                    int a=Connections[link,0],b=Connections[link,1];
                    Vector2 start=Sectors[a].Center,end=Sectors[b].Center,delta=end-start;
                    Vector2 normal=new Vector2(-delta.y,delta.x).normalized;
                    float offset=branch==0 ? -24 : branch==1 ? 24 : 0;
                    var points=new[]{start,start+delta*.25f+normal*offset, start+delta*.5f+normal*offset,
                        start+delta*.75f+normal*offset,end};
                    RouteCondition condition=RouteCondition.Free;
                    if(branch!=clear)
                    {
                        int roll=random.Next(100);
                        condition=branch==2 ? RouteCondition.Gated : roll<30 ? RouteCondition.Blocked : roll<55 ? RouteCondition.Horde :
                            roll<80 ? RouteCondition.Narrow : RouteCondition.Gated;
                    }
                    float length=0; for(int p=1;p<points.Length;p++) length+=Vector2.Distance(points[p-1],points[p]);
                    ItemId[] tools={ItemId.BoltCutters,ItemId.Crowbar,ItemId.HandSaw,ItemId.ServiceKey,ItemId.DepotKey};
                    ItemId required=tools[(link+branch)%tools.Length];
                    if(required==ItemId.DepotKey && a!=1 && b!=1) required=ItemId.ServiceKey;
                    Routes.Add(new ForestRoute {Id=Routes.Count,From=a,To=b,Width=branch==0 ? 10 : 7,Points=points,
                        Length=length,Condition=condition,RequiredItem=required});
                }
            }
        }
        public static int SectorAt(Vector2 point)
        {
            int best=0; for(int i=1;i<Sectors.Length;i++) if((point-Sectors[i].Center).sqrMagnitude<(point-Sectors[best].Center).sqrMagnitude) best=i;
            return best;
        }
        static void Distances(int start,out float[] cost,out ForestRoute[] previous)
        {
            if(cachedVersion!=GeometryVersion)
            {
                cachedVersion=GeometryVersion;
                Array.Clear(distanceCache,0,distanceCache.Length); Array.Clear(routeCache,0,routeCache.Length);
            }
            if(distanceCache[start]!=null) { cost=distanceCache[start]; previous=routeCache[start]; return; }
            cost=new float[Sectors.Length]; previous=new ForestRoute[Sectors.Length]; var used=new bool[Sectors.Length];
            for(int i=0;i<cost.Length;i++) cost[i]=float.PositiveInfinity; cost[start]=0;
            for(int step=0;step<cost.Length;step++)
            {
                int current=-1;
                for(int i=0;i<cost.Length;i++) if(!used[i] && (current<0 || cost[i]<cost[current])) current=i;
                if(current<0 || float.IsInfinity(cost[current])) break;
                used[current]=true;
                foreach(var route in Routes)
                {
                    if(!route.Accessible || route.From!=current && route.To!=current) continue;
                    int next=route.From==current ? route.To : route.From;
                    float candidate=cost[current]+route.Length+(route.Condition==RouteCondition.Horde ? 35 : 0);
                    if(candidate>=cost[next]) continue; cost[next]=candidate; previous[next]=route;
                }
            }
            distanceCache[start]=cost; routeCache[start]=previous;
        }
        public static float TravelDistance(Vector2 from,int exit)
        {
            int start=SectorAt(from); Distances(start,out var cost,out _);
            return Vector2.Distance(from,Sectors[start].Center)+cost[Exits[exit].Sector]+Vector2.Distance(Sectors[Exits[exit].Sector].Center,Exits[exit].Position);
        }
        public static int BestExit(Vector2 from,ushort mask)
        {
            int best=-1; float distance=float.PositiveInfinity;
            for(int i=0;i<Exits.Length;i++) if((mask&(1<<i))!=0)
            { float candidate=TravelDistance(from,i); if(candidate<distance) { distance=candidate; best=i; } }
            return best;
        }
        public static ushort ChooseExits(int seed,Vector2[] spawns)
        {
            var valid=new List<ushort>();
            for(int mask=1;mask<(1<<Exits.Length);mask++)
            {
                int count=0; var sectors=new bool[5];
                for(int i=0;i<Exits.Length;i++) if((mask&(1<<i))!=0) { count++; sectors[Exits[i].Sector]=true; }
                if(count!=4) continue;
                int occupied=0; foreach(bool sector in sectors) if(sector) occupied++;
                if(occupied<3) continue;
                bool fair=true;
                foreach(var spawn in spawns)
                { int exit=BestExit(spawn,(ushort)mask); if(exit<0 || TravelDistance(spawn,exit)>360) { fair=false; break; } }
                if(fair) valid.Add((ushort)mask);
            }
            if(valid.Count==0) throw new InvalidOperationException("Nenhuma combinação de extrações atende às rotas de retorno.");
            return valid[new System.Random(seed^73451).Next(valid.Count)];
        }
        public static bool TryWaypoint(Vector2 from,Vector2 goal,out Vector2 waypoint)
        {
            waypoint=goal; int start=SectorAt(from),end=SectorAt(goal);
            ForestRoute first=null; bool reverse=false;
            if(start==end)
            {
                if(Vector2.Distance(from,Sectors[end].Center)<50) return false;
                float distance=25;
                foreach(var route in Routes)
                {
                    if(!route.Accessible || route.From!=end && route.To!=end) continue;
                    for(int p=1;p<route.Points.Length;p++)
                    {
                        float d=SegmentDistance(from,route.Points[p-1],route.Points[p]);
                        if(d<distance) { distance=d; first=route; }
                    }
                }
                if(first==null) return false;
                reverse=first.From==end;
            }
            else
            {
                Distances(start,out var cost,out var previous); if(float.IsInfinity(cost[end])) return false;
                int current=end;
                while(current!=start)
                {
                    first=previous[current]; if(first==null) return false;
                    current=first.From==current ? first.To : first.From;
                }
                reverse=first.To==start;
            }
            Vector2[] points=first.Points;
            int nearestSegment=0; float best=float.PositiveInfinity;
            for(int i=0;i<points.Length-1;i++)
            {
                Vector2 a=points[reverse ? points.Length-1-i : i],b=points[reverse ? points.Length-2-i : i+1];
                float distance=SegmentDistance(from,a,b);
                if(distance<best) { best=distance; nearestSegment=i; }
            }
            waypoint=points[reverse ? points.Length-2-nearestSegment : nearestSegment+1];
            if(Vector2.Distance(from,waypoint)<3 && nearestSegment<points.Length-2)
                waypoint=points[reverse ? points.Length-3-nearestSegment : nearestSegment+2];
            return true;
        }
        public static float SegmentDistance(Vector2 point,Vector2 a,Vector2 b)
        { Vector2 delta=b-a; return Vector2.Distance(point,a+delta*Mathf.Clamp01(Vector2.Dot(point-a,delta)/Mathf.Max(.001f,delta.sqrMagnitude))); }
    }
}
