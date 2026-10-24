using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CyberExtraction.Match
{
    public interface IMapSpritePalette
    { IEnumerable<Sprite> PaletteSprites {get;} }
    // Presentation patches, shared by every procedural map. Never changes gameplay masks/colliders.
    public sealed class MapVisualEdits : ScriptableObject
    {
        public ExpeditionMap Map;
        public List<MapVisualEdit> Edits=new List<MapVisualEdit>();
        public List<MapVisualAddition> Additions=new List<MapVisualAddition>();
        public static string ResourcePath(ExpeditionMap map)=>"MapEdits/Map_"+(byte)map;
    }
    [Serializable] public sealed class MapVisualAddition
    {
        public string Id;
        public string GridPath;
        public MapVisualEdit Style=new MapVisualEdit();
        public Vector3 Position,Scale=Vector3.one;
        public Vector2 SpriteSize;
        public Quaternion Rotation=Quaternion.identity;
        public Color Color=Color.white;
        public int SortingLayer,SortingOrder,Layer;
        public SpriteMaskInteraction Mask;
        public SpriteSortPoint SortPoint;
        public Material Material;
    }
    [Serializable] public sealed class MapVisualEdit
    {
        public string Path;
        public bool Cell;
        public Vector3Int Position;
        public bool Hidden,ReplaceSprite;
        public Texture2D Texture;
        public Rect SpriteRect;
        public Vector2 Pivot;
        public float PixelsPerUnit=100;
        public Vector3 Offset;
        public float Rotation;
        public bool FlipX,FlipY;
        public bool Matches(string path,bool cell,Vector3Int position)=>Path==path && Cell==cell && (!cell || Position==position);
    }
}
