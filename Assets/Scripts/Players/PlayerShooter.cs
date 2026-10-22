using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Combat;
using CyberExtraction.Match;
using CyberExtraction.Items;

namespace CyberExtraction.Players
{
    // All weapon timing, ammunition and pellet damage are resolved on the server.
    public partial class PlayerShooter : NetworkBehaviour
    {
        PlayerInventory inv;
        PlayerController movement;
        float recycledMetal, serverSpeed;
        Vector2 lastPos;
        double nextShotAt;
        float nextAutoReloadRequest;
        int reloadSlot = -1;
        ItemStack reloadStack;
        public NetworkVariable<double> ReloadEndsAt = new NetworkVariable<double>(0);
        // Public presentation state; never expose the private inventory/reserve to observers.
        public NetworkVariable<byte> ReloadRounds = new NetworkVariable<byte>(0);
        public NetworkVariable<uint> ShotSequence = new NetworkVariable<uint>(0);
        public NetworkVariable<double> BoltEndsAt = new NetworkVariable<double>(0);
        public NetworkVariable<float> AimSpeed01 = new NetworkVariable<float>(0,NetworkVariableReadPermission.Owner);
        double Now => NetworkManager != null ? NetworkManager.ServerTime.Time : 0;
        public bool Reloading => ReloadEndsAt.Value > Now;
        public bool CyclingBolt => BoltEndsAt.Value > Now;
        public WeaponDefinition Weapon => inv != null ? inv.ActiveWeapon : null;
        public int Magazine => inv != null ? inv.WeaponRounds : 0;
        public int Reserve => Weapon != null && !Weapon.IsMelee ? inv.Count(Weapon.Ammo) : 0;
        public float FireCooldown => Weapon != null ? Weapon.Cooldown : GameConfig.FireCooldown;
        public float Range => Weapon != null ? Weapon.Range : GameConfig.AimRange;
        public float AimHalfAngle => Weapon==null ? GameConfig.AimHalfAngleStill : Weapon.IsMelee ? Weapon.StillHalfAngle :
            Weapon.StructuralHalfAngle+Weapon.PointingHalfAngle(AimSpeed01.Value,inv.AimDispersionMultiplier);
        public int MeleeDamage => Weapon!=null && Weapon.IsMelee ? Mathf.RoundToInt(Weapon.Damage*inv.MeleeDamageMultiplier) : 0;
        public Vector2 MuzzlePosition
        {
            get
            {
                // Model coordinates projected by the 2.65-unit orthographic render
                // into a 3.4-unit sprite. Upper body and weapon stay fixed across frames.
                bool crossbow=Weapon!=null && Weapon.Item==ItemId.ImprovisedHandCrossbow;
                float modelLength = crossbow ? .96f : 0f;
                return transform.TransformPoint(new Vector3(modelLength * (3.4f / 2.65f), crossbow ? -.19f*(3.4f/2.65f) : 0f, 0f));
            }
        }
        bool CanAct => IsServer && inv != null && inv.IsAlive && !inv.SwitchingWeapon && !inv.ChangingEquipment && !inv.Lockpicking.Value && inv.DungeonAction.Value<0 && inv.FishingPhase.Value == 0 &&
            Weapon != null && MatchManager.Instance != null && MatchManager.Instance.CurrentState == MatchState.Running;
        bool SameReloadWeapon => reloadSlot >= 0 && inv.ActiveWeaponSlot == reloadSlot &&
            reloadSlot < inv.Equipment.Count && inv.Equipment[reloadSlot].Equals(reloadStack);
        void Awake()
        {
            inv=GetComponent<PlayerInventory>(); lastPos=transform.position;
            movement=GetComponent<PlayerController>();
            if(GetComponent<WeaponVisual>()==null) gameObject.AddComponent<WeaponVisual>();
            if(!Application.isBatchMode && GetComponent<MeleeWeaponVisual>()==null) gameObject.AddComponent<MeleeWeaponVisual>();
        }
        public override void OnNetworkSpawn()
        {
            lastPos=transform.position;
            nextAutoReloadRequest=0;
        }
        void Update()
        {
            if(!IsServer) return;
            StepMelee();
            if(ReloadEndsAt.Value>0)
            {
                if(!CanAct || !SameReloadWeapon) ServerCancelReload();
                else if(Now>=ReloadEndsAt.Value)
                {
                    var weapon=Weapon; int amount=Mathf.Min(weapon.Capacity-Magazine,Reserve);
                    ServerCancelReload();
                    if(amount>0) inv.ServerLoadRounds(amount);
                }
            }
            if(BoltEndsAt.Value>0 && (!CanAct || Now>=BoltEndsAt.Value)) BoltEndsAt.Value=0;
            Vector2 position=transform.position;
            if(Time.deltaTime>0) serverSpeed=Mathf.Lerp(serverSpeed,(position-lastPos).magnitude/Time.deltaTime,.2f);
            AimSpeed01.Value=Mathf.Clamp01(serverSpeed/GameConfig.PlayerSpeed);
            lastPos=position;
        }
        public void RequestFire(float angle) { if(IsOwner) FireServerRpc(angle); }
        public void RequestReload() { if(IsOwner) ReloadServerRpc(); }
        public void RequestAutomaticReload()
        {
            if(!IsOwner || Time.unscaledTime<nextAutoReloadRequest || !GameInput.GameplayActive || UI.UiLayout.BlocksGameplay ||
                inv==null || !inv.IsAlive || inv.SwitchingWeapon || inv.ChangingEquipment || inv.Lockpicking.Value ||
                inv.DungeonAction.Value>=0 || inv.FishingPhase.Value!=0 || Weapon==null || Weapon.IsMelee ||
                Magazine>0 || Reserve<=0 || Reloading || CyclingBolt || MeleeAttacking ||
                MatchManager.Instance==null || MatchManager.Instance.CurrentState!=MatchState.Running) return;
            // Aguarda a replicacao do servidor antes de repetir uma solicitacao.
            nextAutoReloadRequest=Time.unscaledTime+.5f;
            RequestReload();
        }
        [ServerRpc] void FireServerRpc(float angle) { ServerFire(angle); }
        [ServerRpc] void ReloadServerRpc() { ServerReload(); }
        public void ServerCancelReload()
        {
            if(!IsServer) return;
            ReloadEndsAt.Value=0; ReloadRounds.Value=0; reloadSlot=-1; reloadStack=default;
        }
        public void ServerReload()
        {
            if(!CanAct || Weapon.IsMelee || MeleeAttacking || Reloading || CyclingBolt || Magazine>=Weapon.Capacity || Reserve<=0) return;
            reloadSlot=inv.ActiveWeaponSlot; reloadStack=inv.Equipment[reloadSlot];
            ReloadRounds.Value=(byte)Mathf.Clamp(Mathf.Min(Weapon.Capacity-Magazine,Reserve),0,255);
            ReloadEndsAt.Value=Now+Weapon.ReloadSeconds;
            movement?.ServerStopSprint(Weapon.ReloadSeconds);
        }
        public void ServerFire(float angle)
        {
            if(IsServer && !float.IsNaN(angle) && !float.IsInfinity(angle)) movement?.ServerStopSprint(FireCooldown);
            if(IsServer && inv!=null && inv.ChangingEquipment) { inv.ServerCancelEquipmentChange(); return; }
            if(!CanAct || float.IsNaN(angle) || float.IsInfinity(angle) || MeleeAttacking || Reloading || CyclingBolt || Now<nextShotAt) return;
            if(Weapon.IsMelee) { ServerBeginMelee(angle); return; }
            if(Magazine<=0) { ServerReload(); return; }
            var weapon=Weapon;
            Vector2 origin=MuzzlePosition;
            // A protruding sprite must not let the barrel shoot through an obstacle.
            // Reject before spending rounds or starting cooldown when the barrel is blocked.
            if(Physics2D.Linecast(transform.position,origin,GameConfig.WallMask).collider!=null ||
                Physics2D.OverlapPoint(origin,GameConfig.WallMask)!=null) return;
            nextShotAt=Now+weapon.Cooldown;
            if(weapon.BoltSeconds>0) BoltEndsAt.Value=Now+weapon.BoltSeconds;
            bool recoveryShot=inv.Equipment[inv.ActiveWeaponSlot].Instance.RecoveryRounds>0;
            inv.ServerSetRounds(Magazine-1);
            ShotSequence.Value=unchecked(ShotSequence.Value+1);
            int recipeOutput=WeaponCatalog.RecipeOutput(weapon.Ammo);
            if(!recoveryShot && recipeOutput>0) recycledMetal+=(float)GameplayDefinition.Current.MetalRecipeCost/recipeOutput*Mathf.Clamp(inv.RecyclerLevel.Value,0,3)*.1f;
            if(recycledMetal>=1)
            {
                recycledMetal-=1;
                if(!inv.ServerAddItem(ItemId.Metal,1)) MatchManager.Instance.ServerDropItem(transform.position,ItemId.Metal,1);
            }
            float pointing=weapon.PointingHalfAngle(AimSpeed01.Value,inv.AimDispersionMultiplier);
            float aimError=Random.Range(-pointing,pointing);
            for(int pellet=0;pellet<weapon.Pellets;pellet++)
            {
                float radians=(angle+aimError+Random.Range(-weapon.StructuralHalfAngle,weapon.StructuralHalfAngle))*Mathf.Deg2Rad;
                Vector2 direction=new Vector2(Mathf.Cos(radians),Mathf.Sin(radians)), end=origin+direction*weapon.Range;
                Health victim=null;
                DungeonBreakable breakable=null;
                foreach(var hit in Physics2D.RaycastAll(origin,direction,weapon.Range))
                {
                    var monster=hit.collider.GetComponentInParent<CyberExtraction.Enemies.Monster>();
                    var prop=hit.collider.GetComponentInParent<DungeonBreakable>();
                    if(prop!=null && prop.IsSpawned && !prop.Broken.Value) { breakable=prop; end=hit.point; break; }
                    if(hit.collider.isTrigger && (monster==null || !monster.Disguised)) continue;
                    if(hit.collider.GetComponentInParent<NetworkObject>()==NetworkObject) continue;
                    var h=hit.collider.GetComponentInParent<Health>();
                    if(h!=null && h.IsDead) continue;
                    victim=h; end=hit.point; break;
                }
                if(CyberExtraction.Enemies.MonsterWorld.ShootWeb(origin,direction,Vector2.Distance(origin,end),out var webImpact)) end=webImpact;
                else if(breakable!=null) breakable.ServerBreak();
                else if(victim!=null)
                {
                    int damage=weapon.DamageAt(Vector2.Distance(origin,end));
                    var monster=victim.GetComponent<CyberExtraction.Enemies.Monster>();
                    victim.ServerTakeDamage(monster!=null ? monster.ProjectileDamage(damage,origin) : damage,inv);
                }
                CyberExtraction.UI.NetworkBootstrap.Instance.BroadcastShot(origin,end,pellet==0,weapon.NoiseRadius,weapon.Item);
            }
            Noise.Emit(origin,weapon.NoiseRadius);
        }
    }
}
