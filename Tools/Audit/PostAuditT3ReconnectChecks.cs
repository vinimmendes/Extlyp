using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Security.Cryptography;
using System.Collections.Generic;
using CyberExtraction.Camp;
using CyberExtraction.Combat;
using CyberExtraction.Core;
using CyberExtraction.Items;
using CyberExtraction.Match;
using CyberExtraction.Players;
using CyberExtraction.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor-only, copied to the existing isolated worker. No real identities or saves.
[InitializeOnLoad]
public static class PostAuditT3ReconnectChecks
{
    [Serializable] sealed class Report { public bool Success; public string Error; public List<string> Checks=new List<string>(); }
    const string Key="PostAuditT3Reconnect";
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static Report report;
    static string output,cluster,session,expedition,banked;
    static NetworkManager server,client;
    static NetworkBootstrap serverBootstrap,clientBootstrap;
    static CampNetwork serverCamp,clientCamp;
    static PlayerInventory participant;
    static ushort port;
    static ulong id;
    static int stage,round,disconnects,lootCount;
    static double deadline;
    static bool finishing;
    static PostAuditT3ReconnectChecks() { EditorApplication.playModeStateChanged+=Mode; }
    static void Set(object target,string name,object value)=>target.GetType().GetField(name,Hidden).SetValue(target,value);
    static object Call(object target,string name,params object[] args)
    { try { return target.GetType().GetMethod(name,Hidden).Invoke(target,args); } catch(TargetInvocationException e) { throw e.InnerException??e; } }
    static T Callback<T>(object target,string method) where T:Delegate => (T)Delegate.CreateDelegate(typeof(T),target,target.GetType().GetMethod(method,Hidden));
    static void Check(bool valid,string message) { if(!valid) throw new InvalidOperationException(message); report.Checks.Add("connection "+round+": "+message); }
    static string Economic(CampProgress profile) { var copy=profile.Copy(); copy.Revision=0; return JsonUtility.ToJson(copy); }
    static NetworkObject Template(string name)=>UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AuditT2C/"+name+".prefab")).GetComponent<NetworkObject>();
    public static void Run()
    {
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Batch Editor only");
        var args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,"-t3c2-output"); if(i<0 || i+1>=args.Length) throw new ArgumentException("-t3c2-output");
        SessionState.SetString(Key+"Output",args[i+1]); SessionState.SetBool(Key+"Active",true); SessionState.SetBool(Key+"Exit",false);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); EditorApplication.EnterPlaymode();
    }
    static void Mode(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key+"Exit",false))
        { SessionState.SetBool(Key+"Exit",false); EditorApplication.Exit(SessionState.GetInt(Key+"Code",1)); return; }
        if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key+"Active",false)) return;
        output=SessionState.GetString(Key+"Output",""); report=new Report(); deadline=EditorApplication.timeSinceStartup+90;
        Application.logMessageReceived+=Log;
        try { Setup(); EditorApplication.update+=Tick; } catch(Exception error) { Finish(error); }
    }
    static void Log(string text,string trace,LogType type)
    { if(!finishing && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert)) report.Error=(report.Error??"")+text+"\n"; }
    static NetworkManager Manager(string name)
    {
        var go=new GameObject(name); var transport=go.AddComponent<UnityTransport>(); var manager=go.AddComponent<NetworkManager>();
        manager.NetworkConfig=new NetworkConfig {NetworkTransport=transport,EnableSceneManagement=false,ConnectionApproval=true,ForceSamePrefabs=false,ProtocolVersion=GameConfig.NetworkProtocol};
        manager.AddNetworkPrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AuditT2C/Participant.prefab"));
        return manager;
    }
    static NetworkBootstrap Bootstrap(NetworkManager manager)
    {
        var result=manager.gameObject.AddComponent<NetworkBootstrap>(); result.enabled=false; Set(result,"manager",manager); return result;
    }
    static void Setup()
    {
        Directory.CreateDirectory(output); cluster="T3c2-"+Guid.NewGuid().ToString("N"); CampRepository.EnableShared(cluster,Path.Combine(output,"store"));
        using(var rsa=new RSACryptoServiceProvider(2048)) { rsa.PersistKeyInCsp=false; typeof(CampIdentity).GetField("key",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,CampKey.From(rsa.ExportParameters(true))); }
        using(var socket=new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback,0))) port=(ushort)((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
        server=Manager("T3c2 server"); server.SetSingleton(); ((UnityTransport)server.NetworkConfig.NetworkTransport).SetConnectionData("127.0.0.1",port,"127.0.0.1");
        serverBootstrap=Bootstrap(server); typeof(NetworkBootstrap).GetProperty("Instance").SetValue(null,serverBootstrap);
        server.ConnectionApprovalCallback=Callback<Action<NetworkManager.ConnectionApprovalRequest,NetworkManager.ConnectionApprovalResponse>>(serverBootstrap,"ApproveConnection");
        server.OnClientConnectedCallback+=Callback<Action<ulong>>(serverBootstrap,"Connected");
        server.OnClientDisconnectCallback+=Callback<Action<ulong>>(serverBootstrap,"Disconnected");
        server.OnClientDisconnectCallback+=leaving=>{if(leaving==id) disconnects++;};
        Check(server.StartServer(),"native UTP server started"); serverCamp=new CampNetwork(server,serverBootstrap); Set(serverBootstrap,"campNetwork",serverCamp); serverCamp.Register();
        var mm=Template("Match"); var match=mm.GetComponent<MatchManager>(); mm.Spawn(); match.State.Value=(byte)MatchState.Running;
        match.lootPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AuditT2C/Loot.prefab"); match.ServerDropStack(Vector2.zero,new ItemStack(ItemId.Wood,1));
        lootCount=LootPickup.All.Count; Check(lootCount==1,"positive drop control available");
    }
    static void Connect()
    {
        var join=new CampJoinRequest {CampOnly=true,Map=(byte)ExpeditionMap.Large}; CampIdentity.Sign(join);
        if(round==0) session=join.Session; Check(join.Session==session,"same ephemeral identity signs fresh login"); disconnects=0;
        client=Manager("T3c2 client "+round); ((UnityTransport)client.NetworkConfig.NetworkTransport).SetConnectionData("127.0.0.1",port);
        client.NetworkConfig.ConnectionData=Encoding.UTF8.GetBytes(JsonUtility.ToJson(join)); clientBootstrap=Bootstrap(client);
        typeof(NetworkBootstrap).GetProperty("CampConnection").SetValue(clientBootstrap,true);
        clientCamp=new CampNetwork(client,clientBootstrap); Set(clientBootstrap,"campNetwork",clientCamp);
        client.OnClientConnectedCallback+=Callback<Action<ulong>>(clientBootstrap,"Connected");
        client.OnClientDisconnectCallback+=Callback<Action<ulong>>(clientBootstrap,"Disconnected");
        Check(client.StartClient(),"distinct native UTP client started"); clientCamp.Register();
    }
    static CampProgress Received()=>string.IsNullOrEmpty(clientCamp.Snapshot.Profile)?null:JsonUtility.FromJson<CampProgress>(clientCamp.Snapshot.Profile);
    static void BeginExtraction()
    {
        id=client.LocalClientId; Check(serverBootstrap.RequestFor(id)!=null,"production signed approval and connected callbacks accepted client");
        var actor=Template("Participant"); actor.SpawnWithObservers=true; actor.CheckObjectVisibility=observer=>true; actor.SpawnAsPlayerObject(id);
        participant=actor.GetComponent<PlayerInventory>(); stage=2;
    }
    static void Bank()
    {
        Check(client.LocalClient.PlayerObject!=null,"participant prefab replicated to actual client");
        participant.ServerBeginExpedition(); participant.ServerCommitExpedition(); expedition=(string)typeof(PlayerInventory).GetField("expeditionId",Hidden).GetValue(participant);
        participant.ServerSetGrid(new InventoryGrid());
        var rare=EquipmentCatalog.Create(ItemId.WatchmanHood,ItemRarity.Rare);
        Check(participant.ServerAddItem(ItemId.Wood,3) && participant.ServerAddStack(rare),"sample loot includes rare equipment identity and bonuses");
        CampRepository.GrantExperience(new[]{new ExperienceGrant{Session=session,Expedition=expedition,Key="earned",Amount=20}});
        Check(participant.ServerBankExtraction(),"real participant extraction confirmed");
        var saved=CampRepository.Get(session); Check(saved.Character.Experience==24 && saved.Pack.Count(ItemId.Wood)==3 && saved.Pack.Count(ItemId.WatchmanHood)==1,"expected items and 24 XP saved");
        banked=Economic(saved);
        for(int i=0;i<3;i++) Check(participant.ServerBankExtraction(),"repeated extraction reports terminal success");
        Check(Economic(CampRepository.Get(session))==banked,"repeated extraction leaves economic state unchanged");
        serverCamp.Sync(id); stage=3;
    }
    static void CheckReceived(CampProgress received)
    {
        Check(Economic(received)==banked,"actual client snapshot matches complete banked profile except login revision");
        Check(received.ExpeditionState==0 && received.Character.Experience==24 && received.LastExperienceAward==24 && received.LastExperienceExpedition==expedition,"client sees settled expedition and one reward");
        Check(received.Pack.Count(ItemId.Wood)==3 && received.Pack.Count(ItemId.WatchmanHood)==1,"client receives original item quantities");
        Check(!CampSession.Pending && CampSession.Synchronized,"production snapshot and acknowledgement complete");
        server.DisconnectClient(id,"T3_RECONNECT_CHECK"); stage=4;
    }
    static void AfterDisconnect()
    {
        Check(disconnects==1 && serverBootstrap.RequestFor(id)==null,"actual NGO disconnect cleans old session request");
        Check(Economic(CampRepository.Get(session))==banked && LootPickup.All.Count==lootCount,"disconnect preserves banked profile without drops");
        if(round==0)
        {
            Check(participant==null || !participant.IsSpawned,"old participant despawned by NGO");
            CampRepository.Close(); CampRepository.EnableShared(cluster,Path.Combine(output,"store"));
            Check(Economic(CampRepository.Get(session))==banked,"repository reopened from disk before first reconnect");
        }
        client.Shutdown(); UnityEngine.Object.Destroy(client.gameObject); round++; stage=5;
    }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException("round "+round+" stage "+stage);
            if(!string.IsNullOrEmpty(report.Error)) throw new InvalidOperationException(report.Error);
            serverCamp.Tick(); if(client!=null && clientCamp!=null) clientCamp.Tick();
            if(stage==0) { Connect(); stage=1; }
            else if(stage==1 && client.IsConnectedClient && CampSession.Synchronized && !CampSession.Pending && Received()!=null)
            { id=client.LocalClientId; if(round==0) BeginExtraction(); else CheckReceived(Received()); }
            else if(stage==2 && client.LocalClient.PlayerObject!=null && client.LocalClient.PlayerObject.IsSpawned) Bank();
            else if(stage==3 && Received()!=null && Economic(Received())==banked && !CampSession.Pending) CheckReceived(Received());
            else if(stage==4 && disconnects>0 && !client.IsConnectedClient && !server.ConnectedClients.ContainsKey(id)) AfterDisconnect();
            else if(stage==5 && client==null) { if(round<3) stage=0; else Finish(null); }
        }
        catch(Exception error) { Finish(error); }
    }
    static void Finish(Exception error)
    {
        if(finishing) return; finishing=true; EditorApplication.update-=Tick;
        try { if(client!=null) client.Shutdown(); if(server!=null) server.Shutdown(); CampRepository.Close(); } catch(Exception cleanup) { error=error??cleanup; }
        Application.logMessageReceived-=Log; report.Success=error==null && string.IsNullOrEmpty(report.Error); report.Error=error?.ToString()??report.Error;
        File.WriteAllText(Path.Combine(output,"t3c2-result.json"),JsonUtility.ToJson(report,true)); Debug.Log("POST_AUDIT_T3C2_"+(report.Success?"OK":"FAILED"));
        SessionState.SetBool(Key+"Active",false); SessionState.SetBool(Key+"Exit",true); SessionState.SetInt(Key+"Code",report.Success?0:1); EditorApplication.ExitPlaymode();
    }
}
