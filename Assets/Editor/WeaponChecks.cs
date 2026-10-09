using System;
using UnityEditor;
using UnityEngine;
using CyberExtraction.Items;
using CyberExtraction.Camp;

public static class WeaponChecks
{
    [MenuItem("Cyber Extraction/Verificar novas armas")]
    public static void Run()
    {
        int count = 0;
        Action<bool, string> verify = (ok, name) => { if (!ok) throw new Exception(name); count++; };
        verify((byte)ItemId.RawFish == 13 && (byte)ItemId.Mosin == 14, "IDs antigos preservados");
        var rifle = WeaponCatalog.Get(ItemId.Mosin);
        var shotgun = WeaponCatalog.Get(ItemId.DoubleBarrelShotgun);
        verify(rifle.Capacity == 5 && rifle.BoltSeconds > 0 && rifle.Cooldown >= rifle.BoltSeconds, "Ferrolho e capacidade do rifle");
        verify(shotgun.Capacity == 2 && shotgun.Pellets == 8, "Dois canos e oito chumbos por cartucho");
        verify(shotgun.DamageAt(0) == 13 && shotgun.DamageAt(3) == 13 && shotgun.DamageAt(9) < 13 && shotgun.DamageAt(10) == 0, "Queda de dano da espingarda");
        verify(rifle.HalfAngle(0) < rifle.HalfAngle(1) && rifle.Range > shotgun.Range, "Precisao e alcance");
        verify(WeaponCatalog.Get(ItemId.Wood) == null && !ItemCatalog.Weapon(ItemId.RifleAmmo), "Itens nao viram armas");
        var grid = new InventoryGrid();
        verify(grid.Add(new ItemStack(ItemId.Mosin, 2, 0, 3)) && grid.Slots[0].Quantity == 1 && grid.Slots[1].Quantity == 1, "Armas nao empilham");
        verify(grid.MoveTo(0, grid, 8) && grid.Slots[8].Data == 3, "Mover conserva municao carregada");
        var chest = new InventoryGrid();
        verify(grid.MoveTo(8, chest, 4) && chest.Slots[4].Item == ItemId.Mosin && chest.Slots[4].Data == 3, "Transferencia conserva arma e municao");
        foreach (var ammo in new[] { ItemId.PistolAmmo, ItemId.RifleAmmo, ItemId.ShotgunShell })
        {
            var profile = new CampProgress { Pack = new InventoryGrid() };
            verify(!profile.CraftAmmo(ammo), "Sem materiais nao fabrica " + ammo);
            profile.Chest.Add(new ItemStack(ItemId.Metal, 3));
            profile.Chest.Add(new ItemStack(ItemId.Gunpowder, 1));
            profile.Chest.Add(new ItemStack(ItemId.AmmoComponent, 1));
            var action = new CampAction { Kind = CampActionKind.CraftAmmo, Result = (int)ammo };
            verify(profile.Apply(action), "Receita valida " + ammo);
            int expected = ammo == ItemId.PistolAmmo ? 12 : ammo == ItemId.RifleAmmo ? 5 : 4;
            verify(profile.Count(ammo) == expected && profile.Count(ItemId.Metal) == 0, "Quantidade e custo " + ammo);
            verify(!profile.Apply(action) && profile.Count(ammo) == expected, "Acao repetida nao duplica " + ammo);
        }
        var invalid = new CampProgress();
        verify(!invalid.CraftAmmo(ItemId.Mosin) && !invalid.Apply(new CampAction { Kind = CampActionKind.CraftAmmo, Result = 255 }), "Receita invalida rejeitada");
        var full = new CampProgress();
        for (int i = 0; i < 9; i++) { full.Chest.Slots[i] = new ItemStack(ItemId.Wood, 20); full.Pack.Slots[i] = new ItemStack(ItemId.Nails, 20); }
        full.Chest.Slots[0] = new ItemStack(ItemId.Metal, 20);
        full.Chest.Slots[1] = new ItemStack(ItemId.Gunpowder, 20);
        full.Chest.Slots[2] = new ItemStack(ItemId.AmmoComponent, 20);
        verify(!full.CraftAmmo(ItemId.RifleAmmo) && full.Count(ItemId.Metal) == 20 && full.Count(ItemId.Gunpowder) == 20 && full.Count(ItemId.AmmoComponent) == 20,
            "Receita sem espaco reverte todos os custos");
        Debug.Log($"Novas armas: {count} verificacoes passaram.");
    }
}
