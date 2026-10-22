using System;
using Unity.Netcode;

namespace CyberExtraction.Core
{
    // Stable values: these IDs are serialized in saves and equipment definitions.
    public enum AttributeId : byte { Strength, Dexterity, Vigor, Perception, Vitality, Intelligence }

    [Serializable]
    public struct AttributeValues : INetworkSerializable, IEquatable<AttributeValues>
    {
        public int Strength, Dexterity, Vigor, Perception, Vitality, Intelligence;
        public int Get(AttributeId id)
        {
            switch(id)
            {
                case AttributeId.Strength: return Strength;
                case AttributeId.Dexterity: return Dexterity;
                case AttributeId.Vigor: return Vigor;
                case AttributeId.Perception: return Perception;
                case AttributeId.Vitality: return Vitality;
                case AttributeId.Intelligence: return Intelligence;
                default: throw new ArgumentOutOfRangeException(nameof(id));
            }
        }
        public void Set(AttributeId id,int value)
        {
            switch(id)
            {
                case AttributeId.Strength: Strength=value; break;
                case AttributeId.Dexterity: Dexterity=value; break;
                case AttributeId.Vigor: Vigor=value; break;
                case AttributeId.Perception: Perception=value; break;
                case AttributeId.Vitality: Vitality=value; break;
                case AttributeId.Intelligence: Intelligence=value; break;
                default: throw new ArgumentOutOfRangeException(nameof(id));
            }
        }
        public bool Equals(AttributeValues other) => Strength==other.Strength && Dexterity==other.Dexterity &&
            Vigor==other.Vigor && Perception==other.Perception && Vitality==other.Vitality && Intelligence==other.Intelligence;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T:IReaderWriter
        {
            serializer.SerializeValue(ref Strength); serializer.SerializeValue(ref Dexterity);
            serializer.SerializeValue(ref Vigor); serializer.SerializeValue(ref Perception);
            serializer.SerializeValue(ref Vitality); serializer.SerializeValue(ref Intelligence);
        }
    }

    [Serializable]
    public sealed class CharacterProgress
    {
        public long Experience, Coins;
        public int Level=1, UnspentPoints;
        public AttributeValues Invested;
        // Investment and bonuses are stored as deltas; every attribute starts at 1.
        public int BaseAttribute(AttributeId id) => 1+CharacterAttributeRules.Investment(Invested,id);
        public void Normalize()
        {
            if(Coins<0) throw new InvalidOperationException("Saldo de moedas invalido.");
            Experience=Math.Max(0,Math.Min(CharacterProgressionRules.MaxExperience,Experience));
            Level=CharacterProgressionRules.LevelAt(Experience);
            int spent=0;
            for(int i=0;i<6;i++)
            {
                int value=Invested.Get((AttributeId)i);
                if(value<0 || value>CharacterAttributeRules.MaxInvestment || i==5 && value!=0)
                    throw new InvalidOperationException("Investimentos de personagem invalidos.");
                spent+=value;
            }
            if(spent>Level-1) throw new InvalidOperationException("Pontos investidos excedem os niveis concedidos.");
            UnspentPoints=Level-1-spent;
        }
        public long GainExperience(long amount)
        {
            Normalize(); long before=Experience;
            Experience+=Math.Min(Math.Max(0,amount),CharacterProgressionRules.MaxExperience-Experience);
            Normalize(); return Experience-before;
        }
        public bool Invest(AttributeId id)
        {
            Normalize();
            if((byte)id>=(byte)AttributeId.Intelligence || UnspentPoints<=0 || Invested.Get(id)>=CharacterAttributeRules.MaxInvestment) return false;
            Invested.Set(id,Invested.Get(id)+1); Normalize(); return true;
        }
    }
}
