using System;
using UnityEngine;

namespace CyberExtraction.Items
{
    /// <summary>Presentation configuration only. UI replacements never implicitly replace world art.</summary>
    [CreateAssetMenu(menuName="Cyber Extraction/Item visual catalog",fileName="ItemVisualCatalog")]
    public sealed class ItemVisualCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public ItemId Id;
            public Sprite InterfaceIcon;
            public Sprite WorldIcon;
        }

        public int Version=1;
        public Entry[] Entries=Array.Empty<Entry>();
        static bool attempted;
        static ItemVisualCatalog loaded;
        static Entry[] index;

        public static Sprite Interface(ItemId item)=>ItemCatalog.Available(item) ? Find(item)?.InterfaceIcon : null;
        public static Sprite World(ItemId item)=>ItemCatalog.Available(item) ? Find(item)?.WorldIcon : null;

        static Entry Find(ItemId item)
        {
            if(!ItemCatalog.Valid(item)) return null;
            if(!attempted)
            {
                attempted=true;
                loaded=Resources.Load<ItemVisualCatalog>("ItemVisualCatalog");
                index=new Entry[ItemCatalog.Count];
                if(loaded!=null && loaded.Entries!=null)
                    foreach(var entry in loaded.Entries)
                    {
                        if(entry==null || !ItemCatalog.Valid(entry.Id))
                        { Debug.LogWarning("Entrada visual de item inválida; usando arte anterior."); continue; }
                        if(index[(int)entry.Id]!=null)
                        { Debug.LogWarning("Ícone duplicado no catálogo visual: "+entry.Id); continue; }
                        index[(int)entry.Id]=entry;
                    }
            }
            return index[(int)item];
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { attempted=false; loaded=null; index=null; }
    }
}
