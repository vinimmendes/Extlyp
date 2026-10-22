using System;
using System.Collections.Generic;
using Unity.Netcode;
using CyberExtraction.Items;

namespace CyberExtraction.Combat
{
    // Local storage only: never a NetworkVariable/NetworkList serialized on spawn.
    // On the server this is authoritative; clients receive only authorized copies.
    public sealed class LootContents
    {
        readonly List<ItemStack> slots=new List<ItemStack>();
        readonly Action changed;
        public event Action<NetworkListEvent<ItemStack>> OnListChanged;
        internal LootContents(Action changed) { this.changed=changed; }
        public int Count => slots.Count;
        public ItemStack this[int index]
        {
            get => slots[index];
            internal set { slots[index]=value; Notify(); }
        }
        internal void Add(ItemStack stack) { slots.Add(stack); Notify(); }
        internal void Replace(ItemStack[] values) { slots.Clear(); slots.AddRange(values); Notify(); }
        internal void Clear() { slots.Clear(); Notify(); }
        internal ItemStack[] ToArray() => slots.ToArray();
        void Notify()
        {
            changed?.Invoke();
            // Existing inventory presenters consume the same event shape locally.
            if(OnListChanged==null) return;
            foreach(Action<NetworkListEvent<ItemStack>> listener in OnListChanged.GetInvocationList())
                try { listener(new NetworkListEvent<ItemStack> { Type=NetworkListEvent<ItemStack>.EventType.Full }); }
                catch(Exception error) { UnityEngine.Debug.LogException(error); }
        }
    }
}
