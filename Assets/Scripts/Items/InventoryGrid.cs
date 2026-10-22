using System;
using Unity.Netcode;

namespace CyberExtraction.Items
{
    [Serializable]
    public struct ItemStack : INetworkSerializable, IEquatable<ItemStack>
    {
        public ItemId Item;
        public int Quantity, Data;
        public byte Quality;
        public ItemInstance Instance;
        public bool Empty => Quantity <= 0;
        public ItemStack(ItemId item, int quantity, byte quality = 0, int data = 0)
        { Item = item; Quantity = quantity; Quality = quality; Data = data;
            Instance=quantity>0 && EquipmentCatalog.IsDefined(item) ? EquipmentCatalog.RollInstance(item,ItemRarity.Common) :
                quantity>0 && (ItemCatalog.Weapon(item) || ItemCatalog.Equipment(item)) ? ItemInstance.Create() : default; }
        public bool Same(ItemStack other) => Item == other.Item && Quality == other.Quality && Data == other.Data && Instance.Equals(other.Instance);
        public bool Equals(ItemStack other) => Same(other) && Quantity == other.Quantity;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Item); serializer.SerializeValue(ref Quantity);
            serializer.SerializeValue(ref Data); serializer.SerializeValue(ref Quality);
            Instance.NetworkSerialize(serializer);
        }
    }

    [Serializable]
    public class InventoryGrid
    {
        public ItemStack[] Slots;
        public InventoryGrid() : this(ItemCatalog.InventorySlots) {}
        public InventoryGrid(int capacity) { Slots=new ItemStack[capacity]; }
        public InventoryGrid Copy() => new InventoryGrid { Slots = (ItemStack[])Slots.Clone() };
        public bool TryOrganize(out bool changed)
        {
            changed=false;
            if(Slots==null) return false;
            var ordered=(ItemStack[])Slots.Clone();
            // Stable insertion sort keeps variants and unique instances in their original order.
            for(int i=1;i<ordered.Length;i++)
            {
                var item=ordered[i]; int j=i-1;
                while(j>=0 && CompareOrganization(ordered[j],item)>0) { ordered[j+1]=ordered[j]; j--; }
                ordered[j+1]=item;
            }
            var result=new ItemStack[Slots.Length]; int used=0;
            foreach(var item in ordered)
            {
                if(item.Empty) continue;
                if(!ItemCatalog.Valid(item.Item) || item.Instance.HasIdentity && item.Quantity!=1) return false;
                int limit=item.Instance.HasIdentity ? 1 : ItemCatalog.Limit(item.Item);
                if(limit<=0) return false;
                int remaining=item.Quantity;
                if(!item.Instance.HasIdentity && !ItemCatalog.Weapon(item.Item))
                    for(int i=0;i<used && remaining>0;i++)
                    {
                        var existing=result[i];
                        if(!existing.Same(item)) continue;
                        int amount=Math.Min(remaining,Math.Max(0,limit-existing.Quantity));
                        existing.Quantity+=amount; remaining-=amount; result[i]=existing;
                    }
                while(remaining>0)
                {
                    if(used>=result.Length) return false;
                    var part=item; part.Quantity=Math.Min(limit,remaining);
                    result[used++]=part; remaining-=part.Quantity;
                }
            }
            for(int i=0;i<Slots.Length;i++) if(!Slots[i].Equals(result[i])) { changed=true; break; }
            if(changed) Slots=result;
            return true;
        }
        static int CompareOrganization(ItemStack a,ItemStack b)
        {
            if(a.Empty || b.Empty) return a.Empty==b.Empty ? 0 : a.Empty ? 1 : -1;
            int category=OrganizationCategory(a.Item).CompareTo(OrganizationCategory(b.Item));
            return category!=0 ? category : ((byte)a.Item).CompareTo((byte)b.Item);
        }
        static int OrganizationCategory(ItemId item)
        {
            if(ItemCatalog.Weapon(item)) return 0;
            if(ItemCatalog.Equipment(item)) return 1;
            if(ItemCatalog.Ammo(item)) return 2;
            if(item==ItemId.Bandage) return 3;
            if(ItemCatalog.Fish(item) || item==ItemId.CookedFish || item==ItemId.BurntFish || item==ItemId.RawFish) return 4;
            if(item==ItemId.Lockpick || ItemCatalog.PassageTool(item) || ItemCatalog.ExpeditionOnly(item)) return 5;
            return TreasureCatalog.IsDefined(item) ? 6 : 7;
        }
        public int Count(ItemId item) { int count = 0; foreach (var stack in Slots) if (!stack.Empty && stack.Item == item) count += stack.Quantity; return count; }
        public int FreeFor(ItemStack stack)
        {
            if (!ItemCatalog.Available(stack.Item)) return 0;
            int capacity = 0, limit = stack.Instance.HasIdentity ? 1 : ItemCatalog.Limit(stack.Item);
            foreach (var existing in Slots)
                if (existing.Empty) capacity += limit;
                else if (!stack.Instance.HasIdentity && !ItemCatalog.Weapon(stack.Item) && existing.Same(stack)) capacity += limit - existing.Quantity;
            return capacity;
        }
        public bool Add(ItemStack stack)
        {
            if(!EquipmentCatalog.ValidInstance(stack) || !RecoveryRules.Valid(stack)) return false;
            if(!stack.Empty && (ItemCatalog.Weapon(stack.Item) || ItemCatalog.Equipment(stack.Item)) && !stack.Instance.HasIdentity) stack.Instance=ItemInstance.Create();
            if(stack.Instance.HasIdentity)
            {
                if(stack.Quantity!=1) return false;
                foreach(var existing in Slots)
                    if(!existing.Empty && existing.Instance.SameIdentity(stack.Instance)) return false;
            }
            if (stack.Empty || !ItemCatalog.Available(stack.Item) || FreeFor(stack) < stack.Quantity) return false;
            int remaining = stack.Quantity, limit = stack.Instance.HasIdentity ? 1 : ItemCatalog.Limit(stack.Item);
            if (!stack.Instance.HasIdentity && !ItemCatalog.Weapon(stack.Item))
                for (int i = 0; i < Slots.Length && remaining > 0; i++)
                {
                    var existing = Slots[i];
                    if (existing.Empty || !existing.Same(stack)) continue;
                    int amount = Math.Min(remaining, limit - existing.Quantity);
                    existing.Quantity += amount; remaining -= amount; Slots[i] = existing;
                }
            for (int i = 0; i < Slots.Length && remaining > 0; i++)
                if (Slots[i].Empty) { var part = stack; part.Quantity = Math.Min(limit, remaining); Slots[i] = part; remaining -= part.Quantity; }
            return true;
        }
        public bool Remove(ItemId item, int quantity)
        {
            if (quantity < 0 || Count(item) < quantity) return false;
            for (int i = 0; i < Slots.Length && quantity > 0; i++)
            {
                var stack = Slots[i]; if (stack.Empty || stack.Item != item) continue;
                int amount = Math.Min(stack.Quantity, quantity); stack.Quantity -= amount; quantity -= amount;
                Slots[i] = stack.Empty ? default : stack;
            }
            return true;
        }
        public bool MoveTo(int source, InventoryGrid destination, int target)
        {
            if (source < 0 || source >= Slots.Length || target < 0 || target >= destination.Slots.Length ||
                Slots[source].Empty || (ReferenceEquals(this, destination) && source == target)) return false;
            var moving = Slots[source]; var existing = destination.Slots[target];
            if(!existing.Empty && moving.Instance.SameIdentity(existing.Instance)) return false;
            if (!existing.Empty && existing.Same(moving) && !ItemCatalog.Weapon(moving.Item))
            {
                int amount = Math.Min(moving.Quantity, ItemCatalog.Limit(moving.Item) - existing.Quantity);
                if (amount <= 0) return false;
                moving.Quantity -= amount; existing.Quantity += amount;
                Slots[source] = moving.Empty ? default : moving; destination.Slots[target] = existing;
            }
            else { Slots[source] = existing; destination.Slots[target] = moving; }
            return true;
        }
        public static InventoryGrid Starter(bool includeLockpicks = false)
        {
            var grid = new InventoryGrid(ItemCatalog.BackpackSlots);
            foreach(var item in StarterEquipment.Create()) grid.Add(item);
            if (includeLockpicks) grid.Add(new ItemStack(ItemId.Lockpick, 2));
            return grid;
        }
    }
    public static class StarterEquipment
    {
        public const int BoltCount=5;
        public static ItemStack[] Create(bool recovery=false)
        {
            // One bolt loaded in the crossbow, plus five spare bolts for reloading.
            var items=new[] {new ItemStack(ItemId.ImprovisedHandCrossbow,1,data:1),
                new ItemStack(ItemId.RustySword,1),new ItemStack(ItemId.CrossbowBolt,BoltCount)};
            if(recovery) for(int i=0;i<items.Length;i++) items[i].Instance.Origin=ItemOrigin.Recovery;
            return items;
        }
    }
}
