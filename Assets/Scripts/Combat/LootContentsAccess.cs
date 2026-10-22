using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Items;

namespace CyberExtraction.Combat
{
    public partial class LootPickup
    {
        readonly HashSet<ulong> contentReaders=new HashSet<ulong>();
        readonly Dictionary<ulong,string> contentTokens=new Dictionary<ulong,string>();
        string contentToken;
        readonly List<ulong> contentClients=new List<ulong>();
        bool contentsDirty;
        float nextContents;
        public bool HasContents => IsServer || Contents.Count==ItemCatalog.InventorySlots;
        public bool CanInspectContents(CyberExtraction.Players.PlayerInventory player) =>
            IsSpawned && Chest.Value && OpenState.Value==2 && !Locked.Value && FixtureKind.Value!=3 &&
            player!=null && player.IsAlive && player.DungeonAction.Value<0 &&
            !(player.GetComponent<CyberExtraction.Players.PlayerShooter>() is CyberExtraction.Players.PlayerShooter shooter && shooter.MeleeAttacking) &&
            CyberExtraction.Match.MatchManager.Instance!=null &&
            CyberExtraction.Match.MatchManager.Instance.CurrentState==CyberExtraction.Match.MatchState.Running &&
            Vector2.Distance(player.transform.position,transform.position)<=3 && LockLineClear(player.transform.position);
        public void RefreshContentsView()
        {
            ClearContentsView();
            RequestContents();
        }
        public void ClearContentsView() { contentToken=null; if(!IsServer) Contents.Clear(); }
        bool CanReadContents(ulong client) => IsSpawned && Chest.Value && OpenState.Value==2 &&
            !Locked.Value && NetworkObject.IsNetworkVisibleTo(client) && Sender(client)!=null;
        public void RequestContents()
        {
            if(!IsClient || !IsSpawned || !Chest.Value) return;
            contentToken=Guid.NewGuid().ToString("N"); RequestContentsRpc(contentToken);
        }
        [Rpc(SendTo.Server, InvokePermission=RpcInvokePermission.Everyone)]
        void RequestContentsRpc(string token,RpcParams rpc=default)
        {
            ulong client=rpc.Receive.SenderClientId;
            if(!Guid.TryParseExact(token,"N",out _) || !CanReadContents(client)) return;
            contentTokens[client]=token;
            // Repeated requests for the same view are answered by the next dirty update.
            if(contentReaders.Add(client)) SendContents(client,true);
            else contentsDirty=true;
        }
        void SendContents(ulong client,bool allowed)
        {
            if(client==NetworkManager.ServerClientId) return; // The host already owns server state.
            if(!contentTokens.TryGetValue(client,out var token)) return;
            ContentsClientRpc(token,allowed ? Contents.ToArray() : Array.Empty<ItemStack>(),
                new ClientRpcParams { Send=new ClientRpcSendParams { TargetClientIds=new[] { client } } });
        }
        [ClientRpc]
        void ContentsClientRpc(string token,ItemStack[] slots,ClientRpcParams rpc=default)
        {
            if(IsServer || !IsSpawned || contentToken==null || contentToken!=token) return;
            if(slots==null || slots.Length!=0 && slots.Length!=ItemCatalog.InventorySlots)
            { Contents.Clear(); return; }
            Contents.Replace(slots);
        }
        void TickContentsAccess()
        {
            if(!Chest.Value || OpenState.Value!=2 && contentReaders.Count==0 || Time.unscaledTime<nextContents) return;
            nextContents=Time.unscaledTime+.2f;
            contentClients.Clear(); contentClients.AddRange(NetworkManager.ConnectedClientsIds);
            foreach(ulong client in contentClients)
            {
                bool allowed=CanReadContents(client);
                if(allowed)
                {
                    bool added=contentReaders.Add(client);
                    if(added || contentsDirty) SendContents(client,true);
                }
                else if(contentReaders.Remove(client))
                {
                    if(NetworkObject.IsNetworkVisibleTo(client)) SendContents(client,false);
                    contentTokens.Remove(client);
                }
            }
            contentClients.Clear();
            foreach(var client in contentReaders) if(!NetworkManager.ConnectedClients.ContainsKey(client)) contentClients.Add(client);
            foreach(var client in contentClients) { contentReaders.Remove(client); contentTokens.Remove(client); }
            contentsDirty=false;
        }
    }
}
