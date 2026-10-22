using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Security.Cryptography;
using System.Collections;
using System.Collections.Generic;
using CyberExtraction.Camp;
using CyberExtraction.Core;
using CyberExtraction.Items;
using CyberExtraction.Match;
using CyberExtraction.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor-only loopback probe, installed only in the existing isolated worker.
[InitializeOnLoad]
public static class PostAuditT6NetworkChecks
{
    [Serializable] sealed class Report
    {
        public bool Success; public string Error; public int SmallBytes,LargeBytes,ActionBytes,LargeParts,ActionParts;
        public int PartialSamples,AtomicCallbacks; public List<string> Checks=new List<string>();
    }
    const string Key="PostAuditT6a",Channel="ce-camp-snapshot-parts-v1";
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static Report report;
    static string output,session,ack,priorProfile,expectedProfile;
    static NetworkManager server,client;
    static NetworkBootstrap serverBootstrap,clientBootstrap;
    static CampNetwork serverCamp,clientCamp;
    static int stage,partialBefore;
    static bool finishing,captured;
    static double deadline;
    static ulong id;
    static PostAuditT6NetworkChecks() { EditorApplication.playModeStateChanged+=Mode; }
    static object Get(object target,string name)=>target.GetType().GetField(name,Fields).GetValue(target);
    static void Set(object target,string name,object value)=>target.GetType().GetField(name,Fields).SetValue(target,value);
    static T Callback<T>(object target,string method) where T:Delegate =>
        (T)Delegate.CreateDelegate(typeof(T),target,target.GetType().GetMethod(method,Fields));
    static void Check(bool ok,string message) { if(!ok) throw new InvalidOperationException("stage "+stage+": "+message);report.Checks.Add("stage "+stage+": "+message); }
    public static void Run()
    {
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Batch Editor only");
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-t6a-output");if(i<0) throw new ArgumentException("-t6a-output");
        SessionState.SetString(Key+"Output",args[i+1]);SessionState.SetBool(Key+"Active",true);SessionState.SetBool(Key+"Exit",false);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();
    }
    static void Mode(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key+"Exit",false))
        {SessionState.SetBool(Key+"Exit",false);EditorApplication.Exit(SessionState.GetInt(Key+"Code",1));return;}
        if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key+"Active",false)) return;
        output=SessionState.GetString(Key+"Output","");report=new Report();deadline=EditorApplication.timeSinceStartup+75;
        Application.logMessageReceived+=Log;
        try {Setup();EditorApplication.update+=Tick;} catch(Exception e) {Finish(e);}
    }
    static void Log(string text,string trace,LogType type)
    {if(!finishing && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert)) report.Error=(report.Error??"")+text+"\n";}
    static NetworkManager Manager(string name)
    {
        var go=new GameObject(name);var transport=go.AddComponent<UnityTransport>();var manager=go.AddComponent<NetworkManager>();
        manager.NetworkConfig=new NetworkConfig {NetworkTransport=transport,EnableSceneManagement=false,ConnectionApproval=true,ForceSamePrefabs=false,ProtocolVersion=GameConfig.NetworkProtocol};
        manager.AddNetworkPrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AuditT2C/Participant.prefab"));return manager;
    }
    static NetworkBootstrap Bootstrap(NetworkManager manager)
    {
        var bootstrap=manager.gameObject.AddComponent<NetworkBootstrap>();bootstrap.enabled=false;Set(bootstrap,"manager",manager);return bootstrap;
    }
    static void Setup()
    {
        Directory.CreateDirectory(output);CampRepository.EnableShared("T6a-"+Guid.NewGuid().ToString("N"),Path.Combine(output,"store"));
        using(var rsa=new RSACryptoServiceProvider(2048))
        {rsa.PersistKeyInCsp=false;typeof(CampIdentity).GetField("key",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,CampKey.From(rsa.ExportParameters(true)));}
        ushort port;using(var socket=new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback,0)))
            port=(ushort)((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
        server=Manager("T6a server");server.SetSingleton();((UnityTransport)server.NetworkConfig.NetworkTransport).SetConnectionData("127.0.0.1",port,"127.0.0.1");
        serverBootstrap=Bootstrap(server);typeof(NetworkBootstrap).GetProperty("Instance").SetValue(null,serverBootstrap);
        server.ConnectionApprovalCallback=Callback<Action<NetworkManager.ConnectionApprovalRequest,NetworkManager.ConnectionApprovalResponse>>(serverBootstrap,"ApproveConnection");
        server.OnClientConnectedCallback+=Callback<Action<ulong>>(serverBootstrap,"Connected");server.OnClientDisconnectCallback+=Callback<Action<ulong>>(serverBootstrap,"Disconnected");
        Check(server.StartServer(),"native UTP server started");serverCamp=new CampNetwork(server,serverBootstrap);Set(serverBootstrap,"campNetwork",serverCamp);serverCamp.Register();
        var match=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AuditT2C/Match.prefab")).GetComponent<NetworkObject>();match.Spawn();match.GetComponent<MatchManager>().State.Value=(byte)MatchState.Running;
        var join=new CampJoinRequest {CampOnly=true,Map=(byte)ExpeditionMap.Large};CampIdentity.Sign(join);session=join.Session;
        client=Manager("T6a client");((UnityTransport)client.NetworkConfig.NetworkTransport).SetConnectionData("127.0.0.1",port);
        client.NetworkConfig.ConnectionData=Encoding.UTF8.GetBytes(JsonUtility.ToJson(join));clientBootstrap=Bootstrap(client);
        typeof(NetworkBootstrap).GetProperty("CampConnection").SetValue(clientBootstrap,true);
        clientCamp=new CampNetwork(client,clientBootstrap);Set(clientBootstrap,"campNetwork",clientCamp);
        client.OnClientConnectedCallback+=Callback<Action<ulong>>(clientBootstrap,"Connected");client.OnClientDisconnectCallback+=Callback<Action<ulong>>(clientBootstrap,"Disconnected");
        Check(client.StartClient(),"distinct native UTP client started");clientCamp.Register();CampSession.Changed+=Observed;
    }
    static void Observed()
    {
        if(stage<1 || stage>2 || clientCamp.Snapshot.Ack!=ack) return;
        try
        {
            Check(!CampSession.Pending && CampSession.Synchronized,"observer sees acknowledgement and synchronized state together");
            Check(clientCamp.Snapshot.Profile==JsonUtility.ToJson(CampSession.Progress),"observer sees complete matching profile");report.AtomicCallbacks++;
        }
        catch(Exception error) {report.Error=(report.Error??"")+error+"\n";}
    }
    static string Owner()=> (string)typeof(NetworkBootstrap).GetMethod("LeaseOwner",Fields).Invoke(serverBootstrap,new object[]{id});
    static void StartLarge()
    {
        id=client.LocalClientId;Check(serverBootstrap.RequestFor(id)?.Session==session,"signed login accepted by production callbacks");
        priorProfile=clientCamp.Snapshot.Profile;report.SmallBytes=Encoding.UTF8.GetByteCount(JsonUtility.ToJson(clientCamp.Snapshot));
        Check(report.SmallBytes<32768,"initial complete envelope below old limit");
        Check(priorProfile==JsonUtility.ToJson(CampRepository.Get(session)),"small snapshot matches authoritative profile");
        CampRepository.Edit(session,p=>{
            p.Pack=new InventoryGrid();p.Chest=new InventoryGrid();p.Pack.Add(new ItemStack(ItemId.Wood,4));p.Ground.Clear();
            for(int i=0;i<500;i++)
            {
                var stack=i%19==0 ? EquipmentCatalog.Create(ItemId.WatchmanHood,ItemRarity.Rare) :
                    i%13==0 ? new ItemStack(ItemId.Mosin,1,0,5) : new ItemStack(ItemId.Wood,i%20+1);
                p.Ground.Add(new CampGroundItem {Id=(i==0?new string('界',9000):"item")+"-"+i+"-á漢🙂",Stack=stack});
            }
            p.Revision++;p.Sync();return 0;
        },Owner());
        var command=new CampCommand {Kind=CampCommandKind.Sync};ack=command.Id;captured=false;partialBefore=report.PartialSamples;stage=1;
        Check(clientCamp.Request(command),"large Sync command sent over actual NGO channel");Check(CampSession.Pending,"Sync acknowledgement pending");
    }
    static void Capture()
    {
        if(captured || stage<1 || stage>2) return;
        var sender=Get(serverCamp,"snapshotSender");var boxes=(IDictionary)Get(sender,"outboxes");if(!boxes.Contains(id)) return;
        var box=boxes[id];var transfer=Get(box,"Current");if(transfer==null) return;
        var snapshot=(CampSnapshot)Get(transfer,"Snapshot");if(snapshot.Ack!=ack) return;
        var bytes=(byte[])Get(transfer,"Bytes");int parts=(bytes.Length+8191)/8192;
        Check(bytes.Length>32768 && parts>4,"complete large envelope requires multiple sender ticks");
        bool split=false;for(int index=8192;index<bytes.Length;index+=8192) if((bytes[index]&0xc0)==0x80) split=true;
        Check(split,"at least one 8192-byte boundary splits a UTF-8 character");
        Check(snapshot.Profile==JsonUtility.ToJson(CampRepository.Get(session)),"queued payload contains authoritative complete profile");
        File.WriteAllBytes(Path.Combine(output,stage==1?"large-envelope.json":"action-envelope.json"),bytes);
        if(stage==1) {report.LargeBytes=bytes.Length;report.LargeParts=parts;}else {report.ActionBytes=bytes.Length;report.ActionParts=parts;}
        expectedProfile=snapshot.Profile;captured=true;
    }
    static void Partial()
    {
        if(Get(Get(clientCamp,"snapshotReceiver"),"bytes")==null) return;
        report.PartialSamples++;
        if(!CampSession.Pending || clientCamp.Snapshot.Profile!=priorProfile || JsonUtility.ToJson(CampSession.Progress)!=priorProfile)
            throw new InvalidOperationException("Partial transfer applied a profile or acknowledged early");
    }
    static void Complete()
    {
        Check(captured,"actual production sender payload captured");Check(report.PartialSamples>partialBefore,"incomplete assembly observed without applying profile or ACK");
        Check(clientCamp.Snapshot.Profile==expectedProfile && JsonUtility.ToJson(CampSession.Progress)==expectedProfile,"all profile bytes reconstructed and applied exactly");
        Check(CampSession.Progress.Ground.Count==500,"all 500 ground entries retained");
        Check(clientCamp.Snapshot.Ack==ack && CampSession.Synchronized && !CampSession.Pending,"own ACK releases pending command after complete application");
        Check(client.IsConnectedClient && server.ConnectedClients.ContainsKey(id),"connection remains active after large transfer");
        Check(clientCamp.Snapshot.Message==CampSession.Message && !string.IsNullOrEmpty(CampSession.Message),"completion message applied with profile and ACK");
        if(stage==1)
        {
            priorProfile=expectedProfile;captured=false;partialBefore=report.PartialSamples;
            var action=new CampAction {Kind=CampActionKind.Transfer,SourceGrid=0,Source=0,TargetGrid=1,Target=0,Sequence=CampSession.Progress.Revision+1};
            var command=new CampCommand {Kind=CampCommandKind.Action,Action=action};ack=command.Id;stage=2;
            Check(clientCamp.Request(command),"inventory transfer command sent on actual command channel");Check(CampSession.Pending,"inventory command awaits its own large reply");
        }
        else
        {
            var saved=CampRepository.Get(session);
            Check(saved.Chest.Count(ItemId.Wood)==4 && saved.Pack.Count(ItemId.Wood)==0,"inventory transfer moves all four wood exactly once");
            Check(saved.Revision==JsonUtility.FromJson<CampProgress>(priorProfile).Revision+1,"inventory operation advances revision once");
            Check(saved.AppliedActions.Count==1,"one accepted inventory action recorded");
            Check(report.AtomicCallbacks==2,"one complete observer publication per large reply");Finish(null);
        }
    }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException("stage "+stage);
            if(!string.IsNullOrEmpty(report.Error)) throw new InvalidOperationException(report.Error);
            Capture();serverCamp.Tick();clientCamp.Tick();
            if(stage==0 && client.IsConnectedClient && CampSession.Synchronized && !CampSession.Pending && !string.IsNullOrEmpty(clientCamp.Snapshot.Profile)) StartLarge();
            else if(stage==1 || stage==2)
            {Partial();if(!CampSession.Pending && clientCamp.Snapshot.Ack==ack) Complete();}
        }
        catch(Exception error) {Finish(error);}
    }
    static void Finish(Exception error)
    {
        if(finishing) return;finishing=true;EditorApplication.update-=Tick;CampSession.Changed-=Observed;
        try {if(client!=null) client.Shutdown();if(server!=null) server.Shutdown();CampRepository.Close();}catch(Exception e){error=error??e;}
        Application.logMessageReceived-=Log;report.Success=error==null && string.IsNullOrEmpty(report.Error);report.Error=error?.ToString()??report.Error;
        File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));Debug.Log("POST_AUDIT_T6A "+(report.Success?"PASS":"FAIL")+" checks="+report.Checks.Count);
        SessionState.SetBool(Key+"Active",false);SessionState.SetBool(Key+"Exit",true);SessionState.SetInt(Key+"Code",report.Success?0:1);EditorApplication.ExitPlaymode();
    }
}
