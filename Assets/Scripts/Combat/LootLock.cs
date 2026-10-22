using System;
using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Items;
using CyberExtraction.Lockpicking;
using CyberExtraction.Players;
using CyberExtraction.UI;
using CyberExtraction.Match;

namespace CyberExtraction.Combat
{
    [Serializable] public class LockpickSnapshot
    {
        public string Token, Message;
        public int PinCount, ActivePin;
        public bool[] SetPins;
        public bool Active, Held, Cue, Broken, Unlocked;
        public float Depth, Tension;
        public double At;
    }

    public partial class LootPickup
    {
        public bool initialLocked, initialLabLock;
        public byte initialLockGrade, initialFixtureKind;
        public NetworkVariable<bool> Locked=new NetworkVariable<bool>(false);
        public NetworkVariable<byte> LockGrade=new NetworkVariable<byte>(0), FixtureKind=new NetworkVariable<byte>(0);
        public NetworkVariable<bool> LabLock=new NetworkVariable<bool>(false);
        LockpickSession mechanism;
        LockpickTool pick;
        PlayerInventory lockPlayer;
        ulong lockOwner;
        Guid lockToken;
        int lockSlot=-1, lockDamage;
        ItemStack boundTool;
        Vector2 lockOrigin;
        float nextLockSnapshot;
        string lockMessage="";
        BoxCollider2D doorCollider;
        SpriteRenderer fixtureArt;
        Collider2D fixtureCollider;
        public Vector2 SightPoint(Vector2 from) => fixtureCollider!=null && fixtureCollider.enabled ? fixtureCollider.ClosestPoint(from) : (Vector2)transform.position;
        public bool SightLineClear(Vector2 from)
        {
            var hit=Physics2D.Linecast(from,SightPoint(from),GameConfig.VisionMask).collider;
            var marker=hit!=null ? hit.GetComponent<FurnitureObstacle>() : null;
            return hit==null || hit.GetComponentInParent<LootPickup>()==this || marker!=null && marker.BelongsTo(transform.position);
        }
        public void RebindFurnitureObstacle()
        {
            if(FixtureKind.Value!=1 && FixtureKind.Value!=2) return;
            foreach(var collider in Physics2D.OverlapPointAll(transform.position,GameConfig.WallMask))
            {
                var marker=collider.GetComponent<FurnitureObstacle>();
                if(marker==null || !marker.BelongsTo(transform.position)) continue;
                foreach(var owned in GetComponents<Collider2D>()) owned.enabled=false;
                marker.Container=this; fixtureCollider=collider; return;
            }
        }

        void InitializeLock()
        {
            if(IsServer)
            {
                Locked.Value=initialLocked && initialChest;
                LockGrade.Value=(byte)Mathf.Clamp(initialLockGrade,0,2); FixtureKind.Value=(byte)Mathf.Clamp(initialFixtureKind,0,3);
                LabLock.Value=initialLabLock;
                mechanism=new LockpickSession(new LockDefinition((CyberExtraction.Lockpicking.LockGrade)LockGrade.Value),Guid.NewGuid().GetHashCode());
                mechanism.Signalled+=LockSignal;
            }
            Locked.OnValueChanged+=LockChanged;
        }
        public bool LockLineClear(Vector2 from,int obstacleMask=GameConfig.WallMask)
        {
            var hit=Physics2D.Linecast(from,transform.position,obstacleMask).collider;
            var fixture=hit!=null ? hit.GetComponent<FurnitureObstacle>() : null;
            return hit==null || hit.GetComponentInParent<LootPickup>()==this || (fixture!=null && fixture.BelongsTo(transform.position));
        }
        void BuildFixtureAppearance()
        {
            var source=GetComponent<SpriteRenderer>(); source.enabled=false;
            var root=new GameObject("LockedFixture"); root.transform.SetParent(transform,false);
            string spriteName=FixtureKind.Value==1 ? "Wardrobe" : "Cabinet";
            if(FixtureKind.Value!=3)
            {
                transform.localScale=FixtureKind.Value==1 ? new Vector3(1.7f,1.3f,1) : new Vector3(1.4f,1.1f,1);
                foreach(var collider in GetComponents<Collider2D>()) collider.enabled=false;
                gameObject.layer=FixtureKind.Value==1 ? GameConfig.WallLayer : GameConfig.LowObstacleLayer;
                FurnitureObstacle existing=null;
                foreach(var collider in Physics2D.OverlapPointAll(transform.position,GameConfig.WallMask))
                {
                    var marker=collider.GetComponent<FurnitureObstacle>();
                    if(marker!=null && marker.BelongsTo(transform.position)) { existing=marker; break; }
                }
                if(existing!=null) { existing.Container=this; fixtureCollider=existing.GetComponent<Collider2D>(); }
                else { var collider=gameObject.AddComponent<BoxCollider2D>(); collider.size=Vector2.one; fixtureCollider=collider; }
                var sprite=FixtureKind.Value==1 ? WardrobeArt.Get(OpenState.Value==2) : EnvironmentSprites.Get(spriteName);
                if(sprite!=null)
                {
                    fixtureArt=root.AddComponent<SpriteRenderer>(); SpriteFactory.Setup(fixtureArt,sprite,Color.white,5);
                    if(FixtureKind.Value!=1 && EnvironmentSprites.Material!=null) fixtureArt.sharedMaterial=EnvironmentSprites.Material;
                    root.transform.localScale=new Vector3(1/sprite.bounds.size.x,1/sprite.bounds.size.y,1);
                    if(FixtureKind.Value==1) root.transform.localScale=new Vector3(100f/690f,100f/350f,1);
                }
                else CyberExtraction.Enemies.ChestAppearance.Build(transform,new Color(.32f,.25f,.16f));
                OpenState.OnValueChanged+=FixtureOpened;
            }
            else
            {
                transform.localScale=new Vector3(2.6f,1,1);
                foreach(var collider in GetComponents<Collider2D>()) collider.enabled=false;
                doorCollider=gameObject.AddComponent<BoxCollider2D>(); doorCollider.size=new Vector2(1,.36f); doorCollider.enabled=Locked.Value;
                fixtureCollider=doorCollider;
                gameObject.layer=GameConfig.WallLayer;
                CyberExtraction.Enemies.ChestAppearance.Part(root.transform,"OldDoor",SpriteFactory.Square,Vector2.zero,new Vector2(1,.4f),new Color(.35f,.26f,.16f),5);
                CyberExtraction.Enemies.ChestAppearance.Part(root.transform,"DoorBand",SpriteFactory.Square,Vector2.zero,new Vector2(.08f,.4f),new Color(.36f,.38f,.32f),6);
            }
        }
        void FixtureOpened(byte previous,byte current)
        {
            if(fixtureArt!=null && FixtureKind.Value==1) fixtureArt.sprite=WardrobeArt.Get(current==2);
            else if(FixtureKind.Value==2)
            {
                var root=transform.Find("LockedFixture");
                if(root!=null) root.localRotation=Quaternion.Euler(0,0,current==2 ? 12f : 0f);
            }
        }
        void LockChanged(bool oldValue,bool newValue)
        {
            if(doorCollider!=null) doorCollider.enabled=newValue;
            if(FixtureKind.Value==3 && !newValue)
            {
                var art=transform.Find("LockedFixture"); if(art!=null) art.localRotation=Quaternion.Euler(0,0,80);
            }
        }

        void BeginLock(PlayerInventory inv,ulong actor)
        {
            if(!IsServer || !Locked.Value || lockPlayer!=null || inv.Lockpicking.Value || inv.FishingPhase.Value!=0 || inv.GetComponent<PlayerShooter>() is PlayerShooter attacker && attacker.MeleeAttacking) return;
            int slot=-1;
            for(int i=0;i<inv.Slots.Count;i++) if(!inv.Slots[i].Empty && inv.Slots[i].Item==ItemId.Lockpick && inv.Slots[i].Quantity>0) { slot=i; break; }
            if(slot<0) { SendLock(actor,false,"Precisa de uma gazua. No laboratorio, use Kit de gazuas."); return; }
            boundTool=inv.Slots[slot];
            pick=new LockpickTool(()=>Math.Min(.999999999,(double)UnityEngine.Random.value),accumulatedErrors:Mathf.Max(0,boundTool.Data));
            if(!mechanism.TryBegin(actor,pick,true,NetworkManager.ServerTime.Time,out lockToken)) return;
            lockOwner=actor; lockSlot=slot; lockPlayer=inv; lockDamage=inv.GetComponent<Health>().DamageVersion.Value; lockOrigin=inv.transform.position;
            inv.GetComponent<PlayerShooter>()?.ServerCancelReload(); inv.Lockpicking.Value=true;
            lockMessage="Selecione um pino. Segure e solte no pequeno clique."; SendLock(actor,true,lockMessage);
        }
        void LockSignal(LockpickSignal signal)
        {
            if(lockPlayer==null) return;
            if(signal==LockpickSignal.Mistake)
            {
                var grid=lockPlayer.Grid(); var stack=grid.Slots[lockSlot];
                if(pick.Broken) { stack.Quantity--; stack.Data=0; grid.Slots[lockSlot]=stack.Quantity>0 ? stack : default; }
                else { stack.Data=pick.AccumulatedErrors; grid.Slots[lockSlot]=stack; boundTool=stack; }
                lockPlayer.ServerSetGrid(grid); Noise.Emit(transform.position,14);
                lockMessage=pick.Broken ? "A gazua quebrou. Use outra para continuar." : "O metal rangeu. O pino escapou.";
            }
            else if(signal==LockpickSignal.ClickCue) lockMessage="O pino oscilou. Solte no encaixe.";
            else if(signal==LockpickSignal.PinSet) lockMessage="Pino encaixado. Selecione o proximo.";
            else if(signal==LockpickSignal.Pressed) lockMessage="Pressionando...";
            else if(signal==LockpickSignal.Unlocked)
            {
                Locked.Value=false; OpenState.Value=2; OpenProgress.Value=1;
                SendLock(lockOwner,false,"Fechadura destrancada."); FinishLock();
            }
            else if(signal==LockpickSignal.Cancelled || signal==LockpickSignal.PickBroken)
            {
                SendLock(lockOwner,false,signal==LockpickSignal.PickBroken ? "A gazua quebrou. Use outra para continuar." : "Tentativa interrompida."); FinishLock();
            }
        }
        bool BoundToolValid() => lockPlayer!=null && lockSlot>=0 && lockSlot<lockPlayer.Slots.Count && lockPlayer.Slots[lockSlot].Equals(boundTool);
        bool LockContextValid()=>BoundToolValid() && Sender(lockOwner)==lockPlayer && lockPlayer.GetComponent<Health>().DamageVersion.Value==lockDamage && Vector2.Distance(lockOrigin,lockPlayer.transform.position)<=.5f;
        void TickLock()
        {
            if(!IsServer || lockPlayer==null) return;
            bool interrupted=!LockContextValid();
            mechanism.Tick(NetworkManager.ServerTime.Time,true,interrupted);
            if(lockPlayer!=null && Time.unscaledTime>=nextLockSnapshot) { nextLockSnapshot=Time.unscaledTime+.08f; SendLock(lockOwner,true,lockMessage); }
        }
        void FinishLock() { if(lockPlayer!=null && lockPlayer.IsSpawned) lockPlayer.Lockpicking.Value=false; lockPlayer=null; pick=null; lockSlot=-1; lockToken=Guid.Empty; }
        void CancelLock(bool notify=true)
        {
            if(!IsServer || lockPlayer==null) return;
            if(notify) mechanism.Tick(NetworkManager.ServerTime.Time,false,true);
            else { mechanism.Signalled-=LockSignal; FinishLock(); }
        }
        public void RequestPin(string token,long sequence,int pin,bool press) { PinRpc(token,sequence,pin,press); }
        public void RequestCancelLock(string token,long sequence) { CancelLockRpc(token,sequence); }
        [Rpc(SendTo.Server,InvokePermission=RpcInvokePermission.Everyone)]
        void PinRpc(string token,long sequence,int pin,bool press,RpcParams rpc=default)
        {
            if(lockPlayer==null || rpc.Receive.SenderClientId!=lockOwner || token==null || token.Length!=32 || !Guid.TryParseExact(token,"N",out var id)) return;
            if(!LockContextValid()) { mechanism.Tick(NetworkManager.ServerTime.Time,false,true); return; }
            if(press) mechanism.TryPress(lockOwner,id,sequence,pin,true,NetworkManager.ServerTime.Time);
            else mechanism.TryRelease(lockOwner,id,sequence,true,NetworkManager.ServerTime.Time);
            if(lockPlayer!=null) SendLock(lockOwner,true,lockMessage);
        }
        [Rpc(SendTo.Server,InvokePermission=RpcInvokePermission.Everyone)]
        void CancelLockRpc(string token,long sequence,RpcParams rpc=default)
        {
            if(lockPlayer==null || rpc.Receive.SenderClientId!=lockOwner || token==null || token.Length!=32 || !Guid.TryParseExact(token,"N",out var id)) return;
            mechanism.TryCancel(lockOwner,id,sequence,NetworkManager.ServerTime.Time);
        }
        void SendLock(ulong actor,bool active,string message)
        {
            if(!IsSpawned || !NetworkManager.ConnectedClients.ContainsKey(actor)) return;
            var view=mechanism.ViewFor(actor); var pins=new bool[view.SetPins.Count]; for(int i=0;i<pins.Length;i++) pins[i]=view.SetPins[i];
            if(active && view.Held && !view.ClickCue && view.Tension>0) message="Passou do encaixe. Alivie a pressao.";
            var snapshot=new LockpickSnapshot { Token=lockToken.ToString("N"),Message=message,PinCount=pins.Length,SetPins=pins,ActivePin=view.ActivePin,Active=active,
                Held=view.Held,Cue=view.ClickCue,Broken=pick!=null && pick.Broken,Unlocked=!Locked.Value,Depth=(float)view.Depth,Tension=(float)view.Tension,At=NetworkManager.ServerTime.Time };
            LockViewClientRpc(JsonUtility.ToJson(snapshot),new ClientRpcParams {Send=new ClientRpcSendParams {TargetClientIds=new[]{actor}}});
        }
        [ClientRpc] void LockViewClientRpc(string json,ClientRpcParams rpc=default) { LockpickWindow.Receive(this,JsonUtility.FromJson<LockpickSnapshot>(json)); }
    }
}
