// Explicit local Play Mode probe. No account admission, repository or real saves.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using CyberExtraction.Items;
using CyberExtraction.Players;
using CyberExtraction.UI;
using CyberExtraction.Match;
using CyberExtraction.Combat;

[InitializeOnLoad]
public static class NewWeaponsGameplayProbe
{
    const string Flag="NewWeaponsGameplayProbe.Pending";
    static NewWeaponsGameplayProbe()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Flag,false))
            {
                SessionState.SetBool(Flag,false);
                new GameObject("NewWeaponsGameplayProbe").AddComponent<NewWeaponsGameplayProbeRunner>();
            }
        };
    }
    public static void Run()
    {
        Directory.CreateDirectory("Logs/new-weapons-test");
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
        view.position=new Rect(40,40,1280,800);view.Show();
        SessionState.SetBool(Flag,true);EditorApplication.isPlaying=true;
    }
}

public sealed class NewWeaponsGameplayProbeRunner:MonoBehaviour
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    readonly List<string> evidence=new List<string>();
    PlayerInventory inv,target;PlayerShooter shooter;PlayerController controller;
    float deadline;bool finished;
    string Dir=>Path.GetFullPath("Logs/new-weapons-test");
    void Check(bool value,string label)
    {
        evidence.Add((value ? "PASS " : "FAIL ")+label);
        File.WriteAllLines(Path.Combine(Dir,"unity-checks.txt"),evidence);
        Debug.Log(evidence[evidence.Count-1]);
        if(!value)throw new Exception(label);
    }
    void Update()
    {
        if(finished)return;
        if(deadline>0 && Time.realtimeSinceStartup>deadline)Finish(false,"Timeout");
        GameInput.SetFocus(true);
    }
    void OnEnable()=>Application.logMessageReceived+=Log;
    void OnDisable()=>Application.logMessageReceived-=Log;
    void Log(string message,string stack,LogType type)
    {
        if(!finished && (type==LogType.Exception || type==LogType.Error))Finish(false,message+"\n"+stack);
    }
    void Finish(bool passed,string reason)
    {
        if(finished)return;finished=true;
        File.WriteAllText(Path.Combine(Dir,"unity-result.txt"),(passed ? "PASS" : "FAIL")+"\n"+reason+"\n"+string.Join("\n",evidence));
        if(inv!=null && inv.IsSpawned)inv.ServerSetStatus(PlayerStatus.Extracted);
        if(target!=null && target.IsSpawned)target.ServerSetStatus(PlayerStatus.Extracted);
        EditorApplication.delayCall+=()=>{EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(passed ? 0 : 1);};
    }
    IEnumerator Until(Func<bool> ready)
    {
        float until=Time.realtimeSinceStartup+8;
        while(!ready())
        {
            if(finished)yield break;
            if(Time.realtimeSinceStartup>until){Check(false,"Wait: "+string.Join(";",evidence));yield break;}
            yield return null;
        }
    }
    UguiHudPointer Pointer(HudControl control)
    {
        foreach(var p in FindObjectsByType<UguiHudPointer>(FindObjectsSortMode.None))if(p.Control==control && p.gameObject.activeInHierarchy)return p;
        throw new Exception("HUD pointer absent: "+control);
    }
    IEnumerator PressFire(float seconds=.12f)
    {
        var p=Pointer(HudControl.Fire);var point=HudLayout.ScreenBounds(HudControl.Fire).center;
        var data=new PointerEventData(EventSystem.current){pointerId=42,position=point,button=PointerEventData.InputButton.Left};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        Check(hits.Count>0 && hits.Exists(h=>h.gameObject.GetComponentInParent<UguiHudPointer>()==p),"fire HUD receives raycast at displayed bounds");
        ExecuteEvents.ExecuteHierarchy(p.gameObject,data,ExecuteEvents.pointerDownHandler);
        yield return null;
        Check(TouchControls.Instance.FireHeld,"pointer down reaches TouchControls.FireHeld");
        yield return new WaitForSeconds(seconds);
        ExecuteEvents.ExecuteHierarchy(p.gameObject,data,ExecuteEvents.pointerUpHandler);
        Check(!TouchControls.Instance.FireHeld,"pointer up clears attack input");
    }
    void Equip(ItemId id,int rounds=0,int bolts=5)
    {
        shooter.ServerCancelReload();shooter.ServerCancelMelee();
        var pack=new InventoryGrid(ItemCatalog.BackpackSlots);
        if(bolts>0)pack.Add(new ItemStack(ItemId.CrossbowBolt,bolts));
        inv.ServerSetGrid(pack);
        var loadout=new EquipmentLoadout();loadout.Slots[EquipmentLoadout.WeaponA]=new ItemStack(id,1,0,rounds);
        inv.ServerSetLoadout(loadout);controller.ServerResetStamina();
        typeof(PlayerController).GetField("facing",Private).SetValue(controller,0f);
    }
    IEnumerator Start()
    {
        deadline=Time.realtimeSinceStartup+100;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        yield return null;yield return null;
        var bootstrap=NetworkBootstrap.Instance;var nm=NetworkManager.Singleton;
        Check(!nm.IsListening,"isolated editor: no preexisting session");
        var connected=(Action<ulong>)Delegate.CreateDelegate(typeof(Action<ulong>),bootstrap,typeof(NetworkBootstrap).GetMethod("Connected",Private));
        nm.OnClientConnectedCallback-=connected;
        nm.ConnectionApprovalCallback=(request,response)=>{response.Approved=true;response.CreatePlayerObject=true;response.Position=new Vector3(1000,1000);response.Rotation=Quaternion.identity;};
        nm.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1",19387,"127.0.0.1");
        Check(nm.StartHost(),"loopback host started");
        var go=Instantiate(bootstrap.matchManagerPrefab);var match=go.GetComponent<MatchManager>();
        go.GetComponent<NetworkObject>().Spawn();match.State.Value=(byte)MatchState.Running;match.enabled=false;
        yield return Until(()=>nm.LocalClient.PlayerObject!=null && UguiGameHud.Active);
        inv=nm.LocalClient.PlayerObject.GetComponent<PlayerInventory>();shooter=inv.GetComponent<PlayerShooter>();controller=inv.GetComponent<PlayerController>();
        inv.transform.position=new Vector3(1000,1000);inv.GetComponent<Rigidbody2D>().position=inv.transform.position;
        inv.transform.rotation=Quaternion.identity;inv.GetComponent<Rigidbody2D>().rotation=0;
        var targetGo=Instantiate(bootstrap.playerPrefab,new Vector3(1003,1000),Quaternion.identity);
        targetGo.GetComponent<PlayerController>().enabled=false;
        targetGo.GetComponent<NetworkObject>().Spawn();target=targetGo.GetComponent<PlayerInventory>();
        var health=target.GetComponent<Health>();health.Maximum.Value=health.Current.Value=500;
        yield return new WaitForSeconds(.5f);
        foreach(var follow in FindObjectsByType<CameraFollow>(FindObjectsSortMode.None))follow.enabled=false;
        Camera.main.transform.position=new Vector3(1001,1000,-10);Camera.main.orthographicSize=6;
        Check(!UiLayout.BlocksGameplay,"HUD permits gameplay input");
        // Hold a real HUD aim drag east; an unfocused desktop mouse must not
        // steer the fixture between presses on the simulated touch fire button.
        var aim=Pointer(HudControl.Aim);var aimPoint=HudLayout.ScreenBounds(HudControl.Aim).center;
        var aimData=new PointerEventData(EventSystem.current){pointerId=43,position=aimPoint,button=PointerEventData.InputButton.Left};
        ExecuteEvents.ExecuteHierarchy(aim.gameObject,aimData,ExecuteEvents.pointerDownHandler);
        aimData.position=aimPoint+Vector2.right*100;
        ExecuteEvents.ExecuteHierarchy(aim.gameObject,aimData,ExecuteEvents.dragHandler);
        yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
        Check(TouchControls.Instance.AimHeld && Mathf.Abs(Mathf.DeltaAngle(controller.ServerFacing,0))<1,"HUD aim drag points east");
        var starter=InventoryGrid.Starter();
        Check(starter.Count(ItemId.ImprovisedHandCrossbow)==1 && starter.Count(ItemId.RustySword)==1 && starter.Count(ItemId.CrossbowBolt)==5,"starter kit: two weapons and five bolts");
        Equip(ItemId.ImprovisedHandCrossbow);
        yield return new WaitForSeconds(.3f);
        Check(Pointer(HudControl.Fire).GetComponent<UnityEngine.UI.Button>().interactable,"empty crossbow with reserve keeps fire button visually enabled");
        yield return PressFire();
        Check(shooter.Reloading && shooter.Magazine==0,"empty crossbow fire button starts reload without shot");
        yield return Until(()=>!shooter.Reloading);
        Check(shooter.Magazine==1 && shooter.Reserve==4,"reload moves exactly one bolt from reserve");
        int before=health.Current.Value;uint shots=shooter.ShotSequence.Value;
        yield return PressFire();yield return new WaitForSeconds(.1f);
        Check(shooter.ShotSequence.Value==shots+1 && shooter.Magazine==0,"fire button sends RPC and consumes one bolt");
        File.WriteAllText(Path.Combine(Dir,"shot-state.txt"),"player="+inv.transform.position+" target="+target.transform.position+" muzzle="+shooter.MuzzlePosition+" angle="+controller.ServerFacing+" health="+before+"->"+health.Current.Value);
        Check(health.Current.Value<before,"crossbow hits target through real physics");
        shooter.RequestReload();yield return Until(()=>!shooter.Reloading);
        yield return new WaitForSeconds(1.1f);
        shots=shooter.ShotSequence.Value;
        yield return PressFire(.25f);
        Check(shooter.ShotSequence.Value==shots+1,"holding button cannot duplicate crossbow shot");
        shooter.ServerCancelReload();yield return new WaitForSeconds(1.1f);
        Equip(ItemId.ImprovisedHandCrossbow,0,0);yield return PressFire();
        Check(!shooter.Reloading && shooter.Magazine==0,"no ammunition: no phantom reload or shot");
        Equip(ItemId.RustySword);
        target.transform.position=inv.transform.position+Vector3.right;target.GetComponent<Rigidbody2D>().position=target.transform.position;
        yield return new WaitForSeconds(.6f);before=health.Current.Value;float stamina=controller.Stamina.Value;
        yield return PressFire();
        Check(shooter.MeleeAttacking && controller.Stamina.Value<stamina,"attack button starts sword windup and spends stamina");
        yield return new WaitForSeconds(.35f);
        Check(health.Current.Value==before-16,"rusty sword applies damage once at impact");
        yield return new WaitForSeconds(.6f);before=health.Current.Value;
        target.transform.position=inv.transform.position+Vector3.right*2;target.GetComponent<Rigidbody2D>().position=target.transform.position;
        yield return PressFire();yield return new WaitForSeconds(.8f);
        Check(health.Current.Value==before,"sword rejects target outside range");
        controller.Stamina.Value=0;yield return PressFire();
        Check(!shooter.MeleeAttacking,"exhaustion prevents sword attack");
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(Dir,"sword-hud.png"));
        yield return new WaitForSeconds(.3f);
        Finish(true,"Editor Play Mode: uGUI raycast/pointer -> TouchControls -> PlayerController -> host RPC -> ammunition/stamina/physics. Not a separate-client or physical-phone test.");
    }
}
