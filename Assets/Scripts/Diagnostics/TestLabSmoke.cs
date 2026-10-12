using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using CyberExtraction.Camp;
using CyberExtraction.Combat;
using CyberExtraction.Items;
using CyberExtraction.Match;
using CyberExtraction.Players;
using CyberExtraction.UI;

namespace CyberExtraction.Diagnostics
{
    // Explicit -lab-smoke only. Real RPCs, native objects, isolated identity/data supplied by runner.
    public class TestLabSmoke : MonoBehaviour
    {
        double deadline; string before; int checks;
        UnityEngine.InputSystem.Keyboard testKeyboard;
        void Verify(bool ok,string reason)
        {
            if(!ok) { Debug.LogError("TEST LAB SMOKE FAIL: "+reason); Application.Quit(1); throw new Exception(reason); }
            checks++; Debug.Log("TEST LAB CHECK: "+reason);
        }
        IEnumerator Until(Func<bool> ready)
        {
            while(!ready()) { if(Time.realtimeSinceStartupAsDouble>=deadline) Verify(false,"Prazo do teste: "+(MatchManager.Instance!=null ? MatchManager.Instance.TestMessage.Value.ToString() : "sem sala")); yield return null; }
        }
        IEnumerator Command(TestAction action,int value=0)
        {
            var match=MatchManager.Instance; int revision=match.TestRevision.Value;
            match.RequestTest(action,value);
            yield return Until(()=>match.TestRevision.Value>revision);
            yield return new WaitForSecondsRealtime(.25f);
        }
        IEnumerator Key(UnityEngine.InputSystem.Key key)
        {
            var keyboard=testKeyboard ?? UnityEngine.InputSystem.Keyboard.current ?? UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(key));
            yield return null; yield return null;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
            yield return null; yield return null;
        }
        IEnumerator CaptureUi()
        {
            if(Application.isBatchMode) yield break;
            var args=Environment.GetCommandLineArgs(); int index=Array.IndexOf(args,"-lab-capture");
            if(index<0 || index+1>=args.Length) yield break;
            string folder=args[index+1]; System.IO.Directory.CreateDirectory(folder);
            // Test-only virtual input: capture windows need not own the user's keyboard focus.
            UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            testKeyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("TestLabKeyboard");
            GameInput.SetFocus(true); yield return new WaitForSecondsRealtime(.3f);
            for(int tab=0;tab<3;tab++)
            {
                if(tab>0) { yield return Key(UnityEngine.InputSystem.Key.RightArrow); yield return Key(UnityEngine.InputSystem.Key.Enter); }
                Verify(TestLabPanel.IsOpen && TestLabPanel.CurrentTab==tab,"Navegacao do painel por teclado: aba "+tab);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"panel-"+tab+"-"+Screen.width+"x"+Screen.height+".png"));
                yield return new WaitForSecondsRealtime(.2f);
            }
        }
        IEnumerator Start()
        {
            deadline=Time.realtimeSinceStartupAsDouble+150;
            Debug.Log("TEST LAB SMOKE START");
            yield return Until(()=>CampSession.Synchronized && !NetworkBootstrap.Instance.Searching && !CampSession.Pending);
            Debug.Log("TEST LAB CAMP READY");
            before=JsonUtility.ToJson(CampSession.Progress);
            NetworkBootstrap.Instance.BeginExpedition("127.0.0.1");
            yield return Until(()=>MatchManager.Instance!=null && MatchManager.Instance.CurrentState==MatchState.Running);
            var match=MatchManager.Instance; var player=NetworkManager.Singleton.LocalClient.PlayerObject;
            var inv=player.GetComponent<PlayerInventory>();
            Verify(match.Sandbox && match.BotCount.Value==0,"Modo laboratorio sem bots");
            yield return CaptureUi();
            yield return Command(TestAction.Invulnerable,1);
            yield return Command(TestAction.ClearEnemies);
            Verify(match.TestProtection.Value,"Protecao replicada");
            for(int i=0;i<MatchManager.TestWeapons.Length;i++)
            {
                yield return Command(TestAction.Weapon,i);
                Verify(inv.ActiveWeapon.Item==MatchManager.TestWeapons[i] && inv.WeaponRounds==inv.ActiveWeapon.Capacity && inv.Count(inv.ActiveWeapon.Ammo)==60,"Arma e reserva "+i);
            }
            yield return Command(TestAction.Bots,2); Verify(match.BotCount.Value==2,"Criar dois bots via RPC");
            yield return Command(TestAction.Bots,0); Verify(match.BotCount.Value==0,"Remover bots via RPC");
            yield return Command(TestAction.Teleport,0);
            for(int i=0;i<MatchManager.TestMonsters.Length;i++)
            {
                yield return Command(TestAction.Monster,i);
                Verify(match.TestMessage.Value.ToString().Contains("criado"),"Criacao de "+MatchManager.TestMonsters[i]);
                yield return Command(TestAction.ClearEnemies);
            }
            yield return Command(TestAction.Web); yield return Command(TestAction.Acid); yield return Command(TestAction.ClearEffects);
            yield return Command(TestAction.Item,(int)ItemId.FishCommon); Verify(inv.Count(ItemId.FishCommon)==5,"Item de teste no inventario");
            yield return Command(TestAction.ReturnToShelter); Verify(inv.IsAlive && player.GetComponent<Health>().Current.Value==100,"Retorno ao abrigo e vida");
            // Extraction uses native trigger/hold logic, then restore re-enables native collider.
            yield return Command(TestAction.Teleport,2);
            yield return Command(TestAction.ClearEnemies);
            var keyboard=UnityEngine.InputSystem.Keyboard.current ?? UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            GameInput.SetFocus(true);
            // Panel blocks controls; F2 closes it through the same UI action used by players.
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F2));
            yield return null;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
            yield return new WaitForSecondsRealtime(.2f);
            Verify(!TestLabPanel.IsOpen,"F2 fecha painel e libera entrada");
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.D));
            yield return new WaitForSecondsRealtime(.8f);
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
            yield return Until(()=>inv.CurrentStatus==PlayerStatus.Extracted);
            yield return Command(TestAction.ReturnToShelter);
            Verify(inv.IsAlive && player.GetComponent<Collider2D>().enabled,"Reviver depois da extracao reativa colisao");
            NetworkManager.Singleton.Shutdown(); yield return Until(()=>!NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.ShutdownInProgress);
            NetworkBootstrap.Instance.BeginCamp("127.0.0.1");
            yield return Until(()=>CampSession.Synchronized && NetworkBootstrap.Instance.CampConnection);
            Verify(JsonUtility.ToJson(CampSession.Progress)==before,"Progresso do acampamento permanece identico");
            Debug.Log("TEST LAB SMOKE PASS: "+checks+" assertions, weapons/14 enemy choices/bots/hazards/extraction/restore/camp isolation");
            NetworkManager.Singleton.Shutdown(); yield return new WaitForSecondsRealtime(.3f); Application.Quit(0);
        }
        void Update() { if(testKeyboard!=null) GameInput.SetFocus(true); if(deadline>0 && Time.realtimeSinceStartupAsDouble>deadline) { Debug.LogError("TEST LAB SMOKE TIMEOUT"); Application.Quit(1); } }
    }
}
