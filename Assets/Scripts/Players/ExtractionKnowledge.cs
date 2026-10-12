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
            int exit=MapBuilder.Current==ExpeditionMap.Large ? LargeMapPlan.BestExit(transform.position,MatchManager.Instance.ActiveExtractions.Value) : -1;
            ConfirmedExit.Value=SelectedExit.Value=exit;
            KnownActiveExits.Value=exit>=0 ? (ushort)(1<<exit) : (ushort)0;
            KnownInactiveExits.Value=0; ExtractionProgress.Value=0; CurrentExtractionExit.Value=-1;
        }
        public string ExitKnowledge(int id) => (KnownActiveExits.Value&(1<<id))!=0 ? "Confirmada" :
            (KnownInactiveExits.Value&(1<<id))!=0 ? "Indisponível" : "Não verificada";
        void InspectExits()
        {
            if(!IsServer || !IsAlive || MatchManager.Instance==null || MatchManager.Instance.CurrentState!=MatchState.Running ||
                MapBuilder.Current!=ExpeditionMap.Large || Time.time<nextExitInspection) return;
            nextExitInspection=Time.time+.15f;
            foreach(var marker in ForestExtractionMarker.All)
            {
                ushort bit=(ushort)(1<<marker.ExitId);
                if(((KnownActiveExits.Value|KnownInactiveExits.Value)&bit)!=0 || !marker.Inspectable(transform.position,transform.eulerAngles.z)) continue;
                if((MatchManager.Instance.ActiveExtractions.Value&bit)!=0) KnownActiveExits.Value|=bit;
                else KnownInactiveExits.Value|=bit;
            }
        }
        public void RequestExitDestination(int id) { if(IsOwner) SelectExitServerRpc(id); }
        [ServerRpc] void SelectExitServerRpc(int id)
        {
            if(!IsServer || !IsAlive || MapBuilder.Current!=ExpeditionMap.Large || id<0 || id>=LargeMapPlan.Exits.Length) return;
            SelectedExit.Value=id;
        }
    }
}
