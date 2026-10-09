using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using CyberExtraction.Core;
using CyberExtraction.Players;
using CyberExtraction.Items;
using CyberExtraction.Bots;

namespace CyberExtraction.Combat
{
    public class LootPickup : NetworkBehaviour
    {
        public static readonly List<LootPickup> All = new List<LootPickup>();
        public int initialValue = GameConfig.LootValue;
        public ItemId initialItem = ItemId.Wood;
        public int initialQuantity = 1;
        public byte initialQuality;
        public int initialData;
        public NetworkVariable<byte> Quality = new NetworkVariable<byte>(0);
        public NetworkVariable<int> Data = new NetworkVariable<int>(0);
        public NetworkVariable<byte> Item = new NetworkVariable<byte>(0);
        public NetworkVariable<int> Quantity = new NetworkVariable<int>(1);
        public NetworkVariable<int> Value = new NetworkVariable<int>(GameConfig.LootValue);

        public override void OnNetworkSpawn()
        {
            All.Add(this);
            if (IsServer)
            {
                Item.Value = (byte)initialItem;
                Quantity.Value = initialQuantity;
                Quality.Value = initialQuality; Data.Value = initialData;
                Value.Value = initialQuantity * ItemCatalog.Score(initialItem);
            }
            GetComponent<SpriteRenderer>().color = ItemCatalog.Tint((ItemId)Item.Value);
        }

        public override void OnNetworkDespawn() { All.Remove(this); }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer || !IsSpawned) return;
            var inv = other.GetComponentInParent<PlayerInventory>();
            if (inv == null || !inv.IsAlive) return;
            if (inv.ServerAddStack(new ItemStack((ItemId)Item.Value, Quantity.Value, Quality.Value, Data.Value))) NetworkObject.Despawn();
        }

        void OnGUI()
        {
            if (!IsSpawned || IsServer || Camera.main == null) return;
            var player = NetworkManager.SpawnManager.GetLocalPlayerObject();
            if (player == null || !BotController.InVision(player.transform.position, player.transform.eulerAngles.z, transform.position) ||
                Physics2D.Linecast(player.transform.position, transform.position, GameConfig.WallMask).collider != null) return;
            Vector3 point = Camera.main.WorldToScreenPoint(transform.position);
            if (point.z <= 0 || point.x < 0 || point.x > Screen.width || point.y < 0 || point.y > Screen.height) return;
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            var style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.Clamp(Screen.height / 25, 20, 34), alignment = TextAnchor.MiddleCenter };
            GUI.Box(new Rect(point.x - 24, Screen.height - point.y - 22, 48, 44), ItemCatalog.Icon((ItemId)Item.Value), style);
            GUI.matrix = old;
        }
    }
}
