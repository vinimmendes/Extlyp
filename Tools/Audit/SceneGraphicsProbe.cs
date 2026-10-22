// Compiles real SceneGraphics with local preference/event doubles, without Unity.
using System;
using System.Collections.Generic;
using System.Reflection;
using CyberExtraction.Core;
class SceneGraphicsProbe
{
    static void Check(bool result,string label) { if(!result) throw new Exception(label); Console.WriteLine("PASS "+label); }
    static void Restart()=>typeof(SceneGraphics).GetMethod("Reset",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
    static void Main()
    {
        Check(SceneGraphics.Mode==SceneGraphicsMode.Smooth,"smooth default");
        int changes=0; SceneGraphics.Changed+=()=>changes++;
        SceneGraphics.Toggle();
        Check(SceneGraphics.Mode==SceneGraphicsMode.Pixelated && changes==1,"immediate mode event");
        SceneGraphics.Select(SceneGraphicsMode.Pixelated);
        SceneGraphics.Select((SceneGraphicsMode)99);
        Check(changes==1,"same/invalid mode ignored");
        Restart(); Check(SceneGraphics.Mode==SceneGraphicsMode.Pixelated,"preference survives reload");
        SceneGraphics.Toggle(); Restart(); Check(SceneGraphics.Mode==SceneGraphicsMode.Smooth,"smooth choice persisted");
        UnityEngine.PlayerPrefs.SetInt("scene_graphics_mode_v1",99); Restart();
        Check(SceneGraphics.Mode==SceneGraphicsMode.Smooth,"invalid saved value recovers");
    }
}
namespace UnityEngine
{
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public class RuntimeInitializeOnLoadMethodAttribute:Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){} }
    public static class PlayerPrefs
    {
        static readonly Dictionary<string,int> values=new Dictionary<string,int>();
        public static int GetInt(string key,int fallback)=>values.TryGetValue(key,out int value) ? value : fallback;
        public static void SetInt(string key,int value)=>values[key]=value;
        public static void Save(){}
    }
}
