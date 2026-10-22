using System;
using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Items;
using CyberExtraction.Match;

namespace CyberExtraction.Players
{
    public partial class PlayerInventory
    {
        // Owner-only acknowledgement is read in Update, after the inventory delta is applied.
        public NetworkVariable<ulong> OrganizationReceipt=new NetworkVariable<ulong>(0,NetworkVariableReadPermission.Owner);
        public bool OrganizePending { get; private set; }
        public event Action<byte> OrganizationFinished;
        uint organizationSequence,pendingOrganization;
        double nextOrganization;

        public bool CanOrganize
        {
            get
            {
                var match=MatchManager.Instance;
                var shooter=dungeonShooter!=null ? dungeonShooter : GetComponent<PlayerShooter>();
                return IsSpawned && IsAlive && Slots.Count==ItemCatalog.BackpackSlots && match!=null && match.CurrentState==MatchState.Running &&
                    !ChangingEquipment && !SwitchingWeapon && !Lockpicking.Value && DungeonAction.Value<0 &&
                    FishingPhase.Value==0 && ExtractionProgress.Value<=0 &&
                    (shooter==null || !shooter.Reloading && !shooter.MeleeAttacking && !shooter.CyclingBolt);
            }
        }
        public bool RequestOrganize()
        {
            if(!IsOwner || OrganizePending || !CanOrganize) return false;
            pendingOrganization=++organizationSequence;
            OrganizePending=true;
            OrganizeServerRpc(pendingOrganization,Grid().Slots);
            return true;
        }
        [ServerRpc]
        void OrganizeServerRpc(uint request,ItemStack[] expected)
        {
            byte result=0;
            if(CanOrganize && Time.unscaledTimeAsDouble>=nextOrganization && expected!=null && expected.Length==ItemCatalog.BackpackSlots)
            {
                nextOrganization=Time.unscaledTimeAsDouble+.2;
                var grid=Grid(); bool matches=true;
                for(int i=0;i<expected.Length;i++) if(!grid.Slots[i].Equals(expected[i])) { matches=false; break; }
                if(matches && grid.TryOrganize(out bool changed))
                {
                    if(changed) ServerSetGrid(grid);
                    result=changed ? (byte)1 : (byte)2;
                }
            }
            OrganizationReceipt.Value=((ulong)request<<8)|result;
        }
        void StepOrganizationReceipt()
        {
            if(!IsOwner || !OrganizePending || (uint)(OrganizationReceipt.Value>>8)!=pendingOrganization) return;
            OrganizePending=false;
            OrganizationFinished?.Invoke((byte)(OrganizationReceipt.Value&255));
        }
    }
}
