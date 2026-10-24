using UnityEngine;
using UnityEngine.Tilemaps;

namespace CyberExtraction.Match
{
    // Scene-only source. The game reads the separately published, checked document.
    public sealed class DungeonMapAuthoring : MonoBehaviour
    {
        [HideInInspector] public DungeonMapDocument Document=new DungeonMapDocument();
        public DungeonArtCatalog Catalog;
        public DungeonArtCatalog MigratedCatalog;
        public Tile StoneTile,FloorTile,WaterTile,CoverTile;
        public string PublishedPath="Assets/Resources/Environment/Dungeon/ReworkComplete/Published.json";
        [TextArea(2,8)] public string MigrationReport;
        public bool MigrationReconciled;
        public int PreviewSeed=43127;
        public string LegacyRevision;
        [HideInInspector] public string LegacySourceHash;
        public string PrefabFolder="Assets/Art/Catacumbas/Prefabs";
        [HideInInspector] public string[] FunctionalIdentity=System.Array.Empty<string>();
        [HideInInspector] public int VotivePresentationRevision;
        public System.Collections.Generic.IEnumerable<DungeonArtEntry> ArtEntries
        {
            get
            {
                if(Catalog!=null) foreach(var entry in Catalog.Entries) yield return entry;
                if(MigratedCatalog!=null) foreach(var entry in MigratedCatalog.Entries) yield return entry;
            }
        }
        public DungeonArtEntry FindArt(string id)=>Catalog.Find(id)??(MigratedCatalog!=null ? MigratedCatalog.Find(id) : null);
    }
}
