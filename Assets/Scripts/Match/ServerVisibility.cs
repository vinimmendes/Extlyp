using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using CyberExtraction.Players;
using CyberExtraction.Core;
using CyberExtraction.Bots;
namespace CyberExtraction.Match
{
    public class ServerVisibility : MonoBehaviour
    {
        static readonly List<NetworkObject> tracked=new List<NetworkObject>();
        float nextTick;
        public static bool CanSee(Vector2 origin,float facing,Vector2 point) => BotController.InVision(origin,facing,point) && Physics2D.Linecast(origin,point,GameConfig.VisionMask).collider==null;
        public static void Install(NetworkObject obj)
        {
            if(obj==null) return;
            obj.CheckObjectVisibility=client=>CanObserve(obj,client);
            if(!tracked.Contains(obj)) tracked.Add(obj);
        }
        public static bool CanObserve(NetworkObject obj,ulong client)
        {
            if(client==NetworkManager.ServerClientId || obj.IsPlayerObject && obj.OwnerClientId==client) return true;
            var nm=NetworkManager.Singleton;
            if(nm==null || UI.NetworkBootstrap.Instance==null || UI.NetworkBootstrap.Instance.IsCampClient(client) || !nm.ConnectedClients.TryGetValue(client,out var connection) || connection.PlayerObject==null) return false;
            var player=connection.PlayerObject;
            var lockedObject=obj.GetComponent<CyberExtraction.Combat.LootPickup>();
            if(lockedObject!=null && lockedObject.FixtureKind.Value!=0) return BotController.InVision(player.transform.position,player.transform.eulerAngles.z,lockedObject.SightPoint(player.transform.position)) && lockedObject.SightLineClear(player.transform.position);
            return CanSee(player.transform.position,player.transform.eulerAngles.z,obj.transform.position);
        }
        void Update()
        {
            var nm=NetworkManager.Singleton; if(nm==null || !nm.IsServer || Time.unscaledTime<nextTick) return;
            nextTick=Time.unscaledTime+.05f;
            for(int i=tracked.Count-1;i>=0;i--)
            {
                var obj=tracked[i]; if(obj==null) { tracked.RemoveAt(i); continue; } if(!obj.IsSpawned) continue;
                foreach(ulong client in nm.ConnectedClientsIds)
                {
                    if(client==NetworkManager.ServerClientId) continue;
                    bool visible=CanObserve(obj,client), observed=obj.IsNetworkVisibleTo(client);
                    if(visible && !observed) obj.NetworkShow(client);
                    else if(!visible && observed) obj.NetworkHide(client);
                }
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { tracked.Clear(); }
    }
}
