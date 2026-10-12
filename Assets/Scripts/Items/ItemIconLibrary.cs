using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Items
{
    [CreateAssetMenu(menuName = "Cyber Extraction/Item icons")]
    public sealed class ItemIconLibrary : ScriptableObject
    {
        // Indexed by ItemId. Visual configuration only; never stores player state.
        public Sprite[] icons = new Sprite[18];
        static ItemIconLibrary loaded;
        static bool attempted;

        public static Sprite Get(ItemId item)
        {
            if (!ItemCatalog.Valid(item)) return null;
            if(item==ItemId.Lockpick) return Resources.Load<Sprite>("Items/Lockpick");
            if(ItemCatalog.PassageTool(item)) return PassageToolArt.Get(item);
            if (!attempted)
            {
                loaded = Resources.Load<ItemIconLibrary>("ItemIcons");
                attempted = true;
                if (loaded == null) Debug.LogWarning("Catalogo visual ItemIcons ausente. Execute Itens > Preparar icones.");
            }
            return loaded != null && (int)item < loaded.icons.Length ? loaded.icons[(int)item] : null;
        }

        // A child visual keeps the parent's collection collider and network scale intact.
        public static bool ApplyWorld(GameObject root, ItemId item)
        {
            var sprite = Get(item);
            if (sprite == null) return false;
            var source = root.GetComponent<SpriteRenderer>();
            var art = new GameObject("ItemIcon");
            art.transform.SetParent(root.transform, false);
            float longest = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            art.transform.localScale = Vector3.one * Mathf.Clamp(GameplayDefinition.Current.WorldLootVisualScale,1f,2f) / Mathf.Max(.001f, longest);
            SpriteFactory.Setup(art.AddComponent<SpriteRenderer>(), sprite, Color.white, source.sortingOrder);
            source.enabled = false;
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { loaded = null; attempted = false; }
    }
}
