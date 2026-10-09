using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Items;

namespace CyberExtraction.Players
{
    // Silhuetas provisorias feitas com os mesmos sprites geometricos do prototipo.
    public class WeaponVisual : MonoBehaviour
    {
        PlayerInventory inventory;
        GameObject pistol, rifle, shotgun;
        readonly Color wood = new Color(.42f, .25f, .12f);
        readonly Color steel = new Color(.40f, .43f, .44f);
        void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
            pistol = Root("PistolVisual");
            Part(pistol, "Barrel", .57f, 0, .38f, .12f, steel);
            Part(pistol, "Grip", .40f, -.08f, .13f, .18f, wood);
            rifle = Root("MosinVisual");
            Part(rifle, "WoodStock", .42f, 0, .55f, .14f, wood);
            Part(rifle, "LongBarrel", .91f, 0, .58f, .065f, steel);
            Part(rifle, "Bolt", .57f, -.09f, .06f, .12f, steel);
            shotgun = Root("DoubleBarrelVisual");
            Part(shotgun, "WoodStock", .37f, 0, .38f, .18f, wood);
            Part(shotgun, "LeftBarrel", .78f, .055f, .48f, .07f, steel);
            Part(shotgun, "RightBarrel", .78f, -.055f, .48f, .07f, steel);
            pistol.SetActive(false); rifle.SetActive(false); shotgun.SetActive(false);
        }
        GameObject Root(string name)
        {
            var root = new GameObject(name); root.transform.SetParent(transform, false); return root;
        }
        void Part(GameObject root, string name, float x, float y, float width, float height, Color color)
        {
            var part = new GameObject(name); part.transform.SetParent(root.transform, false);
            part.transform.localPosition = new Vector3(x, y, 0);
            part.transform.localScale = new Vector3(width, height, 1);
            SpriteFactory.Setup(part.AddComponent<SpriteRenderer>(), SpriteFactory.Square, color, 6);
        }
        void Update()
        {
            var weapon = inventory != null && inventory.IsAlive ? inventory.ActiveWeapon : null;
            pistol.SetActive(weapon != null && weapon.Item == ItemId.Pistol);
            rifle.SetActive(weapon != null && weapon.Item == ItemId.Mosin);
            shotgun.SetActive(weapon != null && weapon.Item == ItemId.DoubleBarrelShotgun);
        }
    }
}
