using System;
using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Match
{
    // Static dressing only: no new collision, hazards, lights or loot.
    public sealed class DungeonDetails : MonoBehaviour
    {
        [Serializable] sealed class Manifest { public Entry[] assets; }
        [Serializable] sealed class Entry { public string id; public float x,y,width,height; }
        static readonly int[] counts={4,3,3,4,2,3,3,3,3,3,3,3};
        static readonly int[][] wallFamilies={new int[0],new[]{11,10,7},new[]{1,4},new[]{7,11,10},new[]{1,10,4},new[]{10,11,4},new[]{1,11,7},new[]{10,1,7}};
        static readonly int[][] floorFamilies={new int[0],new[]{9,12},new[]{2,6},new[]{12,9},new[]{3,8},new[]{9,8},new[]{6,12},new[]{2,3}};
        readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        readonly List<Sprite> owned=new List<Sprite>();
        readonly List<Bounds> occupied=new List<Bounds>();
        readonly List<SpriteRenderer> niches=new List<SpriteRenderer>();
        readonly int[] used=new int[13];
        static readonly int[][] clusterFamilies={new[]{0,12,7},new[]{0,7,12},new[]{6,2,0},new[]{7,12,11},
            new[]{10,3,12},new[]{10,11,6},new[]{7,11,0},new[]{10,12,0}};
        readonly List<Vector2> clusterCentres=new List<Vector2>();

        public static void Build(Transform world)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var root=new GameObject("DungeonDetails"); root.transform.SetParent(world,false);
            var details=root.AddComponent<DungeonDetails>();
            try { details.Load(); details.DressRooms(); details.DressClusters(); }
            catch(Exception exception)
            { root.SetActive(false); Destroy(root); Debug.LogWarning("Catacumbas: detalhes ambientais omitidos. "+exception.Message); }
        }
        static string Id(int family,int variant) => "CAT-D"+family.ToString("00")+"_v"+variant.ToString("00");
        void Load()
        {
            var json=Resources.Load<TextAsset>("Environment/Dungeon/Details/DetailsArt");
            if(json==null) throw new InvalidOperationException("Manifesto de detalhes ausente.");
            var manifest=JsonUtility.FromJson<Manifest>(json.text);
            if(manifest==null || manifest.assets==null) throw new InvalidOperationException("Manifesto de detalhes invalido.");
            foreach(var entry in manifest.assets)
            {
                var texture=Resources.Load<Texture2D>("Environment/Dungeon/Details/"+entry.id);
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
            LoadPotteryShards();
            foreach(var renderer in transform.parent.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if(renderer.name.StartsWith("CAT-",StringComparison.Ordinal) || renderer.name=="DockPier" || renderer.name=="DockEdge") occupied.Add(renderer.bounds);
                if(renderer.name.StartsWith("CAT-A08_",StringComparison.Ordinal)) niches.Add(renderer);
            }
        }
        void LoadPotteryShards()
        {
            var json=Resources.Load<TextAsset>("Environment/Dungeon/Props/PropsArt");
            if(json==null) return; // Existing detail families remain available.
            var manifest=JsonUtility.FromJson<Manifest>(json.text);
            if(manifest?.assets==null) return;
            foreach(var entry in manifest.assets)
            {
                if(!entry.id.StartsWith("CAT-P10_",StringComparison.Ordinal)) continue;
                var texture=Resources.Load<Texture2D>("Environment/Dungeon/Props/"+entry.id);
                if(texture==null) continue;
                int x=Mathf.Clamp(Mathf.FloorToInt(entry.x*texture.width),0,texture.width-1);
                int y=Mathf.Clamp(Mathf.FloorToInt(entry.y*texture.height),0,texture.height-1);
                int w=Mathf.Clamp(Mathf.CeilToInt(entry.width*texture.width),1,texture.width-x);
                int h=Mathf.Clamp(Mathf.CeilToInt(entry.height*texture.height),1,texture.height-y);
                var sprite=Sprite.Create(texture,new Rect(x,y,w,h),new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect);
                owned.Add(sprite); sprites.Add(entry.id,sprite);
            }
        }
        static Vector2Int Footprint(int family)
        {
            if(family==1 || family==4) return new Vector2Int(2,2);
            if(family==2 || family==9) return new Vector2Int(2,1);
            if(family==11) return new Vector2Int(1,2);
            return Vector2Int.one;
        }
        static bool Near(Rect rect,Vector2 point,float radius)
        {
            var closest=new Vector2(Mathf.Clamp(point.x,rect.xMin,rect.xMax),Mathf.Clamp(point.y,rect.yMin,rect.yMax));
            return (point-closest).sqrMagnitude<radius*radius;
        }
        static bool Reserved(Rect rect)
        {
            if(DungeonLayout.CanalReserved(rect)) return true;
            foreach(var p in DungeonLayout.Spawns) if(Near(rect,p,3)) return true;
            foreach(var p in DungeonLayout.ExitPositions) if(Near(rect,p,5)) return true;
            // Keep the barge's existing departure sweep clear as well as its boarding zone.
            var boat=DungeonLayout.ExitPositions[2];
            if(rect.Overlaps(new Rect(boat-new Vector2(3,2),new Vector2(12,4)))) return true;
            foreach(var slot in DungeonContent.Loot) if(Near(rect,slot.Position,2.8f)) return true;
            foreach(var slot in DungeonContent.Encounters) if(Near(rect,slot.Position,2)) return true;
            for(int i=0;i<4;i++) if(Near(rect,DungeonContent.Mechanism(i),2.8f)) return true;
            return Near(rect,DungeonContent.CrankPedestal,3) || Near(rect,DungeonContent.KeyPedestal,3);
        }
        bool Fits(RectInt cells,bool floor)
        {
            foreach(var p in cells.allPositionsWithin)
            {
                if(DungeonLayout.Index(p.x,p.y)<0 || DungeonLayout.IsWater(p.x,p.y) || DungeonLayout.IsLowCover(p.x,p.y)) return false;
                if(DungeonLayout.IsBaseFloor(p.x,p.y)!=floor) return false;
            }
            var rect=DungeonLayout.WorldRect(cells); if(Reserved(rect) || floor && DungeonBreakableLayout.Reserved(rect)) return false;
            var bounds=new Bounds(rect.center,rect.size+Vector2.one*.15f);
            foreach(var other in occupied) if(other.Intersects(bounds)) return false;
            return true;
        }
        static float Opacity(int family)
        {
            switch(family)
            {
                case 2: return .38f;
                case 3: return .45f;
                case 4: return .3f;
                case 5: return .55f;
                case 6: return .42f;
                case 7: return .9f;
                case 8: return .28f;
                case 9: return .25f;
                case 12: return .38f;
                default: return .65f;
            }
        }
        void Place(int family,RectInt cells,bool floor,bool rotated)
        {
            int variant=used[family]++%counts[family-1]+1;
            var sprite=sprites[Id(family,variant)]; var rect=DungeonLayout.WorldRect(cells);
            var size=rotated ? new Vector2(rect.height,rect.width) : rect.size;
            var go=new GameObject(Id(family,variant)); go.transform.SetParent(transform,false); go.transform.position=rect.center;
            go.transform.rotation=Quaternion.Euler(0,0,rotated ? 90 : 0);
            go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);
            SpriteFactory.Setup(go.AddComponent<SpriteRenderer>(),sprite,new Color(1,1,1,Opacity(family)),floor ? -7 : -3);
            occupied.Add(new Bounds(rect.center,rect.size));
        }
        bool TryEdge(DungeonLayout.Room room,int family,bool floor)
        {
            var r=room.Cells; var size=Footprint(family);
            for(int side=0;side<4;side++)
            {
                bool vertical=side>=2;
                int width=vertical ? size.y : size.x,height=vertical ? size.x : size.y;
                int min=(vertical ? r.yMin : r.xMin)+1,max=(vertical ? r.yMax-height : r.xMax-width)-1;
                for(int offset=min;offset<=max;offset++)
                {
                    int x=vertical ? (side==2 ? (floor ? r.xMin : r.xMin-width) : (floor ? r.xMax-width : r.xMax)) : offset;
                    int y=vertical ? offset : (side==0 ? (floor ? r.yMin : r.yMin-height) : (floor ? r.yMax-height : r.yMax));
                    var cells=new RectInt(x,y,width,height); if(!Fits(cells,floor)) continue;
                    bool edge=true; int length=vertical ? height : width;
                    for(int i=0;i<length;i++)
                    {
                        // Floor decals adjoin blocked stone; wall details adjoin open room floor.
                        int nx=vertical ? (side==2 ? (floor ? r.xMin-1 : r.xMin) : (floor ? r.xMax : r.xMax-1)) : x+i;
                        int ny=vertical ? y+i : (side==0 ? (floor ? r.yMin-1 : r.yMin) : (floor ? r.yMax : r.yMax-1));
                        if(DungeonLayout.Index(nx,ny)<0 || DungeonLayout.IsWater(nx,ny) || DungeonLayout.IsLowCover(nx,ny) || DungeonLayout.IsBaseFloor(nx,ny)==floor) { edge=false; break; }
                    }
                    if(!edge) continue;
                    Place(family,cells,floor,vertical); return true;
                }
            }
            return false;
        }
        void DressRooms()
        {
            for(int i=8;i<DungeonLayout.Rooms.Length;i++)
            {
                var room=DungeonLayout.Rooms[i];
                foreach(int family in wallFamilies[room.Sector]) TryEdge(room,family,false);
                foreach(int family in floorFamilies[room.Sector]) TryEdge(room,family,true);
            }
            DressCocoons();
        }
        void DressCocoons()
        {
            var dressed=new HashSet<int>();
            foreach(var frame in niches)
            {
                Vector2 point=frame.transform.position; int nearest=-1; float distance=float.PositiveInfinity;
                for(int i=8;i<DungeonLayout.Rooms.Length;i++)
                {
                    float squared=(DungeonLayout.Rooms[i].Center-point).sqrMagnitude;
                    if(squared<distance) { nearest=i; distance=squared; }
                }
                if(nearest<0 || DungeonLayout.Rooms[nearest].Sector!=2 || dressed.Contains(nearest)) continue;
                var size=Vector2.one*(.45f*DungeonLayout.CellSize);
                var rect=new Rect(point-size*.5f,size); if(Reserved(rect)) continue;
                var bounds=new Bounds(point,size+Vector2.one*.04f); bool clear=true;
                foreach(var other in occupied)
                    if(!other.Equals(frame.bounds) && other.Intersects(bounds)) { clear=false; break; }
                if(!clear) continue; // Existing urns, bones and grave markers retain their recesses.
                int variant=used[5]++%counts[4]+1; var sprite=sprites[Id(5,variant)];
                var go=new GameObject(Id(5,variant)); go.transform.SetParent(transform,false); go.transform.position=point;
                go.transform.rotation=frame.transform.rotation;
                go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);
                SpriteFactory.Setup(go.AddComponent<SpriteRenderer>(),sprite,new Color(1,1,1,Opacity(5)),-3);
                occupied.Add(bounds); dressed.Add(nearest); // At most one small cocoon in each ossuary room.
            }
        }
        static bool InsideRoom(int x,int y)
        {
            foreach(var room in DungeonLayout.Rooms) if(room.Cells.Contains(new Vector2Int(x,y))) return true;
            return false;
        }
        internal static bool AtDoorApproach(int x,int y)
        {
            foreach(var room in DungeonLayout.Rooms)
            {
                var r=room.Cells;
                if(x<r.xMin-2 || x>r.xMax+1 || y<r.yMin-2 || y>r.yMax+1) continue;
                // Keep both sides of every opening, including corridor junctions
                // on room boundaries, free of additional dressing.
                for(int d=0;d<4;d++)
                {
                    var delta=d==0 ? Vector2Int.up : d==1 ? Vector2Int.right : d==2 ? Vector2Int.down : Vector2Int.left;
                    for(int i=-1;i<=1;i++)
                    {
                        int bx=delta.x==0 ? x+i : (delta.x>0 ? r.xMax-1 : r.xMin);
                        int by=delta.y==0 ? y+i : (delta.y>0 ? r.yMax-1 : r.yMin);
                        if(!r.Contains(new Vector2Int(bx,by))) continue;
                        if(Mathf.Abs(x-bx)>2 || Mathf.Abs(y-by)>2) continue;
                        if(DungeonLayout.IsBaseFloor(bx,by) && DungeonLayout.IsBaseFloor(bx+delta.x,by+delta.y)) return true;
                    }
                }
            }
            return false;
        }
        bool TryCluster(int x,int y,int sector,int seed,float spacing)
        {
            var cells=new RectInt(x,y,1,1);
            if(!Fits(cells,true) || AtDoorApproach(x,y)) return false;
            var rect=DungeonLayout.WorldRect(cells);
            foreach(var centre in clusterCentres) if(((Vector2)rect.center-centre).sqrMagnitude<spacing*spacing) return false;
            var group=new GameObject("DressingCluster_"+clusterCentres.Count.ToString("00"));
            group.transform.SetParent(transform,false); group.transform.position=rect.center;
            var families=clusterFamilies[Mathf.Clamp(sector,0,clusterFamilies.Length-1)];
            int pieces=2+seed%2;
            for(int i=0;i<pieces;i++)
            {
                int family=families[(seed+i)%families.Length];
                int variant=1+(seed+i*7)%(family==0 ? 3 : counts[family-1]);
                string id=family==0 ? "CAT-P10_v"+variant.ToString("00") : Id(family,variant);
                if(!sprites.TryGetValue(id,out var sprite)) sprite=sprites[Id(12,variant)];
                var piece=new GameObject(id); piece.transform.SetParent(group.transform,false);
                piece.transform.localPosition=new Vector3(i==0 ? -.22f : .20f,i==2 ? .22f : -.12f,0)*DungeonLayout.CellSize;
                piece.transform.localRotation=Quaternion.Euler(0,0,90*((seed+i)%4));
                float size=(i==0 ? .48f : .30f)*DungeonLayout.CellSize;
                piece.transform.localScale=new Vector3(size/sprite.bounds.size.x,size/sprite.bounds.size.y,1);
                // Flat, muted fragments rather than silhouettes of usable items.
                SpriteFactory.Setup(piece.AddComponent<SpriteRenderer>(),sprite,new Color(.86f,.84f,.78f,family==0 ? .7f : .58f),-7);
            }
            occupied.Add(new Bounds(rect.center,rect.size)); clusterCentres.Add(rect.center);
            return true;
        }
        void DressClusters()
        {
            const int groupsPerRoom=2,corridorTarget=24;
            int roomTarget=DungeonLayout.Rooms.Length*groupsPerRoom;
            int roomGroups=0,corridorGroups=0;
            for(int i=0;i<DungeonLayout.Rooms.Length;i++)
            {
                var room=DungeonLayout.Rooms[i]; var r=room.Cells; int placed=0;
                // Alternate opposite edges; all candidates are flat floor details.
                for(int pass=0;pass<4 && placed<groupsPerRoom;pass++)
                    for(int offset=1;offset<Mathf.Max(r.width,r.height)-1 && placed<groupsPerRoom;offset++)
                    {
                        int x=pass<2 ? r.xMin+offset : (pass==2 ? r.xMin+1 : r.xMax-2);
                        int y=pass<2 ? (pass==0 ? r.yMin+1 : r.yMax-2) : r.yMin+offset;
                        if(!r.Contains(new Vector2Int(x,y))) continue;
                        if(TryCluster(x,y,room.Sector,i*11+placed,3)) { placed++; roomGroups++; break; }
                    }
            }
            // Deterministic interleaved scan distributes groups across the map,
            // rather than filling one corridor before visiting the others.
            int total=DungeonLayout.GridWidth*DungeonLayout.GridHeight;
            for(int n=0;n<total && corridorGroups<corridorTarget;n++)
            {
                int index=(n*7919+431)%total,x=index%DungeonLayout.GridWidth,y=index/DungeonLayout.GridWidth;
                if(InsideRoom(x,y) || !DungeonLayout.IsBaseFloor(x,y)) continue;
                bool edge=false;
                for(int d=0;d<4;d++)
                {
                    int nx=x+(d==0 ? 1 : d==1 ? -1 : 0),ny=y+(d==2 ? 1 : d==3 ? -1 : 0);
                    if(DungeonLayout.Index(nx,ny)>=0 && !DungeonLayout.IsBaseFloor(nx,ny) && !DungeonLayout.IsWater(nx,ny) && !DungeonLayout.IsLowCover(nx,ny)) edge=true;
                }
                if(edge && TryCluster(x,y,0,index,4)) corridorGroups++;
            }
            // These are optional density targets, not required map content. Reserved
            // approaches (including the canal) and existing art take precedence.
            // Asset failures remain warnings in Build; fewer safe placements do not.
            if(roomGroups<roomTarget || corridorGroups<corridorTarget)
                Debug.Log("Catacumbas: decoração opcional: "+roomGroups+"/"+roomTarget+
                    " grupos de sala e "+corridorGroups+"/"+corridorTarget+
                    " de corredor. Grupos sem espaço seguro omitidos; reservas preservadas.");
        }
        void OnDestroy() { foreach(var sprite in owned) if(sprite!=null) Destroy(sprite); }
    }
}
