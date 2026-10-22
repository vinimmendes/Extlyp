using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using CyberExtraction.Core;

public static class VisionSurfaceChecks
{
    public static void Run()
    {
        RunFog();
        ChargerChecks();
    }
    public static void RunFog()
    {
        var shader=Resources.Load<Shader>("VisionSurface");
        if(shader==null || ShaderUtil.ShaderHasError(shader)) throw new Exception("VisionSurface ausente ou com erro de compilacao.");
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) throw new Exception("Verificacao exige dispositivo grafico.");
        var previous=SceneManagerActive();
        if(string.IsNullOrEmpty(previous.path)) previous=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(scene);
        var material=new Material(shader);
        var target=new RenderTexture(256,256,24);
        var pixels=new Texture2D(256,256,TextureFormat.RGBA32,false);
        var old=RenderTexture.active;
        var lookup=new Texture2D(192,1,TextureFormat.RGBA32,false,true);
        Mesh fogMesh=null;
        Camera camera=null;
        try
        {
            var distances=new Color32[192];
            for(int i=0;i<distances.Length;i++) distances[i]=new Color32(64,0,0,255);
            lookup.filterMode=FilterMode.Bilinear;
            lookup.wrapModeU=TextureWrapMode.Repeat;
            lookup.wrapModeV=TextureWrapMode.Clamp;
            lookup.SetPixels32(distances); lookup.Apply(false,false);
            material.SetTexture("_VisibilityLookup",lookup);
            material.SetFloat("_RayCount",192);
            material.SetFloat("_VisionRange",16);
            material.SetFloat("_NearRadius",2.4f);
            material.SetFloat("_VisionHalfAngle",70*Mathf.Deg2Rad);
            material.SetVector("_VisionOrigin",Vector4.zero);
            material.SetVector("_LookupOrigin",Vector4.zero);
            var fog=new GameObject("FogOverlay"); fog.layer=31;
            fogMesh=new Mesh();
            fogMesh.vertices=new[]{new Vector3(-6,-6,0),new Vector3(6,-6,0),new Vector3(6,6,0),new Vector3(-6,6,0)};
            fogMesh.triangles=new[]{0,1,2,0,2,3};
            fog.AddComponent<MeshFilter>().sharedMesh=fogMesh;
            var renderer=fog.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=material; renderer.sortingOrder=100;
            camera=new GameObject("RegressionCamera").AddComponent<Camera>();
            camera.orthographic=true; camera.orthographicSize=6; camera.transform.position=new Vector3(0,0,-10);
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.white;
            camera.cullingMask=1<<31;
            camera.gameObject.AddComponent<UniversalAdditionalCameraData>().SetRenderer(0);
            target.Create(); camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,256,256),0,0); pixels.Apply();
            float clear=pixels.GetPixel(149,128).r;
            float boundary=pixels.GetPixel(206,128).r;
            float shadow=pixels.GetPixel(235,128).r;
            if(clear<.9f || !(clear>boundary && boundary>shadow) || shadow<.1f)
                throw new Exception("Fog deve preservar area clara e transicao gradual ate a sombra.");
            Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/vision-surface-check.png",pixels.EncodeToPNG());
            Debug.Log("VISION FOG CHECK PASS: interior claro, borda suavizada e sombra com opacidade limitada.");
        }
        finally
        {
            if(camera!=null) camera.targetTexture=null;
            RenderTexture.active=old; target.Release(); UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(pixels); UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(lookup);
            if(fogMesh!=null) UnityEngine.Object.DestroyImmediate(fogMesh);
            EditorSceneManager.CloseScene(scene,true); EditorSceneManager.SetActiveScene(previous);
        }
    }
    static void ChargerChecks()
    {
        var actor=new GameObject("ChargeRegression");
        actor.AddComponent<CyberExtraction.Combat.Health>();
        actor.AddComponent<SpriteRenderer>();
        var monster=actor.AddComponent<CyberExtraction.Enemies.Monster>();
        const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var type=monster.GetType();
        var origin=new Vector2(1000,1000);
        type.GetField("home",flags).SetValue(monster,origin);
        var track=type.GetMethod("TryTrackChargeAim",flags);
        object[] args={origin,origin+new Vector2(7,7),0f};
        if(!(bool)track.Invoke(monster,args) || Mathf.Abs((float)args[2]-45)>.01f) throw new Exception("Mira nao acompanha alvo alem do raio de aquisicao.");
        args=new object[]{origin,origin+new Vector2(2,0),0f};
        if(!(bool)track.Invoke(monster,args)) throw new Exception("Alvo aproximando cancela acompanhamento.");
        var wall=new GameObject("ChargeCover"); wall.layer=GameConfig.WallLayer;
        wall.transform.position=origin+new Vector2(3,3); wall.AddComponent<BoxCollider2D>(); Physics2D.SyncTransforms();
        args=new object[]{origin,origin+new Vector2(7,7),0f};
        if((bool)track.Invoke(monster,args)) throw new Exception("Mira acompanha alvo atraves de parede.");
        UnityEngine.Object.DestroyImmediate(wall); UnityEngine.Object.DestroyImmediate(actor);
        Debug.Log("CHARGER TRACKING CHECK PASS: acompanha alem do alcance inicial e de perto; parede bloqueia acompanhamento.");
    }
    static UnityEngine.SceneManagement.Scene SceneManagerActive() => UnityEngine.SceneManagement.SceneManager.GetActiveScene();
}
