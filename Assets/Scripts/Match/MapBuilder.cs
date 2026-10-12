using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Core;
namespace CyberExtraction.Match
{
    public enum ExpeditionMap : byte { Test, Large }
    public partial class MapBuilder : MonoBehaviour
    {
        public static MapBuilder Instance { get; private set; }
        public static ExpeditionMap Current { get; private set; } = ExpeditionMap.Test;
        public static int LayoutSeed { get; private set; } = 43127;
        public static float Width => Current==ExpeditionMap.Large ? 400 : 30;
        public static float Height => Current==ExpeditionMap.Large ? 600 : 46;
        static readonly Vector2[] largeSpawns={ new Vector2(-65,235),new Vector2(145,175),new Vector2(-155,75),new Vector2(155,-140),new Vector2(-70,-245) };
        static readonly Vector2[] largeExits=MakeExits();
        static readonly Vector2[] smallSpawns={new Vector2(0,18),new Vector2(0,-18)};
        static readonly Vector2[] smallExits={new Vector2(11.5f,0)};
        static readonly Vector2[] smallSites={new Vector2(-11,9),new Vector2(11,9),new Vector2(-11,-9),new Vector2(11,-9)};
        static readonly Vector2[] smallLoot={new Vector2(-11,9),new Vector2(11,9),new Vector2(-11,-9),new Vector2(11,-9),new Vector2(0,17),new Vector2(0,-17),new Vector2(-11,15),new Vector2(11,-15),new Vector2(-10,0),new Vector2(10,6),new Vector2(-11,-15),new Vector2(11,15)};
        static readonly Vector2[] largeSites=MakeSites();
        static Vector2[] largeLoot=MakeLoot();
        public static Vector2[] SpawnPoints => Current==ExpeditionMap.Large ? largeSpawns : smallSpawns;
        public static Vector2[] ExtractionPoints => Current==ExpeditionMap.Large ? largeExits : smallExits;
        public static Vector2[] Sites => Current==ExpeditionMap.Large ? largeSites : smallSites;
        public static Vector2[] LootPoints => Current==ExpeditionMap.Large ? largeLoot : smallLoot;
        public static Vector2 HouseASpawn => SpawnPoints[0];
        public static Vector2 HouseBSpawn => SpawnPoints[1];
        public static Vector2 MonsterSpawn => Current==ExpeditionMap.Large ? new Vector2(15,15) : Vector2.zero;
        public static Vector2 ExtractionCenter => NearestExtraction(Vector2.zero);
        public static Vector2 ParticipantSpawn(int slot)
        {
            if(Current==ExpeditionMap.Large) return largeSpawns[Mathf.Clamp(slot,0,largeSpawns.Length-1)];
            int first=(GameConfig.MatchCapacity+1)/2;
            var house=slot<first ? smallSpawns[0] : smallSpawns[1]; int local=slot<first ? slot : slot-first;
            return house+new Vector2((local%3-1)*1.5f,local<3 ? .8f : -.8f);
        }
        public static float ParticipantFacing(int slot)
        {
            if(Current==ExpeditionMap.Test) return slot<(GameConfig.MatchCapacity+1)/2 ? -90 : 90;
            Vector2 direction=LargeMapPlan.Sectors[Mathf.Clamp(slot,0,4)].Center-ParticipantSpawn(slot);
            return Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
        }
        public static Vector2 NearestExtraction(Vector2 point)
        {
            if(Current==ExpeditionMap.Test) return smallExits[0];
            int id=LargeMapPlan.BestExit(point,MatchManager.Instance!=null ? MatchManager.Instance.ActiveExtractions.Value : (ushort)0);
            return id>=0 ? largeExits[id] : point;
        }
        static Vector2[] MakeExits() { var points=new Vector2[LargeMapPlan.Exits.Length]; for(int i=0;i<points.Length;i++) points[i]=LargeMapPlan.Exits[i].Position; return points; }
        static Vector2[] MakeSites()
        {
            var sites=new List<Vector2>();
            var offsets=new[]{new Vector2(-25,-22),new Vector2(20,-20),new Vector2(-24,15),new Vector2(21,16)};
            for(int sector=0;sector<LargeMapPlan.Sectors.Length;sector++)
                for(int i=0;i<offsets.Length;i++) sites.Add(LargeMapPlan.Sectors[sector].Center+offsets[i]+new Vector2((sector%3-1)*3,(i%2)*3));
            return sites.ToArray();
        }
        static Vector2[] MakeLoot() { var points=new List<Vector2>(); for(int i=0;i<largeSites.Length;i++) { points.Add(RuinPoint(i,new Vector2(0,-3))); points.Add(RuinPoint(i,new Vector2(4,-2))); } foreach(var spawn in largeSpawns) points.Add(spawn+new Vector2(0,-12)); return points.ToArray(); }
        GameObject world;
        void Awake() { Instance=this; Current=ExpeditionMap.Test; LayoutSeed=43127; Rebuild(); }
        public static void Configure(ExpeditionMap map,int layoutSeed=43127)
        {
            if(map!=ExpeditionMap.Test && map!=ExpeditionMap.Large) return;
            bool changed=Current!=map || LayoutSeed!=layoutSeed; Current=map; LayoutSeed=layoutSeed;
            if(Instance!=null && (changed || Instance.world==null)) Instance.Rebuild();
        }
        void Rebuild()
        {
            FurniturePoints.Clear();
            largeLoot=MakeLoot();
            if(world!=null) { world.SetActive(false); if(Application.isPlaying) Destroy(world); else DestroyImmediate(world); }
            world=new GameObject(Current==ExpeditionMap.Large ? "LargeMap" : "QuickTestMap"); world.transform.SetParent(transform,false);
            Block("Ground",Vector2.zero,new Vector2(Width+2,Height+2),new Color(.12f,.16f,.12f),-10);
            if(Current==ExpeditionMap.Test)
            {
            Block("North",new Vector2(0,Height/2+.5f),new Vector2(Width+2,1),Color.gray,-5,true);
            Block("South",new Vector2(0,-Height/2-.5f),new Vector2(Width+2,1),Color.gray,-5,true);
            Block("West",new Vector2(-Width/2-.5f,0),new Vector2(1,Height),Color.gray,-5,true);
            Block("East",new Vector2(Width/2+.5f,0),new Vector2(1,Height),Color.gray,-5,true);
            }
            if(Current==ExpeditionMap.Test) BuildTest(); else BuildLarge();
            if(Current==ExpeditionMap.Test) foreach(var exit in ExtractionPoints)
            {
                var go=Block("Extraction",exit,new Vector2(5,3.5f),new Color(.2f,.8f,.4f,.35f),-7);
                go.AddComponent<BoxCollider2D>().isTrigger=true; go.AddComponent<ExtractionZone>();
            }
            Physics2D.SyncTransforms();
        }
        void BuildTest()
        {
            House(smallSpawns[0],-1); House(smallSpawns[1],1);
            Block("PenLeft",new Vector2(-4,0),new Vector2(.6f,8),Color.gray,-5,true);
            Block("PenRight",new Vector2(4,0),new Vector2(.6f,8),Color.gray,-5,true);
            Block("Crate1",new Vector2(-7,6),new Vector2(2,2),new Color(.45f,.36f,.26f),-4,true);
            Block("Crate2",new Vector2(7,-6),new Vector2(2,2),new Color(.45f,.36f,.26f),-4,true);
            Block("Crate3",new Vector2(-7,14),new Vector2(2,2),new Color(.45f,.36f,.26f),-4,true);
            Block("Crate4",new Vector2(7,-14),new Vector2(2,2),new Color(.45f,.36f,.26f),-4,true);
        }
        void BuildLarge()
        {
            BuildPlannedLarge();
        }
        void House(Vector2 c,float side)
        {
            const float width=8,height=5,thickness=.6f,gap=2;
            Block("HouseFloor",c,new Vector2(width,height),new Color(.22f,.22f,.19f),-8);
            Block("HouseBack",c+new Vector2(0,-side*height/2),new Vector2(width+thickness,thickness),Color.gray,-5,true);
            Block("HouseLeft",c+new Vector2(-width/2,0),new Vector2(thickness,height),Color.gray,-5,true);
            Block("HouseRight",c+new Vector2(width/2,0),new Vector2(thickness,height),Color.gray,-5,true);
            float segment=(width+thickness-gap)/2, offset=(gap+segment)/2;
            Block("DoorL",c+new Vector2(-offset,side*height/2),new Vector2(segment,thickness),Color.gray,-5,true);
            Block("DoorR",c+new Vector2(offset,side*height/2),new Vector2(segment,thickness),Color.gray,-5,true);
        }
        GameObject Block(string name,Vector2 center,Vector2 size,Color color,int order,bool solid=false)
        {
            var go=new GameObject(name); go.transform.SetParent(world.transform,false); go.transform.position=center; go.transform.localScale=new Vector3(size.x,size.y,1);
            SpriteFactory.Setup(go.AddComponent<SpriteRenderer>(),SpriteFactory.Square,color,order);
            if(solid)
            {
                go.layer=name.Contains("Crate") || name=="Cover" ? GameConfig.LowObstacleLayer : GameConfig.WallLayer;
                go.AddComponent<BoxCollider2D>();
            }
            return go;
        }
        void OnDestroy() { if(Instance==this) Instance=null; }
    }
}
