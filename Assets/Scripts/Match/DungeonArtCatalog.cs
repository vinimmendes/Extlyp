using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CyberExtraction.Match
{
    public sealed class DungeonArtCatalog : ScriptableObject
    {
        public const string ResourcePath="Environment/Dungeon/ReworkComplete/CatacombArt";
        public const string MigrationPath="Environment/Dungeon/ReworkComplete/MigratedArt";
        public string ContentHash;
        public DungeonArtEntry[] Entries=Array.Empty<DungeonArtEntry>();
        public Material SpriteMaterial,WaterMaterial;
        public Texture2D WaterFlowTexture;
        public DungeonSurfaceMaterial[] SurfaceMaterials=Array.Empty<DungeonSurfaceMaterial>();
        System.Collections.Generic.Dictionary<string,DungeonArtEntry> indexed;
        void OnValidate()=>indexed=null;
        public DungeonArtEntry Find(string id)
        {
            if(indexed==null || indexed.Count!=Entries.Length) { indexed=new System.Collections.Generic.Dictionary<string,DungeonArtEntry>(); foreach(var entry in Entries) indexed.Add(entry.Id,entry); }
            return id!=null && indexed.TryGetValue(id,out var value) ? value : null;
        }
        public static string CombinedHash(DungeonArtCatalog main,DungeonArtCatalog migrated)=>main.ContentHash+"|"+(migrated!=null ? migrated.ContentHash : "");
    }
    [Serializable] public sealed class DungeonSurfaceMaterial { public string Family;public Material Material; }
    [Serializable] public sealed class DungeonArtEntry
    {
        public string Id,DisplayName,Family,Sector,State;
        public Vector2 WorldSize=Vector2.one;
        public Vector2 Pivot=new Vector2(.5f,.5f);
        public Sprite Sprite;
        public Tile Tile;
        public DungeonCellKind PhysicalKind=DungeonCellKind.Floor;
        public bool Structural;
        public int Order=-3;
    }
}
