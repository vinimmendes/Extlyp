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
        public static bool CanSee(Vector2 origin,float facing,Vector2 point,PlayerInventory observer=null) => BotController.InVision(origin,facing,point,observer) && Physics2D.Linecast(origin,point,GameConfig.VisionMask).collider==null;
        public static bool CanSeeChest(Vector2 origin,float facing,Transform chest,PlayerInventory observer=null)
        {
            // ChestAppearance has a fixed world footprint of 1.5 x 1.2, independent
            // of the loot pickup trigger radius and prefab scale. Observe when any
            // part enters sight; the fog continues to dim the unexposed pixels.
            float angle=chest.eulerAngles.z*Mathf.Deg2Rad;
            Vector2 right=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            Vector2 up=new Vector2(-right.y,right.x);
            Vector2 center=chest.position;
            var loot=chest.GetComponent<CyberExtraction.Combat.LootPickup>();
            // Opening the Blender lid extends the art below the closed body.
            float lower=loot!=null && loot.OpenState.Value!=0 ? -.975f : -.6f;
            for(int y=-1;y<=1;y++)
                for(int x=-1;x<=1;x++)
                    if(CanSee(origin,facing,center+right*(x*.75f)+up*(y<0 ? lower : y*.6f),observer)) return true;
            return false;
        }
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
            var observer=player.GetComponent<PlayerInventory>();
            var lockedObject=obj.GetComponent<CyberExtraction.Combat.LootPickup>();
            if(lockedObject!=null && lockedObject.FixtureKind.Value!=0) return BotController.InVision(player.transform.position,player.transform.eulerAngles.z,lockedObject.SightPoint(player.transform.position),observer) && lockedObject.SightLineClear(player.transform.position);
            if(lockedObject!=null && (lockedObject.Chest.Value || lockedObject.initialChest))
                return CanSeeChest(player.transform.position,player.transform.eulerAngles.z,obj.transform,observer);
            return CanSee(player.transform.position,player.transform.eulerAngles.z,obj.transform.position,observer);
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
