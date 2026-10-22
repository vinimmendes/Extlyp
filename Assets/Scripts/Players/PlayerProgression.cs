using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Camp;
using CyberExtraction.Match;

namespace CyberExtraction.Players
{
    public partial class PlayerInventory
    {
        public NetworkVariable<int> CharacterLevel=new NetworkVariable<int>(1,NetworkVariableReadPermission.Owner);
        public NetworkVariable<long> ConfirmedExperience=new NetworkVariable<long>(0,NetworkVariableReadPermission.Owner);
        public NetworkVariable<int> AvailableAttributePoints=new NetworkVariable<int>(0,NetworkVariableReadPermission.Owner);
        public NetworkVariable<long> PendingExperience=new NetworkVariable<long>(0,NetworkVariableReadPermission.Owner);
        public NetworkVariable<long> LastConfirmedExperience=new NetworkVariable<long>(0,NetworkVariableReadPermission.Owner);
        string progressionSession;
        bool progressionCommitted,progressionFinished;
        float nextExploration;
        readonly HashSet<int> visitedExperienceRegions=new HashSet<int>();
        internal string ProgressionSession => progressionSession;
        internal string ProgressionExpedition => expeditionId;
        internal bool ProgressionActive => progressionCommitted && !progressionFinished && !string.IsNullOrEmpty(progressionSession);
        internal bool ProgressionEligible => IsServer && IsSpawned && IsAlive && health!=null && !health.IsDead && progressionCommitted && !progressionFinished &&
            !string.IsNullOrEmpty(progressionSession) && MatchManager.Instance!=null && MatchManager.Instance.CurrentState==MatchState.Running &&
            !MatchManager.Instance.Sandbox && NetworkManager.ConnectedClients.ContainsKey(OwnerClientId);
        internal void ServerRefreshProgress()
        {
            if(!IsServer || !IsSpawned || string.IsNullOrEmpty(progressionSession)) return;
            var p=CampRepository.Get(progressionSession);
            CharacterLevel.Value=p.Character.Level; ConfirmedExperience.Value=p.Character.Experience;
            AvailableAttributePoints.Value=p.Character.UnspentPoints; PendingExperience.Value=p.ExpeditionExperience;
            LastConfirmedExperience.Value=p.LastExperienceExpedition==expeditionId ? p.LastExperienceAward : 0;
        }
        public void ServerAwardObjective(string key,int amount)
        {
            if(!ProgressionEligible) return;
            TryGrantExperience(key,amount,false);
        }
        bool TryGrantExperience(string key,int amount,bool exploration)
        {
            try
            {
                CampRepository.QueueExperience(new[] { new ExperienceGrant { Session=progressionSession,Expedition=expeditionId,Key=key,Amount=amount,Exploration=exploration } });
            }
            catch(System.Exception error) { Debug.LogError("XP nao enfileirado: "+error.Message); return false; }
            try { CampRepository.FlushPendingExperience(); ServerRefreshProgress(); }
            catch(System.Exception error) { Debug.LogError("XP enfileirado; persistencia/publicacao sera tentada novamente: "+error.Message); }
            return true;
        }
        void StepExplorationExperience()
        {
            if(!ProgressionEligible || Time.time<nextExploration) return;
            nextExploration=Time.time+.5f;
            Vector2 point=transform.position;
            if(MapBuilder.Current==ExpeditionMap.Dungeon)
            {
                foreach(var room in DungeonLayout.Rooms)
                    if(DungeonLayout.WorldRect(room.Cells).Contains(point)) { VisitExperienceRegion(room.Sector); break; }
            }
            else
                for(int i=0;i<MapBuilder.Sites.Length;i++)
                    if((point-MapBuilder.Sites[i]).sqrMagnitude<=36) VisitExperienceRegion(MapBuilder.Current==ExpeditionMap.Large ? i/4 : i);
        }
        void VisitExperienceRegion(int region)
        {
            if(visitedExperienceRegions.Contains(region)) return;
            if(TryGrantExperience("region:"+region,5,true)) visitedExperienceRegions.Add(region);
        }
    }
}
