using System;
using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Camp;
using CyberExtraction.Enemies;
using CyberExtraction.Players;

namespace CyberExtraction.Combat
{
    public partial class Health
    {
        sealed class Contribution { public PlayerInventory Player; public int Amount; public double At; }
        sealed class Share { public PlayerInventory Player; public long Damage,Remainder; public int Amount; }
        readonly List<Contribution> experienceDamage=new List<Contribution>();
        string experienceDeathId;
        bool experienceRewarded;
        void ResetExperienceTracking()
        { experienceDamage.Clear(); experienceDeathId="death:"+Guid.NewGuid().ToString("N"); experienceRewarded=false; }
        void TrackExperienceDamage(PlayerInventory source,int amount)
        {
            double now=Time.timeAsDouble;
            experienceDamage.RemoveAll(hit=>hit.At<now-15 || hit.Player==null);
            if(source!=null && source.ProgressionEligible && source.NetworkObject!=NetworkObject && amount>0)
                experienceDamage.Add(new Contribution { Player=source,Amount=amount,At=now });
        }
        void AwardDeathExperience(PlayerInventory lethalSource)
        {
            if(experienceRewarded) return;
            try
            {
                var participant=GetComponent<PlayerInventory>();
                if(participant!=null)
                {
                    if(NetworkObject.IsPlayerObject && participant.IsAlive && participant.ProgressionActive && lethalSource!=null && lethalSource.ProgressionEligible &&
                        lethalSource!=participant && !string.IsNullOrEmpty(participant.ProgressionSession))
                    {
                        CampRepository.QueuePvp(lethalSource.ProgressionSession,lethalSource.ProgressionExpedition,participant.ProgressionSession,participant.ProgressionExpedition,experienceDeathId);
                        experienceRewarded=true; // The server-owned queue survives this Health's despawn.
                        CampRepository.FlushPendingExperience();
                        lethalSource.ServerRefreshProgress();
                    }
                    experienceRewarded=true;
                    return;
                }
                var monster=GetComponent<Monster>(); var plant=GetComponent<PlantCreature>();
                int reward=monster!=null ? monster.ExperienceReward : plant!=null ? plant.CurrentKind==PlantKind.Mother ? 20 : 10 : GetComponent<Esporifero>()!=null ? 10 : 0;
                if(reward<=0) { experienceRewarded=true; return; }
                var shares=new List<Share>(); long total=0;
                foreach(var hit in experienceDamage)
                {
                    if(hit.At<Time.timeAsDouble-15 || hit.Player==null || !hit.Player.ProgressionEligible) continue;
                    var share=shares.Find(s=>s.Player==hit.Player);
                    if(share==null) { share=new Share { Player=hit.Player }; shares.Add(share); }
                    share.Damage+=hit.Amount; total+=hit.Amount;
                }
                if(total<=0) { experienceRewarded=true; return; }
                int remaining=reward;
                foreach(var share in shares)
                { share.Amount=(int)(reward*share.Damage/total); share.Remainder=reward*share.Damage%total; remaining-=share.Amount; }
                shares.Sort((a,b)=> { int order=b.Remainder.CompareTo(a.Remainder); return order!=0 ? order : a.Player.NetworkObjectId.CompareTo(b.Player.NetworkObjectId); });
                for(int i=0;i<remaining;i++) shares[i].Amount++;
                var grants=new List<ExperienceGrant>();
                foreach(var share in shares) if(share.Amount>0)
                    grants.Add(new ExperienceGrant { Session=share.Player.ProgressionSession,Expedition=share.Player.ProgressionExpedition,Key=experienceDeathId,Amount=share.Amount });
                CampRepository.QueueExperience(grants);
                experienceRewarded=true;
                CampRepository.FlushPendingExperience();
                foreach(var share in shares) share.Player.ServerRefreshProgress();
            }
            catch(Exception error) { Debug.LogError((experienceRewarded ? "XP enfileirado; persistencia/publicacao sera tentada novamente: " : "Nao foi possivel enfileirar a recompensa de XP: ")+error.Message); }
        }
    }
}
