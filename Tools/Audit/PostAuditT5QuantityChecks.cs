using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CyberExtraction.Camp;
using CyberExtraction.Items;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Explicit batch probe, copied only into the isolated worker's Editor folder.
public static class PostAuditT5QuantityChecks
{
    [Serializable] sealed class Report { public bool Success; public string Error; public int Cases; public List<string> Checks=new List<string>(); }
    static Report report;
    static string output,current;
    static void Check(bool ok,string text) { if(!ok) throw new InvalidOperationException(current+": "+text); report.Checks.Add(current+": "+text); }
    static void Case(string name) { current=name; report.Cases++; }
    static void Reject(Action action)
    {
        Exception error=null; try {action();} catch(Exception e) {error=e;}
        Check(error is InvalidOperationException,"invalid candidate rejected");
    }
    static CampProgress Empty()
    {
        var p=CampProfileMigration.Create();p.Chest=new InventoryGrid();p.Pack=new InventoryGrid();p.Equipment=new EquipmentLoadout();
        p.Escrow=null;p.EscrowEquipment=null;return p;
    }
    // 0 chest, 1 pack, 2 ground, 3/4 reserved/committed pack, 5 equipped,
    // 6/7 reserved/committed equipment. Only compatible items use equipment slots.
    static void Put(CampProgress p,int place,ItemStack stack)
    {
        if(place==0) p.Chest.Slots[0]=stack;
        else if(place==1) p.Pack.Slots[0]=stack;
        else if(place==2) p.Ground.Add(new CampGroundItem { Id="drop",Stack=stack });
        else if(place==5) p.Equipment.Slots[ItemCatalog.Weapon(stack.Item)?6:0]=stack;
        else
        {
            p.ExpeditionState=(byte)(place==3 || place==6 ? 1 : 2);p.ExpeditionId="fixture";
            p.Escrow=new InventoryGrid();p.EscrowEquipment=new EquipmentLoadout();
            if(place<5) p.Escrow.Slots[0]=stack;
            else p.EscrowEquipment.Slots[ItemCatalog.Weapon(stack.Item)?6:0]=stack;
        }
    }
    static void Domain(string label,int place,ItemStack stack,bool valid)
    {
        Case(label+"-place"+place);var p=Empty();Put(p,place,stack);string before=JsonUtility.ToJson(p);
        Action validate=()=>CampProfileMigration.ValidateOwnership(new[]{new CampRecord {Session="fixture",Progress=p}});
        if(valid) {validate();Check(true,"boundary accepted");} else Reject(validate);
        Check(JsonUtility.ToJson(p)==before,"validator leaves candidate unchanged");
    }
    static void Quantities()
    {
        foreach(var item in new[]{ItemId.Wood,ItemId.AncientCoin,ItemId.BoltCutters,ItemId.PistolAmmo,ItemId.Pistol,ItemId.WatchmanHood})
        {
            int limit=ItemCatalog.Limit(item);
            foreach(int quantity in new[]{-1,0,1,limit,limit+1}.Distinct())
            {
                var stack=new ItemStack(item,1);stack.Quantity=quantity;
                foreach(int place in new[]{0,1,2,3,4}) Domain("quantity-"+item+"-"+quantity,place,stack,quantity>=0 && quantity<=limit);
                if(ItemCatalog.Weapon(item) || ItemCatalog.Equipment(item))
                    foreach(int place in new[]{5,6,7}) Domain("quantity-"+item+"-"+quantity,place,stack,quantity>=0 && quantity<=limit);
            }
        }
    }
    static void Ammunition()
    {
        foreach(var item in new[]{ItemId.Pistol,ItemId.Mosin,ItemId.DoubleBarrelShotgun,ItemId.ShortSword,ItemId.BattleAxe,ItemId.Spear})
        {
            int capacity=WeaponCatalog.Get(item).Capacity;
            foreach(int rounds in new[]{-1,0,capacity,capacity+1}.Distinct())
                foreach(int place in Enumerable.Range(0,8))
                    Domain("loaded-"+item+"-"+rounds,place,new ItemStack(item,1,0,rounds),rounds>=0 && rounds<=capacity);
            foreach(int bound in new[]{-1,0,capacity,capacity+1}.Distinct())
            {
                var stack=new ItemStack(item,1,0,capacity);stack.Instance.Origin=ItemOrigin.Recovery;stack.Instance.RecoveryRounds=bound;
                foreach(int place in Enumerable.Range(0,8)) Domain("recovery-"+item+"-"+bound,place,stack,bound>=0 && bound<=capacity);
            }
            if(capacity>0)
            {
                var stack=new ItemStack(item,1,0,capacity-1);stack.Instance.RecoveryRounds=capacity;
                foreach(int place in Enumerable.Range(0,8)) Domain("recovery-over-loaded-"+item,place,stack,false);
            }
        }
        var wood=new ItemStack(ItemId.Wood,1);wood.Instance.RecoveryRounds=1;
        foreach(int place in new[]{0,1,2,3,4}) Domain("nonweapon-recovery",place,wood,false);
        foreach(int data in new[]{int.MinValue,int.MaxValue})
            Domain("nonweapon-data-"+data,1,new ItemStack(ItemId.CookedFish,1,0,data),true);
        // Empty slots intentionally permit residual metadata; quantity validation precedes Empty.
        Domain("empty-weapon-residual",1,new ItemStack {Item=ItemId.Pistol,Quantity=0,Data=int.MaxValue},true);
    }
    static void Atomic()
    {
        string dir=Path.Combine(output,"atomic"),name="t5b-"+Guid.NewGuid().ToString("N");
        using(var store=new SharedCampMemory(name,dir))
        {
            store.Transaction(d=>{d["fixture"]=new CampRecord {Session="fixture",Progress=Empty()};return 0;});
            store.Transaction(d=>{d["fixture"].Progress.Revision=9;return 0;});
            string path=Path.Combine(dir,name+".json"),primary=File.ReadAllText(path),backup=File.ReadAllText(path+".bak");
            string profile=store.Transaction(d=>JsonUtility.ToJson(d["fixture"].Progress),false);
            foreach(int place in Enumerable.Range(0,8))
                foreach(bool ammo in new[]{false,true})
                {
                    Case("atomic-"+(ammo?"ammo":"quantity")+"-place"+place);
                    var stack=new ItemStack(ItemId.Pistol,1,0,WeaponCatalog.Get(ItemId.Pistol).Capacity);
                    if(ammo) stack.Data++;else stack.Quantity=2;
                    Reject(()=>store.Transaction(d=>{Put(d["fixture"].Progress,place,stack);d["fixture"].Progress.Revision++;return 0;}));
                    Check(File.ReadAllText(path)==primary,"primary bytes preserved");Check(File.ReadAllText(path+".bak")==backup,"backup bytes preserved");
                    Check(store.Transaction(d=>JsonUtility.ToJson(d["fixture"].Progress),false)==profile,"shared profile and revision preserved");
                }
        }
    }
    static string Document()
    {
        var p=Empty();p.Pack.Slots[0]=new ItemStack(ItemId.Pistol,1,0,0);
        return new JObject { ["Version"]=6,["Records"]=new JArray(new JObject { ["Session"]="fixture",["Progress"]=JObject.Parse(JsonUtility.ToJson(p)) }) }.ToString();
    }
    static void RawCase(string label,Action<JObject> mutate,bool valid=false,bool backupRecovery=false)
    {
        Case(label);string dir=Path.Combine(output,label);Directory.CreateDirectory(dir);
        string name="raw-"+Guid.NewGuid().ToString("N"),path=Path.Combine(dir,name+".json");
        string baseline=Document();var doc=JObject.Parse(baseline);mutate(doc);string raw=doc.ToString();File.WriteAllText(path,raw);
        if(backupRecovery) File.WriteAllText(path+".bak",baseline);
        bool called=false;var expected=JsonUtility.FromJson<CampRecord>(JObject.Parse(baseline)["Records"][0].ToString()).Progress.Pack.Slots[0];
        using(var store=new SharedCampMemory(name,dir))
        {
            Action read=()=>store.Transaction(d=>{
                called=true;var actual=d["fixture"].Progress.Pack.Slots[0];
                Check(actual.Data==0,"valid/default ammo is zero; actual="+actual.Data);
                Check(actual.Instance.RecoveryRounds==0,"valid/default recovery ammo is zero");
                Check(actual.Instance.SameIdentity(expected.Instance),"weapon identity preserved");return 0;
            });
            if(valid || backupRecovery) {read();Check(called,"valid profile reaches callback");}
            else {Reject(read);Check(!called,"invalid numeric document refused before callback");Check(File.ReadAllText(path)==raw,"invalid original bytes preserved");Check(!File.Exists(path+".bak"),"no empty backup written");}
        }
        if(backupRecovery)
        {
            Check(File.ReadAllText(path+".bak")==baseline,"valid backup bytes preserved");
            var archives=Directory.GetFiles(dir,name+".json.rejected-*.json");Check(archives.Length==1 && File.ReadAllText(archives[0])==raw,"invalid numeric primary archived exactly");
        }
    }
    static void RawNumbers()
    {
        foreach(string field in new[]{"Quantity","Data","RecoveryRounds"})
            foreach(string value in new[]{"2147483648","-2147483649","1.5","null","\"1\""})
            {
                string f=field,v=value;
                RawCase("raw-"+field+"-"+Array.IndexOf(new[]{"2147483648","-2147483649","1.5","null","\"1\""},value),doc=>{
                    var s=(JObject)doc["Records"][0]["Progress"]["Pack"]["Slots"][0];
                    if(f=="RecoveryRounds") s=(JObject)s["Instance"];s[f]=JToken.Parse(v);
                });
            }
        RawCase("raw-missing-historical-ammo",doc=>{
            doc["Records"][0]["Progress"]["SchemaVersion"]=3;
            var s=(JObject)doc["Records"][0]["Progress"]["Pack"]["Slots"][0];s.Remove("Data");((JObject)s["Instance"]).Remove("RecoveryRounds");
        },true);
        RawCase("raw-missing-current-ammo",doc=>{
            var s=(JObject)doc["Records"][0]["Progress"]["Pack"]["Slots"][0];s.Remove("Data");((JObject)s["Instance"]).Remove("RecoveryRounds");
        },true);
        RawCase("raw-negative-quantity",doc=>doc["Records"][0]["Progress"]["Pack"]["Slots"][0]["Quantity"]=-1);
        RawCase("raw-over-capacity",doc=>doc["Records"][0]["Progress"]["Pack"]["Slots"][0]["Data"]=WeaponCatalog.Get(ItemId.Pistol).Capacity+1);
        RawCase("raw-backup-over-capacity",doc=>doc["Records"][0]["Progress"]["Pack"]["Slots"][0]["Data"]=WeaponCatalog.Get(ItemId.Pistol).Capacity+1,false,true);
        RawCase("raw-backup-over-quantity",doc=>doc["Records"][0]["Progress"]["Pack"]["Slots"][0]["Quantity"]=2,false,true);
    }
    static void Bulk()
    {
        foreach(var item in new[]{ItemId.Wood,ItemId.AncientCoin,ItemId.PistolAmmo})
        {
            Case("bulk-"+item);var grid=new InventoryGrid();int limit=ItemCatalog.Limit(item),total=limit*2+3;
            Check(grid.Add(new ItemStack(item,total)),"valid bulk accepted");
            Check(grid.Count(item)==total,"bulk total conserved");
            Check(grid.Slots.Count(s=>!s.Empty)==3 && grid.Slots.Where(s=>!s.Empty).All(s=>s.Quantity<=limit),"bulk split across three legal stacks");
            var p=Empty();p.Pack=grid;CampProfileMigration.ValidateOwnership(new[]{new CampRecord {Session="fixture",Progress=p}});Check(true,"split inventory accepted by profile validator");
        }
    }
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();output=args[Array.IndexOf(args,"-t5b-output")+1];Directory.CreateDirectory(output);report=new Report();
        try {Quantities();Ammunition();Atomic();RawNumbers();Bulk();report.Success=true;}
        catch(Exception e) {report.Error=e.ToString();Debug.LogException(e);}
        finally {File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));}
        Debug.Log("POST_AUDIT_T5B "+(report.Success?"PASS":"FAIL")+" cases="+report.Cases+" checks="+report.Checks.Count);
        if(!report.Success) EditorApplication.Exit(1);
    }
    public static void DiagnoseLegacy()
    {
        var args=Environment.GetCommandLineArgs();string folder=args[Array.IndexOf(args,"-t5b-output")+1];Directory.CreateDirectory(folder);
        var doc=JObject.Parse(Document());var stack=(JObject)doc["Records"][0]["Progress"]["Pack"]["Slots"][0];
        stack.Remove("Data");((JObject)stack["Instance"]).Remove("RecoveryRounds");
        string json=doc["Records"][0].ToString();var decoded=JsonUtility.FromJson<CampRecord>(json);
        File.WriteAllText(Path.Combine(folder,"native-missing-fields.json"),new JObject {
            ["DataPresentInRaw"]=stack.ContainsKey("Data"),["RecoveryPresentInRaw"]=((JObject)stack["Instance"]).ContainsKey("RecoveryRounds"),
            ["NativeLoadedRounds"]=decoded.Progress.Pack.Slots[0].Data,["NativeRecoveryRounds"]=decoded.Progress.Pack.Slots[0].Instance.RecoveryRounds,
            ["StarterLoadedRounds"]=InventoryGrid.Starter().Slots[0].Data
        }.ToString());
        Debug.Log("POST_AUDIT_T5B_DIAG native loaded="+decoded.Progress.Pack.Slots[0].Data);
    }
}
