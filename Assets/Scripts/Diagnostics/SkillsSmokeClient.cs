using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using CyberExtraction.Camp;
using CyberExtraction.Items;
using CyberExtraction.Match;
using CyberExtraction.Players;
using CyberExtraction.Combat;
using CyberExtraction.UI;
namespace CyberExtraction.Diagnostics
{
    public class SkillsSmokeClient : MonoBehaviour
    {
        float deadline;
        void Check(bool ok,string reason) { if(!ok) { Debug.LogError("SKILLS SMOKE FAIL: "+reason); Application.Quit(1); throw new Exception(reason); } }
        IEnumerator Wait(Func<bool> condition)
        { while(!condition()) { if(Time.realtimeSinceStartup>deadline) throw new TimeoutException("SKILLS SMOKE TIMEOUT"); yield return null; } }
        IEnumerator Act(CampAction action)
        {
            yield return Wait(()=>CampSession.Synchronized && !CampSession.Pending);
            long revision=CampSession.Progress.Revision;
            Check(CampSession.Act(action),"Enviar comando de acampamento");
            yield return Wait(()=>!CampSession.Pending);
            Check(CampSession.Progress.Revision==revision+1,"Comando aceito pelo servidor: "+CampSession.Message);
        }
        IEnumerator Start()
        {
            deadline=Time.realtimeSinceStartup+180;
            yield return Wait(()=>CampSession.Synchronized && CampSession.Progress.Wood>=20);
            yield return Act(new CampAction { Kind=CampActionKind.Repair });
            int fish=Array.FindIndex(CampSession.Progress.Pack.Slots,s=>!s.Empty && s.Item==ItemId.FishCommon);
            Check(NetworkBootstrap.Instance.SendCamp(new CampCommand { Kind=CampCommandKind.BeginCooking,Slot=fish }),"Iniciar cozimento");
            yield return Wait(()=>NetworkBootstrap.Instance.CampView.Activity==6 && !CampSession.Pending &&
                CookingGame.Quality((float)(NetworkManager.Singleton.ServerTime.Time-NetworkBootstrap.Instance.CampView.CookStartedAt))==3);
            NetworkBootstrap.Instance.SendCamp(new CampCommand { Kind=CampCommandKind.FinishCooking,CookingPressedAt=NetworkManager.Singleton.ServerTime.Time });
            yield return Wait(()=>NetworkBootstrap.Instance.CampView.Activity==0 && !CampSession.Pending);
            int meal=Array.FindIndex(CampSession.Progress.Pack.Slots,s=>!s.Empty && s.Item==ItemId.CookedFish);
            Check(meal>=0 && CampSession.Progress.Pack.Slots[meal].Quality==3,"Qualidade real do cozimento");
            yield return Act(new CampAction { Kind=CampActionKind.Eat,Source=meal });
            int bandage=Array.FindIndex(CampSession.Progress.Chest.Slots,s=>!s.Empty && s.Item==ItemId.Bandage);
            int empty=Array.FindIndex(CampSession.Progress.Pack.Slots,s=>s.Empty);
            yield return Act(new CampAction { Kind=CampActionKind.Transfer,SourceGrid=1,TargetGrid=0,Source=bandage,Target=empty });
            NetworkBootstrap.Instance.BeginExpedition("127.0.0.1");
            yield return Wait(()=>MatchManager.Instance!=null && MatchManager.Instance.CurrentState==MatchState.Running);
            var player=NetworkManager.Singleton.LocalClient.PlayerObject; var inv=player.GetComponent<PlayerInventory>();
            Check(player.GetComponent<Health>().Maximum.Value==125,"Buff equipado na expedicao");
            Vector3 before=player.transform.position; GameInput.SetFocus(true);
            var keyboard=Keyboard.current ?? InputSystem.AddDevice<Keyboard>(); InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));
            yield return new WaitForSecondsRealtime(.4f); InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return new WaitForSecondsRealtime(.3f);
            Check(player.transform.position.x>before.x+.1f,"Movimento atraves da rede");
            int rounds=inv.PistolRounds; player.GetComponent<PlayerShooter>().RequestFire(0);
            yield return Wait(()=>inv.PistolRounds<rounds);
            Debug.Log("SKILLS CLIENT MOVEMENT/FIRE PASS");
            yield return Wait(()=>LootPickup.NearestChest(player.transform.position)!=null);
            var chest=LootPickup.NearestChest(player.transform.position); InventoryWindow.Instance.Interact();
            yield return Wait(()=>chest.OpenState.Value==1); InventoryWindow.Instance.Close();
            yield return Wait(()=>chest.OpenState.Value==0);
            Check(!InventoryWindow.IsOpen,"Cancelamento nao reabre bau");
            player.GetComponent<PlayerShooter>().RequestFire(0);
            yield return Wait(()=>inv.CurrentStatus==PlayerStatus.Extracted);
            NetworkManager.Singleton.Shutdown(); yield return Wait(()=>!NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.ShutdownInProgress);
            NetworkBootstrap.Instance.BeginCamp("127.0.0.1");
            yield return Wait(()=>CampSession.Synchronized && NetworkBootstrap.Instance.CampConnection && !CampSession.Pending);
            Check(CampSession.Progress.PierRepaired && !CampSession.Progress.WellFed && CampSession.Progress.Pack.Count(ItemId.Pistol)==1,"Reconexao carrega progresso confirmado");
            Debug.Log("SKILLS SMOKE CLIENT PASS: signed camp, cooking, transfer, buff, movement, fire, extraction, reconnect");
            NetworkManager.Singleton.Shutdown(); yield return new WaitForSecondsRealtime(.5f); Application.Quit(0);
        }
        void Update() { if(deadline>0 && Time.realtimeSinceStartup>deadline) { Debug.LogError("SKILLS SMOKE CLIENT TIMEOUT"); Application.Quit(1); } }
    }
}
