using System;
using CyberExtraction.Core;
using CyberExtraction.Items;

namespace CyberExtraction.Camp
{
    public static class CampTrade
    {
        public static int RespecPrice(int level) => 50+25*Math.Max(1,Math.Min(20,level));
        public static int SellPrice(ItemStack stack)
        {
            if(stack.Empty || !ItemCatalog.Available(stack.Item) || RecoveryRules.Bound(stack)) return -1;
            if(TreasureCatalog.IsDefined(stack.Item)) return TreasureCatalog.Get(stack.Item).SellPrice;
            if(EquipmentCatalog.IsDefined(stack.Item)) return EquipmentCatalog.ValidInstance(stack) ?
                stack.Instance.Rarity==ItemRarity.Common ? 8 : stack.Instance.Rarity==ItemRarity.Uncommon ? 20 : 45 : -1;
            if(ItemCatalog.Weapon(stack.Item)) return 12;
            if(ItemCatalog.Ammo(stack.Item)) return 1;
            if(stack.Item==ItemId.Lockpick || stack.Item==ItemId.BoltCutters || stack.Item==ItemId.Crowbar || stack.Item==ItemId.HandSaw) return 5;
            switch(stack.Item)
            {
                case ItemId.Wood: case ItemId.Nails: case ItemId.Metal: return 1;
                case ItemId.Gunpowder: case ItemId.AmmoComponent: return 3;
                case ItemId.Bandage: case ItemId.FishCommon: return 2;
                case ItemId.FishRare: return 5;
                case ItemId.FishLarge: return 8;
                case ItemId.CookedFish: return 3;
                case ItemId.RawFish: return 1;
                case ItemId.BurntFish: return 0;
                default: return -1;
            }
        }
    }
    public partial class CampProgress
    {
        bool Sell(CampAction action)
        {
            var grid=action.SourceGrid==0 ? Pack : Chest;
            if(action.Source<0 || action.Source>=grid.Slots.Length || action.Quantity<1) return false;
            var stack=grid.Slots[action.Source]; int price=CampTrade.SellPrice(stack);
            if(!stack.Equals(action.ExpectedItem) || action.Quantity>stack.Quantity || price<0) return false;
            long total=(long)price*action.Quantity;
            if(Character.Coins>long.MaxValue-total) return false;
            stack.Quantity-=action.Quantity; grid.Slots[action.Source]=stack.Empty ? default : stack;
            Character.Coins+=total; action.TradeFeedback="Venda concluÃ­da: +"+total+" moedas."; return true;
        }
        bool Buy(CampAction action)
        {
            int offerId=action.Result;
            if(Merchant==null || Merchant.Cycle!=action.Reference || offerId<0 || offerId>=Merchant.Offers.Length || action.Quantity<1) return false;
            var offer=Merchant.Offers[offerId];
            if(!ItemCatalog.Available(offer.Item.Item)) return false;
            if(!offer.Item.Equals(action.ExpectedItem) || action.Quantity>offer.Remaining || (offer.Item.Instance.HasIdentity && action.Quantity!=1)) return false;
            long price=(long)offer.Price*action.Quantity;
            if(Character.Coins<price) return false;
            var pack=Pack.Copy(); var incoming=offer.Item; incoming.Quantity=action.Quantity;
            if(!pack.Add(incoming)) return false;
            Pack=pack; Character.Coins-=price; offer.Remaining-=action.Quantity;
            action.TradeFeedback="Compra entregue na mochila: -"+price+" moedas."; return true;
        }
        bool Unload(CampAction action)
        {
            var source=action.SourceGrid==0 ? Pack : Chest;
            if(action.Source<0 || action.Source>=source.Slots.Length) return false;
            var stack=source.Slots[action.Source]; var weapon=WeaponCatalog.Get(stack.Item);
            if(!stack.Equals(action.ExpectedItem) || stack.Empty || weapon==null || weapon.IsMelee || stack.Data<=0 || stack.Data>weapon.Capacity || !RecoveryRules.Valid(stack)) return false;
            int bound=stack.Instance.RecoveryRounds;
            var pack=Pack.Copy(); var chest=Chest.Copy();
            if(stack.Data>bound && !pack.Add(new ItemStack(weapon.Ammo,stack.Data-bound))) return false;
            if(bound>0)
            {
                var ammo=new ItemStack(weapon.Ammo,bound); ammo.Instance.Origin=ItemOrigin.Recovery;
                if(!pack.Add(ammo)) return false;
            }
            stack.Data=0; stack.Instance.RecoveryRounds=0;
            (action.SourceGrid==0 ? pack : chest).Slots[action.Source]=stack;
            Pack=pack; Chest=chest; action.TradeFeedback="MuniÃ§Ã£o descarregada na mochila, preservando a origem."; return true;
        }
        bool Respec(CampAction action)
        {
            Character.Normalize(); int price=CampTrade.RespecPrice(Character.Level);
            if(Character.UnspentPoints==Character.Level-1 || Character.Coins<price) return false;
            Character.Coins-=price; Character.Invested=default; Character.Normalize(); action.TradeFeedback="Pontos investidos devolvidos: âˆ’"+price+" moedas."; return true;
        }
    }
}
