using System;
using System.Collections.Generic;
using CyberExtraction.Core;

namespace CyberExtraction.Camp
{
    public sealed class ExperienceGrant
    {
        public string Session,Expedition,Key;
        public int Amount;
        public bool Exploration;
    }
    [Serializable] public sealed class PvpRewardPair { public string Other; public long Until; }

    public static partial class CampRepository
    {
        static void ConfirmExperience(CampProgress profile,bool extracted)
        {
            if(profile.ExpeditionState!=2 || profile.LastExperienceExpedition==profile.ExpeditionId) return;
            profile.LastExperienceAward=profile.Character.GainExperience(CharacterProgressionRules.ConfirmedExperience(profile.ExpeditionExperience,extracted));
            profile.LastExperienceExpedition=profile.ExpeditionId; profile.LastExperienceExtracted=extracted;
        }
        static bool RecordExperience(CampProgress p,ExperienceGrant grant)
        {
            if(p.ExpeditionState!=2 || p.ExpeditionId!=grant.Expedition || grant.Amount<=0 || string.IsNullOrEmpty(grant.Key) || grant.Key.Length>96) return false;
            if(p.ExpeditionRewardKeys.Contains(grant.Key)) return true;
            int amount=grant.Exploration ? Math.Min(grant.Amount,40-p.ExpeditionExplorationExperience) : grant.Amount;
            if(amount<=0 || p.ExpeditionExperience>=CharacterProgressionRules.MaxPendingExperience) return true;
            amount=(int)Math.Min(amount,CharacterProgressionRules.MaxPendingExperience-p.ExpeditionExperience);
            p.ExpeditionRewardKeys.Add(grant.Key); p.ExpeditionExperience+=amount;
            if(grant.Key.StartsWith("death:",StringComparison.Ordinal)) p.Statistics.CombatXp+=amount;
            if(grant.Exploration) p.ExpeditionExplorationExperience+=amount;
            return true;
        }
        public static void GrantExperience(IReadOnlyList<ExperienceGrant> grants)=>Transaction(data=>
        {
            // Validate the whole batch before rewarding any participant.
            foreach(var grant in grants) OwnedRecord(data,grant.Session);
            foreach(var grant in grants)
                RecordExperience(data[grant.Session].Progress,grant);
            return 0;
        });
        public static bool GrantPvp(string session,string expedition,string victimSession,string eventKey)=>Transaction(data=>
            ApplyPvp(data,session,expedition,victimSession,eventKey));
        static bool ApplyPvp(Dictionary<string,CampRecord> data,string session,string expedition,string victimSession,string eventKey,bool checkOwnership=true)
        {
            if(session==victimSession || !data.TryGetValue(session,out var killer) || !data.TryGetValue(victimSession,out var victim) || victim.Progress.ExpeditionState!=2) return false;
            if(checkOwnership) { OwnedRecord(data,session); OwnedRecord(data,victimSession); }
            var p=killer.Progress;
            if(p.ExpeditionState!=2 || p.ExpeditionId!=expedition || p.ExpeditionPvpExperience>=60 || p.ExpeditionRewardKeys.Contains(eventKey)) return false;
            long now=DateTime.UtcNow.Ticks;
            if(killer.PvpRewards==null) killer.PvpRewards=new List<PvpRewardPair>();
            if(victim.PvpRewards==null) victim.PvpRewards=new List<PvpRewardPair>();
            killer.PvpRewards.RemoveAll(pair=>pair==null || pair.Until<=now);
            victim.PvpRewards.RemoveAll(pair=>pair==null || pair.Until<=now);
            if(killer.PvpRewards.Exists(pair=>pair.Other==victimSession) || victim.PvpRewards.Exists(pair=>pair.Other==session)) return false;
            long until=now+TimeSpan.FromHours(24).Ticks;
            if(!RecordExperience(p,new ExperienceGrant { Expedition=expedition,Key=eventKey,Amount=20 })) return false;
            p.ExpeditionPvpExperience+=20;
            killer.PvpRewards.Add(new PvpRewardPair { Other=victimSession,Until=until });
            victim.PvpRewards.Add(new PvpRewardPair { Other=session,Until=until });
            return true;
        }
    }
}
