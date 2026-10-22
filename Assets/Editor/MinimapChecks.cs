using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CyberExtraction.UI;
using CyberExtraction.Match;

public static class MinimapChecks
{
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Saia do Play para a verificacao isolada do minimapa.");
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) throw new Exception("A verificacao exige GPU.");
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var previousMap=MapBuilder.Current; int seed=MapBuilder.LayoutSeed;
        var active=RenderTexture.active;
        var actor=new GameObject("MinimapFocusedChecks"); actor.SetActive(false);
        var map=actor.AddComponent<Minimap>();
        var pointer=new MinimapPointer();
        RenderTexture target=null; Texture2D readback=null;
        int headings=0;
        try
        {
            // Every rotation stays in the same texture box, with clear outer pixels.
            for(int angle=0;angle<360;angle+=5)
            {
                var texture=pointer.Texture(angle); var pixels=texture.GetPixels(); float sum=0;
                for(int y=0;y<texture.height;y++) for(int x=0;x<texture.width;x++)
                {
                    float alpha=pixels[y*texture.width+x].a; sum+=alpha;
                    if((x<2 || y<2 || x>=texture.width-2 || y>=texture.height-2) && alpha>.01f)
                        throw new Exception("Marcador saiu da margem da textura em "+angle+" graus.");
                }
                if(sum<200) throw new Exception("Marcador desapareceu em "+angle+" graus."); headings++;
            }
            MapBuilder.Configure(ExpeditionMap.Large,43127); LargeMapPlan.Build(43127);
            typeof(Minimap).GetMethod("BuildTerrain",flags).Invoke(map,null);
            var terrain=(Texture2D)typeof(Minimap).GetField("terrain",flags).GetValue(map);
            if(terrain==null) throw new Exception("Atlas do minimapa ausente.");
            target=new RenderTexture(terrain.width,terrain.height,0,RenderTextureFormat.ARGB32); target.Create();
            Graphics.Blit(terrain,target); RenderTexture.active=target;
            readback=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            readback.ReadPixels(new Rect(0,0,target.width,target.height),0,0); readback.Apply();
            var point=LargeMapPlan.Routes[0].Points[0];
            int px=Mathf.RoundToInt((point.x/MapBuilder.Width+.5f)*target.width);
            int py=Mathf.RoundToInt((point.y/MapBuilder.Height+.5f)*target.height);
            if(readback.GetPixel(px,py).r<.14f) throw new Exception("Rotas reais nao aparecem no atlas.");
            typeof(Minimap).GetField("worldView",flags).SetValue(map,new Rect(-200,-300,400,600));
            var project=typeof(Minimap).GetMethod("Project",flags); var area=new Rect(120,80,178,160);
            var center=(Vector2)project.Invoke(map,new object[] {Vector2.zero,area});
            var north=(Vector2)project.Invoke(map,new object[] {new Vector2(0,300),area});
            if(Vector2.Distance(center,area.center)>.001f || Mathf.Abs(north.y-area.y)>.001f)
                throw new Exception("Projecao perdeu centro ou orientacao norte.");
            // Exercise route clipping with endpoints well outside a tiny raster.
            var clipped=new Color[64];
            typeof(Minimap).GetMethod("PaintRoute",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,
                new object[] {clipped,8,8,new Vector2(-100,-100),new Vector2(100,100),Color.white,2f});
            if(clipped[4*8+4].r<.5f) throw new Exception("Rota atravessando limites nao foi recortada corretamente.");
            Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/minimap-atlas-check.png",readback.EncodeToPNG());
            File.WriteAllText("Logs/minimap-check.json","{\"passed\":true,\"pointerAngles\":72,\"atlasRoutes\":true,\"projection\":true,\"routeClipping\":true}");
            Debug.Log("MINIMAP CHECK PASS: "+headings+" rotacoes contidas, rotas no atlas, projecao e raster limitado.");
        }
        finally
        {
            RenderTexture.active=active; pointer.Dispose();
            if(target!=null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            if(readback!=null) UnityEngine.Object.DestroyImmediate(readback);
            // Inactive edit-mode components do not run their usual lifecycle.
            typeof(Minimap).GetMethod("OnDestroy",flags).Invoke(map,null);
            UnityEngine.Object.DestroyImmediate(actor);
            MapBuilder.Configure(previousMap,seed); LargeMapPlan.Build(seed);
        }
    }
}
