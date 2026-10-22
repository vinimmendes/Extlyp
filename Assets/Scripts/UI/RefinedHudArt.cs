using System.Collections.Generic;
using CyberExtraction.Items;
using UnityEngine;

namespace CyberExtraction.UI
{
    public static class RefinedHudArt
    {
        [System.Serializable] sealed class Manifest { public Slot[] Slots; public Theme[] Themes; }
        [System.Serializable] sealed class Slot { public string Name; public int Atlas,X,Y,Width,Height,Border; }
        [System.Serializable] sealed class Theme { public bool Somber; public string Button,Fire,Panel; }
        static Manifest manifest;
        static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        static readonly Texture2D[] atlases=new Texture2D[2];
        static void Load()
        {
            if(manifest!=null) return;
            var source=Resources.Load<TextAsset>("UI/RefinedHud/Visuals");
            manifest=source!=null ? JsonUtility.FromJson<Manifest>(source.text) : new Manifest();
            if(manifest.Slots==null) return;
            foreach(var s in manifest.Slots)
            {
                if(atlases[s.Atlas]==null) atlases[s.Atlas]=Resources.Load<Texture2D>("UI/RefinedHud/Atlas"+s.Atlas);
                if(atlases[s.Atlas]==null) continue;
                var sprite=Sprite.Create(atlases[s.Atlas],new Rect(s.X,s.Y,s.Width,s.Height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,Vector4.one*s.Border);
                sprite.name="Refined HUD / "+s.Name; sprite.hideFlags=HideFlags.HideAndDontSave; sprites[s.Name]=sprite;
            }
        }
        public static Sprite Get(string name) { Load(); return sprites.TryGetValue(name,out var sprite) ? sprite : null; }
        public static Sprite Surface(bool circle,bool primary=false)
        {
            Load();
            if(manifest.Themes!=null) foreach(var t in manifest.Themes) if(t.Somber==HudTheme.Somber) return Get(circle ? primary ? t.Fire : t.Button : t.Panel);
            return null;
        }
        public static Sprite Icon(HudSymbol symbol)=>symbol==HudSymbol.Ammo || symbol==HudSymbol.Bullet || symbol==HudSymbol.Pistol ? UiSprites.Icon(symbol) : Get(symbol.ToString()) ?? UiSprites.Icon(symbol);
        public static Sprite Weapon(ItemId id)=>ItemCatalog.Available(id) ? ItemIconLibrary.Get(id) : null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset()
        { foreach(var s in sprites.Values) if(s!=null) Object.Destroy(s); sprites.Clear(); manifest=null; atlases[0]=atlases[1]=null; }
    }
}
