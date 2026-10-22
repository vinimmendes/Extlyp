using System;
using UnityEngine;

namespace CyberExtraction.Core
{
    public enum SceneGraphicsMode { Smooth, Pixelated }

    // Local presentation preference only; never changes gameplay visibility.
    public static class SceneGraphics
    {
        const string Key="scene_graphics_mode_v1";
        static bool loaded;
        static SceneGraphicsMode mode;
        public static event Action Changed;
        public static SceneGraphicsMode Mode
        {
            get
            {
                if(!loaded)
                {
                    mode=PlayerPrefs.GetInt(Key,0)==1 ? SceneGraphicsMode.Pixelated : SceneGraphicsMode.Smooth;
                    loaded=true;
                }
                return mode;
            }
        }
        public static string Name=>Mode==SceneGraphicsMode.Pixelated ? "Pixelado" : "Suave";
        public static void Toggle()=>Select(Mode==SceneGraphicsMode.Smooth ? SceneGraphicsMode.Pixelated : SceneGraphicsMode.Smooth);
        public static void Select(SceneGraphicsMode value)
        {
            if(value!=SceneGraphicsMode.Smooth && value!=SceneGraphicsMode.Pixelated || Mode==value) return;
            mode=value; loaded=true;
            PlayerPrefs.SetInt(Key,(int)value); PlayerPrefs.Save();
            Changed?.Invoke();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { loaded=false; mode=SceneGraphicsMode.Smooth; Changed=null; }
    }
}
