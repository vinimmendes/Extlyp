// Audit-only command driver: no production sources are replaced by this harness.
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;
using CyberExtraction.UI;
using CyberExtraction.Match;
using CyberExtraction.Camp;
using CyberExtraction.Players;
using CyberExtraction.Combat;
using CyberExtraction.Items;
using CyberExtraction.Enemies;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public sealed class Stage6Runtime : MonoBehaviour
{
    string dir,role; float next; bool busy; Keyboard keyboard; Mouse mouse; Gamepad pad;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install()
    {
        var args=Environment.GetCommandLineArgs(); if(Array.IndexOf(args,"-audit-stage6")<0) return;
        var go=new GameObject("Stage6AuditDriver"); DontDestroyOnLoad(go); go.AddComponent<Stage6Runtime>();
    }
    void Awake()
    {
        role=Arg("-audit-role"); dir=Path.Combine(Arg("-audit-output"),role); Directory.CreateDirectory(dir);
        Application.runInBackground=true; Application.targetFrameRate=30; QualitySettings.vSyncCount=0;
        if(role!="server") InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        Application.logMessageReceived+=Log;
    }
    static string Arg(string key) { var a=Environment.GetCommandLineArgs(); int i=Array.IndexOf(a,key); return i>=0 && i+1<a.Length ? a[i+1] : ""; }
    void Log(string s,string stack,LogType type) { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) File.AppendAllText(Path.Combine(dir,"errors.txt"),type+": "+s+"\n"+stack+"\n"); }
    void Update()
    {
        if(Time.realtimeSinceStartup<next || busy) return; next=Time.realtimeSinceStartup+.25f;
        try {
            File.WriteAllText(Path.Combine(dir,"state.json"),JsonUtility.ToJson(Snapshot(),true));
            string file=Path.Combine(dir,"command.json"); if(!File.Exists(file)) return;
            var cmd=JsonUtility.FromJson<Command>(File.ReadAllText(file)); File.Move(file,Path.Combine(dir,cmd.id+"-command.json"));
            StartCoroutine(Execute(cmd));
        } catch(Exception e) { File.AppendAllText(Path.Combine(dir,"driver-errors.txt"),e+"\n"); }
    }
    static object Call(object obj,string name,params object[] args) => obj.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(obj,args);
    IEnumerator Execute(Command c)
    {
        busy=true; string error=null;
        try { Act(c); } catch(Exception e) { error=e.ToString(); }
        if(c.action=="capture") { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(dir,c.id+".png")); }
        File.WriteAllText(Path.Combine(dir,c.id+"-result.json"),JsonUtility.ToJson(new Result { id=c.id,error=error,state=Snapshot() },true)); busy=false;
    }
    PlayerInventory Local => NetworkManager.Singleton!=null && NetworkManager.Singleton.LocalClient!=null && NetworkManager.Singleton.LocalClient.PlayerObject!=null ? NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerInventory>() : null;
    void Act(Command c)
    {
        var n=NetworkManager.Singleton; var b=NetworkBootstrap.Instance; var m=MatchManager.Instance; var p=Local;
        switch(c.action) {
            case "camp": b.ChooseEnvironment(CampEnvironment.Normal); if(!n.IsListening) b.BeginCamp("127.0.0.1"); break;
            case "expedition": b.ConfigureExpedition(ExpeditionMap.Dungeon,0); b.BeginExpedition("127.0.0.1"); break;
            case "start": Call(m,"StartMatch"); break;
            case "freeze": foreach(var a in FindObjectsByType<Monster>(FindObjectsSortMode.None)) a.enabled=false; foreach(var a in FindObjectsByType<CyberExtraction.Bots.BotController>(FindObjectsSortMode.None)) a.enabled=false; break;
            case "position": foreach(var a in PlayerInventory.All) if(a.IsServer && a.OwnerClientId==c.client) { a.transform.position=new Vector3(c.x,c.y,0); a.transform.rotation=Quaternion.Euler(0,0,c.angle); var rb=a.GetComponent<Rigidbody2D>(); if(rb!=null) rb.position=new Vector2(c.x,c.y); } break;
            case "chest": var chest=Instantiate(m.lootPrefab,new Vector3(c.x,c.y,0),Quaternion.identity); var l=chest.GetComponent<LootPickup>(); l.initialChest=true; l.initialLocked=false; l.initialQuantity=0; chest.GetComponent<NetworkObject>().Spawn(); break;
            case "open": foreach(var a in LootPickup.All) if(a.NetworkObjectId==c.target) a.RequestOpen(); break;
            case "fire": p.GetComponent<PlayerShooter>().RequestFire(c.angle); break;
            case "reload": p.GetComponent<PlayerShooter>().RequestReload(); break;
            case "input":
                GameInput.SetFocus(true);
                if(keyboard==null) keyboard=InputSystem.AddDevice<Keyboard>();
                InputSystem.QueueStateEvent(keyboard,c.value==1 ? new KeyboardState(Key.D) : c.value==2 ? new KeyboardState(Key.D,Key.LeftShift) : new KeyboardState()); break;
            case "aim":
                GameInput.SetFocus(true); if(mouse==null) mouse=InputSystem.AddDevice<Mouse>();
                InputSystem.QueueStateEvent(mouse,new MouseState { position=new Vector2(Screen.width*.25f,Screen.height*.5f) });
                if(pad==null) pad=InputSystem.AddDevice<Gamepad>();
                InputSystem.QueueStateEvent(pad,new GamepadState { rightStick=Vector2.up }); break;
            case "bag": InventoryWindow.Instance.ToggleBag(); break;
            case "drop": p.RequestDrop(c.value); break;
            case "add": foreach(var a in PlayerInventory.All) if(a.IsServer && a.OwnerClientId==c.client) a.ServerAddItem(ItemId.Metal,c.value); break;
            case "damage": foreach(var a in PlayerInventory.All) if(a.IsServer && a.OwnerClientId==c.client) a.GetComponent<Health>().ServerTakeDamage(c.value); break;
            case "bank": foreach(var a in PlayerInventory.All) if(a.IsServer && a.OwnerClientId==c.client) a.ServerBankExtraction(); break;
            case "disconnect": n.Shutdown(); break;
            case "capture": break;
            case "quit": Application.Quit(); break;
            default: throw new Exception("Unknown audit command: "+c.action);
        }
    }
    State Snapshot()
    {
        var n=NetworkManager.Singleton; var m=MatchManager.Instance;
        var s=new State { role=role,time=Time.realtimeSinceStartup,server=n!=null && n.IsServer,connected=n!=null && n.IsConnectedClient,
            camp=NetworkBootstrap.Instance!=null && NetworkBootstrap.Instance.CampConnection,synchronized=CampSession.Synchronized,
            state=m!=null ? m.CurrentState.ToString() : "none",map=MapBuilder.Current.ToString(),camera=Camera.main!=null ? Camera.main.orthographicSize : -1 };
        if(n!=null) s.client=n.LocalClientId;
        foreach(var p in PlayerInventory.All) if(p!=null && p.IsSpawned) {
            var controller=p.GetComponent<PlayerController>();
            var d=new Participant { id=p.NetworkObjectId,owner=p.OwnerClientId,human=p.NetworkObject.IsPlayerObject,local=p.IsOwner,x=p.transform.position.x,y=p.transform.position.y,angle=p.transform.eulerAngles.z,status=p.CurrentStatus.ToString(),vision=p.VisionAngle,health=p.GetComponent<Health>().Current.Value,stamina=controller!=null ? controller.Stamina.Value : -1 };
            foreach(var a in p.Slots) d.slots.Add(JsonUtility.ToJson(a)); foreach(var a in p.Equipment) d.equipment.Add(JsonUtility.ToJson(a));
            var shooter=p.GetComponent<PlayerShooter>(); if(shooter!=null) { d.shots=shooter.ShotSequence.Value; d.rounds=(p.IsOwner || p.IsServer) ? p.WeaponRounds : -1; } s.players.Add(d);
        }
        foreach(var l in LootPickup.All) if(l!=null && l.IsSpawned && l.Chest.Value) { var d=new Chest { id=l.NetworkObjectId,x=l.transform.position.x,y=l.transform.position.y,open=l.OpenState.Value }; foreach(var a in l.Contents) if(!a.Empty) d.contents.Add(a.Item+":"+a.Quantity); s.chests.Add(d); }
        return s;
    }
    [Serializable] public class Command { public string id,action; public int value; public ulong client,target; public float x,y,angle; }
    [Serializable] class Result { public string id,error; public State state; }
    [Serializable] class State { public string role,state,map; public float time,camera; public bool server,connected,camp,synchronized; public ulong client; public List<Participant> players=new List<Participant>(); public List<Chest> chests=new List<Chest>(); }
    [Serializable] class Participant { public ulong id,owner; public bool human,local; public float x,y,angle,vision,stamina; public int health,rounds; public uint shots; public string status; public List<string> slots=new List<string>(),equipment=new List<string>(); }
    [Serializable] class Chest { public ulong id; public float x,y; public int open; public List<string> contents=new List<string>(); }
}
