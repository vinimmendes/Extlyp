using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CyberExtraction.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace CyberExtraction.EditorTools
{
    public static class DungeonLegacyMigration
    {
        public static void Archive(DungeonMapAuthoring root)
        {
            if(Application.isPlaying) throw new InvalidOperationException("Migração de autoria somente fora do Play.");
            var original=Resources.Load<MapVisualEdits>(MapVisualEdits.ResourcePath(ExpeditionMap.Dungeon));
            if(original==null) { root.MigrationReconciled=true; root.MigrationReport="Nenhum ajuste anterior."; return; }
            var sourcePath=AssetDatabase.GetAssetPath(original);
            string revision=AssetDatabase.GetAssetDependencyHash(sourcePath).ToString();
            string sourceHash=DungeonCatalogEditing.SourceFingerprint(sourcePath);
            if(root.transform.Find("Referencias_anteriores_arquivadas")!=null)
            {
                if(string.IsNullOrEmpty(root.LegacySourceHash) && root.LegacyRevision==revision)
                { root.LegacySourceHash=sourceHash;EditorUtility.SetDirty(root); }
                if(root.LegacySourceHash!=sourceHash) throw new InvalidOperationException("O asset anterior mudou. Preserve o arquivo de autoria e concilie os novos ajustes antes de substituir o arquivo de referência.");
                return;
            }
            var group=new GameObject("Referencias_anteriores_arquivadas"); group.transform.SetParent(root.transform,false);
            var catalog=ScriptableObject.CreateInstance<DungeonArtCatalog>();
            var entries=new List<DungeonArtEntry>(); var report=new StringBuilder();
            var preview=EditorSceneManager.NewPreviewScene(); MapBuilder builder=null;
            try
            {
                builder=MapBuilder.CreateLegacyAuthoringReference(); SceneManager.MoveGameObjectToScene(builder.gameObject,preview);
                CaptureFunctionalParts(root,builder.EditingWorld,entries);
                var runtime=builder.EditingWorld.GetComponent<MapVisualEditRuntime>();
                int i=0;
                foreach(var edit in original.Edits)
                {
                    string id="LegacyEdit_"+i.ToString("00");
                    var node=new GameObject(id); node.transform.SetParent(group.transform,false);
                    var source=node.AddComponent<DungeonLegacySource>();
                    source.Original=JsonUtility.FromJson<MapVisualEdit>(JsonUtility.ToJson(edit)); source.Original.Texture=edit.Texture;
                    var renderer=runtime.VisualFor(edit); var target=runtime.ResolveTarget(edit.Path);
                    Sprite sprite=renderer!=null ? renderer.sprite : null;
                    Color color=renderer!=null ? renderer.color : Color.white;
                    Matrix4x4 matrix=renderer!=null ? renderer.transform.localToWorldMatrix : Matrix4x4.identity;
                    bool flipX=renderer!=null && renderer.flipX,flipY=renderer!=null && renderer.flipY;
                    int order=renderer!=null ? renderer.sortingOrder : -3;
                    if(sprite==null && target!=null)
                    {
                        if(edit.Cell)
                        {
                            var map=target.GetComponent<Tilemap>(); sprite=map!=null ? map.GetSprite(edit.Position) : null;
                            if(map!=null) matrix=map.transform.localToWorldMatrix*Matrix4x4.Translate(map.CellToLocalInterpolated((Vector3)edit.Position+map.tileAnchor))*map.orientationMatrix*map.GetTransformMatrix(edit.Position);
                        }
                        else
                        {
                            var old=target.GetComponent<SpriteRenderer>();
                            if(old!=null) { sprite=old.sprite; color=old.color; matrix=old.transform.localToWorldMatrix; flipX=old.flipX; flipY=old.flipY; order=old.sortingOrder; }
                        }
                    }
                    if(sprite!=null)
                    {
                        var imported=ExportSprite(sprite,id);
                        var x=(Vector3)matrix.GetColumn(0); var y=(Vector3)matrix.GetColumn(1);
                        float sign=Vector3.Cross(x,y).z<0 ? -1 : 1;
                        var size=new Vector2(x.magnitude*sprite.bounds.size.x,y.magnitude*sprite.bounds.size.y);
                        var entry=new DungeonArtEntry {Id=id,DisplayName="Referência anterior "+i,Family="Migrated",Sector="Migração / Referência anterior",Sprite=imported,WorldSize=size,Order=order};
                        entries.Add(entry);
                        var marker=node.AddComponent<DungeonAuthoringElement>(); marker.Kind=DungeonElementKind.Visual; marker.Id=Guid.NewGuid().ToString("N"); marker.AssetId=id; marker.Group="LegacyArchive";
                        node.transform.position=matrix.GetColumn(3); node.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(x.y*sign,x.x*sign)*Mathf.Rad2Deg);
                        node.transform.localScale=new Vector3(size.x/imported.bounds.size.x,size.y/imported.bounds.size.y,1);
                        var copy=node.AddComponent<SpriteRenderer>(); copy.sprite=imported; copy.color=color; copy.sharedMaterial=CyberExtraction.Core.SpriteFactory.SpriteMaterial;
                        copy.flipX=flipX^(sign<0); copy.flipY=flipY; copy.sortingOrder=order; copy.enabled=!edit.Hidden;
                        source.Resolution="Ajuste anterior preservado integralmente nesta referência desativada. A montagem principal segue o conceito aprovado e o novo kit; ativar/copiar esta referência requer nova comparação da sala.";
                    }
                    else
                    {
                        node.transform.position=DungeonLayout.ToWorld((Vector3)edit.Position+Vector3.one*.5f);
                        source.Resolution="O gerador anterior atual já não fornece sprite para este alvo. Caminho, recorte, textura e transformação originais foram preservados no componente; o novo contorno usa os encaixes do conceito.";
                    }
                    report.AppendLine(id+" | "+edit.Path+" | "+source.Resolution); i++;
                }
                foreach(var addition in original.Additions)
                {
                    var renderer=runtime.AdditionVisual(addition.Id);
                    if(renderer==null) { report.AppendLine("Cópia sem representação: "+addition.Id+"; original preservado em Map_2.asset."); continue; }
                    string id="LegacyCopy_"+addition.Id; var sprite=ExportSprite(renderer.sprite,id);
                    var size=Vector2.Scale(renderer.sprite.bounds.size,renderer.transform.lossyScale);
                    var entry=new DungeonArtEntry {Id=id,DisplayName="Cópia anterior",Family="Migrated",Sector="Migração / Referência anterior",Sprite=sprite,WorldSize=size,Order=renderer.sortingOrder}; entries.Add(entry);
                    var node=DungeonAuthoringScene.Place(root,entry,renderer.transform.position); node.transform.SetParent(group.transform,true);
                    node.transform.rotation=renderer.transform.rotation; var copy=node.GetComponent<SpriteRenderer>(); copy.color=renderer.color; copy.flipX=renderer.flipX; copy.flipY=renderer.flipY;
                    report.AppendLine(id+" | cópia preservada na referência desativada.");
                }
                const string path="Assets/Resources/Environment/Dungeon/ReworkComplete/MigratedArt.asset";
                if(File.Exists(path)) throw new InvalidOperationException("Catálogo de migração já existe; arquivo preservado. Reutilize a cena de autoria correspondente.");
                catalog.Entries=entries.ToArray();
                DungeonCatalogEditing.Rehash(catalog);
                AssetDatabase.CreateAsset(catalog,path); AssetDatabase.SaveAssets();
                if(root.MigratedCatalog!=null && !AssetDatabase.Contains(root.MigratedCatalog)) UnityEngine.Object.DestroyImmediate(root.MigratedCatalog);
                root.MigratedCatalog=catalog;
                group.SetActive(false); root.LegacyRevision=revision; root.LegacySourceHash=sourceHash; root.MigrationReport=report.ToString(); root.MigrationReconciled=true;
                Directory.CreateDirectory("Docs/Mapas/ReworkCompleto"); File.WriteAllText("Docs/Mapas/ReworkCompleto/MIGRACAO_EXECUTADA.txt",report.ToString());
                EditorUtility.SetDirty(root); EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(group);
                if(!AssetDatabase.Contains(catalog)) UnityEngine.Object.DestroyImmediate(catalog);
                throw;
            }
            finally
            {
                if(builder!=null) UnityEngine.Object.DestroyImmediate(builder.gameObject);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }
        static void CaptureFunctionalParts(DungeonMapAuthoring root,Transform world,List<DungeonArtEntry> entries)
        {
            if(root.Document.functionalVisuals.Length>0) return;
            var art=world.GetComponentInChildren<DungeonExtractionArt>(); if(art==null) throw new InvalidOperationException("Arte das extrações indisponível para autoria.");
            var parts=new List<DungeonVisualRecord>();
            foreach(var pair in art.PieceKeys)
            {
                var renderer=pair.Key; string id=renderer.name; var entry=root.FindArt(id);
                if(entry==null)
                {
                    entry=new DungeonArtEntry {Id=id,DisplayName=id,Family="ExtractionStates",Sector="Extrações / Componentes",Sprite=ExportSprite(renderer.sprite,id)};
                    entries.Add(entry);
                    // Make the just-created entry available while placing its native scene proxy.
                    if(root.MigratedCatalog==null) root.MigratedCatalog=ScriptableObject.CreateInstance<DungeonArtCatalog>();
                    var added=new List<DungeonArtEntry>(root.MigratedCatalog.Entries) {entry}; root.MigratedCatalog.Entries=added.ToArray();
                }
                int room=id.StartsWith("CAT-E01") || id.StartsWith("CAT-E02") || id=="CAT-E17_v01" || pair.Value=="CAT-E03_v01#0" ? 14 :
                    id.StartsWith("CAT-E04") || id.StartsWith("CAT-E05") || id.StartsWith("CAT-E06") || id.StartsWith("CAT-E07") || id.StartsWith("CAT-E13") || id=="CAT-E17_v02" ? 18 : 13;
                var scale=renderer.transform.lossyScale;
                var part=new DungeonVisualRecord {id=pair.Value,asset=id,group=id,room=room,position=renderer.transform.position,
                    size=new Vector2(Mathf.Abs(scale.x)*renderer.sprite.bounds.size.x,Mathf.Abs(scale.y)*renderer.sprite.bounds.size.y),
                    angle=renderer.transform.eulerAngles.z,order=renderer.sortingOrder,color=renderer.color,flipX=renderer.flipX,flipY=renderer.flipY,carrier=id=="CAT-E09_v01"};
                if(id=="CAT-E04_v01") part.size=Vector2.one*3.2f;
                if(part.carrier)
                {
                    var origin=DungeonLayout.ExitPositions[2]; float facing=Vector2.SignedAngle(Vector2.right,DungeonLayout.CanalDeparture);
                    part.position=origin+(Vector2)(Quaternion.Euler(0,0,-facing)*(part.position-origin)); part.angle-=facing;
                }
                parts.Add(part); DungeonAuthoringScene.PlaceFunctional(root,part);
            }
            root.Document.functionalVisuals=parts.ToArray();
        }
        static Sprite ExportSprite(Sprite sprite,string id)
        {
            const string folder="Assets/Resources/Environment/Dungeon/ReworkComplete/Migration";
            Directory.CreateDirectory(folder); string path=folder+"/"+id+".png";
            if(!File.Exists(path))
            {
                var source=sprite.texture; var old=RenderTexture.active;
                var temporary=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                var crop=new Texture2D(Mathf.RoundToInt(sprite.rect.width),Mathf.RoundToInt(sprite.rect.height),TextureFormat.RGBA32,false);
                try { Graphics.Blit(source,temporary); RenderTexture.active=temporary; crop.ReadPixels(sprite.rect,0,0); crop.Apply(); File.WriteAllBytes(path,crop.EncodeToPNG()); }
                finally { RenderTexture.active=old; RenderTexture.ReleaseTemporary(temporary); UnityEngine.Object.DestroyImmediate(crop); }
            }
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=128; importer.mipmapEnabled=false; importer.npotScale=TextureImporterNPOTScale.None; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency=true; importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
