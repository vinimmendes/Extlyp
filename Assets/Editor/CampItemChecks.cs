using System;
using UnityEditor;
using UnityEngine;
using CyberExtraction.Items;
using CyberExtraction.Camp;

public static class CampItemChecks
{
    [MenuItem("Cyber Extraction/Verificar acampamento e itens")]
    public static void Run()
    {
        int count = 0;
        Action<bool, string> verify = (ok, name) => { if (!ok) throw new Exception(name); count++; };
        verify(ItemCatalog.Icon(ItemId.Wood) == "M" && ItemCatalog.Icon(ItemId.Nails) == "P" && ItemCatalog.Icon(ItemId.Bandage) == "B", "Icones dos itens");
        var profile = new CampProgress();
        verify(!profile.RepairPier() && !profile.Fish(), "Sem recursos nao repara nem pesca");
        profile.Deposit(20, 9, 5);
        verify(!profile.RepairPier() && profile.Wood == 20 && profile.Nails == 9, "Reparo insuficiente nao consome materiais");
        profile.Deposit(0, 1, 0);
        verify(profile.RepairPier() && profile.Wood == 0 && profile.Nails == 0, "Reparo consome custo exato");
        verify(!profile.RepairPier(), "Nao cobrar reparo repetido");
        verify(profile.Fish() && !profile.Fish(), "Buff nao empilha");
        verify(profile.ConsumeFoodBuff() == 20 && profile.ConsumeFoodBuff() == 0, "Buff serve para uma expedicao");
        verify(profile.TakeBandages(999) == 3 && profile.Bandages == 2, "Limite do equipamento de bandagens");
        verify(profile.TakeBandages(-1) == 0 && profile.Bandages == 2, "Quantidade negativa nao cria itens");
        verify(profile.TakeBandages(3) == 2 && profile.Bandages == 0, "Nao levar mais do que ha no deposito");
        var snapshot = profile.Copy(); snapshot.Deposit(3, 3, 3);
        verify(profile.Wood == 0 && profile.Nails == 0, "Snapshot nao altera servidor");
        var request = new CampJoinRequest { Session = Guid.NewGuid().ToString("N"), RepairPier = true, Fish = true };
        var authoritative = CampRepository.Get(request.Session);
        CampRepository.Apply(request);
        verify(!authoritative.PierRepaired && !authoritative.WellFed, "Pedido do cliente nao concede reparo/buff sem materiais");
        authoritative.Deposit(20, 10, 0);
        CampRepository.Apply(request); CampRepository.Apply(request);
        verify(authoritative.PierRepaired && authoritative.WellFed && authoritative.Wood == 0 && authoritative.Nails == 0,
            "Pedido repetido nao duplica custo ou buff");
        Debug.Log($"Acampamento/itens: {count} verificacoes passaram.");
    }
}
