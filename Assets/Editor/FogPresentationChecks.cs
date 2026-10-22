using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using CyberExtraction.Core;
using CyberExtraction.Players;

public static class FogPresentationChecks
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    public static void Baseline() { Render("baseline",false); }
    public static void Run()
    {
        CheckPartialChest();
        Render("adjusted",true);
        Render("adjusted-mobile",true,true);
    }
    static void Render(string label,bool assert,bool mobileBudget=false)
    {
        var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(string.IsNullOrEmpty(previous.path)) previous=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(scene);
        var rt=new RenderTexture(640,480,24);
        var pixels=new Texture2D(640,480,TextureFormat.RGBA32,false);
        var old=RenderTexture.active;
        Camera camera=null;
        PlayerVision ownedVision=null;
        try
        {
            var shader=Resources.Load<Shader>("VisionSurface");
            if(shader!=null)
                foreach(var message in ShaderUtil.GetShaderMessages(shader))
                    Debug.Log($"FOG SHADER {message.severity}: {message.message} at {message.file}:{message.line}");
            if(shader==null || ShaderUtil.ShaderHasError(shader)) throw new Exception("Fog shader compilation failed.");
            camera=new GameObject("FogTestCamera").AddComponent<Camera>();
            camera.orthographic=true; camera.orthographicSize=6;
            camera.transform.position=new Vector3(4,0,-10);
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.25f,.30f,.23f);
            camera.cullingMask=(1<<31)|(1<<GameConfig.WallLayer);
            camera.gameObject.AddComponent<UniversalAdditionalCameraData>().SetRenderer(0);
            camera.targetTexture=rt; rt.Create();
            Wall(new Vector2(3,0),new Vector2(.4f,8));
            Wall(new Vector2(7,0),new Vector2(.4f,8));
            Wall(new Vector2(5,4),new Vector2(4.4f,.4f));
            Physics2D.SyncTransforms();
            var player=new GameObject("FogTestPlayer");
            var vision=player.AddComponent<PlayerVision>();
            ownedVision=vision;
            // MonoBehaviour.Awake is not dispatched by AddComponent in Edit mode.
            typeof(PlayerVision).GetMethod("Awake",Private).Invoke(vision,null);
            if(mobileBudget)
            {
                var texture=new Texture2D(128,1,TextureFormat.RGBA32,false,true);
                texture.filterMode=FilterMode.Bilinear; texture.wrapModeU=TextureWrapMode.Repeat;
                texture.wrapModeV=TextureWrapMode.Clamp;
                var previousLookup=(Texture2D)typeof(PlayerVision).GetField("visibilityLookup",Private).GetValue(vision);
                typeof(PlayerVision).GetField("visibilityLookup",Private).SetValue(vision,texture);
                typeof(PlayerVision).GetField("lookupPixels",Private).SetValue(vision,new Color32[128]);
                typeof(PlayerVision).GetField("rayCount",Private).SetValue(vision,128);
                var material=(Material)typeof(PlayerVision).GetField("fogMaterial",Private).GetValue(vision);
                material.SetTexture("_VisibilityLookup",texture); material.SetFloat("_RayCount",128);
                UnityEngine.Object.DestroyImmediate(previousLookup);
            }
            typeof(PlayerVision).GetField("viewCamera",Private).SetValue(vision,camera);
            typeof(PlayerVision).GetMethod("RefreshLookup",Private).Invoke(vision,new object[]{Vector2.zero});
            typeof(PlayerVision).GetMethod("BuildOverlay",Private).Invoke(vision,new object[]{Vector2.zero,0f});
            var fog=(GameObject)typeof(PlayerVision).GetField("darkness",Private).GetValue(vision);
            fog.layer=31; fog.GetComponent<MeshRenderer>().enabled=true;
            Capture(camera,rt,pixels,"Logs/Fog/"+label+"-corridor.png");
            float near=Sample(pixels,camera,new Vector2(3.6f,0)).r;
            float far=Sample(pixels,camera,new Vector2(5,0)).r;
            float clear=Sample(pixels,camera,new Vector2(1,0)).r;
            float halo=near-far;
            float junction=Sample(pixels,camera,new Vector2(3.5f,4.5f)).r;
            float outside=Sample(pixels,camera,new Vector2(-1,-4)).r;
            if(assert && (Mathf.Abs(junction-far)>.015f || Mathf.Abs(outside-far)>.015f))
                throw new Exception($"Fog gray mismatch / corner leak: junction={junction}, outside={outside}, shadow={far}");
            Debug.Log($"FOG MEASURE {label}: near={near:F5} far={far:F5} clear={clear:F5} halo={halo:F5}");
            if(assert && (Mathf.Abs(halo)>.015f || near>=clear*.65f || far<.02f))
                throw new Exception("Fog must keep the same gray behind the wall without halo or black fill.");
            typeof(PlayerVision).GetMethod("BuildOverlay",Private).Invoke(vision,new object[]{Vector2.zero,35f});
            Capture(camera,rt,pixels,"Logs/Fog/"+label+"-corner.png");
            float seamAbove=Sample(pixels,camera,new Vector2(5,.05f)).r;
            float seamBelow=Sample(pixels,camera,new Vector2(5,-.05f)).r;
            if(assert && Mathf.Abs(seamAbove-seamBelow)>.015f) throw new Exception("Fog angular seam at 0/360.");
            var movedOrigin=new Vector2(.2f,.1f);
            typeof(PlayerVision).GetMethod("RefreshLookup",Private).Invoke(vision,new object[]{movedOrigin});
            typeof(PlayerVision).GetMethod("BuildOverlay",Private).Invoke(vision,new object[]{movedOrigin,0f});
            Capture(camera,rt,pixels,"Logs/Fog/"+label+"-moved.png");
            float movedNear=Sample(pixels,camera,new Vector2(3.6f,0)).r;
            float movedFar=Sample(pixels,camera,new Vector2(5,0)).r;
            if(assert && Mathf.Abs(movedNear-movedFar)>.015f) throw new Exception("Halo after player movement.");
            Debug.Log($"FOG PRESENTATION {label} PASS: real GPU render, halo, gray shadow, seam and moved origin. Budget={(mobileBudget ? 128 : 192)}");
        }
        finally
        {
            if(camera!=null) camera.targetTexture=null;
            if(ownedVision!=null)
                foreach(var field in new[]{"mesh","fogMaterial","visibilityLookup"})
                {
                    var resource=typeof(PlayerVision).GetField(field,Private).GetValue(ownedVision) as UnityEngine.Object;
                    if(resource!=null) UnityEngine.Object.DestroyImmediate(resource);
                }
            RenderTexture.active=old; rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(pixels);
            EditorSceneManager.CloseScene(scene,true);
            if(previous.IsValid()) EditorSceneManager.SetActiveScene(previous);
        }
    }
    static void CheckPartialChest()
    {
        var chest=new GameObject("ChestSightRegression");
        var blocker=new GameObject("ChestSightWall");
        blocker.layer=GameConfig.WallLayer;
        var box=blocker.AddComponent<BoxCollider2D>();
        Vector2 origin=new Vector2(1000,1000);
        try
        {
            blocker.SetActive(false);
            chest.transform.position=origin+new Vector2(5,8);
            if(CyberExtraction.Match.ServerVisibility.CanSee(origin,0,chest.transform.position))
                throw new Exception("Chest fixture must have its center outside the cone.");
            if(!CyberExtraction.Match.ServerVisibility.CanSeeChest(origin,0,chest.transform))
                throw new Exception("Partially exposed chest was not observed.");
            chest.transform.position=origin+new Vector2(16.6f,0);
            if(!CyberExtraction.Match.ServerVisibility.CanSeeChest(origin,0,chest.transform))
                throw new Exception("Chest front edge at the range boundary was not observed.");
            chest.transform.position=origin+new Vector2(18,0);
            if(CyberExtraction.Match.ServerVisibility.CanSeeChest(origin,0,chest.transform))
                throw new Exception("Chest wholly outside range was observed.");
            chest.transform.position=origin+new Vector2(5,8);
            blocker.SetActive(true); blocker.transform.position=origin+new Vector2(2.5f,4);
            box.size=new Vector2(.2f,.8f); Physics2D.SyncTransforms();
            if(CyberExtraction.Match.ServerVisibility.CanSee(origin,0,chest.transform.position))
                throw new Exception("Chest center should be covered by fixture wall.");
            if(!CyberExtraction.Match.ServerVisibility.CanSeeChest(origin,0,chest.transform))
                throw new Exception("Exposed chest corner should remain observable past cover.");
            box.size=new Vector2(1,6); Physics2D.SyncTransforms();
            if(CyberExtraction.Match.ServerVisibility.CanSeeChest(origin,0,chest.transform))
                throw new Exception("Fully covered chest must remain hidden.");
            blocker.transform.position=origin+new Vector2(3,0);
            blocker.transform.rotation=Quaternion.Euler(0,0,45); box.size=new Vector2(.4f,8);
            Physics2D.SyncTransforms();
            var method=typeof(PlayerVision).GetMethod("WallExitDistance",BindingFlags.Static|BindingFlags.NonPublic);
            float exit=(float)method.Invoke(null,new object[]{box,origin,Vector2.right});
            float expected=3+.2f*Mathf.Sqrt(2);
            if(Mathf.Abs(exit-expected)>.001f) throw new Exception("Rotated wall exit must match its real collider, not the AABB.");
            Debug.Log("CHEST PARTIAL SIGHT PASS: cone edge, range edge, exposed corner, full cover; rotated wall exit PASS.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(chest);
            UnityEngine.Object.DestroyImmediate(blocker);
        }
    }
    static void Wall(Vector2 center,Vector2 size)
    {
        var go=new GameObject("FogTestWall"); go.layer=GameConfig.WallLayer;
        go.transform.position=center;
        go.AddComponent<BoxCollider2D>().size=size;
        var sprite=go.AddComponent<SpriteRenderer>();
        SpriteFactory.Setup(sprite,SpriteFactory.Square,new Color(.48f,.47f,.44f),0);
        // Scale sprite separately so the collider remains in world-sized local coordinates.
        var visual=new GameObject("WallVisual"); visual.layer=GameConfig.WallLayer;
        visual.transform.SetParent(go.transform,false); visual.transform.localScale=new Vector3(size.x,size.y,1);
        SpriteFactory.Setup(visual.AddComponent<SpriteRenderer>(),SpriteFactory.Square,sprite.color,0);
        UnityEngine.Object.DestroyImmediate(sprite);
    }
    static Color Sample(Texture2D pixels,Camera camera,Vector2 world)
    {
        Vector3 uv=camera.WorldToViewportPoint(world);
        return pixels.GetPixel(Mathf.Clamp(Mathf.RoundToInt(uv.x*pixels.width),0,pixels.width-1),
            Mathf.Clamp(Mathf.RoundToInt(uv.y*pixels.height),0,pixels.height-1));
    }
    static void Capture(Camera camera,RenderTexture rt,Texture2D pixels,string path)
    {
        camera.Render(); RenderTexture.active=rt;
        pixels.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); pixels.Apply();
        Directory.CreateDirectory("Logs/Fog"); File.WriteAllBytes(path,pixels.EncodeToPNG());
    }
}
