using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CyberExtraction.Core;
using CyberExtraction.Match;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CyberExtraction.EditorTools
{
    // Freeze the complete art dependency set with a publication. Source edits
    // remain drafts; they cannot mutate the last playable map through shared PNGs.
    public static class DungeonArtPublication
    {
        public static string Export(DungeonMapAuthoring root,string artHash)
        {
            string revision;
            using(var sha=SHA256.Create()) revision=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes("catacomb-art-publication-v3\n"+artHash))).Replace("-","").ToLowerInvariant().Substring(0,24);
            string resource="Environment/Dungeon/ReworkComplete/Publications/"+revision+"/Catalog";
            string folder="Assets/Resources/Environment/Dungeon/ReworkComplete/Publications/"+revision;
            var previous=AssetDatabase.LoadAssetAtPath<DungeonArtCatalog>(folder+"/Catalog.asset");
            if(previous!=null)
            {
                if(previous.ContentHash!=artHash) throw new InvalidOperationException("Identificação da arte publicada incompatível; versão anterior preservada.");
                return resource;
            }
            if(Directory.Exists(folder)) throw new InvalidOperationException("Pasta de publicação incompleta; preservar e conciliar antes de publicar: "+folder);
            string staging="Assets/Art/Catacumbas/PublicacaoEmProducao_"+Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(staging+"/Textures");Directory.CreateDirectory(staging+"/Materials");Directory.CreateDirectory(staging+"/Shaders");
            AssetDatabase.Refresh();
            var textures=new Dictionary<Texture2D,Texture2D>();var materials=new Dictionary<Material,Material>();var shaders=new Dictionary<Shader,Shader>();
            var copies=new Dictionary<string,string>();
            string Copy(string path,string subfolder)
            {
                if(copies.TryGetValue(path,out var existing)) return existing;
                if(!path.StartsWith("Assets/",StringComparison.Ordinal) || !File.Exists(path)) throw new InvalidOperationException("Fonte de arte não persistente: "+path);
                string target=staging+"/"+subfolder+"/"+AssetDatabase.AssetPathToGUID(path)+Path.GetExtension(path);
                if(!AssetDatabase.CopyAsset(path,target)) throw new InvalidOperationException("Não foi possível conservar a fonte: "+path);
                copies.Add(path,target);return target;
            }
            Texture2D Texture(Texture2D source)
            {
                if(source==null) return null;if(textures.TryGetValue(source,out var existing)) return existing;
                if(source==Texture2D.whiteTexture || source==Texture2D.blackTexture || source==Texture2D.grayTexture) return source;
                string path=AssetDatabase.GetAssetPath(source);
                if(!path.StartsWith("Assets/",StringComparison.Ordinal))
                {
                    // Built-in white/black textures are engine constants.
                    if(path.Contains("unity_builtin_extra") || path.Contains("unity default resources")) return source;
                    throw new InvalidOperationException("Textura da publicação precisa ser um asset persistente: "+source.name);
                }
                string target=Copy(path,"Textures");
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string _,out long id);
                var texture=AssetDatabase.LoadAllAssetsAtPath(target).OfType<Texture2D>().FirstOrDefault(t=>
                { AssetDatabase.TryGetGUIDAndLocalFileIdentifier(t,out string guid,out long localId);return localId==id; });
                if(texture==null) texture=AssetDatabase.LoadAssetAtPath<Texture2D>(target);
                if(texture==null || texture.width!=source.width || texture.height!=source.height) throw new InvalidOperationException("Recorte/textura divergente na publicação: "+source.name);
                textures.Add(source,texture);return texture;
            }
            Shader ShaderCopy(Shader source)
            {
                if(source==null) throw new InvalidOperationException("Material sem shader.");
                if(shaders.TryGetValue(source,out var existing)) return existing;
                string path=AssetDatabase.GetAssetPath(source);
                if(!path.StartsWith("Assets/",StringComparison.Ordinal)) return source;
                if(Path.GetExtension(path)!=".shader") throw new InvalidOperationException("Publicação de shader personalizado requer fonte .shader persistente: "+path);
                var shader=AssetDatabase.LoadAssetAtPath<Shader>(Copy(path,"Shaders"));
                if(shader==null) throw new InvalidOperationException("Shader não conservado: "+path);
                shaders.Add(source,shader);return shader;
            }
            Material MaterialCopy(Material source)
            {
                if(source==null) throw new InvalidOperationException("Material de produção ausente.");
                if(materials.TryGetValue(source,out var existing)) return existing;
                var material=new Material(source) {name=source.name,shader=ShaderCopy(source.shader)};
                foreach(var property in source.GetTexturePropertyNames())
                    if(source.GetTexture(property) is Texture2D texture) material.SetTexture(property,Texture(texture));
                string path=staging+"/Materials/Material_"+materials.Count.ToString("00")+".mat";
                AssetDatabase.CreateAsset(material,path);materials.Add(source,material);return material;
            }
            var catalog=ScriptableObject.CreateInstance<DungeonArtCatalog>();catalog.name="CatacombPublishedArt";catalog.ContentHash=artHash;
            string catalogPath=staging+"/Catalog.asset";AssetDatabase.CreateAsset(catalog,catalogPath);
            var entries=new List<DungeonArtEntry>();var sprites=new Dictionary<Sprite,Sprite>();
            foreach(var entry in root.ArtEntries)
            {
                var source=entry.Sprite;if(source==null) throw new InvalidOperationException("Sprite ausente: "+entry.Id);
                if(!sprites.TryGetValue(source,out var sprite))
                {
                    var texture=Texture(source.texture);
                    sprite=Sprite.Create(texture,source.rect,new Vector2(source.pivot.x/source.rect.width,source.pivot.y/source.rect.height),source.pixelsPerUnit,0,SpriteMeshType.FullRect);
                    sprite.name=entry.Id;
                    // FullRect sprites already have this mesh. Unity refuses an
                    // unnecessary override on them; compare before requesting it.
                    var vertices=source.vertices;var triangles=source.triangles;
                    if(!sprite.vertices.SequenceEqual(vertices) || !sprite.triangles.SequenceEqual(triangles))
                    {
                        UnityEngine.Object.DestroyImmediate(sprite);
                        // Custom imported meshes need an editable Tight sprite
                        // and CPU access while conserving their exact geometry.
                        string path=AssetDatabase.GetAssetPath(texture);
                        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                        if(importer==null) throw new InvalidOperationException("Textura sem importador para conservar malha: "+entry.Id);
                        if(!importer.isReadable) { importer.isReadable=true;importer.SaveAndReimport();texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);textures[source.texture]=texture; }
                        sprite=Sprite.Create(texture,source.rect,new Vector2(source.pivot.x/source.rect.width,source.pivot.y/source.rect.height),source.pixelsPerUnit,0,SpriteMeshType.Tight);
                        sprite.name=entry.Id;
                        if(!sprite.vertices.SequenceEqual(vertices) || !sprite.triangles.SequenceEqual(triangles)) sprite.OverrideGeometry(vertices,triangles);
                        if(!sprite.vertices.SequenceEqual(vertices) || !sprite.triangles.SequenceEqual(triangles))
                            throw new InvalidOperationException("Malha divergente ao conservar sprite: "+entry.Id);
                    }
                    if(Vector3.Distance(sprite.bounds.size,source.bounds.size)>.0001f) throw new InvalidOperationException("Escala divergente ao conservar sprite: "+entry.Id);
                    AssetDatabase.AddObjectToAsset(sprite,catalog);sprites.Add(source,sprite);
                }
                Tile tile=null;
                if(entry.Tile!=null)
                {
                    tile=ScriptableObject.CreateInstance<Tile>();tile.name=entry.Id+"_Tile";tile.sprite=sprite;
                    tile.transform=entry.Tile.transform;tile.color=entry.Tile.color;tile.flags=entry.Tile.flags;tile.colliderType=Tile.ColliderType.None;
                    AssetDatabase.AddObjectToAsset(tile,catalog);
                }
                entries.Add(new DungeonArtEntry {Id=entry.Id,DisplayName=entry.DisplayName,Family=entry.Family,Sector=entry.Sector,State=entry.State,
                    WorldSize=entry.WorldSize,Pivot=entry.Pivot,PhysicalKind=entry.PhysicalKind,Structural=entry.Structural,Order=entry.Order,Sprite=sprite,Tile=tile});
            }
            if(entries.Select(e=>e.Id).Distinct().Count()!=entries.Count) throw new InvalidOperationException("Catálogos possuem identidades de arte repetidas.");
            catalog.Entries=entries.ToArray();catalog.SpriteMaterial=MaterialCopy(SpriteFactory.SpriteMaterial);
            catalog.SurfaceMaterials=new[]{"Ornament","Ruin","Earth","Mixed","Roots","Damp","Flooded"}.Select(f=>new DungeonSurfaceMaterial {Family=f,Material=MaterialCopy(DungeonMapArt.SurfaceMaterial(f))}).ToArray();
            catalog.WaterMaterial=MaterialCopy(Resources.Load<Material>("DungeonWaterAppearance"));
            catalog.WaterFlowTexture=Texture(Resources.Load<Texture2D>("Environment/Dungeon/Floors/CAT-F07_v01"));
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Path.GetDirectoryName(folder));AssetDatabase.Refresh();
            string error=AssetDatabase.MoveAsset(staging,folder);
            if(!string.IsNullOrEmpty(error)) throw new InvalidOperationException("Arte ainda em produção; publicação anterior preservada: "+error);
            AssetDatabase.SaveAssets();return resource;
        }
    }
}
