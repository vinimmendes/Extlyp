using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;
using CyberExtraction.Core;

namespace CyberExtraction.Match
{
    public readonly struct TerrainRoad
    {
        public readonly Vector2 From,To;
        public readonly float Width;
        public readonly SpriteRenderer Legacy;
        public TerrainRoad(Vector2 from,Vector2 to,float width,SpriteRenderer legacy)
        { From=from; To=to; Width=width; Legacy=legacy; }
    }

    // Client-only dressing. All colliders and authoritative map data stay with MapBuilder.
    public sealed class SawmillTerrain : MonoBehaviour
    {
        const int MaskSize=512,ContourSize=256;
        const float RegionSize=128,CellSize=2;
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        readonly List<TerrainRoad> roads=new List<TerrainRoad>();
        readonly List<KeyValuePair<SpriteRenderer,Material>> originals=new List<KeyValuePair<SpriteRenderer,Material>>();
        readonly List<SpriteRenderer> hiddenLogs=new List<SpriteRenderer>();
        Vector2 minimum;
        Func<Vector2,bool> openTerrain;
        Texture2D soil,road,wet,mask;
        Material groundMaterial,roadMaterial,legacyMaterial;
        int seed;

        public static void Build(Transform world,List<TerrainRoad> paths,Func<Vector2,bool> isOpen,int layoutSeed)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var soil=Resources.Load<Texture2D>("Environment/Sawmill/LeafSoil");
            var road=Resources.Load<Texture2D>("Environment/Sawmill/RoadSoil");
            var wet=Resources.Load<Texture2D>("Environment/Sawmill/WetSoil");
            var shader=Resources.Load<Shader>("SawmillTerrain");
            if(soil==null || road==null || wet==null || shader==null || !shader.isSupported)
            { Debug.LogWarning("Piloto da serraria: recursos ausentes/incompativeis; mantendo mapa anterior."); return; }
            var root=new GameObject("SawmillTerrainPilot"); root.transform.SetParent(world,false);
            var terrain=root.AddComponent<SawmillTerrain>();
            terrain.soil=soil; terrain.road=road; terrain.wet=wet; terrain.seed=layoutSeed;
            terrain.minimum=LargeMapPlan.Sectors[0].Center-Vector2.one*64;
            terrain.openTerrain=isOpen;
            foreach(var path in paths)
            {
                // AABB exclusion only; intersecting roads retain their full authoritative segments.
                if(Mathf.Max(path.From.x,path.To.x)+path.Width<terrain.minimum.x ||
                   Mathf.Min(path.From.x,path.To.x)-path.Width>terrain.minimum.x+128 ||
                   Mathf.Max(path.From.y,path.To.y)+path.Width<terrain.minimum.y ||
                   Mathf.Min(path.From.y,path.To.y)-path.Width>terrain.minimum.y+128) continue;
                terrain.roads.Add(path);
            }
            try
            {
                terrain.CreateMask();
                terrain.groundMaterial=terrain.Material(shader,soil,0,0);
                terrain.roadMaterial=terrain.Material(shader,road,1,0);
                terrain.legacyMaterial=terrain.Material(shader,road,1,1);
                terrain.CreateTiles(); terrain.CreateRoadShapes(); terrain.CreateDetails(world);
                foreach(var path in terrain.roads)
                {
                    if(path.Legacy==null) continue;
                    terrain.originals.Add(new KeyValuePair<SpriteRenderer,Material>(path.Legacy,path.Legacy.sharedMaterial));
                    path.Legacy.sharedMaterial=terrain.legacyMaterial;
                }
            }
            catch(Exception exception)
            {
                Debug.LogWarning("Piloto da serraria cancelado; visual anterior preservado. "+exception.Message);
                root.SetActive(false); Destroy(root);
            }
        }
        T Own<T>(T value) where T:UnityEngine.Object { owned.Add(value); return value; }
        Material Material(Shader shader,Texture2D surface,float isRoad,float legacy)
        {
            var material=Own(new Material(shader));
            material.SetTexture("_SurfaceTex",surface); material.SetTexture("_WetTex",wet); material.SetTexture("_MaskTex",mask);
            material.SetVector("_Region",new Vector4(minimum.x,minimum.y,128,128));
            material.SetFloat("_Road",isRoad); material.SetFloat("_Legacy",legacy);
            return material;
        }
        float Coverage(Vector2 p)
        {
            var local=p-minimum;
            float edge=Mathf.Min(Mathf.Min(local.x,local.y),Mathf.Min(128-local.x,128-local.y));
            return Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/8));
        }
        float RoadDistance(Vector2 p)
        {
            float result=-1000;
            foreach(var path in roads)
                result=Mathf.Max(result,path.Width*.5f-LargeMapPlan.SegmentDistance(p,path.From,path.To));
            // Very small border variation, never a new gameplay obstruction.
            return result+(Mathf.PerlinNoise((p.x+seed%997)*.65f,p.y*.65f)-.5f)*.45f;
        }
        void CreateMask()
        {
            mask=Own(new Texture2D(MaskSize,MaskSize,TextureFormat.RGBA32,false,true));
            mask.name="Sawmill road union / open terrain / moisture / fade";
            mask.wrapMode=TextureWrapMode.Clamp; mask.filterMode=FilterMode.Bilinear;
            var pixels=new Color32[MaskSize*MaskSize];
            for(int y=0;y<MaskSize;y++) for(int x=0;x<MaskSize;x++)
            {
                var p=minimum+new Vector2((x+.5f)*128/MaskSize,(y+.5f)*128/MaskSize);
                float border=Mathf.SmoothStep(0,1,Mathf.Clamp01((RoadDistance(p)+.6f)/1.2f));
                float moisture=Mathf.PerlinNoise((p.x+seed%233)*.06f,(p.y+79)*.06f);
                pixels[y*MaskSize+x]=new Color(border,openTerrain(p) ? 1 : 0,moisture,Coverage(p));
            }
            mask.SetPixels32(pixels); mask.Apply(false,true);
        }
        void CreateTiles()
        {
            var gridObject=new GameObject("SoilGrid"); gridObject.transform.SetParent(transform,false);
            gridObject.transform.localPosition=minimum;
            gridObject.AddComponent<Grid>().cellSize=new Vector3(CellSize,CellSize,1);
            var tileObject=new GameObject("SoilTilemap"); tileObject.transform.SetParent(gridObject.transform,false);
            var map=tileObject.AddComponent<Tilemap>();
            var renderer=tileObject.AddComponent<TilemapRenderer>(); renderer.sortingOrder=-9; renderer.sharedMaterial=groundMaterial;
            var variants=new Tile[16];
            for(int y=0;y<4;y++) for(int x=0;x<4;x++)
            {
                var sprite=Own(Sprite.Create(soil,new Rect(x*256,y*256,256,256),new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect));
                var tile=Own(ScriptableObject.CreateInstance<Tile>()); tile.sprite=sprite; tile.colliderType=Tile.ColliderType.None;
                variants[y*4+x]=tile;
            }
            var tiles=new TileBase[64*64];
            int phase=(int)((uint)seed%4);
            for(int y=0;y<64;y++) for(int x=0;x<64;x++) tiles[y*64+x]=variants[((y+phase)%4)*4+(x+phase)%4];
            map.SetTilesBlock(new BoundsInt(0,0,0,64,64,1),tiles);
        }
        void CreateRoadShapes()
        {
            // Trace the union, rather than drawing borders around each intersecting segment.
            // A wider hidden envelope keeps the visible feather inside the generated geometry.
            var occupied=new bool[ContourSize,ContourSize];
            for(int y=0;y<ContourSize;y++) for(int x=0;x<ContourSize;x++)
                occupied[x,y]=RoadDistance(minimum+new Vector2((x+.5f)*.5f,(y+.5f)*.5f))> -1.3f;
            var edges=new Dictionary<Vector2Int,List<Vector2Int>>();
            for(int y=0;y<ContourSize;y++) for(int x=0;x<ContourSize;x++)
            {
                if(!occupied[x,y]) continue;
                if(y==0 || !occupied[x,y-1]) AddEdge(edges,new Vector2Int(x,y),new Vector2Int(x+1,y));
                if(x==ContourSize-1 || !occupied[x+1,y]) AddEdge(edges,new Vector2Int(x+1,y),new Vector2Int(x+1,y+1));
                if(y==ContourSize-1 || !occupied[x,y+1]) AddEdge(edges,new Vector2Int(x+1,y+1),new Vector2Int(x,y+1));
                if(x==0 || !occupied[x-1,y]) AddEdge(edges,new Vector2Int(x,y+1),new Vector2Int(x,y));
            }
            var profile=Own(ScriptableObject.CreateInstance<SpriteShape>()); profile.fillTexture=road;
            int count=0;
            while(edges.Count>0)
            {
                Vector2Int start=default; foreach(var key in edges.Keys) { start=key; break; }
                var cursor=start; var loop=new List<Vector2Int>();
                do
                {
                    loop.Add(cursor);
                    if(!edges.TryGetValue(cursor,out var targets)) throw new InvalidOperationException("Contorno de estrada incompleto.");
                    var next=targets[targets.Count-1]; targets.RemoveAt(targets.Count-1);
                    if(targets.Count==0) edges.Remove(cursor);
                    cursor=next;
                } while(cursor!=start && loop.Count<ContourSize*ContourSize*4);
                if(cursor!=start) throw new InvalidOperationException("Contorno de estrada sem fechamento.");
                long area=0;
                for(int i=0;i<loop.Count;i++) { var a=loop[i]; var b=loop[(i+1)%loop.Count]; area+=(long)a.x*b.y-(long)b.x*a.y; }
                if(area<=0) continue; // Holes are clipped by the union mask in the shared shader.
                var points=new List<Vector2>();
                for(int i=0;i<loop.Count;i++)
                {
                    var before=loop[(i+loop.Count-1)%loop.Count]; var at=loop[i]; var after=loop[(i+1)%loop.Count];
                    if(at-before==after-at) continue;
                    points.Add(minimum+(Vector2)at*.5f);
                }
                if(points.Count<3) continue;
                var go=new GameObject("RoadUnion_"+count++); go.transform.SetParent(transform,false);
                var controller=go.AddComponent<SpriteShapeController>(); controller.spriteShape=profile;
                controller.spline.Clear(); controller.spline.isOpenEnded=false;
                for(int i=0;i<points.Count;i++)
                { controller.spline.InsertPointAt(i,points[i]); controller.spline.SetTangentMode(i,ShapeTangentMode.Linear); }
                // This installed package keeps Stretch UV private and false by default.
                controller.worldSpaceUVs=true; controller.fillPixelsPerUnit=128;
                controller.spriteShapeRenderer.sharedMaterial=roadMaterial; controller.spriteShapeRenderer.sortingOrder=-8;
                controller.RefreshSpriteShape();
            }
            if(count==0 && roads.Count>0) throw new InvalidOperationException("Nenhuma superficie de estrada gerada.");
        }
        static void AddEdge(Dictionary<Vector2Int,List<Vector2Int>> edges,Vector2Int from,Vector2Int to)
        {
            if(!edges.TryGetValue(from,out var list)) { list=new List<Vector2Int>(1); edges.Add(from,list); }
            list.Add(to);
        }
        Sprite LoadSprite(string name,float width)
        {
            var texture=Resources.Load<Texture2D>("Environment/Sawmill/"+name);
            return texture==null ? null : Own(Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),texture.width/width,0,SpriteMeshType.FullRect));
        }
        bool ClearDetail(Vector2 p)
        {
            if(Coverage(p)<.99f || !openTerrain(p) || Physics2D.OverlapCircle(p,2,GameConfig.WallMask)!=null) return false;
            foreach(var spawn in MapBuilder.SpawnPoints) if(Vector2.Distance(p,spawn)<10) return false;
            foreach(var exit in MapBuilder.ExtractionPoints) if(Vector2.Distance(p,exit)<8) return false;
            foreach(var site in MapBuilder.Sites) if(Vector2.Distance(p,site)<16) return false;
            return true;
        }
        void Detail(Sprite sprite,Vector2 p,float angle,Color color,string name)
        {
            if(sprite==null) return;
            var go=new GameObject(name); go.transform.SetParent(transform,false); go.transform.localPosition=p;
            go.transform.localRotation=Quaternion.Euler(0,0,angle);
            SpriteFactory.Setup(go.AddComponent<SpriteRenderer>(),sprite,color,-7);
        }
        void CreateDetails(Transform world)
        {
            var names=new[]{"Leaves","Moss","Pebbles","DampPatch"}; var sprites=new Sprite[names.Length];
            for(int i=0;i<sprites.Length;i++) sprites[i]=LoadSprite(names[i],i==2 ? 2 : 4);
            var r=new System.Random(seed^901731); int placed=0;
            for(int attempt=0;attempt<512 && placed<48;attempt++)
            {
                var p=minimum+new Vector2((float)r.NextDouble()*128,(float)r.NextDouble()*128);
                if(!ClearDetail(p) || RoadDistance(p)>-.4f) continue;
                int kind=r.Next(sprites.Length);
                Detail(sprites[kind],p,r.Next(360),Color.white,names[kind]); placed++;
            }
            var ruts=LoadSprite("WheelRuts",3); int rutCount=0;
            foreach(var path in roads)
            {
                Vector2 delta=path.To-path.From;
                for(float d=5;d<delta.magnitude && rutCount<12;d+=12)
                {
                    var p=path.From+delta.normalized*d;
                    if(!ClearDetail(p)) continue;
                    Detail(ruts,p,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg-90,Color.white,"WheelRuts"); rutCount++;
                }
            }
            var logs=LoadSprite("LogPile",7); var dust=LoadSprite("Sawdust",9);
            foreach(var renderer in world.GetComponentsInChildren<SpriteRenderer>())
            {
                if(renderer.name!="LogPile" || Coverage(renderer.transform.position)<.99f || logs==null) continue;
                var child=new GameObject("BoundLogArt"); child.transform.SetParent(renderer.transform,false);
                child.transform.localScale=new Vector3(1/renderer.transform.lossyScale.x,1/renderer.transform.lossyScale.y,1);
                child.transform.localScale=Vector3.Scale(child.transform.localScale,new Vector3(1,1.8f/logs.bounds.size.y,1));
                SpriteFactory.Setup(child.AddComponent<SpriteRenderer>(),logs,Color.white,renderer.sortingOrder);
                hiddenLogs.Add(renderer); renderer.enabled=false;
                // Parent this art to the pilot so disabling it restores the old renderer cleanly.
                child.transform.SetParent(transform,true);
                Detail(dust,renderer.transform.position,0,Color.white,"SawmillSawdust");
            }
        }
        void OnDisable()
        {
            foreach(var pair in originals) if(pair.Key!=null) pair.Key.sharedMaterial=pair.Value;
            foreach(var renderer in hiddenLogs) if(renderer!=null) renderer.enabled=true;
        }
        void OnEnable()
        {
            foreach(var pair in originals) if(pair.Key!=null) pair.Key.sharedMaterial=legacyMaterial;
            foreach(var renderer in hiddenLogs) if(renderer!=null) renderer.enabled=false;
        }
        void OnDestroy()
        {
            foreach(var value in owned) if(value!=null)
            { if(Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
        }
    }
}
