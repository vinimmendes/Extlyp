// Actual production rules are compiled alongside this driver. All checks state desired behavior.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using CyberExtraction.Camp;
using CyberExtraction.Core;
using CyberExtraction.Items;
using CyberExtraction.Match;
using CyberExtraction.Bots;
using UnityEngine;

class Stage5Probe
{
    sealed class RuleFailure:Exception {public RuleFailure(string text):base(text){}}
    static readonly List<object> results=new List<object>();
    static int passed,failed,errors;
    static string output;
    static string filter;
    static void Assert(bool value,string detail="Rule expectation was not met") {if(!value) throw new RuleFailure(detail);}
    static void Reject(Action action,string detail="Invalid state was accepted")
    {try {action();} catch(InvalidOperationException){return;} catch(ArgumentException){return;} throw new RuleFailure(detail);}
    static void Case(string group,string name,Action action,string issue=null)
    {
        if(!string.IsNullOrEmpty(filter) && !name.Contains(filter,StringComparison.Ordinal)) return;
        string status="PASS",detail="Desired rule holds in this adapter scope.";
        try {action();passed++;}
        catch(RuleFailure e){status="FAIL";detail=e.Message;failed++;}
        catch(Exception e){status="ERROR";detail=e.GetType().Name+": "+e.Message;errors++;}
        results.Add(new {group,name,status,issue,detail}); Console.WriteLine(status+" "+group+"/"+name+" "+(status=="PASS"?"":detail));
    }
    static CampProgress Empty()=>new CampProgress {SchemaVersion=CampProfileMigration.CurrentVersion,Pack=new InventoryGrid(),Chest=new InventoryGrid()};
    static CampAction Command(CampProgress p,CampActionKind kind)=>new CampAction {Sequence=p.Revision+1,Kind=kind};
    static InventoryGrid Full() {var g=new InventoryGrid();for(int i=0;i<9;i++)g.Slots[i]=new ItemStack(ItemId.Wood,20);return g;}
    static bool Act(CampProgress p,CampActionKind kind,int source=0,int target=0,int result=0,int quantity=1,ItemStack expected=default)
    {var c=Command(p,kind);c.Source=source;c.Target=target;c.Result=result;c.Quantity=quantity;c.ExpectedItem=expected;return p.Apply(c);}
    static void Validate(CampProgress p)=>CampProfileMigration.ValidateOwnership(new[]{new CampRecord {Session="fixture",Progress=p}});
    static string Session()=>Guid.NewGuid().ToString("N");
    static string BeginSession(out string expedition)
    {string s=Session();CampRepository.Edit(s,p=>0);expedition=Session();CampRepository.Begin(s,expedition,out _,out _);CampRepository.Commit(s,expedition);return s;}

    static void Inventory()
    {
        Case("inventory","starter_weapon_and_reserve",()=>{var g=InventoryGrid.Starter();Assert(g.Count(ItemId.Pistol)==1 && g.Count(ItemId.PistolAmmo)==24 && g.Slots.Where(s=>s.Item==ItemId.PistolAmmo).All(s=>s.Quantity<=20));});
        Case("inventory","full_add_is_atomic",()=>{var g=Full();string before=JsonUtility.ToJson(g);Assert(!g.Add(new ItemStack(ItemId.Metal,1)) && before==JsonUtility.ToJson(g));});
        Case("inventory","negative_remove_is_rejected",()=>{var g=Full();Assert(!g.Remove(ItemId.Wood,-1) && g.Count(ItemId.Wood)==180);});
        Case("inventory","partial_merge_conserves_quantity",()=>{var a=new InventoryGrid();var b=new InventoryGrid();a.Add(new ItemStack(ItemId.Metal,15));b.Add(new ItemStack(ItemId.Metal,18));Assert(a.MoveTo(0,b,0) && a.Count(ItemId.Metal)==13 && b.Count(ItemId.Metal)==20);});
        Case("inventory","weapon_identity_cannot_be_inserted_twice",()=>{var g=new InventoryGrid();var w=new ItemStack(ItemId.Mosin,1,0,3);Assert(g.Add(w) && !g.Add(w) && g.Count(ItemId.Mosin)==1);});
        Case("inventory","quantity_two_unique_weapon_rejected",()=>Assert(!new InventoryGrid().Add(new ItemStack(ItemId.Mosin,2,0,3))));
        Case("inventory","weapon_transfer_preserves_identity_and_rounds",()=>{var a=new InventoryGrid();var b=new InventoryGrid();var w=new ItemStack(ItemId.Mosin,1,0,3);a.Add(w);Assert(a.MoveTo(0,b,8) && b.Slots[8].Equals(w) && a.Slots[0].Empty);});
        Case("inventory","equipment_exchange_rejects_wrong_slot_atomically",()=>{var p=Empty();var g=EquipmentCatalog.Create(ItemId.WatchmanHood,ItemRarity.Rare);p.Pack.Add(g);Assert(!p.Equipment.Exchange(p.Pack,0,1) && p.Pack.Slots[0].Equals(g) && p.Equipment.Slots[1].Empty);});
        Case("inventory","equipment_roundtrip_conserves_instance",()=>{var p=Empty();var g=EquipmentCatalog.Create(ItemId.WatchmanHood,ItemRarity.Rare);p.Pack.Add(g);Assert(p.Equipment.Exchange(p.Pack,0,0) && p.Equipment.Exchange(p.Pack,0,0) && p.Pack.Slots[0].Equals(g));});
        Case("inventory","cross_profile_duplicate_guid_rejected",()=>{var a=Empty();var b=Empty();var w=new ItemStack(ItemId.Pistol,1);a.Pack.Add(w);b.Chest.Add(w);Reject(()=>CampProfileMigration.ValidateOwnership(new[]{new CampRecord {Session="a",Progress=a},new CampRecord {Session="b",Progress=b}}));});
    }
    static void TradeAndRecovery()
    {
        Case("trade","server_offer_price_and_empty_gun",()=>{var p=Empty();p.Character.Coins=40;Assert(Act(p,CampActionKind.Buy,result:5) && p.Character.Coins==0 && p.Pack.Slots[0].Item==ItemId.Pistol && p.Pack.Slots[0].Data==0);});
        Case("trade","buy_full_pack_does_not_charge",()=>{var p=Empty();p.Pack=Full();p.Character.Coins=100;Assert(!Act(p,CampActionKind.Buy) && p.Character.Coins==100);});
        Case("trade","invalid_offer_rejected",()=>{var p=Empty();p.Character.Coins=100;Assert(!Act(p,CampActionKind.Buy,result:999) && p.Character.Coins==100);});
        Case("trade","sell_checks_expected_instance",()=>{var p=Empty();p.Pack.Add(new ItemStack(ItemId.Metal,4));Assert(!Act(p,CampActionKind.Sell,quantity:4,expected:new ItemStack(ItemId.Metal,3)) && p.Pack.Count(ItemId.Metal)==4 && p.Character.Coins==0);});
        Case("trade","sell_quantity_and_balance",()=>{var p=Empty();p.Pack.Add(new ItemStack(ItemId.Metal,4));Assert(Act(p,CampActionKind.Sell,quantity:3,expected:p.Pack.Slots[0]) && p.Character.Coins==3 && p.Pack.Count(ItemId.Metal)==1);});
        Case("trade","coin_overflow_rejected_atomically",()=>{var p=Empty();p.Character.Coins=long.MaxValue;p.Pack.Add(new ItemStack(ItemId.Metal,1));Assert(!Act(p,CampActionKind.Sell,expected:p.Pack.Slots[0]) && p.Pack.Count(ItemId.Metal)==1 && p.Character.Coins==long.MaxValue);});
        Case("recovery","free_kit_only_without_any_weapon",()=>{var p=Empty();p.Chest.Add(new ItemStack(ItemId.ShortSword,1));p.PrepareRecoveryEquipment();Assert(p.Pack.Count(ItemId.Pistol)==0);});
        Case("recovery","free_kit_is_bound_and_not_repeatable",()=>{var p=Empty();p.PrepareRecoveryEquipment();p.PrepareRecoveryEquipment();Assert(p.Pack.Count(ItemId.Pistol)==1 && p.Pack.Count(ItemId.PistolAmmo)==12 && p.Pack.Slots.Where(s=>!s.Empty).All(RecoveryRules.Bound));});
        Case("recovery","bound_sell_and_discard_rejected",()=>{var p=Empty();p.PrepareRecoveryEquipment();Assert(CampTrade.SellPrice(p.Pack.Slots[0])<0 && !Act(p,CampActionKind.Sell,expected:p.Pack.Slots[0]) && !Act(p,CampActionKind.Discard));});
        Case("recovery","death_discards_bound_and_strips_only_bound_rounds",()=>{var w=new ItemStack(ItemId.Pistol,1,0,8);w.Instance.RecoveryRounds=3;var drop=RecoveryRules.DeathDrop(w);Assert(drop.Data==5 && drop.Instance.RecoveryRounds==0 && drop.Instance.SameIdentity(w.Instance));w.Instance.Origin=ItemOrigin.Recovery;Assert(RecoveryRules.DeathDrop(w).Empty);});
        Case("recovery","unload_splits_normal_and_bound_rounds",()=>{var p=Empty();var w=new ItemStack(ItemId.Pistol,1,0,8);w.Instance.RecoveryRounds=3;p.Pack.Add(w);Assert(Act(p,CampActionKind.Unload,expected:w) && p.Pack.Slots[0].Data==0 && p.Pack.Slots[0].Instance.RecoveryRounds==0 && p.Pack.Slots.Where(s=>s.Item==ItemId.PistolAmmo && s.Instance.Origin==ItemOrigin.Normal).Sum(s=>s.Quantity)==5 && p.Pack.Slots.Where(s=>s.Item==ItemId.PistolAmmo && s.Instance.Origin==ItemOrigin.Recovery).Sum(s=>s.Quantity)==3);});
        Case("recovery","unload_without_two_spaces_is_atomic",()=>{var p=Empty();p.Pack=Full();var w=new ItemStack(ItemId.Pistol,1,0,8);w.Instance.RecoveryRounds=3;p.Pack.Slots[0]=w;p.Pack.Slots[1]=default;p.Ensure();string before=JsonUtility.ToJson(p);Assert(!Act(p,CampActionKind.Unload,expected:w) && before==JsonUtility.ToJson(p));});
        Case("trade","respec_returns_points_without_xp_gain",()=>{var p=Empty();p.Character.Experience=500;p.Character.Normalize();p.Character.Invest(AttributeId.Strength);p.Character.Coins=1000;int price=CampTrade.RespecPrice(p.Character.Level),level=p.Character.Level;Assert(Act(p,CampActionKind.Respec) && p.Character.Coins==1000-price && p.Character.Experience==500 && p.Character.UnspentPoints==level-1 && p.Character.Invested.Strength==0);});
    }
    static void Progression()
    {
        Case("progression","level_boundaries_and_cap",()=>{Assert(CharacterProgressionRules.LevelAt(99)==1 && CharacterProgressionRules.LevelAt(100)==2 && CharacterProgressionRules.LevelAt(10450)==20);var p=new CharacterProgress();p.GainExperience(long.MaxValue);Assert(p.Experience==10450 && p.Level==20 && p.UnspentPoints==19);});
        Case("progression","investment_budget_and_reserved_intelligence",()=>{var p=new CharacterProgress {Experience=100};p.Normalize();Assert(p.Invest(AttributeId.Strength) && !p.Invest(AttributeId.Dexterity) && !p.Invest(AttributeId.Intelligence));});
        Case("progression","negative_or_excess_investment_rejected",()=>{var p=new CharacterProgress();p.Invested.Strength=-1;Reject(p.Normalize);p.Invested.Strength=1;Reject(p.Normalize);});
        Case("progression","effective_attributes_and_vision_limits",()=>{Assert(CharacterAttributeRules.Effective(-9)==1 && CharacterAttributeRules.Effective(999)==20 && CharacterAttributeRules.VisionAngle(1,true)==80 && CharacterAttributeRules.VisionAngle(20,true)==110);});
        Case("progression","dexterity_preserves_shotgun_structure",()=>{var w=WeaponCatalog.Get(ItemId.DoubleBarrelShotgun);Assert(w.StructuralHalfAngle==7 && w.PointingHalfAngle(0,.5f)==0 && w.PointingHalfAngle(1,.5f)<w.PointingHalfAngle(1,1));});
        Case("expedition","rollback_restores_reserved_instances_and_food",()=>{string s=Session(),e=Session();var before=CampRepository.Get(s);before.WellFed=true;before.FoodHealth=25;CampRepository.Save(s,before);var grid=CampRepository.Begin(s,e,out int food,out var loadout);Assert(food==25 && CampRepository.Get(s).ExpeditionState==1);CampRepository.Rollback(s,e);var p=CampRepository.Get(s);Assert(p.ExpeditionState==0 && p.WellFed && p.FoodHealth==25 && p.Equipment.Slots[6].Instance.HasIdentity && p.Equipment.Slots[6].Instance.SameIdentity(loadout.Slots[6].Instance));});
        Case("expedition","bank_conserves_items_and_is_idempotent",()=>{string s=Session(),e=Session();var grid=CampRepository.Begin(s,e,out _,out var loadout);CampRepository.Commit(s,e);grid.Add(new ItemStack(ItemId.Metal,7));Assert(CampRepository.Bank(s,e,grid,loadout) && !CampRepository.Bank(s,e,grid,loadout) && CampRepository.Get(s).Pack.Count(ItemId.Metal)==7);});
        Case("xp","grant_duplicate_and_extraction_multiplier_once",()=>{string s=BeginSession(out string e);var g=new ExperienceGrant {Session=s,Expedition=e,Key="objective:x",Amount=100};CampRepository.GrantExperience(new[]{g,g});Assert(CampRepository.Get(s).ExpeditionExperience==100);Assert(CampRepository.Bank(s,e,new InventoryGrid(),new EquipmentLoadout()));var p=CampRepository.Get(s);Assert(p.Character.Experience==120);CampRepository.Lose(s,e);Assert(CampRepository.Get(s).Character.Experience==120);});
        Case("xp","loss_confirms_half_without_returning_escrow",()=>{string s=BeginSession(out string e);CampRepository.GrantExperience(new[]{new ExperienceGrant {Session=s,Expedition=e,Key="a",Amount=101}});CampRepository.Lose(s,e);var p=CampRepository.Get(s);Assert(p.Character.Experience==50 && p.ExpeditionState==0 && p.Pack.Slots.All(x=>x.Empty) && p.Equipment.Slots.All(x=>x.Empty));});
        Case("xp","exploration_cap_is_forty",()=>{string s=BeginSession(out string e);for(int i=0;i<20;i++)CampRepository.GrantExperience(new[]{new ExperienceGrant {Session=s,Expedition=e,Key="region:"+i,Amount=5,Exploration=true}});Assert(CampRepository.Get(s).ExpeditionExperience==40);});
        Case("xp","pvp_pair_cooldown_is_bidirectional",()=>{string a=BeginSession(out string ea),b=BeginSession(out string eb);Assert(CampRepository.GrantPvp(a,ea,b,"kill:1") && !CampRepository.GrantPvp(a,ea,b,"kill:2") && !CampRepository.GrantPvp(b,eb,a,"kill:3"));});
        Case("xp","pvp_cap_is_sixty",()=>{string a=BeginSession(out string e);for(int i=0;i<4;i++){string victim=BeginSession(out _);bool granted=CampRepository.GrantPvp(a,e,victim,"kill:"+i);Assert(granted==(i<3));}Assert(CampRepository.Get(a).ExpeditionExperience==60);});
        Case("xp","wrong_expedition_grant_has_no_effect",()=>{string s=BeginSession(out string e);CampRepository.GrantExperience(new[]{new ExperienceGrant {Session=s,Expedition="wrong",Key="a",Amount=100}});Assert(CampRepository.Get(s).ExpeditionExperience==0);});
    }
    static void IdentityAndMigration()
    {
        Case("identity","signed_device_login_and_tamper",()=>{var r=new CampJoinRequest {CampOnly=true};CampIdentity.Sign(r);Assert(CampIdentity.Verify(r,DateTime.UtcNow.Ticks));r.Map=2;Assert(!CampIdentity.Verify(r,DateTime.UtcNow.Ticks));});
        Case("identity","expired_future_and_invalid_nonce_rejected",()=>{var r=new CampJoinRequest();CampIdentity.Sign(r);Assert(!CampIdentity.Verify(r,r.IssuedTicks+TimeSpan.FromMinutes(3).Ticks) && !CampIdentity.Verify(r,r.IssuedTicks-TimeSpan.FromMinutes(1).Ticks));r.Nonce="bad";Assert(!CampIdentity.Verify(r,DateTime.UtcNow.Ticks));});
        Case("identity","economic_login_payload_rejected",()=>{var r=new CampJoinRequest {Actions=new[]{new CampAction()}};CampIdentity.Sign(r);Assert(!CampIdentity.Verify(r,DateTime.UtcNow.Ticks));});
        Case("identity","replayed_signed_login_rejected",()=>{var r=new CampJoinRequest();CampIdentity.Sign(r);Assert(CampRepository.Authenticate(r) && !CampRepository.Authenticate(r));});
        Case("migration","legacy_weapon_gets_identity_without_ammo_change",()=>{var p=Empty();p.SchemaVersion=1;var w=new ItemStack(ItemId.Mosin,1,0,3);w.Instance=default;p.Pack.Slots[0]=w;Assert(CampProfileMigration.Upgrade(p) && p.Equipment.Slots[6].Data==3 && p.Equipment.Slots[6].Instance.HasIdentity);Validate(p);});
        Case("migration","v3_recovery_loaded_rounds_become_bound",()=>{var p=Empty();p.SchemaVersion=3;var w=new ItemStack(ItemId.Pistol,1,0,6);w.Instance.Origin=ItemOrigin.Recovery;p.Pack.Slots[0]=w;CampProfileMigration.Upgrade(p);Assert(p.Pack.Slots[0].Instance.RecoveryRounds==6);});
        Case("migration","future_version_rejected",()=>{var p=Empty();p.SchemaVersion=999;Reject(()=>CampProfileMigration.Upgrade(p));});
        Case("migration","active_legacy_profile_not_silently_migrated",()=>{var p=Empty();p.SchemaVersion=1;p.ExpeditionState=2;Reject(()=>CampProfileMigration.Upgrade(p));});
        Case("validation","wrong_grid_length_must_be_rejected",()=>{var p=Empty();p.Pack.Slots=new ItemStack[1];Reject(()=>Validate(p),"E2-05: one-slot pack passed persistent validation");},"E2-05");
        Case("validation","oversized_material_pile_must_be_rejected",()=>{var p=Empty();p.Pack.Slots[0]=new ItemStack(ItemId.Metal,999);Reject(()=>Validate(p),"E2-05: 999 metals passed persistent validation");},"E2-05");
        Case("validation","overloaded_normal_weapon_must_be_rejected",()=>{var p=Empty();p.Pack.Slots[0]=new ItemStack(ItemId.Pistol,1,0,999);Reject(()=>Validate(p),"E2-05: 999 loaded rounds passed persistent validation");},"E2-05");
        Case("validation","unknown_item_id_must_be_rejected",()=>{var p=Empty();p.Pack.Slots[0]=new ItemStack((ItemId)255,1);Reject(()=>Validate(p),"E2-05: item ID 255 passed persistent validation");},"E2-05");
        Case("lease","live_owner_blocks_other_claim",()=>{string s=Session();Assert(CampRepository.Claim(s,"A") && !CampRepository.Claim(s,"B"));});
        Case("lease","stale_disconnect_must_not_lose_new_expedition",()=>
        {
            string s=Session();Assert(CampRepository.Claim(s,"A"));
            var records=(Dictionary<string,CampRecord>)typeof(CampRepository).GetField("records",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            records[s].LeaseExpires=0; // Force expiry only; do not replace Claim/Begin/Commit/Lose rules.
            Assert(CampRepository.Claim(s,"B"));string e=Session();CampRepository.Begin(s,e,out _,out _);CampRepository.Commit(s,e);
            // Exact repository-call sequence of NetworkBootstrap.Disconnected for an old room.
            var leaving=CampRepository.Get(s);if(leaving.ExpeditionState==2)CampRepository.Lose(s,leaving.ExpeditionId);CampRepository.Release(s,"A");
            Assert(CampRepository.Get(s).ExpeditionState==2,"E2-01: old callback call sequence cleared the new owner's active expedition");
        },"E2-01");
    }
    static void LootAndActivity()
    {
        Case("loot","real_authoring_definitions_are_complete",()=>{Assert(Resources.LoadAll<ItemDefinition>("Items/Equipment/Definitions").Length==18 && Resources.LoadAll<ItemDefinition>("Items/Treasure/Definitions").Length==6 && Resources.LoadAll<LootTableDefinition>("Items/Loot/Tables").Length==10 && GameplayDefinition.Current.Valid);foreach(ItemId id in Enum.GetValues(typeof(ItemId))){if(EquipmentCatalog.IsDefined(id))Assert(EquipmentCatalog.Get(id)!=null);if(TreasureCatalog.IsDefined(id))Assert(TreasureCatalog.Get(id)!=null);}});
        Case("loot","all_equipment_rarity_rolls_validate",()=>{foreach(var d in Resources.LoadAll<ItemDefinition>("Items/Equipment/Definitions"))for(int rarity=0;rarity<3;rarity++)for(int i=0;i<20;i++)Assert(EquipmentCatalog.ValidInstance(EquipmentCatalog.Create(d.Id,(ItemRarity)rarity)));});
        Case("loot","bundles_obey_limits_in_all_contexts",()=>
        {
            foreach(var table in Resources.LoadAll<LootTableDefinition>("Items/Loot/Tables"))
            for(int risk=0;risk<3;risk++)foreach(var theme in new[]{LootTheme.General,LootTheme.Armored,LootTheme.Organic,LootTheme.Corrosive})for(int i=0;i<16;i++)
            {
                var stacks=LootRoller.Roll(table,new LootContext(table.Map,table.Source,(LootRisk)risk,theme));
                Assert(stacks.Length<=table.MaximumStacks && (stacks.Length==0 || stacks.Length>=table.MinimumStacks));
                Assert(stacks.Select(s=>s.Item).Distinct().Count()==stacks.Length && stacks.Count(s=>ItemCatalog.Weapon(s.Item))<=1 && stacks.Count(s=>EquipmentCatalog.IsDefined(s.Item))<=1);
                Assert(!table.GuaranteeEquipment || stacks.Any(s=>EquipmentCatalog.IsDefined(s.Item)));
                foreach(var stack in stacks)Assert(ItemCatalog.Valid(stack.Item) && !ItemCatalog.ExpeditionOnly(stack.Item) && stack.Quantity<=ItemCatalog.Limit(stack.Item) && EquipmentCatalog.ValidInstance(stack));
            }
        });
        Case("loot","client_cannot_roll",()=>{Unity.Netcode.NetworkManager.Singleton.IsServer=false;try {Reject(()=>LootRoller.Roll(new LootContext(ExpeditionMap.Dungeon,LootSource.Ground)));}finally{Unity.Netcode.NetworkManager.Singleton.IsServer=true;}});
        Case("loot","bad_weights_are_rejected",()=>{var table=Resources.LoadAll<LootTableDefinition>("Items/Loot/Tables")[0];var copy=JsonUtility.FromJson<LootTableDefinition>(JsonUtility.ToJson(table));copy.Rarities[0].Common=101;Reject(()=>LootTables.Validate(copy));});
        Case("activity","forged_fishing_and_cooking_rejected",()=>{var p=Empty();p.PierRepaired=true;p.Pack.Add(new ItemStack(ItemId.FishCommon,1));Assert(!Act(p,CampActionKind.CatchFish) && !Act(p,CampActionKind.CookFish,result:3));});
        Case("activity","old_replay_rejected_after_id_window",()=>{var p=Empty();p.Pack.Add(new ItemStack(ItemId.Metal,1));var c=Command(p,CampActionKind.Transfer);c.Target=1;Assert(p.Apply(c));for(int i=0;i<260;i++)Assert(Act(p,CampActionKind.Transfer,source:i%2==0?1:0,target:i%2==0?0:1));Assert(!p.Apply(c) && p.Pack.Count(ItemId.Metal)==1);});
        Case("activity","cooking_early_and_old_click_rejected",()=>{var p=Empty();p.Pack.Add(new ItemStack(ItemId.FishCommon,1));var a=new CampActivity();Assert(a.BeginCooking(p,0,100,100));Assert(!a.FinishCooking(p,100,.01f,100) && !a.FinishCooking(p,102,.2f,102) && p.Pack.Count(ItemId.FishCommon)==1);});
        Case("activity","cooking_real_timing_produces_one_reward",()=>{var p=Empty();p.Pack.Add(new ItemStack(ItemId.FishCommon,1));var a=new CampActivity();Assert(a.BeginCooking(p,0,100,100) && a.FinishCooking(p,100.744f,.744f,100.744) && a.LastCookingQuality==3 && p.Pack.Count(ItemId.CookedFish)==1 && !a.FinishCooking(p,100.8f));});
        Case("activity","cooking_overflow_preserves_result_on_ground",()=>{var p=Empty();p.Pack=Full();p.Chest=Full();p.Pack.Slots[0]=new ItemStack(ItemId.FishCommon,20);var a=new CampActivity();Assert(a.BeginCooking(p,0,0,0) && a.FinishCooking(p,.744f,.744f,.744) && p.Pack.Count(ItemId.FishCommon)==19 && p.Ground.Count==1 && p.Ground[0].Stack.Item==ItemId.CookedFish);});
        Case("activity","fish_early_press_cannot_award_capture",()=>{var f=new FishingGame();f.Begin(ItemId.FishCommon,0);f.Input(true,.1f);f.Tick(.2f,.2f);Assert(f.Phase==5);});
        Case("activity","ground_growth_observation",()=>{var p=Empty();for(int i=0;i<128;i++){Assert(p.Pack.Add(new ItemStack(ItemId.Metal,1)));Assert(Act(p,CampActionKind.Discard));}Assert(p.Ground.Count==128);File.WriteAllText(Path.Combine(output,"ground-growth-adapter.json"),JsonUtility.ToJson(new CampSnapshot {Profile=JsonUtility.ToJson(p)}));});
    }
    static void Storage()
    {
        Case("storage","durable_reopen_preserves_profile",()=>
        {
            string cluster="audit5_"+Session(),s=Session();string folder=Path.Combine(output,"isolated-data",cluster);Directory.CreateDirectory(folder);
            using(var m=new SharedCampMemory(cluster,folder))m.Transaction(d=>{d.Add(s,new CampRecord {Session=s,Progress=Empty()});d[s].Progress.Character.Coins=71;return 0;});
            using(var m=new SharedCampMemory(cluster,folder))Assert(m.Transaction(d=>d[s].Progress.Character.Coins,false)==71);
        });
        Case("storage","failed_operation_does_not_publish_partial_state",()=>
        {
            string cluster="audit5_"+Session(),s=Session();string folder=Path.Combine(output,"isolated-data",cluster);
            using var m=new SharedCampMemory(cluster,folder);m.Transaction(d=>{d.Add(s,new CampRecord {Session=s,Progress=Empty()});return 0;});
            Reject(()=>m.Transaction<int>(d=>{d[s].Progress.Character.Coins=999;throw new InvalidOperationException("injected-before-commit");}));
            Assert(m.Transaction(d=>d[s].Progress.Character.Coins,false)==0);
        });
        Case("storage","semantic_primary_failure_must_try_valid_backup",()=>
        {
            string cluster="audit5_"+Session(),s=Session();string folder=Path.Combine(output,"isolated-data",cluster);Directory.CreateDirectory(folder);
            using(var m=new SharedCampMemory(cluster,folder))m.Transaction(d=>{d.Add(s,new CampRecord {Session=s,Progress=Empty()});return 0;});
            string path=Path.Combine(folder,cluster+".json");File.Copy(path,path+".bak");File.WriteAllText(path,"{\"Version\":6,\"Records\":null}");
            using var restored=new SharedCampMemory(cluster,folder);
            try {Assert(restored.Transaction(d=>d.ContainsKey(s),false));}
            catch(InvalidOperationException){throw new RuleFailure("E2-03: semantic primary error prevented valid backup loading (JSON adapter; confirm Unity parser separately)");}
        },"E2-03");
    }
    static void AdditionalConservation()
    {
        foreach(var ammo in new[]{ItemId.PistolAmmo,ItemId.RifleAmmo,ItemId.ShotgunShell})
        Case("conservation","additional_recipe_"+ammo,()=>{var p=Empty();p.Chest.Add(new ItemStack(ItemId.Metal,3));p.Chest.Add(new ItemStack(ItemId.Gunpowder,1));p.Chest.Add(new ItemStack(ItemId.AmmoComponent,1));Assert(Act(p,CampActionKind.CraftAmmo,result:(int)ammo) && p.Count(ammo)==WeaponCatalog.RecipeOutput(ammo) && p.Count(ItemId.Metal)==0 && p.Count(ItemId.Gunpowder)==0 && p.Count(ItemId.AmmoComponent)==0);});
        Case("conservation","additional_craft_full_grid_is_atomic",()=>{var p=Empty();p.Pack=Full();p.Chest=Full();p.Chest.Slots[0]=new ItemStack(ItemId.Metal,20);p.Chest.Slots[1]=new ItemStack(ItemId.Gunpowder,20);p.Chest.Slots[2]=new ItemStack(ItemId.AmmoComponent,20);p.Ensure();string before=JsonUtility.ToJson(p);Assert(!Act(p,CampActionKind.CraftAmmo) && before==JsonUtility.ToJson(p));});
        Case("conservation","additional_bank_unbinds_return_but_not_chest",()=>{string s=Session();var p=Empty();var bound=new ItemStack(ItemId.PistolAmmo,5);bound.Instance.Origin=ItemOrigin.Recovery;p.Chest.Add(bound);CampRepository.Save(s,p);string e=Session();CampRepository.Begin(s,e,out _,out _);CampRepository.Commit(s,e);var returned=new InventoryGrid();returned.Add(bound);Assert(CampRepository.Bank(s,e,returned,new EquipmentLoadout()));var saved=CampRepository.Get(s);Assert(saved.Chest.Slots[0].Instance.Origin==ItemOrigin.Recovery && saved.Pack.Slots[0].Instance.Origin==ItemOrigin.Normal && saved.Pack.Count(ItemId.PistolAmmo)==5);});
        Case("conservation","additional_bank_wrong_expedition_rejected",()=>{string s=BeginSession(out string e);Assert(!CampRepository.Bank(s,"wrong",new InventoryGrid(),new EquipmentLoadout()) && CampRepository.Get(s).ExpeditionState==2);});
    }
    static int Main(string[] args)
    {
        output=args[1];filter=args.Length>2?args[2]:null;Application.persistentDataPath=Path.Combine(output,"isolated-data");Resources.Initialize(args[0]);
        Inventory();TradeAndRecovery();Progression();IdentityAndMigration();LootAndActivity();Storage();
        AdditionalConservation();
        Case("policy","queue_and_bot_extraction_boundaries",()=>{var q=new MatchQueue();q.SecondsLeft(1,100);Assert(!q.ShouldStart(1,129) && q.ShouldStart(1,130) && !BotDecision.CanExtract(61) && BotDecision.CanExtract(60) && BotDecision.TimeToLeave(150,100));});
        CampRepository.Close();
        var report=new {scope="Frozen real C# rules; System.Text.Json field adapter, extracted numeric YAML fixtures, System.Random, minimal Netcode types. Native Unity/IL/RPC/physics/rendering/input NOT validated.",filter,passed,failed,errors,results};
        File.WriteAllText(Path.Combine(output,"results.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions {WriteIndented=true}));
        Console.WriteLine($"TOTAL pass={passed} fail={failed} error={errors}");return errors>0?3:failed>0?2:0;
    }
}
