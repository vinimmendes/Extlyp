using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using CyberExtraction.Core;

namespace CyberExtraction.Match
{
    // Cosmetic tiles only. DungeonLayout and MapBuilder retain all collision/navigation rules.
    public sealed class DungeonTerrain : MonoBehaviour
    {
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        readonly Dictionary<string,Tile[]> families=new Dictionary<string,Tile[]>();
        Material waterMaterial,channelWaterMaterial;
        static readonly int[] counts={4,4,3,3,3,8,2,8,3,2};
        static readonly Vector2Int[] directions={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};

        public static void Build(Transform world)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var root=new GameObject("DungeonTerrain"); root.transform.SetParent(world,false);
            var terrain=root.AddComponent<DungeonTerrain>();
            try { terrain.Load(); terrain.Paint(); }
            catch(Exception exception)
            {
                root.SetActive(false); Destroy(root);
                Debug.LogWarning("Catacumbas: pisos anteriores preservados. "+exception.Message);
            }
        }
        T Own<T>(T asset) where T:UnityEngine.Object { owned.Add(asset); return asset; }
        void Load()
        {
            var waterShader=Resources.Load<Shader>("DungeonWater");
            if(waterShader!=null && waterShader.isSupported)
            {
                var appearance=Resources.Load<Material>("DungeonWaterAppearance");
                waterMaterial=Own(appearance!=null && appearance.shader==waterShader ?
                    new Material(appearance) : new Material(waterShader));
                waterMaterial.name="DungeonWaterSurface";
                // Independent, repeating texture: do not scroll sprite/atlas UVs.
                var flow=Resources.Load<Texture2D>("Environment/Dungeon/Floors/CAT-F07_v01");
                if(flow!=null) waterMaterial.SetTexture("_FlowTex",flow);
                waterMaterial.SetFloat("_FlowScale",1/(2*DungeonLayout.CellSize));
                channelWaterMaterial=Own(new Material(waterMaterial) { name="DungeonChannelWaterSurface" });
                waterMaterial.SetVector("_Current",new Vector4(-1,0,.10f,0));
                channelWaterMaterial.SetVector("_Current",new Vector4(0,-1,.10f,0));
            }
            else Debug.LogWarning("Catacumbas: shader de agua indisponivel; superficie estatica preservada.");
            for(int family=1;family<=10;family++)
            {
                string id="CAT-F"+family.ToString("00");
                var tiles=new Tile[counts[family-1]];
                for(int variant=0;variant<tiles.Length;variant++)
                {
                    string name=id+"_v"+(variant+1).ToString("00");
                    var texture=Resources.Load<Texture2D>("Environment/Dungeon/Floors/"+name);
                    if(texture==null) throw new InvalidOperationException("Asset ausente: "+name);
                    var sprite=Own(Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect));
                    var tile=Own(ScriptableObject.CreateInstance<Tile>());
                    tile.name=name; tile.sprite=sprite; tile.colliderType=Tile.ColliderType.None;
                    // Sources retain their resolution; the visual footprint follows the catalogue.
                    float width=family==7 || family>=9 ? 2 : 1;
                    float height=family==7 ? 2 : 1;
                    tile.transform=Matrix4x4.Scale(new Vector3(width*128/texture.width,height*128/texture.height,1));
                    tiles[variant]=tile;
                }
                families.Add(id,tiles);
            }
        }
        Tilemap Layer(string name,int order)
        {
            var go=new GameObject(name); go.transform.SetParent(transform,false);
            var map=go.AddComponent<Tilemap>();
            var renderer=go.AddComponent<TilemapRenderer>(); renderer.sortingOrder=order;
            if(SpriteFactory.SpriteMaterial!=null) renderer.sharedMaterial=SpriteFactory.SpriteMaterial;
            renderer.mode=TilemapRenderer.Mode.Chunk;
            return map;
        }
        static uint Hash(int x,int y)
        { unchecked { uint h=(uint)(x*73856093 ^ y*19349663 ^ 43127); h^=h>>13; h*=1274126177; return h^(h>>16); } }
        Tile Pick(int family,int x,int y)
        { var options=families["CAT-F"+family.ToString("00")]; return options[(int)(Hash(x,y)%(uint)options.Length)]; }
        static int Surface(int x,int y)
        {
            switch(DungeonLayout.SectorAt(x,y))
            {
                case 2: return Hash(x/4,y/4)%3==0 ? 5 : 4;
                case 4: return 2;
                case 5: return 2;
                case 6: return 3;
                default: return 1;
            }
        }
        void Paint()
        {
            transform.localPosition=DungeonLayout.ToWorld(Vector2.zero);
            transform.localScale=new Vector3(DungeonLayout.CellSize,DungeonLayout.CellSize,1);
            gameObject.AddComponent<Grid>();
            var floor=Layer("Floors",-8); var water=Layer("WaterEastReach",-8); var river=Layer("WaterSouthReach",-8);
            if(waterMaterial!=null) water.GetComponent<TilemapRenderer>().sharedMaterial=waterMaterial;
            if(channelWaterMaterial!=null) river.GetComponent<TilemapRenderer>().sharedMaterial=channelWaterMaterial;
            var transitions=new Tilemap[4];
            for(int d=0;d<4;d++) transitions[d]=Layer("FloorTransitions_"+d,-7);
            var banks=Layer("WaterBanks",-7);
            var stains=Layer("FloodStains",-4); var submerged=Layer("SubmergedSteps",-7);
            int width=DungeonLayout.GridWidth,height=DungeonLayout.GridHeight;
            var floorTiles=new TileBase[width*height]; var waterTiles=new TileBase[width*height]; var riverTiles=new TileBase[width*height];
            for(int y=0;y<height;y++) for(int x=0;x<width;x++)
            {
                // Cover masks block movement, but transparent prop edges still need a floor beneath them.
                if(DungeonLayout.IsBaseFloor(x,y) || DungeonLayout.IsLowCover(x,y)) floorTiles[y*width+x]=Pick(Surface(x,y),x,y);
                // Water has a two-cell footprint; aligned anchors avoid overlapping surfaces.
                if(DungeonLayout.IsWater(x,y) && x%2==0 && y%2==0)
                {
                    var target=DungeonLayout.CanalSouthRiver.Contains(new Vector2Int(x,y)) ? riverTiles : waterTiles;
                    target[y*width+x]=Pick(7,x/2,y/2);
                }
            }
            floor.SetTilesBlock(new BoundsInt(0,0,0,width,height,1),floorTiles);
            water.SetTilesBlock(new BoundsInt(0,0,0,width,height,1),waterTiles);
            river.SetTilesBlock(new BoundsInt(0,0,0,width,height,1),riverTiles);
            // The large water sprite is anchored at the centre of its 2x2 footprint.
            water.tileAnchor=new Vector3(1,1,0);
            river.tileAnchor=new Vector3(1,1,0);
            ConfigureRiverMaterial(waterMaterial,DungeonLayout.CanalEastRiver);
            ConfigureRiverMaterial(channelWaterMaterial,DungeonLayout.CanalSouthRiver);
            for(int y=0;y<height;y++) for(int x=0;x<width;x++)
            {
                var cell=new Vector3Int(x,y,0);
                // Tide marks belong on existing stone walls, never on a walkable landing.
                if(IsStone(x,y) && IsStone(x+1,y) && x%3==0 &&
                    (WetFloor(x,y-1) && WetFloor(x+1,y-1) || WetFloor(x,y+1) && WetFloor(x+1,y+1)))
                    Put(stains,cell,Pick(9,x,y),0);
                if(DungeonLayout.IsWater(x,y))
                {
                    int first=-1,second=-1;
                    for(int d=0;d<4;d++)
                    {
                        var delta=directions[d];
                        if(!DungeonLayout.IsBaseFloor(x+delta.x,y+delta.y)) continue;
                        if(first<0) first=d; else second=d;
                    }
                    if(first>=0)
                    {
                        var choices=families["CAT-F08"];
                        bool corner=second>=0 && (second-first)%2!=0;
                        int turn=corner && first==0 && second==3 ? 3 : first;
                        Put(banks,cell,choices[(corner ? 4 : 0)+(int)(Hash(x,y)%4)],turn);
                    }
                    continue;
                }
                if(!DungeonLayout.IsBaseFloor(x,y)) continue;
                int transitionMask=0;
                for(int d=0;d<4;d++)
                {
                    var delta=directions[d]; int nx=x+delta.x,ny=y+delta.y;
                    if(DungeonLayout.IsBaseFloor(nx,ny) && Surface(x,y)<Surface(nx,ny)) transitionMask|=1<<d;
                }
                for(int d=0;d<4;d++)
                {
                    if((transitionMask&(1<<d))==0) continue;
                    bool corner=(transitionMask&(1<<((d+1)%4)))!=0;
                    var choices=families["CAT-F06"];
                    Put(transitions[d],cell,choices[(corner ? 4 : 0)+(int)(Hash(x,y)%4)],d);
                    if(corner) transitionMask&=~(1<<((d+1)%4));
                }
            }
            // Decorative submerged steps remain entirely inside blocked water, beneath the bridge.
            var crossing=DungeonLayout.CanalBridge;
            Put(submerged,new Vector3Int(crossing.xMin+1,crossing.yMin+1,0),families["CAT-F10"][0],0);
            Put(submerged,new Vector3Int(crossing.xMin+1,crossing.yMax-2,0),families["CAT-F10"][1],2);
            submerged.tileAnchor=new Vector3(1,.5f,0);
            stains.tileAnchor=new Vector3(1,.5f,0);
            // Commit only after every resource and layer has been built successfully.
            foreach(var renderer in transform.parent.GetComponentsInChildren<SpriteRenderer>())
                if(renderer.gameObject.name=="DungeonFloor" || renderer.gameObject.name=="DungeonWater" || renderer.gameObject.name=="FuneralChannel") renderer.enabled=false;
        }
        static void Put(Tilemap map,Vector3Int cell,Tile tile,int quarterTurns)
        {
            map.SetTile(cell,tile); map.SetTileFlags(cell,TileFlags.None);
            map.SetTransformMatrix(cell,Matrix4x4.Rotate(Quaternion.Euler(0,0,-90*quarterTurns))*tile.transform);
        }
        static bool IsStone(int x,int y) => DungeonLayout.Index(x,y)>=0 && !DungeonLayout.IsBaseFloor(x,y) && !DungeonLayout.IsWater(x,y) && !DungeonLayout.IsLowCover(x,y);
        static bool WetFloor(int x,int y) => DungeonLayout.IsBaseFloor(x,y) && DungeonLayout.SectorAt(x,y)==4;
        static void ConfigureRiverMaterial(Material material,RectInt cells)
        {
            if(material==null) return;
            var bounds=DungeonLayout.WorldRect(cells);
            material.SetVector("_WaterBounds",new Vector4(bounds.x,bounds.y,bounds.width,bounds.height));
            ConfigureDrips(material,bounds,true);
        }
        static void ConfigureDrips(Material material,Rect bounds,bool channel)
        {
            // Fixed points inside the existing rectangular water surfaces.
            // Radius .38 stays clear of banks and the centre bridge route.
            var a=new Vector2(bounds.x+bounds.width*.25f,bounds.y+bounds.height*.32f);
            var b=new Vector2(bounds.x+bounds.width*.75f,bounds.y+bounds.height*.68f);
            // Keep the complete ring within its bank even after tuning the
            // appearance asset or shrinking a rectangular water surface.
            float clearance=Mathf.Min(bounds.width*.25f,bounds.height*.32f);
            material.SetFloat("_RippleRadius",Mathf.Min(material.GetFloat("_RippleRadius"),Mathf.Max(.025f,clearance-.08f)));
            material.SetVector("_RippleA",new Vector4(a.x,a.y,channel ? 5.3f : 4.7f,channel ? 2.2f : .7f));
            material.SetVector("_RippleB",new Vector4(b.x,b.y,channel ? 7.4f : 6.1f,channel ? 4.6f : 3.1f));
        }
        void OnDestroy()
        { foreach(var asset in owned) if(asset!=null) Destroy(asset); }
    }
}
