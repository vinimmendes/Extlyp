using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using CyberExtraction.Camp;
using CyberExtraction.Items;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Explicit Editor probe; installed only in the isolated audit worker.
public static class PostAuditT5Checks
{
    [Serializable] sealed class Report { public bool Success; public string Error; public int Cases; public List<string> Checks=new List<string>(); }
    static Report report;
    static string output,current;
    static void Check(bool ok,string message) { if(!ok) throw new Exception(current+": "+message); report.Checks.Add(current+": "+message); }
    static CampProgress Empty()
    {
        var p=CampProfileMigration.Create(); p.Chest=new InventoryGrid(); p.Pack=new InventoryGrid(); p.Equipment=new EquipmentLoadout(); return p;
    }
    static ItemStack Gun()=>new ItemStack(ItemId.Pistol,1,0,5);
    static void Reject(Action action)
    {
        Exception error=null; try { action(); } catch(Exception e) { error=e is TargetInvocationException ? e.InnerException : e; }
        Check(error is InvalidOperationException,"invalid candidate rejected by domain validation");
    }
    static void TransactionCase(string label,Action<Dictionary<string,CampRecord>> mutate,bool accepted=false)
    {
        current=label; report.Cases++;
        string dir=Path.Combine(output,label),name="t5-"+Guid.NewGuid().ToString("N");
        using(var store=new SharedCampMemory(name,dir))
        {
            store.Transaction(data=>{ data["a"]=new CampRecord { Session="a",Progress=Empty() }; data["b"]=new CampRecord { Session="b",Progress=Empty() }; return 0; });
            // A second durable checkpoint makes backup preservation observable.
            store.Transaction(data=>{ data["a"].Progress.Revision=7; return 0; });
            string path=Path.Combine(dir,name+".json"),before=File.ReadAllText(path),backup=File.ReadAllText(path+".bak");
            if(accepted)
            {
                store.Transaction(data=>{mutate(data); return 0;});
                Check(File.ReadAllText(path)!=before,"valid ownership state committed");
                store.Transaction(data=>{ CampProfileMigration.ValidateOwnership(data.Values); return 0; },false);
                Check(true,"committed state remains valid on reread"); return;
            }
            Reject(()=>store.Transaction(data=>{mutate(data); return 0;}));
            Check(File.ReadAllText(path)==before,"primary checkpoint unchanged");
            Check(File.ReadAllText(path+".bak")==backup,"backup checkpoint unchanged");
            string reread=store.Transaction(data=>JsonUtility.ToJson(data["a"].Progress),false);
            Check(reread==JsonUtility.ToJson(JObject.Parse(before)["Records"][0].ToObject<CampRecord>().Progress),"shared profile unchanged after rejection");
        }
    }
    static void Put(CampProgress p,int container,ItemStack stack)
    {
        if(container==0) p.Chest.Slots[0]=stack;
        else if(container==1) p.Pack.Slots[0]=stack;
        else if(container==2) p.Equipment.Slots[EquipmentLoadout.WeaponA]=stack;
        else p.Ground.Add(new CampGroundItem {Id=Guid.NewGuid().ToString("N"),Stack=stack});
    }
    static void Shapes()
    {
        var cases=new Dictionary<string,Action<CampProgress>> {
            {"null-chest",p=>p.Chest=null},{"null-pack",p=>p.Pack=null},
            {"null-grid-slots",p=>p.Pack.Slots=null},{"short-grid",p=>p.Chest.Slots=new ItemStack[8]},
            {"long-grid",p=>p.Pack.Slots=new ItemStack[10]},{"null-equipment",p=>p.Equipment=null},
            {"short-equipment",p=>p.Equipment.Slots=new ItemStack[7]},{"long-equipment",p=>p.Equipment.Slots=new ItemStack[9]},
            {"null-equipment-slots",p=>p.Equipment.Slots=null},{"invalid-active-slot",p=>p.Equipment.ActiveWeaponSlot=0},
            {"null-ground",p=>p.Ground=null},{"null-drop",p=>p.Ground.Add(null)},
            {"empty-drop-id",p=>p.Ground.Add(new CampGroundItem {Id="",Stack=Gun()})},
            {"duplicate-drop-id",p=>{p.Ground.Add(new CampGroundItem {Id="same",Stack=Gun()});p.Ground.Add(new CampGroundItem {Id="same",Stack=Gun()});}},
            {"unknown-item",p=>p.Pack.Slots[0]=new ItemStack {Item=(ItemId)255,Quantity=1}},
            {"wrong-equipment-slot",p=>p.Equipment.Slots[0]=Gun()},
            {"weapon-without-identity",p=>{var s=Gun();s.Instance=default;p.Pack.Slots[0]=s;}},
            {"reserved-without-escrow",p=>{p.ExpeditionState=1;p.ExpeditionId="run";p.Escrow=null;}},
            {"unsupported-schema",p=>p.SchemaVersion=CampProfileMigration.CurrentVersion+1}
        };
        foreach(var item in cases) TransactionCase(item.Key,d=>item.Value(d["a"].Progress));
    }
    static void Identities()
    {
        for(int a=0;a<4;a++) for(int b=a+1;b<4;b++)
        { int first=a,second=b; TransactionCase("duplicate-containers-"+a+"-"+b,d=>{var gun=Gun();Put(d["a"].Progress,first,gun);Put(d["a"].Progress,second,gun);}); }
        TransactionCase("duplicate-participants",d=>{var gun=Gun();Put(d["a"].Progress,0,gun);Put(d["b"].Progress,1,gun);});
        TransactionCase("reserved-escrow-duplicate",d=>{var p=d["a"].Progress;var gun=Gun();Put(p,0,gun);p.ExpeditionState=1;p.ExpeditionId="run";p.Escrow=new InventoryGrid();p.EscrowEquipment=new EquipmentLoadout();p.Escrow.Slots[0]=gun;});
        TransactionCase("committed-escrow-historical",d=>{var p=d["a"].Progress;var gun=Gun();Put(d["b"].Progress,1,gun);p.ExpeditionState=2;p.ExpeditionId="run";p.Escrow=new InventoryGrid();p.EscrowEquipment=new EquipmentLoadout();p.Escrow.Slots[0]=gun;},true);
        TransactionCase("duplicate-inside-historical-escrow",d=>{var p=d["a"].Progress;var gun=Gun();p.ExpeditionState=2;p.ExpeditionId="run";p.Escrow=new InventoryGrid();p.EscrowEquipment=new EquipmentLoadout();p.Escrow.Slots[0]=gun;p.EscrowEquipment.Slots[6]=gun;});
        TransactionCase("unique-identities",d=>{for(int i=0;i<4;i++)Put(d["a"].Progress,i,Gun());},true);
    }
    static void Raw()
    {
        var mutations=new Dictionary<string,Action<JObject>> {
            {"missing-chest",p=>p.Remove("Chest")},{"null-raw-pack",p=>p["Pack"]=JValue.CreateNull()},
            {"missing-ground",p=>p.Remove("Ground")},{"missing-equipment",p=>p.Remove("Equipment")},
            {"raw-short-grid",p=>((JArray)p["Pack"]["Slots"]).RemoveAt(0)},
            {"raw-long-equipment",p=>((JArray)p["Equipment"]["Slots"]).Add(JObject.FromObject(default(ItemStack)))},
            {"raw-null-stack",p=>p["Chest"]["Slots"][0]=JValue.CreateNull()}
        };
        foreach(var entry in mutations)
        {
            current=entry.Key;report.Cases++;string dir=Path.Combine(output,current);Directory.CreateDirectory(dir);
            string name="raw-"+Guid.NewGuid().ToString("N"),path=Path.Combine(dir,name+".json");
            var profile=JObject.Parse(JsonUtility.ToJson(Empty()));entry.Value(profile);
            var doc=new JObject { ["Version"]=6,["Records"]=new JArray(new JObject { ["Session"]="a",["Progress"]=profile }) };
            string raw=doc.ToString();File.WriteAllText(path,raw); bool called=false;
            using(var store=new SharedCampMemory(name,dir)) Reject(()=>store.Transaction(data=>{called=true;return 0;}));
            Check(!called,"malformed raw document rejected before callback/normalization");
            Check(File.ReadAllText(path)==raw,"malformed original preserved");
            Check(!File.Exists(path+".bak"),"no blank backup created");
        }
    }
    static void Save()
    {
        current="repository-save";report.Cases++;CampRepository.Close();Check(CampRepository.Claim("t5-player","t5-room"),"lease acquired");
        var save=typeof(CampRepository).GetMethod("Save",BindingFlags.NonPublic|BindingFlags.Static);
        foreach(bool duplicate in new[]{false,true})
        {
            var prior=CampRepository.Get("t5-player");string before=JsonUtility.ToJson(prior);var candidate=prior.Copy();candidate.Revision++;
            if(duplicate) { var gun=Gun();candidate.Pack.Slots[0]=gun;candidate.Chest.Slots[0]=gun; } else candidate.Pack.Slots=new ItemStack[8];
            Reject(()=>save.Invoke(null,new object[]{"t5-player",candidate,prior.Revision,"t5-room"}));
            Check(JsonUtility.ToJson(CampRepository.Get("t5-player"))==before,"Save rejection preserves stored profile and revision");
        }
        CampRepository.Close();
    }
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();output=args[Array.IndexOf(args,"-t5-output")+1];Directory.CreateDirectory(output);report=new Report();
        try { Shapes();Identities();Raw();Save();report.Success=true; }
        catch(Exception e) { report.Error=e.ToString();Debug.LogException(e); }
        finally { CampRepository.Close();File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true)); }
        Debug.Log("POST_AUDIT_T5A "+(report.Success?"PASS":"FAIL")+" cases="+report.Cases+" checks="+report.Checks.Count);
        if(!report.Success) EditorApplication.Exit(1);
    }
}
