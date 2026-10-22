using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CyberExtraction.Items;

// Read-only Editor checks. No scenes, players, repository, saves or build pipeline.
public static class PostAuditT1Checks
{
    [Serializable] sealed class Report
    {
        public string Unity,Assembly;
        public bool Success;
        public List<string> Checks=new List<string>();
        public string Error;
    }
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs(); int index=Array.IndexOf(args,"-t1-output");
        if(index<0 || index+1>=args.Length) throw new ArgumentException("-t1-output required");
        string output=args[index+1]; Directory.CreateDirectory(output);
        var report=new Report { Unity=Application.unityVersion,Assembly=typeof(ItemStack).Assembly.Location };
        var random=UnityEngine.Random.state;
        try
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Editor only");
            var models=Resources.LoadAll<ItemDefinition>("Items/Equipment/Definitions");
            Require(models.Length==EquipmentCatalog.ModelCount,"Expected 18 native equipment definitions");
            var ids=new HashSet<ItemId>();
            foreach(var model in models)
            {
                Require(model!=null && ids.Add(model.Id),"Missing or duplicated equipment asset");
                Require(EquipmentCatalog.Get(model.Id)==model,"Catalog/native asset mismatch");
                var serialized=new SerializedObject(model);
                Require(serialized.FindProperty("SecondaryPool")?.arraySize==2,"Native secondary pool invalid: "+model.name);
                for(int rarity=0;rarity<=2;rarity++)
                {
                    var stack=EquipmentCatalog.Create(model.Id,(ItemRarity)rarity);
                    Require(EquipmentCatalog.ValidInstance(stack) && EquipmentLoadout.Accepts((int)model.Slot-1,stack),"Generated equipment invalid: "+model.name);
                }
                report.Checks.Add("equipment: "+model.Id+" | "+AssetDatabase.GetAssetPath(model)+" | 3 rarities accepted");
            }
            foreach(var type in new[] { typeof(CyberExtraction.Combat.LootPickup),typeof(CyberExtraction.Enemies.Monster),
                typeof(CyberExtraction.Players.PlayerInventory),typeof(CyberExtraction.Players.PlayerShooter),typeof(CyberExtraction.Players.PlayerController) })
            {
                int handlers=type.GetMethods(BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Static|BindingFlags.DeclaredOnly)
                    .Count(method=>method.Name.StartsWith("__rpc_handler_",StringComparison.Ordinal));
                Require(handlers>0,"NGO generated handlers missing: "+type.FullName);
                report.Checks.Add("NGO ILPP: "+type.FullName+" | "+handlers+" handlers");
            }
            Require(typeof(Unity.Netcode.INetworkSerializable).IsAssignableFrom(typeof(ItemStack)),"ItemStack serializer missing");
            Require(typeof(Unity.Netcode.INetworkSerializable).IsAssignableFrom(typeof(ItemInstance)),"ItemInstance serializer missing");
            Require(typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule).IsAssignableFrom(typeof(CyberExtraction.UI.ContextUiInputModule)),"UI module type invalid");
            report.Checks.Add("serialization/UI type metadata loaded");
            report.Success=true;
        }
        catch(Exception error) { report.Error=error.ToString(); throw; }
        finally
        {
            UnityEngine.Random.state=random;
            File.WriteAllText(Path.Combine(output,"t1-result.json"),JsonUtility.ToJson(report,true));
            Debug.Log("POST_AUDIT_T1_"+(report.Success ? "OK" : "FAILED"));
        }
    }
    static void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
}
