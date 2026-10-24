using System.Collections.Generic;
using CyberExtraction.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CyberExtraction.Match
{
    public sealed class DungeonMapArt : MonoBehaviour,IMapSpritePalette
    {
        DungeonArtCatalog catalog;
        DungeonArtCatalog ownedCatalog;
        readonly List<Material> ownedMaterials=new List<Material>();
        public IEnumerable<Sprite> PaletteSprites
        { get { if(catalog!=null) foreach(var entry in catalog.Entries) if(entry.Sprite!=null) yield return entry.Sprite; } }
        public static DungeonMapArt Build(Transform parent,DungeonMapDocument data,DungeonArtCatalog art=null)
        {
            bool frozen=art==null && DungeonMapPublication.ArtCatalog!=null;
            if(frozen) art=DungeonMapPublication.ArtCatalog;
            art=art!=null ? art : Resources.Load<DungeonArtCatalog>(DungeonArtCatalog.ResourcePath);
            if(art==null) { Debug.LogError("Catálogo completo das Catacumbas indisponível."); return null; }
            var go=new GameObject("DungeonPublishedArt"); go.transform.SetParent(parent,false);
            var renderer=go.AddComponent<DungeonMapArt>();
            if(frozen) { renderer.catalog=art;renderer.Draw(data);return renderer; }
            var migrated=Resources.Load<DungeonArtCatalog>(DungeonArtCatalog.MigrationPath);
            renderer.ownedCatalog=ScriptableObject.CreateInstance<DungeonArtCatalog>();
            var combined=new List<DungeonArtEntry>(art.Entries); if(migrated!=null) combined.AddRange(migrated.Entries);
            renderer.ownedCatalog.Entries=combined.ToArray(); renderer.ownedCatalog.ContentHash=DungeonArtCatalog.CombinedHash(art,migrated);
            renderer.catalog=renderer.ownedCatalog; renderer.Draw(data); return renderer;
        }
        Tilemap Layer(Transform parent,string name,int order,float cell)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false);
            var map=go.AddComponent<Tilemap>(); var render=go.AddComponent<TilemapRenderer>();
            render.sortingOrder=order; render.sharedMaterial=catalog.SpriteMaterial??SpriteFactory.SpriteMaterial; render.mode=TilemapRenderer.Mode.Chunk;
            return map;
        }
        void Draw(DungeonMapDocument data)
        {
            var grid=new GameObject("ArtGrid"); grid.transform.SetParent(transform,false);
            grid.transform.localPosition=new Vector3(-64*data.cellSize,-64*data.cellSize,0);
            grid.transform.localScale=new Vector3(data.cellSize,data.cellSize,1); grid.AddComponent<Grid>();
            var floor=Layer(grid.transform,"Floor",-7,data.cellSize);
            floor.color=data.floorTint;
            var walls=Layer(grid.transform,"Walls",-5,data.cellSize);
            var water=Layer(grid.transform,"Water",-7,data.cellSize);
            var southWater=Layer(grid.transform,"WaterSouth",-7,data.cellSize);
            var east=System.Array.Find(data.regions,r=>r.id=="CanalEastRiver");
            var south=System.Array.Find(data.regions,r=>r.id=="CanalSouthRiver");
            var exit=System.Array.Find(data.points,p=>p.id=="Exit/2"); var end=System.Array.Find(data.points,p=>p.id=="BoatEnd");
            var direction=exit!=null && end!=null ? (end.position-exit.position).normalized : Vector2.down;
            WaterMaterial(water,east,new Vector2(direction.y,-direction.x)); WaterMaterial(southWater,south,direction);
            bool Solid(int x,int y)=>x>=0 && y>=0 && x<128 && y<128 && data.cells[y*128+x]==0;
            for(int y=0;y<128;y++) for(int x=0;x<128;x++)
            {
                int kind=data.cells[y*128+x]; var cell=new Vector3Int(x,y,0);
                if(kind==1 || kind==3)
                    floor.SetTile(cell,catalog.Find("Floor_Limestone_"+((y%6)*6+x%6).ToString("00"))?.Tile);
                else if(kind==2)
                {
                    bool inSouth=south!=null && south.Rect.Contains(new Vector2(x+.5f,y+.5f));
                    (inSouth ? southWater : water).SetTile(cell,catalog.Find("Floor_Wet_"+((y%4)*4+x%4).ToString("00"))?.Tile);
                }
                else
                {
                    int mask=DungeonWallCatalog.MaskAt(x,y,Solid);
                    walls.SetTile(cell,catalog.Find("Wall_Dry_"+mask.ToString("X2"))?.Tile);
                }
            }
            var layers=new Dictionary<string,Tilemap>();
            foreach(var surface in data.surfaces)
            {
                string family=SurfaceFamily(surface.asset),key=surface.order+"/"+family;
                if(!layers.TryGetValue(key,out var layer))
                { layer=Layer(grid.transform,"Surface_"+surface.order+"_"+family,surface.order,data.cellSize); layer.color=surface.order<=-6 ? data.floorTint : Color.white; layers.Add(key,layer);
                    layer.GetComponent<TilemapRenderer>().sharedMaterial=System.Array.Find(catalog.SurfaceMaterials??System.Array.Empty<DungeonSurfaceMaterial>(),m=>m.Family==family)?.Material??SurfaceMaterial(family)??catalog.SpriteMaterial??SpriteFactory.SpriteMaterial; }
                layer.SetTile(new Vector3Int(surface.x,surface.y,0),catalog.Find(surface.asset)?.Tile);
                layer.SetTileFlags(new Vector3Int(surface.x,surface.y,0),TileFlags.None);
                layer.SetColor(new Vector3Int(surface.x,surface.y,0),surface.color);
                layer.SetTransformMatrix(new Vector3Int(surface.x,surface.y,0),Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,0,surface.angle),new Vector3(surface.flipX ? -1 : 1,surface.flipY ? -1 : 1,1))*catalog.Find(surface.asset).Tile.transform);
            }
            foreach(var rect in CoverRectangles(data.cells))
                Place("Base_"+rect.x+"_"+rect.y,catalog.Find("LowCoverStone"),new Vector2((rect.center.x-64)*data.cellSize,(rect.center.y-64)*data.cellSize),(Vector2)rect.size*data.cellSize,0,-4,false,false);
            bool legacyVigils=DungeonVotivePresentation.NeedsLegacyFix(data);
            foreach(var v in data.visuals)
            {
                if(legacyVigils && DungeonVotivePresentation.ObsoleteLedge(v.id,v.group,v.asset,v.size)) continue;
                if(legacyVigils && DungeonVotivePresentation.DuplicateNorthCloth(v,data)) continue;
                float angle=legacyVigils && DungeonVotivePresentation.ProjectedCloth(v.id,v.group,v.asset) ? 0 : v.angle;
                var render=Place(v.id,catalog.Find(v.asset),v.position,v.size,angle,v.order,v.flipX,v.flipY);
                if(render!=null) { render.color=v.color; render.enabled=!v.hidden; }
            }
        }
        void WaterMaterial(Tilemap map,DungeonRectRecord region,Vector2 direction)
        {
            var appearance=catalog.WaterMaterial??Resources.Load<Material>("DungeonWaterAppearance");
            var shader=appearance!=null ? appearance.shader : Resources.Load<Shader>("DungeonWater"); if(shader==null || !shader.isSupported) return;
            var material=appearance!=null && appearance.shader==shader ? new Material(appearance) : new Material(shader);
            ownedMaterials.Add(material);
            var flow=catalog.WaterFlowTexture??Resources.Load<Texture2D>("Environment/Dungeon/Floors/CAT-F07_v01"); if(flow!=null) material.SetTexture("_FlowTex",flow);
            material.SetFloat("_FlowScale",1/(2*1.06f)); material.SetVector("_Current",new Vector4(direction.x,direction.y,.10f,0));
            if(region!=null) DungeonTerrain.ConfigureRiverMaterial(material,new RectInt(Mathf.RoundToInt(region.x),Mathf.RoundToInt(region.y),Mathf.RoundToInt(region.width),Mathf.RoundToInt(region.height)));
            map.GetComponent<TilemapRenderer>().sharedMaterial=material;
        }
        public static string SurfaceFamily(string asset)=>asset!=null && asset.StartsWith("Transition_",System.StringComparison.Ordinal) ? asset.Split('_')[1] : "";
        public static Material SurfaceMaterial(string family)=>string.IsNullOrEmpty(family) ? null : Resources.Load<Material>("Environment/Dungeon/ReworkComplete/Surface_"+family);
        void OnDestroy()
        {
            foreach(var material in ownedMaterials) DungeonArtLifetime.Release(material);
            DungeonArtLifetime.Release(ownedCatalog);
        }
        SpriteRenderer Place(string id,DungeonArtEntry entry,Vector2 position,Vector2 size,float angle,int order,bool flipX,bool flipY)
        {
            if(entry?.Sprite==null) { Debug.LogError("Arte das Catacumbas ausente: "+id); return null; }
            var go=new GameObject(id); go.transform.SetParent(transform,false);
            go.transform.localPosition=position; go.transform.localRotation=Quaternion.Euler(0,0,angle);
            go.transform.localScale=new Vector3(size.x/entry.Sprite.bounds.size.x,size.y/entry.Sprite.bounds.size.y,1);
            var render=go.AddComponent<SpriteRenderer>(); SpriteFactory.Setup(render,entry.Sprite,Color.white,order);
            if(catalog.SpriteMaterial!=null) render.sharedMaterial=catalog.SpriteMaterial;
            render.flipX=flipX; render.flipY=flipY;
            return render;
        }
        public static List<RectInt> CoverRectangles(int[] cells)
        {
            var result=new List<RectInt>(); var visited=new bool[cells.Length];
            for(int y=0;y<128;y++) for(int x=0;x<128;x++)
            {
                int index=y*128+x; if(visited[index] || cells[index]!=3) continue;
                int width=1; while(x+width<128 && cells[index+width]==3 && !visited[index+width]) width++;
                int height=1;
                while(y+height<128)
                {
                    bool fits=true;
                    for(int xx=x;xx<x+width;xx++) if(cells[(y+height)*128+xx]!=3 || visited[(y+height)*128+xx]) { fits=false; break; }
                    if(!fits) break; height++;
                }
                for(int yy=y;yy<y+height;yy++) for(int xx=x;xx<x+width;xx++) visited[yy*128+xx]=true;
                result.Add(new RectInt(x,y,width,height));
            }
            return result;
        }
    }
}
