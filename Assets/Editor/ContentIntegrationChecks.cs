using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using CyberExtraction.Items;
using CyberExtraction.Camp;
using CyberExtraction.Enemies;
using CyberExtraction.Combat;
using CyberExtraction.Players;

public static class ContentIntegrationChecks
{
    static int checks;
    static void Verify(bool ok,string message) { if(!ok) throw new Exception(message); checks++; }
    [MenuItem("Cyber Extraction/Avançado/Diagnósticos/Armas, plantas, esporos e minigames")]
    public static void Run()
    {
        checks=0;
        Verify((int)ItemId.RawFish==13 && (int)ItemId.Mosin==14,"IDs antigos dos itens preservados");
        foreach(var item in new[] {ItemId.Pistol,ItemId.Mosin,ItemId.DoubleBarrelShotgun})
        {
            var w=WeaponCatalog.Get(item);
            var bag=new InventoryGrid(); var chest=new InventoryGrid();
            Verify(bag.Add(new ItemStack(item,2,0,w.Capacity-1)) && bag.Slots[0].Quantity==1 && bag.Slots[1].Quantity==1,"Armas nao empilham: "+item);
            Verify(bag.MoveTo(0,chest,8) && chest.Slots[8].Data==w.Capacity-1,"Transferencia conserva tiros: "+item);
            var p=new CampProgress { Pack=new InventoryGrid() };
            p.Chest.Add(new ItemStack(ItemId.Metal,3)); p.Chest.Add(new ItemStack(ItemId.Gunpowder,1)); p.Chest.Add(new ItemStack(ItemId.AmmoComponent,1));
            var action=new CampAction { Sequence=1,Kind=CampActionKind.CraftAmmo,Result=(int)w.Ammo };
            Verify(p.Apply(action) && p.Count(w.Ammo)==WeaponCatalog.RecipeOutput(w.Ammo),"Receita de "+w.Ammo);
            Verify(!p.Apply(action),"Receita repetida rejeitada: "+w.Ammo);
        }
        Verify(WeaponCatalog.Get(ItemId.Mosin).Range==24 && WeaponCatalog.Get(ItemId.Mosin).Capacity==5,"Rifle conserva alcance e carregador");
        var shotgun=WeaponCatalog.Get(ItemId.DoubleBarrelShotgun);
        Verify(shotgun.Capacity==2 && shotgun.Pellets==8 && shotgun.DamageAt(3)==13 && shotgun.DamageAt(9)<13 && shotgun.DamageAt(10)==0,"Espingarda conserva queda de dano e chumbos");
        PlantChecks.Run();
        var spore=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Spores/Esporifero.prefab");
        Verify(spore!=null && spore.GetComponent<Esporifero>().cloudPrefab!=null,"Prefab de Esporifero e nuvem ligados");
        Verify(spore.GetComponent<EsporiferoVisual>().idleSprite!=null,"Sprite do Esporifero ligado");
        foreach(string name in new[] {"Player","Bot"})
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/"+name+".prefab");
            Verify(prefab.GetComponent<SporeContamination>()!=null,"Contaminacao em "+name);
        }
        Verify(SporeRules.DamageThreshold(150)==38 && SporeRules.Exposure(0,true,2)==40 && !SporeRules.Harmful(60,false),"Limites de descarga exposicao e recuperacao");
        var effects=new[] {PlantEncounter.Prefab,spore,Resources.Load<GameObject>("Spores/SporeCloud")};
        var hashes=new System.Collections.Generic.HashSet<uint>();
        foreach(var prefab in effects)
        {
            Verify(prefab!=null,"Prefab de rede presente");
            var serialized=new SerializedObject(prefab.GetComponent<NetworkObject>());
            Verify(hashes.Add((uint)serialized.FindProperty("GlobalObjectIdHash").longValue),"Prefab de rede com hash exclusivo");
        }
        ImplementationChecks.Activities();
        Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/content-integration-checks.txt","PASS: "+checks+" checks + PlantChecks + Activities; Unity import/C# NGO compilation completed. Multiplayer and Android manual checks pending.");
        Debug.Log("CONTENT INTEGRATION CHECKS PASS: "+checks+" + plants and activities");
    }
}
