using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CyberExtraction.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CyberExtraction.EditorTools
{
    // Native asset/scene production only. Never enters Play, runs tests or builds a player.
    public static class DungeonAuthoringProduction
    {
        [Serializable] sealed class Recipes
        { public DungeonVisualRecord[] visuals; public DungeonSurfaceRecord[] surfaces; public int[] dressedRooms; public FunctionalOverride[] functionalOverrides; public DungeonPointRecord[] pointOverrides; }
        [Serializable] sealed class FunctionalOverride
        { public string id,asset,point; public float width,height,offsetX; public int order; public bool preview,hidden; }
        [Serializable] sealed class ComparisonReview
        { public string sourceSha256,kind; public DungeonFidelityRecord[] rooms; }
        public static void SaveProductionReviewDraft()
        {
            const string folder="Art/Catacumbas/ReworkCompleto/";
            var review=JsonUtility.FromJson<ComparisonReview>(File.ReadAllText(folder+"Comparacoes/revisao.json"));
            string snapshot=folder+"MontagensUnity/Autoria.json",sha;
            using(var hash=SHA256.Create()) sha=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(snapshot))).Replace("-","").ToLowerInvariant();
            if(review==null || review.sourceSha256!=sha || review.rooms==null || review.rooms.Length!=24 ||
                review.rooms.Any(r=>r.room<0 || r.room>23 || r.resolved) || review.rooms.Select(r=>r.room).Distinct().Count()!=24)
                throw new InvalidOperationException("Registro de revisão pendente incompatível com a captura.");
            var root=DungeonAuthoringScene.Open();var current=DungeonAuthoringCompiler.Capture(root,out var errors);
            var previous=JsonUtility.FromJson<DungeonMapDocument>(File.ReadAllText(snapshot));
            if(current==null || current.Fingerprint()!=previous.Fingerprint()) throw new InvalidOperationException("A autoria mudou; refazer a comparação antes de registrar a revisão.");
            Undo.RecordObject(root,"Registrar replanejamento visual");root.Document.fidelity=review.rooms;
            if(!DungeonAuthoringCompiler.Save(root,false,out errors) || errors.Count>0) throw new InvalidOperationException(string.Join("\n",errors));
            var saved=DungeonAuthoringCompiler.Capture(root,out errors);
            if(saved==null || Enumerable.Range(0,24).Any(i=>saved.RoomFingerprint(i)!=previous.RoomFingerprint(i))) throw new InvalidOperationException("A revisão modificou a montagem; refazer as imagens.");
            File.WriteAllText(snapshot,JsonUtility.ToJson(saved,true));
            Debug.Log("CATACUMBAS_REVIEW_DRAFT_SAVED — pendências preservadas, sem publicação.");
        }
        public static void PublishReviewedProduction()
        {
            const string folder="Art/Catacumbas/ReworkCompleto/";
            var review=JsonUtility.FromJson<ComparisonReview>(File.ReadAllText(folder+"Comparacoes/revisao.json"));
            string snapshot=folder+"MontagensUnity/Autoria.json",sha;
            using(var hash=SHA256.Create()) sha=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(snapshot))).Replace("-","").ToLowerInvariant();
            if(review==null || review.sourceSha256!=sha || review.rooms==null || review.rooms.Length!=24 ||
                review.rooms.Select(r=>r.room).Distinct().Count()!=24 || review.rooms.Any(r=>!r.resolved || !string.IsNullOrWhiteSpace(r.pendingDeviations)))
                throw new InvalidOperationException("Revisão incompleta ou sem correspondência com as imagens produzidas. Rascunho preservado.");
            var root=DungeonAuthoringScene.Open();
            var current=DungeonAuthoringCompiler.Capture(root,out var errors);
            var captured=JsonUtility.FromJson<DungeonMapDocument>(File.ReadAllText(snapshot));
            if(current==null || current.Fingerprint()!=captured.Fingerprint())
                throw new InvalidOperationException("A autoria mudou depois da captura. Refazer comparações antes de publicar.");
            foreach(var r in review.rooms)
            {
                if(!File.Exists(r.concept) || !File.Exists(r.comparison)) throw new InvalidOperationException("Comparação ausente: R"+r.room);
                r.comparisonHash=current.RoomFingerprint(r.room);
            }
            Undo.RecordObject(root,"Aplicar revisão de fidelidade"); root.Document.fidelity=review.rooms;
            if(!DungeonAuthoringCompiler.Save(root,true,out errors)) throw new InvalidOperationException(string.Join("\n",errors));
            var publishedSource=DungeonAuthoringCompiler.Capture(root,out errors);
            if(publishedSource==null || Enumerable.Range(0,24).Any(i=>publishedSource.RoomFingerprint(i)!=current.RoomFingerprint(i)))
                throw new InvalidOperationException("A fonte mudou durante a publicação; refazer a comparação.");
            Directory.CreateDirectory(folder+"Historico");
            string history=folder+"Historico/Autoria-revisada-"+sha.Substring(0,12)+".json";
            if(!File.Exists(history)) File.Copy(snapshot,history);
            File.WriteAllText(snapshot,JsonUtility.ToJson(publishedSource,true));
            using(var hash=SHA256.Create()) review.sourceSha256=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(snapshot))).Replace("-","").ToLowerInvariant();
            File.WriteAllText(folder+"Comparacoes/revisao.json",JsonUtility.ToJson(review,true));
            File.WriteAllText(folder+"MontagensUnity/PRODUCAO.txt","Autoria e conteúdo publicado com revisão estática de fidelidade. Produção fora do Play; avaliação prática pelo usuário ainda pendente.\n");
            File.WriteAllText(folder+"PUBLICACAO.txt","Conteúdo publicado: "+DungeonMapPublication.ContentHash+"\nRevisão estática vinculada às montagens por SHA-256: "+sha+"\nSem Play, testes automatizados ou build. Avaliação prática pendente do usuário.\n");
            Debug.Log("CATACUMBAS_PUBLICATION_COMPLETE "+DungeonMapPublication.ContentHash);
        }
        static void UpdateRecipes(DungeonMapAuthoring root)
        {
            const string snapshot="Art/Catacumbas/ReworkCompleto/MontagensUnity/Autoria.json";
            if(!File.Exists(snapshot)) return;
            var current=DungeonAuthoringCompiler.Capture(root,out var errors);
            string previousJson=File.ReadAllText(snapshot);
            var previous=JsonUtility.FromJson<DungeonMapDocument>(previousJson);
            if(current==null) throw new InvalidOperationException(string.Join("\n",errors));
            previous.artHash=current.artHash;
            if(!previousJson.Contains("\"floorTint\"")) previous.floorTint=current.floorTint;
            if(!previousJson.Contains("\"requiredFunctionalKeys\"")) previous.requiredFunctionalKeys=current.requiredFunctionalKeys;
            foreach(var v in previous.visuals.Concat(previous.functionalVisuals)) v.color=(Color)(Color32)v.color;
            foreach(var s in previous.surfaces) s.color=(Color)(Color32)s.color;
            if(previous.Fingerprint()!=current.Fingerprint())
            {
                File.WriteAllText("Art/Catacumbas/ReworkCompleto/Autoria-conflito-atual.json",JsonUtility.ToJson(current,true));
                File.WriteAllText("Art/Catacumbas/ReworkCompleto/Autoria-conflito-anterior.json",JsonUtility.ToJson(previous,true));
                throw new InvalidOperationException("A autoria foi editada desde a última produção. Cena preservada; concilie as receitas manualmente.");
            }
            var recipes=JsonUtility.FromJson<Recipes>(File.ReadAllText("Assets/Resources/Environment/Dungeon/ReworkComplete/AuthoringRecipes.json"));
            foreach(var element in root.GetComponentsInChildren<DungeonAuthoringElement>().Where(e=>e.Kind==DungeonElementKind.Visual).ToArray()) UnityEngine.Object.DestroyImmediate(element.gameObject);
            foreach(var source in root.GetComponentsInChildren<DungeonTerrainSource>().Where(s=>s.VisualSurface).ToArray()) UnityEngine.Object.DestroyImmediate(source.transform.parent.gameObject);
            root.Document.visuals=recipes.visuals; root.Document.surfaces=recipes.surfaces; root.Document.dressedRooms=recipes.dressedRooms;
            foreach(var change in recipes.pointOverrides??Array.Empty<DungeonPointRecord>())
                root.GetComponentsInChildren<DungeonAuthoringElement>().First(e=>e.Kind==DungeonElementKind.Point && e.Id==change.id).transform.position=change.position;
            foreach(var v in recipes.visuals) DungeonAuthoringScene.Place(root,root.FindArt(v.asset),v.position,v);
            foreach(var change in recipes.functionalOverrides??Array.Empty<FunctionalOverride>())
            {
                var element=root.GetComponentsInChildren<DungeonAuthoringElement>().First(e=>e.Kind==DungeonElementKind.FunctionalVisual && e.Id==change.id);
                var entry=root.FindArt(change.asset); var render=element.GetComponent<SpriteRenderer>();
                element.AssetId=entry.Id; element.Hidden=change.hidden; render.sprite=entry.Sprite; render.sortingOrder=change.order; render.enabled=change.preview;
                if(!string.IsNullOrEmpty(change.point)) element.transform.position=root.Document.points.First(p=>p.id==change.point).position+Vector2.right*change.offsetX;
                element.transform.localScale=new Vector3(change.width/entry.Sprite.bounds.size.x,change.height/entry.Sprite.bounds.size.y,1);
            }
            foreach(var group in recipes.surfaces.GroupBy(s=>(s.order,family:DungeonMapArt.SurfaceFamily(s.asset))))
            {
                var map=DungeonAuthoringScene.CreateSurface(root,group.Key.order,group.Key.family).GetComponent<UnityEngine.Tilemaps.Tilemap>();
                foreach(var s in group)
                { var cell=new Vector3Int(s.x,s.y,0); map.SetTile(cell,root.FindArt(s.asset)?.Tile); map.SetTileFlags(cell,UnityEngine.Tilemaps.TileFlags.None); map.SetColor(cell,s.color); }
            }
        }
        public static void Produce()
        {
            ProduceSurfaceMaterials();
            var root=DungeonAuthoringScene.Open();
            if(root==null) throw new InvalidOperationException("Autoria não aberta.");
            UpdateRecipes(root);
            DungeonLegacyMigration.Archive(root);
            string revision;
            using(var hash=SHA256.Create()) revision=BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(
                DungeonArtCatalog.CombinedHash(root.Catalog,root.MigratedCatalog)+File.ReadAllText("Assets/Resources/Environment/Dungeon/ReworkComplete/AuthoringRecipes.json")))).Replace("-","").Substring(0,12).ToLowerInvariant();
            root.PrefabFolder="Assets/Art/Catacumbas/Prefabs/Producao_"+revision;
            DungeonAuthoringWindow.ExportPrefabs(root);
            DungeonKitReference.Produce();
            DungeonAuthoringScene.RefreshArt(root);
            if(!DungeonAuthoringCompiler.Save(root,false,out var errors))
                throw new InvalidOperationException(string.Join("\n",errors));
            var data=DungeonAuthoringCompiler.Capture(root,out errors);
            if(data==null) throw new InvalidOperationException(string.Join("\n",errors));
            const string folder="Art/Catacumbas/ReworkCompleto/MontagensUnity";
            Directory.CreateDirectory(folder);
            File.WriteAllText(folder+"/Autoria.json",JsonUtility.ToJson(data,true));
            var camera=new GameObject("Camera_de_producao").AddComponent<Camera>();
            camera.orthographic=true; camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.055f,.065f,.06f); camera.nearClipPlane=.1f; camera.farClipPlane=100;
            try
            {
                foreach(var room in data.rooms)
                {
                    int w=(room.width+(room.id==13 ? 8 : 4))*72,h=(room.height+4)*72;
                    camera.transform.position=new Vector3((room.x+room.width*.5f-64+(room.id==13 ? 2 : 0))*1.06f,(room.y+room.height*.5f-64)*1.06f,-30);
                    camera.orthographicSize=(room.height+4)*1.06f*.5f;
                    var target=RenderTexture.GetTemporary(w,h,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                    var image=new Texture2D(w,h,TextureFormat.RGB24,false); var previous=RenderTexture.active;
                    try
                    {
                        camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
                        image.ReadPixels(new Rect(0,0,w,h),0,0); image.Apply();
                        File.WriteAllBytes(folder+"/R"+room.id.ToString("00")+".png",image.EncodeToPNG());
                    }
                    finally { camera.targetTexture=null; RenderTexture.active=previous; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image); }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(camera.gameObject); }
            EditorSceneManager.SaveScene(root.gameObject.scene);
            File.WriteAllText(folder+"/PRODUCAO.txt","Cena, sprites, tiles, prefabs, migração e imagens de montagem produzidos fora do Play. Publicação permanece bloqueada até a revisão de fidelidade de todas as salas.\n");
            Debug.Log("CATACUMBAS_PRODUCTION_COMPLETE");
        }
        static void ProduceSurfaceMaterials()
        {
            const string folder="Assets/Resources/Environment/Dungeon/ReworkComplete/";
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(folder+"DungeonFloorOverlay.shader");
            if(shader==null) throw new InvalidOperationException("Shader de transição ausente.");
            foreach(var family in new[]{"Ornament","Ruin","Earth","Mixed","Roots","Damp","Flooded"})
            {
                string path=folder+"Surface_"+family+".mat";
                if(File.Exists(path)) continue;
                var material=new Material(shader);
                material.SetTexture("_FloorTex",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"FloorMaterial_"+family+".png"));
                material.SetFloat("_Period",family=="Damp" ? 6 : 4);
                AssetDatabase.CreateAsset(material,path);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(folder+"CatacombArt.catacombkit",ImportAssetOptions.ForceUpdate);
        }
    }
}
