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
        public static Rect Bounds=>HudLayout.Actions;
        static bool Visible
        {
            get { var inv=InventoryWindow.Local(); return !Camp.CampController.Active && !UiLayout.BlocksGameplay && inv!=null && inv.IsAlive && inv.FishingPhase.Value==0 && MatchManager.Instance!=null && MatchManager.Instance.CurrentState==MatchState.Running; }
        }
        public static bool ContainsScreenPoint(Vector2 point)=>Visible && (Bounds.Contains(UiLayout.Point(point)) || HudLayout.Weapon.Contains(UiLayout.Point(point))) || InventoryWindow.ContainsControlPoint(point) || Minimap.ContainsScreenPoint(point);
        void Update()
        {
            var inv=InventoryWindow.Local(); if(inv==null || UiLayout.BlocksGameplay) return;
            if(GameInput.BandagePressed) inv.RequestBandage();
        }
        bool Action(Rect rect,ItemId? icon,string text)
        {
            bool clicked=GUI.Button(rect,GUIContent.none,HudTheme.ActionStyle);
            if(icon.HasValue) ItemIconGui.Draw(new Rect(rect.center.x-17,rect.y+8,34,28),icon.Value);
            GUI.Label(new Rect(rect.x+3,rect.y+(icon.HasValue ? 35 : 8),rect.width-6,rect.height-(icon.HasValue ? 38 : 12)),text,HudTheme.Label(14,TextAnchor.MiddleCenter));
            return clicked;
        }
        void OnGUI()
        {
            if(!Visible) return;
            using var frame=UiLayout.Begin();
            var inv=InventoryWindow.Local(); var hp=inv.GetComponent<Health>(); var shooter=inv.GetComponent<PlayerShooter>();
            var weapon=HudLayout.Weapon; GUI.Box(weapon,"");
            string name=shooter.Weapon==null ? "SEM ARMA" : shooter.Weapon.Item==ItemId.Mosin ? "RIFLE" : shooter.Weapon.Item==ItemId.DoubleBarrelShotgun ? "ESPINGARDA" : "PISTOLA";
            GUI.Label(new Rect(weapon.x+10,weapon.y+6,weapon.width-20,22),name,HudTheme.Label(15));
            GUI.Label(new Rect(weapon.x+10,weapon.y+25,weapon.width-20,30),shooter.Magazine+" / "+(shooter.Weapon!=null ? shooter.Weapon.Capacity : 0),HudTheme.Label(26));
            GUI.Label(new Rect(weapon.x+90,weapon.y+36,weapon.width-100,26),"Reserva "+shooter.Reserve,HudTheme.Label(13));
            var bounds=Bounds; float size=HudLayout.ActionSize; bool before=GUI.enabled;
            double cooldown=System.Math.Max(0,inv.BandageReadyAt.Value-NetworkManager.Singleton.ServerTime.Time);
            GUI.enabled=inv.IsAlive && inv.Bandages.Value>0 && hp.Current.Value<hp.Maximum.Value && cooldown<=0;
            if(Action(new Rect(bounds.x,bounds.y,size,size),ItemId.Bandage,(cooldown>0 ? System.Math.Ceiling(cooldown)+"s" : "Curar ×"+inv.Bandages.Value)+(HudLayout.Touch ? "" : " [1]"))) inv.RequestBandage();
            GUI.enabled=inv.WeaponCount>1;
            if(Action(new Rect(bounds.x+size+8,bounds.y,size,size),shooter.Weapon!=null ? (ItemId?)shooter.Weapon.Item : null,HudLayout.Touch ? "Trocar" : "Trocar [Q]")) inv.RequestNextWeapon();
            GUI.enabled=shooter.Weapon!=null && !shooter.Reloading && !shooter.CyclingBolt && shooter.Reserve>0 && shooter.Magazine<shooter.Weapon.Capacity;
            if(Action(new Rect(bounds.x+(size+8)*2,bounds.y,size,size),shooter.Weapon!=null ? (ItemId?)shooter.Weapon.Ammo : null,shooter.Reloading ? "Recarga..." : shooter.CyclingBolt ? "Ferrolho" : HudLayout.Touch ? "Recarregar" : "Recarga [R]")) shooter.RequestReload();
            GUI.enabled=before;
            if(Action(new Rect(bounds.x+(size+8)*3,bounds.y,size,size),null,HudLayout.Touch ? "Mochila" : "Mochila [I]")) InventoryWindow.Instance.ToggleBag();
        }
    }
}
