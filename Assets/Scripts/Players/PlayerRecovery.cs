using UnityEngine;
using CyberExtraction.Items;

namespace CyberExtraction.Players
{
    public partial class PlayerInventory
    {
        public bool ServerLoadRounds(int amount)
        {
            var weapon=ActiveWeapon;
            if(!IsServer || weapon==null || weapon.IsMelee || amount<=0 || amount>weapon.Capacity-WeaponRounds) return false;
            var grid=Grid(); if(grid.Count(weapon.Ammo)<amount) return false;
            int remaining=amount,bound=0;
            foreach(var ammo in grid.Slots)
            {
                if(ammo.Empty || ammo.Item!=weapon.Ammo) continue;
                int taken=Mathf.Min(remaining,ammo.Quantity);
                if(ammo.Instance.Origin==ItemOrigin.Recovery) bound+=taken;
                remaining-=taken; if(remaining==0) break;
            }
            if(!grid.Remove(weapon.Ammo,amount)) return false;
            var stack=Equipment[ActiveWeaponSlot]; stack.Data+=amount; stack.Instance.RecoveryRounds+=bound;
            Equipment[ActiveWeaponSlot]=stack; ServerSetGrid(grid); return true;
        }
    }
}
