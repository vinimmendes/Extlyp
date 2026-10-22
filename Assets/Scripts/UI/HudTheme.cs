using System.Collections.Generic;
using UnityEngine;

namespace CyberExtraction.UI
{
    public enum HudAppearance { Simple, Somber }
    public static class HudTheme
    {
        static bool loaded;
        static HudAppearance appearance;
        static readonly GUISkin[] skins=new GUISkin[2];
        static readonly GUIStyle[] circles=new GUIStyle[2];
        static readonly List<Texture2D> textures=new List<Texture2D>();
        static readonly Dictionary<int,GUIStyle> labels=new Dictionary<int,GUIStyle>();
        static GUIStyle invisibleButton;
        static Font regularFont,numberFont;
        static bool fontsLoaded;
        static void LoadFonts()
        {
            if(fontsLoaded) return;
            regularFont=Resources.Load<Font>("UI/Fonts/BarlowCondensed-Regular");
            numberFont=Resources.Load<Font>("UI/Fonts/BarlowCondensed-SemiBold"); fontsLoaded=true;
        }
        public static HudAppearance Appearance { get { if(!loaded) { appearance=(HudAppearance)Mathf.Clamp(PlayerPrefs.GetInt("hud_theme",0),0,1); loaded=true; } return appearance; } }
        public static bool Somber=>Appearance==HudAppearance.Somber;
        public static Color Text=>Somber ? new Color(.9f,.85f,.73f) : new Color(.94f,.97f,.94f);
        public static Color Muted=>Somber ? new Color(.69f,.66f,.54f) : new Color(.66f,.76f,.7f);
        public static Color Accent=>Somber ? new Color(.67f,.47f,.29f) : new Color(.38f,.7f,.53f);
        public static Color Panel=>Somber ? new Color(.075f,.09f,.065f,.98f) : new Color(.09f,.14f,.115f,.97f);
        public static Color Good=>Somber ? new Color(.48f,.61f,.36f) : new Color(.34f,.72f,.49f);
        public static Color Danger=>Somber ? new Color(.68f,.32f,.24f) : new Color(.9f,.35f,.28f);
        public static void Select(HudAppearance value) { appearance=value; loaded=true; PlayerPrefs.SetInt("hud_theme",(int)value); PlayerPrefs.Save(); }
        public static GUISkin Skin(GUISkin original)
        {
            int index=(int)Appearance;
            if(skins[index]!=null) return skins[index];
            // IMGUI is retained so scenes and existing network prefabs do not need replacement.
            var skin=Object.Instantiate(original); skin.name="HUD "+Appearance; skin.hideFlags=HideFlags.HideAndDontSave;
            LoadFonts(); if(regularFont!=null) skin.font=regularFont;
            var panel=Texture(Panel,Accent,Somber ? 2 : 8,Somber);
            var button=Texture(Somber ? new Color(.12f,.135f,.09f,.98f) : new Color(.15f,.22f,.18f),Accent,Somber ? 3 : 8,Somber);
            var active=Texture(Color.Lerp(Panel,Accent,.35f),Accent,Somber ? 3 : 8,Somber);
            Set(skin.box,panel,panel); Set(skin.window,panel,panel); Set(skin.button,button,active);
            Set(skin.textField,button,active); Set(skin.textArea,button,active);
            skin.label.normal.textColor=Text; skin.label.wordWrap=true; skin.label.fontSize=18;
            skin.button.fontSize=17; skin.button.wordWrap=true;
            skin.box.fontSize=18; skin.box.wordWrap=true; skin.box.alignment=TextAnchor.MiddleCenter;
            skin.toggle.normal.textColor=skin.toggle.onNormal.textColor=Text;
            skin.toggle.hover.textColor=skin.toggle.onHover.textColor=Text; skin.toggle.fontSize=17;
            skins[index]=skin; return skin;
        }
        static void Set(GUIStyle style,Texture2D normal,Texture2D selected)
        {
            style.normal.background=style.onNormal.background=normal;
            style.hover.background=style.active.background=style.focused.background=selected;
            style.onHover.background=style.onActive.background=style.onFocused.background=selected;
            style.normal.textColor=style.hover.textColor=style.active.textColor=style.focused.textColor=Text;
            style.onNormal.textColor=style.onHover.textColor=style.onActive.textColor=style.onFocused.textColor=Text;
            style.border=new RectOffset(8,8,8,8); style.padding=new RectOffset(8,8,5,5);
        }
        // Shared with retained uGUI panels so migration keeps the original finish.
        internal static Texture2D Surface(bool button)
        {
            var fill=button ? (Somber ? new Color(.12f,.135f,.09f,.98f) : new Color(.15f,.22f,.18f)) : Panel;
            return Texture(fill,Accent,Somber ? (button ? 3 : 2) : 8,Somber);
        }
        static Texture2D Texture(Color fill,Color edge,int radius,bool grain)
        {
            const int size=64;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false) { name="HudPanel",hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp };
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                int distance=Mathf.Min(Mathf.Min(x,size-1-x),Mathf.Min(y,size-1-y));
                float dx=Mathf.Max(0,radius-Mathf.Min(x,size-1-x)),dy=Mathf.Max(0,radius-Mathf.Min(y,size-1-y));
                if(dx*dx+dy*dy>radius*radius) { pixels[y*size+x]=Color.clear; continue; }
                Color color=distance<1 ? Color.Lerp(fill,edge,.7f) : fill;
                if(grain && distance>1) { float n=((x*17+y*31+x*y*3)%19-9)*.0018f; color.r+=n; color.g+=n; color.b+=n; }
                pixels[y*size+x]=color;
            }
            texture.SetPixels(pixels); texture.Apply(false,true); textures.Add(texture); return texture;
        }
        public static GUIStyle Label(int size=18,TextAnchor alignment=TextAnchor.UpperLeft)
        {
            int key=((int)Appearance*10+(int)alignment)*100+size;
            if(!labels.TryGetValue(key,out var style))
            { LoadFonts(); style=new GUIStyle(GUI.skin.label) { fontSize=size,alignment=alignment,wordWrap=true,padding=new RectOffset(),margin=new RectOffset() };
              var font=size>=22 ? numberFont : regularFont; if(font!=null) style.font=font; labels.Add(key,style); }
            style.normal.textColor=Text; return style;
        }
        public static GUIStyle InvisibleButton=>invisibleButton ?? (invisibleButton=new GUIStyle(GUIStyle.none));
        public static GUIStyle ActionStyle
        {
            get
            {
                if(!Somber) return GUI.skin.button;
                int index=(int)Appearance;
                if(circles[index]!=null) return circles[index];
                var style=new GUIStyle(GUI.skin.button); Set(style,Circle(Panel),Circle(Color.Lerp(Panel,Accent,.3f)));
                style.border=new RectOffset(); circles[index]=style; return style;
            }
        }
        static Texture2D Circle(Color fill)
        {
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false) { hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp };
            var pixels=new Color[4096];
            for(int y=0;y<64;y++) for(int x=0;x<64;x++) { float d=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f)); pixels[y*64+x]=d>31.5f ? Color.clear : d>30 ? Accent : fill; }
            texture.SetPixels(pixels); texture.Apply(false,true); textures.Add(texture); return texture;
        }
        public static void Fill(Rect rect,Color color)
        { var old=GUI.color; GUI.color=color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=old; }
        public static void Meter(Rect rect,float value,Color color)
        { Fill(rect,new Color(.5f,.5f,.5f,.18f)); Fill(new Rect(rect.x,rect.y,rect.width*Mathf.Clamp01(value),rect.height),color); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset()
        { foreach(var skin in skins) if(skin!=null) Object.Destroy(skin); skins[0]=skins[1]=null; circles[0]=circles[1]=null; foreach(var texture in textures) if(texture!=null) Object.Destroy(texture); textures.Clear(); labels.Clear(); invisibleButton=null; regularFont=numberFont=null; fontsLoaded=loaded=false; }
    }
    public enum HudControl { Health, Food, Spores, Clock, Map, Menu, Weapon, Heal, Swap, Reload, Backpack, Move, Aim, Fire, Interaction, Minimap, Sprint, Stamina }
    [System.Serializable]
    public struct HudPlacement
    {
        public Vector2 Anchor, Offset, Size;
        public HudPlacement(Vector2 anchor,Vector2 offset,Vector2 size) { Anchor=anchor; Offset=offset; Size=size; }
    }
}
