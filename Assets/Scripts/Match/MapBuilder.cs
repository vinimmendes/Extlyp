using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Core;
namespace CyberExtraction.Match
{
    public class MapBuilder : MonoBehaviour
    {
        public const float Width = 900, Height = 1380;
        public static readonly Vector2[] SpawnPoints = { new Vector2(0,600), new Vector2(0,-600), new Vector2(-360,0), new Vector2(360,0), Vector2.zero };
        public static readonly Vector2 HouseASpawn = SpawnPoints[0], HouseBSpawn = SpawnPoints[1], MonsterSpawn = new Vector2(15,15);
        public static readonly Vector2[] ExtractionPoints = { new Vector2(-400,610), new Vector2(400,610), new Vector2(-400,-610), new Vector2(400,-610), new Vector2(-415,0), new Vector2(415,0), new Vector2(0,60) };
        public static readonly Vector2 ExtractionCenter = ExtractionPoints[6];
        public static readonly Vector2[] Sites = MakeSites();
        public static readonly Vector2[] LootPoints = MakeLoot();
        public static Vector2 ParticipantSpawn(int slot) => SpawnPoints[Mathf.Clamp(slot,0,SpawnPoints.Length-1)];
        public static float ParticipantFacing(int slot) => slot == 1 ? 90 : -90;
        public static Vector2 NearestExtraction(Vector2 point)
        { Vector2 best = ExtractionPoints[0]; foreach (var exit in ExtractionPoints) if ((exit-point).sqrMagnitude < (best-point).sqrMagnitude) best = exit; return best; }
        static Vector2[] MakeSites()
        {
            var sites = new List<Vector2>();
            foreach (var spawn in SpawnPoints) { sites.Add(spawn + new Vector2(16,-12)); sites.Add(spawn + new Vector2(-22,20)); sites.Add(spawn + new Vector2(42,32)); }
            var random = new System.Random(43127);
            for (int i=0;i<65;i++)
            {
                var point = new Vector2(random.Next(-390,391), random.Next(-620,621));
                bool safe = true; foreach (var spawn in SpawnPoints) if (Vector2.Distance(point,spawn)<35) safe=false;
                if (safe) sites.Add(point);
            }
            return sites.ToArray();
        }
        static Vector2[] MakeLoot() { var points = new List<Vector2>(); foreach(var site in Sites) { points.Add(site); points.Add(site + new Vector2(4,0)); } return points.ToArray(); }
        void Awake()
        {
            Block("Ground", Vector2.zero,new Vector2(Width+2,Height+2),new Color(.12f,.16f,.12f),-10);
            Block("North",new Vector2(0,Height/2+.5f),new Vector2(Width+2,1),Color.gray,-5,true);
            Block("South",new Vector2(0,-Height/2-.5f),new Vector2(Width+2,1),Color.gray,-5,true);
            Block("West",new Vector2(-Width/2-.5f,0),new Vector2(1,Height),Color.gray,-5,true);
            Block("East",new Vector2(Width/2+.5f,0),new Vector2(1,Height),Color.gray,-5,true);
            foreach(var spawn in SpawnPoints) House(spawn);
            for(int i=0;i<Sites.Length;i++)
            {
                Vector2 c=Sites[i];
                Block("RuinFloor",c+new Vector2(0,5),new Vector2(10,9),new Color(.24f,.22f,.20f),-8);
                Block("RuinBack",c+new Vector2(0,9),new Vector2(10,.6f),new Color(.35f,.35f,.34f),-4,true);
                Block("RuinLeft",c+new Vector2(-5,5),new Vector2(.6f,8),Color.gray,-4,true);
                Block("Cover",c+new Vector2(8,5),new Vector2(2,2),new Color(.4f,.3f,.2f),-3,true);
                Block("Trail",c+new Vector2(0,-5),new Vector2(3,12),new Color(.22f,.20f,.16f),-9);
                if(i%5==0) Block("Lake",c+new Vector2(-15,0),new Vector2(12,18),new Color(.08f,.23f,.3f),-8);
            }
            foreach(var exit in ExtractionPoints)
            {
                var go=Block("Extraction",exit,new Vector2(5,4),new Color(.2f,.8f,.4f,.35f),-7);
                go.AddComponent<BoxCollider2D>().isTrigger=true; go.AddComponent<ExtractionZone>();
            }
        }
        void House(Vector2 c)
        {
            Block("ShelterFloor",c,new Vector2(8,5),new Color(.22f,.22f,.19f),-8);
            Block("ShelterBack",c+new Vector2(0,2.5f),new Vector2(8,.6f),Color.gray,-4,true);
            Block("ShelterLeft",c+new Vector2(-4,0),new Vector2(.6f,5),Color.gray,-4,true);
            Block("ShelterRight",c+new Vector2(4,0),new Vector2(.6f,5),Color.gray,-4,true);
        }
        GameObject Block(string name,Vector2 center,Vector2 size,Color color,int order,bool solid=false)
        {
            var go=new GameObject(name); go.transform.SetParent(transform,false); go.transform.position=center; go.transform.localScale=new Vector3(size.x,size.y,1);
            SpriteFactory.Setup(go.AddComponent<SpriteRenderer>(),SpriteFactory.Square,color,order);
            if(solid) { go.layer=GameConfig.WallLayer; go.AddComponent<BoxCollider2D>(); } return go;
        }
    }
}
