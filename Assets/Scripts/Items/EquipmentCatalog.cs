using System;
using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Items
{
    public static class EquipmentCatalog
    {
        public const int ModelCount=18;
        public const float MaximumPhysicalProtection=.15f;
        static ItemDefinition[] models;
        public static bool IsDefined(ItemId item) => item>=ItemId.WatchmanHood && item<=ItemId.PersistenceRing;
        public static ItemDefinition Get(ItemId item)
        {
            if(!IsDefined(item)) return null;
            if(models==null)
            {
                var loaded=new ItemDefinition[ModelCount];
                foreach(var model in Resources.LoadAll<ItemDefinition>("Items/Equipment/Definitions"))
                {
                    if(!IsDefined(model.Id)) throw new InvalidOperationException("Modelo de equipamento fora do catalogo: "+model.name);
                    int index=(int)model.Id-(int)ItemId.WatchmanHood;
                    if(loaded[index]!=null || model.Category!=ItemCategory.Equipment || model.Slot!=(EquipmentSlot)(index/3+1) ||
                        string.IsNullOrEmpty(model.DisplayName) || (byte)model.PrimaryAttribute>=5 || model.SecondaryPool==null || model.SecondaryPool.Length!=2 ||
                        (byte)model.SecondaryPool[0]>=5 || (byte)model.SecondaryPool[1]>=5 || model.SecondaryPool[0]==model.SecondaryPool[1] ||
                        model.SecondaryPool[0]==model.PrimaryAttribute || model.SecondaryPool[1]==model.PrimaryAttribute)
                        throw new InvalidOperationException("Definicao de equipamento invalida: "+model.name);
                    loaded[index]=model;
                }
                for(int i=0;i<loaded.Length;i++) if(loaded[i]==null)
                    throw new InvalidOperationException("Modelo de equipamento ausente: "+((ItemId)((int)ItemId.WatchmanHood+i)));
                models=loaded;
            }
            return models[(int)item-(int)ItemId.WatchmanHood];
        }
        public static ItemStack Create(ItemId item,ItemRarity rarity) =>
            new ItemStack { Item=item,Quantity=1,Instance=RollInstance(item,rarity) };
        public static ItemInstance RollInstance(ItemId item,ItemRarity rarity)
        {
            var model=Get(item);
            if(model==null || (byte)rarity>(byte)ItemRarity.Rare) throw new ArgumentException("Equipamento ou raridade invalida.");
            var instance=ItemInstance.Create(); instance.Rarity=rarity;
            int minimum=1+(int)rarity;
            instance.Bonuses.Set(model.PrimaryAttribute,UnityEngine.Random.Range(minimum,minimum+2));
            if(rarity!=ItemRarity.Common)
                instance.Bonuses.Set(model.SecondaryPool[UnityEngine.Random.Range(0,2)],rarity==ItemRarity.Uncommon ? 1 : UnityEngine.Random.Range(1,3));
            return instance;
        }
        public static bool ValidInstance(ItemStack stack)
        {
            if(!IsDefined(stack.Item)) return true;
            if(stack.Quantity!=1 || !stack.Instance.HasIdentity || stack.Data!=0 || stack.Quality!=0 || stack.Instance.RecoveryRounds!=0 ||
                (byte)stack.Instance.Rarity>2 || (byte)stack.Instance.Origin>(byte)ItemOrigin.Recovery) return false;
            var model=Get(stack.Item); var bonuses=stack.Instance.Bonuses; int rarity=(int)stack.Instance.Rarity;
            int main=bonuses.Get(model.PrimaryAttribute),secondaryCount=0;
            if(main<1+rarity || main>2+rarity) return false;
            for(int i=0;i<6;i++)
            {
                var id=(AttributeId)i; int value=bonuses.Get(id);
                if(id==model.PrimaryAttribute || value==0) continue;
                if(rarity==0 || (id!=model.SecondaryPool[0] && id!=model.SecondaryPool[1]) || value<1 || value>(rarity==1 ? 1 : 2)) return false;
                secondaryCount++;
            }
            return secondaryCount==(rarity==0 ? 0 : 1);
        }
        public static float PhysicalProtection(ItemStack stack)
        {
            if(stack.Empty || !IsDefined(stack.Item) || !ValidInstance(stack)) return 0;
            var slot=Get(stack.Item).Slot;
            return slot==EquipmentSlot.Head ? .02f+.01f*(int)stack.Instance.Rarity :
                slot==EquipmentSlot.Torso ? .04f+.02f*(int)stack.Instance.Rarity : 0;
        }
        public static string RarityName(ItemRarity rarity) => rarity==ItemRarity.Common ? "Comum" : rarity==ItemRarity.Uncommon ? "Incomum" : "Raro";
        public static Color RarityColor(ItemRarity rarity) => rarity==ItemRarity.Rare ? new Color(.65f,.74f,.88f) :
            rarity==ItemRarity.Uncommon ? new Color(.65f,.8f,.57f) : new Color(.87f,.84f,.74f);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { models=null; }
    }
}
