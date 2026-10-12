// Executes actual inventory, camp progress and cooking activity code outside Unity.
using System;
using CyberExtraction.Camp;
using CyberExtraction.Items;
class CookingInventoryProbe
{
    static void Check(bool ok,string name) { if(!ok) throw new Exception(name); Console.WriteLine("PASS "+name); }
    static int Slot(CampProgress p,ItemId item)=>Array.FindIndex(p.Pack.Slots,s=>!s.Empty && s.Item==item);
    static float Perfect=CookingGame.TraverseSeconds*(CookingGame.PerfectStart+CookingGame.PerfectEnd)/2;
    static bool Cook(CampActivity a,CampProgress p,ItemId item,float at,float elapsed)
    { Check(a.BeginCooking(p,Slot(p,item),at,at),"start "+item); return a.FinishCooking(p,at+elapsed,elapsed,at+elapsed); }
    static void Main(string[] args)
    {
        bool before=args.Length>0 && args[0]=="--expect-bug";
        var p=CampTestResources.Create(); var a=new CampActivity();
        Check(Cook(a,p,ItemId.FishCommon,100,Perfect),"first fish cooks with one empty slot");
        bool second=Cook(a,p,ItemId.FishRare,110,Perfect);
        if(before)
        {
            Check(!second && a.Busy && p.Pack.Count(ItemId.FishRare)==3,"reproduce second species rejected after first fills backpack");
            Console.WriteLine(a.CookingRejection); return;
        }
        Check(second && !a.Busy && p.Pack.Count(ItemId.FishRare)==2,"second species cooks with full backpack");
        Check(p.Chest.Count(ItemId.CookedFish)==1,"overflow sent to safe chest");
        Check(Cook(a,p,ItemId.FishCommon,120,.6f),"different quality also completes");
        Check(p.Chest.Count(ItemId.CookedFish)==1 && p.Ground.Count==1 && p.Ground[0].Stack.Quality==2 && p.Ground[0].Stack.Data==(int)ItemId.FishCommon,"different quality preserved on ground when supplied chest is full");
        Check(Cook(a,p,ItemId.FishCommon,130,Perfect),"matching output still stacks in backpack");
        Check(p.Pack.Count(ItemId.CookedFish)==2,"matching backpack stack grows");
        Check(p.Chest.Slots[Array.FindIndex(p.Chest.Slots,s=>s.Item==ItemId.CookedFish && s.Quality==3)].Data==(int)ItemId.FishRare,"origin and perfect quality preserved");
        p=new CampProgress { Pack=new InventoryGrid(),Chest=new InventoryGrid() };
        p.Pack.Slots[0]=new ItemStack(ItemId.FishLarge,2);
        for(int i=1;i<9;i++) p.Pack.Slots[i]=new ItemStack(ItemId.Wood,20);
        for(int i=0;i<9;i++) p.Chest.Slots[i]=new ItemStack(ItemId.Nails,20);
        long revision=p.Revision;
        Check(Cook(a,p,ItemId.FishLarge,140,Perfect),"both inventories full still completes");
        Check(p.Ground.Count==1 && p.Ground[0].Stack.Item==ItemId.CookedFish && p.Ground[0].Stack.Quality==3,"full chest places exact output on camp ground");
        Check(p.Pack.Count(ItemId.FishLarge)==1 && p.Revision==revision+1,"one ingredient consumed and revision advanced once");
        Check(!a.FinishCooking(p,140+Perfect,Perfect,140+Perfect) && p.Ground.Count==1,"repeat completion cannot duplicate output");
        Check(Cook(a,p,ItemId.FishLarge,150,Perfect) && p.Pack.Count(ItemId.CookedFish)==1,"last ingredient frees its own slot");
        Check(!p.Apply(new CampAction { Kind=CampActionKind.CookFish,Source=0,Sequence=p.Revision+1,Result=3,Duration=Perfect }),"forged client cooking completion remains rejected");
    }
}
namespace UnityEngine
{
    public struct Color { public Color(float r,float g,float b) {} }
    public static class Random { public static float Range(float a,float b)=>(a+b)/2; }
    public static class JsonUtility
    { public static string ToJson(object o)=>throw new NotSupportedException(); public static T FromJson<T>(string json)=>throw new NotSupportedException(); }
    public static class Mathf
    {
        public static float Clamp(float x,float a,float b)=>Math.Max(a,Math.Min(b,x));
        public static float Clamp01(float x)=>Clamp(x,0,1);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Abs(float x)=>Math.Abs(x);
        public static float Sin(float x)=>(float)Math.Sin(x);
        public static float PingPong(float t,float length) { t-=(float)Math.Floor(t/(length*2))*length*2; return length-Abs(t-length); }
        public static float MoveTowards(float a,float b,float d)=>Abs(b-a)<=d ? b : a+Math.Sign(b-a)*d;
    }
}
namespace Unity.Netcode
{
    public interface IReaderWriter {}
    public interface INetworkSerializable { void NetworkSerialize<T>(BufferSerializer<T> serializer) where T:IReaderWriter; }
    public struct BufferSerializer<T> where T:IReaderWriter { public void SerializeValue<TValue>(ref TValue value) {} }
}
namespace CyberExtraction.Items { public static class WeaponCatalog { public static int RecipeOutput(ItemId item)=>12; } }
namespace CyberExtraction.Core
{
    public class GameplayDefinition
    {
        public static readonly GameplayDefinition Current=new GameplayDefinition();
        public int Magazine=12,MetalRecipeCost=3,PowderRecipeCost=1,ComponentRecipeCost=1,ComponentPrice=2;
    }
}
