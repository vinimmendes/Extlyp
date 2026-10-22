using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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

// Copied to the isolated project's Editor folder. Never added to game builds.
[InitializeOnLoad]
public static class PostAuditT2NetworkChecks
{
    [Serializable] sealed class Report { public bool Success; public string Error; public List<string> Checks=new List<string>(); public List<string> ExpectedErrors=new List<string>(); }
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const BindingFlags StaticPrivate=BindingFlags.Static|BindingFlags.NonPublic;
    const string Key="PostAuditT2Network";
    static Report report;
    static string output,session,cluster,newRecord;
    static NetworkManager server,client;
    static NetworkBootstrap bootstrap;
    static CampNetwork camp;
    static MatchManager match;
    static PlayerInventory inventory;
    static Health health;
    static CampActivity activity;
    static ulong id;
    static ushort port;
    static int stage,scenario,callbacks,lootCount;
    static bool timeUp,injecting,finishing,lostLeaseDespawn;
    static object newClaim;
    static double deadline;
    static SharedCampMemory Store => (SharedCampMemory)typeof(CampRepository).GetField("shared",StaticPrivate).GetValue(null);
    static IDictionary Requests => (IDictionary)Get(bootstrap,"requests");
    static IDictionary Claims => (IDictionary)typeof(CampRepository).GetField("heldLeases",StaticPrivate).GetValue(null);
    static object Get(object target,string field)=>target.GetType().GetField(field,Private).GetValue(target);
    static void Set(object target,string field,object value)=>target.GetType().GetField(field,Private).SetValue(target,value);
    static object Invoke(object target,string method,params object[] args)
    { try { return target.GetType().GetMethod(method,Private).Invoke(target,args); } catch(TargetInvocationException e) { throw e.InnerException??e; } }
    static T Callback<T>(string method) where T:Delegate => (T)Delegate.CreateDelegate(typeof(T),bootstrap,typeof(NetworkBootstrap).GetMethod(method,Private));
    static void Check(bool value,string message) { if(!value) throw new InvalidOperationException(message); report.Checks.Add("case "+scenario+": "+message); }
    static string Record()=>Store.Transaction(data=>JsonUtility.ToJson(data[session]),false);
    static PostAuditT2NetworkChecks() { EditorApplication.playModeStateChanged+=Mode; }
    public static void Run()
    {
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Batch Editor only");
        var args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,"-t2c-output");
        if(i<0 || i+1>=args.Length) throw new ArgumentException("-t2c-output");
        SessionState.SetString(Key+"Output",args[i+1]); SessionState.SetBool(Key+"Active",true); SessionState.SetBool(Key+"Exit",false);
        SessionState.SetInt(Key+"Case",Array.IndexOf(args,"-t2c-raw-disconnect")>=0?3:0);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Directory.CreateDirectory("Assets/AuditT2C");
        foreach(string name in new[]{"Match","Loot","Participant"})
        {
            var fixture=new GameObject("T2c "+name); var obj=fixture.AddComponent<NetworkObject>(); obj.SpawnWithObservers=false;
            if(name=="Match") fixture.AddComponent<MatchManager>().enabled=false;
            else if(name=="Loot") { fixture.AddComponent<SpriteRenderer>(); fixture.AddComponent<LootPickup>(); }
            else { fixture.AddComponent<PlayerInventory>(); fixture.AddComponent<Health>(); }
            string path="Assets/AuditT2C/"+name+".prefab";
            var asset=PrefabUtility.SaveAsPrefabAsset(fixture,path);
            UnityEngine.Object.DestroyImmediate(fixture);
            typeof(NetworkObject).GetMethod("OnValidate",Private).Invoke(asset.GetComponent<NetworkObject>(),null);
            EditorUtility.SetDirty(asset);
        }
        AssetDatabase.SaveAssets();
        EditorApplication.EnterPlaymode();
    }
    static void Mode(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key+"Exit",false))
        {
            SessionState.SetBool(Key+"Exit",false);
            EditorApplication.Exit(SessionState.GetInt(Key+"Code",1)); return;
        }
        if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key+"Active",false)) return;
        output=SessionState.GetString(Key+"Output",""); report=new Report(); deadline=EditorApplication.timeSinceStartup+90;
        scenario=SessionState.GetInt(Key+"Case",0);
        Application.logMessageReceived+=Log;
        try { Setup(); EditorApplication.update+=Tick; } catch(Exception error) { Finish(error); }
    }
    static void Log(string text,string trace,LogType type)
    {
        if(type==LogType.Warning && text.StartsWith("Participante removido sem drops: a sala perdeu a posse")) lostLeaseDespawn=true;
        if(type!=LogType.Error && type!=LogType.Exception && type!=LogType.Assert) return;
        if(injecting && (text.StartsWith("Falha ao renovar sessao de acampamento:") || text.StartsWith("Falha ao finalizar conexao de acampamento:"))) report.ExpectedErrors.Add(text);
        else if(!finishing) report.Error=(report.Error??"")+text+"\n";
    }
    static NetworkManager Manager(string name)
    {
        var go=new GameObject(name); var transport=go.AddComponent<UnityTransport>();
        var nm=go.AddComponent<NetworkManager>();
        nm.NetworkConfig=new NetworkConfig {NetworkTransport=transport,EnableSceneManagement=false,ConnectionApproval=true,ForceSamePrefabs=false,ProtocolVersion=GameConfig.NetworkProtocol};
        return nm;
    }
    static NetworkObject Object(string name)
    { return UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AuditT2C/"+name+".prefab")).GetComponent<NetworkObject>(); }
    static void Setup()
    {
        Directory.CreateDirectory(output); cluster="T2c-"+Guid.NewGuid().ToString("N");
        CampRepository.EnableShared(cluster,Path.Combine(output,"store"));
        using(var socket=new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback,0))) port=(ushort)((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
        server=Manager("T2c server"); server.SetSingleton();
        ((UnityTransport)server.NetworkConfig.NetworkTransport).SetConnectionData("127.0.0.1",port,"127.0.0.1");
        bootstrap=server.gameObject.AddComponent<NetworkBootstrap>(); bootstrap.enabled=false;
        Set(bootstrap,"manager",server);
        typeof(NetworkBootstrap).GetProperty("Instance").SetValue(null,bootstrap);
        server.ConnectionApprovalCallback=Callback<Action<NetworkManager.ConnectionApprovalRequest,NetworkManager.ConnectionApprovalResponse>>("ApproveConnection");
        server.OnClientDisconnectCallback+=Callback<Action<ulong>>("Disconnected");
        server.OnClientConnectedCallback+=Callback<Action<ulong>>("Connected");
        server.OnClientDisconnectCallback+=disconnected=>{if(disconnected==id) callbacks++;};
        Check(server.StartServer(),"NGO server started with Unity Transport loopback");
        camp=new CampNetwork(server,bootstrap); Set(bootstrap,"campNetwork",camp); camp.Register();
        var mm=Object("Match"); match=mm.GetComponent<MatchManager>(); mm.Spawn();
        match.lootPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AuditT2C/Loot.prefab");
        match.State.Value=(byte)MatchState.Running;
        match.ServerDropStack(Vector2.zero,new ItemStack(ItemId.Wood,1));
        Check(LootPickup.All.Count==1,"positive control: real drop path creates loot"); lootCount=LootPickup.All.Count;
    }
    static void Connect()
    {
        callbacks=0; timeUp=false; lostLeaseDespawn=false;
        using(var rsa=new RSACryptoServiceProvider(2048)) { rsa.PersistKeyInCsp=false; typeof(CampIdentity).GetField("key",StaticPrivate).SetValue(null,CampKey.From(rsa.ExportParameters(true))); }
        var join=new CampJoinRequest {CampOnly=true,Map=(byte)ExpeditionMap.Large}; CampIdentity.Sign(join); session=join.Session;
        client=Manager("T2c client "+scenario);
        ((UnityTransport)client.NetworkConfig.NetworkTransport).SetConnectionData("127.0.0.1",port);
        client.NetworkConfig.ConnectionData=Encoding.UTF8.GetBytes(JsonUtility.ToJson(join));
        Check(client.StartClient(),"NGO client started with ephemeral signed identity");
        string snapshotName=(string)typeof(CampNetwork).Assembly.GetType("CyberExtraction.Camp.CampSnapshotTransfer").GetField("MessageName").GetRawConstantValue();
        client.CustomMessagingManager.RegisterNamedMessageHandler(snapshotName,(sender,reader)=>{});
    }
    static void PrepareParticipant()
    {
        id=client.LocalClientId;
        Check(server.ConnectedClients.ContainsKey(id) && bootstrap.RequestFor(id)!=null,"real approval/connected callbacks registered session");
        var join=bootstrap.RequestFor(id);
        Check(CampRepository.Get(session).ExpeditionState==0,"signed login loaded camp");
        CampRepository.Begin(session,"old",out _); CampRepository.Commit(session,"old");
        join.CampOnly=false;
        ((IDictionary)Get(bootstrap,"slots"))[id]=0; ((HashSet<ulong>)Get(bootstrap,"readyClients")).Add(id);
        activity=new CampActivity {Phase=1}; ((IDictionary)Get(camp,"activities"))[id]=activity;
        var obj=Object("Participant"); inventory=obj.GetComponent<PlayerInventory>(); health=obj.GetComponent<Health>();
        obj.CheckObjectVisibility=observer=>false; obj.SpawnWithOwnership(id);
        server.ConnectedClients[id].PlayerObject=obj;
        Set(inventory,"progressionSession",session); Set(inventory,"expeditionId","old"); Set(inventory,"progressionCommitted",true);
        inventory.ServerSetGrid(new InventoryGrid()); inventory.ServerAddItem(ItemId.Wood,3);
        inventory.Status.OnValueChanged+=(previous,current)=>{if(current==(byte)PlayerStatus.TimeUp) timeUp=true;};
        Check(PlayerInventory.All.Contains(inventory) && Health.All.Contains(health),"participant spawned and tracked");
        object oldClaim=Claims[session];
        Store.Transaction(data=>{data[session].LeaseExpires=0;return 0;});
        Check(CampRepository.Claim(session,"new-room"),"new owner acquired expired session");
        CampRepository.Begin(session,"new",out _); CampRepository.Commit(session,"new"); newRecord=Record();
        newClaim=Claims[session];
        if(scenario==3) Claims[session]=oldClaim; // Preserve the obsolete room's local credential for raw NGO despawn.
    }
    static void Trigger()
    {
        Set(bootstrap,"nextLeaseRefresh",0d);
        if(scenario==3)
        {
            server.DisconnectClient(id,"T2_RAW_DISCONNECT");
            Check(!timeUp,"direct NGO disconnect bypasses preemptive TimeUp path");
            return;
        }
        if(scenario==1)
        {
            var mutex=(Mutex)Get(Store,"mutex"); using(var entered=new ManualResetEvent(false)) using(var release=new ManualResetEvent(false))
            {
                var thread=new Thread(()=>{mutex.WaitOne();entered.Set();try{release.WaitOne(20000);}finally{mutex.ReleaseMutex();}});
                thread.IsBackground=true; thread.Start();
                Check(entered.WaitOne(2000),"fixture holds actual storage mutex on another thread");
                injecting=true;
                try { Invoke(bootstrap,"Update"); }
                finally { release.Set(); thread.Join(5000); injecting=false; }
                Check(report.ExpectedErrors.Count==2,"renewal and finalization storage timeouts were caught");
            }
        }
        else if(scenario==2)
        {
            bootstrap.RequestFor(id).CampOnly=true;
            camp.Sync(id); // GetOwned detects superseded connection; production catch disconnects.
        }
        else Invoke(bootstrap,"Update");
        Check(timeUp,"participant marked TimeUp before NGO despawn");
    }
    static void Verify()
    {
        if(scenario==3)
        {
            Check(lostLeaseDespawn,"alive despawn caught actual CampLeaseLostException without drops");
            Claims[session]=newClaim;
        }
        Check(callbacks==1,"NGO delivered one actual disconnect callback");
        Check(!server.ConnectedClients.ContainsKey(id) && !client.IsConnectedClient,"transport connection ended");
        Check(!PlayerInventory.All.Contains(inventory) && !Health.All.Contains(health),"despawn removed inventory and health tracking");
        Check(health.GetType().GetField("OnDiedServer",Private).GetValue(health)==null,"despawn removed death subscription");
        Check(inventory.Status.OnValueChanged.GetInvocationList().Length==1,"despawn removed game status subscription");
        Check(bootstrap.RequestFor(id)==null && bootstrap.SlotFor(id)==-1 && !bootstrap.ClientReady(id),"requests slots and readiness cleaned");
        Check(activity.Phase==0 && !((IDictionary)Get(camp,"activities")).Contains(id),"activity cancelled and removed");
        var sender=Get(camp,"snapshotSender");
        Check(!((IDictionary)Get(sender,"outboxes")).Contains(id),"outgoing snapshot state removed");
        Check(!((IDictionary)Get(camp,"nextInput")).Contains(id) && !((IDictionary)Get(camp,"testProfiles")).Contains(id),"input and sandbox records absent");
        Check(LootPickup.All.Count==lootCount,"disconnect created no extra loot");
        Check(Record()==newRecord && CampRepository.Refresh(session,"new-room"),"new expedition and owner preserved");
        string beforeLate=Record(); Invoke(bootstrap,"Disconnected",id); Invoke(bootstrap,"Disconnected",id);
        Check(Record()==beforeLate,"repeated late callbacks preserve complete new record");
        Check(CampRepository.Get(session).ExpeditionState==2,"new expedition remains committed");
        client.Shutdown(); UnityEngine.Object.Destroy(client.gameObject); scenario++; stage=3;
    }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException("scenario "+scenario+" stage "+stage);
            if(!string.IsNullOrEmpty(report.Error)) throw new InvalidOperationException(report.Error);
            if(stage==0) { Connect(); stage=1; }
            else if(stage==1 && client.IsConnectedClient && server.ConnectedClients.Count==1) { PrepareParticipant(); Trigger(); stage=2; }
            else if(stage==2 && callbacks>0 && !client.IsConnectedClient) Verify();
            else if(stage==3 && client==null) { if(scenario<3) stage=0; else Finish(null); }
        }
        catch(Exception error) { Finish(error); }
    }
    static void Finish(Exception error)
    {
        if(finishing) return; finishing=true; EditorApplication.update-=Tick;
        try { if(client!=null) client.Shutdown(); if(server!=null) server.Shutdown(); CampRepository.Close(); }
        catch(Exception cleanup) { error=error??cleanup; }
        Application.logMessageReceived-=Log;
        report.Success=error==null && string.IsNullOrEmpty(report.Error); report.Error=error?.ToString()??report.Error;
        File.WriteAllText(Path.Combine(output,"t2c-result.json"),JsonUtility.ToJson(report,true));
        Debug.Log("POST_AUDIT_T2C_"+(report.Success?"OK":"FAILED"));
        SessionState.SetBool(Key+"Active",false); SessionState.SetBool(Key+"Exit",true); SessionState.SetInt(Key+"Code",report.Success?0:1);
        EditorApplication.ExitPlaymode();
    }
}
