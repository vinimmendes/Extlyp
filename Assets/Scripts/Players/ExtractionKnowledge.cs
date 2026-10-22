using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Match;

namespace CyberExtraction.Players
{
    public partial class PlayerInventory
    {
        public NetworkVariable<int> ConfirmedExit=new NetworkVariable<int>(-1,NetworkVariableReadPermission.Owner);
        public NetworkVariable<int> SelectedExit=new NetworkVariable<int>(-1,NetworkVariableReadPermission.Owner);
        public NetworkVariable<ushort> KnownActiveExits=new NetworkVariable<ushort>(0,NetworkVariableReadPermission.Owner);
        public NetworkVariable<ushort> KnownInactiveExits=new NetworkVariable<ushort>(0,NetworkVariableReadPermission.Owner);
        public NetworkVariable<float> ExtractionProgress=new NetworkVariable<float>(0,NetworkVariableReadPermission.Owner);
        public NetworkVariable<int> CurrentExtractionExit=new NetworkVariable<int>(-1,NetworkVariableReadPermission.Owner);
        float nextExitInspection;
        public void ServerInitializeExits()
        {
            if(!IsServer) return;
            int exit=MapBuilder.Current==ExpeditionMap.Dungeon ? MapBuilder.NearestDungeonExit(transform.position,5) : MapBuilder.Current==ExpeditionMap.Large ? LargeMapPlan.BestExit(transform.position,MatchManager.Instance.ActiveExtractions.Value) : -1;
            ConfirmedExit.Value=SelectedExit.Value=exit;
            KnownActiveExits.Value=exit>=0 ? (ushort)(1<<exit) : (ushort)0;
            KnownInactiveExits.Value=MapBuilder.Current==ExpeditionMap.Dungeon ? (ushort)7 : (ushort)0;
            if(MapBuilder.Current==ExpeditionMap.Dungeon) KnownActiveExits.Value=0;
            DungeonAction.Value=-1; DungeonActionEnds.Value=0; ExtractionProgress.Value=0; CurrentExtractionExit.Value=-1;
        }
        public string ExitKnowledge(int id) => MapBuilder.Current==ExpeditionMap.Dungeon ? DungeonContent.ExitStatus(id) : (KnownActiveExits.Value&(1<<id))!=0 ? "Confirmada" :
            (KnownInactiveExits.Value&(1<<id))!=0 ? "Indisponível" : "Não verificada";
        void InspectExits()
        {
            if(!IsServer || !IsAlive || MatchManager.Instance==null || MatchManager.Instance.CurrentState!=MatchState.Running ||
                MapBuilder.Current==ExpeditionMap.Test || Time.time<nextExitInspection) return;
            nextExitInspection=Time.time+.15f;
            if(MapBuilder.Current==ExpeditionMap.Dungeon)
            { KnownActiveExits.Value=MatchManager.Instance.ActiveExtractions.Value; KnownInactiveExits.Value=(ushort)(7 & ~KnownActiveExits.Value); return; }
            foreach(var marker in ForestExtractionMarker.All)
            {
                ushort bit=(ushort)(1<<marker.ExitId);
                if(((KnownActiveExits.Value|KnownInactiveExits.Value)&bit)!=0 || !marker.Inspectable(transform.position,transform.eulerAngles.z,this)) continue;
                if((MatchManager.Instance.ActiveExtractions.Value&bit)!=0) KnownActiveExits.Value|=bit;
                else KnownInactiveExits.Value|=bit;
            }
        }
        public void RequestExitDestination(int id) { if(IsOwner) SelectExitServerRpc(id); }
        [ServerRpc] void SelectExitServerRpc(int id)
        {
            if(!IsServer || !IsAlive || MapBuilder.Current==ExpeditionMap.Test || id<0 || id>=MapBuilder.ExitCount) return;
            SelectedExit.Value=id;
        }
    }
}
