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
        public bool Empty => Quantity <= 0;
        public ItemStack(ItemId item, int quantity, byte quality = 0, int data = 0)
        { Item = item; Quantity = quantity; Quality = quality; Data = data; }
        public bool Same(ItemStack other) => Item == other.Item && Quality == other.Quality && Data == other.Data;
        public bool Equals(ItemStack other) => Same(other) && Quantity == other.Quantity;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Item); serializer.SerializeValue(ref Quantity);
            serializer.SerializeValue(ref Data); serializer.SerializeValue(ref Quality);
        }
    }

    [Serializable]
    public class InventoryGrid
    {
        public ItemStack[] Slots = new ItemStack[ItemCatalog.InventorySlots];
        public InventoryGrid Copy() => new InventoryGrid { Slots = (ItemStack[])Slots.Clone() };
        public int Count(ItemId item) { int count = 0; foreach (var stack in Slots) if (!stack.Empty && stack.Item == item) count += stack.Quantity; return count; }
        public int FreeFor(ItemStack stack)
        {
            if (!ItemCatalog.Valid(stack.Item)) return 0;
            int capacity = 0, limit = ItemCatalog.Limit(stack.Item);
            foreach (var existing in Slots)
                if (existing.Empty) capacity += limit;
                else if (!ItemCatalog.Weapon(stack.Item) && existing.Same(stack)) capacity += limit - existing.Quantity;
            return capacity;
        }
        public bool Add(ItemStack stack)
        {
            if (stack.Empty || !ItemCatalog.Valid(stack.Item) || FreeFor(stack) < stack.Quantity) return false;
            int remaining = stack.Quantity, limit = ItemCatalog.Limit(stack.Item);
            if (!ItemCatalog.Weapon(stack.Item))
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
        public static InventoryGrid Starter(bool includeLockpicks = true)
        {
            var grid = new InventoryGrid();
            grid.Add(new ItemStack(ItemId.Pistol, 1, 0, CyberExtraction.Core.GameplayDefinition.Current.Magazine));
            grid.Add(new ItemStack(ItemId.PistolAmmo, 24));
            if (includeLockpicks) grid.Add(new ItemStack(ItemId.Lockpick, 2));
            return grid;
        }
    }
}
