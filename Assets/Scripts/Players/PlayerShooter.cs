using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Combat;
using CyberExtraction.Match;
using CyberExtraction.Items;

namespace CyberExtraction.Players
{
    // All weapon timing, ammunition and pellet damage are resolved on the server.
    public class PlayerShooter : NetworkBehaviour
    {
        PlayerInventory inv;
        float recycledMetal, serverSpeed;
        Vector2 lastPos;
        double nextShotAt;
        int reloadSlot = -1;
        ItemStack reloadStack;
        public NetworkVariable<double> ReloadEndsAt = new NetworkVariable<double>(0);
        public NetworkVariable<double> BoltEndsAt = new NetworkVariable<double>(0);
        double Now => NetworkManager != null ? NetworkManager.ServerTime.Time : 0;
        public bool Reloading => ReloadEndsAt.Value > Now;
        public bool CyclingBolt => BoltEndsAt.Value > Now;
        public WeaponDefinition Weapon => inv != null ? inv.ActiveWeapon : null;
        public int Magazine => inv != null ? inv.WeaponRounds : 0;
        public int Reserve => Weapon != null ? inv.Count(Weapon.Ammo) : 0;
        public float FireCooldown => Weapon != null ? Weapon.Cooldown : GameConfig.FireCooldown;
        public float Range => Weapon != null ? Weapon.Range : GameConfig.AimRange;
        public Vector2 MuzzlePosition
        {
            get
            {
                // Model coordinates projected by the 2.65-unit orthographic render
                // into a 3.4-unit sprite. Upper body and weapon stay fixed across frames.
                float modelLength = Weapon == null ? 0f : Weapon.Item == ItemId.Mosin ? 1.15f :
                    Weapon.Item == ItemId.DoubleBarrelShotgun ? 1.10f : 1.025f;
                return transform.TransformPoint(new Vector3(modelLength * (3.4f / 2.65f), 0f, 0f));
            }
        }
        bool CanAct => IsServer && inv != null && inv.IsAlive && !inv.Lockpicking.Value && inv.FishingPhase.Value == 0 &&
            Weapon != null && MatchManager.Instance != null && MatchManager.Instance.CurrentState == MatchState.Running;
        bool SameReloadWeapon => reloadSlot >= 0 && inv.ActiveWeaponSlot == reloadSlot &&
            reloadSlot < inv.Slots.Count && inv.Slots[reloadSlot].Equals(reloadStack);
        void Awake()
        {
            inv=GetComponent<PlayerInventory>(); lastPos=transform.position;
            if(GetComponent<WeaponVisual>()==null) gameObject.AddComponent<WeaponVisual>();
        }
        public override void OnNetworkSpawn() { lastPos=transform.position; }
        void Update()
        {
            if(!IsServer) return;
            if(ReloadEndsAt.Value>0)
            {
                if(!CanAct || !SameReloadWeapon) ServerCancelReload();
                else if(Now>=ReloadEndsAt.Value)
                {
                    var weapon=Weapon; int amount=Mathf.Min(weapon.Capacity-Magazine,Reserve), rounds=Magazine;
                    ServerCancelReload();
                    if(amount>0 && inv.ServerRemoveItem(weapon.Ammo,amount)) inv.ServerSetRounds(rounds+amount);
                }
            }
            if(BoltEndsAt.Value>0 && (!CanAct || Now>=BoltEndsAt.Value)) BoltEndsAt.Value=0;
            Vector2 position=transform.position;
            if(Time.deltaTime>0) serverSpeed=Mathf.Lerp(serverSpeed,(position-lastPos).magnitude/Time.deltaTime,.2f);
            lastPos=position;
        }
        public void RequestFire(float angle) { if(IsOwner) FireServerRpc(angle); }
        public void RequestReload() { if(IsOwner) ReloadServerRpc(); }
        [ServerRpc] void FireServerRpc(float angle) { ServerFire(angle); }
        [ServerRpc] void ReloadServerRpc() { ServerReload(); }
        public void ServerCancelReload()
        {
            if(!IsServer) return;
            ReloadEndsAt.Value=0; reloadSlot=-1; reloadStack=default;
        }
        public void ServerReload()
        {
            if(!CanAct || Reloading || CyclingBolt || Magazine>=Weapon.Capacity || Reserve<=0) return;
            reloadSlot=inv.ActiveWeaponSlot; reloadStack=inv.Slots[reloadSlot];
            ReloadEndsAt.Value=Now+Weapon.ReloadSeconds;
        }
        public void ServerFire(float angle)
        {
            if(!CanAct || float.IsNaN(angle) || float.IsInfinity(angle) || Reloading || CyclingBolt || Now<nextShotAt) return;
            if(Magazine<=0) { ServerReload(); return; }
            var weapon=Weapon;
            Vector2 origin=MuzzlePosition;
            // A protruding sprite must not let the barrel shoot through an obstacle.
            // Reject before spending rounds or starting cooldown when the barrel is blocked.
            if(Physics2D.Linecast(transform.position,origin,GameConfig.WallMask).collider!=null ||
                Physics2D.OverlapPoint(origin,GameConfig.WallMask)!=null) return;
            nextShotAt=Now+weapon.Cooldown;
            if(weapon.BoltSeconds>0) BoltEndsAt.Value=Now+weapon.BoltSeconds;
            inv.ServerSetRounds(Magazine-1);
            recycledMetal+=(float)GameplayDefinition.Current.MetalRecipeCost/WeaponCatalog.RecipeOutput(weapon.Ammo)*Mathf.Clamp(inv.RecyclerLevel.Value,0,3)*.1f;
            if(recycledMetal>=1)
            {
                recycledMetal-=1;
                if(!inv.ServerAddItem(ItemId.Metal,1)) MatchManager.Instance.ServerDropItem(transform.position,ItemId.Metal,1);
            }
            float half=weapon.HalfAngle(serverSpeed/GameConfig.PlayerSpeed);
            for(int pellet=0;pellet<weapon.Pellets;pellet++)
            {
                float radians=(angle+Random.Range(-half,half))*Mathf.Deg2Rad;
                Vector2 direction=new Vector2(Mathf.Cos(radians),Mathf.Sin(radians)), end=origin+direction*weapon.Range;
                Health victim=null;
                foreach(var hit in Physics2D.RaycastAll(origin,direction,weapon.Range))
                {
                    var monster=hit.collider.GetComponentInParent<CyberExtraction.Enemies.Monster>();
                    if(hit.collider.isTrigger && (monster==null || !monster.Disguised)) continue;
                    if(hit.collider.GetComponentInParent<NetworkObject>()==NetworkObject) continue;
                    var h=hit.collider.GetComponentInParent<Health>();
                    if(h!=null && h.IsDead) continue;
                    victim=h; end=hit.point; break;
                }
                if(CyberExtraction.Enemies.MonsterWorld.ShootWeb(origin,direction,Vector2.Distance(origin,end),out var webImpact)) end=webImpact;
                else if(victim!=null)
                {
                    int damage=weapon.DamageAt(Vector2.Distance(origin,end));
                    var monster=victim.GetComponent<CyberExtraction.Enemies.Monster>();
                    victim.ServerTakeDamage(monster!=null ? monster.ProjectileDamage(damage,origin) : damage);
                }
                CyberExtraction.UI.NetworkBootstrap.Instance.BroadcastShot(origin,end,pellet==0,weapon.NoiseRadius);
            }
            Noise.Emit(origin,weapon.NoiseRadius);
        }
    }
}
