using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Core;
namespace CyberExtraction.Match
{
    public enum ExpeditionMap : byte { Test=0, Large=1, Dungeon=2 }
    public partial class MapBuilder : MonoBehaviour
    {
        [SerializeField, Tooltip("Piloto visual da serraria. Desative antes de iniciar a partida para usar o mapa anterior.")]
        bool sawmillTerrainPilot=true;
        readonly List<TerrainRoad> terrainRoads=new List<TerrainRoad>();
        public static MapBuilder Instance { get; private set; }
        public static ExpeditionMap Current { get; private set; } = ExpeditionMap.Test;
        public static int LayoutSeed { get; private set; } = 43127;
        public static int CapacityFor(ExpeditionMap map) => map==ExpeditionMap.Dungeon ? 8 : map==ExpeditionMap.Test ? 2 : GameConfig.MatchCapacity;
        public static int Capacity => CapacityFor(Current);
        public static string MapName(ExpeditionMap map) => map==ExpeditionMap.Dungeon ? "Catacumbas dos Afogados" : map==ExpeditionMap.Large ? "Floresta Velha" : "Área de testes";
        public static float Width => Current==ExpeditionMap.Dungeon ? DungeonLayout.GridWidth*DungeonLayout.CellSize : Current==ExpeditionMap.Large ? 400 : 30;
        public static float Height => Current==ExpeditionMap.Dungeon ? DungeonLayout.GridHeight*DungeonLayout.CellSize : Current==ExpeditionMap.Large ? 600 : 46;
        static readonly Vector2[] largeSpawns={ new Vector2(-65,235),new Vector2(145,175),new Vector2(-155,75),new Vector2(155,-140),new Vector2(-70,-245) };
        static readonly Vector2[] largeExits=MakeExits();
        static readonly Vector2[] smallSpawns={new Vector2(0,18),new Vector2(0,-18)};
        static readonly Vector2[] smallExits={new Vector2(11.5f,0)};
        static readonly Vector2[] smallSites={new Vector2(-11,9),new Vector2(11,9),new Vector2(-11,-9),new Vector2(11,-9)};
        static readonly Vector2[] smallLoot={new Vector2(-11,9),new Vector2(11,9),new Vector2(-11,-9),new Vector2(11,-9),new Vector2(0,17),new Vector2(0,-17),new Vector2(-11,15),new Vector2(11,-15),new Vector2(-10,0),new Vector2(10,6),new Vector2(-11,-15),new Vector2(11,15)};
        static readonly Vector2[] largeSites=MakeSites();
        static Vector2[] largeLoot=MakeLoot();
        public static Vector2[] SpawnPoints => Current==ExpeditionMap.Dungeon ? DungeonLayout.Spawns : Current==ExpeditionMap.Large ? largeSpawns : smallSpawns;
        public static Vector2[] ExtractionPoints => Current==ExpeditionMap.Dungeon ? DungeonLayout.ExitPositions : Current==ExpeditionMap.Large ? largeExits : smallExits;
        public static Vector2[] Sites => Current==ExpeditionMap.Dungeon ? DungeonLayout.ExplorationPoints : Current==ExpeditionMap.Large ? largeSites : smallSites;
        public static Vector2[] LootPoints => Current==ExpeditionMap.Dungeon ? DungeonLayout.ExplorationPoints : Current==ExpeditionMap.Large ? largeLoot : smallLoot;
        public static Vector2 HouseASpawn => SpawnPoints[0];
        public static Vector2 HouseBSpawn => SpawnPoints[1];
        public static Vector2 MonsterSpawn => Current==ExpeditionMap.Large ? new Vector2(15,15) : Vector2.zero;
        public static Vector2 ExtractionCenter => NearestExtraction(Vector2.zero);
        public static int ExitCount => Current==ExpeditionMap.Dungeon ? 3 : Current==ExpeditionMap.Large ? LargeMapPlan.Exits.Length : 1;
        public static string ExitName(int id) => id<0 || id>=ExitCount ? "Extração" : Current==ExpeditionMap.Dungeon ? DungeonLayout.ExitNames[id] : Current==ExpeditionMap.Large ? LargeMapPlan.Exits[id].Name : "Extração";
        public static bool ExitTransport(int id) => id>=0 && id<ExitCount && (Current==ExpeditionMap.Dungeon ? id>0 : Current==ExpeditionMap.Large && LargeMapPlan.Exits[id].Transport);
        public static Vector2 ExitPosition(int id) => ExtractionPoints[Mathf.Clamp(id,0,ExitCount-1)];
        public static int NearestDungeonExit(Vector2 point,ushort mask=7)
        { int best=-1; float distance=float.PositiveInfinity; for(int i=0;i<3;i++) if((mask&(1<<i))!=0) { float d=DungeonLayout.TravelDistance(point,DungeonLayout.ExitPositions[i]); if(d<distance) { distance=d; best=i; } } return best; }
        public static Vector2 ParticipantSpawn(int slot)
        {
            if(Current==ExpeditionMap.Dungeon) return DungeonLayout.Spawns[Mathf.Clamp(slot,0,7)];
            if(Current==ExpeditionMap.Large) return largeSpawns[Mathf.Clamp(slot,0,largeSpawns.Length-1)];
            return smallSpawns[Mathf.Clamp(slot,0,smallSpawns.Length-1)];
        }
        public static float ParticipantFacing(int slot)
        {
            if(Current==ExpeditionMap.Dungeon)
            {
                int room=slot==0 ? 18 : slot==1 ? 8 : slot==2 ? 10 : slot==3 ? 11 : slot==4 ? 17 : slot==5 ? 16 : slot==6 ? 14 : 13;
                Vector2 d=DungeonLayout.Rooms[room].Center-ParticipantSpawn(slot);
                return Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg;
            }
            if(Current==ExpeditionMap.Test) return slot==0 ? -90 : 90;
            Vector2 direction=LargeMapPlan.Sectors[Mathf.Clamp(slot,0,4)].Center-ParticipantSpawn(slot);
            return Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
        }
        public static Vector2 NearestExtraction(Vector2 point)
        {
            if(Current==ExpeditionMap.Dungeon)
            { ushort mask=MatchManager.Instance!=null ? MatchManager.Instance.ActiveExtractions.Value : (ushort)0; int exit=NearestDungeonExit(point,mask!=0 ? mask : (ushort)5); return exit>=0 ? DungeonLayout.ExitPositions[exit] : point; }
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
            if(map!=ExpeditionMap.Test && map!=ExpeditionMap.Large && map!=ExpeditionMap.Dungeon) return;
            bool changed=Current!=map || LayoutSeed!=layoutSeed; Current=map; LayoutSeed=layoutSeed;
            if(Instance!=null && (changed || Instance.world==null)) Instance.Rebuild();
        }
        void Rebuild()
        {
            terrainRoads.Clear();
            FurniturePoints.Clear();
            largeLoot=MakeLoot();
            if(world!=null) { world.SetActive(false); if(Application.isPlaying) Destroy(world); else DestroyImmediate(world); }
            world=new GameObject(Current==ExpeditionMap.Dungeon ? "CatacumbasDosAfogados" : Current==ExpeditionMap.Large ? "LargeMap" : "QuickTestMap"); world.transform.SetParent(transform,false);
            Block("Ground",Vector2.zero,new Vector2(Width+2,Height+2),Current==ExpeditionMap.Dungeon ? Color.black : new Color(.12f,.16f,.12f),-10);
            if(Current==ExpeditionMap.Test)
            {
            Block("North",new Vector2(0,Height/2+.5f),new Vector2(Width+2,1),Color.gray,-5,true);
            Block("South",new Vector2(0,-Height/2-.5f),new Vector2(Width+2,1),Color.gray,-5,true);
            Block("West",new Vector2(-Width/2-.5f,0),new Vector2(1,Height),Color.gray,-5,true);
            Block("East",new Vector2(Width/2+.5f,0),new Vector2(1,Height),Color.gray,-5,true);
            }
            if(Current==ExpeditionMap.Dungeon) BuildDungeon(); else if(Current==ExpeditionMap.Test) BuildTest(); else BuildLarge();
            if(Current==ExpeditionMap.Test) foreach(var exit in ExtractionPoints)
            {
                var go=Block("Extraction",exit,new Vector2(5,3.5f),new Color(.2f,.8f,.4f,.35f),-7);
                go.AddComponent<BoxCollider2D>().isTrigger=true; go.AddComponent<ExtractionZone>();
            }
            Physics2D.SyncTransforms();
            if(Current==ExpeditionMap.Large && sawmillTerrainPilot)
                SawmillTerrain.Build(world.transform,terrainRoads,OpenTerrain,LayoutSeed);
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
            if(name.Contains("Crate") || name=="Cover") CoverCrateArt.Apply(go,order);
            return go;
        }
        void OnDestroy() { if(Instance==this) Instance=null; }
    }
}
