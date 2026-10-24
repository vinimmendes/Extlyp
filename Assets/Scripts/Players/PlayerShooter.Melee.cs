using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Combat;
using CyberExtraction.Core;
using CyberExtraction.Enemies;
using CyberExtraction.Items;

namespace CyberExtraction.Players
{
    public partial class PlayerShooter
    {
        public NetworkVariable<double> MeleeStartedAt=new NetworkVariable<double>(0);
        public NetworkVariable<double> MeleeImpactAt=new NetworkVariable<double>(0);
        public NetworkVariable<double> MeleeEndsAt=new NetworkVariable<double>(0);
        public NetworkVariable<float> MeleeAngle=new NetworkVariable<float>(0);
        public NetworkVariable<byte> MeleeItem=new NetworkVariable<byte>(255);
        public bool MeleeAttacking => MeleeEndsAt.Value>Now;
        public bool MeleeDirectionLocked => MeleeAttacking && Now>=MeleeImpactAt.Value;
        ItemStack meleeStack;
        int meleeSlot=-1;
        bool meleeImpacted;
        double meleeAimReceivedAfter;
        readonly List<Health> meleeCandidates=new List<Health>();
        readonly HashSet<ulong> meleeHitIds=new HashSet<ulong>();

        void ServerBeginMelee(float angle)
        {
            var weapon=Weapon;
            if(!IsServer || weapon==null || !weapon.IsMelee || MeleeEndsAt.Value>0 || movement==null ||
                inv.ExtractionProgress.Value>0 || !movement.ServerSpendStamina(weapon.StaminaCost,weapon.Cooldown)) return;
            meleeSlot=inv.ActiveWeaponSlot; meleeStack=inv.Equipment[meleeSlot]; meleeImpacted=false;
            meleeAimReceivedAfter=Time.timeAsDouble;
            MeleeItem.Value=(byte)weapon.Item;
            MeleeAngle.Value=Mathf.Repeat(angle,360);
            MeleeStartedAt.Value=Now; MeleeImpactAt.Value=Now+weapon.WindupSeconds;
            MeleeEndsAt.Value=Now+weapon.Cooldown;
            nextShotAt=MeleeEndsAt.Value;
        }

        public void ServerCancelMelee()
        {
            if(!IsServer) return;
            // Keep the spend and attack cooldown after cancellation.
            MeleeEndsAt.Value=0; MeleeItem.Value=255; meleeSlot=-1; meleeCandidates.Clear();
        }

        void StepMelee()
        {
            if(MeleeEndsAt.Value<=0) return;
            if(!CanAct || meleeSlot<0 || inv.ActiveWeaponSlot!=meleeSlot ||
                meleeSlot>=inv.Equipment.Count || !inv.Equipment[meleeSlot].Equals(meleeStack))
            { ServerCancelMelee(); return; }
            if(!meleeImpacted)
            {
                if(Now<MeleeImpactAt.Value)
                {
                    // An older unreliable movement packet must not overwrite the reliable attack aim.
                    if(movement!=null && movement.ServerAimReceivedAt>=meleeAimReceivedAfter)
                        MeleeAngle.Value=movement.ServerFacing;
                    return;
                }
                // The last authoritative aim during preparation becomes the locked impact direction.
                meleeImpacted=true;
                ResolveMeleeImpact(Weapon);
            }
            if(Now>=MeleeEndsAt.Value) ServerCancelMelee();
        }

        void ResolveMeleeImpact(WeaponDefinition weapon)
        {
            meleeCandidates.Clear();
            meleeHitIds.Clear();
            foreach(var target in Health.All)
                if(MeleeTarget(target,weapon)) meleeCandidates.Add(target);
            Vector2 origin=transform.position;
            meleeCandidates.Sort((a,b)=>
            {
                int distance=((Vector2)a.transform.position-origin).sqrMagnitude.CompareTo(((Vector2)b.transform.position-origin).sqrMagnitude);
                return distance!=0 ? distance : a.NetworkObjectId.CompareTo(b.NetworkObjectId);
            });
            int hitCount=0;
            for(int i=0;i<meleeCandidates.Count && hitCount<3;i++)
            {
                var target=meleeCandidates[i];
                if(!MeleeTarget(target,weapon) || !meleeHitIds.Add(target.NetworkObjectId)) continue;
                // One application per entity, independent of its collider/component count.
                target.ServerTakeDamage(Mathf.RoundToInt(weapon.Damage*inv.MeleeDamageMultiplier),inv,origin:DamageOrigin.Player(inv,weapon.Item));
                hitCount++;
            }
            DungeonBreakable.ServerMelee(origin,MeleeAngle.Value,weapon.Range,weapon.StillHalfAngle,gameObject.scene);
            meleeCandidates.Clear();
        }

        bool MeleeTarget(Health target,WeaponDefinition weapon)
        {
            if(target==null || !target.IsSpawned || target.IsDead || target.NetworkObject==NetworkObject || target.gameObject.scene!=gameObject.scene) return false;
            var participant=target.GetComponent<PlayerInventory>();
            if(participant!=null ? !participant.IsAlive : target.GetComponent<Monster>()==null && target.GetComponent<PlantCreature>()==null) return false;
            Vector2 origin=transform.position,point=target.transform.position,delta=point-origin;
            if(delta.sqrMagnitude>weapon.Range*weapon.Range) return false;
            if(delta.sqrMagnitude>.0001f && Mathf.Abs(Mathf.DeltaAngle(MeleeAngle.Value,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg))>weapon.StillHalfAngle) return false;
            var wall=Physics2D.Linecast(origin,point,GameConfig.WallMask).collider;
            return wall==null || wall.GetComponentInParent<Health>()==target;
        }
    }
}
