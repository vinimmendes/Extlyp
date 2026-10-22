using System;
using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Match
{
    // Static local landmarks. Existing floor, collision and visibility masks remain authoritative.
    public sealed class DungeonSectorArt : MonoBehaviour
    {
        [Serializable] sealed class Manifest { public Entry[] assets; }
        [Serializable] sealed class Entry { public string id; public float x,y,width,height; }
        readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        readonly List<Sprite> owned=new List<Sprite>();
        readonly List<Bounds> occupied=new List<Bounds>();
        readonly HashSet<int> dressedEntrances=new HashSet<int>();

        public static void Build(Transform world)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var root=new GameObject("DungeonSectorArt"); root.transform.SetParent(world,false);
            var art=root.AddComponent<DungeonSectorArt>();
            try { art.Load(); art.BuildEntrances(); art.BuildLandmarks(); art.Commit(); }
            catch(Exception exception)
            { root.SetActive(false); Destroy(root); Debug.LogWarning("Catacumbas: marcos dos setores indisponiveis. "+exception.Message); }
        }
        static string Id(int family,int variant) => "CAT-S"+family.ToString("00")+"_v"+variant.ToString("00");
        void Load()
        {
            var json=Resources.Load<TextAsset>("Environment/Dungeon/Sectors/SectorsArt");
            if(json==null) throw new InvalidOperationException("Manifesto de setores ausente.");
            var manifest=JsonUtility.FromJson<Manifest>(json.text);
            if(manifest==null || manifest.assets==null) throw new InvalidOperationException("Manifesto de setores invalido.");
            foreach(var entry in manifest.assets)
            {
                var texture=Resources.Load<Texture2D>("Environment/Dungeon/Sectors/"+entry.id);
                if(texture==null) throw new InvalidOperationException("Asset ausente: "+entry.id);
                int x=Mathf.Clamp(Mathf.FloorToInt(entry.x*texture.width),0,texture.width-1);
                int y=Mathf.Clamp(Mathf.FloorToInt(entry.y*texture.height),0,texture.height-1);
                int w=Mathf.Clamp(Mathf.CeilToInt(entry.width*texture.width),1,texture.width-x);
                int h=Mathf.Clamp(Mathf.CeilToInt(entry.height*texture.height),1,texture.height-y);
                var sprite=Sprite.Create(texture,new Rect(x,y,w,h),new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect);
                owned.Add(sprite); sprites.Add(entry.id,sprite);
            }
            for(int family=1;family<=8;family++) for(int variant=1;variant<=(family==1 ? 8 : 1);variant++)
                if(!sprites.ContainsKey(Id(family,variant))) throw new InvalidOperationException("Variante ausente: "+Id(family,variant));
            foreach(var renderer in transform.parent.GetComponentsInChildren<SpriteRenderer>())
                if(renderer.name.StartsWith("CAT-",StringComparison.Ordinal)) occupied.Add(renderer.bounds);
        }
        static bool NearInteraction(Vector2 point,float radius)
        {
            float squared=radius*radius;
            foreach(var p in DungeonLayout.Spawns) if((point-p).sqrMagnitude<squared) return true;
            foreach(var p in DungeonLayout.ExitPositions) if((point-p).sqrMagnitude<squared) return true;
            foreach(var loot in DungeonContent.Loot) if((point-loot.Position).sqrMagnitude<squared) return true;
            for(int i=0;i<4;i++) if((point-DungeonContent.Mechanism(i)).sqrMagnitude<squared) return true;
            return (point-DungeonContent.CrankPedestal).sqrMagnitude<squared || (point-DungeonContent.KeyPedestal).sqrMagnitude<squared;
        }
        bool Fits(RectInt cells,bool floor)
        {
            if(DungeonLayout.CanalReserved(DungeonLayout.WorldRect(cells))) return false;
            foreach(var cell in cells.allPositionsWithin)
            {
                if(DungeonLayout.Index(cell.x,cell.y)<0 || DungeonLayout.IsWater(cell.x,cell.y) || DungeonLayout.IsLowCover(cell.x,cell.y)) return false;
                if(DungeonLayout.IsBaseFloor(cell.x,cell.y)!=floor) return false;
            }
            var rect=DungeonLayout.WorldRect(cells);
            if(NearInteraction(rect.center,floor ? 2.5f : 3.5f)) return false;
            var bounds=new Bounds(rect.center,rect.size+Vector2.one*.08f);
            foreach(var other in occupied) if(other.Intersects(bounds)) return false;
            return true;
        }
        void Place(int family,int variant,RectInt cells,float angle,int order)
        {
            var rect=DungeonLayout.WorldRect(cells); var sprite=sprites[Id(family,variant)];
            var size=angle==90 ? new Vector2(rect.height,rect.width) : rect.size;
            var go=new GameObject(Id(family,variant)); go.transform.SetParent(transform,false);
            go.transform.position=rect.center; go.transform.rotation=Quaternion.Euler(0,0,angle);
            go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);
            var renderer=go.AddComponent<SpriteRenderer>(); SpriteFactory.Setup(renderer,sprite,Color.white,order);
            occupied.Add(renderer.bounds);
        }
        void BuildEntrances()
        {
            for(int room=0;room<8;room++)
            {
                var r=DungeonLayout.Rooms[room].Cells; bool placed=false;
                // Flat inset reliefs at the room edges, leaving the spawn centre clear.
                for(int side=0;side<2 && !placed;side++) for(int x=r.xMin+1;x<=r.xMax-3;x++)
                {
                    var cells=new RectInt(x,side==0 ? r.yMin+1 : r.yMax-2,2,1);
                    if(!Fits(cells,true)) continue;
                    Place(1,room+1,cells,0,-7); dressedEntrances.Add(room+1); placed=true; break;
                }
                if(!placed) Debug.LogWarning("Catacumbas: sem encaixe para a marca da entrada "+(room+1));
            }
        }
        bool TryRoom(int room,int family)
        {
            var r=DungeonLayout.Rooms[room].Cells;
            for(int side=0;side<4;side++)
            {
                bool vertical=side>=2;
                int min=vertical ? r.yMin : r.xMin,max=(vertical ? r.yMax : r.xMax)-4;
                for(int offset=min;offset<=max;offset++)
                {
                    var cells=vertical ? new RectInt(side==2 ? r.xMin-3 : r.xMax,offset,3,4)
                                       : new RectInt(offset,side==0 ? r.yMax : r.yMin-3,4,3);
                    if(!Fits(cells,false)) continue;
                    // The entire inner edge must touch this room's open floor, never a sealed remote pocket.
                    bool touches=true;
                    for(int i=0;i<4;i++)
                    {
                        int x=vertical ? (side==2 ? r.xMin : r.xMax-1) : offset+i;
                        int y=vertical ? offset+i : (side==0 ? r.yMax-1 : r.yMin);
                        if(!DungeonLayout.IsBaseFloor(x,y)) { touches=false; break; }
                    }
                    if(!touches) continue;
                    Place(family,1,cells,vertical ? 90 : 0,-3); return true;
                }
            }
            return false;
        }
        void BuildLandmarks()
        {
            for(int sector=1;sector<8;sector++)
            {
                int preferred=DungeonLayout.SectorLabelRooms[sector];
                bool placed=TryRoom(preferred,sector+1);
                for(int room=8;room<DungeonLayout.Rooms.Length && !placed;room++)
                    if(room!=preferred && DungeonLayout.Rooms[room].Sector==sector) placed=TryRoom(room,sector+1);
                if(!placed) Debug.LogWarning("Catacumbas: sem alvenaria livre para o marco "+DungeonLayout.SectorNames[sector]);
            }
        }
        void Commit()
        {
            foreach(var renderer in transform.parent.GetComponentsInChildren<SpriteRenderer>())
                if(renderer.name.StartsWith("Entrance_",StringComparison.Ordinal) && int.TryParse(renderer.name.Substring(9),out int entry) && dressedEntrances.Contains(entry)) renderer.enabled=false;
        }
        void OnDestroy() { foreach(var sprite in owned) if(sprite!=null) Destroy(sprite); }
    }
}
