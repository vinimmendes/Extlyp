using System;
using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Match
{
    // Authored cosmetic groups, including the original pilots. No gameplay state.
    public sealed class DungeonNarrativePilot : MonoBehaviour
    {
        [Serializable] sealed class Manifest { public int version; public Art[] assets; public Group[] groups; }
        [Serializable] sealed class Group { public string id; public int room; public Alternative[] alternatives; }
        [Serializable] sealed class Alternative { public Piece[] pieces; }
        [Serializable] sealed class Art { public string id,resource; public float x,y,width,height; }
        [Serializable] sealed class Piece
        {
            public string id,art,support,layerGroup,zone;
            public int room,order;
            public float x,y,width,height,angle,opacity;
        }
        readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        readonly List<Sprite> owned=new List<Sprite>();
        readonly List<Bounds> occupied=new List<Bounds>();
        readonly List<string> layers=new List<string>();
        SpriteRenderer[] previous;

        public static void Build(Transform world)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var root=new GameObject("DungeonNarrativeDressing"); root.transform.SetParent(world,false);
            var pilot=root.AddComponent<DungeonNarrativePilot>();
            try { pilot.LoadAndPlace(); }
            catch(Exception exception)
            {
                root.SetActive(false); DungeonArtLifetime.Release(root);
                Debug.LogWarning("Catacumbas: composições decorativas omitidas; cenário anterior preservado. "+exception.Message);
            }
        }
        void LoadAndPlace()
        {
            var json=Resources.Load<TextAsset>("Environment/Dungeon/Narrative/NarrativeDistribution");
            if(json==null) throw new InvalidOperationException("Manifesto de distribuição ausente.");
            var manifest=JsonUtility.FromJson<Manifest>(json.text);
            if(manifest==null || manifest.version!=1 || manifest.assets==null || manifest.groups==null)
                throw new InvalidOperationException("Manifesto de distribuição inválido.");
            foreach(var entry in manifest.assets)
            {
                var texture=Resources.Load<Texture2D>(entry.resource);
                if(texture==null) throw new InvalidOperationException("Arte ausente: "+entry.resource);
                int x=Mathf.Clamp(Mathf.FloorToInt(entry.x*texture.width),0,texture.width-1);
                int y=Mathf.Clamp(Mathf.FloorToInt(entry.y*texture.height),0,texture.height-1);
                int w=Mathf.Clamp(Mathf.CeilToInt(entry.width*texture.width),1,texture.width-x);
                int h=Mathf.Clamp(Mathf.CeilToInt(entry.height*texture.height),1,texture.height-y);
                var sprite=Sprite.Create(texture,new Rect(x,y,w,h),new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect);
                owned.Add(sprite); sprites.Add(entry.id,sprite);
            }
            previous=transform.parent.GetComponentsInChildren<SpriteRenderer>();
            var omitted=new List<string>(); int placed=0;
            foreach(var group in manifest.groups)
            {
                if(group==null || string.IsNullOrEmpty(group.id) || group.alternatives==null)
                    throw new InvalidOperationException("Grupo decorativo inválido.");
                Alternative accepted=null;
                foreach(var alternative in group.alternatives)
                {
                    if(FitsGroup(group,alternative)) { accepted=alternative; break; }
                }
                if(accepted==null) { omitted.Add(group.id); continue; }
                foreach(var piece in accepted.pieces) Place(piece);
                placed++;
            }
            if(omitted.Count>0) Debug.Log("Catacumbas: "+placed+"/"+manifest.groups.Length+
                " composições seguras; grupos opcionais omitidos: "+string.Join(", ",omitted)+". Reservas preservadas.");
        }
        static Rect Footprint(Piece piece)
        {
            var centre=DungeonLayout.CanalPoint(piece.x,piece.y);
            var size=new Vector2(piece.width,piece.height)*DungeonLayout.CellSize;
            if(Mathf.Abs(Mathf.DeltaAngle(0,piece.angle))==90) size=new Vector2(size.y,size.x);
            return new Rect(centre-size*.5f,size);
        }
        bool FitsGroup(Group group,Alternative alternative)
        {
            if(alternative==null || alternative.pieces==null || alternative.pieces.Length==0)
                throw new InvalidOperationException("Alternativa decorativa vazia: "+group.id);
            int start=occupied.Count;
            try
            {
                foreach(var piece in alternative.pieces)
                {
                    if(piece==null || piece.room!=group.room || piece.room< -1 || piece.room>=DungeonLayout.Rooms.Length ||
                       string.IsNullOrEmpty(piece.layerGroup) || piece.width<=0 || piece.height<=0 ||
                       !sprites.ContainsKey(piece.art) || float.IsNaN(piece.x) || float.IsNaN(piece.y) ||
                       float.IsInfinity(piece.x) || float.IsInfinity(piece.y) || piece.angle%90!=0)
                        throw new InvalidOperationException("Peça decorativa inválida: "+group.id);
                    var rect=Footprint(piece);
                    if(!Fits(piece,rect)) return false;
                    // Reserve the entire composition before committing any renderer.
                    occupied.Add(new Bounds(rect.center,rect.size)); layers.Add(piece.layerGroup);
                }
                return true;
            }
            finally
            {
                occupied.RemoveRange(start,occupied.Count-start); layers.RemoveRange(start,layers.Count-start);
            }
        }
        void Place(Piece piece)
        {
            var sprite=sprites[piece.art]; var rect=Footprint(piece);
            var size=new Vector2(piece.width,piece.height)*DungeonLayout.CellSize;
            var go=new GameObject("CAT-Narrative_"+piece.id); go.transform.SetParent(transform,false);
            go.transform.position=rect.center; go.transform.rotation=Quaternion.Euler(0,0,piece.angle);
            float scale=Mathf.Min(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y);
            go.transform.localScale=new Vector3(scale,scale,1);
            SpriteFactory.Setup(go.AddComponent<SpriteRenderer>(),sprite,new Color(1,1,1,Mathf.Clamp01(piece.opacity)),piece.order);
            occupied.Add(new Bounds(rect.center,rect.size)); layers.Add(piece.layerGroup);
        }
        static bool Near(Rect rect,Vector2 point,float radius)
        {
            var nearest=new Vector2(Mathf.Clamp(point.x,rect.xMin,rect.xMax),Mathf.Clamp(point.y,rect.yMin,rect.yMax));
            return (nearest-point).sqrMagnitude<radius*radius;
        }
        static bool Protected(Rect rect)
        {
            foreach(var p in DungeonLayout.Spawns) if(Near(rect,p,3)) return true;
            foreach(var p in DungeonLayout.ExitPositions) if(Near(rect,p,5)) return true;
            foreach(var p in DungeonContent.Loot) if(Near(rect,p.Position,2.8f)) return true;
            foreach(var p in DungeonContent.Encounters) if(Near(rect,p.Position,2)) return true;
            for(int i=0;i<4;i++) if(Near(rect,DungeonContent.Mechanism(i),2.8f)) return true;
            if(Near(rect,DungeonContent.CrankPedestal,3) || Near(rect,DungeonContent.KeyPedestal,3)) return true;
            // Southward sweep of the already rotated boat, plus a visual margin.
            var boat=DungeonLayout.ExitPositions[2]; var end=boat+DungeonLayout.CanalDeparture;
            var sweep=Rect.MinMaxRect(Mathf.Min(boat.x,end.x)-1.5f*DungeonLayout.CellSize,
                Mathf.Min(boat.y,end.y)-2.5f*DungeonLayout.CellSize,
                Mathf.Max(boat.x,end.x)+1.5f*DungeonLayout.CellSize,
                Mathf.Max(boat.y,end.y)+2.5f*DungeonLayout.CellSize);
            return rect.Overlaps(sweep) || DungeonBreakableLayout.Reserved(rect);
        }
        bool Fits(Piece piece,Rect rect)
        {
            if(Protected(rect)) return false;
            var gallery=DungeonLayout.WorldRect(DungeonLayout.CanalDetour);
            bool inRoom=piece.room>=0 && Contains(DungeonLayout.WorldRect(DungeonLayout.Rooms[piece.room].Cells),rect);
            if(piece.room>=0 && piece.support!="Stone" && !inRoom) return false;
            // Only explicit quay/gallery anchors may enter the broad canal reservation.
            // Automatic Props/Details/SectorArt keep their existing CanalReserved guard.
            if(DungeonLayout.CanalReserved(rect) &&
                !((piece.room==13 && piece.support=="Floor" && inRoom) ||
                  (piece.room==-1 && piece.zone=="CanalDetour" && piece.support=="Floor" && Contains(gallery,rect)))) return false;
            int minX=Mathf.FloorToInt(rect.xMin/DungeonLayout.CellSize+64+.0001f);
            int minY=Mathf.FloorToInt(rect.yMin/DungeonLayout.CellSize+64+.0001f);
            int maxX=Mathf.CeilToInt(rect.xMax/DungeonLayout.CellSize+64-.0001f);
            int maxY=Mathf.CeilToInt(rect.yMax/DungeonLayout.CellSize+64-.0001f);
            if(piece.support!="Floor" && piece.support!="Stone" && piece.support!="Cover") return false;
            for(int y=minY;y<maxY;y++) for(int x=minX;x<maxX;x++)
            {
                if(DungeonLayout.Index(x,y)<0 || DungeonLayout.IsWater(x,y)) return false;
                if(piece.room==-1)
                    foreach(var room in DungeonLayout.Rooms) if(room.Cells.Contains(new Vector2Int(x,y))) return false;
                if(piece.support=="Floor" && (!DungeonLayout.IsBaseFloor(x,y) || DungeonDetails.AtDoorApproach(x,y))) return false;
                if(piece.support=="Stone" && (DungeonLayout.IsBaseFloor(x,y) || DungeonLayout.IsLowCover(x,y))) return false;
                if(piece.support=="Cover" && !DungeonLayout.IsLowCover(x,y)) return false;
            }
            var bounds=new Bounds(rect.center,rect.size);
            for(int i=0;i<occupied.Count;i++)
                if(layers[i]!=piece.layerGroup && occupied[i].Intersects(bounds)) return false;
            foreach(var renderer in previous)
            {
                if(!renderer.enabled || !renderer.name.StartsWith("CAT-",StringComparison.Ordinal) || !renderer.bounds.Intersects(bounds)) continue;
                // Explicit surface overlays: masonry furniture and a shroud on an existing tomb.
                if(piece.support=="Stone" && IsMasonry(renderer.name)) continue;
                if(piece.support=="Cover" && (renderer.name.StartsWith("CAT-P01_",StringComparison.Ordinal) ||
                    renderer.name.StartsWith("CAT-P02_",StringComparison.Ordinal))) continue;
                return false;
            }
            return true;
        }
        static bool Contains(Rect outer,Rect inner) => inner.xMin>=outer.xMin && inner.yMin>=outer.yMin &&
            inner.xMax<=outer.xMax && inner.yMax<=outer.yMax;
        static bool IsMasonry(string name)
        {
            for(int i=1;i<=7;i++) if(name.StartsWith("CAT-A"+i.ToString("00")+"_",StringComparison.Ordinal)) return true;
            return false;
        }
        void OnDestroy() { foreach(var sprite in owned) if(sprite!=null) DungeonArtLifetime.Release(sprite); }
    }
}
