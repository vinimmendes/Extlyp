using System;

namespace CyberExtraction.Items
{
    [Serializable]
    public sealed class EquipmentLoadout
    {
        public const int Count=8, WeaponA=6, WeaponB=7;
        public ItemStack[] Slots=new ItemStack[Count];
        public int ActiveWeaponSlot=WeaponA;
        public EquipmentLoadout Copy() => new EquipmentLoadout { Slots=(ItemStack[])Slots.Clone(),ActiveWeaponSlot=ActiveWeaponSlot };
        public static bool Accepts(int slot,ItemStack item) => slot>=0 && slot<Count &&
            (item.Empty || ItemCatalog.Available(item.Item) && item.Quantity==1 && item.Instance.HasIdentity && EquipmentCatalog.ValidInstance(item) && RecoveryRules.Valid(item) &&
                (slot>=WeaponA ? ItemCatalog.Weapon(item.Item) : ItemCatalog.EquipmentSlotFor(item.Item)==(EquipmentSlot)(slot+1)));
        public bool Exchange(InventoryGrid pack,int bagSlot,int equippedSlot)
        {
            if(pack==null || bagSlot<0 || bagSlot>=pack.Slots.Length || equippedSlot<0 || equippedSlot>=Count) return false;
            var incoming=pack.Slots[bagSlot]; var outgoing=Slots[equippedSlot];
            if(incoming.Empty && outgoing.Empty || !Accepts(equippedSlot,incoming)) return false;
            if(!incoming.Empty)
                for(int i=0;i<Count;i++) if(i!=equippedSlot && !Slots[i].Empty && Slots[i].Instance.SameIdentity(incoming.Instance)) return false;
            if(!incoming.Empty && !outgoing.Empty && incoming.Instance.SameIdentity(outgoing.Instance)) return false;
            pack.Slots[bagSlot]=outgoing; Slots[equippedSlot]=incoming;
            if(!AcceptsWeapon(ActiveWeaponSlot) || Slots[ActiveWeaponSlot].Empty)
                ActiveWeaponSlot=!Slots[WeaponA].Empty ? WeaponA : WeaponB;
            return true;
        }
        public static bool AcceptsWeapon(int slot) => slot==WeaponA || slot==WeaponB;
        public void FillWeapons(InventoryGrid pack)
        {
            for(int slot=WeaponA;slot<=WeaponB;slot++)
                if(Slots[slot].Empty)
                    for(int i=0;i<pack.Slots.Length;i++)
                        if(!pack.Slots[i].Empty && ItemCatalog.Weapon(pack.Slots[i].Item)) { Exchange(pack,i,slot); break; }
        }
        public static string Name(int slot)
        {
            switch(slot)
            {
                case 0:return "Cabeça"; case 1:return "Torso"; case 2:return "Mãos";
                case 3:return "Pés"; case 4:return "Amuleto"; case 5:return "Anel";
                case 6:return "Arma A"; case 7:return "Arma B"; default:return "?";
            }
        }
    }
}
