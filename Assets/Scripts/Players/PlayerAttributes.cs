using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Items;
using CyberExtraction.Match;

namespace CyberExtraction.Players
{
    public partial class PlayerInventory
    {
        public NetworkVariable<AttributeValues> AttributeInvestment=new NetworkVariable<AttributeValues>(default,NetworkVariableReadPermission.Owner);
        // Only this derived public value is needed by observers; equipment remains private.
        public NetworkVariable<float> VisionOpening=new NetworkVariable<float>(0);
        public NetworkVariable<bool> WellFed=new NetworkVariable<bool>(false,NetworkVariableReadPermission.Owner);
        int foodHealthBonus;
        public int BaseAttribute(AttributeId id) => 1+CharacterAttributeRules.Investment(AttributeInvestment.Value,id);
        public long TotalAttribute(AttributeId id)
        {
            if(id==AttributeId.Intelligence) return 1;
            long total=BaseAttribute(id);
            for(int i=0;i<Mathf.Min(Equipment.Count,EquipmentLoadout.WeaponA);i++)
                if(!Equipment[i].Empty) total+=Equipment[i].Instance.Bonuses.Get(id);
            return total;
        }
        public int EffectiveAttribute(AttributeId id) => CharacterAttributeRules.Effective(TotalAttribute(id));
        public float MeleeDamageMultiplier => CharacterAttributeRules.MeleeMultiplier(EffectiveAttribute(AttributeId.Strength));
        public float AimDispersionMultiplier => CharacterAttributeRules.AimMultiplier(EffectiveAttribute(AttributeId.Dexterity));
        public float StaminaCapacity => CharacterAttributeRules.Stamina(EffectiveAttribute(AttributeId.Vigor));
        public float PhysicalDamageReduction
        {
            get
            {
                float total=0;
                for(int i=0;i<Mathf.Min(Equipment.Count,2);i++) total+=EquipmentCatalog.PhysicalProtection(Equipment[i]);
                return Mathf.Clamp(total,0,EquipmentCatalog.MaximumPhysicalProtection);
            }
        }
        public float VisionAngle => VisionOpening.Value>0 ? VisionOpening.Value : CharacterAttributeRules.VisionAngle(1,MapBuilder.Current==ExpeditionMap.Dungeon);
        public float VisionRadiusAtAngle(float offset) => GameConfig.VisionRadiusAtAngle(offset,VisionAngle);

        void ServerRefreshAttributes()
        {
            if(!IsServer) return;
            VisionOpening.Value=CharacterAttributeRules.VisionAngle(EffectiveAttribute(AttributeId.Perception),MapBuilder.Current==ExpeditionMap.Dungeon);
            if(health!=null)
            {
                int maximum=CharacterAttributeRules.Health(EffectiveAttribute(AttributeId.Vitality))+foodHealthBonus;
                health.maxHealth=maximum; health.Maximum.Value=maximum;
                health.Current.Value=Mathf.Min(health.Current.Value,maximum);
            }
            GetComponent<PlayerController>()?.ServerSetMaxStamina(StaminaCapacity);
        }
    }
}
