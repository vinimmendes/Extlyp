using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CyberExtraction.Camp;
using CyberExtraction.Combat;
using CyberExtraction.Items;
using CyberExtraction.Match;
using CyberExtraction.Players;
using CyberExtraction.UI;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor-only probe copied into the existing isolated worker; no production hooks.
[InitializeOnLoad]
public static class PostAuditT3Checks
{
    [Serializable] sealed class Report { public bool Success; public string Error; public List<string> Checks=new List<string>(); public List<string> ExpectedWarnings=new List<string>(); }
    const string Key="PostAuditT3";
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static Report report;
    static string output,cluster,session,expedition,bankedProfile;
    static NetworkManager manager;
    static NetworkBootstrap bootstrap;
    static MatchManager match;
    static PlayerInventory inv;
    static Health health;
    static int scenario,stage,lootCount,failures,warningsBefore;
    static float retryAfter;
    static double deadline;
    static bool finishing,partB,partC;
    static PostAuditT3Checks() { EditorApplication.playModeStateChanged+=Mode; }
    static object Get(object target,string field)=>target.GetType().GetField(field,Hidden).GetValue(target);
    static object Call(object target,string method,params object[] args)
    { try { return target.GetType().GetMethod(method,Hidden).Invoke(target,args); } catch(TargetInvocationException e) { throw e.InnerException??e; } }
    static void Check(bool value,string message) { if(!value) throw new InvalidOperationException(message); report.Checks.Add("case "+scenario+": "+message); }
    static NetworkObject SpawnTemplate(string name)=>UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AuditT2C/"+name+".prefab")).GetComponent<NetworkObject>();
    static string Profile()=>JsonUtility.ToJson(CampRepository.Get(session));
    public static void Run()
    {
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Batch Editor only");
        var args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,"-t3-output"); if(i<0 || i+1>=args.Length) throw new ArgumentException("-t3-output");
        if(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AuditT2C/Participant.prefab")==null) throw new InvalidOperationException("Prepared isolated T2c fixtures required");
        SessionState.SetString(Key+"Output",args[i+1]); SessionState.SetBool(Key+"Active",true); SessionState.SetBool(Key+"Exit",false);
        SessionState.SetBool(Key+"PartB",Array.IndexOf(args,"-t3b")>=0);
        bool c=Array.IndexOf(args,"-t3c1")>=0; SessionState.SetBool(Key+"PartC",c);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); EditorApplication.EnterPlaymode();
    }
    static void Mode(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key+"Exit",false))
        { SessionState.SetBool(Key+"Exit",false); EditorApplication.Exit(SessionState.GetInt(Key+"Code",1)); return; }
        if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key+"Active",false)) return;
        output=SessionState.GetString(Key+"Output",""); report=new Report(); deadline=EditorApplication.timeSinceStartup+60;
        partB=SessionState.GetBool(Key+"PartB",false); partC=SessionState.GetBool(Key+"PartC",false); scenario=partC?6:partB?2:0;
        Application.logMessageReceived+=Log;
        try { Setup(); EditorApplication.update+=Tick; } catch(Exception e) { Finish(e); }
    }
    static void Log(string text,string trace,LogType type)
    {
        if(type==LogType.Warning && text.StartsWith("Extracao salva; estado sera publicado novamente: T3_STATUS_FAULT")) report.ExpectedWarnings.Add(text);
        if(type==LogType.Warning && partB && (text.StartsWith("Extracao salva; progresso sera atualizado novamente: T3_PROGRESS_FAULT") ||
            ((scenario==3 || scenario==5) && text.StartsWith("Extracao salva; acampamento sera sincronizado novamente:")))) report.ExpectedWarnings.Add(text);
        if(!finishing && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert)) report.Error=(report.Error??"")+text+"\n";
    }
    static void Setup()
    {
        Directory.CreateDirectory(output); cluster=(partC?"T3c1-":partB?"T3b-":"T3a-")+Guid.NewGuid().ToString("N"); CampRepository.EnableShared(cluster,Path.Combine(output,"store"));
        var go=new GameObject("T3 host"); var transport=go.AddComponent<UnityTransport>(); manager=go.AddComponent<NetworkManager>(); manager.SetSingleton();
        ushort port;
        using(var socket=new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback,0))) port=(ushort)((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
        transport.SetConnectionData("127.0.0.1",port,"127.0.0.1");
        manager.NetworkConfig=new NetworkConfig { NetworkTransport=transport,EnableSceneManagement=false,ConnectionApproval=false };
        bootstrap=go.AddComponent<NetworkBootstrap>(); bootstrap.enabled=false;
        typeof(NetworkBootstrap).GetField("manager",Hidden).SetValue(bootstrap,manager); typeof(NetworkBootstrap).GetProperty("Instance").SetValue(null,bootstrap);
        Check(manager.StartHost(),"native NGO host started");
        var mm=SpawnTemplate("Match"); match=mm.GetComponent<MatchManager>(); mm.Spawn(); match.State.Value=(byte)MatchState.Running;
        match.lootPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AuditT2C/Loot.prefab");
        match.ServerDropStack(Vector2.zero,new ItemStack(ItemId.Wood,1)); lootCount=LootPickup.All.Count;
        Check(lootCount==1,"positive control confirms real drop path available");
    }
    static void Begin()
    {
        if(partC) { BeginCombat(); return; }
        failures=0; warningsBefore=report.ExpectedWarnings.Count;
        session=Guid.NewGuid().ToString("N"); string owner=(string)Call(bootstrap,"LeaseOwner",0UL);
        Check(CampRepository.Claim(session,owner),"session acquired in isolated durable store");
        ((IDictionary)Get(bootstrap,"requests"))[0UL]=new CampJoinRequest {Session=session};
        var obj=SpawnTemplate("Participant"); inv=obj.GetComponent<PlayerInventory>(); health=obj.GetComponent<Health>(); obj.SpawnAsPlayerObject(0);
        inv.ServerBeginExpedition(); inv.ServerCommitExpedition(); expedition=(string)Get(inv,"expeditionId");
        inv.ServerSetGrid(new InventoryGrid()); Check(inv.ServerAddItem(ItemId.Wood,3),"participant receives expedition loot");
        CampRepository.GrantExperience(new[]{new ExperienceGrant{Session=session,Expedition=expedition,Key="earned",Amount=20}});
        Check(CampRepository.Get(session).ExpeditionExperience==20,"pending expedition XP prepared");
        if(scenario==1)
        {
            inv.Status.OnValueChanged+=(previous,current)=>
            {
                if(current!=(byte)PlayerStatus.Extracted || failures!=0) return;
                failures++; inv.Status.Value=(byte)PlayerStatus.Alive;
                throw new InvalidOperationException("T3_STATUS_FAULT");
            };
        }
        else if(scenario==2 || scenario==4)
        {
            inv.ConfirmedExperience.OnValueChanged+=(previous,current)=>
            {
                if(failures!=0) return;
                failures++; throw new InvalidOperationException("T3_PROGRESS_FAULT");
            };
        }
        else if(scenario==3 || scenario==5)
        {
            // A missing session during sync makes its real repository lookup throw.
            // Progress uses the separate captured progressionSession and can still publish.
            inv.Status.OnValueChanged+=(previous,current)=>
            {
                if(current!=(byte)PlayerStatus.Extracted || failures!=0) return;
                failures++; bootstrap.RequestFor(0).Session=null;
            };
        }
        Check(inv.ServerBankExtraction(),"bank reports success despite publication outcome");
        var p=CampRepository.Get(session);
        Check(p.ExpeditionState==0 && p.ExpeditionId!=expedition && p.ExpeditionExperience==0,"escrow settled after bank");
        Check(p.Pack.Count(ItemId.Wood)==3 && p.Character.Experience==24 && p.LastExperienceAward==24 && p.LastExperienceExtracted,"three wood and 24 XP (20 plus extraction bonus) credited exactly once");
        bankedProfile=Profile();
        Check((bool)Get(inv,"extractionBanked") && (bool)Get(inv,"progressionFinished") && !inv.IsAlive,"terminal server marker blocks active participant");
        Check(!(bool)typeof(PlayerInventory).GetProperty("CanAct",Hidden).GetValue(inv) && !inv.ServerAddItem(ItemId.Wood,1),"actions and pickup rejected after bank");
        inv.ServerAwardObjective("late",20);
        inv.ServerLoseExpedition(); inv.ServerRollbackExpedition(); inv.ServerCommitExpedition();
        Check(Profile()==bankedProfile,"late XP and expedition finalizers cannot alter banked profile");
        for(int i=0;i<3;i++) Check(inv.ServerBankExtraction(),"repeated bank returns terminal success");
        Check(Profile()==bankedProfile,"repeated bank does not repeat inventory XP or revision effects");
        if(scenario==1)
            Check(failures==1 && report.ExpectedWarnings.Count==1 && inv.CurrentStatus==PlayerStatus.Alive && !(bool)Get(inv,"extractionStatusPublished"),"status failure observed while bank remains terminal");
        else Check(inv.CurrentStatus==PlayerStatus.Extracted && (bool)Get(inv,"extractionStatusPublished"),"extraction status published independently");
        if(partB)
        {
            bool progressFailed=scenario==2 || scenario==4;
            Check(failures==1 && report.ExpectedWarnings.Count==warningsBefore+1,"one intended publication failure observed");
            Check((bool)Get(inv,"extractionProgressPublished")==!progressFailed && (bool)Get(inv,"extractionCampPublished")==progressFailed,
                "only failed publication remains pending; independent publication succeeds");
            bootstrap.RequestFor(0).Session=session; // Repair fixture before retry or disconnect.
        }
        health.ServerTakeDamage(10000);
        Check(health.IsDead && LootPickup.All.Count==lootCount && Profile()==bankedProfile,"lethal damage after bank creates no drops or repeated rewards");
        if(scenario>=4)
        {
            Check(!(bool)Get(inv,"extractionProgressPublished") || !(bool)Get(inv,"extractionCampPublished"),"participant removed while publication is still pending");
            CleanupParticipant();
        }
        else { retryAfter=Time.unscaledTime+2.2f; stage=1; }
    }
    static void BeginCombat()
    {
        bool sandbox=scenario==7; match.QuickTest.Value=sandbox;
        session=Guid.NewGuid().ToString("N"); string owner=(string)Call(bootstrap,"LeaseOwner",0UL);
        Check(CampRepository.Claim(session,owner),"isolated session acquired");
        ((IDictionary)Get(bootstrap,"requests"))[0UL]=new CampJoinRequest {Session=session};
        string initialProfile=Profile();
        var obj=SpawnTemplate("Participant");
        // Components are added before spawn; all native Awake/OnNetworkSpawn paths run.
        obj.gameObject.AddComponent<NetworkTransform>();
        var shooter=obj.gameObject.AddComponent<PlayerShooter>(); shooter.enabled=false;
        var movement=obj.gameObject.AddComponent<PlayerController>(); movement.enabled=false;
        inv=obj.GetComponent<PlayerInventory>(); health=obj.GetComponent<Health>(); obj.SpawnAsPlayerObject(0);
        inv.ServerBeginExpedition(); inv.ServerCommitExpedition(); expedition=(string)Get(inv,"expeditionId");
        inv.ServerSetGrid(new InventoryGrid());
        Check(inv.ServerAddItem(ItemId.Wood,3) && inv.ServerAddItem(ItemId.PistolAmmo,12),"loot and reload reserve available before extraction");
        if(!sandbox) CampRepository.GrantExperience(new[]{new ExperienceGrant{Session=session,Expedition=expedition,Key="earned",Amount=20}});
        Drive(movement);
        Check(obj.GetComponent<Rigidbody2D>().linearVelocity.sqrMagnitude>0 && movement.Sprinting.Value,"positive control: native movement and sprint active");
        int rounds=shooter.Magazine; uint shots=shooter.ShotSequence.Value;
        shooter.ServerFire(0);
        Check(shooter.Magazine==rounds-1 && shooter.ShotSequence.Value==shots+1,"positive control: native shot consumes round and advances sequence");
        shooter.ServerReload(); Check(shooter.Reloading,"positive control: native reload can begin"); shooter.ServerCancelReload();
        Check(inv.ServerBankExtraction() && !inv.IsAlive && (bool)Get(inv,"extractionBanked"),"extraction establishes terminal marker");
        bankedProfile=Profile();
        if(sandbox) Check(bankedProfile==initialProfile && (string)Get(inv,"progressionSession")==null,"Sandbox extraction leaves real profile unchanged");
        else Check(CampRepository.Get(session).Pack.Count(ItemId.Wood)==3 && CampRepository.Get(session).Character.Experience==24,"normal extraction stores loot and XP");
        rounds=shooter.Magazine; shots=shooter.ShotSequence.Value;
        typeof(PlayerShooter).GetField("nextShotAt",Hidden).SetValue(shooter,0d);
        shooter.ServerFire(0); shooter.ServerReload();
        Check(shooter.Magazine==rounds && shooter.ShotSequence.Value==shots && !shooter.Reloading,"terminal participant cannot fire or reload despite valid weapon and reserve");
        Drive(movement);
        Check(obj.GetComponent<Rigidbody2D>().linearVelocity==Vector2.zero && !movement.Sprinting.Value && !movement.CanSprint,"fresh movement/sprint input is blocked after extraction");
        float stamina=movement.Stamina.Value;
        Check(!movement.ServerSpendStamina(1) && movement.Stamina.Value==stamina,"terminal participant cannot spend stamina");
        Check(!inv.ServerAddItem(ItemId.Wood,1),"terminal participant cannot collect loot");
        for(int i=0;i<3;i++) Check(inv.ServerBankExtraction(),"repeated terminal extraction returns success");
        Check(Profile()==bankedProfile,"repeated extraction leaves item and XP profile unchanged");
        health.ServerTakeDamage(10000);
        Check(health.IsDead && LootPickup.All.Count==lootCount && Profile()==bankedProfile,"lethal damage after extraction does not drop or duplicate rewards");
        CleanupParticipant();
    }
    static void Drive(PlayerController movement)
    {
        typeof(PlayerController).GetField("moveInput",Hidden).SetValue(movement,Vector2.right);
        typeof(PlayerController).GetField("sprintHeld",Hidden).SetValue(movement,true);
        typeof(PlayerController).GetField("nextInputSend",Hidden).SetValue(movement,0f);
        Call(movement,"FixedUpdate");
    }
    static void AfterRetry()
    {
        Check(inv.CurrentStatus==PlayerStatus.Extracted && (bool)Get(inv,"extractionStatusPublished") && (bool)Get(inv,"extractionProgressPublished") && (bool)Get(inv,"extractionCampPublished"),"automatic publication retry completes all pending flags");
        Check(Profile()==bankedProfile,"publication retry leaves durable item and XP state unchanged");
        if(partB) Check(inv.ConfirmedExperience.Value==24 && inv.PendingExperience.Value==0 && inv.LastConfirmedExperience.Value==24 && CampSession.Progress.Character.Experience==24,
            "recovered progress and host camp snapshot contain banked XP");
        inv.ServerSetStatus(PlayerStatus.Alive); inv.ServerSetStatus(PlayerStatus.Dead); inv.ServerSetStatus(PlayerStatus.TimeUp);
        Check(inv.CurrentStatus==PlayerStatus.Extracted && !inv.IsAlive,"status setters cannot reactivate terminal extraction");
        CleanupParticipant();
    }
    static void CleanupParticipant()
    {
        inv.NetworkObject.Despawn();
        Check(!PlayerInventory.All.Contains(inv) && LootPickup.All.Count==lootCount,"native despawn cleans participant without drops");
        Call(bootstrap,"Disconnected",0UL);
        Check(Profile()==bankedProfile && bootstrap.RequestFor(0)==null,"disconnect callback releases request without changing banked profile");
        CampRepository.Close(); CampRepository.EnableShared(cluster,Path.Combine(output,"store"));
        Check(Profile()==bankedProfile,"fresh repository loads identical banked profile from disk");
        scenario++; stage=2;
    }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException("stage "+stage);
            if(!string.IsNullOrEmpty(report.Error)) throw new InvalidOperationException(report.Error);
            if(stage==0) Begin();
            else if(stage==1 && Time.unscaledTime>=retryAfter) AfterRetry();
            else if(stage==2 && inv==null) { if(scenario<(partC?8:partB?6:2)) stage=0; else Finish(null); }
        }
        catch(Exception e) { Finish(e); }
    }
    static void Finish(Exception error)
    {
        if(finishing) return; finishing=true; EditorApplication.update-=Tick;
        try { if(manager!=null) manager.Shutdown(); CampRepository.Close(); } catch(Exception e) { error=error??e; }
        Application.logMessageReceived-=Log; report.Success=error==null && string.IsNullOrEmpty(report.Error); report.Error=error?.ToString()??report.Error;
        File.WriteAllText(Path.Combine(output,partC?"t3c1-result.json":partB?"t3b-result.json":"t3a-result.json"),JsonUtility.ToJson(report,true)); Debug.Log((partC?"POST_AUDIT_T3C1_":partB?"POST_AUDIT_T3B_":"POST_AUDIT_T3A_")+(report.Success?"OK":"FAILED"));
        SessionState.SetBool(Key+"Active",false); SessionState.SetBool(Key+"Exit",true); SessionState.SetInt(Key+"Code",report.Success?0:1); EditorApplication.ExitPlaymode();
    }
}
