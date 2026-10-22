using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Combat;
using CyberExtraction.Items;
using CyberExtraction.Players;
using CyberExtraction.Enemies;

namespace CyberExtraction.Match
{
    public partial class MatchManager
    {
        public NetworkVariable<bool> DungeonShortcut=new NetworkVariable<bool>(false);
        float nextDungeonRecovery,nextDungeonNoise;
        bool dungeonResetting;
        public bool DungeonReady(int id) => id>=0 && id<3 && (ActiveExtractions.Value&(1<<id))!=0;
        public bool DungeonActionNeeded(int id) => id>=0 && id<=3 && (id==3 ? !DungeonShortcut.Value : !DungeonReady(id));
        public bool CanOperateDungeon(PlayerInventory inv,int id)
        {
            if(inv==null || !inv.IsSpawned || !inv.IsAlive || inv.Lockpicking.Value || inv.DungeonWeaponBusy || inv.FishingPhase.Value!=0 ||
                MapBuilder.Current!=ExpeditionMap.Dungeon || CurrentState!=MatchState.Running || !DungeonActionNeeded(id)) return false;
            if((id==1 || id==3) && inv.Count(DungeonContent.RequiredItem(id))<1) return false;
            Vector2 position=inv.transform.position,target=DungeonContent.Mechanism(id);
            return Vector2.Distance(position,target)<=DungeonContent.InteractionRange &&
                Physics2D.Linecast(position,target,GameConfig.WallMask).collider==null;
        }
        public int NearbyDungeonAction(PlayerInventory inv)
        {
            if(MapBuilder.Current!=ExpeditionMap.Dungeon || inv==null) return -1;
            if(inv.DungeonAction.Value>=0)
            {
                int active=inv.DungeonAction.Value;
                // Replication of cancellation can lag behind leaving the mechanism.
                if(!DungeonActionNeeded(active)) return -1;
                Vector2 target=DungeonContent.Mechanism(active);
                return Vector2.Distance(inv.transform.position,target)<=DungeonContent.InteractionRange &&
                    Physics2D.Linecast(inv.transform.position,target,GameConfig.WallMask).collider==null ? active : -1;
            }
            float distance=DungeonContent.InteractionRange; int best=-1;
            for(int i=0;i<4;i++)
            {
                if(!DungeonActionNeeded(i)) continue;
                float d=Vector2.Distance(inv.transform.position,DungeonContent.Mechanism(i));
                if(d<distance && Physics2D.Linecast(inv.transform.position,DungeonContent.Mechanism(i),GameConfig.WallMask).collider==null)
                { distance=d; best=i; }
            }
            return best;
        }
        public int ContextDungeonAction(PlayerInventory inv,LootPickup chest)
        {
            int id=NearbyDungeonAction(inv);
            return id>=0 && (inv.DungeonAction.Value>=0 || chest==null || Vector2.Distance(inv.transform.position,DungeonContent.Mechanism(id))<Vector2.Distance(inv.transform.position,chest.transform.position)) ? id : -1;
        }
        public string DungeonActionLabel(PlayerInventory inv,int id)
        {
            if(inv.DungeonAction.Value==id) return DungeonContent.Action(id)+" · "+Mathf.CeilToInt((1-inv.DungeonActionProgress)*DungeonContent.Duration(id))+" s · cancelar";
            if((id==1 || id==3) && inv.Count(DungeonContent.RequiredItem(id))<1)
                return id==1 ? "Falta manivela · busque nos Contrapesos" : "Falta chave · busque na Prisão";
            return DungeonContent.Action(id)+" · "+DungeonContent.Duration(id)+" s";
        }
        public void ServerBeginDungeonAction(PlayerInventory inv,int id)
        {
            if(!IsServer || inv==null) return;
            if(id<0 || inv.DungeonAction.Value==id) { CancelDungeonAction(inv); return; }
            if(Time.time<inv.DungeonNextActionAt || !CanOperateDungeon(inv,id) || inv.DungeonAction.Value>=0) return;
            inv.DungeonNextActionAt=Time.time+.25f;
            var shooter=inv.GetComponent<PlayerShooter>(); if(shooter!=null) shooter.ServerCancelReload();
            inv.DungeonActionOrigin=inv.transform.position;
            inv.DungeonActionDamage=inv.DungeonDamageVersion;
            inv.DungeonActionEnds.Value=NetworkManager.ServerTime.Time+DungeonContent.Duration(id);
            inv.DungeonAction.Value=id;
            Noise.Emit(DungeonContent.Mechanism(id),18);
        }
        static void CancelDungeonAction(PlayerInventory inv)
        { inv.DungeonAction.Value=-1; inv.DungeonActionEnds.Value=0; }
        void TickDungeon()
        {
            if(!IsServer || MapBuilder.Current!=ExpeditionMap.Dungeon) return;
            foreach(var inv in PlayerInventory.All)
            {
                if(inv==null || !inv.IsSpawned || inv.DungeonAction.Value<0) continue;
                int id=inv.DungeonAction.Value;
                if(!CanOperateDungeon(inv,id) || ((Vector2)inv.transform.position-inv.DungeonActionOrigin).sqrMagnitude>.16f ||
                    inv.DungeonDamageVersion!=inv.DungeonActionDamage)
                { CancelDungeonAction(inv); continue; }
                if(NetworkManager.ServerTime.Time<inv.DungeonActionEnds.Value) continue;
                if((id==1 || id==3) && !inv.ServerRemoveItem(DungeonContent.RequiredItem(id),1)) { CancelDungeonAction(inv); continue; }
                if(id==3) { DungeonShortcut.Value=true; DungeonLayout.SetShortcutOpen(true); }
                else ActiveExtractions.Value|=(ushort)(1<<id);
                inv.ServerAwardObjective("dungeon:"+id,id==3 ? 15 : 25);
                Noise.Emit(DungeonContent.Mechanism(id),24); CancelDungeonAction(inv);
            }
            if(Time.time>=nextDungeonNoise)
            {
                foreach(var inv in PlayerInventory.All) if(inv!=null && inv.IsSpawned && inv.DungeonAction.Value>=0)
                    Noise.Emit(DungeonContent.Mechanism(inv.DungeonAction.Value),18);
                nextDungeonNoise=Time.time+1;
            }
            if(Time.time>=nextDungeonRecovery)
            { RecoverDungeonTool(ItemId.DungeonCrank); RecoverDungeonTool(ItemId.DungeonKey); nextDungeonRecovery=Time.time+1; }
        }
        void ResetDungeon(bool resetting=false)
        {
            dungeonResetting=resetting;
            DungeonShortcut.Value=false; DungeonLayout.SetShortcutOpen(false); nextDungeonRecovery=nextDungeonNoise=0;
            foreach(var inv in PlayerInventory.All) if(inv!=null && inv.IsSpawned) CancelDungeonAction(inv);
        }
        public void ServerReleaseDungeonTools(PlayerInventory inv)
        {
            if(!IsServer || MapBuilder.Current!=ExpeditionMap.Dungeon || inv==null) return;
            CancelDungeonAction(inv);
            foreach(var item in new[]{ItemId.DungeonCrank,ItemId.DungeonKey})
            {
                int count=inv.Count(item); if(count<=0) continue;
                inv.ServerRemoveItem(item,count);
                if(CurrentState==MatchState.Running) RecoverDungeonTool(item);
            }
        }
        bool DungeonToolUsed(ItemId item) => item==ItemId.DungeonCrank ? DungeonReady(1) : DungeonShortcut.Value;
        void RecoverDungeonTool(ItemId item)
        {
            if(dungeonResetting || CurrentState!=MatchState.Running || DungeonToolUsed(item)) return;
            foreach(var inv in PlayerInventory.All)
                if(inv!=null && inv.IsSpawned && inv.IsAlive && inv.Count(item)>0) return;
            // Tools may be dropped into an existing loot container through the normal inventory UI.
            for(int i=LootPickup.All.Count-1;i>=0;i--)
            {
                var loot=LootPickup.All[i]; if(loot==null || !loot.IsSpawned) continue;
                bool contains=!loot.Chest.Value && loot.Item.Value==(byte)item;
                if(loot.Chest.Value) for(int slot=0;slot<loot.Contents.Count;slot++) if(!loot.Contents[slot].Empty && loot.Contents[slot].Item==item) { contains=true; break; }
                if(!contains) continue;
                if(DungeonLayout.IsFloor(loot.transform.position) && Physics2D.OverlapCircle(loot.transform.position,.3f,GameConfig.WallMask)==null) return;
                if(!loot.Chest.Value) loot.NetworkObject.Despawn();
                else for(int s=0;s<loot.Contents.Count;s++) if(loot.Contents[s].Item==item) loot.Contents[s]=default;
            }
            SpawnItem(item==ItemId.DungeonCrank ? DungeonContent.CrankPedestal : DungeonContent.KeyPedestal,item,1);
        }
        void SpawnDungeonContent()
        {
            if(lootPrefab==null || monsterPrefab==null) throw new System.InvalidOperationException("Catacumbas requerem os prefabs existentes de saque e monstro.");
            foreach(var slot in DungeonContent.Loot)
            {
                if(!slot.Valuable) { SpawnRolledLoot(slot.Position,new LootContext(ExpeditionMap.Dungeon,LootSource.Ground,slot.Risk)); continue; }
                var chest=Instantiate(lootPrefab,slot.Position,Quaternion.identity); var loot=chest.GetComponent<LootPickup>();
                loot.initialChest=true; loot.initialQuantity=0;
                loot.initialRarity=(byte)Mathf.Max(1,ExpeditionRolls.ContainerGrade());
                loot.initialLocked=ExpeditionRolls.Chance(30); loot.initialLockGrade=loot.initialRarity;
                loot.ConfigureLoot(new LootContext(ExpeditionMap.Dungeon,LootSource.ValuableContainer,slot.Risk));
                chest.GetComponent<NetworkObject>().Spawn();
            }
            SpawnItem(DungeonContent.CrankPedestal,ItemId.DungeonCrank,1); SpawnItem(DungeonContent.KeyPedestal,ItemId.DungeonKey,1);
            foreach(var encounter in DungeonContent.Encounters)
            {
                Vector2 position=encounter.Position; bool free=false;
                var room=DungeonLayout.WorldRect(DungeonLayout.Rooms[encounter.Room].Cells);
                for(int attempt=0;attempt<64;attempt++)
                {
                    var offset=attempt==0 ? Vector2.zero : new Vector2(Mathf.Cos(attempt*2.4f),Mathf.Sin(attempt*2.4f))*(.4f+attempt*.12f);
                    position=encounter.Position+offset;
                    if(!room.Contains(position) || !DungeonLayout.IsFloor(position) || Physics2D.OverlapCircle(position,.85f,GameConfig.WallMask)!=null) continue;
                    bool safe=true;
                    foreach(var spawn in DungeonLayout.Spawns) if(Vector2.Distance(position,spawn)<10) safe=false;
                    foreach(var exit in DungeonLayout.ExitPositions) if(Vector2.Distance(position,exit)<4) safe=false;
                    foreach(var other in Monster.All) if(other!=null && Vector2.Distance(position,other.transform.position)<2.5f) safe=false;
                    foreach(var slot in DungeonContent.Loot) if(Vector2.Distance(position,slot.Position)<2) safe=false;
                    if(safe) { free=true; break; }
                }
                if(!free) throw new System.InvalidOperationException("Sem espaço para encontro: "+DungeonLayout.Rooms[encounter.Room].Name);
                var monster=Instantiate(monsterPrefab,position,Quaternion.identity);
                monster.GetComponent<Monster>().Configure(encounter.Kind); monster.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}
