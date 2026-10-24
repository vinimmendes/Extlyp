using System;
using System.Collections.Generic;
using UnityEngine;

namespace CyberExtraction.Match
{
    // Immutable packaged content. Never accepts a client-authored map or a live editor draft.
    public static class DungeonMapPublication
    {
        public const string ResourcePath="Environment/Dungeon/ReworkComplete/Published";
        static bool attempted;
        static DungeonMapDocument current;
        static DungeonArtCatalog publishedArt;
        static readonly Dictionary<string,Vector2> points=new Dictionary<string,Vector2>();
        static readonly Dictionary<string,Rect> regions=new Dictionary<string,Rect>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Invalidate()
        { attempted=false; current=null; publishedArt=null; points.Clear(); regions.Clear(); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void PrepareSession()=>DungeonLayout.EnsurePublishedContent();
        public static DungeonMapDocument Current
        {
            get
            {
                if(attempted) return current;
                attempted=true;
                var source=Resources.Load<TextAsset>(ResourcePath);
                if(source==null) return null;
                try
                {
                    var data=JsonUtility.FromJson<DungeonMapDocument>(source.text);
                    if(data==null) throw new InvalidOperationException("Documento ausente.");
                    var errors=data.Problems(true);
                    if(errors.Count>0 || string.IsNullOrEmpty(data.contentHash) || data.Fingerprint()!=data.contentHash)
                        throw new InvalidOperationException(errors.Count>0 ? string.Join("; ",errors) : "Identificação de conteúdo divergente.");
                    if(string.IsNullOrEmpty(data.artResource) || !data.artResource.StartsWith("Environment/Dungeon/ReworkComplete/Publications/",StringComparison.Ordinal))
                        throw new InvalidOperationException("Catálogo da arte publicada ausente.");
                    publishedArt=Resources.Load<DungeonArtCatalog>(data.artResource);
                    if(publishedArt==null || publishedArt.ContentHash!=data.artHash)
                        throw new InvalidOperationException("Arte da versão publicada indisponível ou incompatível.");
                    foreach(var p in data.points) points.Add(p.id,p.position);
                    foreach(var r in data.regions) regions.Add(r.id,r.Rect);
                    current=data;
                }
                catch(Exception e)
                {
                    points.Clear(); regions.Clear();publishedArt=null;
                    Debug.LogError("Catacumbas publicadas inválidas; usar mapa anterior: "+e.Message);
                }
                return current;
            }
        }
        public static string ContentHash=>Current!=null ? Current.contentHash : "catacombs-legacy-v1";
        public static DungeonArtCatalog ArtCatalog { get { var data=Current;return data!=null ? publishedArt : null; } }
        public static Vector2 Point(string id,Vector2 fallback)
        { return Current!=null && points.TryGetValue(id,out var value) ? value : fallback; }
        public static DungeonPointRecord Identity(string id)
        { return Current==null ? null : Array.Find(Current.points,p=>p.id==id); }
        public static Sprite ArtSprite(string slot)
        {
            var data=Current; if(data==null) return null;
            var replacement=Array.Find(data.artReplacements,r=>r.slot==slot);
            string id=replacement!=null ? replacement.asset : slot;
            return publishedArt?.Find(id)?.Sprite;
        }
        public static Rect Region(string id,Rect fallback)
        { return Current!=null && regions.TryGetValue(id,out var value) ? value : fallback; }
        public static RectInt CellRegion(string id,RectInt fallback)
        {
            var r=Region(id,new Rect(fallback.x,fallback.y,fallback.width,fallback.height));
            return new RectInt(Mathf.RoundToInt(r.x),Mathf.RoundToInt(r.y),Mathf.RoundToInt(r.width),Mathf.RoundToInt(r.height));
        }
    }
}
