// Runs the real FishingGame/FishingPresentation with small engine/input adapters.
// Does not validate Unity input delivery, rendering, serialization or RPC generation.
using System;
using CyberExtraction.UI;
using CyberExtraction.Items;
using CyberExtraction.Players;

class FishingProbe
{
    static void Check(bool value,string name) { if(!value) throw new Exception(name); Console.WriteLine("PASS "+name); }
    static void Main()
    {
        var view=new FishingPresentation();
        view.ReadInput(true); view.Sent(0);
        GameInput.Hold=true; view.ReadInput(true);
        Check(view.NeedsSend(.01f),"press bypasses heartbeat"); view.Sent(.01f);
        GameInput.Hold=false; view.ReadInput(true);
        Check(view.NeedsSend(.02f),"release bypasses heartbeat"); view.Sent(.02f);
        Check(!view.NeedsSend(.1f) && view.NeedsSend(.23f),"held state refresh cadence");
        GameInput.Hold=true; view.ReadInput(true); view.Sent(.24f);
        view.ReadInput(false);
        Check(!view.Held && view.NeedsSend(.25f),"hidden panel releases input immediately");
        foreach(int fps in new[] {30,60,120})
        {
            view.Reset(); GameInput.Hold=true; view.ReadInput(true);
            float dt=1f/fps;
            view.Tick(3,.5f,.5f,.28f,.35f,0,dt);
            float initial=view.Bar;
            view.Tick(3,.5f,.5f,.28f,.35f,0,dt);
            Check(view.Bar>initial,"local response before snapshot at "+fps+" fps");
            for(int i=0;i<fps*2;i++) view.Tick(3,.5f,.5f,.28f,.35f,0,dt);
            Check(Math.Abs(view.Bar-.5f)<.001f && Math.Abs(view.Progress-.35f)<.0001f,"stale snapshots settle without inventing progress at "+fps+" fps");
            float bar=.5f,speed=0;
            for(int i=0;i<fps*4;i++) FishingGame.MoveBar(ref bar,ref speed,.28f,true,dt);
            Check(Math.Abs(bar-.86f)<.0001f && speed==0,"upper boundary at "+fps+" fps");
            for(int i=0;i<fps*4;i++) FishingGame.MoveBar(ref bar,ref speed,.28f,false,dt);
            Check(Math.Abs(bar-.14f)<.0001f && speed==0,"lower boundary at "+fps+" fps");
        }
        var game=new FishingGame(); game.Begin(ItemId.FishCommon,0); game.Input(true,.1f); game.Tick(.1f,.1f);
        Check(game.Phase==5,"early hook remains rejected by authority");
        game.Begin(ItemId.FishCommon,0);
        game.Tick(4,.016f); Check(game.Phase==2,"server opens bite window");
        game.Input(true,4.1f); game.Tick(4.1f,.016f);
        Check(game.Phase==3,"server accepts hook in bite window");
        view.Reset(); Check(view.Phase==0 && !view.Held,"new session clears presentation");
    }
}
namespace CyberExtraction.Items { public enum ItemId { FishCommon,FishLarge,FishRare } }
namespace CyberExtraction.Players { public static class GameInput { public static bool Hold; } }
namespace CyberExtraction.UI
{
    public static class MinigameView { public static UnityEngine.Rect FishingControlBounds=>new UnityEngine.Rect(); public static void BiteCue() {} }
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
        public static float Min(float a,float b)=>Math.Min(a,b);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Abs(float x)=>Math.Abs(x);
        public static float Sin(float x)=>(float)Math.Sin(x);
        public static float Exp(float x)=>(float)Math.Exp(x);
        public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);
        public static float MoveTowards(float a,float b,float delta)=>Math.Abs(b-a)<=delta ? b : a+Math.Sign(b-a)*delta;
    }
}
namespace UnityEngine.InputSystem
{
    public class Button { public bool wasPressedThisFrame, isPressed; }
    public class Position { public UnityEngine.Vector2 ReadValue()=>default; }
    public class TouchId { public int ReadValue()=>0; }
    public class Touch { public TouchId touchId=new TouchId(); public Button press=new Button(); public Position position=new Position(); }
    public class Touchscreen { public static Touchscreen current; public Touch[] touches=Array.Empty<Touch>(); }
    public class Mouse { public static Mouse current; public Button leftButton=new Button(); public Position position=new Position(); }
}
