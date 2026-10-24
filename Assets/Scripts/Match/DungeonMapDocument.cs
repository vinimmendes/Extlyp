using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace CyberExtraction.Match
{
    public enum DungeonCellKind { Stone, Floor, Water, LowCover }

    [Serializable] public sealed class DungeonMapDocument
    {
        public int version=1, width=128, height=128;
        public float cellSize=1.06f;
        public string contentHash,artHash;
        public string artResource;
        public Color floorTint=new Color(.82f,.85f,.86f,1);
        public int[] cells, sectors;
        public DungeonRoomRecord[] rooms;
        public DungeonRectRecord[] regions;
        public DungeonPointRecord[] points;
        public DungeonVisualRecord[] visuals=Array.Empty<DungeonVisualRecord>();
        public DungeonVisualRecord[] functionalVisuals=Array.Empty<DungeonVisualRecord>();
        public string[] requiredFunctionalKeys=Array.Empty<string>();
        public DungeonArtReplacement[] artReplacements=Array.Empty<DungeonArtReplacement>();
        public DungeonSurfaceRecord[] surfaces=Array.Empty<DungeonSurfaceRecord>();
        public int[] dressedRooms=Array.Empty<int>();
        public DungeonFidelityRecord[] fidelity=Array.Empty<DungeonFidelityRecord>();
        public string legacyMigration;

        public string Fingerprint()
        {
            // Hash the complete deterministic document without including its own hash.
            var copy=JsonUtility.FromJson<DungeonMapDocument>(JsonUtility.ToJson(this));
            copy.contentHash=null;
            using(var sha=SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(copy)))).Replace("-","").ToLowerInvariant();
        }
        [Serializable] sealed class RoomComparison
        {
            public string art; public Color floorTint; public DungeonRoomRecord room; public int[] cells;
            public DungeonVisualRecord[] visuals,functionalVisuals; public DungeonSurfaceRecord[] surfaces; public DungeonPointRecord[] points;
            public DungeonRectRecord[] regions;
            public DungeonArtReplacement[] artReplacements;
        }
        public string RoomFingerprint(int roomId)
        {
            if(rooms==null || cells==null || cells.Length!=16384) return "";
            var room=Array.Find(rooms,r=>r!=null && r.id==roomId); if(room==null) return "";
            var rect=new Rect((room.x-65)*cellSize,(room.y-65)*cellSize,(room.width+2)*cellSize,(room.height+2)*cellSize);
            var terrain=new List<int>();
            for(int y=Mathf.Max(0,room.y-1);y<Mathf.Min(128,room.y+room.height+1);y++)
                for(int x=Mathf.Max(0,room.x-1);x<Mathf.Min(128,room.x+room.width+1);x++) terrain.Add(cells[y*128+x]);
            var visible=new List<DungeonVisualRecord>();
            foreach(var v in visuals??Array.Empty<DungeonVisualRecord>())
            {
                float radians=v.angle*Mathf.Deg2Rad;
                var size=new Vector2(Mathf.Abs(Mathf.Cos(radians))*v.size.x+Mathf.Abs(Mathf.Sin(radians))*v.size.y,
                    Mathf.Abs(Mathf.Sin(radians))*v.size.x+Mathf.Abs(Mathf.Cos(radians))*v.size.y);
                if(v.room==roomId || rect.Overlaps(new Rect(v.position-size*.5f,size))) visible.Add(v);
            }
            var painted=new List<DungeonSurfaceRecord>();
            foreach(var s in surfaces??Array.Empty<DungeonSurfaceRecord>()) if(rect.Contains(new Vector2((s.x-63.5f)*cellSize,(s.y-63.5f)*cellSize))) painted.Add(s);
            var localPoints=new List<DungeonPointRecord>();
            foreach(var p in points??Array.Empty<DungeonPointRecord>()) if(rect.Contains(p.position)) localPoints.Add(p);
            var localRegions=new List<DungeonRectRecord>();
            foreach(var r in regions??Array.Empty<DungeonRectRecord>())
            {
                bool cellRegion=r.id.StartsWith("Canal") || r.id.StartsWith("Pillar/") || r.id.StartsWith("Cover/");
                var bounds=cellRegion ? new Rect((r.x-64)*cellSize,(r.y-64)*cellSize,r.width*cellSize,r.height*cellSize) : r.Rect;
                if(rect.Overlaps(bounds)) localRegions.Add(r);
            }
            var functional=new List<DungeonVisualRecord>();
            foreach(var v in functionalVisuals??Array.Empty<DungeonVisualRecord>())
                if(v.room==roomId || rect.Overlaps(new Rect(v.position-v.size*.5f,v.size))) functional.Add(v);
            var comparison=new RoomComparison {art=artHash,floorTint=floorTint,room=room,cells=terrain.ToArray(),visuals=visible.ToArray(),functionalVisuals=functional.ToArray(),surfaces=painted.ToArray(),points=localPoints.ToArray(),regions=localRegions.ToArray(),artReplacements=artReplacements};
            using(var sha=SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(comparison))));
        }
        public List<string> Problems(bool requireFidelity)
        {
            var errors=new List<string>();
            if(version!=1 || width!=128 || height!=128 || Mathf.Abs(cellSize-1.06f)>.00001f)
                errors.Add("Formato ou escala incompatível com as Catacumbas.");
            if(float.IsNaN(floorTint.r) || float.IsNaN(floorTint.g) || float.IsNaN(floorTint.b) ||
                float.IsInfinity(floorTint.r) || float.IsInfinity(floorTint.g) || float.IsInfinity(floorTint.b) || floorTint.a!=1)
                errors.Add("A tonalidade do piso precisa ser finita e opaca.");
            if(cells==null || cells.Length!=128*128 || sectors==null || sectors.Length!=128*128)
                errors.Add("Máscaras de terreno/setor incompletas.");
            else for(int i=0;i<cells.Length;i++)
                if(cells[i]<0 || cells[i]>3 || sectors[i]<-1 || sectors[i]>7)
                { errors.Add("Célula ou setor inválido: "+i); break; }
            var roomIds=new HashSet<int>();
            if(rooms==null || rooms.Length!=24) errors.Add("Preservar as 24 salas.");
            else foreach(var room in rooms)
                if(room==null || room.id<0 || room.id>=24 || !roomIds.Add(room.id) || room.sector<0 || room.sector>7 ||
                    room.width<=0 || room.height<=0 || room.x<0 || room.y<0 || room.x+room.width>128 || room.y+room.height>128)
                    errors.Add("Sala inválida ou repetida.");
            var pointIds=new HashSet<string>();
            if(points==null) errors.Add("Pontos funcionais ausentes.");
            else foreach(var point in points)
            {
                if(point==null || string.IsNullOrEmpty(point.id) || !pointIds.Add(point.id) || !Finite(point.position))
                { errors.Add("Ponto funcional inválido ou repetido."); continue; }
                if(point.id.StartsWith("Spawn/") || point.id.StartsWith("Loot/") || point.id.StartsWith("Encounter/") || point.id.StartsWith("Breakable/"))
                {
                    int x=Mathf.FloorToInt(point.position.x/cellSize+64), y=Mathf.FloorToInt(point.position.y/cellSize+64);
                    if(x<0 || x>=128 || y<0 || y>=128 || cells==null || cells.Length!=16384 || cells[y*128+x]!=1)
                        errors.Add("Ponto fora de piso transitável: "+point.id);
                    if(point.id.StartsWith("Breakable/") && (Vector2.Distance(point.position,new Vector2((x-64+.5f)*cellSize,(y-64+.5f)*cellSize))>.001f))
                        errors.Add("Quebrável precisa ficar no centro da célula: "+point.id);
                    if(point.id.StartsWith("Breakable/") && (point.variant<1 || point.variant>3 ||
                        (point.family!="Urn" && point.family!="Crate" && point.family!="Barrel") || float.IsNaN(point.angle) || float.IsInfinity(point.angle)))
                        errors.Add("Variante/família/rotação de quebrável inválida: "+point.id);
                }
            }
            for(int i=0;i<8;i++) Require(pointIds,"Spawn/"+i,errors);
            for(int i=0;i<3;i++) Require(pointIds,"Exit/"+i,errors);
            for(int i=0;i<4;i++) Require(pointIds,"Mechanism/"+i,errors);
            for(int i=0;i<24;i++) Require(pointIds,"Exploration/"+i,errors);
            for(int i=0;i<24;i++) Require(pointIds,"Loot/"+i,errors);
            for(int i=0;i<18;i++) Require(pointIds,"Encounter/"+i,errors);
            for(int i=0;i<64;i++) Require(pointIds,"Breakable/"+i,errors);
            Require(pointIds,"CrankPedestal",errors); Require(pointIds,"KeyPedestal",errors); Require(pointIds,"BoatEnd",errors); Require(pointIds,"LiftEnd",errors);
            var regionIds=new HashSet<string>();
            if(regions==null) errors.Add("Regiões funcionais ausentes.");
            else foreach(var region in regions)
                if(region==null || string.IsNullOrEmpty(region.id) || !regionIds.Add(region.id) ||
                    !Finite(new Vector2(region.x,region.y)) || !Finite(new Vector2(region.width,region.height)) || region.width<=0 || region.height<=0)
                    errors.Add("Região inválida ou repetida.");
            foreach(var id in new[]{"CanalArea","CanalEastRiver","CanalSouthRiver","CanalBridge","CanalDetour","CanalBoarding","StairBorder/0","StairBorder/1","StairBorder/2"}) Require(regionIds,id,errors);
            foreach(var region in regions??Array.Empty<DungeonRectRecord>())
                if(region!=null && (region.id.StartsWith("Canal") || region.id.StartsWith("Pillar/") || region.id.StartsWith("Cover/")) &&
                    (region.x<0 || region.y<0 || region.x+region.width>128 || region.y+region.height>128 ||
                    Mathf.Abs(region.x-Mathf.Round(region.x))>.001f || Mathf.Abs(region.y-Mathf.Round(region.y))>.001f ||
                    Mathf.Abs(region.width-Mathf.Round(region.width))>.001f || Mathf.Abs(region.height-Mathf.Round(region.height))>.001f))
                    errors.Add("Região física fora da grade: "+region.id);
            var visualIds=new HashSet<string>();
            var allVisuals=new List<DungeonVisualRecord>(visuals??Array.Empty<DungeonVisualRecord>()); allVisuals.AddRange(functionalVisuals??Array.Empty<DungeonVisualRecord>());
            foreach(var visual in allVisuals)
                if(visual==null || string.IsNullOrEmpty(visual.id) || !visualIds.Add(visual.id) || string.IsNullOrEmpty(visual.asset) ||
                    !Finite(visual.position) || !Finite(visual.size) || visual.size.x<=0 || visual.size.y<=0 || float.IsNaN(visual.angle) || float.IsInfinity(visual.angle))
                    errors.Add("Peça visual inválida ou repetida.");
            var slots=new HashSet<string>();
            foreach(var replacement in artReplacements??Array.Empty<DungeonArtReplacement>())
                if(replacement==null || string.IsNullOrEmpty(replacement.slot) || string.IsNullOrEmpty(replacement.asset) || !slots.Add(replacement.slot)) errors.Add("Substituição de estado inválida ou repetida.");
            foreach(var surface in surfaces??Array.Empty<DungeonSurfaceRecord>())
                if(surface==null || surface.x<0 || surface.y<0 || surface.x>=128 || surface.y>=128 || string.IsNullOrEmpty(surface.asset)) errors.Add("Superfície inválida.");
            if(requireFidelity)
            {
                var actualFunctions=new HashSet<string>();
                foreach(var v in functionalVisuals??Array.Empty<DungeonVisualRecord>()) if(v!=null) actualFunctions.Add(v.id);
                var expectedFunctions=new HashSet<string>();
                if(requiredFunctionalKeys==null || requiredFunctionalKeys.Length==0) errors.Add("Identidades dos conjuntos funcionais ausentes.");
                else foreach(var key in requiredFunctionalKeys)
                {
                    if(string.IsNullOrEmpty(key) || !expectedFunctions.Add(key)) errors.Add("Identidade funcional obrigatória inválida/repetida.");
                    else Require(actualFunctions,key,errors);
                }
                var checkedRooms=new HashSet<int>();
                foreach(var entry in fidelity??Array.Empty<DungeonFidelityRecord>())
                {
                    if(entry==null || !checkedRooms.Add(entry.room) || entry.room<0 || entry.room>=24)
                    { errors.Add("Revisão de fidelidade inválida."); continue; }
                    if(!entry.resolved || string.IsNullOrWhiteSpace(entry.comparison) || !string.IsNullOrWhiteSpace(entry.pendingDeviations))
                        errors.Add("Replanejar desvios antes de publicar: R"+entry.room.ToString("00"));
                    else if(entry.comparisonHash!=RoomFingerprint(entry.room)) errors.Add("A sala mudou depois da comparação; revisar R"+entry.room.ToString("00"));
                }
                if(checkedRooms.Count!=24) errors.Add("Comparar as 24 salas com os conceitos antes de publicar.");
                if(legacyMigration!="reconciled") errors.Add("Conciliar os ajustes visuais anteriores antes de publicar.");
            }
            return errors;
        }
        static bool Finite(Vector2 p)=>!float.IsNaN(p.x) && !float.IsNaN(p.y) && !float.IsInfinity(p.x) && !float.IsInfinity(p.y);
        static void Require(HashSet<string> ids,string id,List<string> errors) { if(!ids.Contains(id)) errors.Add("Identidade obrigatória ausente: "+id); }
    }
    [Serializable] public sealed class DungeonRoomRecord { public int id,sector,x,y,width,height; public string name; }
    [Serializable] public sealed class DungeonPointRecord
    { public string id,family; public int variant; public float angle; public Vector2 position; }
    [Serializable] public sealed class DungeonRectRecord
    {
        public string id; public float x,y,width,height;
        public Rect Rect=>new Rect(x,y,width,height);
    }
    [Serializable] public sealed class DungeonVisualRecord
    {
        public string id,asset,group; public int room=-1,order=-3;
        public Vector2 position,size=Vector2.one; public float angle;
        public bool flipX,flipY;
        public bool hidden,carrier;
        public Color color=Color.white;
    }
    [Serializable] public sealed class DungeonSurfaceRecord { public int x,y,order=-7; public string asset; public float angle; public bool flipX,flipY; public Color color=Color.white; }
    [Serializable] public sealed class DungeonFidelityRecord
    {
        public int room; public string concept,comparison,comparisonHash,pendingDeviations,replan; public bool resolved;
    }
    [Serializable] public sealed class DungeonArtReplacement { public string slot,asset; }
}
