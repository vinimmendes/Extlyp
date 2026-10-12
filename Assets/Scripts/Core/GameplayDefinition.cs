using UnityEngine;
namespace CyberExtraction.Core
{
    [CreateAssetMenu(fileName="Gameplay",menuName="Cyber Extraction/Gameplay")]
    public class GameplayDefinition : ScriptableObject
    {
        [Min(1)] public int Magazine=12, AmmoRecipeOutput=12, MetalRecipeCost=3, PowderRecipeCost=1, ComponentRecipeCost=1;
        [Min(.1f)] public float ReloadSeconds=1.5f;
        [Min(1)] public int ComponentPrice=2;
        [Range(0,100)] public int MimicChestChancePercent=10;
        [Header("Encontros da expedicao")]
        [Range(0,100)] public int MonsterSpawnChancePercent=45;
        [Range(0,100)] public int PlantSpawnChancePercent=45, SporeSpawnChancePercent=60;
        [Header("Loot sorteado no servidor")]
        [Range(0,100)] public int GroundLootChancePercent=65, ChestLootChancePercent=85, MonsterLootChancePercent=60;
        [Range(0,100)] public int LootWeaponChancePercent=10, LootAmmoChancePercent=30;
        [Range(0,100)] public int TwoLootItemsChancePercent=30, ThreeLootItemsChancePercent=15;
        [Range(1f,2f)] public float WorldLootVisualScale=1.5f;
        public static GameplayDefinition Current
        {
            get
            {
                if(current==null) current=Resources.Load<GameplayDefinition>("Gameplay");
                if(current==null) { current=CreateInstance<GameplayDefinition>(); current.hideFlags=HideFlags.HideAndDontSave; }
                return current;
            }
        }
        static GameplayDefinition current;
        static bool Percent(int value) => value>=0 && value<=100;
        public bool Valid => Magazine>0 && Magazine<=100 && ReloadSeconds>=.1f && ReloadSeconds<=30 &&
            AmmoRecipeOutput>0 && AmmoRecipeOutput<=180 && MetalRecipeCost>0 && PowderRecipeCost>0 &&
            ComponentRecipeCost>0 && ComponentPrice>0 && Percent(MimicChestChancePercent) &&
            Percent(MonsterSpawnChancePercent) && Percent(PlantSpawnChancePercent) && Percent(SporeSpawnChancePercent) &&
            Percent(GroundLootChancePercent) && Percent(ChestLootChancePercent) && Percent(MonsterLootChancePercent) &&
            Percent(LootWeaponChancePercent) && Percent(LootAmmoChancePercent) && LootWeaponChancePercent+LootAmmoChancePercent<=100 &&
            Percent(TwoLootItemsChancePercent) && Percent(ThreeLootItemsChancePercent) &&
            TwoLootItemsChancePercent+ThreeLootItemsChancePercent<=100 && WorldLootVisualScale>=1 && WorldLootVisualScale<=2;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { if(current!=null && current.hideFlags==HideFlags.HideAndDontSave) Destroy(current); current=null; }
    }
}
