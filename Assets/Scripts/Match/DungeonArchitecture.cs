using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using CyberExtraction.Core;

namespace CyberExtraction.Match
{
    // Presentation only: the authoritative floor, cover and wall masks remain in DungeonLayout.
    public sealed class DungeonArchitecture : MonoBehaviour,IMapSpritePalette
    {
        [Serializable] sealed class ArtManifest { public ArtEntry[] assets; }
        [Serializable] sealed class ArtEntry { public string id; public float x,y,width,height,rotation; }
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        public IEnumerable<Sprite> PaletteSprites=>sprites.Values;
        readonly Dictionary<string,float> sourceRotations=new Dictionary<string,float>();
        readonly HashSet<int> pillarCells=new HashSet<int>();
        readonly HashSet<Vector3Int> portals=new HashSet<Vector3Int>();
        readonly HashSet<int> dressedEntrances=new HashSet<int>();
        static readonly int[] counts={3,3,3,2,3,2,3,3,3,2,3,2};
        static readonly Vector2Int[] directions={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};

        public static void Build(Transform world)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var root=new GameObject("DungeonArchitecture"); root.transform.SetParent(world,false);
            var art=root.AddComponent<DungeonArchitecture>();
            try { art.Load(); art.BuildWalls(); art.BuildPillars(); art.BuildPortals(); art.BuildRiverMouth(); art.BuildWallDetails(); art.Commit(); }
            catch(Exception exception)
            { root.SetActive(false); DungeonArtLifetime.Release(root); Debug.LogWarning("Catacumbas: arquitetura anterior preservada. "+exception.Message); }
        }
        T Own<T>(T asset) where T:UnityEngine.Object { owned.Add(asset); return asset; }
        void Load()
        {
            var json=Resources.Load<TextAsset>("Environment/Dungeon/Architecture/ArchitectureArt");
            if(json==null) throw new InvalidOperationException("Manifesto de arquitetura ausente.");
            var manifest=JsonUtility.FromJson<ArtManifest>(json.text);
            if(manifest==null || manifest.assets==null) throw new InvalidOperationException("Manifesto de arquitetura invalido.");
            foreach(var entry in manifest.assets)
            {
                var texture=Resources.Load<Texture2D>("Environment/Dungeon/Architecture/"+entry.id);
                if(texture==null) throw new InvalidOperationException("Asset ausente: "+entry.id);
                // Normalized alpha bounds remove source padding without editing the original PNG.
                int x=Mathf.Clamp(Mathf.FloorToInt(entry.x*texture.width),0,texture.width-1);
                int y=Mathf.Clamp(Mathf.FloorToInt(entry.y*texture.height),0,texture.height-1);
                int w=Mathf.Clamp(Mathf.CeilToInt(entry.width*texture.width),1,texture.width-x);
                int h=Mathf.Clamp(Mathf.CeilToInt(entry.height*texture.height),1,texture.height-y);
                sprites.Add(entry.id,Own(Sprite.Create(texture,new Rect(x,y,w,h),new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect)));
                sourceRotations.Add(entry.id,entry.rotation);
                if(entry.id.StartsWith("CAT-A01_",StringComparison.Ordinal))
                    sprites.Add(entry.id+"_face",Own(Sprite.Create(texture,new Rect(x,y,w,h*.5f),new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect)));
                if(entry.id.StartsWith("CAT-A06_",StringComparison.Ordinal))
                {
                    float side=w*.18f;
                    sprites.Add(entry.id+"_left",Own(Sprite.Create(texture,new Rect(x,y,side,h),new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect)));
                    sprites.Add(entry.id+"_middle",Own(Sprite.Create(texture,new Rect(x+side,y,w-side*2,h),new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect)));
                    sprites.Add(entry.id+"_right",Own(Sprite.Create(texture,new Rect(x+w-side,y,side,h),new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect)));
                }
            }
            for(int f=1;f<=counts.Length;f++) for(int v=1;v<=counts[f-1];v++)
                if(!sprites.ContainsKey(Id(f,v))) throw new InvalidOperationException("Variante ausente: "+Id(f,v));
            foreach(var rect in DungeonLayout.Pillars) foreach(var cell in rect.allPositionsWithin)
                pillarCells.Add(DungeonLayout.Index(cell.x,cell.y));
        }
        static string Id(int family,int variant) => "CAT-A"+family.ToString("00")+"_v"+variant.ToString("00");
        static int Variant(int family,int x,int y)
        { unchecked { return (int)((uint)(x*73856093 ^ y*19349663 ^ 43127)%(uint)counts[family-1])+1; } }
        static bool Stone(int x,int y) => DungeonLayout.Index(x,y)>=0 && !DungeonLayout.IsBaseFloor(x,y) && !DungeonLayout.IsWater(x,y) && !DungeonLayout.IsLowCover(x,y);
        static bool Floor(int x,int y) => DungeonLayout.IsBaseFloor(x,y);
        bool Wall(int x,int y) => Stone(x,y) && !pillarCells.Contains(DungeonLayout.Index(x,y));
        void BuildWalls()
        {
            var grid=new GameObject("ArchitectureGrid"); grid.transform.SetParent(transform,false);
            grid.transform.localPosition=DungeonLayout.ToWorld(Vector2.zero);
            grid.transform.localScale=new Vector3(DungeonLayout.CellSize,DungeonLayout.CellSize,1); grid.AddComponent<Grid>();
            // Contour sprites have transparent corners and only cover the outermost cells.
            // Give every solid cell an opaque masonry bed, including multi-cell wall decorations.
            var bedObject=new GameObject("WallMasonry"); bedObject.transform.SetParent(grid.transform,false);
            var bed=bedObject.AddComponent<Tilemap>(); var bedRenderer=bedObject.AddComponent<TilemapRenderer>(); bedRenderer.sortingOrder=-6;
            if(SpriteFactory.SpriteMaterial!=null) bedRenderer.sharedMaterial=SpriteFactory.SpriteMaterial;
            var texture=Resources.Load<Texture2D>("Environment/Dungeon/Floors/CAT-F01_v01");
            if(texture==null) throw new InvalidOperationException("Base de alvenaria ausente.");
            var bedSprite=Own(Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect));
            var bedTile=Own(ScriptableObject.CreateInstance<Tile>()); bedTile.sprite=bedSprite;
            bedTile.color=new Color(.7f,.69f,.67f); bedTile.colliderType=Tile.ColliderType.None;
            bedTile.transform=Matrix4x4.Scale(new Vector3(1/bedSprite.bounds.size.x,1/bedSprite.bounds.size.y,1));
            var bedTiles=new TileBase[DungeonLayout.GridWidth*DungeonLayout.GridHeight];
            for(int y=0;y<DungeonLayout.GridHeight;y++) for(int x=0;x<DungeonLayout.GridWidth;x++)
                if(Stone(x,y)) bedTiles[y*DungeonLayout.GridWidth+x]=bedTile;
            bed.SetTilesBlock(new BoundsInt(0,0,0,DungeonLayout.GridWidth,DungeonLayout.GridHeight,1),bedTiles);
            var go=new GameObject("WallContour"); go.transform.SetParent(grid.transform,false);
            var map=go.AddComponent<Tilemap>(); var renderer=go.AddComponent<TilemapRenderer>(); renderer.sortingOrder=-5;
            if(SpriteFactory.SpriteMaterial!=null) renderer.sharedMaterial=SpriteFactory.SpriteMaterial;
            var oppositeObject=new GameObject("WallOppositeContour"); oppositeObject.transform.SetParent(grid.transform,false);
            var opposite=oppositeObject.AddComponent<Tilemap>();
            var oppositeRenderer=oppositeObject.AddComponent<TilemapRenderer>(); oppositeRenderer.sortingOrder=-5;
            if(SpriteFactory.SpriteMaterial!=null) oppositeRenderer.sharedMaterial=SpriteFactory.SpriteMaterial;
            var tileCache=new Dictionary<string,Tile>();
            for(int y=0;y<DungeonLayout.GridHeight;y++) for(int x=0;x<DungeonLayout.GridWidth;x++)
            {
                if(!Wall(x,y)) continue;
                int mask=0,first=-1,exposed=0;
                for(int d=0;d<4;d++) if(Floor(x+directions[d].x,y+directions[d].y))
                { mask|=1<<d; if(first<0) first=d; exposed++; }
                if(mask==5 || mask==10)
                {
                    // A one-cell partition has two exposed faces. Each owns half the cell,
                    // so an opaque full sprite cannot paint over the opposite face.
                    SetWallTile(map,tileCache,x,y,1,(2-first)*90,true);
                    SetWallTile(opposite,tileCache,x,y,1,-first*90,true);
                    continue;
                }
                int family=1; float angle=first>=0 ? (2-first)*90 : 0;
                if(exposed==0)
                {
                    int diagonal=-1;
                    for(int d=0;d<4;d++)
                    { var delta=directions[d]+directions[(d+1)%4]; if(Floor(x+delta.x,y+delta.y)) { diagonal=d; break; } }
                    if(diagonal<0) continue;
                    family=3; angle=(1-diagonal)*90;
                }
                else if(exposed==2 && mask!=5 && mask!=10)
                {
                    // A02's canonical opening faces north/west; A01's face points south.
                    family=2; angle=mask==3 ? 270 : mask==6 ? 180 : mask==12 ? 90 : 0;
                }
                else if(exposed==3)
                { family=4; angle=mask==7 ? 0 : mask==14 ? 270 : mask==13 ? 180 : 90; }
                else if(exposed==4)
                { family=7; angle=0; } // An isolated stone needs a closed outline, not a three-sided cap.
                SetWallTile(map,tileCache,x,y,family,angle);
            }
        }
        void SetWallTile(Tilemap map,Dictionary<string,Tile> cache,int x,int y,int family,float angle,bool halfFace=false)
        {
            // A02_v03 depicts a framed recess, not an open L; keep it out of contour selection.
            int variant=family==2 ? (Variant(family,x,y)-1)%2+1 : Variant(family,x,y);
            string id=Id(family,variant),key=id+(halfFace ? "_face" : "");
            if(!cache.TryGetValue(key,out var tile))
            {
                var sprite=sprites[key]; tile=Own(ScriptableObject.CreateInstance<Tile>());
                tile.sprite=sprite; tile.colliderType=Tile.ColliderType.None;
                tile.transform=Matrix4x4.TRS(halfFace ? new Vector3(0,-.25f,0) : Vector3.zero,Quaternion.identity,
                    new Vector3(1/sprite.bounds.size.x,(halfFace ? .5f : 1)/sprite.bounds.size.y,1));
                cache.Add(key,tile);
            }
            var cell=new Vector3Int(x,y,0); map.SetTile(cell,tile); map.SetTileFlags(cell,TileFlags.None);
            map.SetTransformMatrix(cell,Matrix4x4.Rotate(Quaternion.Euler(0,0,angle+sourceRotations[id]))*tile.transform);
        }
        void Place(int family,int variant,Vector2 cellCenter,Vector2 cells,float angle,int order)
        {
            string id=Id(family,variant); PlaceSprite(id,sprites[id],cellCenter,cells,angle,order);
        }
        void PlaceSprite(string name,Sprite sprite,Vector2 cellCenter,Vector2 cells,float angle,int order)
        {
            var go=new GameObject(name); go.transform.SetParent(transform,false);
            go.transform.position=DungeonLayout.ToWorld(cellCenter); go.transform.rotation=Quaternion.Euler(0,0,angle);
            go.transform.localScale=new Vector3(cells.x*DungeonLayout.CellSize/sprite.bounds.size.x,cells.y*DungeonLayout.CellSize/sprite.bounds.size.y,1);
            SpriteFactory.Setup(go.AddComponent<SpriteRenderer>(),sprite,Color.white,order);
        }
        void BuildPillars()
        {
            int i=0; foreach(var rect in DungeonLayout.Pillars)
                Place(7,++i%3+1,rect.center,new Vector2(rect.width,rect.height),0,-4);
        }
        bool ClearPortal(int x,int y,bool vertical)
        {
            // Four open cells with open approaches and solid flanks; never create a visual doorway in a wall.
            for(int i=0;i<4;i++) for(int depth=-1;depth<=1;depth++)
                if(!Floor(x+(vertical ? depth : i),y+(vertical ? i : depth))) return false;
            return vertical ? Stone(x,y-1) && Stone(x,y+4) : Stone(x-1,y) && Stone(x+4,y);
        }
        bool NearExit(Vector2 centre)
        {
            var point=DungeonLayout.ToWorld(centre);
            foreach(var exit in DungeonLayout.ExitPositions) if((point-exit).sqrMagnitude<16) return true;
            return false;
        }
        void Portal(int room,int x,int y,bool vertical)
        {
            if(!ClearPortal(x,y,vertical)) return;
            var centre=new Vector2(x+(vertical ? .5f : 2),y+(vertical ? 2 : .5f));
            if(NearExit(centre)) return;
            var key=new Vector3Int(x,y,vertical ? 1 : 0); if(!portals.Add(key)) return;
            int family=room<8 ? 10 : 6;
            if(family==10) Place(family,room%2+1,centre,new Vector2(4,1),vertical ? 90 : 0,-6);
            else
            {
                string id=Id(6,room%2+1); var offset=vertical ? Vector2.up*2.5f : Vector2.right*2.5f;
                float angle=vertical ? 90 : 0;
                // Support stones sit on the two already-solid flanks, outside the four-cell opening.
                PlaceSprite(id+"_left",sprites[id+"_left"],centre-offset,Vector2.one,angle,-4);
                PlaceSprite(id+"_middle",sprites[id+"_middle"],centre,new Vector2(4,1),angle,-6);
                PlaceSprite(id+"_right",sprites[id+"_right"],centre+offset,Vector2.one,angle,-4);
            }
            if(room<8) dressedEntrances.Add(room);
        }
        void BuildPortals()
        {
            for(int room=0;room<DungeonLayout.Rooms.Length;room++)
            {
                var r=DungeonLayout.Rooms[room].Cells;
                for(int x=r.xMin;x<=r.xMax-4;x++) { Portal(room,x,r.yMin-1,false); Portal(room,x,r.yMax,false); }
                for(int y=r.yMin;y<=r.yMax-4;y++) { Portal(room,r.xMin-1,y,true); Portal(room,r.xMax,y,true); }
            }
        }
        void BuildWallDetails()
        {
            int grille=0,door=0;
            for(int room=0;room<DungeonLayout.Rooms.Length;room++)
            {
                var r=DungeonLayout.Rooms[room].Cells; int sector=DungeonLayout.Rooms[room].Sector;
                for(int slot=0;slot<4;slot++)
                {
                    int x=r.xMin+2+(slot%2)*(r.width-6),y=slot<2 ? r.yMax : r.yMin-1,inward=slot<2 ? -1 : 1;
                    if(!Wall(x,y) || !Wall(x+1,y) || !Floor(x,y+inward) || !Floor(x+1,y+inward)) continue;
                    int family=sector==5 ? (slot%2==0 ? 11 : 12) : sector==1 ? 9 : slot==0 ? 5 : 8;
                    int variant=family==11 ? grille++%3+1 : family==12 ? door++%2+1 : Variant(family,x,y);
                    // Every recess and closed/open door remains entirely on already blocked stone.
                    Place(family,variant,new Vector2(x+1,y+.5f),new Vector2(2,1),slot<2 ? 0 : 180,-4);
                }
            }
        }
        void BuildRiverMouth()
        {
            // The downstream arch spans permanent water; supports sit on the stone banks.
            string id=Id(6,2);
            var centre=new Vector2(122,3); // relative (58,-61), before the southern map edge
            PlaceSprite("CanalOutletLeft",sprites[id+"_left"],centre+Vector2.left*4.5f,new Vector2(1,3),0,-3);
            PlaceSprite("CanalOutletArch",sprites[id+"_middle"],centre,new Vector2(8,2),0,-3);
            PlaceSprite("CanalOutletRight",sprites[id+"_right"],centre+Vector2.right*4.5f,new Vector2(1,3),0,-3);
        }
        void Commit()
        {
            foreach(var renderer in transform.parent.GetComponentsInChildren<SpriteRenderer>())
            {
                if(renderer.name.StartsWith("RoomThreshold_",StringComparison.Ordinal) && portals.Count>0) renderer.enabled=false;
                if(renderer.name.StartsWith("Entrance_",StringComparison.Ordinal) && int.TryParse(renderer.name.Substring(9),out int entry) && dressedEntrances.Contains(entry-1)) renderer.enabled=false;
            }
        }
        void OnDestroy() { foreach(var asset in owned) if(asset!=null) DungeonArtLifetime.Release(asset); }
    }
}
