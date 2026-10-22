using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using CyberExtraction.Camp;
using CyberExtraction.Items;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Three sequential native Editor processes; test fixtures only, no Play/build.
public static class PostAuditT4RestartChecks
{
    [Serializable] sealed class Document { public int Version; public CampRecord[] Records; }
    [Serializable] sealed class Entry
    {
        public string Label,Name,Identity,RecoveredJson;
        public int Schema,DocumentVersion,State,Wood,ExpectedWood,ExpectedXp,ExpectedRounds,Archives;
        public bool HasWeapon;
    }
    [Serializable] sealed class Manifest { public string Stage; public int PreparedPid,RecoveredPid; public List<Entry> Entries=new List<Entry>(); }
    [Serializable] sealed class Report { public bool Success; public string Error,Role; public int ProcessId; public List<string> Checks=new List<string>(); }
    static string output,role,label;
    static Report report;
    static void Check(bool value,string message) { if(!value) throw new InvalidOperationException(label+": "+message); report.Checks.Add(label+": "+message); }
    static string Identity(ItemStack item)=>item.Instance.IdHigh.ToString("x16")+item.Instance.IdLow.ToString("x16");
    static string Folder(Entry entry)=>Path.Combine(output,entry.Label);
    static string PathFor(Entry entry)=>Path.Combine(Folder(entry),entry.Name+".json");
    static string[] Archives(Entry entry)=>Directory.GetFiles(Folder(entry),entry.Name+".json.pre-v6-*.json");
    static string Arg(string[] args,string key) { int i=Array.IndexOf(args,key); if(i<0 || i+1>=args.Length) throw new ArgumentException(key); return args[i+1]; }
    static void Prepare(Manifest manifest)
    {
        manifest.Stage="Prepared"; manifest.PreparedPid=report.ProcessId;
        manifest.Entries.AddRange(new[]{
            new Entry {Label="legacy-idle",Schema=0,DocumentVersion=2,State=0,Wood=3,ExpectedWood=3,HasWeapon=true},
            new Entry {Label="legacy-reserved",Schema=0,DocumentVersion=2,State=1,Wood=4,ExpectedWood=4,HasWeapon=true},
            new Entry {Label="legacy-committed",Schema=3,DocumentVersion=5,State=2,Wood=6,ExpectedWood=0,ExpectedXp=10},
            new Entry {Label="current-reserved",Schema=4,DocumentVersion=6,State=1,Wood=8,ExpectedWood=8,HasWeapon=true},
            new Entry {Label="legacy-recovery-rounds",Schema=3,DocumentVersion=5,State=0,Wood=9,ExpectedWood=9,HasWeapon=true,ExpectedRounds=5}
        });
        foreach(var entry in manifest.Entries)
        {
            label=entry.Label; entry.Name="T4c-"+Guid.NewGuid().ToString("N"); Directory.CreateDirectory(Folder(entry));
            var p=new CampProgress {SchemaVersion=entry.Schema,Pack=new InventoryGrid(),Chest=new InventoryGrid(),Equipment=new EquipmentLoadout(),Revision=10};
            var weapon=new ItemStack(ItemId.Pistol,1,0,5);
            if(entry.Schema==0) weapon.Instance=default;
            else entry.Identity=Identity(weapon);
            if(entry.ExpectedRounds>0) weapon.Instance.Origin=ItemOrigin.Recovery; // Legacy v3 lacked bound-round metadata.
            var source=new InventoryGrid(); var equipment=new EquipmentLoadout();
            source.Slots[0]=new ItemStack(ItemId.Wood,entry.Wood); source.Slots[1]=new ItemStack(ItemId.PistolAmmo,7);
            if(entry.Schema==0) source.Slots[2]=weapon; else equipment.Slots[EquipmentLoadout.WeaponA]=weapon;
            if(entry.State==0) {p.Pack=source;p.Equipment=equipment;}
            else
            {
                p.ExpeditionState=(byte)entry.State; p.ExpeditionId=entry.Label; p.Escrow=source; p.EscrowEquipment=equipment;
                if(entry.State==2) {p.ExpeditionExperience=20;p.ExpeditionRewardKeys.Add("earned");}
            }
            var record=new CampRecord {Session="player",Lease="old-room",LeaseToken="old-token",LeaseExpires=DateTime.UtcNow.AddHours(1).Ticks,Progress=p};
            string json=JsonUtility.ToJson(new Document {Version=entry.DocumentVersion,Records=new[]{record}});
            File.WriteAllBytes(PathFor(entry),Encoding.UTF8.GetBytes(json)); File.WriteAllBytes(Path.Combine(Folder(entry),"input-original.json"),Encoding.UTF8.GetBytes(json));
            Check(JObject.Parse(json)["Records"][0]["Progress"]["SchemaVersion"].Value<int>()==entry.Schema,"legacy/current fixture serialized natively without pre-migration");
        }
    }
    static void Recover(Manifest manifest,bool restarting)
    {
        Check(manifest.Stage==(restarting?"Recovered":"Prepared"),"prior process completed required role");
        Check(manifest.PreparedPid!=report.ProcessId && (!restarting || manifest.RecoveredPid!=report.ProcessId),"verification runs in a distinct native Editor process");
        foreach(var entry in manifest.Entries)
        {
            label=entry.Label;
            CampRecord record;
            using(var store=new SharedCampMemory(entry.Name,Folder(entry))) record=store.Transaction(data=>data["player"]);
            var p=record.Progress;
            Check(p.SchemaVersion==CampProfileMigration.CurrentVersion && p.ExpeditionState==0,"profile migrated and expedition settled");
            Check(string.IsNullOrEmpty(record.Lease) && string.IsNullOrEmpty(record.LeaseToken) && record.LeaseExpires==0,"disk recovery clears transient room credentials");
            Check(p.Pack.Count(ItemId.Wood)==entry.ExpectedWood && p.Character.Experience==entry.ExpectedXp,"expected loot and confirmed experience retained");
            var gun=p.Equipment.Slots[EquipmentLoadout.WeaponA];
            if(entry.HasWeapon)
            {
                Check(gun.Item==ItemId.Pistol && gun.Quantity==1 && gun.Instance.HasIdentity && gun.Data==5,"weapon has identity and preserves five loaded rounds");
                Check(p.Pack.Count(ItemId.Pistol)==0 && p.Pack.Count(ItemId.PistolAmmo)==7,"legacy weapon moved to equipment without losing reserve ammo");
                Check(gun.Instance.RecoveryRounds==entry.ExpectedRounds,"recovery ammo binding matches supported migration");
                if(!string.IsNullOrEmpty(entry.Identity)) Check(Identity(gun)==entry.Identity,"existing weapon identity preserved");
            }
            else Check(gun.Empty && p.Pack.Count(ItemId.Pistol)==0 && p.LastExperienceExpedition==entry.Label && p.LastExperienceAward==10 && !p.LastExperienceExtracted,"committed loss does not restore escrow or repeat extraction bonus");
            Check(JObject.Parse(File.ReadAllText(PathFor(entry)))["Version"].Value<int>()==6,"checkpoint uses current document version");
            string[] archives=Archives(entry);
            if(entry.Schema<CampProfileMigration.CurrentVersion || entry.DocumentVersion<6)
            {
                Check(archives.Length==1 && Convert.ToBase64String(File.ReadAllBytes(archives[0]))==Convert.ToBase64String(File.ReadAllBytes(Path.Combine(Folder(entry),"input-original.json"))),"original migration input preserved byte for byte once");
            }
            else Check(archives.Length==0,"current format does not create unnecessary migration archive");
            if(restarting)
                Check(JsonUtility.ToJson(p)==entry.RecoveredJson && archives.Length==entry.Archives,"second process reload changes no items identities XP revision or archive count");
            else {entry.RecoveredJson=JsonUtility.ToJson(p);entry.Archives=archives.Length;}
        }
        manifest.Stage=restarting?"Verified":"Recovered"; if(!restarting) manifest.RecoveredPid=report.ProcessId;
    }
    public static void Run()
    {
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Batch Editor without Play only");
        var args=Environment.GetCommandLineArgs(); output=Arg(args,"-t4c-output"); role=Arg(args,"-t4c-role"); Directory.CreateDirectory(output);
        report=new Report {Role=role,ProcessId=System.Diagnostics.Process.GetCurrentProcess().Id}; var random=UnityEngine.Random.state;
        try
        {
            string manifestPath=Path.Combine(output,"manifest.json");
            var manifest=role=="Prepare"?new Manifest():JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            if(role=="Prepare") Prepare(manifest); else if(role=="Recover" || role=="Restart") Recover(manifest,role=="Restart"); else throw new ArgumentException(role);
            File.WriteAllText(manifestPath,JsonUtility.ToJson(manifest,true)); report.Success=true;
        }
        catch(Exception error) {report.Error=error.ToString();throw;}
        finally
        {
            UnityEngine.Random.state=random; File.WriteAllText(Path.Combine(output,role+"-result.json"),JsonUtility.ToJson(report,true));
            Debug.Log("POST_AUDIT_T4C_"+role+"_"+(report.Success?"OK":"FAILED"));
        }
    }
}
