using UnityEngine;
using Unity.Netcode;
using CyberExtraction.Combat;
using CyberExtraction.Players;
using CyberExtraction.Match;
using CyberExtraction.Items;
namespace CyberExtraction.UI
{
    public class Quickbar : MonoBehaviour
    {
        static bool Visible
        {
            get { var inv=InventoryWindow.Local(); return !Camp.CampController.Active && !UiLayout.BlocksGameplay && inv!=null && inv.IsAlive && inv.FishingPhase.Value==0 && MatchManager.Instance!=null && MatchManager.Instance.CurrentState==MatchState.Running; }
        }
        public static bool ContainsScreenPoint(Vector2 point)
        {
            var p=UiLayout.Point(point);
            return Visible && (HudLayout.HitBounds(HudControl.Heal).Contains(p) || HudLayout.HitBounds(HudControl.Swap).Contains(p) ||
                HudLayout.HitBounds(HudControl.Reload).Contains(p) || HudLayout.HitBounds(HudControl.Backpack).Contains(p) || HudLayout.Weapon.Contains(p)) ||
                InventoryWindow.ContainsControlPoint(point) || Minimap.ContainsScreenPoint(point);
        }
        void Update()
        {
            var inv=InventoryWindow.Local(); if(inv==null || UiLayout.BlocksGameplay) return;
            if(GameInput.BandagePressed) inv.RequestBandage();
        }
        bool Action(HudControl control,HudSymbol icon,string text)
        {
            var rect=HudLayout.Bounds(control); bool enabled=GUI.enabled;
            bool clicked=GUI.Button(HudLayout.HitBounds(control),GUIContent.none,HudTheme.InvisibleButton);
            HudArt.Circle(rect,held:enabled && HudLayout.HitBounds(control).Contains(Event.current.mousePosition) && InputHeld());
            var tint=HudTheme.Text; if(!enabled) tint.a=.4f;
            float size=rect.width*.55f;
            HudArt.Icon(new Rect(rect.center.x-size/2,rect.center.y-size/2,size,size),icon,tint);
            var previous=GUI.color; GUI.color=enabled ? Color.white : new Color(1,1,1,.45f);
            float labelScale=rect.width/HudLayout.DefaultSize(control).x;
            GUI.Label(new Rect(rect.center.x-38*labelScale,rect.yMax+3,76*labelScale,40*labelScale),text,HudTheme.Label(HudLayout.TextSize(control,13),TextAnchor.UpperCenter)); GUI.color=previous;
            return clicked;
        }
        static bool InputHeld()=>Event.current.type==EventType.MouseDown || GUIUtility.hotControl!=0;
        void OnGUI()
        {
            if(UguiGameHud.Active) return;
            if(!Visible) return;
            using var frame=UiLayout.Begin();
            var inv=InventoryWindow.Local(); var hp=inv.GetComponent<Health>(); var shooter=inv.GetComponent<PlayerShooter>();
            bool melee=shooter.Weapon!=null && shooter.Weapon.IsMelee;
            var weapon=HudLayout.Weapon; HudArt.Panel(weapon);
            string name=shooter.Weapon==null ? "SEM ARMA" : ItemCatalog.Name(shooter.Weapon.Item);
            if(shooter.Weapon!=null)
                ItemIconGui.Draw(HudLayout.Detail(HudControl.Weapon,7,17,34,34),shooter.Weapon.Item);
            if(melee) name=ItemCatalog.Name(shooter.Weapon.Item).ToUpperInvariant();
            GUI.Label(HudLayout.Detail(HudControl.Weapon,47,4,93,16),name,HudTheme.Label(HudLayout.TextSize(HudControl.Weapon,12)));
            GUI.Label(HudLayout.Detail(HudControl.Weapon,47,19,93,26),melee ? shooter.MeleeDamage+" dano" : shooter.Magazine+" / "+(shooter.Weapon!=null ? shooter.Weapon.Capacity : 0),HudTheme.Label(HudLayout.TextSize(HudControl.Weapon,22)));
            GUI.Label(HudLayout.Detail(HudControl.Weapon,47,46,93,16),melee ? shooter.Weapon.StaminaCost+" stamina" : "Reserva "+shooter.Reserve,HudTheme.Label(HudLayout.TextSize(HudControl.Weapon,12)));
            bool before=GUI.enabled;
            double cooldown=System.Math.Max(0,inv.BandageReadyAt.Value-NetworkManager.Singleton.ServerTime.Time);
            GUI.enabled=inv.IsAlive && inv.Bandages.Value>0 && hp.Current.Value<hp.Maximum.Value && cooldown<=0;
            if(Action(HudControl.Heal,HudSymbol.Heal,cooldown>0 ? "Curar\n"+System.Math.Ceiling(cooldown)+" s" : "Curar\n×"+inv.Bandages.Value)) inv.RequestBandage();
            GUI.enabled=inv.WeaponCount>1 && !shooter.MeleeAttacking;
            if(Action(HudControl.Swap,HudSymbol.Swap,"Trocar")) inv.RequestNextWeapon();
            GUI.enabled=shooter.Weapon!=null && !shooter.Reloading && !shooter.CyclingBolt && shooter.Reserve>0 && shooter.Magazine<shooter.Weapon.Capacity;
            if(Action(HudControl.Reload,HudSymbol.Ammo,shooter.Reloading ? "Recarga..." : shooter.CyclingBolt ? "Ferrolho" : "Recarregar")) shooter.RequestReload();
            GUI.enabled=before;
            if(Action(HudControl.Backpack,HudSymbol.Backpack,"Mochila")) InventoryWindow.Instance.ToggleBag();
        }
    }
}
