using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Match;

namespace CyberExtraction.Enemies
{
    public static class PlantEncounter
    {
        static GameObject prefab;
        public static GameObject Prefab => prefab != null ? prefab : (prefab = Resources.Load<GameObject>("Plants/PlantCreature"));
        public static bool ClearSpawn(Vector2 position, float radius)
        {
            foreach (var collider in Physics2D.OverlapCircleAll(position, radius + .15f))
                if (collider != null && !collider.isTrigger) return false;
            return Mathf.Abs(position.x) < MapBuilder.Width / 2 - radius &&
                Mathf.Abs(position.y) < MapBuilder.Height / 2 - radius;
        }
        public static PlantCreature Spawn(PlantKind kind, Vector2 position, PlantCreature mother = null)
        {
            var manager = NetworkManager.Singleton;
            if (manager == null || !manager.IsServer || Prefab == null) return null;
            var go = Object.Instantiate(Prefab, position, Quaternion.identity);
            var creature = go.GetComponent<PlantCreature>();
            creature.Configure(kind, mother);
            go.GetComponent<NetworkObject>().Spawn(); return creature;
        }
        public static void ServerSpawnMothers()
        {
            var manager = NetworkManager.Singleton;
            if (manager == null || !manager.IsServer) return;
            if (Prefab == null)
            {
                Debug.LogError("Prefab da planta ausente. Execute Cyber Extraction > Plantas > Preparar sprites e prefab."); return;
            }
            // Uma colonia perto de cada abrigo, fora dos pontos de nascimento e extracao.
            foreach (var shelter in MapBuilder.SpawnPoints)
            {
                for (int attempt = 0; attempt < 12; attempt++)
                {
                    Vector2 position = shelter + new Vector2(16 + attempt * 1.5f, -16);
                    if (!ClearSpawn(position, PlantRules.Radius(PlantKind.Mother))) continue;
                    Spawn(PlantKind.Mother, position); break;
                }
            }
        }
    }
}
