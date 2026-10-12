using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using CyberExtraction.Camp;
using CyberExtraction.Items;
using CyberExtraction.Match;
using CyberExtraction.Players;
using CyberExtraction.Combat;
using CyberExtraction.UI;
namespace CyberExtraction.Diagnostics
{
    public class SkillsSmokeServer : MonoBehaviour
    {
        readonly HashSet<string> seeded=new HashSet<string>();
        float stopAt;
        bool completed;
        void Update()
        {
            foreach(var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var join=NetworkBootstrap.Instance.RequestFor(client.ClientId);
                if(join==null || !join.CampOnly || !seeded.Add(join.Session)) continue;
                CampRepository.Edit(join.Session,p=> { p.Deposit(20,10,3); p.Pack.Add(new ItemStack(ItemId.FishCommon,1)); return 0; });
                // Client explicitly requests another snapshot after this test-only fixture is seeded.
                NetworkBootstrap.Instance.SendCampSnapshot(client.ClientId);
            }
            if(completed && Time.realtimeSinceStartup>stopAt && NetworkManager.Singleton.ConnectedClientsIds.Count==0) Application.Quit(0);
        }
        IEnumerator Start()
        {
            while(MatchManager.Instance==null || MatchManager.Instance.CurrentState!=MatchState.Running) yield return null;
            var mm=MatchManager.Instance; var player=NetworkManager.Singleton.ConnectedClientsList[0].PlayerObject; var inv=player.GetComponent<PlayerInventory>(); var hp=player.GetComponent<Health>();
            // This fixture isolates the camp/network transaction from competitive AI damage.
            foreach(var participant in PlayerInventory.All)
            { var bot=participant.GetComponent<CyberExtraction.Bots.BotController>(); if(bot!=null) { bot.enabled=false; participant.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero; } }
            yield return new WaitForSecondsRealtime(5);
            var chestObject=Instantiate(mm.lootPrefab,player.transform.position+Vector3.right,Quaternion.identity);
            var chest=chestObject.GetComponent<LootPickup>(); chest.initialChest=true; chest.initialRarity=2; chest.initialQuantity=0;
            chestObject.GetComponent<NetworkObject>().Spawn();
            while(inv.PistolRounds>10) yield return null;
            if(hp.Maximum.Value!=125 || inv.Bandages.Value!=3) throw new Exception("SKILLS SERVER equipment/buff");
            hp.ServerTakeDamage(40); if(!inv.ServerUseBandage() || hp.Current.Value!=110) throw new Exception("SKILLS SERVER heal");
            inv.BandageReadyAt.Value=0; if(!inv.ServerUseBandage() || hp.Current.Value!=125) throw new Exception("SKILLS SERVER max heal");
            mm.ServerParticipantExtracted(inv); mm.ServerParticipantExtracted(inv);
            var join=NetworkBootstrap.Instance.RequestFor(inv.OwnerClientId); var profile=CampRepository.Get(join.Session);
            if(profile.Pack.Count(ItemId.Pistol)!=1 || profile.Pack.Count(ItemId.Bandage)!=1 || profile.ExpeditionState!=0) throw new Exception("SKILLS SERVER extraction accounting");
            Debug.Log("SKILLS SMOKE SERVER PASS: server authority, heal, extraction exactly once"); completed=true; stopAt=Time.realtimeSinceStartup+15;
        }
    }
}
