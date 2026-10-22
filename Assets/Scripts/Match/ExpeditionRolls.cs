using UnityEngine;
using CyberExtraction.Enemies;

namespace CyberExtraction.Match
{
    // Encounter selection and container presentation grade. Loot contents use LootRoller.
    public static class ExpeditionRolls
    {
        static readonly MonsterKind[] Monsters = { MonsterKind.Charger, MonsterKind.Listener, MonsterKind.Shell,
            MonsterKind.Weaver, MonsterKind.Herald, MonsterKind.Acid, MonsterKind.Stalker, MonsterKind.Devourer };
        static readonly int[] MonsterWeights = { 12, 20, 14, 18, 8, 12, 12, 4 };

        public static bool Chance(int percent) => Random.Range(0,100)<Mathf.Clamp(percent,0,100);

        public static byte ContainerGrade()
        {
            int roll=Random.Range(0,100);
            return roll<65 ? (byte)0 : roll<90 ? (byte)1 : (byte)2;
        }

        public static MonsterKind Monster()
        {
            int roll=Random.Range(0,100);
            for(int i=0;i<Monsters.Length;i++) { if(roll<MonsterWeights[i]) return Monsters[i]; roll-=MonsterWeights[i]; }
            return MonsterKind.Charger;
        }

    }
}
