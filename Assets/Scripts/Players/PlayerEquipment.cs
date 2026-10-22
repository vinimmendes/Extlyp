using UnityEngine;
using Unity.Netcode;
using CyberExtraction.Items;

namespace CyberExtraction.Players
{
    public partial class PlayerInventory
    {
        public NetworkVariable<double> EquipmentChangeEnds=new NetworkVariable<double>(0,NetworkVariableReadPermission.Owner);
        int changingBag=-1,changingSlot=-1,changingDamage;
        Vector2 changingOrigin;
        ItemStack changingIncoming,changingOutgoing;
        public bool ChangingEquipment => EquipmentChangeEnds.Value>0;
        public EquipmentLoadout Loadout()
        {
            var result=new EquipmentLoadout { ActiveWeaponSlot=ActiveWeaponSlot };
            for(int i=0;i<Mathf.Min(Equipment.Count,EquipmentLoadout.Count);i++) result.Slots[i]=Equipment[i];
            return result;
        }
        public void ServerSetLoadout(EquipmentLoadout value)
        {
            if(!IsServer) return;
            if(value==null || value.Slots==null || value.Slots.Length!=EquipmentLoadout.Count) throw new System.InvalidOperationException("Equipamento invalido.");
            for(int i=0;i<EquipmentLoadout.Count;i++) if(!EquipmentLoadout.Accepts(i,value.Slots[i])) throw new System.InvalidOperationException("Equipamento incompativel.");
            GetComponent<PlayerShooter>()?.ServerCancelReload();
            GetComponent<PlayerShooter>()?.ServerCancelMelee();
            while(Equipment.Count<EquipmentLoadout.Count) Equipment.Add(default);
            for(int i=0;i<EquipmentLoadout.Count;i++) if(!Equipment[i].Equals(value.Slots[i])) Equipment[i]=value.Slots[i];
            EquippedSlot.Value=value.ActiveWeaponSlot;
            if(!IsWeaponSlot(EquippedSlot.Value)) EquippedSlot.Value=FirstWeaponSlot();
            ServerRefreshEquipped();
            ServerRefreshAttributes();
        }
        public void RequestEquipmentExchange(int bag,int slot)
        {
            if(IsOwner && bag>=0 && bag<Slots.Count && slot>=0 && slot<Equipment.Count)
                ExchangeEquipmentServerRpc(bag,slot,Slots[bag],Equipment[slot]);
        }
        public void CancelEquipmentChange() { if(IsOwner) CancelEquipmentServerRpc(); }
        public void ServerCancelEquipmentChange() { if(IsServer) StopEquipmentChange(); }
        [ServerRpc] void CancelEquipmentServerRpc() { StopEquipmentChange(); }
        void StopEquipmentChange() { changingBag=changingSlot=-1; EquipmentChangeEnds.Value=0; }
        bool EquipmentIdle => CanAct && FishingPhase.Value==0 && ExtractionProgress.Value<=0 && !SwitchingWeapon &&
            dungeonShooter!=null && !dungeonShooter.MeleeAttacking && !dungeonShooter.Reloading && !dungeonShooter.CyclingBolt;
        [ServerRpc] void ExchangeEquipmentServerRpc(int bag,int slot,ItemStack incoming,ItemStack outgoing)
        {
            if(!EquipmentIdle || ChangingEquipment || bag<0 || bag>=Slots.Count || slot<0 || slot>=Equipment.Count ||
                !Slots[bag].Equals(incoming) || !Equipment[slot].Equals(outgoing)) return;
            var pack=Grid(); var loadout=Loadout();
            if(!loadout.Exchange(pack,bag,slot)) return;
            changingBag=bag; changingSlot=slot; changingIncoming=incoming; changingOutgoing=outgoing;
            changingOrigin=transform.position; changingDamage=health.DamageVersion.Value;
            EquipmentChangeEnds.Value=NetworkManager.ServerTime.Time+2;
        }
        void StepEquipmentChange()
        {
            if(!ChangingEquipment) return;
            if(!EquipmentIdle || health.DamageVersion.Value!=changingDamage ||
                Vector2.Distance(changingOrigin,transform.position)>.05f || changingBag<0 ||
                !Slots[changingBag].Equals(changingIncoming) || !Equipment[changingSlot].Equals(changingOutgoing))
            { StopEquipmentChange(); return; }
            if(NetworkManager.ServerTime.Time<EquipmentChangeEnds.Value) return;
            var pack=Grid(); var loadout=Loadout();
            if(loadout.Exchange(pack,changingBag,changingSlot)) { ServerSetLoadout(loadout); ServerSetGrid(pack); }
            StopEquipmentChange();
        }
    }
}
