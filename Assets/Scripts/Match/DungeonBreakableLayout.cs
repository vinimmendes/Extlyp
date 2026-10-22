using System;
using UnityEngine;

namespace CyberExtraction.Match
{
    // Shared authored positions/art; runtime state belongs to each network object.
    public static class DungeonBreakableLayout
    {
        [Serializable] public sealed class Candidate
        {
            public int id,room,cellX,cellY,variant,angle;
            public string group,family;
            public float x,y;
            public Vector2 Position => new Vector2(x,y);
        }
        [Serializable] public sealed class ArtEntry
        {
            public string family,state,resource;
            public int variant;
            public SpriteRect rect; // Normalized, bottom-left origin.
        }
        [Serializable] public sealed class SpriteRect
        {
            public float x,y,width,height;
            public Rect Value => new Rect(x,y,width,height);
        }
        [Serializable] public sealed class Catalog
        {
            public int version;
            public float cellSize,worldFootprint;
            public Candidate[] candidates;
            public ArtEntry[] art;
        }
        static Catalog cached;
        static bool attempted;
        public static Catalog Load()
        {
            if(attempted) return cached;
            attempted=true;
            var source=Resources.Load<TextAsset>("Environment/Dungeon/Breakables/BreakablesCatalog");
            if(source==null) return null;
            var data=JsonUtility.FromJson<Catalog>(source.text);
            if(data==null || data.version!=1 || data.candidates==null || data.candidates.Length!=64 ||
                data.art==null || data.art.Length!=18 || Mathf.Abs(data.cellSize-DungeonLayout.CellSize)>.001f ||
                data.worldFootprint<=0 || data.worldFootprint>DungeonLayout.CellSize)
            { Debug.LogWarning("Catacumbas: catalogo de quebraveis invalido; reservas omitidas."); return null; }
            var roomCounts=new int[DungeonLayout.Rooms.Length]; int corridors=0;
            for(int i=0;i<data.candidates.Length;i++)
            {
                var point=data.candidates[i];
                if(point==null || !DungeonLayout.IsBaseFloor(point.cellX,point.cellY) || DungeonLayout.IsWater(point.cellX,point.cellY) ||
                    DungeonLayout.IsLowCover(point.cellX,point.cellY) || point.variant<1 || point.variant>3 ||
                    point.id!=i || float.IsNaN(point.x) || float.IsNaN(point.y) ||
                    (point.Position-DungeonLayout.ToWorld(new Vector2(point.cellX+.5f,point.cellY+.5f))).sqrMagnitude>.00001f ||
                    (point.family!="Urn" && point.family!="Crate" && point.family!="Barrel"))
                { Debug.LogWarning("Catacumbas: candidato de quebravel incompativel com o mapa; reservas omitidas."); return null; }
                if(point.group=="Corridor" && point.room==-1) corridors++;
                else if(point.room>=0 && point.room<roomCounts.Length &&
                    (point.group=="Entry" && point.room<8 || point.group=="Room" && point.room>=8)) roomCounts[point.room]++;
                else { Debug.LogWarning("Catacumbas: grupo/sala de quebravel invalido; reservas omitidas."); return null; }
            }
            if(corridors!=8) return InvalidDistribution();
            for(int room=0;room<roomCounts.Length;room++) if(roomCounts[room]!=(room<8 ? 1 : 3)) return InvalidDistribution();
            cached=data; return cached;
        }
        static Catalog InvalidDistribution()
        { Debug.LogWarning("Catacumbas: distribuicao de candidatos invalida; reservas omitidas."); return null; }
        public static bool Reserved(Rect rect)
        {
            var data=Load(); if(data==null) return false;
            float size=data.worldFootprint+.2f;
            foreach(var point in data.candidates)
                if(rect.Overlaps(new Rect(point.Position-Vector2.one*(size*.5f),Vector2.one*size))) return true;
            return false;
        }
    }
}
