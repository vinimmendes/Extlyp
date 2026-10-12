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
        public static bool NaturalSpawnClear(Vector2 position,float radius)
        {
            if(!ClearSpawn(position,radius)) return false;
            foreach(var spawn in MapBuilder.SpawnPoints) if(Vector2.Distance(position,spawn)<18+radius) return false;
            foreach(var exit in MapBuilder.ExtractionPoints) if(Vector2.Distance(position,exit)<8+radius) return false;
            return true;
        }
        public static void ServerSpawnMothers()
        {
            var manager = NetworkManager.Singleton;
            if (manager == null || !manager.IsServer) return;
            if (Prefab == null)
            {
                Debug.LogError("Prefab da planta ausente. Execute Cyber Extraction > Plantas > Preparar sprites e prefab."); return;
            }
            // Potential colonies: independently rolled, outside protected areas.
            foreach (var shelter in MapBuilder.SpawnPoints)
            {
                if(!ExpeditionRolls.Chance(GameplayDefinition.Current.PlantSpawnChancePercent)) continue;
                for (int attempt = 0; attempt < 12; attempt++)
                {
                    float angle=Random.Range(0f,Mathf.PI*2);
                    Vector2 position=shelter+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*Random.Range(24f,38f);
                    if (!NaturalSpawnClear(position, PlantRules.Radius(PlantKind.Mother))) continue;
                    Spawn(PlantKind.Mother, position); break;
                }
            }
        }
    }
}
