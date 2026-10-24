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
        static readonly bool[] reportedWorldFallback=new bool[ItemCatalog.Count];

        public static Sprite Get(ItemId item)
        {
            if(!ItemCatalog.Available(item)) return null;
            return ItemVisualCatalog.Interface(item) ?? GetLegacy(item);
        }

        public static Sprite GetWorld(ItemId item)
        {
            if(!ItemCatalog.Available(item)) return null;
            if(CyberExtraction.Match.MapBuilder.Current==CyberExtraction.Match.ExpeditionMap.Dungeon &&
                (item==ItemId.DungeonCrank || item==ItemId.DungeonKey))
            {
                var authored=CyberExtraction.Match.DungeonMapPublication.ArtSprite(item==ItemId.DungeonCrank ? "CAT-E13_v01" : "CAT-E14_v01");
                if(authored!=null) return authored;
            }
            var explicitWorld=ItemVisualCatalog.World(item) ?? DungeonToolArt.GetWorld(item);
            if(explicitWorld!=null) return explicitWorld;
            // Training gear had no world art: its legacy representation is a solid square.
            // Missing legacy references must not leave the pickup prefab's square visible either.
            bool training=item>=ItemId.TrainingHead && item<=ItemId.TrainingRing;
            var legacy=training ? null : GetLegacy(item);
            if(legacy!=null) return legacy;
            var replacement=ItemVisualCatalog.Interface(item);
            if(!training && !reportedWorldFallback[(int)item])
            {
                reportedWorldFallback[(int)item]=true;
                if(replacement!=null) Debug.LogWarning("Arte antiga do item no mapa ausente: "+item+". Usando o sprite do catálogo visual.");
                else Debug.LogError("Item sem sprite de mundo ou interface: "+item+". Verifique os catálogos visuais.");
            }
            return replacement;
        }

        static Sprite GetLegacy(ItemId item)
        {
            if (!ItemCatalog.Valid(item)) return null;
            if(TreasureCatalog.IsDefined(item))
            { var icon=TreasureCatalog.Get(item).Icon; return icon!=null ? icon : TreasureArt.Get(item); }
            if(ItemCatalog.Melee(item)) return MeleeWeaponArt.Get(item);
            if(EquipmentCatalog.IsDefined(item))
            { var icon=EquipmentCatalog.Get(item).Icon; return icon!=null ? icon : EquipmentArt.Get(item); }
            if(ItemCatalog.Equipment(item)) return SpriteFactory.Square;
            if(item==ItemId.Lockpick) return Resources.Load<Sprite>("Items/Lockpick");
            if(ItemCatalog.PassageTool(item) || ItemCatalog.ExpeditionOnly(item)) return PassageToolArt.Get(item);
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
            var dungeonSprite = DungeonToolArt.GetWorld(item);
            var sprite = GetWorld(item);
            if (sprite == null) return false;
            var source = root.GetComponent<SpriteRenderer>();
            // Network visibility may present the same pickup again after a hide/show cycle.
            var existing = root.transform.Find("ItemIcon");
            var art = existing != null ? existing.gameObject : new GameObject("ItemIcon");
            if(existing == null) art.transform.SetParent(root.transform, false);
            art.transform.localPosition = Vector3.zero;
            art.transform.localRotation = Quaternion.identity;
            float longest = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            art.transform.localScale = Vector3.one * Mathf.Clamp(GameplayDefinition.Current.WorldLootVisualScale,1f,2f) / Mathf.Max(.001f, longest);
            if(dungeonSprite!=null)
            {
                float scale=.8f/Mathf.Max(.001f,longest);
                art.transform.localScale=new Vector3(scale/Mathf.Max(.001f,Mathf.Abs(root.transform.lossyScale.x)),scale/Mathf.Max(.001f,Mathf.Abs(root.transform.lossyScale.y)),1);
            }
            var renderer = art.GetComponent<SpriteRenderer>();
            if(renderer == null) renderer = art.AddComponent<SpriteRenderer>();
            SpriteFactory.Setup(renderer, sprite, Color.white, source.sortingOrder);
            renderer.enabled = true;
            source.enabled = false;
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            loaded = null; attempted = false;
            System.Array.Clear(reportedWorldFallback,0,reportedWorldFallback.Length);
        }
    }
}
