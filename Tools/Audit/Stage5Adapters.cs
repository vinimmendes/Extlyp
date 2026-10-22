// Engine boundary adapters ONLY. No claim of Unity serialization, RNG, assets or networking fidelity.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CyberExtraction.Items;
using CyberExtraction.Core;

namespace UnityEngine
{
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public enum HideFlags { HideAndDontSave }
    public sealed class RuntimeInitializeOnLoadMethodAttribute:Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType value){} }
    public sealed class CreateAssetMenuAttribute:Attribute { public string fileName,menuName; }
    public sealed class MinAttribute:Attribute { public MinAttribute(float value){} }
    public sealed class RangeAttribute:Attribute { public RangeAttribute(float min,float max){} }
    public sealed class HeaderAttribute:Attribute { public HeaderAttribute(string value){} }
    public class ScriptableObject { public string name; public HideFlags hideFlags; public static T CreateInstance<T>() where T:new()=>new T(); public static void Destroy(object value){} }
    public sealed class Sprite {}
    public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a=1) {this.r=r;this.g=g;this.b=b;this.a=a;} }
    public static class Mathf
    {
        public static int Clamp(int x,int min,int max)=>Math.Clamp(x,min,max);
        public static float Clamp(float x,float min,float max)=>Math.Clamp(x,min,max);
        public static float Clamp01(float x)=>Clamp(x,0,1);
        public static float Max(float a,float b)=>Math.Max(a,b); public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Min(float a,float b)=>Math.Min(a,b); public static int Min(int a,int b)=>Math.Min(a,b);
        public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);
        public static float InverseLerp(float a,float b,float v)=>a==b?0:Clamp01((v-a)/(b-a));
        public static float SmoothStep(float a,float b,float t) {t=Clamp01(t);return a+(b-a)*t*t*(3-2*t);}
        public static int RoundToInt(float x)=>(int)Math.Round(x,MidpointRounding.ToEven);
        public static float Sin(float x)=>(float)Math.Sin(x); public static float Abs(float x)=>Math.Abs(x);
        public static float MoveTowards(float a,float b,float d)=>Math.Abs(b-a)<=d?b:a+Math.Sign(b-a)*d;
        public static float PingPong(float x,float length) {float v=x-(float)Math.Floor(x/(length*2))*length*2;return length-Math.Abs(v-length);}
    }
    public static class Random
    {
        static System.Random generator=new System.Random(5016);
        public static int Range(int min,int max)=>generator.Next(min,max);
        public static float Range(float min,float max)=>(float)(min+(max-min)*generator.NextDouble());
    }
    public static class Application { public static bool isEditor=false; public static string persistentDataPath; }
    public static class Debug { public static bool isDebugBuild=false; }
    public static class PlayerPrefs
    {
        static readonly Dictionary<string,string> data=new Dictionary<string,string>();
        public static string GetString(string key,string fallback)=>data.TryGetValue(key,out var v)?v:fallback;
        public static void SetString(string key,string value)=>data[key]=value; public static void Save(){}
    }
    public static class JsonUtility
    {
        public static readonly JsonSerializerOptions Options=new JsonSerializerOptions { IncludeFields=true,IgnoreReadOnlyProperties=true };
        public static string ToJson(object value)=>JsonSerializer.Serialize(value,value.GetType(),Options);
        public static T FromJson<T>(string value)=>JsonSerializer.Deserialize<T>(value,Options);
    }
    public static class Resources
    {
        static readonly Dictionary<string,object> values=new Dictionary<string,object>();
        public static void Initialize(string fixture)
        {
            foreach(var row in JsonDocument.Parse(File.ReadAllText(fixture)).RootElement.EnumerateArray())
            {
                string name=row.GetProperty("type").GetString();
                Type type=name=="ItemDefinition"?typeof(ItemDefinition):name=="LootTableDefinition"?typeof(LootTableDefinition):typeof(GameplayDefinition);
                values.Add(row.GetProperty("resource").GetString(),JsonSerializer.Deserialize(row.GetProperty("values").GetRawText(),type,JsonUtility.Options));
            }
        }
        public static T Load<T>(string path) where T:class=>values.TryGetValue(path,out var value)?value as T:null;
        public static T[] LoadAll<T>(string path) where T:class=>values.Where(v=>v.Key.StartsWith(path+"/",StringComparison.Ordinal)).Select(v=>v.Value as T).Where(v=>v!=null).ToArray();
    }
}
namespace Unity.Netcode
{
    public interface IReaderWriter {}
    public interface INetworkSerializable {void NetworkSerialize<T>(BufferSerializer<T> serializer) where T:IReaderWriter;}
    public struct BufferSerializer<T> where T:IReaderWriter {public void SerializeValue<TValue>(ref TValue value){} }
    public sealed class NetworkManager {public static NetworkManager Singleton=new NetworkManager(); public bool IsServer=true;}
}
namespace CyberExtraction.Match
{
    public enum ExpeditionMap:byte {Test,Large,Dungeon}
    public static class MapBuilder {public static ExpeditionMap Current=ExpeditionMap.Dungeon;}
}
namespace CyberExtraction.UI
{
    public sealed class NetworkBootstrap {public static NetworkBootstrap Instance;public bool SendCamp(CyberExtraction.Camp.CampCommand command)=>throw new NotSupportedException("Transport is not simulated.");}
}
