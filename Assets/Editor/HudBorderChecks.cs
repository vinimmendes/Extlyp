using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CyberExtraction.UI;

public static class HudBorderChecks
{
    [MenuItem("Cyber Extraction/Verificar bordas pequenas da HUD")]
    public static void Run()
    {
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)
            throw new Exception("Verificacao de bordas exige renderizacao grafica.");
        var method=typeof(HudArt).GetMethod("MakeRing",BindingFlags.Static|BindingFlags.NonPublic);
        var appearance=HudTheme.Appearance;
        var previous=RenderTexture.active;
        int cases=0;
        float minimum=1;
        try
        {
            HudTheme.Select(HudAppearance.Somber);
            foreach(int diameter in new[] {32,50,78})
            {
                var texture=(Texture2D)method.Invoke(null,new object[] {diameter*2,(float)diameter,false,false});
                var target=new RenderTexture(diameter,diameter,0,RenderTextureFormat.ARGB32);
                var pixels=new Texture2D(diameter,diameter,TextureFormat.RGBA32,false);
                try
                {
                    // GPU reduction with the real generated texture and bilinear filtering.
                    target.Create(); Graphics.Blit(texture,target); RenderTexture.active=target;
                    pixels.ReadPixels(new Rect(0,0,diameter,diameter),0,0); pixels.Apply();
                    for(int i=0;i<64;i++)
                    {
                        float angle=i*Mathf.PI/32;
                        float radius=diameter*.5f-1-1.35f*.5f;
                        var c=pixels.GetPixelBilinear(.5f+Mathf.Cos(angle)*radius/diameter,.5f+Mathf.Sin(angle)*radius/diameter);
                        float visible=(c.r*.2126f+c.g*.7152f+c.b*.0722f)*c.a;
                        minimum=Mathf.Min(minimum,visible);
                        if(visible<.12f) throw new Exception("Borda interrompida apos reducao: "+diameter+" px, angulo "+i+", luminancia "+visible);
                    }
                    cases++;
                }
                finally
                {
                    RenderTexture.active=previous; target.Release();
                    UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(pixels);
                }
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/hud-border-check.json","{\"passed\":true,\"sizes\":[32,50,78],\"anglesPerSize\":64,\"minimumVisibleLuminance\":"+minimum.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
            Debug.Log("HUD BORDER CHECK PASS: "+cases+" tamanhos, 64 angulos por tamanho, contorno continuo apos reducao na GPU.");
        }
        finally { RenderTexture.active=previous; HudTheme.Select(appearance); }
    }
}
