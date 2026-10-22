using System.Collections;
using System.IO;
using UnityEngine;
using Unity.Netcode;
using CyberExtraction.Camp;
using CyberExtraction.Items;
using CyberExtraction.UI;

namespace CyberExtraction.Diagnostics
{
    // Opt-in disposable preview scene, never added to normal game scenes.
    public sealed class ItemIconPreview : MonoBehaviour
    {
        IEnumerator Start()
        {
            ClientEnvironment.Select(CampEnvironment.Normal);
            new GameObject("PreviewNetworkManager").AddComponent<NetworkManager>();
            var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true;
            camera.backgroundColor = new Color(.09f, .11f, .09f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            CampSession.Progress = new CampProgress { Pack = new InventoryGrid(ItemCatalog.BackpackSlots), Chest = new InventoryGrid(ItemCatalog.SafeChestSlots) };
            for (int i = 0; i < 18; i++)
                (i < 9 ? CampSession.Progress.Pack : CampSession.Progress.Chest).Slots[i % 9] =
                    new ItemStack((ItemId)i, ItemCatalog.Weapon((ItemId)i) ? 1 : i + 1, 1, ItemCatalog.Weapon((ItemId)i) ? 2 : 0);
            new GameObject("PreviewCamp").AddComponent<CampController>();
            var bag = new GameObject("PreviewInventory").AddComponent<InventoryWindow>();
            camera.transform.position = new Vector3(2000, 0, -10); camera.orthographicSize = 5;
            for (int i = 0; i < 18; i++)
            {
                var root = new GameObject("PreviewLoot" + (ItemId)i);
                root.transform.position = new Vector3(1994 + (i % 6) * 2.3f, 3 - i / 6 * 2.3f, 0);
                root.transform.localScale = Vector3.one * 1.5f;
                root.AddComponent<SpriteRenderer>().sortingOrder = 8;
                ItemIconLibrary.ApplyWorld(root, (ItemId)i);
            }
            string folder = Path.GetFullPath("Logs/item-icons"); Directory.CreateDirectory(folder);
            string size = Screen.width + "x" + Screen.height;
            yield return new WaitForSecondsRealtime(1);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "world-" + size + ".png"));
            yield return new WaitForSecondsRealtime(.5f);
            bag.OpenSafe();
            yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "inventory-" + size + ".png"));
            yield return new WaitForSecondsRealtime(.5f);
            Debug.Log("ITEM ICON PREVIEW PASS: world and actual InventoryWindow rendered at " + size);
            Application.Quit(0);
        }
    }
}
