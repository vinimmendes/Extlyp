using System;
using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Match
{
    // Art on existing cover and stone only; pottery shards are flat floor decals.
    public sealed class DungeonProps : MonoBehaviour
    {
        [Serializable] sealed class Manifest { public Entry[] assets; }
        [Serializable] sealed class Entry { public string id; public float x,y,width,height; }
        static readonly int[] counts={3,3,3,3,3,2,2,3,3,3};
        readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        readonly List<SpriteRenderer> wallDetails=new List<SpriteRenderer>();

        public static void Build(Transform world)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var root=new GameObject("DungeonProps"); root.transform.SetParent(world,false);
            var props=root.AddComponent<DungeonProps>();
            try { props.Load(); props.BuildCovers(); props.FillNiches(); props.BuildFurniture(); props.BuildShards(); props.Commit(); }
            catch(Exception exception)
            { root.SetActive(false); DungeonArtLifetime.Release(root); Debug.LogWarning("Catacumbas: coberturas anteriores preservadas. "+exception.Message); }
        }
        void Load()
        {
            var json=Resources.Load<TextAsset>("Environment/Dungeon/Props/PropsArt");
            if(json==null) throw new InvalidOperationException("Manifesto de sepulturas ausente.");
            var manifest=JsonUtility.FromJson<Manifest>(json.text);
            if(manifest==null || manifest.assets==null) throw new InvalidOperationException("Manifesto de sepulturas invalido.");
            foreach(var entry in manifest.assets)
            {
                var texture=Resources.Load<Texture2D>("Environment/Dungeon/Props/"+entry.id);
                if(texture==null) throw new InvalidOperationException("Asset ausente: "+entry.id);
                int x=Mathf.Clamp(Mathf.FloorToInt(entry.x*texture.width),0,texture.width-1);
                int y=Mathf.Clamp(Mathf.FloorToInt(entry.y*texture.height),0,texture.height-1);
                int w=Mathf.Clamp(Mathf.CeilToInt(entry.width*texture.width),1,texture.width-x);
                int h=Mathf.Clamp(Mathf.CeilToInt(entry.height*texture.height),1,texture.height-y);
                var sprite=Sprite.Create(texture,new Rect(x,y,w,h),new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect);
                owned.Add(sprite); sprites.Add(entry.id,sprite);
            }
            for(int f=1;f<=counts.Length;f++) for(int v=1;v<=counts[f-1];v++)
                if(!sprites.ContainsKey(Id(f,v))) throw new InvalidOperationException("Variante ausente: "+Id(f,v));
            foreach(var renderer in transform.parent.GetComponentsInChildren<SpriteRenderer>())
                if(renderer.name.StartsWith("CAT-A08_",StringComparison.Ordinal) || renderer.name.StartsWith("CAT-A09_",StringComparison.Ordinal) ||
                   renderer.name.StartsWith("CAT-A11_",StringComparison.Ordinal) || renderer.name.StartsWith("CAT-A12_",StringComparison.Ordinal)) wallDetails.Add(renderer);
        }
        static string Id(int family,int variant) => "CAT-P"+family.ToString("00")+"_v"+variant.ToString("00");
        void Place(int family,int variant,Vector2 centre,Vector2 size,float angle,int order)
        {
            string id=Id(family,variant); var sprite=sprites[id];
            var go=new GameObject(id); go.transform.SetParent(transform,false); go.transform.position=centre;
            go.transform.rotation=Quaternion.Euler(0,0,angle);
            go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);
            SpriteFactory.Setup(go.AddComponent<SpriteRenderer>(),sprite,Color.white,order);
        }
        void BuildCovers()
        {
            int i=0; foreach(var cells in DungeonLayout.Covers)
            {
                var rect=DungeonLayout.WorldRect(cells);
                int sector=DungeonLayout.SectorAt(cells.xMin,cells.yMin),variant=i%3+1;
                int family=sector==6 ? (i%2==0 ? 8 : 9) : sector==7 ? 8 : 1;
                Place(family,variant,rect.center,rect.size,0,-4);
                // Closed and open tombs are static dressing, with the same low-cover footprint.
                if(family==1 && i%4!=0) Place(2,variant,rect.center,rect.size*.88f,0,-3);
                i++;
            }
        }
        static int NearestSector(Vector2 point)
        {
            float best=float.PositiveInfinity; int sector=0;
            foreach(var room in DungeonLayout.Rooms)
            { float distance=(room.Center-point).sqrMagnitude; if(distance<best) { best=distance; sector=room.Sector; } }
            return sector;
        }
        void FillNiches()
        {
            int i=0; foreach(var frame in wallDetails)
            {
                if(!frame.name.StartsWith("CAT-A08_",StringComparison.Ordinal)) continue;
                int slot=i++,sector=NearestSector(frame.transform.position);
                if(slot%4==3) continue; // Empty recesses keep the wall rhythm quiet.
                int family=sector==2 ? 4 : slot%4==2 ? 5 : 3;
                Vector2 size=family==4 ? new Vector2(1.44f,.5f) : Vector2.one*.45f;
                Place(family,slot%3+1,frame.transform.position,size*DungeonLayout.CellSize,frame.transform.eulerAngles.z,-3);
            }
        }
        static bool Stone(int x,int y) => DungeonLayout.Index(x,y)>=0 && !DungeonLayout.IsBaseFloor(x,y) && !DungeonLayout.IsLowCover(x,y) && !DungeonLayout.IsWater(x,y);
        bool FurnitureFits(RectInt cells)
        {
            foreach(var cell in cells.allPositionsWithin) if(!Stone(cell.x,cell.y)) return false;
            var rect=DungeonLayout.WorldRect(cells);
            var bounds=new Bounds(rect.center,rect.size+Vector2.one*.1f);
            foreach(var detail in wallDetails) if(detail.bounds.Intersects(bounds)) return false;
            return !NearInteraction(rect.center,2);
        }
        void BuildFurniture()
        {
            int bench=0,table=0;
            for(int i=8;i<DungeonLayout.Rooms.Length;i++)
            {
                var room=DungeonLayout.Rooms[i]; int family=room.Sector==3 || room.Sector==5 ? 6 : room.Sector==6 || room.Sector==7 ? 7 : 0;
                if(family==0) continue;
                int width=family==6 ? 2 : 3;
                for(int side=0;side<2;side++)
                {
                    var cells=new RectInt(Mathf.FloorToInt(room.Cells.center.x-width*.5f),side==0 ? room.Cells.yMax : room.Cells.yMin-1,width,1);
                    if(!FurnitureFits(cells)) continue;
                    var rect=DungeonLayout.WorldRect(cells);
                    int variant=family==6 ? bench++%2+1 : table++%2+1;
                    Place(family,variant,rect.center,rect.size,side==0 ? 0 : 180,-3);
                    break; // One furniture group per room, entirely on inaccessible masonry.
                }
            }
        }
        static bool NearInteraction(Vector2 point,float radius)
        {
            if(DungeonLayout.CanalReserved(new Rect(point-Vector2.one*radius,Vector2.one*radius*2))) return true;
            float squared=radius*radius;
            foreach(var exit in DungeonLayout.ExitPositions) if((point-exit).sqrMagnitude<squared) return true;
            foreach(var spawn in DungeonLayout.Spawns) if((point-spawn).sqrMagnitude<squared) return true;
            foreach(var loot in DungeonContent.Loot) if((point-loot.Position).sqrMagnitude<squared) return true;
            for(int i=0;i<4;i++) if((point-DungeonContent.Mechanism(i)).sqrMagnitude<squared) return true;
            return (point-DungeonContent.CrankPedestal).sqrMagnitude<squared || (point-DungeonContent.KeyPedestal).sqrMagnitude<squared;
        }
        void BuildShards()
        {
            for(int i=8;i<DungeonLayout.Rooms.Length;i++)
            {
                var r=DungeonLayout.Rooms[i].Cells; int x=r.xMin+1,y=r.yMin+1;
                var p=DungeonLayout.ToWorld(new Vector2(x+.5f,y+.5f));
                if(!DungeonLayout.IsBaseFloor(x,y) || NearInteraction(p,2.5f)) continue;
                Place(10,i%3+1,p,Vector2.one*DungeonLayout.CellSize,90*(i%4),-7);
            }
        }
        void Commit()
        {
            foreach(var renderer in transform.parent.GetComponentsInChildren<SpriteRenderer>())
                if(renderer.name=="DungeonSarcophagus" || renderer.name=="SarcophagusLid") renderer.enabled=false;
        }
        void OnDestroy() { foreach(var asset in owned) if(asset!=null) DungeonArtLifetime.Release(asset); }
    }
}
