using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using CyberExtraction.Combat;
using CyberExtraction.Items;
using CyberExtraction.Players;
using CyberExtraction.Match;

namespace CyberExtraction.UI
{
    public class Quickbar : MonoBehaviour
    {
        public static Rect Bounds
        {
            get
            {
                float height = Mathf.Clamp(Screen.height * 0.10f, 60, 110);
                float width = Mathf.Min(Screen.width * 0.65f, height * 3.5f);
                return new Rect((Screen.width - width) / 2, Screen.height * 0.79f - height, width, height);
            }
        }

        public static bool ContainsScreenPoint(Vector2 point) => Bounds.Contains(new Vector2(point.x, Screen.height - point.y));

        static PlayerInventory LocalInventory()
        {
            var nm = NetworkManager.Singleton;
            var player = nm != null && nm.IsClient && nm.SpawnManager != null ? nm.SpawnManager.GetLocalPlayerObject() : null;
            return player != null ? player.GetComponent<PlayerInventory>() : null;
        }

        void Update()
        {
            var inv = LocalInventory();
            if (inv == null || Keyboard.current == null) return;
            var keyboard = Keyboard.current;
            if (keyboard.digit1Key.wasPressedThisFrame) inv.RequestBandage();
            if (keyboard.qKey.wasPressedThisFrame || keyboard.digit2Key.wasPressedThisFrame) inv.RequestNextWeapon();
            if (keyboard.digit3Key.wasPressedThisFrame) inv.GetComponent<PlayerShooter>().RequestReload();
        }

        void OnGUI()
        {
            var inv = LocalInventory();
            if (inv == null || MatchManager.Instance == null || MatchManager.Instance.CurrentState != MatchState.Running) return;
            var hp = inv.GetComponent<Health>();
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            Rect bounds = Bounds;
            var box = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp((int)(bounds.height * 0.29f), 16, 28) };
            double cooldown = System.Math.Max(0, inv.BandageReadyAt.Value - NetworkManager.Singleton.ServerTime.Time);
            bool oldEnabled = GUI.enabled;
            float slot = bounds.width / 3;
            GUI.enabled = inv.IsAlive && inv.Bandages.Value > 0 && hp.Current.Value < hp.Maximum.Value && cooldown <= 0;
            string label = cooldown > 0 ? $"B  {System.Math.Ceiling(cooldown)}s" : $"B  x{inv.Bandages.Value}";
            if (GUI.Button(new Rect(bounds.x, bounds.y, slot - 4, bounds.height), "1\n" + label, box)) inv.RequestBandage();
            var shooter = inv.GetComponent<PlayerShooter>();
            var weapon = inv.ActiveWeapon;
            GUI.enabled = inv.IsAlive && inv.WeaponCount > 1;
            string weaponLabel = weapon != null ? ItemCatalog.Icon(weapon.Item) : "-";
            if (GUI.Button(new Rect(bounds.x + slot, bounds.y, slot - 4, bounds.height), "2 / Q\n" + weaponLabel, box)) inv.RequestNextWeapon();
            GUI.enabled = inv.IsAlive && weapon != null && shooter.Magazine < weapon.Capacity && shooter.Reserve > 0 && !shooter.Reloading && !shooter.CyclingBolt;
            string reload = shooter.Reloading ? "Recarga" : shooter.CyclingBolt ? "Ferrolho" : "Recarregar";
            if (GUI.Button(new Rect(bounds.x + slot * 2, bounds.y, slot - 4, bounds.height), "3 / R\n" + reload, box)) shooter.RequestReload();
            GUI.enabled = oldEnabled;
            GUI.matrix = oldMatrix;
        }
    }
}
