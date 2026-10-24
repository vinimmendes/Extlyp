using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CyberExtraction.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CyberExtraction.EditorTools
{
    // One-shot investigation of the reported missing stair art. No Play,
    // network, publication or edits to the user's scene are performed.
    public static class DungeonStairArtDiagnostics
    {
        [MenuItem("Cyber Extraction/Avançado/Diagnósticos/Sprites das Catacumbas")]
        public static void Run()
        {
            if(Application.isPlaying) throw new InvalidOperationException("Execute fora do Play.");
            var args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-catacomb-sprite-report");
            string folder=output>=0 && output+1<args.Length ? Path.GetFullPath(args[output+1]) : Path.GetFullPath(Path.Combine(Application.dataPath,"../Diagnostics/CatacumbasSprites"));
            Directory.CreateDirectory(folder);
            var report=new StringBuilder();
            var previous=SceneManager.GetActiveScene();
            var current=typeof(MapBuilder).GetField("<Current>k__BackingField",BindingFlags.NonPublic|BindingFlags.Static);
            var saved=current.GetValue(null);
            var mode=Application.isBatchMode && string.IsNullOrEmpty(previous.path) ? NewSceneMode.Single : NewSceneMode.Additive;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,mode);
            SceneManager.SetActiveScene(scene);
            try
            {
                var data=DungeonMapPublication.Current;
                if(data==null) throw new Exception("Mapa publicado indisponível.");
                string fingerprint=data.Fingerprint();
                var catalog=DungeonMapPublication.ArtCatalog;
                foreach(var id in new[]{"CAT-E01_v01","CAT-E02_v01_f01","CAT-E03_v01","CoffinClosed"})
                {
                    var sprite=catalog.Find(id)?.Sprite;
                    report.AppendLine(id+" sprite="+(sprite!=null ? sprite.name : "NULL"));
                    if(sprite!=null) report.AppendLine("texture="+sprite.texture.name+" bounds="+sprite.bounds+" uv="+string.Join(";",sprite.uv.Select(v=>v.ToString("F4"))));
                }
                var builder=new GameObject("Diagnostico_escadaria").AddComponent<MapBuilder>();
                var world=new GameObject("Mapa_isolado");world.transform.SetParent(builder.transform,false);
                typeof(MapBuilder).GetField("world",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(builder,world);
                current.SetValue(null,ExpeditionMap.Dungeon);
                typeof(MapBuilder).GetMethod("BuildDungeon",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(builder,null);
                if(data.Fingerprint()!=fingerprint) throw new Exception("A apresentação alterou a definição publicada.");
                int ledges=0,cloths=0,duplicates=0;
                foreach(var piece in data.visuals)
                {
                    var visual=world.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==piece.id);
                    if(DungeonVotivePresentation.ObsoleteLedge(piece.id,piece.group,piece.asset,piece.size))
                    { ledges++;if(visual!=null) throw new Exception("Borda duplicada ainda renderizada: "+piece.id); }
                    if(DungeonVotivePresentation.DuplicateNorthCloth(piece,data))
                    { duplicates++;if(visual!=null) throw new Exception("Tecido duplicado ainda renderizado: "+piece.id);continue; }
                    if(DungeonVotivePresentation.ProjectedCloth(piece.id,piece.group,piece.asset))
                    { cloths++;if(visual==null || Quaternion.Angle(visual.rotation,Quaternion.identity)>.001f) throw new Exception("Tecido com projeção incorreta: "+piece.id); }
                }
                report.AppendLine("VIGILIA_OK bordas_duplicadas_ocultas="+ledges+" tecidos_com_projecao_preservada="+cloths+" tecidos_duplicados_omitidos="+duplicates+" definicao_publicada_inalterada=True");
                foreach(var renderer in world.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name.StartsWith("CAT-E01") || r.name.StartsWith("CAT-E02") || r.name.StartsWith("CAT-E03")))
                    report.AppendLine(renderer.name+" sprite="+(renderer.sprite!=null ? renderer.sprite.name : "NULL")+" enabled="+renderer.enabled+" active="+renderer.gameObject.activeInHierarchy+" position="+renderer.transform.position+" scale="+renderer.transform.lossyScale+" order="+renderer.sortingOrder+" bounds="+renderer.bounds);
                foreach(var node in world.GetComponentsInChildren<Transform>(true)) node.gameObject.layer=31;
                var camera=new GameObject("Camera_diagnostico").AddComponent<Camera>();
                camera.orthographic=true;camera.orthographicSize=5;camera.cullingMask=1<<31;
                var point=DungeonLayout.ExitPositions[0];camera.transform.position=new Vector3(point.x,point.y,-30);
                var target=RenderTexture.GetTemporary(960,720,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                var pixels=new Texture2D(960,720,TextureFormat.RGB24,false);var old=RenderTexture.active;
                try
                {
                    camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                    pixels.ReadPixels(new Rect(0,0,960,720),0,0);pixels.Apply();File.WriteAllBytes(folder+"/montagem.png",pixels.EncodeToPNG());
                    foreach(var id in new[]{"R16_EastVigil_1","R20_SouthVigil_0","R16_EastVigil_0","R16_WestVigil_0","R16_NorthVigil_0"})
                    {
                        var piece=data.visuals.FirstOrDefault(v=>v.id==id);
                        if(piece==null) continue;
                        camera.orthographicSize=2.0f;camera.transform.position=new Vector3(piece.position.x,piece.position.y,-30);
                        camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,960,720),0,0);pixels.Apply();
                        File.WriteAllBytes(folder+"/"+id+".png",pixels.EncodeToPNG());
                    }
                }
                finally { camera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(pixels); }
            }
            catch(Exception e) { report.AppendLine(e.ToString());Debug.LogException(e); }
            finally
            {
                current.SetValue(null,saved);
                if(previous.IsValid() && previous.isLoaded) { EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous); }
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                File.WriteAllText(folder+"/diagnostico.txt",report.ToString());
            }
        }
    }
}
