// Focused outside-Unity clock/input checks. Inventory/engine are fixtures, not integration tests.
using System;
using CyberExtraction.Camp;
using CyberExtraction.Items;
using CyberExtraction.UI;
class CookingProbe
{
    static void Check(bool ok,string name) { if(!ok) throw new Exception(name); Console.WriteLine("PASS "+name); }
    static CampProgress Profile()=>new CampProgress();
    static CampActivity Begin(CampProgress p) { var a=new CampActivity(); a.BeginCooking(p,0,1000,100); return a; }
    static void Main()
    {
        float click=.63f*CookingGame.TraverseSeconds;
        var p=Profile(); var a=Begin(p);
        Check(!a.FinishCooking(p,1001.3f,click),"reproduce mixed clocks rejecting a valid click");
        Check(a.FinishCooking(p,1001.3f,click,100+click+.1),"same network clock accepts the valid click");
        Check(a.LastCookingQuality==3 && p.Completions==1,"perfect quality computed by authoritative curve");
        Check(!a.FinishCooking(p,1001.3f,click,100+click+.1) && p.Completions==1,"completion cannot repeat");
        a=Begin(Profile());
        Check(!a.FinishCooking(Profile(),1001,click,102),"old timestamp remains rejected");
        Check(!a.FinishCooking(Profile(),1001,2,101),"future timestamp remains rejected");
        Check(!a.FinishCooking(Profile(),1001,float.NaN,101),"nonfinite timestamp remains rejected");
        p=Profile(); a=Begin(p);
        a.Tick(p,1010,0,101);
        Check(a.Busy && p.Completions==0,"realtime drift does not expire an active network QTE");
        a.Tick(p,1010,0,108.1);
        Check(!a.Busy && a.LastCookingQuality==1 && p.Completions==1,"timeout uses network clock and completes once");
        var view=new CookingPresentation(); var s=new CampSnapshot { Activity=6,CookStartedAt=100 };
        view.Tick(s,false,100+click,.016f);
        Check(Math.Abs(view.Marker-.63f)<.0001f,"visual marker matches the evaluated click");
        view.Submit(); view.Tick(s,true,101,.016f);
        Check(view.Submitted && Math.Abs(view.Marker-.63f)<.0001f,"submitted indicator freezes while pending");
        s=new CampSnapshot { Activity=6,CookStartedAt=100,Message="Clique recusado." };
        view.Tick(s,false,101,.016f);
        Check(!view.Submitted && view.Notice==s.Message,"rejection is visible and allows another input");
        view.Tick(new CampSnapshot(),false,101,.016f);
        Check(!view.Active && view.Notice==null,"completion clears pending presentation");
    }
}
namespace CyberExtraction.Camp
{
    public enum CampActionKind { CookFish }
    public class CampAction { public CampActionKind Kind; public int Sequence,Source,Result; public float Duration; internal string CookingStorage; }
    public class CampProgress
    {
        public bool PierRepaired=true; public int Revision,Completions;
        public TestPack Pack=new TestPack();
        public static ItemId CatchItem(string id)=>ItemId.FishCommon;
        public bool CompleteFishing(ItemId id)=>true;
        public bool CompleteCooking(CampAction action) { if(action.Duration<.1f) return false; Completions++; Revision++; return true; }
    }
    public class TestPack { public TestStack[] Slots={ new TestStack { Item=ItemId.FishCommon } }; }
    public struct TestStack { public ItemId Item; public bool Empty; }
}
namespace CyberExtraction.Items
{
    public enum ItemId { FishCommon,FishLarge,FishRare,RawFish }
    public static class ItemCatalog { public static bool Fish(ItemId i)=>i!=ItemId.RawFish; public static string Name(ItemId i)=>i.ToString(); }
}
namespace CyberExtraction.Players { public static class GameInput { public static bool HoldPressed; } }
namespace CyberExtraction.UI
{
    public static class MinigameView { public static UnityEngine.Rect CookingControlBounds=>default; }
    public static class UiLayout { public static UnityEngine.Vector2 Point(UnityEngine.Vector2 p)=>p; }
}
namespace UnityEngine
{
    public struct Vector2 { }
    public struct Rect { public bool Contains(Vector2 value)=>false; }
    public static class Application { public static bool isFocused=true; }
    public static class Random { public static float Range(float a,float b)=>(a+b)/2; }
    public static class Mathf
    {
        public static float Clamp(float x,float a,float b)=>Math.Max(a,Math.Min(b,x));
        public static float Clamp01(float x)=>Clamp(x,0,1);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Abs(float x)=>Math.Abs(x);
        public static float Sin(float x)=>(float)Math.Sin(x);
        public static float PingPong(float t,float length) { t-= (float)Math.Floor(t/(length*2))*length*2; return length-Abs(t-length); }
        public static float MoveTowards(float a,float b,float d)=>Abs(b-a)<=d ? b : a+Math.Sign(b-a)*d;
    }
}
namespace UnityEngine.InputSystem
{
    public class Button { public bool wasPressedThisFrame; }
    public class Position { public UnityEngine.Vector2 ReadValue()=>default; }
    public class Touch { public Button press=new Button(); public Position position=new Position(); }
    public class Touchscreen { public static Touchscreen current; public Touch[] touches=Array.Empty<Touch>(); }
    public class Mouse { public static Mouse current; public Button leftButton=new Button(); public Position position=new Position(); }
}
