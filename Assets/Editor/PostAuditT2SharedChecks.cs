using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CyberExtraction.Camp;
using CyberExtraction.Items;
using UnityEditor;
using UnityEngine;

// Three explicit batch invocations; no scene, NGO transport, builds or real saves.
public static class PostAuditT2SharedChecks
{
    [Serializable] sealed class Report
    {
        public string Role,Error;
        public bool Success;
        public List<string> Checks=new List<string>();
    }
    const BindingFlags Hidden=BindingFlags.Static|BindingFlags.NonPublic;
    static string output,cluster,role;
    static int step;
    static double deadline;
    static Report report;
    static SharedCampMemory Store => (SharedCampMemory)typeof(CampRepository).GetField("shared",Hidden).GetValue(null);
    static bool Has(string file)=>File.Exists(Path.Combine(output,file));
    static void Signal(string file,string value="ready")=>File.WriteAllText(Path.Combine(output,file),value);
    static void Check(bool value,string message)
    { if(!value) throw new InvalidOperationException(message); report.Checks.Add(message); }
    static string Record()=>Store.Transaction(data=>JsonUtility.ToJson(data["player"]),false);
    static void Reject(Action action,string message)
    {
        try { action(); }
        catch(CampLeaseLostException) { report.Checks.Add(message); return; }
        catch(TargetInvocationException e) when(e.InnerException is CampLeaseLostException) { report.Checks.Add(message); return; }
        throw new InvalidOperationException("Unauthorized operation accepted: "+message);
    }
    public static void Run()
    {
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Batch Editor only");
        var args=Environment.GetCommandLineArgs();
        output=Arg(args,"-t2-output"); cluster=Arg(args,"-t2-cluster"); role=Arg(args,"-t2-role");
        if(Store!=null || ((IDictionary)typeof(CampRepository).GetField("records",Hidden).GetValue(null)).Count!=0 ||
            ((IDictionary)typeof(CampRepository).GetField("heldLeases",Hidden).GetValue(null)).Count!=0 ||
            ((IList)typeof(CampRepository).GetField("pendingRewards",Hidden).GetValue(null)).Count!=0)
            throw new InvalidOperationException("Repository must be unused");
        report=new Report {Role=role}; deadline=EditorApplication.timeSinceStartup+300;
        try
        {
            Directory.CreateDirectory(output);
            CampRepository.EnableShared(cluster,Path.Combine(output,"store"));
            if(role=="B") Signal("b-ready");
            EditorApplication.update+=Tick;
        }
        catch(Exception error) { Finish(error); }
    }
    static string Arg(string[] args,string name)
    { int i=Array.IndexOf(args,name); if(i<0 || i+1>=args.Length) throw new ArgumentException(name); return args[i+1]; }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException(role+" step "+step);
            if(role=="A") TickA(); else if(role=="B") TickB(); else if(role=="Restart") Restart(); else throw new ArgumentException(role);
        }
        catch(Exception error) { Finish(error); }
    }
    static void TickA()
    {
        if(step==0 && Has("b-ready"))
        {
            Check(CampRepository.Claim("player","A"),"A acquired session");
            CampRepository.Begin("player","old",out _); CampRepository.Commit("player","old");
            Signal("a-ready"); step=1;
        }
        else if(step==1 && Has("b-blocked"))
        {
            CampRepository.QueueExperience(new[] {new ExperienceGrant {Session="player",Expedition="old",Key="old-queued",Amount=20}});
            // Advance only the lease deadline, under the real shared mutex; retain A's cached token.
            Store.Transaction(data=>{data["player"].LeaseExpires=0;return 0;});
            Signal("a-expired"); step=2;
        }
        else if(step==2 && Has("b-owned"))
        {
            Check(CampRepository.FlushPendingExperience(),"old queued XP processed after takeover");
            Check(CampRepository.Get("player").ExpeditionExperience==0,"old queued XP not granted to new expedition");
            string before=Record(); var old=CampRepository.Get("player"); old.Revision++;
            var operations=new Dictionary<string,Action> {
                {"Edit",()=>CampRepository.Edit("player",p=>{p.Revision++;return 0;})},
                {"Save",()=>typeof(CampRepository).GetMethod("Save",Hidden).Invoke(null,new object[]{"player",old,old.Revision-1,"A"})},
                {"Begin",()=>CampRepository.Begin("player","bad",out _)},
                {"Commit",()=>CampRepository.Commit("player","new")},
                {"Rollback",()=>CampRepository.Rollback("player","new")},
                {"Bank",()=>CampRepository.Bank("player","new",new InventoryGrid())},
                {"Lose",()=>CampRepository.Lose("player","new")},
                {"XP",()=>CampRepository.GrantExperience(new[]{new ExperienceGrant{Session="player",Expedition="new",Key="late",Amount=20}})}
            };
            foreach(var operation in operations) Reject(operation.Value,"stale A "+operation.Key+" rejected across processes");
            Check(!CampRepository.Refresh("player","A"),"stale A refresh rejected");
            Check(!CampRepository.EndConnection("player","A",true) && !CampRepository.EndConnection("player","A",true),"repeated stale disconnect rejected");
            Check(Record()==before,"new shared record unchanged by stale A operations");
            Check(CampRepository.Claim("healthy","A"),"healthy participant acquired");
            CampRepository.Begin("healthy","healthy-exp",out _); CampRepository.Commit("healthy","healthy-exp");
            Reject(()=>CampRepository.GrantExperience(new[]{
                new ExperienceGrant{Session="healthy",Expedition="healthy-exp",Key="batch",Amount=20},
                new ExperienceGrant{Session="player",Expedition="new",Key="batch",Amount=20}}),"mixed-owner XP batch rejected");
            Check(CampRepository.Get("healthy").ExpeditionExperience==0,"mixed-owner XP batch has no partial grant");
            CampRepository.EndConnection("healthy","A",true);
            CampRepository.Close(); Signal("a-checked"); Finish(null);
        }
    }
    static void TickB()
    {
        if(step==0 && Has("a-ready"))
        {
            Check(!CampRepository.Claim("player","B"),"B cannot acquire live A session from another process");
            Signal("b-blocked"); step=1;
        }
        else if(step==1 && Has("a-expired"))
        {
            Check(CampRepository.Claim("player","B"),"B acquired expired A session");
            Check(CampRepository.Get("player").ExpeditionState==0,"takeover recovered old expedition");
            CampRepository.Begin("player","new",out _); CampRepository.Commit("player","new");
            Signal("b-owned",Store.Transaction(data=>data["player"].LeaseToken,false)); step=2;
        }
        else if(step==2 && Has("a-checked"))
        {
            var p=CampRepository.Get("player");
            Check(p.ExpeditionState==2 && p.ExpeditionId=="new" && p.ExpeditionExperience==0,"B expedition survives A stale operations");
            Check(CampRepository.Refresh("player","B"),"B retains authority after A disconnect");
            // Leave the expedition committed on disk, then close both processes for recovery.
            CampRepository.Edit("player",profile=>{profile.Revision++;return 0;},"B");
            Finish(null);
        }
    }
    static void Restart()
    {
        var p=CampRepository.Get("player");
        Check(p.ExpeditionState==0 && p.LastExperienceExpedition=="new","fresh process recovers committed expedition from disk");
        Check(Store.Transaction(data=>string.IsNullOrEmpty(data["player"].Lease) && string.IsNullOrEmpty(data["player"].LeaseToken),false),"disk recovery clears transient credentials");
        Check(CampRepository.Claim("player","C"),"fresh process reacquires recovered session");
        Check(Store.Transaction(data=>data["player"].LeaseToken!=File.ReadAllText(Path.Combine(output,"b-owned")),false),"restart acquisition uses new token");
        Check(CampRepository.EndConnection("player","C",true),"restarted owner disconnect succeeds");
        Finish(null);
    }
    static void Finish(Exception error)
    {
        EditorApplication.update-=Tick;
        try { CampRepository.Close(); } catch(Exception cleanup) { error=error??cleanup; }
        report.Success=error==null; report.Error=error?.ToString();
        File.WriteAllText(Path.Combine(output,role+"-result.json"),JsonUtility.ToJson(report,true));
        Debug.Log("POST_AUDIT_T2_SHARED_"+role+"_"+(report.Success?"OK":"FAILED"));
        if(error!=null) Debug.LogException(error);
        EditorApplication.Exit(error==null?0:1);
    }
}
