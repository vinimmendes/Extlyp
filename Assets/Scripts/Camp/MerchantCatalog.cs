using System;
using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Items;

namespace CyberExtraction.Camp
{
    public enum MerchantGroup { Consumable,Weapon,Equipment,Tool }
    [Serializable] public sealed class MerchantEntry
    {
        public ItemId Item;
        public MerchantGroup Group;
        public int Price,Stock;
    }
    [CreateAssetMenu(menuName="Cyber Extraction/Merchant Catalog")]
    public sealed class MerchantCatalog : ScriptableObject
    {
        public MerchantEntry[] Entries;
        public int Weapons=2,Equipment=4,Tools=2;
        static MerchantCatalog current;
        public static MerchantCatalog Current => current!=null ? current : current=Resources.Load<MerchantCatalog>("MerchantCatalog");
        public MerchantState CreateStock()
        {
            if(Entries==null || Weapons!=2 || Equipment!=4 || Tools!=2) throw new InvalidOperationException("Catalogo do comerciante invalido.");
            var offers=new List<MerchantOffer>();
            var weapons=new List<MerchantEntry>(); var tools=new List<MerchantEntry>();
            var equipment=new List<MerchantEntry>(); var ids=new HashSet<ItemId>();
            foreach(var entry in Entries)
            {
                if(entry==null || !ItemCatalog.Available(entry.Item) || entry.Price<=0 || entry.Stock<=0 || !ids.Add(entry.Item)) throw new InvalidOperationException("Oferta invalida.");
                if(entry.Group==MerchantGroup.Consumable) offers.Add(CreateOffer(entry));
                else if(entry.Group==MerchantGroup.Weapon && ItemCatalog.Weapon(entry.Item)) weapons.Add(entry);
                else if(entry.Group==MerchantGroup.Equipment && EquipmentCatalog.IsDefined(entry.Item)) equipment.Add(entry);
                else if(entry.Group==MerchantGroup.Tool && (entry.Item==ItemId.Lockpick || entry.Item==ItemId.BoltCutters || entry.Item==ItemId.Crowbar || entry.Item==ItemId.HandSaw)) tools.Add(entry);
                else throw new InvalidOperationException("Grupo de mercadoria invalido.");
            }
            if(offers.Count!=2 || weapons.Count<Weapons || tools.Count<Tools) throw new InvalidOperationException("Catalogo incompleto.");
            Draw(weapons,Weapons,offers); Draw(tools,Tools,offers);
            var categories=new List<int> {0,1,2,3,4,5};
            for(int i=0;i<Equipment;i++)
            {
                int at=UnityEngine.Random.Range(0,categories.Count),category=categories[at]; categories.RemoveAt(at);
                var candidates=equipment.FindAll(e=>(int)ItemCatalog.EquipmentSlotFor(e.Item)==category+1);
                if(candidates.Count==0) throw new InvalidOperationException("Categoria de equipamento sem modelos.");
                offers.Add(CreateOffer(candidates[UnityEngine.Random.Range(0,candidates.Count)]));
            }
            // Consumables, weapons, equipment, tools: a stable visual grouping per cycle.
            var ordered=new List<MerchantOffer>();
            foreach(var offer in offers) if(offer.Group==MerchantGroup.Consumable) ordered.Add(offer);
            foreach(var offer in offers) if(offer.Group==MerchantGroup.Weapon) ordered.Add(offer);
            foreach(var offer in offers) if(offer.Group==MerchantGroup.Equipment) ordered.Add(offer);
            foreach(var offer in offers) if(offer.Group==MerchantGroup.Tool) ordered.Add(offer);
            return new MerchantState {Cycle=Guid.NewGuid().ToString("N"),Offers=ordered.ToArray()};
        }
        static void Draw(List<MerchantEntry> pool,int count,List<MerchantOffer> output)
        { for(int i=0;i<count;i++) { int at=UnityEngine.Random.Range(0,pool.Count); output.Add(CreateOffer(pool[at])); pool.RemoveAt(at); } }
        public MerchantState RemoveRetiredOffers(MerchantState old)
        {
            if(MerchantState.IsAbsent(old)) return CreateStock();
            if(!Array.Exists(old.Offers,offer=>ItemCatalog.Retired(offer.Item.Item))) return old;
            var fresh=CreateStock(); var offers=new List<MerchantOffer>(); var used=new HashSet<ItemId>();
            int[] limits={2,2,4,2};
            for(int group=0;group<4;group++)
            {
                int count=0;
                foreach(var offer in old.Offers)
                    if((int)offer.Group==group && ItemCatalog.Available(offer.Item.Item) && count<limits[group] && used.Add(offer.Item.Item))
                    {offers.Add(offer);count++;}
                foreach(var offer in fresh.Offers)
                    if((int)offer.Group==group && count<limits[group] && used.Add(offer.Item.Item))
                    {offers.Add(offer);count++;}
                if(count!=limits[group]) throw new InvalidOperationException("Migracao da vitrine incompleta.");
            }
            fresh.Offers=offers.ToArray();fresh.LastRestockedExpedition=old.LastRestockedExpedition;
            return fresh;
        }
        static MerchantOffer CreateOffer(MerchantEntry entry)
        {
            var stack=EquipmentCatalog.IsDefined(entry.Item) ? EquipmentCatalog.Create(entry.Item,ItemRarity.Common) : new ItemStack(entry.Item,1);
            return new MerchantOffer {Item=stack,Group=entry.Group,Price=entry.Price,Remaining=entry.Stock,InitialStock=entry.Stock};
        }
    }
    [Serializable] public sealed class MerchantOffer
    {
        public ItemStack Item;
        public MerchantGroup Group;
        public int Price,Remaining,InitialStock;
    }
    [Serializable] public sealed class MerchantState
    {
        public string Cycle,LastRestockedExpedition;
        public MerchantOffer[] Offers;
        // JsonUtility can materialize a missing/null optional inline object as
        // an instance with null strings and a null/empty array. Only that exact
        // empty shape represents absence; partial stock remains invalid.
        public static bool IsAbsent(MerchantState state)=>state==null ||
            string.IsNullOrEmpty(state.Cycle) && string.IsNullOrEmpty(state.LastRestockedExpedition) &&
            (state.Offers==null || state.Offers.Length==0);
    }
    public partial class CampProgress
    {
        public MerchantState Merchant,TestMerchant;
        public void RestockMerchant(string expedition)
        {
            if(string.IsNullOrEmpty(expedition) || Merchant!=null && Merchant.LastRestockedExpedition==expedition) return;
            Merchant=MerchantCatalog.Current.CreateStock(); Merchant.LastRestockedExpedition=expedition;
        }
        // Ground is legacy migration storage only. No gameplay action appends to it.
        public void DeliverLegacyItems()
        {
            if(ExpeditionState!=0 || Ground==null) return;
            for(int i=0;i<Ground.Count;)
            {
                var item=Ground[i].Stack;
                if(Chest.Add(item) || Pack.Add(item)) Ground.RemoveAt(i); else i++;
            }
        }
        public bool CanStoreCooking(int slot)
        {
            if(slot<0 || slot>=Pack.Slots.Length) return false;
            var pack=Pack.Copy(); var fish=pack.Slots[slot]; if(fish.Empty) return false;
            fish.Quantity--; pack.Slots[slot]=fish.Empty ? default : fish;
            // A reserved empty slot accepts every possible quality/species result.
            return Array.Exists(pack.Slots,s=>s.Empty) || Array.Exists(Chest.Slots,s=>s.Empty);
        }
        public bool CanPrepareRecoveryEquipment()
        {
            if(HasAnyWeapon) return true;
            var pack=Pack.Copy(); var chest=Chest.Copy();
            foreach(var stack in StarterEquipment.Create(true))
            {
                while(pack.FreeFor(stack)<stack.Quantity)
                {
                    int slot=Array.FindLastIndex(pack.Slots,item=>!item.Empty && !ItemCatalog.Weapon(item.Item) && !item.Same(stack));
                    if(slot<0 || !chest.Add(pack.Slots[slot])) return false;
                    pack.Slots[slot]=default;
                }
                if(!pack.Add(stack)) return false;
            }
            return true;
        }
    }
    public static partial class CampRepository
    {
        static MerchantState CopyMerchant(MerchantState state)=>JsonUtility.FromJson<MerchantState>(JsonUtility.ToJson(state));
        public static MerchantState GetTestMerchant(string session,string owner)=>Transaction(data=>
        {
            var profile=OwnedRecord(data,session,owner).Progress;
            if(MerchantState.IsAbsent(profile.TestMerchant)) { profile.TestMerchant=MerchantCatalog.Current.CreateStock(); profile.Revision++; }
            return CopyMerchant(profile.TestMerchant);
        });
        public static void SaveTestMerchant(string session,string owner,MerchantState state)=>Transaction(data=>
        {
            var profile=OwnedRecord(data,session,owner).Progress;
            profile.TestMerchant=CopyMerchant(state); profile.Revision++; return 0;
        });
        public static void RestockTestMerchant(string session,string owner,string expedition)=>Transaction(data=>
        {
            var profile=OwnedRecord(data,session,owner).Progress;
            if(profile.TestMerchant!=null && profile.TestMerchant.LastRestockedExpedition==expedition) return 0;
            profile.TestMerchant=MerchantCatalog.Current.CreateStock(); profile.TestMerchant.LastRestockedExpedition=expedition;
            profile.Revision++; return 0;
        });
    }
}
