// Focused resolver proof: compiles the real ItemIconLibrary against presentation doubles.
// Does not emulate Unity serialization, import, rendering or networking.
using System;
using CyberExtraction.Items;
using UnityEngine;
public static class WorldItemVisualProbe
{
    static void Expect(bool ok,string label) { if(!ok) throw new Exception(label); Console.WriteLine("PASS "+label); }
    public static void Main()
    {
        var old=new Sprite(); var ui=new Sprite(); var special=new Sprite();
        ItemVisualCatalog.InterfaceValue=ui;
        Resources.Library.icons[0]=old;
        Expect(ItemIconLibrary.GetWorld(ItemId.Wood)==old,"existing world art preserved");
        Resources.Library.icons[0]=null;
        Expect(ItemIconLibrary.GetWorld(ItemId.Wood)==ui,"missing ordinary reference uses complete UI sprite");
        Expect(ItemIconLibrary.GetWorld(ItemId.Wood)==ui && Debug.Warnings==1,"missing reference warning once per item");
        for(int i=26;i<=31;i++) Expect(ItemIconLibrary.GetWorld((ItemId)i)==ui,"training slot "+i+" never returns legacy square");
        ItemVisualCatalog.WorldValue=special;
        Expect(ItemIconLibrary.GetWorld(ItemId.Wood)==special,"explicit world override takes precedence");
        ItemVisualCatalog.WorldValue=null; DungeonToolArt.WorldValue=special;
        Expect(ItemIconLibrary.GetWorld(ItemId.DungeonKey)==special,"dungeon world art preserved");
        DungeonToolArt.WorldValue=null;
        Expect(ItemIconLibrary.GetWorld(ItemId.ShortSword)==MeleeWeaponArt.Art,"procedural melee world art preserved");
        Expect(ItemIconLibrary.GetWorld((ItemId)255)==null,"invalid item rejected");
        ItemVisualCatalog.InterfaceValue=null;
        Expect(ItemIconLibrary.GetWorld(ItemId.Nails)==null && Debug.Errors==1,"missing both sources reported");
        Console.WriteLine("World item resolver proof complete; Unity rendering not exercised.");
    }
}
namespace UnityEngine
{
    public class Object { }
    public class ScriptableObject:Object { }
    public class CreateAssetMenuAttribute:Attribute { public string menuName; }
    public class RuntimeInitializeOnLoadMethodAttribute:Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){} }
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public class Sprite:Object { public Bounds bounds; }
    public struct Bounds { public Vector3 size; }
    public struct Vector3 { public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;} public static Vector3 zero=>default; public static Vector3 one=>new Vector3(1,1,1); public static Vector3 operator*(Vector3 v,float n)=>v; public static Vector3 operator/(Vector3 v,float n)=>v; }
    public struct Quaternion { public static Quaternion identity=>default; }
    public struct Color { public static Color white=>default; }
    public static class Mathf { public static float Max(float a,float b)=>Math.Max(a,b); public static float Abs(float v)=>Math.Abs(v); public static float Clamp(float v,float a,float b)=>Math.Min(b,Math.Max(a,v)); }
    public class Transform { public GameObject gameObject; public Vector3 localPosition,localScale,lossyScale; public Quaternion localRotation; public Transform Find(string n)=>null; public void SetParent(Transform t,bool w){} }
    public class GameObject { public Transform transform=new Transform(); public GameObject(string n){} public T GetComponent<T>() where T:new()=>new T(); public T AddComponent<T>() where T:new()=>new T(); }
    public class SpriteRenderer { public int sortingOrder; public bool enabled; }
    public static class Debug { public static int Warnings,Errors; public static void LogWarning(object s){Warnings++;} public static void LogError(object s){Errors++;} }
    public static class Resources { public static ItemIconLibrary Library=new ItemIconLibrary(); public static T Load<T>(string p) where T:class=>p=="ItemIcons" ? Library as T : null; }
}
namespace CyberExtraction.Core
{
    public static class SpriteFactory { public static Sprite Square=new Sprite(); public static void Setup(SpriteRenderer r,Sprite s,Color c,int o){} }
    public class GameplayDefinition { public static GameplayDefinition Current=new GameplayDefinition(); public float WorldLootVisualScale=1; }
}
namespace CyberExtraction.Items
{
    public enum ItemId:byte { Wood=0,Nails=1,Lockpick=18,DungeonKey=25,TrainingHead=26,TrainingRing=31,ShortSword=32 }
    public static class ItemCatalog { public const int Count=59; public static bool Valid(ItemId i)=>(byte)i<Count; public static bool Melee(ItemId i)=>i==ItemId.ShortSword; public static bool Equipment(ItemId i)=>i>=ItemId.TrainingHead && i<=ItemId.TrainingRing; public static bool PassageTool(ItemId i)=>false; public static bool ExpeditionOnly(ItemId i)=>i==ItemId.DungeonKey; }
    public static class ItemVisualCatalog { public static Sprite InterfaceValue,WorldValue; public static Sprite Interface(ItemId i)=>InterfaceValue; public static Sprite World(ItemId i)=>WorldValue; }
    public class Definition { public Sprite Icon; }
    public static class TreasureCatalog { public static bool IsDefined(ItemId i)=>false; public static Definition Get(ItemId i)=>new Definition(); }
    public static class EquipmentCatalog { public static bool IsDefined(ItemId i)=>false; public static Definition Get(ItemId i)=>new Definition(); }
    public static class TreasureArt { public static Sprite Get(ItemId i)=>null; }
    public static class EquipmentArt { public static Sprite Get(ItemId i)=>null; }
    public static class MeleeWeaponArt { public static Sprite Art=new Sprite(); public static Sprite Get(ItemId i)=>Art; }
    public static class PassageToolArt { public static Sprite Get(ItemId i)=>null; }
    public static class DungeonToolArt { public static Sprite WorldValue; public static Sprite GetWorld(ItemId i)=>WorldValue; }
}
