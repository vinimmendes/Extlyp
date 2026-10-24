using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CyberExtraction.Camp;
using CyberExtraction.Items;
using CyberExtraction.UI;

public static class ExpeditionRegressionChecks
{
    static readonly List<string> failures=new List<string>();
    static int checks;
    static void Verify(bool ok,string name) { checks++; if(!ok) { failures.Add(name); Debug.LogError("REGRESSION: "+name); } }
    [MenuItem("Cyber Extraction/Avançado/Diagnósticos/Inventário e equipamento inicial")]
    public static void Run()
    {
        checks=0; failures.Clear(); CampRepository.Close();
        try
        {
            string session=Guid.NewGuid().ToString("N");
            CampRepository.Edit(session,p=> { p.Pack=new InventoryGrid(); return 0; });
            var first=CampRepository.Begin(session,"first",out int food);
            Verify(first.Count(ItemId.Pistol)==1 && first.Slots[Array.FindIndex(first.Slots,s=>!s.Empty && s.Item==ItemId.Pistol)].Data==12,"Mochila vazia inicia com pistola carregada");
            Verify(first.Count(ItemId.PistolAmmo)==12,"Kit basico inclui um pacote de municao");
            CampRepository.Commit(session,"first"); CampRepository.Lose(session,"first");
            var second=CampRepository.Begin(session,"second",out food);
            Verify(second.Count(ItemId.Pistol)==1 && second.Count(ItemId.PistolAmmo)==12,"Morte sem armas guardadas fornece kit na proxima expedicao");
            CampRepository.Rollback(session,"second");
            var restored=CampRepository.Get(session);
            Verify(restored.Pack.Count(ItemId.Pistol)==1 && restored.Pack.Count(ItemId.PistolAmmo)==12,"Falha de inicio restaura kit sem duplicar");
            CampRepository.Edit(session,p=> { p.Pack=new InventoryGrid(); p.Pack.Add(new ItemStack(ItemId.Mosin,1,0,3)); p.Pack.Add(new ItemStack(ItemId.RifleAmmo,7)); return 0; });
            var rifle=CampRepository.Begin(session,"rifle",out food);
            Verify(rifle.Count(ItemId.Pistol)==0 && rifle.Count(ItemId.Mosin)==1 && rifle.Slots[0].Data==3 && rifle.Count(ItemId.RifleAmmo)==7,"Arma escolhida e carregador preservados");
            CampRepository.Rollback(session,"rifle");
            CampRepository.Edit(session,p=> { p.Pack=new InventoryGrid(); p.Chest=new InventoryGrid(); p.Chest.Add(new ItemStack(ItemId.DoubleBarrelShotgun,1,0,1)); return 0; });
            var stored=CampRepository.Begin(session,"stored",out food);
            Verify(stored.Count(ItemId.Pistol)==0 && stored.Count(ItemId.PistolAmmo)==0 && CampRepository.Get(session).Chest.Count(ItemId.DoubleBarrelShotgun)==1,"Arma guardada no bau impede kit gratuito");
            CampRepository.Rollback(session,"stored");
            CampRepository.Edit(session,p=> { p.Chest=new InventoryGrid(); p.Ground.Add(new CampGroundItem { Id=Guid.NewGuid().ToString("N"),Stack=new ItemStack(ItemId.Mosin,1,0,2) }); return 0; });
            var ground=CampRepository.Begin(session,"ground",out food);
            Verify(ground.Count(ItemId.Pistol)==0,"Arma recuperavel no acampamento impede kit gratuito");
            CampRepository.Rollback(session,"ground");
            CampRepository.Edit(session,p=> { p.Pack=new InventoryGrid(); p.Chest=new InventoryGrid(); p.Ground.Clear(); for(int i=0;i<9;i++) { p.Pack.Slots[i]=new ItemStack(ItemId.Wood,20); p.Chest.Slots[i]=new ItemStack(ItemId.Nails,20); } return 0; });
            var full=CampRepository.Begin(session,"full",out food); var profile=CampRepository.Get(session);
            int wood=full.Count(ItemId.Wood)+profile.Chest.Count(ItemId.Wood); foreach(var drop in profile.Ground) if(drop.Stack.Item==ItemId.Wood) wood+=drop.Stack.Quantity;
            Verify(full.Count(ItemId.Pistol)==1 && full.Count(ItemId.PistolAmmo)==12 && wood==180 && profile.Chest.Count(ItemId.Nails)==180,"Kit em mochila cheia conserva todos os materiais");
            InactiveCampInventory();
            if(failures.Count>0) throw new Exception(string.Join("; ",failures));
            Debug.Log("EXPEDITION REGRESSION CHECKS PASS: "+checks);
        }
        finally { CampRepository.Close(); }
    }
    static void InactiveCampInventory()
    {
        var oldCamp=CampController.Instance; var oldWindow=InventoryWindow.Instance;
        GameObject campObject=new GameObject("InactiveCampRegression"),windowObject=new GameObject("InventoryRegression");
        try
        {
            var camp=campObject.AddComponent<CampController>();
            typeof(CampController).GetProperty("Instance").SetValue(null,camp);
            camp.ExpeditionPanelOpen=true;
            typeof(CampController).GetField("activity",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(camp,5);
            var window=windowObject.AddComponent<InventoryWindow>();
            typeof(InventoryWindow).GetProperty("Instance").SetValue(null,window);
            Verify(!CampController.Active,"Fixture usa acampamento inativo");
            window.ToggleBag(); Verify(InventoryWindow.IsOpen,"Inventario abre com flags antigas de menu e atividade do acampamento");
            window.Close(); Verify(!InventoryWindow.IsOpen,"Inventario fecha normalmente");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(windowObject); UnityEngine.Object.DestroyImmediate(campObject);
            typeof(CampController).GetProperty("Instance").SetValue(null,oldCamp);
            typeof(InventoryWindow).GetProperty("Instance").SetValue(null,oldWindow);
        }
    }
}
