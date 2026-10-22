// Audit-only overlay. Copied into the isolated project by Prepare-Stage6.py.
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class Stage6Editor
{
    public static void Run()
    {
        string output=Arg("-audit-output"); Directory.CreateDirectory(output);
        var sample=JsonUtility.FromJson<CyberExtraction.Camp.CampProgress>(JsonUtility.ToJson(CyberExtraction.Camp.CampProfileMigration.Create()));
        File.WriteAllText(Path.Combine(output,"native-json.txt"),JsonUtility.ToJson(sample));
        NativeChecks(output);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{"Assets/Scenes/Game.unity"}, locationPathName=Path.Combine(output,"Player/CyberExtractionAudit.exe"),
            target=BuildTarget.StandaloneWindows64,subtarget=(int)StandaloneBuildSubtarget.Player,options=BuildOptions.Development });
        File.WriteAllText(Path.Combine(output,"build-summary.json"),JsonUtility.ToJson(new Summary {
            unity=Application.unityVersion,result=report.summary.result.ToString(),errors=report.summary.totalErrors,
            warnings=report.summary.totalWarnings,bytes=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds },true));
        if(report.summary.result!=BuildResult.Succeeded) throw new Exception("Stage6 build failed");
        Debug.Log("AUDIT_STAGE6_BUILD_OK");
    }
    static string Arg(string key) { var a=Environment.GetCommandLineArgs(); int i=Array.IndexOf(a,key); if(i<0 || i+1>=a.Length) throw new Exception(key+" required"); return a[i+1]; }
    public static void NativeOnly() { NativeChecks(Arg("-audit-output"),true); }
    static void NativeChecks(string output,bool backupOnly=false)
    {
        var lines=new System.Collections.Generic.List<string>();
        string folder=Path.Combine(output,"native-probes"); Directory.CreateDirectory(folder);
        string cluster="audit-backup-"+Guid.NewGuid().ToString("N");
        string file=Path.Combine(folder,cluster+".json");
        File.WriteAllText(file,"{\"Version\":6,\"Records\":null}");
        var marker=new CyberExtraction.Camp.CampRecord { Session="backup-marker",Progress=CyberExtraction.Camp.CampProfileMigration.Create() };
        string backup="{\"Version\":6,\"Records\":["+JsonUtility.ToJson(marker)+"]}";
        File.WriteAllText(file+".bak",backup);
        try { using(var store=new CyberExtraction.Camp.SharedCampMemory(cluster,folder)) { int count=store.Transaction(data=>data.ContainsKey("backup-marker") ? 1 : 0,false); lines.Add("E2-03 null-records primary; backup marker recovered="+(count==1)); } }
        catch(Exception e) { lines.Add("E2-03 backup NOT recovered: "+e.GetType().Name+": "+e.Message); }
        cluster="audit-backup-"+Guid.NewGuid().ToString("N"); file=Path.Combine(folder,cluster+".json");
        File.WriteAllText(file,"{\"Version\":999,\"Records\":[]}"); File.WriteAllText(file+".bak",backup);
        try { using(var store=new CyberExtraction.Camp.SharedCampMemory(cluster,folder)) { int count=store.Transaction(data=>data.ContainsKey("backup-marker") ? 1 : 0,false); lines.Add("E2-03 invalid-version primary; backup marker recovered="+(count==1)); } }
        catch(Exception e) { lines.Add("E2-03 invalid-version primary; backup NOT recovered: "+e.GetType().Name+": "+e.Message); }
        if(backupOnly) { File.WriteAllLines(Path.Combine(output,"native-backup-retest.txt"),lines); return; }
        for(int kind=0;kind<4;kind++)
        {
            var p=CyberExtraction.Camp.CampProfileMigration.Create();
            if(kind==0) p.Pack.Slots=new CyberExtraction.Items.ItemStack[10];
            if(kind==1) p.Pack.Slots[0]=new CyberExtraction.Items.ItemStack { Item=(CyberExtraction.Items.ItemId)254,Quantity=1 };
            if(kind==2) p.Pack.Slots[0]=new CyberExtraction.Items.ItemStack { Item=CyberExtraction.Items.ItemId.Metal,Quantity=100000 };
            if(kind==3) { var stack=new CyberExtraction.Items.ItemStack(CyberExtraction.Items.ItemId.Pistol,1,0,100000); p.Pack.Slots[0]=stack; }
            try { CyberExtraction.Camp.CampProfileMigration.ValidateOwnership(new[]{new CyberExtraction.Camp.CampRecord { Session="native-probe",Progress=p }}); lines.Add("E2-05 malformed case "+kind+" ACCEPTED"); }
            catch(Exception e) { lines.Add("E2-05 malformed case "+kind+" REJECTED: "+e.Message); }
        }
        var big=CyberExtraction.Camp.CampProfileMigration.Create();
        for(int i=0;i<128;i++) big.Ground.Add(new CyberExtraction.Camp.CampGroundItem { Id=Guid.NewGuid().ToString("N"),Stack=new CyberExtraction.Items.ItemStack(CyberExtraction.Items.ItemId.Metal,1) });
        string json=JsonUtility.ToJson(new CyberExtraction.Camp.CampSnapshot { Profile=JsonUtility.ToJson(big) });
        using(var writer=new Unity.Netcode.FastBufferWriter(json.Length*2+8,Unity.Collections.Allocator.Temp)) { writer.WriteValueSafe(json); lines.Add("E2-04 synthetic 128-ground-item snapshot native NGO bytes="+writer.Length+" receiver limit=32768"); }
        File.WriteAllLines(Path.Combine(output,"native-probes.txt"),lines);
    }
    [Serializable] class Summary { public string unity,result; public int errors,warnings; public ulong bytes; public double seconds; }
}
