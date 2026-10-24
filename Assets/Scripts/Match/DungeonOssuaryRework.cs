using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using CyberExtraction.Core;

namespace CyberExtraction.Match
{
    // R17 presentation pilot. No collision, NetworkObjects, random placement or Update.
    // Old object names/transforms and saved MapVisualEdits remain intact.
    public sealed class DungeonOssuaryRework : MonoBehaviour,IMapSpritePalette
    {
        const string ResourceRoot="Environment/Dungeon/ReworkPilot/";
        [Serializable] sealed class Manifest
        {
            public int version,room;
            public float cellSize;
            public Art[] assets;
            public Floor[] floors;
            public Floor[] water;
            public Wall[] walls;
            public Piece[] pieces;
        }
        [Serializable] sealed class Art { public string id,resource; public int x,y,width,height; }
        [Serializable] sealed class Floor { public string art; public int x,y; }
        [Serializable] sealed class Wall { public string state,art; public int mask; }
        [Serializable] sealed class Piece
        {
            public string id,art,support,group;
            public float x,y,width,height,angle;
            public int order;
            public bool stretch;
        }
        sealed class HiddenTile { public Tilemap map; public Vector3Int cell; public Color color; public TileFlags flags; }
        readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        readonly Dictionary<string,Tile> tiles=new Dictionary<string,Tile>();
        readonly Dictionary<string,string> walls=new Dictionary<string,string>();
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        readonly List<string> omitted=new List<string>();
        readonly List<SpriteRenderer> hiddenRenderers=new List<SpriteRenderer>();
        readonly List<HiddenTile> hiddenTiles=new List<HiddenTile>();
        Manifest data;
        RectInt room;
        Tilemap floors,contour;
        public IEnumerable<Sprite> PaletteSprites=>sprites.Values;

        public static void Build(Transform world)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var root=new GameObject("DungeonOssuaryRework_R17");
            root.transform.SetParent(world,false);
            var pilot=root.AddComponent<DungeonOssuaryRework>();
            try
            {
                pilot.Load();
                pilot.BuildSurfaces();
                pilot.BuildObjects();
                // Do not suppress any old art until all resources and layers are ready.
                pilot.Commit();
                if(pilot.omitted.Count>0)
                    Debug.LogWarning("Piloto do Ossário inferior: peças sem encaixe seguro omitidas: "+string.Join(", ",pilot.omitted),pilot);
            }
            catch(Exception exception)
            {
                pilot.RestoreLegacy();
                root.SetActive(false); DungeonArtLifetime.Release(root);
                Debug.LogWarning("Piloto do Ossário inferior indisponível; arte anterior preservada. "+exception.Message);
            }
        }
        T Own<T>(T asset) where T:UnityEngine.Object { owned.Add(asset); return asset; }
        void Load()
        {
            var json=Resources.Load<TextAsset>(ResourceRoot+"Pilot");
            if(json==null) throw new InvalidOperationException("Pilot.json ausente.");
            data=JsonUtility.FromJson<Manifest>(json.text);
            if(data==null || data.version!=1 || data.room!=17 || Mathf.Abs(data.cellSize-DungeonLayout.CellSize)>.0001f ||
                data.assets==null || data.floors==null || data.water==null || data.walls==null || data.pieces==null)
                throw new InvalidOperationException("Manifesto do piloto inválido.");
            room=DungeonLayout.Rooms[data.room].Cells;
            // The floor atlas was authored for this exact room; never stretch it after a layout change.
            if(room.x!=11 || room.y!=36 || room.width!=18 || room.height!=16)
                throw new InvalidOperationException("R17 mudou de geometria; revisar o piloto antes de aplicar.");
            var textures=new Dictionary<string,Texture2D>();
            foreach(var entry in data.assets)
            {
                if(!textures.TryGetValue(entry.resource,out var texture))
                {
                    texture=Resources.Load<Texture2D>(entry.resource);
                    if(texture==null) throw new InvalidOperationException("Arte ausente: "+entry.resource);
                    textures.Add(entry.resource,texture);
                }
                if(entry.x<0 || entry.y<0 || entry.width<=0 || entry.height<=0 ||
                    entry.x+entry.width>texture.width || entry.y+entry.height>texture.height)
                    throw new InvalidOperationException("Recorte inválido ou textura reduzida na importação: "+entry.id);
                var sprite=Own(Sprite.Create(texture,new Rect(entry.x,entry.y,entry.width,entry.height),new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect));
                sprite.name=entry.id; sprites.Add(entry.id,sprite);
            }
            foreach(var entry in data.walls)
            {
                if(DungeonWallCatalog.Normalize(entry.mask)!=entry.mask || !sprites.ContainsKey(entry.art))
                    throw new InvalidOperationException("Máscara de parede inválida.");
                walls.Add(entry.state+"_"+entry.mask,entry.art);
            }
            foreach(var state in new[]{"Dry","Damp","Cracked","Roots"})
                for(int mask=0;mask<256;mask++)
                    if(DungeonWallCatalog.Normalize(mask)==mask && !walls.ContainsKey(state+"_"+mask))
                        throw new InvalidOperationException("Vizinhança ausente: "+state+"/"+mask);
            if(data.floors.Length!=room.width*room.height) throw new InvalidOperationException("Piso incompleto.");
            foreach(var entry in data.floors)
                if(!room.Contains(new Vector2Int(entry.x,entry.y)) || !sprites.ContainsKey(entry.art))
                    throw new InvalidOperationException("Célula de piso inválida.");
            foreach(var entry in data.water)
                if(!room.Contains(new Vector2Int(entry.x,entry.y)) || !sprites.ContainsKey(entry.art))
                    throw new InvalidOperationException("Célula de alagamento visual inválida.");
            foreach(var piece in data.pieces)
                if(string.IsNullOrEmpty(piece.id) || !sprites.ContainsKey(piece.art) || piece.width<=0 || piece.height<=0 ||
                    float.IsNaN(piece.x) || float.IsNaN(piece.y) || float.IsInfinity(piece.x) || float.IsInfinity(piece.y) ||
                    piece.angle%90!=0 || (piece.support!="Stone" && piece.support!="Cover" && piece.support!="Floor"))
                    throw new InvalidOperationException("Peça inválida: "+piece.id);
        }
        Tile TileFor(string id)
        {
            if(tiles.TryGetValue(id,out var tile)) return tile;
            tile=Own(ScriptableObject.CreateInstance<Tile>());
            tile.name=id; tile.sprite=sprites[id]; tile.colliderType=Tile.ColliderType.None;
            tiles.Add(id,tile); return tile;
        }
        Tilemap Layer(Transform grid,string name,int order)
        {
            var go=new GameObject(name); go.transform.SetParent(grid,false);
            var map=go.AddComponent<Tilemap>(); var renderer=go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder=order; renderer.mode=TilemapRenderer.Mode.Chunk;
            if(SpriteFactory.SpriteMaterial!=null) renderer.sharedMaterial=SpriteFactory.SpriteMaterial;
            return map;
        }
        static bool Stone(int x,int y)=>DungeonLayout.Index(x,y)>=0 && !DungeonLayout.IsBaseFloor(x,y) &&
            !DungeonLayout.IsLowCover(x,y) && !DungeonLayout.IsWater(x,y);
        string State(int x,int y)=>x<room.xMin+4 && y>room.center.y ? "Roots" :
            x>room.center.x+2 && y<room.yMin+2 ? "Damp" : (x+3*y)%9==0 ? "Cracked" : "Dry";
        void BuildSurfaces()
        {
            var grid=new GameObject("PilotGrid"); grid.transform.SetParent(transform,false);
            grid.transform.localPosition=DungeonLayout.ToWorld(Vector2.zero);
            grid.transform.localScale=new Vector3(DungeonLayout.CellSize,DungeonLayout.CellSize,1);
            grid.AddComponent<Grid>();
            floors=Layer(grid.transform,"CRP_Floor_R17",-7);
            var water=Layer(grid.transform,"CRP_ShallowWater_R17",-6);
            contour=Layer(grid.transform,"CRP_Walls_R17",-5);
            foreach(var cell in data.floors)
                if(DungeonLayout.IsBaseFloor(cell.x,cell.y) || DungeonLayout.IsLowCover(cell.x,cell.y))
                    floors.SetTile(new Vector3Int(cell.x,cell.y,0),TileFor(cell.art));
            foreach(var cell in data.water)
                if(DungeonLayout.IsBaseFloor(cell.x,cell.y))
                    water.SetTile(new Vector3Int(cell.x,cell.y,0),TileFor(cell.art));
            for(int y=room.yMin-1;y<=room.yMax;y++) for(int x=room.xMin-1;x<=room.xMax;x++)
                if(Stone(x,y))
                {
                    int mask=DungeonWallCatalog.MaskAt(x,y,Stone);
                    contour.SetTile(new Vector3Int(x,y,0),TileFor(walls[State(x,y)+"_"+mask]));
                }
            foreach(var cover in DungeonLayout.Covers)
                if(room.Contains(cover.min) && room.Contains(cover.max-Vector2Int.one))
                    Place("CRP_LowCoverSupport_"+cover.x+"_"+cover.y,sprites["CRP_LowCoverStone"],cover.center,cover.size,0,-4,false);
        }
        static bool Near(Rect rect,Vector2 point,float radius)
        {
            var nearest=new Vector2(Mathf.Clamp(point.x,rect.xMin,rect.xMax),Mathf.Clamp(point.y,rect.yMin,rect.yMax));
            return (nearest-point).sqrMagnitude<radius*radius;
        }
        static bool Protected(Rect rect)
        {
            if(DungeonBreakableLayout.Reserved(rect) || DungeonLayout.CanalReserved(rect)) return true;
            foreach(var spawn in DungeonLayout.Spawns) if(Near(rect,spawn,3)) return true;
            foreach(var exit in DungeonLayout.ExitPositions) if(Near(rect,exit,5)) return true;
            foreach(var loot in DungeonContent.Loot) if(Near(rect,loot.Position,2.8f)) return true;
            foreach(var encounter in DungeonContent.Encounters) if(Near(rect,encounter.Position,2)) return true;
            for(int i=0;i<4;i++) if(Near(rect,DungeonContent.Mechanism(i),2.8f)) return true;
            return Near(rect,DungeonContent.CrankPedestal,3) || Near(rect,DungeonContent.KeyPedestal,3);
        }
        bool Fits(Piece piece)
        {
            var size=new Vector2(piece.width,piece.height);
            if(Mathf.Abs(piece.angle%180)>1) size=new Vector2(size.y,size.x);
            var rect=new Rect(DungeonLayout.ToWorld(new Vector2(piece.x,piece.y))-size*DungeonLayout.CellSize*.5f,size*DungeonLayout.CellSize);
            if(Protected(rect)) return false;
            int minX=Mathf.FloorToInt(rect.xMin/DungeonLayout.CellSize+64+.0001f),maxX=Mathf.CeilToInt(rect.xMax/DungeonLayout.CellSize+64-.0001f);
            int minY=Mathf.FloorToInt(rect.yMin/DungeonLayout.CellSize+64+.0001f),maxY=Mathf.CeilToInt(rect.yMax/DungeonLayout.CellSize+64-.0001f);
            for(int y=minY;y<maxY;y++) for(int x=minX;x<maxX;x++)
            {
                if(DungeonLayout.Index(x,y)<0 || DungeonLayout.IsWater(x,y)) return false;
                if(piece.support=="Stone" && !Stone(x,y)) return false;
                if(piece.support=="Cover" && !DungeonLayout.IsLowCover(x,y)) return false;
                if(piece.support=="Floor" && (!room.Contains(new Vector2Int(x,y)) || !DungeonLayout.IsBaseFloor(x,y) || DungeonDetails.AtDoorApproach(x,y))) return false;
            }
            return true;
        }
        void Place(string name,Sprite sprite,Vector2 cellCentre,Vector2 cells,float angle,int order,bool uniform=true)
        {
            var go=new GameObject(name); go.transform.SetParent(transform,false);
            go.transform.position=DungeonLayout.ToWorld(cellCentre); go.transform.rotation=Quaternion.Euler(0,0,angle);
            var size=cells*DungeonLayout.CellSize;
            var scale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);
            if(uniform) scale.x=scale.y=Mathf.Min(scale.x,scale.y);
            go.transform.localScale=scale;
            SpriteFactory.Setup(go.AddComponent<SpriteRenderer>(),sprite,Color.white,order);
        }
        void BuildObjects()
        {
            foreach(var piece in data.pieces)
            {
                if(!Fits(piece)) { omitted.Add(piece.id); continue; }
                Place("CRP_"+piece.group+"_"+piece.id,sprites[piece.art],new Vector2(piece.x,piece.y),new Vector2(piece.width,piece.height),piece.angle,piece.order,!piece.stretch);
            }
        }
        static bool Contains(Rect outer,Bounds bounds)=>bounds.min.x>=outer.xMin-.001f && bounds.max.x<=outer.xMax+.001f &&
            bounds.min.y>=outer.yMin-.001f && bounds.max.y<=outer.yMax+.001f;
        void Commit()
        {
            var area=DungeonLayout.WorldRect(new RectInt(room.xMin-1,room.yMin-1,room.width+2,room.height+2));
            foreach(var renderer in transform.parent.GetComponentsInChildren<SpriteRenderer>())
            {
                if(renderer.transform.IsChildOf(transform) || !Contains(area,renderer.bounds)) continue;
                // Only legacy art, never networked loot, mechanisms or arbitrary user additions.
                var parent=renderer.transform.parent;
                string owner=parent!=null ? parent.name : "";
                bool legacy=owner=="DungeonArchitecture" || owner=="DungeonProps" || owner=="DungeonDetails" ||
                    owner=="DungeonNarrativeDressing" || owner=="DungeonSectorArt";
                if(renderer.enabled && (legacy || renderer.name=="DungeonSarcophagus" || renderer.name=="SarcophagusLid" ||
                    renderer.name=="RoomThreshold_"+DungeonLayout.Rooms[17].Name))
                { hiddenRenderers.Add(renderer); renderer.enabled=false; }
            }
            // Keep original tiles/paths for saved editor adjustments; clear their visual alpha only.
            foreach(var map in transform.parent.GetComponentsInChildren<Tilemap>())
            {
                if(map.transform.IsChildOf(transform)) continue;
                bool architecture=map.name=="WallContour" || map.name=="WallOppositeContour";
                bool terrain=map.transform.parent!=null && map.transform.parent.name=="DungeonTerrain";
                if(!architecture && !terrain) continue;
                for(int y=room.yMin-1;y<=room.yMax;y++) for(int x=room.xMin-1;x<=room.xMax;x++)
                {
                    var cell=new Vector3Int(x,y,0);
                    bool replaced=architecture ? contour.HasTile(cell) : floors.HasTile(cell);
                    if(!replaced || !map.HasTile(cell)) continue;
                    hiddenTiles.Add(new HiddenTile {map=map,cell=cell,color=map.GetColor(cell),flags=map.GetTileFlags(cell)});
                    map.SetTileFlags(cell,map.GetTileFlags(cell)&~TileFlags.LockColor); map.SetColor(cell,Color.clear);
                }
            }
        }
        void RestoreLegacy()
        {
            foreach(var renderer in hiddenRenderers) if(renderer!=null) renderer.enabled=true;
            hiddenRenderers.Clear();
            foreach(var tile in hiddenTiles)
                if(tile.map!=null)
                {
                    tile.map.SetTileFlags(tile.cell,TileFlags.None); tile.map.SetColor(tile.cell,tile.color);
                    tile.map.SetTileFlags(tile.cell,tile.flags);
                }
            hiddenTiles.Clear();
        }
        void OnDestroy()
        {
            RestoreLegacy();
            foreach(var asset in owned) if(asset!=null) DungeonArtLifetime.Release(asset);
        }
    }
}
