using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Players;

namespace CyberExtraction.Match
{
    public partial class MatchManager
    {
        public NetworkVariable<ushort> ActiveExtractions=new NetworkVariable<ushort>(0);
        public NetworkVariable<uint> OpenedPassages=new NetworkVariable<uint>(0);
        void ExitStateChanged(ushort previous,ushort current) { foreach(var marker in ForestExtractionMarker.All) if(marker!=null) marker.Apply(); }
        void PassageStateChanged(uint previous,uint current)
        {
            foreach(var gate in ForestPassage.All) if(gate!=null) gate.Apply();
            LargeMapPlan.PassageChanged(); Physics2D.SyncTransforms();
        }
        public void RequestPassage(int id) { OpenPassageRpc(id); }
        [Rpc(SendTo.Server,InvokePermission=RpcInvokePermission.Everyone)]
        void OpenPassageRpc(int id,RpcParams rpc=default)
        {
            if(!IsServer || CurrentState!=MatchState.Running || MapBuilder.Current!=ExpeditionMap.Large || id<0 || id>=LargeMapPlan.Routes.Count ||
                !NetworkManager.ConnectedClients.TryGetValue(rpc.Receive.SenderClientId,out var client) || client.PlayerObject==null) return;
            var inv=client.PlayerObject.GetComponent<PlayerInventory>();
            if(inv==null || !inv.IsAlive || inv.Lockpicking.Value) return;
            foreach(var gate in ForestPassage.All)
                if(gate.RouteId==id && gate.Route.Condition==RouteCondition.Gated && gate.CanOperate(inv))
                { OpenedPassages.Value|=1u<<id; return; }
        }
    }
}
