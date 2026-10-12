using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Match;
using CyberExtraction.Core;

namespace CyberExtraction.Enemies
{
    public static class SporeEncounter
    {
        static GameObject prefab;
        public static GameObject Prefab => prefab != null ? prefab : (prefab = Resources.Load<GameObject>("Spores/Esporifero"));
        public static readonly Vector2 Wind = new Vector2(.65f,.15f);
        public static bool IsSheltered(Vector2 point)
        {
            foreach(var center in MapBuilder.SpawnPoints)
                if(Mathf.Abs(point.x-center.x)<4 && Mathf.Abs(point.y-center.y)<2.5f) return true;
            return false;
        }
        public static void Spawn()
        {
            if(NetworkManager.Singleton==null || !NetworkManager.Singleton.IsServer || MapBuilder.Current!=ExpeditionMap.Large || Prefab==null) return;
            foreach(var shelter in MapBuilder.SpawnPoints)
            {
                if(!ExpeditionRolls.Chance(GameplayDefinition.Current.SporeSpawnChancePercent)) continue;
                for(int attempt=0;attempt<12;attempt++)
                {
                    float angle=Random.Range(0f,Mathf.PI*2);
                    Vector2 point=shelter+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*Random.Range(24f,38f);
                    if(!PlantEncounter.NaturalSpawnClear(point,.55f)) continue;
                    Object.Instantiate(Prefab,point,Quaternion.identity).GetComponent<NetworkObject>().Spawn(); break;
                }
            }
        }
    }
}
