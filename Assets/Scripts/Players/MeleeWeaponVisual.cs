using UnityEngine;
using CyberExtraction.Combat;
using CyberExtraction.Core;
using CyberExtraction.Items;
using CyberExtraction.Match;

namespace CyberExtraction.Players
{
    // Presentation follows replicated server timestamps. Never applies damage locally.
    [DefaultExecutionOrder(100)]
    public sealed class MeleeWeaponVisual : MonoBehaviour
    {
        PlayerShooter shooter;
        PlayerInventory inventory;
        SpriteRenderer body,held;
        PlayerCharacterVisual character;
        LineRenderer arc;
        void Awake()
        {
            shooter=GetComponent<PlayerShooter>(); inventory=GetComponent<PlayerInventory>(); body=GetComponent<SpriteRenderer>();
            character=GetComponent<PlayerCharacterVisual>();
            var art=new GameObject("HeldMeleeWeapon"); art.transform.SetParent(transform,false); art.layer=gameObject.layer;
            held=art.AddComponent<SpriteRenderer>();
            var preview=new GameObject("MeleePreparation"); preview.transform.SetParent(transform,false);
            arc=preview.AddComponent<LineRenderer>(); arc.useWorldSpace=true; arc.positionCount=25;
            arc.startWidth=arc.endWidth=.035f; arc.sharedMaterial=SpriteFactory.Unlit;
            held.enabled=arc.enabled=false;
            GetComponent<VisibilityTarget>()?.Refresh();
        }
        void LateUpdate()
        {
            var weapon=shooter!=null && shooter.IsSpawned ? shooter.Weapon : null;
            // Crossbow geometry is now baked into its Blender body/action frames.
            bool show=weapon!=null && weapon.IsMelee && inventory!=null && inventory.IsAlive && body!=null && body.enabled &&
                MatchManager.Instance!=null && MatchManager.Instance.CurrentState==MatchState.Running;
            held.enabled=show; arc.enabled=false;
            if(!show) return;
            held.sprite=MeleeWeaponArt.Get(weapon.Item);
            if(held.sprite==null) { held.enabled=false; return; }
            held.sharedMaterial=body.sharedMaterial;
            held.sortingLayerID=body.sortingLayerID; held.sortingOrder=body.sortingOrder+2; held.color=body.color;
            float angle=-22,extension=0;
            float artAngle=weapon.Item==ItemId.RustySword ? -45 : 0;
            double now=shooter.NetworkManager.ServerTime.Time;
            bool attack=weapon.IsMelee && shooter.MeleeAttacking && shooter.MeleeItem.Value==(byte)weapon.Item;
            if(attack)
            {
                float prep=Mathf.Clamp01((float)((now-shooter.MeleeStartedAt.Value)/weapon.WindupSeconds));
                float recovery=Mathf.Clamp01((float)((now-shooter.MeleeImpactAt.Value)/weapon.RecoverySeconds));
                angle=now<shooter.MeleeImpactAt.Value ? Mathf.Lerp(-weapon.StillHalfAngle,0,prep*prep*prep) :
                    Mathf.Lerp(0,weapon.StillHalfAngle,Mathf.Sin(recovery*Mathf.PI));
                if(weapon.Item==ItemId.Spear) { angle=0; extension=now<shooter.MeleeImpactAt.Value ? -.15f*(1-prep) : .25f*(1-recovery); }
                float heading=shooter.MeleeAngle.Value;
                held.transform.rotation=Quaternion.Euler(0,0,heading+angle+artAngle);
                if(shooter.IsOwner)
                {
                    arc.enabled=true; arc.sortingLayerID=body.sortingLayerID; arc.sortingOrder=body.sortingOrder+3;
                    var color=now<shooter.MeleeImpactAt.Value ? new Color(.85f,.72f,.44f,.3f) : new Color(1,.88f,.64f,.65f*(1-recovery));
                    arc.startColor=arc.endColor=color;
                    Vector2 origin=transform.position;
                    for(int i=0;i<arc.positionCount;i++)
                    {
                        float radians=(heading+Mathf.Lerp(-weapon.StillHalfAngle,weapon.StillHalfAngle,i/(float)(arc.positionCount-1)))*Mathf.Deg2Rad;
                        var direction=new Vector2(Mathf.Cos(radians),Mathf.Sin(radians));
                        var wall=Physics2D.Raycast(origin,direction,weapon.Range,GameConfig.WallMask);
                        arc.SetPosition(i,origin+direction*(wall.collider!=null ? wall.distance : weapon.Range));
                    }
                }
            }
            else held.transform.localRotation=Quaternion.Euler(0,0,angle+artAngle);
            // The diagonal rusty sword needs a smaller sprite to reach 2.1 units at impact.
            // The animated grip below keeps its handle attached to the unchanged hand pose.
            float scale=weapon.Item==ItemId.Spear ? 2.25f : weapon.Item==ItemId.RustySword ? 1.08f : 1.55f;
            float longest=Mathf.Max(held.sprite.bounds.size.x,held.sprite.bounds.size.y);
            held.transform.localScale=Vector3.one*(scale/Mathf.Max(.001f,longest));
            // Sprites pivot in their centre; grip lies left of centre along the shaft.
            Vector3 grip=new Vector3(.38f,-.26f,0);
            var gripRotation=held.transform.localRotation*Quaternion.Euler(0,0,-artAngle);
            held.transform.localPosition=grip+gripRotation*new Vector3(scale*.25f+extension,0,0);
            if(character!=null && character.MeleePresentationActive &&
                PlayerMeleeActionArt.TrySample(shooter,out _,out var actionGrip,out var weaponFrame))
            {
                if(weaponFrame!=null)
                {
                    // Framing already contains the grip's 3D position and orientation.
                    held.sprite=weaponFrame;
                    held.transform.localPosition=Vector3.zero;
                    held.transform.localRotation=Quaternion.identity;
                    held.transform.localScale=Vector3.one;
                    return;
                }
                // Match each existing weapon handle, including diagonal sword art.
                var uv=weapon.Item==ItemId.RustySword ? new Vector2(.22f,.22f) :
                    weapon.Item==ItemId.BattleAxe ? new Vector2(50f/128f,.5f) :
                    weapon.Item==ItemId.Spear ? new Vector2(40f/128f,.5f) : new Vector2(30f/128f,.5f);
                var artGrip=(Vector2.Scale(uv,held.sprite.rect.size)-held.sprite.pivot)/held.sprite.pixelsPerUnit;
                var rotation=Quaternion.Euler(0,0,actionGrip.angle+artAngle);
                held.transform.localRotation=rotation;
                held.transform.localScale=Vector3.Scale(held.transform.localScale,new Vector3(actionGrip.scaleX,actionGrip.scaleY,1));
                held.transform.localPosition=new Vector3(actionGrip.x,actionGrip.y,0)-
                    rotation*Vector3.Scale((Vector3)artGrip,held.transform.localScale);
            }
        }
        void OnDisable() { if(held!=null) held.enabled=false; if(arc!=null) arc.enabled=false; }
    }
}
