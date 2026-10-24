using System;
using System.Collections.Generic;

namespace CyberExtraction.Camp
{
    public static partial class CampRepository
    {
        sealed class RewardOwner { public string Session,Owner,Token; }
        sealed class PendingReward
        {
            public ExperienceGrant[] Grants;
            public RewardOwner[] Owners;
            public string Victim,VictimExpedition;
            public bool Rejected;
        }
        static readonly List<PendingReward> pendingRewards=new List<PendingReward>();
        public static long ExperienceGeneration { get; private set; }
        static RewardOwner CaptureRewardOwner(string session)
        {
            if(string.IsNullOrEmpty(session) || !heldLeases.TryGetValue(session,out var lease)) throw new CampLeaseLostException();
            return new RewardOwner { Session=session,Owner=lease.Owner,Token=lease.Token };
        }
        public static void QueueExperience(IReadOnlyList<ExperienceGrant> grants)
        {
            if(grants==null || grants.Count==0) return;
            var reward=new PendingReward { Grants=new ExperienceGrant[grants.Count],Owners=new RewardOwner[grants.Count] };
            for(int i=0;i<grants.Count;i++)
            {
                var grant=grants[i];
                if(grant==null || grant.Amount<=0 || string.IsNullOrEmpty(grant.Expedition) || string.IsNullOrEmpty(grant.Key) || grant.Key.Length>96)
                    throw new InvalidOperationException("Evento de XP invalido.");
                reward.Owners[i]=CaptureRewardOwner(grant.Session);
                reward.Grants[i]=new ExperienceGrant { Session=grant.Session,Expedition=grant.Expedition,Key=grant.Key,Amount=grant.Amount,Exploration=grant.Exploration };
            }
            // Repeated objective/exploration notifications do not grow the pending queue.
            if(pendingRewards.Exists(old=>SameReward(old,reward))) return;
            pendingRewards.Add(reward);
        }
        static bool SameReward(PendingReward a,PendingReward b)
        {
            if(a.Victim!=b.Victim || a.Grants.Length!=b.Grants.Length) return false;
            for(int i=0;i<a.Grants.Length;i++)
                if(a.Grants[i].Session!=b.Grants[i].Session || a.Grants[i].Expedition!=b.Grants[i].Expedition || a.Grants[i].Key!=b.Grants[i].Key) return false;
            return true;
        }
        public static void QueuePvp(string session,string expedition,string victimSession,string victimExpedition,string key)
        {
            if(session==victimSession || string.IsNullOrEmpty(expedition) || string.IsNullOrEmpty(victimExpedition) || string.IsNullOrEmpty(key) || key.Length>96)
                throw new InvalidOperationException("Evento PvP invalido.");
            var reward=new PendingReward {
                Grants=new[] { new ExperienceGrant { Session=session,Expedition=expedition,Key=key,Amount=20 } },
                Owners=new[] { CaptureRewardOwner(session),CaptureRewardOwner(victimSession) },
                Victim=victimSession,VictimExpedition=victimExpedition };
            if(!pendingRewards.Exists(old=>SameReward(old,reward))) pendingRewards.Add(reward);
        }
        public static bool FlushPendingExperience()
        { if(pendingRewards.Count==0 && pendingProfile.Count==0) return false; Transaction(data=>0); return true; }
        static void ApplyPendingExperience(Dictionary<string,CampRecord> data,PendingReward[] batch)
        {
            foreach(var reward in batch)
            {
                reward.Rejected=false;
                // The captured credential is checked under the transaction mutex.
                // It can outlive the local connection cache, but never its lease/token.
                foreach(var owner in reward.Owners)
                    if(!data.TryGetValue(owner.Session,out var record) || record.Lease!=owner.Owner || record.LeaseToken!=owner.Token || record.LeaseExpires<=DateTime.UtcNow.Ticks)
                    { reward.Rejected=true; break; }
                if(reward.Rejected) continue; // Never grant through a superseded room's credential.
                if(reward.Victim!=null)
                {
                    var grant=reward.Grants[0];
                    if(data[reward.Victim].Progress.ExpeditionId!=reward.VictimExpedition) { reward.Rejected=true; continue; }
                    if(ApplyPvp(data,grant.Session,grant.Expedition,reward.Victim,grant.Key,false))
                    { data[grant.Session].Progress.Revision++; data[reward.Victim].Progress.Revision++; }
                }
                else
                {
                    foreach(var grant in reward.Grants)
                        if(data[grant.Session].Progress.ExpeditionState!=2 || data[grant.Session].Progress.ExpeditionId!=grant.Expedition)
                        { reward.Rejected=true; break; }
                    if(!reward.Rejected) foreach(var grant in reward.Grants)
                    {
                        var profile=data[grant.Session].Progress;
                        long before=profile.ExpeditionExperience;
                        RecordExperience(profile,grant);
                        // A caller holding an older profile must not overwrite this XP via Save.
                        if(profile.ExpeditionExperience!=before) profile.Revision++;
                    }
                }
            }
        }
        static void CompletePendingExperience(PendingReward[] batch)
        {
            foreach(var reward in batch)
            {
                pendingRewards.Remove(reward);
                if(reward.Rejected) UnityEngine.Debug.LogWarning("Evento de XP encerrado sem concessao: posse ou expedicao original nao e mais valida.");
            }
            if(batch.Length>0) ExperienceGeneration++;
        }
        static void ClearPendingExperience()
        {
            if(pendingRewards.Count>0) UnityEngine.Debug.LogError("Repositorio encerrado com "+pendingRewards.Count+" evento(s) de XP ainda nao persistido(s).");
            pendingRewards.Clear(); ExperienceGeneration=0;
        }
    }
}
