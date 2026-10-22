using System;
using System.Collections.Generic;
using CyberExtraction.Core;
using CyberExtraction.Items;

namespace CyberExtraction.Camp
{
    public static class CampProfileMigration
    {
        public const int CurrentVersion=6;
        public static CampProgress Create()
        {
            var profile=new CampProgress {SchemaVersion=CurrentVersion,Merchant=MerchantCatalog.Current.CreateStock()};
            profile.Ensure();
            return profile;
        }
        public static bool NeedsUpgrade(CampProgress profile)
        {
            if(profile==null || profile.SchemaVersion<0 || profile.SchemaVersion>CurrentVersion)
                throw new InvalidOperationException("Versao de perfil nao suportada.");
            return profile.SchemaVersion<CurrentVersion;
        }
        public static bool Upgrade(CampProgress profile)
        {
            bool upgrade=NeedsUpgrade(profile);
            ValidateStructure(profile);
            if(upgrade && profile.ExpeditionState!=0)
                throw new InvalidOperationException("Encerre ou recupere a expedicao antes de migrar o perfil.");
            profile.Ensure();
            if(!upgrade) return false;
            // Preserve identities/ammo; only the v2 weapon-slot migration moves items.
            Identify(profile.Chest); Identify(profile.Pack);
            foreach(var drop in profile.Ground)
            {
                if(drop==null) throw new InvalidOperationException("Item de acampamento invalido.");
                var stack=drop.Stack; Identify(ref stack); drop.Stack=stack;
            }
            if(profile.SchemaVersion<1) profile.Character=new CharacterProgress();
            if(profile.SchemaVersion<2) profile.Equipment.FillWeapons(profile.Pack);
            if(profile.SchemaVersion<4)
            {
                BindLegacyRecovery(profile.Chest.Slots); BindLegacyRecovery(profile.Pack.Slots); BindLegacyRecovery(profile.Equipment.Slots);
                foreach(var drop in profile.Ground)
                { var stack=drop.Stack; BindLegacyRecovery(ref stack); drop.Stack=stack; }
            }
            if(profile.SchemaVersion<5)
            {
                Array.Resize(ref profile.Pack.Slots,ItemCatalog.BackpackSlots);
                Array.Resize(ref profile.Chest.Slots,ItemCatalog.SafeChestSlots);
                if(profile.Escrow!=null) Array.Resize(ref profile.Escrow.Slots,ItemCatalog.BackpackSlots);
                profile.Merchant=MerchantCatalog.Current.CreateStock();
                profile.DeliverLegacyItems();
            }
            if(profile.SchemaVersion<6)
            {
                RemoveRetired(profile.Pack.Slots);RemoveRetired(profile.Chest.Slots);RemoveRetired(profile.Equipment.Slots);
                if(profile.Escrow!=null) RemoveRetired(profile.Escrow.Slots);
                if(profile.EscrowEquipment!=null) RemoveRetired(profile.EscrowEquipment.Slots);
                profile.Ground.RemoveAll(drop=>ItemCatalog.Retired(drop.Stack.Item));
                profile.Merchant=MerchantCatalog.Current.RemoveRetiredOffers(profile.Merchant);
                if(!MerchantState.IsAbsent(profile.TestMerchant)) profile.TestMerchant=MerchantCatalog.Current.RemoveRetiredOffers(profile.TestMerchant);
                profile.DeliverLegacyItems();
            }
            profile.Sync();
            profile.SchemaVersion=CurrentVersion;
            return true;
        }
        static void Identify(InventoryGrid grid)
        {
            if(grid.Slots==null) throw new InvalidOperationException("Inventario sem slots.");
            for(int i=0;i<grid.Slots.Length;i++) Identify(ref grid.Slots[i]);
        }
        static void RemoveRetired(ItemStack[] slots)
        {for(int i=0;i<slots.Length;i++) if(ItemCatalog.Retired(slots[i].Item)) slots[i]=default;}
        static void BindLegacyRecovery(ItemStack[] slots)
        { for(int i=0;i<slots.Length;i++) BindLegacyRecovery(ref slots[i]); }
        static void BindLegacyRecovery(ref ItemStack stack)
        {
            if(!stack.Empty && stack.Instance.Origin==ItemOrigin.Recovery && ItemCatalog.Weapon(stack.Item) && !ItemCatalog.Melee(stack.Item))
                stack.Instance.RecoveryRounds=System.Math.Max(0,stack.Data);
        }
        static void Identify(ref ItemStack stack)
        {
            if(stack.Empty || !ItemCatalog.Weapon(stack.Item)) return;
            if(stack.Quantity!=1) throw new InvalidOperationException("Arma legada com quantidade invalida; perfil preservado para revisao.");
            if(!stack.Instance.HasIdentity)
            { var identity=ItemInstance.Create(); stack.Instance.IdLow=identity.IdLow; stack.Instance.IdHigh=identity.IdHigh; }
        }
        // A registry is unnecessary in v1: the metadata travels with the value.
        // Validate ownership across all containers before committing a profile set.
        public static void ValidateOwnership(IEnumerable<CampRecord> records,bool allowLegacyWeapons=false)
        {
            if(records==null) throw new InvalidOperationException("Registros de progresso invalidos.");
            var identities=new HashSet<string>();
            foreach(var record in records)
            {
                if(record==null) throw new InvalidOperationException("Registro de progresso invalido.");
                var p=record.Progress;
                ValidateStructure(p);
                bool legacy=allowLegacyWeapons && p.SchemaVersion<5;
                if(p.SchemaVersion>=6)
                {
                    RejectRetired(p.Pack.Slots);RejectRetired(p.Chest.Slots);
                    if(p.Equipment!=null) RejectRetired(p.Equipment.Slots);
                    if(p.Escrow!=null) RejectRetired(p.Escrow.Slots);
                    if(p.EscrowEquipment!=null) RejectRetired(p.EscrowEquipment.Slots);
                    foreach(var drop in p.Ground) if(ItemCatalog.Retired(drop.Stack.Item)) throw new InvalidOperationException("Item retirado do jogo.");
                }
                ValidateGrid(p.Chest,identities,legacy); ValidateGrid(p.Pack,identities,legacy);
                if(p.Equipment!=null) ValidateEquipment(p.Equipment,identities,p.SchemaVersion<6);
                if(!MerchantState.IsAbsent(p.Merchant)) foreach(var offer in p.Merchant.Offers)
                {
                    if(offer.Remaining>0) ValidateStack(offer.Item,identities);
                    else ValidateItemValues(offer.Item);
                }
                if(!MerchantState.IsAbsent(p.TestMerchant)) foreach(var offer in p.TestMerchant.Offers)
                { if(offer.Remaining>0) ValidateStack(offer.Item,identities); else ValidateItemValues(offer.Item); }
                // After commit, escrow is a historical recovery snapshot. A weapon
                // may already have been dropped/traded to another participant.
                var escrowIdentities=p.ExpeditionState==1 ? identities : new HashSet<string>();
                if(p.Escrow!=null) ValidateGrid(p.Escrow,escrowIdentities,legacy);
                if(p.EscrowEquipment!=null) ValidateEquipment(p.EscrowEquipment,escrowIdentities,p.SchemaVersion<6);
                foreach(var drop in p.Ground)
                {
                    if(drop==null) throw new InvalidOperationException("Item de acampamento invalido.");
                    ValidateStack(drop.Stack,identities,legacy);
                }
            }
        }
        public static void ValidateStructure(CampProgress profile)
        {
            NeedsUpgrade(profile);
            ValidateGridShape(profile.Chest,profile.SchemaVersion<5 ? ItemCatalog.InventorySlots : ItemCatalog.SafeChestSlots); ValidateGridShape(profile.Pack,profile.SchemaVersion<5 ? ItemCatalog.InventorySlots : ItemCatalog.BackpackSlots);
            if(profile.SchemaVersion>=5) ValidateMerchant(profile.Merchant,profile.SchemaVersion<6);
            if(!MerchantState.IsAbsent(profile.TestMerchant)) ValidateMerchant(profile.TestMerchant,profile.SchemaVersion<6);
            if(profile.Ground==null) throw new InvalidOperationException("Lista de itens do acampamento invalida.");
            if(profile.Equipment==null && profile.SchemaVersion>=2)
                throw new InvalidOperationException("Equipamento ausente.");
            ValidateEquipmentShape(profile.Equipment);
            if(profile.ExpeditionState>2 || profile.ExpeditionState!=0 &&
                (string.IsNullOrEmpty(profile.ExpeditionId) || profile.Escrow==null || profile.SchemaVersion>=2 && profile.EscrowEquipment==null))
                throw new InvalidOperationException("Reserva de expedicao invalida.");
            if(profile.Escrow!=null) ValidateGridShape(profile.Escrow,ExpectedEscrowSlots(profile.SchemaVersion,profile.ExpeditionState,profile.Escrow.Slots?.Length ?? -1));
            ValidateEquipmentShape(profile.EscrowEquipment);
            var drops=new HashSet<string>();
            foreach(var drop in profile.Ground)
            {
                if(drop==null || string.IsNullOrEmpty(drop.Id) || !drops.Add(drop.Id))
                    throw new InvalidOperationException("Identificador de item no chao invalido ou duplicado.");
                ValidateItemValues(drop.Stack);
            }
        }
        static void RejectRetired(ItemStack[] slots)
        {foreach(var stack in slots) if(!stack.Empty && ItemCatalog.Retired(stack.Item)) throw new InvalidOperationException("Item retirado do jogo.");}
        // Inactive escrow is only a historical snapshot. JsonUtility may also
        // materialize a null inline grid using its legacy nine-slot constructor.
        // Active reservations must always match the current backpack capacity.
        public static int ExpectedEscrowSlots(int schema,int expeditionState,int storedCapacity)
        {
            if(schema<5 || expeditionState==0 && storedCapacity==ItemCatalog.InventorySlots)
                return ItemCatalog.InventorySlots;
            return ItemCatalog.BackpackSlots;
        }
        static void ValidateMerchant(MerchantState merchant,bool legacy)
        {
            if(merchant==null || !Guid.TryParseExact(merchant.Cycle,"N",out _) || merchant.Offers==null || (legacy ? merchant.Offers.Length!=12 && merchant.Offers.Length!=10 : merchant.Offers.Length!=10)) throw new InvalidOperationException("Estoque do comerciante invalido.");
            var items=new HashSet<ItemId>(); var categories=new HashSet<EquipmentSlot>(); var groups=new int[4];
            foreach(var offer in merchant.Offers)
            {
                if((!legacy || merchant.Offers.Length==10) && offer!=null && !ItemCatalog.Available(offer.Item.Item)) throw new InvalidOperationException("Mercadoria retirada do jogo.");
                if(offer==null || (int)offer.Group<0 || (int)offer.Group>3 || offer.Price<=0 || offer.InitialStock<1 || offer.Remaining<0 || offer.Remaining>offer.InitialStock || offer.Item.Quantity!=1 || !items.Add(offer.Item.Item)) throw new InvalidOperationException("Oferta persistida invalida.");
                if(EquipmentCatalog.IsDefined(offer.Item.Item) && (offer.Item.Instance.Rarity!=ItemRarity.Common || !categories.Add(ItemCatalog.EquipmentSlotFor(offer.Item.Item)))) throw new InvalidOperationException("Equipamento do comerciante invalido.");
                if(offer.Item.Instance.HasIdentity && offer.InitialStock!=1) throw new InvalidOperationException("Estoque de exemplar invalido.");
                bool validGroup=offer.Group==MerchantGroup.Consumable ? offer.Item.Item==ItemId.Bandage || ItemCatalog.Ammo(offer.Item.Item) :
                    offer.Group==MerchantGroup.Weapon ? ItemCatalog.Weapon(offer.Item.Item) : offer.Group==MerchantGroup.Equipment ? EquipmentCatalog.IsDefined(offer.Item.Item) :
                    offer.Item.Item==ItemId.Lockpick || offer.Item.Item==ItemId.BoltCutters || offer.Item.Item==ItemId.Crowbar || offer.Item.Item==ItemId.HandSaw;
                if(!validGroup || offer.Item.Instance.Origin!=ItemOrigin.Normal || offer.Item.Data!=0) throw new InvalidOperationException("Mercadoria nao autorizada.");
                ValidateItemValues(offer.Item); groups[(int)offer.Group]++;
            }
            if(groups[0]!=merchant.Offers.Length-8 || groups[1]!=2 || groups[2]!=4 || groups[3]!=2 || categories.Count!=4) throw new InvalidOperationException("Composicao do comerciante invalida.");
        }
        static void ValidateGridShape(InventoryGrid grid,int expected=0)
        {
            if(grid==null || grid.Slots==null || (expected>0 ? grid.Slots.Length!=expected : grid.Slots.Length!=9 && grid.Slots.Length!=12 && grid.Slots.Length!=16))
                throw new InvalidOperationException("Capacidade de inventario invalida.");
            foreach(var stack in grid.Slots) ValidateItemValues(stack);
        }
        static void ValidateEquipmentShape(EquipmentLoadout loadout)
        {
            if(loadout==null) return; // Equipment did not exist before schema v2.
            if(loadout.Slots==null || loadout.Slots.Length!=EquipmentLoadout.Count)
                throw new InvalidOperationException("Equipamento com estrutura invalida.");
            if(loadout.ActiveWeaponSlot!=-1 && !EquipmentLoadout.AcceptsWeapon(loadout.ActiveWeaponSlot))
                throw new InvalidOperationException("Slot de arma ativa invalido.");
            foreach(var stack in loadout.Slots) ValidateItemValues(stack);
        }
        static void ValidateItemValues(ItemStack stack)
        {
            if(!ItemCatalog.Valid(stack.Item)) throw new InvalidOperationException("Identificador de item inexistente.");
            // A non-positive quantity used to bypass validation through ItemStack.Empty.
            if(stack.Quantity<0 || stack.Quantity>ItemCatalog.Limit(stack.Item))
                throw new InvalidOperationException("Quantidade de item fora do limite; perfil preservado para revisao.");
            if(stack.Empty || !ItemCatalog.Weapon(stack.Item)) return;
            var weapon=WeaponCatalog.LegacyDefinition(stack.Item);
            if(weapon==null || stack.Data<0 || stack.Data>weapon.Capacity)
                throw new InvalidOperationException("Municao carregada fora do limite; perfil preservado para revisao.");
        }
        static void ValidateGrid(InventoryGrid grid,HashSet<string> identities,bool legacy=false)
        {
            ValidateGridShape(grid);
            foreach(var stack in grid.Slots) ValidateStack(stack,identities,legacy);
        }
        static void ValidateEquipment(EquipmentLoadout loadout,HashSet<string> identities,bool retiredLegacy=false)
        {
            if(loadout==null || loadout.Slots==null || loadout.Slots.Length!=EquipmentLoadout.Count)
                throw new InvalidOperationException("Equipamento invalido.");
            for(int i=0;i<loadout.Slots.Length;i++)
            {
                if(!EquipmentLoadout.Accepts(i,loadout.Slots[i]) && !(retiredLegacy && EquipmentLoadout.AcceptsWeapon(i) && ItemCatalog.Retired(loadout.Slots[i].Item))) throw new InvalidOperationException("Item em slot incompativel.");
                ValidateStack(loadout.Slots[i],identities);
            }
        }
        static void ValidateStack(ItemStack stack,HashSet<string> identities,bool legacy=false)
        {
            ValidateItemValues(stack);
            if(stack.Empty) return;
            if(!RecoveryRules.Valid(stack)) throw new InvalidOperationException("Municao de recuperacao invalida.");
            if(!EquipmentCatalog.ValidInstance(stack)) throw new InvalidOperationException("Exemplar de equipamento invalido; perfil preservado para revisao.");
            if((ItemCatalog.Equipment(stack.Item) || ItemCatalog.Weapon(stack.Item) && !legacy) && !stack.Instance.HasIdentity)
                throw new InvalidOperationException("Arma ou equipamento sem identidade.");
            if(!stack.Instance.HasIdentity) return;
            string key=stack.Instance.IdHigh.ToString("x16")+stack.Instance.IdLow.ToString("x16");
            if(stack.Quantity!=1 || !identities.Add(key)) throw new InvalidOperationException("Identidade de equipamento duplicada.");
        }
    }
}
