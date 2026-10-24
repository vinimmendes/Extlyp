using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Items;
using CyberExtraction.Match;

public static class TestLabChecks
{
    static int checks;
    public static void PrepareAndRun() { EnvironmentSetup.Prepare(); Run(); }
    public static void Build() { EnvironmentSetup.Prepare(); Run(); ContentIntegrationChecks.Run(); ServerBuild.BuildWindowsBoth(); }
    static void Verify(bool value,string reason) { if(!value) throw new Exception(reason); checks++; }
    [MenuItem("Cyber Extraction/Avançado/Diagnósticos/Laboratório e cenário grande")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do Play para verificar em uma cena isolada.");
        var previous=UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if(string.IsNullOrEmpty(previous.path))
        {
            if(previous.isDirty) throw new InvalidOperationException("Salve a cena aberta antes de verificar.");
            previous=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        }
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
        UnityEditor.SceneManagement.EditorSceneManager.SetActiveScene(scene);
        var map=new GameObject("ValidationMap").AddComponent<MapBuilder>();
        try
        {
            // Awake is not invoked for this ordinary MonoBehaviour in edit mode.
            typeof(MapBuilder).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(map,null);
            Verify(MapBuilder.Width==30 && MapBuilder.Height==46,"Dimensoes do mapa de teste preservadas");
            Verify(map.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("Crate"))==4,"Quatro caixas originais preservadas");
            Verify(!map.GetComponentsInChildren<Transform>().Any(t=>t.name=="OldBed" || t.name=="ForestTree"),"Cenario de teste sem novos moveis/arvores");
            Physics2D.SyncTransforms();
            Verify(MatchManager.FindTestPoint(Vector2.zero,Vector2.right,1.5f,out var testSpawn),"Mirar na parede do cercado encontra outra direcao livre");
            Verify(!Physics2D.OverlapCircle(testSpawn,1.5f,GameConfig.WallMask),"Criacao alternativa nao sobrepoe paredes");
            MapBuilder.Configure(ExpeditionMap.Large);
            var all=map.GetComponentsInChildren<Transform>();
            Verify(MapBuilder.Width==400 && MapBuilder.Height==600,"Dimensoes do mapa grande atual");
            Verify(all.Count(t=>t.name.StartsWith("Ruin_"))==MapBuilder.Sites.Length,"Todas as ruinas construidas sem exigir moveis identicos");
            Verify(map.GetComponentsInChildren<FurnitureObstacle>().Length==MapBuilder.FurniturePoints.Count,"Recipientes exploraveis correspondem aos obstaculos registrados");
            var trees=all.Where(t=>t.name=="ForestTree" || t.name=="DeadTree").ToArray();
            Verify(trees.Length>100,"Floresta adicionada ao mapa grande");
            foreach(string name in new[] {"ForestTree","DeadTree","Wardrobe","Bed","Table","Cabinet"}) Verify(EnvironmentSprites.Get(name)!=null,"Sprite de ambiente presente: "+name);
            foreach(var tree in trees)
            {
                Verify(tree.GetComponent<CircleCollider2D>()!=null && tree.gameObject.layer==GameConfig.LowObstacleLayer,"Tronco solido sem bloquear visao");
            }
            Physics2D.SyncTransforms(); var c=MapBuilder.Sites[0];
            for(int ruin=0;ruin<MapBuilder.Sites.Length;ruin++)
            {
                Vector2 entrance=MapBuilder.RuinPoint(ruin,Vector2.zero), chest=MapBuilder.RuinPoint(ruin,new Vector2(0,4));
                Verify(Physics2D.OverlapCircle(chest,.55f,GameConfig.WallMask)==null,"Bau acessivel na ruina "+ruin);
                Verify(!Physics2D.Linecast(entrance,chest,GameConfig.WallMask),"Entrada livre com orientacao da ruina "+ruin);
            }
            Verify(MatchManager.TestAccess(true,true,MatchState.Running,true),"Laboratorio autorizado");
            Verify(!MatchManager.TestAccess(false,true,MatchState.Running,true),"Servidor sem testes rejeita comandos");
            Verify(!MatchManager.TestAccess(true,false,MatchState.Running,true),"Partida normal rejeita comandos");
            Verify(!MatchManager.TestAccess(true,true,MatchState.Waiting,true),"Espera rejeita comandos");
            Verify(!MatchManager.TestAccess(true,true,MatchState.Running,false),"Cliente sem participante rejeitado");
            foreach(var item in MatchManager.TestWeapons)
            {
                var weapon=WeaponCatalog.Get(item); var bag=new InventoryGrid();
                Verify(bag.Add(new ItemStack(item,1,0,weapon.Capacity)) && bag.Add(new ItemStack(weapon.Ammo,60)) && bag.Add(new ItemStack(ItemId.Bandage,10)),"Equipamento de teste cabe: "+item);
                Verify(bag.Count(weapon.Ammo)==60 && bag.Slots[0].Data==weapon.Capacity,"Carregador/reserva: "+item);
            }
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/test-lab-checks.json","{\"passed\":"+checks+",\"trees\":"+trees.Length+",\"houses\":"+MapBuilder.Sites.Length+"}");
            if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null) RenderViews(c);
            Debug.Log("TEST LAB CHECKS PASS: "+checks);
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
            UnityEditor.SceneManagement.EditorSceneManager.SetActiveScene(previous);
        }
    }
    static void RenderViews(Vector2 site)
    {
        var camera=new GameObject("MapReviewCamera").AddComponent<Camera>(); camera.orthographic=true;
        camera.backgroundColor=new Color(.08f,.10f,.08f); camera.clearFlags=CameraClearFlags.SolidColor;
        var target=new RenderTexture(1280,720,24); target.Create();
        var old=RenderTexture.active; var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            for(int view=0;view<2;view++)
            {
                camera.transform.position=new Vector3(site.x,site.y+5,-10); camera.orthographicSize=view==0 ? 8 : 25;
                camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1280,720),0,0); pixels.Apply();
                File.WriteAllBytes(view==0 ? "Logs/test-lab-house.png" : "Logs/test-lab-forest.png",pixels.EncodeToPNG());
            }
        }
        finally { RenderTexture.active=old; camera.targetTexture=null; UnityEngine.Object.DestroyImmediate(pixels); target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(camera.gameObject); }
    }
}
