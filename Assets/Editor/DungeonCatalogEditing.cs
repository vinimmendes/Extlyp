using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CyberExtraction.Match;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CyberExtraction.EditorTools
{
    public static class DungeonCatalogEditing
    {
        public static void Add(DungeonMapAuthoring root,Sprite sprite,string label,Vector2 size,bool tile)
        {
            if(sprite==null || !AssetDatabase.Contains(sprite) || size.x<=0 || size.y<=0) throw new InvalidOperationException("Use um sprite persistente do Project e dimensões positivas.");
            var catalog=root.MigratedCatalog;
            if(catalog==null)
            {
                catalog=ScriptableObject.CreateInstance<DungeonArtCatalog>();
                AssetDatabase.CreateAsset(catalog,"Assets/Resources/"+DungeonArtCatalog.MigrationPath+".asset");
                Undo.RecordObject(root,"Catálogo adicional"); root.MigratedCatalog=catalog;
            }
            Undo.RecordObject(catalog,"Registrar sprite editável");
            string id="Custom_"+Guid.NewGuid().ToString("N");
            var entry=new DungeonArtEntry {Id=id,DisplayName=label,Family="Custom",Sector="Autoria / Personalizados",Sprite=sprite,WorldSize=size,
                Pivot=new Vector2(sprite.pivot.x/sprite.rect.width,sprite.pivot.y/sprite.rect.height)};
            if(tile)
            {
                var asset=ScriptableObject.CreateInstance<Tile>(); asset.name=id+"_Tile"; asset.sprite=sprite; asset.flags=TileFlags.None; asset.colliderType=Tile.ColliderType.None;
                asset.transform=Matrix4x4.Scale(new Vector3(1/sprite.bounds.size.x,1/sprite.bounds.size.y,1));
                AssetDatabase.AddObjectToAsset(asset,catalog); entry.Tile=asset;
            }
            catalog.Entries=catalog.Entries.Concat(new[]{entry}).ToArray(); Rehash(catalog);
            EditorUtility.SetDirty(root); AssetDatabase.SaveAssets();
        }
        public static void Rehash(DungeonArtCatalog catalog)
        {
            var text=new StringBuilder("additional-catalog-v2");var sources=new Dictionary<string,string>();
            foreach(var e in catalog.Entries.OrderBy(e=>e.Id,StringComparer.Ordinal))
            {
                string path=AssetDatabase.GetAssetPath(e.Sprite);
                if(e.Sprite==null) { text.Append(e.Id).Append("|missing"); continue; }
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(e.Sprite,out string guid,out long localId);
                text.Append(e.Id).Append('|').Append(e.DisplayName).Append('|').Append(e.Family).Append('|').Append(e.Sector).Append('|').Append(e.State)
                    .Append('|').Append(e.WorldSize.x.ToString("R",CultureInfo.InvariantCulture)).Append('|').Append(e.WorldSize.y.ToString("R",CultureInfo.InvariantCulture))
                    .Append('|').Append(e.Pivot.x.ToString("R",CultureInfo.InvariantCulture)).Append('|').Append(e.Pivot.y.ToString("R",CultureInfo.InvariantCulture))
                    .Append('|').Append((int)e.PhysicalKind).Append('|').Append(e.Structural).Append('|').Append(e.Order).Append('|').Append(guid).Append('|').Append(localId)
                    .Append('|');
                if(!sources.TryGetValue(path,out var source)) { source=SourceFingerprint(path,AssetDatabase.GetAssetPath(catalog));sources.Add(path,source); }
                text.Append(source);
            }
            using(var sha=SHA256.Create())
            {
                string next=Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())));
                if(catalog.ContentHash!=next) { catalog.ContentHash=next; EditorUtility.SetDirty(catalog); }
            }
        }
        public static string SourceFingerprint(string path,string exclude="")
        {
            var text=new StringBuilder("source-files-v1");
            foreach(var dependency in AssetDatabase.GetDependencies(path,true).OrderBy(p=>p,StringComparer.Ordinal))
            {
                if(dependency==exclude) continue;
                foreach(var file in new[]{dependency,dependency+".meta"})
                {
                    if(!File.Exists(file)) continue;
                    string extension=Path.GetExtension(file).ToLowerInvariant();
                    bool plain=new[]{".asset",".meta",".mat",".json",".catacombkit",".shader",".cs",".hlsl",".cginc",".asmdef",".txt"}.Contains(extension);
                    byte[] bytes=plain ? Encoding.UTF8.GetBytes(File.ReadAllText(file).Replace("\r\n","\n")) : File.ReadAllBytes(file);
                    using(var sha=SHA256.Create()) text.Append(file.Replace('\\','/')).Append('|').Append(Convert.ToBase64String(sha.ComputeHash(bytes)));
                }
            }
            using(var sha=SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())));
        }
    }
    public sealed class DungeonAdditionalArtImporter : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] movedFrom)
        {
            string path="Assets/Resources/"+DungeonArtCatalog.MigrationPath+".asset";
            var catalog=AssetDatabase.LoadAssetAtPath<DungeonArtCatalog>(path); if(catalog==null) return;
            if(!imported.Contains(path) && deleted.Length==0 && !catalog.Entries.Any(e=>e.Sprite!=null && imported.Concat(moved).Contains(AssetDatabase.GetAssetPath(e.Sprite)))) return;
            EditorApplication.delayCall+=()=> { if(catalog!=null) { DungeonCatalogEditing.Rehash(catalog); AssetDatabase.SaveAssets(); DungeonAuthoringLifecycle.Queue(); } };
        }
    }
}
