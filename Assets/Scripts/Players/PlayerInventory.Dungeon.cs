using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Match;

namespace CyberExtraction.Players
{
    public partial class PlayerInventory
    {
        public NetworkVariable<int> DungeonAction=new NetworkVariable<int>(-1,NetworkVariableReadPermission.Owner);
        public NetworkVariable<double> DungeonActionEnds=new NetworkVariable<double>(0,NetworkVariableReadPermission.Owner);
        internal Vector2 DungeonActionOrigin;
        internal int DungeonActionDamage;
        internal float DungeonNextActionAt;
        PlayerShooter dungeonShooter;
        internal bool DungeonWeaponBusy => ChangingEquipment || SwitchingWeapon || dungeonShooter!=null && (dungeonShooter.CyclingBolt || dungeonShooter.MeleeAttacking);
        internal int DungeonDamageVersion => health!=null ? health.DamageVersion.Value : -1;
        float nextDungeonRequest;
        public void RequestDungeonAction(int id)
        {
            if(!IsOwner || Time.unscaledTime<nextDungeonRequest) return;
            nextDungeonRequest=Time.unscaledTime+.2f; DungeonActionServerRpc(id);
        }
        [ServerRpc] void DungeonActionServerRpc(int id)
        { if(MatchManager.Instance!=null) MatchManager.Instance.ServerBeginDungeonAction(this,id); }
        public float DungeonActionProgress => DungeonAction.Value<0 || NetworkManager==null ? 0 :
            Mathf.Clamp01(1-(float)(DungeonActionEnds.Value-NetworkManager.ServerTime.Time)/DungeonContent.Duration(DungeonAction.Value));
    }
}
