using TMPro;
using UnityEngine;

namespace CyberExtraction.UI
{
    public static class UiFontAssets
    {
        static TMP_FontAsset regular,bold,title;
        public static TMP_FontAsset Regular=>regular!=null ? regular : regular=Load("BarlowCondensed-Regular");
        public static TMP_FontAsset Bold=>bold!=null ? bold : bold=Load("BarlowCondensed-SemiBold");
        public static TMP_FontAsset Title=>HudTheme.Somber ? (title!=null ? title : title=Load("Cinzel")) : Bold;
        static TMP_FontAsset Load(string name)
        {
            var font=Resources.Load<TMP_FontAsset>("UI/Fonts/"+name+"-SDF");
            if(font==null)
            {
                var source=Resources.Load<Font>("UI/Fonts/"+name);
                font=source!=null ? TMP_FontAsset.CreateFontAsset(source) : TMP_Settings.defaultFontAsset;
            }
            PrepareEllipsis(font);
            return font;
        }
        static void PrepareEllipsis(TMP_FontAsset font)
        {
            // TMP resolves weighted special characters through this table before
            // consulting the base glyph. Our SemiBold is already a physical face.
            RegisterWeight(font,6,font);
            RegisterWeight(font,7,font);
            // Dynamic font atlases can be cleared for a build. Prepare TMP's
            // overflow character before any active prefab triggers a canvas rebuild.
            if(font==null || font.HasCharacter('\u2026',false,true)) return;
            var fallback=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            RegisterWeight(fallback,6,fallback); RegisterWeight(fallback,7,fallback);
            if(fallback==null || fallback==font || !fallback.HasCharacter('\u2026',false,true)) return;
            if(font.fallbackFontAssetTable==null) font.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>();
            if(!font.fallbackFontAssetTable.Contains(fallback)) font.fallbackFontAssetTable.Add(fallback);
        }
        static void RegisterWeight(TMP_FontAsset font,int index,TMP_FontAsset face)
        {
            if(font==null || face==null || font.fontWeightTable==null || font.fontWeightTable.Length<=index) return;
            var pair=font.fontWeightTable[index];
            if(pair.regularTypeface!=null && pair.regularTypeface!=font) return;
            pair.regularTypeface=face; font.fontWeightTable[index]=pair;
        }
        public static void PrepareForUi()
        {
            var normal=Regular; var strong=Bold; _=Title;
            RegisterWeight(normal,6,strong); RegisterWeight(normal,7,strong);
        }
        public static void Apply(GameObject root)
        {
            foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            { text.font=text.fontWeight>=FontWeight.SemiBold ? Bold : Regular; text.color=HudTheme.Text; }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { regular=bold=title=null; }
    }
}
