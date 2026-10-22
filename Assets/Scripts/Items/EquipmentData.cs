using System;
using Unity.Netcode;
using CyberExtraction.Core;

namespace CyberExtraction.Items
{
    public enum EquipmentSlot : byte { None, Head, Torso, Hands, Feet, Amulet, Ring, WeaponA, WeaponB }
    public enum ItemOrigin : byte { Normal, Recovery }
    public enum ItemRarity : byte { Common, Uncommon, Rare }

    // Unmanaged value type so ItemStack remains compatible with NetworkList<T>.
    // Two serialized halves preserve the GUID through JSON and network transfers.
    [Serializable]
    public struct ItemInstance : INetworkSerializable, IEquatable<ItemInstance>
    {
        public ulong IdLow, IdHigh;
        public AttributeValues Bonuses;
        public ItemOrigin Origin;
        public ItemRarity Rarity;
        // Loaded recovery ammunition stays bound even inside a normal weapon.
        public int RecoveryRounds;
        public bool HasIdentity => IdLow!=0 || IdHigh!=0;
        public static ItemInstance Create()
        {
            byte[] bytes=Guid.NewGuid().ToByteArray();
            return new ItemInstance { IdLow=BitConverter.ToUInt64(bytes,0),IdHigh=BitConverter.ToUInt64(bytes,8) };
        }
        public bool SameIdentity(ItemInstance other) => HasIdentity && IdLow==other.IdLow && IdHigh==other.IdHigh;
        public bool Equals(ItemInstance other) => IdLow==other.IdLow && IdHigh==other.IdHigh &&
            Bonuses.Equals(other.Bonuses) && Origin==other.Origin && Rarity==other.Rarity && RecoveryRounds==other.RecoveryRounds;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T:IReaderWriter
        {
            serializer.SerializeValue(ref IdLow); serializer.SerializeValue(ref IdHigh);
            Bonuses.NetworkSerialize(serializer);
            serializer.SerializeValue(ref Origin);
            serializer.SerializeValue(ref Rarity);
            serializer.SerializeValue(ref RecoveryRounds);
        }
    }
}
