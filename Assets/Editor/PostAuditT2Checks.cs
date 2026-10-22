using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CyberExtraction.Camp;
using CyberExtraction.Items;
using UnityEditor;
using UnityEngine;

// Explicit batch command only. Uses empty, in-memory repository; never opens saves.
public static class PostAuditT2Checks
{
    [Serializable] sealed class Report
    {
        public bool Success;
        public List<string> Checks=new List<string>();
        public string Error;
    }
    const BindingFlags Hidden=BindingFlags.Static|BindingFlags.NonPublic;
    static Dictionary<string,CampRecord> Records => (Dictionary<string,CampRecord>)typeof(CampRepository).GetField("records",Hidden).GetValue(null);
    static IDictionary Claims => (IDictionary)typeof(CampRepository).GetField("heldLeases",Hidden).GetValue(null);
    static void Require(bool value,string message) { if(!value) throw new InvalidOperationException(message); }
    static void Rejected(Action action)
    {
        try { action(); }
        catch(CampLeaseLostException) { return; }
        catch(TargetInvocationException error) when(error.InnerException is CampLeaseLostException) { return; }
        throw new InvalidOperationException("Expected CampLeaseLostException");
    }
    static void Save(string session,CampProgress profile,long revision,string owner) =>
        typeof(CampRepository).GetMethod("Save",Hidden).Invoke(null,new object[] {session,profile,revision,owner});
    public static void Run()
    {
        Require(Application.isBatchMode && !EditorApplication.isPlayingOrWillChangePlaymode,"Batch Editor only");
        Require(typeof(CampRepository).GetField("shared",Hidden).GetValue(null)==null && Records.Count==0 && Claims.Count==0 &&
            ((IList)typeof(CampRepository).GetField("pendingRewards",Hidden).GetValue(null)).Count==0,"Repository must be unused");
        var args=Environment.GetCommandLineArgs(); int index=Array.IndexOf(args,"-t2-output");
        Require(index>=0 && index+1<args.Length,"-t2-output required");
        string output=args[index+1]; Directory.CreateDirectory(output);
        var report=new Report(); var random=UnityEngine.Random.state;
        try
        {
            string s="T2_"+Guid.NewGuid().ToString("N");
            Rejected(()=>CampRepository.Edit(s,p=>0)); report.Checks.Add("write before Claim rejected");
            Require(CampRepository.Claim(s,"A"),"A claim");
            string token=Records[s].LeaseToken;
            Require(CampRepository.Claim(s,"A") && Records[s].LeaseToken==token,"active claim must preserve token");
            Require(!CampRepository.Claim(s,"B"),"B cannot steal live lease");
            CampRepository.Edit(s,p=>{ p.Revision++; return 0; },"A");
            report.Checks.Add("active owner writes; renewal preserves token; other owner blocked");
            CampRepository.Begin(s,"old",out _); CampRepository.Commit(s,"old");
            Records[s].LeaseExpires=0; // Fixture: elapsed deadline, without sleeping 90 seconds.
            Rejected(()=>CampRepository.Edit(s,p=>0));
            Require(!CampRepository.Refresh(s,"A"),"expired refresh");
            report.Checks.Add("expired lease cannot write or renew");
            Require(CampRepository.Claim(s,"B") && Records[s].LeaseToken!=token,"takeover token");
            CampRepository.Begin(s,"new",out _); CampRepository.Commit(s,"new");
            string before=JsonUtility.ToJson(Records[s]);
            Require(!CampRepository.EndConnection(s,"A",true) && !CampRepository.EndConnection(s,"A",true),"late disconnect accepted");
            Require(JsonUtility.ToJson(Records[s])==before && CampRepository.Refresh(s,"B"),"late callback changed new expedition/credential");
            Rejected(()=>CampRepository.Edit(s,p=>0,"A"));
            report.Checks.Add("same-process old owner and repeated late EndConnection cannot affect B expedition or credential");

            // Fixture: remote room changed persistent credential, while this process holds B's old token.
            // This exercises authorization branches, not actual cross-process delivery/mutex behavior.
            Records[s].Lease="remote"; Records[s].LeaseToken=Guid.NewGuid().ToString("N");
            var snapshot=CampRepository.Get(s); before=JsonUtility.ToJson(Records[s]);
            var operations=new Dictionary<string,Action> {
                {"Edit",()=>CampRepository.Edit(s,p=>{p.Revision++;return 0;})},
                {"Save",()=>Save(s,snapshot,snapshot.Revision,"B")},
                {"Begin",()=>CampRepository.Begin(s,"wrong",out _)},
                {"Commit",()=>CampRepository.Commit(s,"new")},
                {"Rollback",()=>CampRepository.Rollback(s,"new")},
                {"Bank",()=>CampRepository.Bank(s,"new",new InventoryGrid())},
                {"Lose",()=>CampRepository.Lose(s,"new")},
                {"XP",()=>CampRepository.GrantExperience(new[] {new ExperienceGrant {Session=s,Expedition="new",Key="late",Amount=10}})}
            };
            foreach(var operation in operations)
            {
                Rejected(operation.Value);
                Require(JsonUtility.ToJson(Records[s])==before,"stale "+operation.Key+" changed profile");
                report.Checks.Add("stale token: "+operation.Key+" rejected without mutation");
            }
            Require(!CampRepository.Refresh(s,"B") && !CampRepository.EndConnection(s,"B",true),"stale credential accepted");
            Require(JsonUtility.ToJson(Records[s])==before,"stale disconnect mutated remote record");
            report.Checks.Add("stale refresh and disconnect rejected without mutation");

            Records[s].LeaseExpires=0;
            Require(CampRepository.Claim(s,"remote"),"reacquire");
            string previous=Records[s].LeaseToken; Records[s].LeaseExpires=0;
            Require(CampRepository.Claim(s,"remote") && Records[s].LeaseToken!=previous,"same-name reacquisition token reused");
            var old=CampRepository.Get(s);
            CampRepository.Edit(s,p=>{p.Revision++;return 0;},"remote");
            old.Revision++;
            bool staleRejected=false;
            try { Save(s,old,old.Revision-1,"remote"); }
            catch(TargetInvocationException error) when(error.InnerException is InvalidOperationException) { staleRejected=true; }
            Require(staleRejected,"old snapshot accepted");
            report.Checks.Add("same-name reacquisition generates new token; obsolete snapshot rejected");
            Require(CampRepository.EndConnection(s,"remote",true) && !CampRepository.EndConnection(s,"remote",true),"current disconnect not idempotent");
            // JsonUtility may round-trip a null string as empty during the repeated transaction.
            Require(string.IsNullOrEmpty(Records[s].Lease) && string.IsNullOrEmpty(Records[s].LeaseToken) &&
                Records[s].LeaseExpires==0 && !Claims.Contains(s),"disconnect did not release credential");
            report.Checks.Add("current owner disconnect releases lease; repeated callback is harmless");
            report.Success=true;
        }
        catch(Exception error) { report.Error=error.ToString(); throw; }
        finally
        {
            CampRepository.Close(); UnityEngine.Random.state=random;
            File.WriteAllText(Path.Combine(output,"t2a-result.json"),JsonUtility.ToJson(report,true));
            Debug.Log("POST_AUDIT_T2A_"+(report.Success ? "OK" : "FAILED"));
        }
    }
}
