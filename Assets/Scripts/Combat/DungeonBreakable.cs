using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Match;
using CyberExtraction.Items;

namespace CyberExtraction.Combat
{
    // Server-owned transition, deliberately separate from Health/experience.
    public sealed class DungeonBreakable : NetworkBehaviour
    {
        static readonly List<DungeonBreakable> all=new List<DungeonBreakable>();
        public NetworkVariable<int> CandidateId=new NetworkVariable<int>(-1);
        public NetworkVariable<double> BrokenAt=new NetworkVariable<double>(0);
        public NetworkVariable<bool> Broken=new NetworkVariable<bool>(false);
        BoxCollider2D hitbox;
        SpriteRenderer visual;
        Sprite intact,broken;
        bool ownsIntact,ownsBroken;
        double lastBurst=-1;
        ItemStack reward;
        bool rewardConfigured,rewardClaimed;
        readonly List<SpriteRenderer> fragments=new List<SpriteRenderer>();
        void Awake()
        {
            hitbox=gameObject.AddComponent<BoxCollider2D>();
            hitbox.isTrigger=true; hitbox.size=Vector2.one*.9f;
            ServerVisibility.Install(GetComponent<NetworkObject>());
        }
        public override void OnNetworkSpawn()
        {
            all.Add(this); CandidateId.OnValueChanged+=CandidateChanged; Broken.OnValueChanged+=StateChanged; BrokenAt.OnValueChanged+=TimeChanged;
            BuildVisual(); Apply(false);
            lastBurst=BrokenAt.Value;
        }
        void CandidateChanged(int previous,int current) { ReleaseSprites(); BuildVisual(); Apply(false); }
        void BuildVisual()
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var data=DungeonBreakableLayout.Load();
            if(data==null || CandidateId.Value<0 || CandidateId.Value>=data.candidates.Length) return;
            var point=data.candidates[CandidateId.Value];
            if(visual==null)
            {
                var go=new GameObject("BreakableArt"); go.transform.SetParent(transform,false);
                visual=go.AddComponent<SpriteRenderer>();
            }
            foreach(var entry in data.art)
            {
                if(entry.family!=point.family || entry.variant!=point.variant) continue;
                var published=DungeonMapPublication.ArtSprite("Breakable_"+entry.family+"_"+entry.state+"_"+entry.variant);
                if(published!=null)
                {
                    if(entry.state=="Intact") { intact=published; ownsIntact=false; }
                    else if(entry.state=="Broken") { broken=published; ownsBroken=false; }
                    continue;
                }
                var texture=Resources.Load<Texture2D>(entry.resource); if(texture==null || entry.rect==null) continue;
                var r=entry.rect.Value;
                var sprite=Sprite.Create(texture,new Rect(r.x*texture.width,r.y*texture.height,r.width*texture.width,r.height*texture.height),Vector2.one*.5f,128,0,SpriteMeshType.FullRect);
                if(entry.state=="Intact") { intact=sprite; ownsIntact=true; } else if(entry.state=="Broken") { broken=sprite; ownsBroken=true; } else Destroy(sprite);
            }
        }
        void StateChanged(bool previous,bool current) => Apply(!previous && current);
        void TimeChanged(double previous,double current) { if(Broken.Value) Apply(true); }
        void Apply(bool animate)
        {
            hitbox.enabled=IsServer && !Broken.Value;
            if(visual==null) return;
            var sprite=Broken.Value ? broken : intact;
            if(sprite==null) { visual.enabled=false; return; }
            visual.enabled=true;
            SpriteFactory.Setup(visual,sprite,Broken.Value ? new Color(.8f,.78f,.72f,.85f) : Color.white,Broken.Value ? DungeonMapPublication.Current!=null ? -6 : -7 : -5);
            if(DungeonMapPublication.ArtCatalog?.SpriteMaterial!=null) visual.sharedMaterial=DungeonMapPublication.ArtCatalog.SpriteMaterial;
            float size=Broken.Value ? 1.05f : .9f;
            visual.transform.localScale=new Vector3(size/sprite.bounds.size.x,size/sprite.bounds.size.y,1);
            // Late join / visibility re-spawn shows final debris without replay.
            double age=NetworkManager.ServerTime.Time-BrokenAt.Value;
            if(animate && BrokenAt.Value>lastBurst && age>=0 && age<.5)
            { lastBurst=BrokenAt.Value; StartCoroutine(FragmentBurst()); }
        }
        IEnumerator FragmentBurst()
        {
            if(broken==null) yield break;
            ClearFragments();
            for(int i=0;i<3;i++)
            {
                var piece=new GameObject("BreakFragment"); piece.transform.SetParent(transform,false);
                var renderer=piece.AddComponent<SpriteRenderer>();
                SpriteFactory.Setup(renderer,broken,new Color(.7f,.67f,.58f,.65f),-5);
                if(DungeonMapPublication.ArtCatalog?.SpriteMaterial!=null) renderer.sharedMaterial=DungeonMapPublication.ArtCatalog.SpriteMaterial;
                piece.transform.localScale=new Vector3(.16f/broken.bounds.size.x,.16f/broken.bounds.size.y,1);
                fragments.Add(renderer);
            }
            float time=0;
            while(time<.3f)
            {
                time+=Time.deltaTime; float p=Mathf.Clamp01(time/.3f);
                for(int i=0;i<fragments.Count;i++)
                {
                    float angle=(i*120+CandidateId.Value*37)*Mathf.Deg2Rad;
                    fragments[i].transform.localPosition=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*(.25f*p);
                    fragments[i].transform.localRotation=Quaternion.Euler(0,0,i*90+p*75);
                    fragments[i].color=new Color(.7f,.67f,.58f,.65f*(1-p));
                }
                yield return null;
            }
            ClearFragments();
        }
        public void ConfigureReward(ItemStack stack)
        {
            if(IsSpawned || rewardConfigured || Unity.Netcode.NetworkManager.Singleton==null || !Unity.Netcode.NetworkManager.Singleton.IsServer)
                throw new System.InvalidOperationException("Recompensa de quebravel so pode ser configurada uma vez, antes do spawn no servidor.");
            if(!stack.Empty && (!DungeonBreakableLoot.Common(stack.Item) || stack.Quantity>ItemCatalog.Limit(stack.Item) ||
                stack.Data!=0 || stack.Quality!=0 || stack.Instance.HasIdentity))
                throw new System.InvalidOperationException("Recompensa de quebravel invalida.");
            reward=stack; rewardConfigured=true;
        }
        public bool ServerBreak()
        {
            if(!IsServer || !IsSpawned || Broken.Value || MatchManager.Instance==null || MatchManager.Instance.CurrentState!=MatchState.Running) return false;
            BrokenAt.Value=NetworkManager.ServerTime.Time; Broken.Value=true; hitbox.enabled=false;
            if(!rewardClaimed)
            {
                // Claim before spawning: simultaneous pellets/hits never retry.
                rewardClaimed=true; var drop=reward; reward=default;
                if(!drop.Empty) MatchManager.Instance.ServerDropStack(transform.position,drop);
            }
            return true;
        }
        public static void ServerMelee(Vector2 origin,float angle,float range,float halfAngle,UnityEngine.SceneManagement.Scene scene)
        {
            foreach(var target in all)
            {
                if(target==null || !target.IsServer || !target.IsSpawned || target.Broken.Value || target.gameObject.scene!=scene) continue;
                Vector2 point=target.hitbox.ClosestPoint(origin),delta=point-origin;
                if(delta.sqrMagnitude>range*range || delta.sqrMagnitude>.0001f && Mathf.Abs(Mathf.DeltaAngle(angle,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg))>halfAngle) continue;
                if(Physics2D.Linecast(origin,point,GameConfig.WallMask).collider!=null) continue;
                target.ServerBreak();
            }
        }
        void ClearFragments() { foreach(var sr in fragments) if(sr!=null) Destroy(sr.gameObject); fragments.Clear(); }
        void ReleaseSprites() { if(ownsIntact && intact!=null) Destroy(intact); if(ownsBroken && broken!=null) Destroy(broken); intact=broken=null; ownsIntact=ownsBroken=false; }
        public override void OnNetworkDespawn()
        {
            CandidateId.OnValueChanged-=CandidateChanged; Broken.OnValueChanged-=StateChanged; BrokenAt.OnValueChanged-=TimeChanged;
            all.Remove(this); hitbox.enabled=false; StopAllCoroutines(); ClearFragments(); ReleaseSprites();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() => all.Clear();
    }
}
