using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Reflection;
using System.IO.MemoryMappedFiles;
using CyberExtraction.Camp;
using CyberExtraction.Items;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Explicit batch Editor command, copied only to the isolated worker.
public static class PostAuditT4Checks
{
    [Serializable] sealed class Report { public bool Success; public string Error; public List<string> Checks=new List<string>(); public List<string> ExpectedFailures=new List<string>(); }
    static Report report;
    static string output,current;
    static bool partB;
    static void Check(bool value,string message) { if(!value) throw new InvalidOperationException(current+": "+message); report.Checks.Add(current+": "+message); }
    static string Mutate(string json,Action<JObject> action) { var doc=JObject.Parse(json); action(doc); return doc.ToString(); }
    static string Seed(int wood)
    {
        string name="seed-"+Guid.NewGuid().ToString("N"),dir=Path.Combine(output,"seeds");
        using(var store=new SharedCampMemory(name,dir)) store.Transaction(data=>
        {
            var p=CampProfileMigration.Create(); p.Pack.Add(new ItemStack(ItemId.Wood,wood)); p.Revision=23;
            data["player"]=new CampRecord { Session="player",Progress=p }; return 0;
        });
        return File.ReadAllText(Path.Combine(dir,name+".json"));
    }
    static string Bytes(string path)=>Convert.ToBase64String(File.ReadAllBytes(path));
    static string Encoded(string text)=>Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
    static string Archive(string path,string label,string content)
    {
        using(var sha=SHA256.Create())
        {
            var hash=sha.ComputeHash(Encoding.UTF8.GetBytes(content));
            return path+"."+label+"-"+BitConverter.ToString(hash,0,16).Replace("-","").ToLowerInvariant()+".json";
        }
    }
    static int Read(string name,string dir,Action visited)
    {
        using(var store=new SharedCampMemory(name,dir)) return store.Transaction(data=>
        { visited(); return data.TryGetValue("player",out var record)?record.Progress.Pack.Count(ItemId.Wood):0; });
    }
    static void Fixture(string label,string primary,string backup,out string dir,out string name,out string path)
    {
        current=label; dir=Path.Combine(output,label); Directory.CreateDirectory(dir); name="T4b-"+Guid.NewGuid().ToString("N"); path=Path.Combine(dir,name+".json");
        if(primary!=null) File.WriteAllBytes(path,Encoding.UTF8.GetBytes(primary));
        if(backup!=null) File.WriteAllBytes(path+".bak",Encoding.UTF8.GetBytes(backup));
    }
    static Exception Failure(Action action)
    {
        Exception error=null; try { action(); } catch(Exception caught) { error=caught; }
        Check(error!=null,"injected I/O obstacle rejects operation");
        Check(IsIo(error),"failure contains a real filesystem exception");
        report.ExpectedFailures.Add(current+": "+error.GetType().Name+" | "+error.Message); return error;
    }
    static bool IsIo(Exception error)=>error is IOException || error is UnauthorizedAccessException ||
        error is AggregateException group && group.InnerExceptions.Any(IsIo) || error?.InnerException!=null && IsIo(error.InnerException);
    static void Intact(string path,string primary,string backup)
    {
        Check(primary==null?!File.Exists(path):Bytes(path)==Encoded(primary),"previous primary bytes intact");
        Check(Bytes(path+".bak")==Encoded(backup),"previous backup bytes intact");
        Check(Directory.GetFiles(Path.GetDirectoryName(path),Path.GetFileName(path)+"*.tmp").Length==0,"failed operation leaves no pending temp files");
    }
    static string Headers(SharedCampMemory store)
    {
        var view=(MemoryMappedViewAccessor)typeof(SharedCampMemory).GetField("view",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(store);
        return view.ReadInt32(0)+":"+view.ReadInt32(16)+":"+view.ReadInt32(16+2*1024*1024);
    }
    static void MoveBlocker(string path,bool directory)
    {
        string source=Path.GetFullPath(path),destination=Path.GetFullPath(path+".fixture-blocker"),root=Path.GetFullPath(output)+Path.DirectorySeparatorChar;
        if(!source.StartsWith(root,StringComparison.OrdinalIgnoreCase) || !destination.StartsWith(root,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Fixture must stay inside output");
        if(directory) Directory.Move(source,destination); else File.Move(source,destination);
    }
    static void FaultCases(string primary,string backup)
    {
        string dir,name,path; bool called;
        Fixture("primary-read-locked",primary,backup,out dir,out name,out path); called=false;
        using(var held=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None))
            Failure(()=>Read(name,dir,()=>called=true));
        Check(!called,"locked primary refuses before economic callback"); Intact(path,primary,backup);
        Check(Read(name,dir,()=>{})==3,"load succeeds after primary lock is released");

        Fixture("backup-read-locked",null,backup,out dir,out name,out path); called=false;
        using(var held=new FileStream(path+".bak",FileMode.Open,FileAccess.Read,FileShare.None))
            Failure(()=>Read(name,dir,()=>called=true));
        Check(!called,"missing primary plus locked backup refuses before callback"); Intact(path,null,backup);
        Check(Read(name,dir,()=>{})==7,"backup recovery succeeds after lock is released");

        foreach(bool wrongContent in new[]{false,true})
        {
            Fixture(wrongContent?"archive-content-collision":"archive-promotion-blocked","{broken",backup,out dir,out name,out path); called=false;
            string archive=Archive(path,"rejected","{broken");
            if(wrongContent) File.WriteAllText(archive,"fixture collision"); else Directory.CreateDirectory(archive);
            Failure(()=>Read(name,dir,()=>called=true));
            Check(!called,"failed preservation stops before economic callback"); Intact(path,"{broken",backup);
            Check(wrongContent?File.ReadAllText(archive)=="fixture collision":Directory.Exists(archive),"preservation obstacle not overwritten");
            MoveBlocker(archive,!wrongContent);
            Check(Read(name,dir,()=>{})==7,"recovery succeeds after preservation obstacle is moved aside");
            Check(Bytes(archive)==Encoded("{broken"),"rejected bytes preserved after retry");
        }

        Fixture("checkpoint-promotion-locked",primary,backup,out dir,out name,out path);
        using(var store=new SharedCampMemory(name,dir))
        {
            store.Transaction(data=>data["player"].Progress.Pack.Count(ItemId.Wood));
            string beforePrimary=File.ReadAllText(path),beforeBackup=File.ReadAllText(path+".bak"),headers=Headers(store); called=false;
            using(var held=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
                Failure(()=>store.Transaction(data=>{called=true;data["player"].Progress.Pack.Add(new ItemStack(ItemId.Wood,4));data["player"].Progress.Revision++;return 0;}));
            Check(called,"mutation callback ran before durable promotion failed"); Intact(path,beforePrimary,beforeBackup);
            Check(Headers(store)==headers && store.Transaction(data=>data["player"].Progress.Pack.Count(ItemId.Wood),false)==3,"failed durable commit did not publish mutation to shared state");
            store.Transaction(data=>{data["player"].Progress.Pack.Add(new ItemStack(ItemId.Wood,4));data["player"].Progress.Revision++;return 0;});
            Check(store.Transaction(data=>data["player"].Progress.Pack.Count(ItemId.Wood),false)==7,"single retry commits exactly one mutation");
        }
        Check(Read(name,dir,()=>{})==7,"committed retry survives mapping disposal");

        Fixture("recovery-promotion-locked","{broken",backup,out dir,out name,out path);
        using(var store=new SharedCampMemory(name,dir))
        {
            string headers=Headers(store); called=false;
            using(var held=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
                Failure(()=>store.Transaction(data=>{called=true;return data["player"].Progress.Pack.Count(ItemId.Wood);}));
            Check(called,"backup decoded before replacement of locked primary failed");
            Intact(path,"{broken",backup); Check(Headers(store)==headers,"failed recovery did not publish partial or empty mapping");
            Check(Bytes(Archive(path,"rejected","{broken"))==Encoded("{broken") && Bytes(Archive(path,"recovered-backup",backup))==Encoded(backup),"both source documents archived before attempted replacement");
            Check(store.Transaction(data=>data["player"].Progress.Pack.Count(ItemId.Wood))==7,"retry restores complete backup after replacement lock is released");
            Check(Bytes(path+".bak")==Encoded(backup),"successful recovery retry retains valid backup");
        }
        Check(Read(name,dir,()=>{})==7,"restored checkpoint survives mapping disposal");
    }
    static void Case(string label,string primary,string backup,int expected,bool recover=false,bool reject=false,bool future=false)
    {
        current=label; string dir=Path.Combine(output,label),name="T4a-"+Guid.NewGuid().ToString("N"); Directory.CreateDirectory(dir);
        string path=Path.Combine(dir,name+".json");
        if(primary!=null) File.WriteAllBytes(path,Encoding.UTF8.GetBytes(primary));
        if(backup!=null) File.WriteAllBytes(path+".bak",Encoding.UTF8.GetBytes(backup));
        if(reject)
        {
            int archiveCount=-1;
            for(int attempt=0;attempt<2;attempt++)
            {
                bool visited=false; Exception error=null;
                try { Read(name,dir,()=>visited=true); } catch(Exception caught) { error=caught; }
                Check(error!=null && !visited,"rejected before transaction callback, attempt "+attempt);
                if(future) Check(error.GetType().Name=="UnsupportedCampVersionException","future version explicitly refused");
                Check(primary==null?!File.Exists(path):Bytes(path)==Encoded(primary),"primary bytes unchanged after rejection");
                Check(backup==null?!File.Exists(path+".bak"):Bytes(path+".bak")==Encoded(backup),"backup bytes unchanged after rejection");
                int count=Directory.GetFiles(dir,name+".*.json").Length;
                if(attempt==0) archiveCount=count; else Check(count==archiveCount,"identical failed retry does not multiply archives");
            }
            return;
        }
        bool called=false; int marker=Read(name,dir,()=>called=true);
        Check(called && marker==expected,"correct candidate loaded by native repository");
        Check(File.Exists(path),"complete primary checkpoint exists after load");
        if(recover)
        {
            Check(Bytes(path+".bak")==Encoded(backup),"valid backup bytes preserved during recovery");
            string archivedBackup=Archive(path,"recovered-backup",backup);
            Check(File.Exists(archivedBackup) && Bytes(archivedBackup)==Encoded(backup),"recovered backup archived byte for byte");
            if(primary!=null)
            {
                string archivedPrimary=Archive(path,"rejected",primary);
                Check(File.Exists(archivedPrimary) && Bytes(archivedPrimary)==Encoded(primary),"rejected primary archived byte for byte");
            }
        }
        int archives=Directory.GetFiles(dir,name+".*.json").Length;
        Check(Read(name,dir,()=>{})==expected,"reopening disposed mapping loads checkpoint from disk");
        Check(Directory.GetFiles(dir,name+".*.json").Length==archives,"reopening does not multiply preserved archives");
    }
    public static void Run()
    {
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Batch Editor without Play only");
        var args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,"-t4-output"); if(i<0 || i+1>=args.Length) throw new ArgumentException("-t4-output");
        output=args[i+1]; Directory.CreateDirectory(output); report=new Report(); var random=UnityEngine.Random.state;
        partB=Array.IndexOf(args,"-t4b")>=0;
        try
        {
            string primary=Seed(3),backup=Seed(7);
            if(partB) { FaultCases(primary,backup); report.Success=true; return; }
            Case("valid-primary",primary,backup,3);
            Case("invalid-json","{broken",backup,7,true);
            Case("null-records",Mutate(primary,d=>d["Records"]=JValue.CreateNull()),backup,7,true);
            Case("missing-records",Mutate(primary,d=>d.Remove("Records")),backup,7,true);
            Case("incomplete-record",Mutate(primary,d=>((JObject)d["Records"][0]).Remove("Progress")),backup,7,true);
            Case("duplicate-session",Mutate(primary,d=>((JArray)d["Records"]).Add(d["Records"][0].DeepClone())),backup,7,true);
            Case("invalid-quantity",Mutate(primary,d=>((JArray)d["Records"][0]["Progress"]["Pack"]["Slots"]).First(t=>t["Item"].Value<int>()==(int)ItemId.Wood && t["Quantity"].Value<int>()>0)["Quantity"]=-1),backup,7,true);
            Case("missing-primary",null,backup,7,true);
            Case("new-store",null,null,0);
            Case("both-invalid","{broken","{also-broken",0,reject:true);
            string futureDocument=Mutate(primary,d=>d["Version"]=d["Version"].Value<int>()+1);
            string futureProfile=Mutate(primary,d=>d["Records"][0]["Progress"]["SchemaVersion"]=CampProfileMigration.CurrentVersion+1);
            Case("future-document",futureDocument,backup,0,reject:true,future:true);
            Case("future-profile",futureProfile,backup,0,reject:true,future:true);
            Case("future-backup","{broken",futureDocument,0,reject:true,future:true);
            Case("explicit-empty-primary",Mutate(primary,d=>d["Records"]=new JArray()),backup,0);
            int version=JObject.Parse(primary)["Version"].Value<int>();
            string duplicateProperty="{\"Version\":"+version+",\"Version\":"+version+",\"Records\":"+JObject.Parse(primary)["Records"].ToString()+"}";
            Case("duplicate-property",duplicateProperty,backup,7,true);
            report.Success=true;
        }
        catch(Exception error) { report.Error=error.ToString(); throw; }
        finally
        {
            UnityEngine.Random.state=random;
            File.WriteAllText(Path.Combine(output,partB?"t4b-result.json":"t4a-result.json"),JsonUtility.ToJson(report,true));
            Debug.Log((partB?"POST_AUDIT_T4B_":"POST_AUDIT_T4A_")+(report.Success?"OK":"FAILED"));
        }
    }
}
