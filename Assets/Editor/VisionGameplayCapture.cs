using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using CyberExtraction.Match;
using CyberExtraction.Players;
using CyberExtraction.UI;

// Explicit editor-only, local visual probe. Never included in player builds.
[InitializeOnLoad]
public static class VisionGameplayCapture
{
    const string Flag="VisionGameplayCapture.Pending";
    static VisionGameplayCapture()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Flag,false))
            {
                SessionState.SetBool(Flag,false);
                new GameObject("VisionGameplayCapture").AddComponent<VisionGameplayCaptureRunner>();
            }
        };
    }
    public static void Run()
    {
        Directory.CreateDirectory("Logs/VisionGameplay");
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
        view.position=new Rect(50,50,1280,800); view.Show(); view.Focus();
        SessionState.SetBool(Flag,true);
        EditorApplication.isPlaying=true;
    }
}

public sealed class VisionGameplayCaptureRunner : MonoBehaviour
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    IEnumerator Start()
    {
        yield return null; yield return null;
        var bootstrap=NetworkBootstrap.Instance;
        var nm=NetworkManager.Singleton;
        // This probe uses only a loopback host and temporary in-memory actors.
        // Bypass account admission solely inside this editor-only capture runner.
        var connected=(Action<ulong>)Delegate.CreateDelegate(typeof(Action<ulong>),bootstrap,
            typeof(NetworkBootstrap).GetMethod("Connected",Private));
        nm.OnClientConnectedCallback-=connected;
        nm.ConnectionApprovalCallback=(request,response)=>
        {
            response.Approved=true; response.CreatePlayerObject=true;
            response.Position=DungeonLayout.Rooms[20].Center;
            response.Rotation=Quaternion.identity;
        };
        nm.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1",19386,"127.0.0.1");
        MapBuilder.Configure(ExpeditionMap.Dungeon);
        if(!nm.StartHost()) throw new Exception("Visual capture host failed.");
        var go=Instantiate(bootstrap.matchManagerPrefab);
        var match=go.GetComponent<MatchManager>();
        go.GetComponent<NetworkObject>().Spawn();
        match.MapSelection.Value=(byte)ExpeditionMap.Dungeon;
        match.QuickTest.Value=true; match.QuickBots.Value=0;
        var ready=(HashSet<ulong>)typeof(NetworkBootstrap).GetField("readyClients",Private).GetValue(bootstrap);
        ready.Add(nm.LocalClientId);
        yield return new WaitForSeconds(2);
        if(match.CurrentState!=MatchState.Running)
            typeof(MatchManager).GetMethod("StartMatchCore",Private).Invoke(match,null);
        var player=nm.LocalClient.PlayerObject;
        var pc=player.GetComponent<PlayerController>();
        pc.enabled=false; // Deterministic stationary aim, no OS mouse dependency.
        player.transform.position=DungeonLayout.Rooms[20].Center;
        player.GetComponent<Rigidbody2D>().position=player.transform.position;
        player.transform.rotation=Quaternion.identity;
        player.GetComponent<Rigidbody2D>().rotation=0;
        foreach(var monster in CyberExtraction.Enemies.Monster.All)
            if(monster!=null) monster.enabled=false;
        yield return new WaitForSeconds(3);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/VisionGameplay/facing-east.png"));
        yield return new WaitForSeconds(1);
        player.transform.rotation=Quaternion.Euler(0,0,90);
        player.GetComponent<Rigidbody2D>().rotation=90;
        yield return new WaitForSeconds(1);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/VisionGameplay/facing-north.png"));
        yield return new WaitForSeconds(1);
        var shader=Resources.Load<Shader>("VisionSurface");
        if(ShaderUtil.ShaderHasError(shader)) throw new Exception("VisionSurface shader error.");
        File.WriteAllText("Logs/VisionGameplay/result.txt","Play Mode loopback host; actual Dungeon scene; east/north captures. No player build.");
        Debug.Log("VISION GAMEPLAY CAPTURE COMPLETE");
        EditorApplication.isPlaying=false;
    }
}
