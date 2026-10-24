// Prova focada: identidade C# real; Android, PlayerPrefs e JsonUtility adaptados.
// Nao valida backup Android real, serializacao Unity, transporte ou interface.
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Collections.Generic;
using CyberExtraction.Camp;

namespace UnityEngine
{
    public enum RuntimePlatform { Android, WindowsPlayer }
    public static class Application { public static RuntimePlatform platform=RuntimePlatform.Android; public static string persistentDataPath; }
    public static class PlayerPrefs
    {
        public static readonly Dictionary<string,string> Data=new Dictionary<string,string>();
        public static string GetString(string name,string fallback)=>Data.TryGetValue(name,out var value)?value:fallback;
        public static void SetString(string name,string value)=>Data[name]=value;
        public static bool HasKey(string name)=>Data.ContainsKey(name);
        public static void DeleteKey(string name)=>Data.Remove(name);
        public static void Save() { }
    }
    public static class JsonUtility
    {
        static readonly JsonSerializerOptions Options=new JsonSerializerOptions {IncludeFields=true,IgnoreReadOnlyProperties=true};
        public static string ToJson(object value)=>JsonSerializer.Serialize(value,value.GetType(),Options);
        public static T FromJson<T>(string value)=>JsonSerializer.Deserialize<T>(value,Options);
    }
    public class AndroidJavaObject:IDisposable
    {
        public static string NoBackupFolder;
        public T Call<T>(string method,params object[] args)=>(T)(method=="getAbsolutePath"?(object)NoBackupFolder:new AndroidJavaObject());
        public void Dispose() { }
    }
    public sealed class AndroidJavaClass:AndroidJavaObject
    {
        public AndroidJavaClass(string name) { }
        public T GetStatic<T>(string name)=>(T)(object)new AndroidJavaObject();
    }
}
namespace CyberExtraction.Camp
{
    // Fronteira DTO equivalente ao pedido de login; nenhuma regra do repositorio adaptada.
    public class CampJoinRequest
    {
        public string Session,PublicKey,Signature,Nonce,DungeonContentHash;
        public long IssuedTicks;
        public bool CampOnly,RepairPier,Fish,CampSandbox,QuickTest,TestAccessProbe;
        public byte Map;
        public int QuickBots;
        public object[] Actions;
    }
}
public static class CampIdentityDeviceProbe
{
    static int passed;
    static void Check(bool condition,string name)
    { if(!condition) throw new Exception(name); passed++; Console.WriteLine("PASS "+name); }
    static void Device(string folder,string legacy=null)
    {
        Directory.CreateDirectory(folder);
        UnityEngine.AndroidJavaObject.NoBackupFolder=folder;
        UnityEngine.Application.persistentDataPath=folder;
        UnityEngine.PlayerPrefs.Data.Clear();
        if(legacy!=null) UnityEngine.PlayerPrefs.SetString("camp_key_v2",legacy);
        typeof(CampIdentity).GetField("key",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
    }
    static void Separate()=>typeof(CampIdentity).GetMethod("CreateSeparateProfile").Invoke(null,null);
    public static int Main(string[] args)
    {
        try
        {
            string root=Path.GetFullPath(args[0]);
            string a=Path.Combine(root,"device-a"),b=Path.Combine(root,"device-b"),c=Path.Combine(root,"device-c");
            CampKey original;
            using(var rsa=new RSACryptoServiceProvider(2048)) { rsa.PersistKeyInCsp=false; original=CampKey.From(rsa.ExportParameters(true)); }
            string legacy=UnityEngine.JsonUtility.ToJson(original);
            Device(a,legacy);
            Check(CampIdentity.Session==original.Session,"migration_preserves_existing_profile");
            Check(!UnityEngine.PlayerPrefs.HasKey("camp_key_v2"),"identity_removed_from_backed_up_preferences");
            Check(File.Exists(Path.Combine(a,"camp-identity-v2.json")),"identity_written_to_no_backup_directory");
            Device(a);
            Check(CampIdentity.Session==original.Session,"existing_profile_survives_restart");
            Device(b,legacy);
            Check(CampIdentity.Session==original.Session,"copied_legacy_credentials_reproduce_shared_identity");
            Separate();
            string separate=CampIdentity.Session;
            Check(separate!=original.Session,"confirmed_separation_creates_distinct_identity");
            string archived=Path.Combine(b,"identity-archive",original.Session+".json");
            Check(UnityEngine.JsonUtility.FromJson<CampKey>(File.ReadAllText(archived)).Session==original.Session,"original_credential_preserved_in_archive");
            var join=new CampJoinRequest {CampOnly=true}; CampIdentity.Sign(join);
            Check(CampIdentity.Verify(join,DateTime.UtcNow.Ticks),"new_identity_signs_valid_login");
            Device(b);
            Check(CampIdentity.Session==separate,"separate_profile_survives_restart");
            Device(c);
            string fresh=CampIdentity.Session;
            Check(fresh!=original.Session && fresh!=separate,"new_device_without_credentials_gets_own_profile");
            Device(a);
            Check(CampIdentity.Session==original.Session,"other_device_profile_unchanged");
            Directory.CreateDirectory(Path.Combine(a,"camp-identity-v2.json.tmp"));
            bool failed=false;
            try { Separate(); } catch(TargetInvocationException) { failed=true; }
            Check(failed && CampIdentity.Session==original.Session,"failed_key_write_does_not_replace_current_identity");
            Check(UnityEngine.JsonUtility.FromJson<CampKey>(File.ReadAllText(Path.Combine(a,"camp-identity-v2.json"))).Session==original.Session,"failed_key_write_keeps_saved_identity");
            Console.WriteLine("RESULT "+passed+" focused checks passed");
            return 0;
        }
        catch(Exception error) { Console.WriteLine("FAIL "+error.GetType().Name+": "+error.Message); return 1; }
    }
}
