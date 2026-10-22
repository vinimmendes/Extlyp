using System.Collections.Generic;
using UnityEngine;

namespace CyberExtraction.UI
{
    public static class HudLayout
    {
        const string LegacyKey="hud_layout_v1", Key="hud_layout_refined_v2_";
        static readonly Dictionary<HudControl,HudPlacement> placements=new Dictionary<HudControl,HudPlacement>();
        static readonly Dictionary<int,string> stagedProfiles=new Dictionary<int,string>();
        static int loadedProfile=-1;
        public static float WeaponContentWidth=104,HealthContentWidth=144;
        [System.Serializable] public class LayoutData { public int Version=2; public LayoutEntry[] Controls; public ProfileData[] Profiles; }
        [System.Serializable] public class ProfileData { public int Profile; public string Json; }
        [System.Serializable] public struct LayoutEntry { public HudControl Control; public HudPlacement Placement; }
        public static readonly HudControl[] Controls=(HudControl[])System.Enum.GetValues(typeof(HudControl));
        public static bool Touch=>Players.GameInput.TouchActive;
        static string ProfileKey=>Key+(int)HudMetrics.Profile;
        static void EnsureLoaded()
        {
            if(loadedProfile==(int)HudMetrics.Profile) return;
            if(loadedProfile>=0) stagedProfiles[loadedProfile]=CaptureControls();
            placements.Clear(); loadedProfile=(int)HudMetrics.Profile;
            if(stagedProfiles.TryGetValue(loadedProfile,out var staged)) Restore(staged);
            else if(PlayerPrefs.HasKey(ProfileKey)) Restore(PlayerPrefs.GetString(ProfileKey));
        }
        public static HudPlacement Placement(HudControl control)
        {
            control=LayoutSlot(control);
            EnsureLoaded(); var p=placements.TryGetValue(control,out var saved) ? saved : DefaultPlacement(control);
            p.Offset*=HudMetrics.Unit; p.Size*=HudMetrics.Unit; return p;
        }
        static HudControl LayoutSlot(HudControl control)
        {
            // Troca a geometria efetiva, preservando os perfis salvos e usando
            // os mesmos limites para desenho, toque e edicao.
            if(!Players.TouchAimSettings.SwapAimFire) return control;
            return control==HudControl.Aim ? HudControl.Fire : control==HudControl.Fire ? HudControl.Aim : control;
        }
        public static Vector2 DefaultSize(HudControl control)=>DefaultPlacement(LayoutSlot(control)).Size*HudMetrics.Unit;
        static HudPlacement At(float ax,float ay,float x,float y,float w,float h)=>new HudPlacement(new Vector2(ax,ay),new Vector2(x,y),new Vector2(w,h));
        static HudPlacement DefaultPlacement(HudControl control)
        {
            float a=HudMetrics.Action,f=HudMetrics.Fire,m=HudMetrics.Move;
            var map=HudMetrics.Map; float row=4*a+24, right=16+f+12;
            switch(control)
            {
                case HudControl.Health: return At(0,0,12,12,144,29);
                case HudControl.Stamina: return At(0,0,12,43,144,29);
                case HudControl.Food: return At(0,0,12,78,96,20);
                case HudControl.Spores: return At(0,0,12,100,96,20);
                case HudControl.Clock: return At(.5f,0,-48,12,96,52);
                case HudControl.Minimap: return At(1,0,-12-map.x,12,map.x,map.y);
                case HudControl.Menu: return At(1,0,-12-map.x-8-48,12,48,48);
                case HudControl.Map: return At(1,0,0,0,0,0);
                case HudControl.Move: return At(0,1,16,-16-m,m,m);
                case HudControl.Sprint: return At(0,1,24+m,-16-m-8,48,48);
                case HudControl.Fire: return At(1,1,-16-f,-16-f,f,f);
                case HudControl.Aim: return At(1,1,-16-f/2-24,-16-f-12-48,48,48);
                case HudControl.Heal: return At(1,1,-right-row,-16-a,a,a);
                case HudControl.Swap: return At(1,1,-right-row+a+8,-16-a,a,a);
                case HudControl.Reload: return At(1,1,-right-row+2*(a+8),-16-a,a,a);
                case HudControl.Backpack: return At(1,1,-right-a,-16-a,a,a);
                case HudControl.Weapon: return At(1,1,-right-row/2-52,-16-a-8-40,104,40);
                // Contextual actions sit at the bottom center of the safe area.
                // Saved placements still take precedence in Placement().
                case HudControl.Interaction: return At(.5f,1,-100,-16-48,200,48);
                default: return At(.5f,1,-100,-16-a-8-48-12-48,200,48);
            }
        }
        public static Rect Bounds(HudControl control)
        {
            if(control==HudControl.Map) return Rect.zero;
            if(control==HudControl.Stamina)
            { var h=Bounds(HudControl.Health); return new Rect(h.x,h.yMax+4,h.width,h.height); }
            if(control==HudControl.Food || control==HudControl.Spores)
            { var s=Bounds(HudControl.Stamina); return new Rect(s.x,s.yMax+HudMetrics.Dp(control==HudControl.Food ? 6 : 28),HudMetrics.Dp(96),HudMetrics.Dp(20)); }
            var p=Placement(control); var size=p.Size;
            if(control==HudControl.Weapon) size.x=Mathf.Max(size.x,HudMetrics.Dp(WeaponContentWidth));
            if(control==HudControl.Health) size.x=Mathf.Max(size.x,HudMetrics.Dp(HealthContentWidth));
            if(Interactive(control)) size=Vector2.Max(size,Vector2.one*HudMetrics.Dp(48));
            // Information cannot be shrunk below the size needed for its typography.
            if(!Interactive(control)) size=Vector2.Max(size,DefaultSize(control));
            size=Vector2.Min(size,new Vector2(UiLayout.Width,UiLayout.Height));
            var pos=new Vector2(UiLayout.Width*p.Anchor.x,UiLayout.Height*p.Anchor.y)+p.Offset;
            if(control==HudControl.Weapon) pos.x-=(size.x-p.Size.x)/2;
            float reserve=control==HudControl.Health ? size.y+4+HudMetrics.Dp(48) : 0;
            return new Rect(Mathf.Clamp(pos.x,0,Mathf.Max(0,UiLayout.Width-size.x)),Mathf.Clamp(pos.y,0,Mathf.Max(0,UiLayout.Height-size.y-reserve)),size.x,size.y);
        }
        public static bool Interactive(HudControl c)=>c!=HudControl.Map && c!=HudControl.Health && c!=HudControl.Stamina && c!=HudControl.Food && c!=HudControl.Spores && c!=HudControl.Clock && c!=HudControl.Weapon;
        public static Rect HitBounds(HudControl control)=>Bounds(control);
        public static Rect ScreenBounds(HudControl control)
        { var r=HitBounds(control); var safe=UiLayout.SafeScreen; return new Rect(safe.x+r.x*UiLayout.Scale,safe.yMax-r.yMax*UiLayout.Scale,r.width*UiLayout.Scale,r.height*UiLayout.Scale); }
        public static Rect Detail(HudControl control,float x,float y,float width,float height)
        { var r=Bounds(control); var basis=DefaultSize(control); return new Rect(r.x+x*r.width/basis.x,r.y+y*r.height/basis.y,width*r.width/basis.x,height*r.height/basis.y); }
        public static int TextSize(HudControl control,int size)=>Mathf.RoundToInt(HudMetrics.Dp(size));
        static bool Finite(float v)=>!float.IsNaN(v) && !float.IsInfinity(v);
        public static void SetPlacement(HudControl control,HudPlacement p)
        { SetStoredPlacement(LayoutSlot(control),p); }
        static void SetStoredPlacement(HudControl control,HudPlacement p)
        {
            EnsureLoaded();
            if(!Finite(p.Offset.x) || !Finite(p.Offset.y) || !Finite(p.Size.x) || !Finite(p.Size.y) || !Finite(p.Anchor.x) || !Finite(p.Anchor.y)) return;
            p.Anchor=new Vector2(Mathf.Clamp01(p.Anchor.x),Mathf.Clamp01(p.Anchor.y));
            p.Offset/=HudMetrics.Unit; p.Size/=HudMetrics.Unit;
            p.Size=Vector2.Max(p.Size,Interactive(control) ? Vector2.one*48 : DefaultPlacement(control).Size);
            if(Circular(control)) p.Size=Vector2.one*Mathf.Max(p.Size.x,p.Size.y);
            placements[control]=p;
        }
        static bool Circular(HudControl c)=>c==HudControl.Move || c==HudControl.Sprint || c==HudControl.Aim || c==HudControl.Fire || c==HudControl.Menu || c==HudControl.Heal || c==HudControl.Swap || c==HudControl.Reload || c==HudControl.Backpack;
        public static HudControl EditingControl(HudControl c)=>c==HudControl.Stamina || c==HudControl.Food || c==HudControl.Spores ? HudControl.Health : c;
        static bool OverlapsOther(HudControl c)
        {
            if(!Interactive(c)) return false;
            var box=HitBounds(c);
            foreach(var other in Controls) if(other!=c && Interactive(other) && box.Overlaps(HitBounds(other))) return true;
            return false;
        }
        public static void MoveForEditing(HudControl c,Vector2 position)
        {
            var target=EditingControl(c); var p=Placement(target); var before=p;
            p.Offset+=position-Bounds(c).position; SetPlacement(target,p);
            if(OverlapsOther(target)) SetPlacement(target,before);
        }
        public static void ResizeForEditing(HudControl c,float scale)
        {
            var target=EditingControl(c); var p=Placement(target); var before=p; var center=Bounds(target).center;
            p.Size=DefaultSize(target)*scale; SetPlacement(target,p);
            p=Placement(target); p.Offset+=center-Bounds(target).center; SetPlacement(target,p);
            if(OverlapsOther(target)) SetPlacement(target,before);
        }
        public static void ResetPlacement(HudControl control) { EnsureLoaded(); placements.Remove(LayoutSlot(control)); }
        public static void RestoreDefaults() { EnsureLoaded(); placements.Clear(); }
        public static bool HasLegacy=>PlayerPrefs.HasKey(LegacyKey);
        public static void RestoreLegacy() { if(HasLegacy) Restore(PlayerPrefs.GetString(LegacyKey)); }
        public static string Capture()
        {
            EnsureLoaded(); stagedProfiles[loadedProfile]=CaptureControls();
            var profiles=new List<ProfileData>();
            foreach(var p in stagedProfiles) profiles.Add(new ProfileData {Profile=p.Key,Json=p.Value});
            return JsonUtility.ToJson(new LayoutData {Profiles=profiles.ToArray()},true);
        }
        static string CaptureControls()
        {
            var entries=new List<LayoutEntry>();
            foreach(var p in placements) entries.Add(new LayoutEntry {Control=p.Key,Placement=p.Value});
            return JsonUtility.ToJson(new LayoutData {Controls=entries.ToArray()},true);
        }
        public static void Restore(string json)
        {
            try
            {
                var data=JsonUtility.FromJson<LayoutData>(json);
                if(data!=null && data.Version==2 && data.Profiles!=null)
                {
                    stagedProfiles.Clear(); placements.Clear(); loadedProfile=-1;
                    foreach(var p in data.Profiles) if(p.Profile>=0 && p.Profile<=2) stagedProfiles[p.Profile]=p.Json;
                    EnsureLoaded(); return;
                }
                if(data==null || data.Controls==null || (data.Version!=1 && data.Version!=2)) return;
                placements.Clear(); loadedProfile=(int)HudMetrics.Profile;
                foreach(var entry in data.Controls)
                {
                    if(!System.Enum.IsDefined(typeof(HudControl),entry.Control)) continue;
                    var p=entry.Placement;
                    // v1 is virtual 600-high; v2 stores density-independent units.
                    if(data.Version==2) { p.Size*=HudMetrics.Unit; p.Offset*=HudMetrics.Unit; }
                    SetStoredPlacement(entry.Control,p);
                }
            }
            catch(System.ArgumentException) { Debug.LogWarning("Layout da HUD inválido; usando o padrão do perfil."); }
        }
        public static void Save()
        {
            EnsureLoaded(); stagedProfiles[loadedProfile]=CaptureControls();
            foreach(var p in stagedProfiles) PlayerPrefs.SetString(Key+p.Key,p.Value);
            PlayerPrefs.Save();
        }
        public static Rect Menu=>Bounds(HudControl.Menu);
        public static Rect Weapon=>Bounds(HudControl.Weapon);
        public static Rect Interaction=>Bounds(HudControl.Interaction);
        public static Rect CampInventory=>new Rect(UiLayout.Width-162,UiLayout.Height-70,148,50);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { placements.Clear(); stagedProfiles.Clear(); loadedProfile=-1; WeaponContentWidth=104; HealthContentWidth=144; }
    }
}
