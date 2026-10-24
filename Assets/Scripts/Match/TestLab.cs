using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Bots;
using CyberExtraction.Combat;
using CyberExtraction.Core;
using CyberExtraction.Enemies;
using CyberExtraction.Items;
using CyberExtraction.Players;

namespace CyberExtraction.Match
{
    public enum TestAction : byte { Weapon, Monster, ClearEnemies, Bots, Restore, ReturnToShelter, Invulnerable, Item, Web, Acid, ClearEffects, Teleport, LockKit, CreateLock, ClearLocks, Equipment }

    public partial class MatchManager
    {
        public bool Sandbox => QuickTest.Value;
        public NetworkVariable<bool> TestProtection = new NetworkVariable<bool>(false);
        public NetworkVariable<FixedString128Bytes> TestMessage = new NetworkVariable<FixedString128Bytes>();
        public NetworkVariable<int> TestRevision = new NetworkVariable<int>(0);
        readonly Dictionary<ulong,double> nextTestCommand = new Dictionary<ulong,double>();
        public static readonly ItemId[] TestWeapons = { ItemId.ShortSword, ItemId.BattleAxe, ItemId.Spear, ItemId.ImprovisedHandCrossbow, ItemId.RustySword };
        public static readonly string[] TestMonsters = { "Investidor", "Ouvinte", "Couracado", "Aranha", "Arauto", "Mimico", "Corrosivo", "Espreitador", "Devorador", "Esporifero", "Planta-mae", "Bulbo de ataque", "Bulbo de cura", "Servo vegetal" };

        public static bool TestAccess(bool allowed, bool quick, MatchState state, bool hasParticipant) =>
            allowed && quick && state == MatchState.Running && hasParticipant;

        public void RequestTest(TestAction action, int value=0) { TestCommandRpc((byte)action,value); }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void TestCommandRpc(byte action,int value,RpcParams rpc=default)
        {
            ulong sender=rpc.Receive.SenderClientId;
            double now=Time.unscaledTimeAsDouble;
            if(nextTestCommand.TryGetValue(sender,out var next) && now<next)
            {
                if(Sandbox) Debug.Log("TEST LAB RATE LIMIT: "+action);
                return;
            }
            nextTestCommand[sender]=now+.15;
            ServerTestCommand(sender,action,value);
        }

        // Every mutation rechecks the server's test mode and the sender's actual participant.
        public bool ServerTestCommand(ulong sender,byte action,int value)
        {
            if(!IsServer) return false;
            var bootstrap=CyberExtraction.UI.NetworkBootstrap.Instance;
            var player=NetworkManager.ConnectedClients.TryGetValue(sender,out var client) ? client.PlayerObject : null;
            bool participant=player!=null && bootstrap!=null && bootstrap.ClientReady(sender) && !bootstrap.IsCampClient(sender);
            if(!TestAccess(bootstrap!=null && bootstrap.ServerAllowsTests(sender),Sandbox,CurrentState,participant) || action>(byte)TestAction.Equipment) return false;
            var inv=player.GetComponent<PlayerInventory>(); var hp=player.GetComponent<Health>();
            Debug.Log("TEST LAB COMMAND: "+(TestAction)action+" "+value+" at "+player.transform.position);
            switch((TestAction)action)
            {
                case TestAction.Weapon:
                    if(value<0 || value>=TestWeapons.Length || !inv.IsAlive) return false;
                    var weapon=WeaponCatalog.Get(TestWeapons[value]);
                    var grid=new InventoryGrid(ItemCatalog.BackpackSlots); grid.Add(new ItemStack(weapon.Item,1,0,weapon.Capacity));
                    if(!weapon.IsMelee) grid.Add(new ItemStack(weapon.Ammo,60)); grid.Add(new ItemStack(ItemId.Bandage,10));
                    var testLoadout=inv.Loadout();
                    testLoadout.Slots[EquipmentLoadout.WeaponA]=default; testLoadout.Slots[EquipmentLoadout.WeaponB]=default;
                    testLoadout.FillWeapons(grid); inv.ServerSetLoadout(testLoadout);
                    inv.FishingPhase.Value=0; inv.ServerSetGrid(grid);
                    player.GetComponent<PlayerShooter>().ServerCancelReload();
                    player.GetComponent<PlayerShooter>().BoltEndsAt.Value=0;
                    Say(weapon.IsMelee ? "Arma corpo a corpo equipada; ataque usa stamina." : "Arma equipada com 60 tiros de reserva."); break;
                case TestAction.Monster:
                    if(value<0 || value>=TestMonsters.Length || Monster.All.Count+PlantCreature.All.Count>=40) return false;
                    if(!FindTestPoint(player.transform.position,player.transform.right,1.5f,out var point)) { Say("Sem espaco livre por perto. Mude de posicao."); return false; }
                    if(value<9)
                    {
                        var enemy=Instantiate(monsterPrefab,point,Quaternion.identity);
                        enemy.GetComponent<Monster>().Configure((MonsterKind)value); enemy.GetComponent<NetworkObject>().Spawn();
                    }
                    else if(value==9) Instantiate(SporeEncounter.Prefab,point,Quaternion.identity).GetComponent<NetworkObject>().Spawn();
                    else if(value==10) PlantEncounter.Spawn(PlantKind.Mother,point);
                    else
                    {
                        // Bulbs/minions need a living mother to exercise their real lifecycle.
                        if(!FindTestPoint(point,Vector2.up,1.5f,out var motherPoint)) return false;
                        var mother=PlantEncounter.Spawn(PlantKind.Mother,motherPoint);
                        PlantEncounter.Spawn((PlantKind)(value-10),point,mother);
                    }
                    Say(TestMonsters[value]+" criado perto do jogador."); break;
                case TestAction.ClearEnemies:
                    ClearTestEnemies(); Say("Monstros, teias, acido e nuvens removidos."); break;
                case TestAction.Bots:
                    if(value<0 || value>=MapBuilder.Capacity) return false;
                    foreach(var other in new List<PlayerInventory>(PlayerInventory.All))
                        if(other!=null && other.GetComponent<BotController>()!=null && other.IsSpawned) other.NetworkObject.Despawn();
                    BotCount.Value=0; QuickBots.Value=value;
                    var freeBotSlots=new List<int>();
                    for(int candidate=0;candidate<MapBuilder.Capacity;candidate++)
                    {
                        bool reserved=false;
                        foreach(var spawnClient in NetworkManager.Singleton.ConnectedClientsList)
                            if(bootstrap.SlotFor(spawnClient.ClientId)==candidate) { reserved=true; break; }
                        if(!reserved) freeBotSlots.Add(candidate);
                    }
                    for(int i=0;i<value && freeBotSlots.Count>0;i++)
                    {
                        int pick=Random.Range(0,freeBotSlots.Count),slot=freeBotSlots[pick];
                        freeBotSlots.RemoveAt(pick);
                        Vector2 at=MapBuilder.ParticipantSpawn(slot);
                        if(!PlantEncounter.ClearSpawn(at,.5f)) continue;
                        var bot=Instantiate(botPrefab,at,Quaternion.Euler(0,0,MapBuilder.ParticipantFacing(slot)));
                        bot.GetComponent<BotController>().difficulty=botDifficulty;
                        bot.GetComponent<NetworkObject>().Spawn(); bot.GetComponent<PlayerInventory>().ServerBeginExpedition(); BotCount.Value++;
                    }
                    Say(BotCount.Value+" bots ativos."); break;
                case TestAction.Restore:
                case TestAction.ReturnToShelter:
                    inv.FishingPhase.Value=0; inv.ServerSlowUntil=0; inv.BandageReadyAt.Value=0;
                    hp.Current.Value=hp.Maximum.Value; inv.ServerSetStatus(PlayerStatus.Alive);
                    var exposure=player.GetComponent<SporeContamination>(); if(exposure!=null) exposure.ServerResetExposure();
                    if((TestAction)action==TestAction.ReturnToShelter) TestMove(player,MapBuilder.ParticipantSpawn(bootstrap.SlotFor(sender)));
                    if(inv.ActiveWeapon==null) { var starter=InventoryGrid.Starter(); var starterLoadout=inv.Loadout(); starterLoadout.FillWeapons(starter); inv.ServerSetLoadout(starterLoadout); inv.ServerSetGrid(starter); }
                    inv.ServerSetRounds(inv.ActiveWeapon.Capacity); int missing=inv.ActiveWeapon.IsMelee ? 0 : 60-inv.Count(inv.ActiveWeapon.Ammo);
                    if(missing>0) inv.ServerAddItem(inv.ActiveWeapon.Ammo,missing);
                    player.GetComponent<PlayerShooter>().ServerCancelMelee();
                    player.GetComponent<PlayerController>()?.ServerResetStamina();
                    player.GetComponent<PlayerShooter>().ServerCancelReload(); player.GetComponent<PlayerShooter>().BoltEndsAt.Value=0;
                    Say("Vida e carregador restaurados; reserva reposta se houver espaco."); break;
                case TestAction.Invulnerable:
                    if(value!=0 && value!=1) return false;
                    hp.TestInvulnerable=value==1; TestProtection.Value=value==1; Say(value==1 ? "Protecao ligada: dano bloqueado." : "Protecao desligada: dano normal."); break;
                case TestAction.Item:
                    if(value<0 || value>=ItemCatalog.Count || !ItemCatalog.Available((ItemId)value) || !inv.IsAlive) return false;
                    var item=(ItemId)value; var stack=new ItemStack(item,Mathf.Min(5,ItemCatalog.Limit(item)),0,ItemCatalog.Weapon(item) ? WeaponCatalog.Get(item).Capacity : 0);
                    if(ItemCatalog.Equipment(item) && !EquipmentCatalog.IsDefined(item))
                    {
                        var bonuses=new AttributeValues();
                        switch(ItemCatalog.EquipmentSlotFor(item))
                        { case EquipmentSlot.Head: bonuses.Perception=1; break; case EquipmentSlot.Torso: bonuses.Vitality=1; break;
                          case EquipmentSlot.Hands: bonuses.Strength=1; break; case EquipmentSlot.Feet: bonuses.Vigor=1; break;
                          case EquipmentSlot.Amulet: bonuses.Perception=1; break; case EquipmentSlot.Ring: bonuses.Dexterity=1; break; }
                        stack.Instance.Bonuses=bonuses;
                    }
                    if(!inv.ServerAddStack(stack)) ServerDropStack(player.transform.position+player.transform.right*1.5f,stack);
                    Say("Item de teste entregue."); break;
                case TestAction.Equipment:
                    if(value<(int)ItemId.WatchmanHood*3 || value>(int)ItemId.PersistenceRing*3+2 || !inv.IsAlive) return false;
                    var equipment=EquipmentCatalog.Create((ItemId)(value/3),(ItemRarity)(value%3));
                    if(!inv.ServerAddStack(equipment)) ServerDropStack(player.transform.position+player.transform.right*1.5f,equipment);
                    Say(ItemCatalog.Name(equipment.Item)+" · "+EquipmentCatalog.RarityName(equipment.Instance.Rarity)+" entregue."); break;
                case TestAction.Web:
                case TestAction.Acid:
                    if(MonsterEffects.Count>=256) return false;
                    if(!FindTestPoint(player.transform.position,player.transform.right,.3f,out var hazard)) return false;
                    MonsterWorld.Add((TestAction)action==TestAction.Web ? MonsterEffectKind.Web : MonsterEffectKind.AcidPool,hazard,hazard,(TestAction)action==TestAction.Web ? 1.3f : 2.3f,(TestAction)action==TestAction.Web ? 12 : 8);
                    Say("Area de perigo criada a frente."); break;
                case TestAction.ClearEffects:
                    ClearTestEffects(); Say("Areas de perigo removidas."); break;
                case TestAction.Teleport:
                    if(value<0 || value>2) return false;
                    Vector2 destination=value==0 ? MapBuilder.MonsterSpawn : value==1 ? MapBuilder.Sites[0]+new Vector2(0,2) : MapBuilder.ExtractionCenter+new Vector2(-4,0);
                    if(PlantEncounter.ClearSpawn(destination,.55f)) TestMove(player,destination);
                    else if(FindTestPoint(destination,Vector2.down,.6f,out var alternate)) TestMove(player,alternate);
                    else return false;
                    Say("Jogador levado ao ponto de teste."); break;
                case TestAction.LockKit:
                    if(!inv.IsAlive || inv.Lockpicking.Value) return false;
                    var lockKit=InventoryGrid.Starter(false); lockKit.Add(new ItemStack(ItemId.Lockpick,3)); lockKit.Add(new ItemStack(ItemId.Bandage,5));
                    inv.ServerSetGrid(lockKit); Say("Kit pronto: 3 gazuas, besta de mão, espada enferrujada, virotes e bandagens."); break;
                case TestAction.CreateLock:
                    if(value<0 || value>=12 || !inv.IsAlive || inv.Lockpicking.Value) return false;
                    int count=0; foreach(var existing in LootPickup.All) if(existing!=null && existing.LabLock.Value) count++;
                    if(count>=12) { Say("Limite de fechaduras de teste. Remova as existentes."); return false; }
                    Vector2 lockPoint=(Vector2)player.transform.position+(Vector2)player.transform.right*2;
                    if(!PlantEncounter.ClearSpawn(lockPoint,1.35f) && !FindTestPoint(player.transform.position,player.transform.right,1.35f,out lockPoint)) return false;
                    var fixture=Instantiate(lootPrefab,lockPoint,Quaternion.identity).GetComponent<LootPickup>();
                    fixture.initialChest=true; fixture.initialQuantity=0; fixture.initialLocked=true; fixture.initialLabLock=true;
                    fixture.initialFixtureKind=(byte)(value/3); fixture.initialLockGrade=(byte)(value%3); fixture.initialRarity=0;
                    fixture.GetComponent<NetworkObject>().Spawn(); Say("Fechadura criada. Aproxime-se e pressione E / Interagir."); break;
                case TestAction.ClearLocks:
                    foreach(var existing in new List<LootPickup>(LootPickup.All)) if(existing!=null && existing.IsSpawned && existing.LabLock.Value) existing.NetworkObject.Despawn();
                    Say("Fechaduras de teste removidas."); break;
            }
            TestRevision.Value++;
            return true;
        }
        void Say(string message) { TestMessage.Value=new FixedString128Bytes(message); Debug.Log("TEST LAB RESULT: "+message); }
        public static bool FindTestPoint(Vector2 origin,Vector2 forward,float radius,out Vector2 point)
        {
            for(int i=0;i<36;i++)
            {
                float angle=(i%9-4)*12*Mathf.Deg2Rad;
                Vector2 dir=new Vector2(forward.x*Mathf.Cos(angle)-forward.y*Mathf.Sin(angle),forward.x*Mathf.Sin(angle)+forward.y*Mathf.Cos(angle));
                point=origin+dir*(4.5f+i/9*1.5f);
                if(PlantEncounter.ClearSpawn(point,radius) && !Physics2D.Linecast(origin,point,GameConfig.WallMask)) return true;
            }
            // A menu blocks aim input. A sideways initial facing can point into the test pen's
            // walls; fall back to another visible, clear direction rather than force a menu exit.
            for(int i=0;i<36;i++)
            {
                float angle=i*Mathf.PI*2/12;
                point=origin+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(4.5f+i/12*1.5f);
                if(PlantEncounter.ClearSpawn(point,radius) && !Physics2D.Linecast(origin,point,GameConfig.WallMask)) return true;
            }
            point=default; return false;
        }
        static void TestMove(NetworkObject player,Vector2 at)
        {
            var body=player.GetComponent<Rigidbody2D>(); body.linearVelocity=Vector2.zero; body.position=at; player.transform.position=at;
            player.GetComponent<Unity.Netcode.Components.NetworkTransform>()?.Teleport(player.transform.position,player.transform.rotation,player.transform.localScale);
        }
        void ClearTestEnemies()
        {
            foreach(var monster in new List<Monster>(Monster.All)) if(monster!=null && monster.IsSpawned) monster.NetworkObject.Despawn();
            foreach(var plant in new List<PlantCreature>(PlantCreature.All)) if(plant!=null && plant.IsSpawned) plant.NetworkObject.Despawn();
            ClearTestEffects();
        }
        void ClearTestEffects()
        {
            MonsterEffects.Clear();
            foreach(var cloud in new List<SporeCloud>(SporeCloud.All)) if(cloud!=null && cloud.IsSpawned) cloud.NetworkObject.Despawn();
            foreach(var inv in PlayerInventory.All) if(inv!=null) { inv.ServerSlowUntil=0; inv.GetComponent<SporeContamination>()?.ServerResetExposure(); }
        }
    }
}
