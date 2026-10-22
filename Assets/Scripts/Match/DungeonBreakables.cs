using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Combat;

namespace CyberExtraction.Match
{
    public static class DungeonBreakables
    {
        public static GameObject Prefab => Resources.Load<GameObject>("Environment/Dungeon/Breakables/DungeonBreakable");
        public static void ServerSpawn()
        {
            if(NetworkManager.Singleton==null || !NetworkManager.Singleton.IsServer || MapBuilder.Current!=ExpeditionMap.Dungeon) return;
            var data=DungeonBreakableLayout.Load(); var prefab=Prefab;
            if(data==null || prefab==null) { Debug.LogWarning("Catacumbas: prefab/catalogo de quebraveis ausente."); return; }
            int seed=MatchManager.Instance.LayoutSeed.Value;
            var selection=new System.Random(seed ^ 0x18b74c2);
            // Reward seed stays server-local; clients know the layout seed.
            var rewards=new System.Random(System.Guid.NewGuid().GetHashCode());
            var loot=Resources.Load<DungeonBreakableLoot>("Environment/Dungeon/Breakables/BreakableLoot");
            bool validLoot=loot!=null && loot.Valid();
            if(!validLoot) Debug.LogWarning("Catacumbas: configuracao de loot dos quebraveis ausente/invalida; objetos sem recompensa.");
            var skipped=new int[DungeonLayout.Rooms.Length];
            for(int room=8;room<skipped.Length;room++) skipped[room]=selection.Next(3);
            var perRoom=new int[DungeonLayout.Rooms.Length];
            foreach(var point in data.candidates)
            {
                // Each room keeps two distinct positions out of its three;
                // entry and corridor candidates remain present.
                if(point.group=="Room" && perRoom[point.room]++==skipped[point.room]) continue;
                var go=Object.Instantiate(prefab,point.Position,Quaternion.Euler(0,0,point.angle));
                var breakable=go.GetComponent<DungeonBreakable>();
                breakable.CandidateId.Reset(point.id);
                breakable.ConfigureReward(validLoot ? loot.Roll(rewards) : default);
                go.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}
