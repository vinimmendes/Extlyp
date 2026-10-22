using System;
using UnityEngine;
using CyberExtraction.Items;

namespace CyberExtraction.Players
{
    // Cosmetic samples only. The server timestamps remain the source of attack timing.
    static class PlayerMeleeActionArt
    {
        [Serializable] public struct Grip { public float x, y, angle, scaleX, scaleY; }
        [Serializable] sealed class Clip { public Grip[] frames; }
        sealed class Action
        {
            public readonly string Atlas, Metadata, WeaponAtlas;
            public readonly float WeaponWorldSize;
            public readonly int Count, Impact;
            public bool Loaded;
            public Sprite[] Frames, WeaponFrames;
            public Clip Data;
            public Action(string atlas,string metadata,int count,int impact,string weaponAtlas=null,float weaponWorldSize=3.4f)
            { Atlas=atlas; Metadata=metadata; Count=count; Impact=impact; WeaponAtlas=weaponAtlas; WeaponWorldSize=weaponWorldSize; }
            public void Reset() { Loaded=false; Frames=null; WeaponFrames=null; Data=null; }
        }
        // Impact is zero-based: the first recovery sample coincides with server damage.
        static readonly Action Sword=new Action("SwordAttack","SwordGrip",20,8);
        static readonly Action Axe=new Action("AxeAttack","AxeGrip",26,10,"AxeWeapon",5.1f);
        static readonly Action Spear=new Action("SpearAttack","SpearGrip",40,15,"SpearWeapon",5.1f);
        public static bool IsSword(ItemId item) => item==ItemId.RustySword || item==ItemId.ShortSword;
        public static bool Supports(ItemId item) => IsSword(item) || item==ItemId.BattleAxe || item==ItemId.Spear;

        public static bool TrySample(PlayerShooter shooter,out Sprite sprite,out Grip grip)
            => TrySample(shooter,out sprite,out grip,out _);

        public static bool TrySample(PlayerShooter shooter,out Sprite sprite,out Grip grip,out Sprite weaponSprite)
        {
            sprite=null; grip=default; weaponSprite=null;
            if(shooter==null || !shooter.IsSpawned || shooter.Weapon==null || !Supports(shooter.Weapon.Item)) return false;
            var action=IsSword(shooter.Weapon.Item) ? Sword : shooter.Weapon.Item==ItemId.Spear ? Spear : Axe;
            if(!action.Loaded)
            {
                action.Loaded=true;
                var json=Resources.Load<TextAsset>("PlayerCharacter/Actions/"+action.Metadata);
                if(json!=null) action.Data=JsonUtility.FromJson<Clip>(json.text);
                if(action.Data!=null && action.Data.frames!=null && action.Data.frames.Length==action.Count)
                    action.Frames=PlayerActionArt.Get(action.Atlas,action.Count);
                if(action.Frames!=null && action.WeaponAtlas!=null)
                    action.WeaponFrames=PlayerActionArt.Get(action.WeaponAtlas,action.Count,action.WeaponWorldSize,optional:true);
            }
            if(action.Frames==null) return false;
            int frame=0;
            double now=shooter.NetworkManager.ServerTime.Time;
            if(shooter.MeleeAttacking && shooter.MeleeItem.Value==(byte)shooter.Weapon.Item)
            {
                if(now<shooter.MeleeImpactAt.Value)
                {
                    double duration=Math.Max(.001,shooter.MeleeImpactAt.Value-shooter.MeleeStartedAt.Value);
                    frame=Mathf.Clamp(Mathf.FloorToInt((float)((now-shooter.MeleeStartedAt.Value)/duration)*action.Impact),0,action.Impact-1);
                }
                else
                {
                    double duration=Math.Max(.001,shooter.MeleeEndsAt.Value-shooter.MeleeImpactAt.Value);
                    int recovery=action.Count-action.Impact;
                    frame=action.Impact+Mathf.Clamp(Mathf.FloorToInt((float)((now-shooter.MeleeImpactAt.Value)/duration)*recovery),0,recovery-1);
                }
            }
            sprite=action.Frames[frame]; grip=action.Data.frames[frame];
            if(action.WeaponFrames!=null) weaponSprite=action.WeaponFrames[frame];
            // Existing sword metadata predates apparent scale; its visual stays unchanged.
            if(grip.scaleX<=0) grip.scaleX=1;
            if(grip.scaleY<=0) grip.scaleY=1;
            return sprite!=null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Sword.Reset(); Axe.Reset(); Spear.Reset(); }
    }
}
