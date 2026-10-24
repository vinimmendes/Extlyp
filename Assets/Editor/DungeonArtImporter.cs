using System;
using System.Collections.Generic;
using System.IO;
using CyberExtraction.Match;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CyberExtraction.EditorTools
{
    [ScriptedImporter(4,"catacombkit")]
    public sealed class DungeonArtImporter : ScriptedImporter
    {
        [Serializable] sealed class Source { public Entry[] entries; }
        [Serializable] sealed class Entry
        {
            public string id,label,texture,family,sector,state;
            public int x,y,width,height,order=-3;
            public float worldWidth=1.06f,worldHeight=1.06f;
            public float pivotX=.5f,pivotY=.5f;
            public bool structural,tile;
            public int physicalKind=1;
        }
        public override void OnImportAsset(AssetImportContext context)
        {
            var source=JsonUtility.FromJson<Source>(File.ReadAllText(context.assetPath));
            if(source?.entries==null) throw new InvalidOperationException("Catálogo de arte vazio.");
            var catalog=ScriptableObject.CreateInstance<DungeonArtCatalog>(); catalog.name="CatacombArt";
            var content=new System.Text.StringBuilder("catacomb-kit-v4\n"+File.ReadAllText(context.assetPath).Replace("\r\n","\n"));
            string folder=Path.GetDirectoryName(context.assetPath).Replace('\\','/');
            var materials=new List<string>{folder+"/DungeonFloorOverlay.shader"};
            materials.Add("Assets/Resources/DungeonWater.shader");materials.Add("Assets/Resources/DungeonWaterAppearance.mat");
            materials.Add("Assets/Resources/UnlitSprite2D.mat");
            materials.Add("Assets/Resources/Environment/Dungeon/Floors/CAT-F07_v01.png");
            foreach(var family in new[]{"Ornament","Ruin","Earth","Mixed","Roots","Damp","Flooded"})
            { materials.Add(folder+"/FloorMaterial_"+family+".png"); materials.Add(folder+"/Surface_"+family+".mat"); }
            foreach(var path in materials) if(File.Exists(path))
            {
                context.DependsOnSourceAsset(path); context.DependsOnArtifact(path);
                byte[] bytes=Path.GetExtension(path)==".png" ? File.ReadAllBytes(path) : System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(path).Replace("\r\n","\n"));
                using(var sha=System.Security.Cryptography.SHA256.Create()) content.Append(Convert.ToBase64String(sha.ComputeHash(bytes)));
                if(File.Exists(path+".meta")) content.Append(File.ReadAllText(path+".meta").Replace("\r\n","\n"));
            }
            foreach(var path in System.Linq.Enumerable.Distinct(System.Linq.Enumerable.Select(source.entries,e=>e.texture)))
                using(var sha=System.Security.Cryptography.SHA256.Create()) content.Append(Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))));
            var entries=new List<DungeonArtEntry>(); var ids=new HashSet<string>();
            foreach(var e in source.entries)
            {
                if(e==null || string.IsNullOrEmpty(e.id) || !ids.Add(e.id) || e.width<=0 || e.height<=0 || e.worldWidth<=0 || e.worldHeight<=0)
                    throw new InvalidOperationException("Identidade/recorte/dimensão inválida no catálogo.");
                context.DependsOnSourceAsset(e.texture);
                context.DependsOnArtifact(e.texture);
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(e.texture);
                if(texture==null || e.x<0 || e.y<0 || e.x+e.width>texture.width || e.y+e.height>texture.height)
                    throw new InvalidOperationException("Textura ou recorte incompatível: "+e.id);
                var sprite=Sprite.Create(texture,new Rect(e.x,e.y,e.width,e.height),new Vector2(e.pivotX,e.pivotY),128,0,SpriteMeshType.FullRect);
                sprite.name=e.id; context.AddObjectToAsset("sprite/"+e.id,sprite);
                Tile tile=null;
                if(e.tile)
                {
                    tile=ScriptableObject.CreateInstance<Tile>(); tile.name=e.id+"_Tile";
                    tile.sprite=sprite; tile.colliderType=Tile.ColliderType.None; tile.flags=TileFlags.None;
                    tile.transform=Matrix4x4.Scale(new Vector3(128f/e.width,128f/e.height,1));
                    context.AddObjectToAsset("tile/"+e.id,tile);
                }
                entries.Add(new DungeonArtEntry {Id=e.id,DisplayName=e.label??e.id,Sprite=sprite,Tile=tile,Family=e.family,Sector=e.sector,State=e.state,
                    WorldSize=new Vector2(e.worldWidth,e.worldHeight),Pivot=new Vector2(e.pivotX,e.pivotY),PhysicalKind=(DungeonCellKind)e.physicalKind,Structural=e.structural,Order=e.order});
            }
            foreach(var path in System.Linq.Enumerable.Distinct(System.Linq.Enumerable.Select(source.entries,e=>e.texture)))
                if(File.Exists(path+".meta")) content.Append(File.ReadAllText(path+".meta").Replace("\r\n","\n"));
            using(var sha=System.Security.Cryptography.SHA256.Create()) catalog.ContentHash=Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content.ToString())));
            catalog.Entries=entries.ToArray(); context.AddObjectToAsset("catalog",catalog); context.SetMainObject(catalog);
        }
    }
}
