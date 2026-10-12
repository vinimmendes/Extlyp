using System;
using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using CyberExtraction.Combat;
using CyberExtraction.Enemies;

public static class PlantChecks
{
    [MenuItem("Cyber Extraction/Plantas/Verificar regras e recursos")]
    public static void Run()
    {
        int count = 0;
        Action<bool, string> verify = (ok, name) => { if (!ok) throw new Exception(name); count++; };
        verify(PlantRules.CanSpawn(0, 0, PlantKind.AttackBulb), "Permite bulbo atacante");
        verify(!PlantRules.CanSpawn(PlantRules.MaxAttackChildren, 0, PlantKind.AttackBulb), "Limita atacantes e bulbos juntos");
        verify(!PlantRules.CanSpawn(0, PlantRules.MaxHealers, PlantKind.HealingBulb), "Limita curadores");
        verify(!PlantRules.CanSpawn(0, 0, PlantKind.Mother) && !PlantRules.CanSpawn(0, 0, PlantKind.Minion), "Mae so produz bulbos");
        verify(PlantRules.IsAttacker(PlantKind.AttackBulb) && PlantRules.IsAttacker(PlantKind.Minion), "Amadurecimento conserva limite");
        verify(PlantRules.CanHeal(true, 200, 300, 4, true), "Cura mae ferida em alcance");
        verify(!PlantRules.CanHeal(true, 300, 300, 4, true), "Vida cheia nao emite cura");
        verify(!PlantRules.CanHeal(false, 0, 300, 4, true), "Nao ressuscita mae morta");
        verify(!PlantRules.CanHeal(true, 200, 300, 7, true), "Nao cura fora do alcance");
        verify(!PlantRules.CanHeal(true, 200, 300, 4, false), "Parede bloqueia cura");
        verify(PlantRules.Maturity(0, 10, 19) == 0 && PlantRules.Maturity(30, 10, 19) == 1, "Progresso limitado");
        verify(Math.Abs(PlantRules.Maturity(14.5, 10, 19) - .5f) < .001f, "Meio do amadurecimento");
        verify(PlantRules.Maturity(10, 10, 10) == 0, "Duracao invalida nao divide por zero");
        verify(PlantRules.PulseFrequency(1) > PlantRules.PulseFrequency(0), "Pulso acelera perto de nascer");
        verify(PlantRules.ParticleTravel(10, 10, 0) == 0, "Particula inicia no bulbo");
        verify(PlantRules.ParticleTravel(10.5, 10, 0) < 1, "Sem chegada antecipada");
        verify(PlantRules.ParticleTravel(10 + PlantRules.HealTravelSeconds, 10, 0) > .999f, "Primeira particula chega apos viajar");
        verify(PlantRules.ParticleTravel(10 + PlantRules.HealTravelSeconds, 10, PlantRules.HealParticleCount - 1) < 1, "Ultima particula ainda esta em transito");
        verify(PlantRules.ParticleTravel(10 + PlantRules.HealArrivalSeconds, 10, PlantRules.HealParticleCount - 1) > .999f, "Cura coincide com chegada da ultima particula");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlantSetup.PrefabPath);
        verify(prefab != null, "Prefab incluido");
        verify(prefab.GetComponent<NetworkObject>() != null && prefab.GetComponent<NetworkTransform>() != null, "Replicacao de identidade e movimento");
        verify(prefab.GetComponent<PlantCreature>() != null && prefab.GetComponent<PlantVisual>() != null, "Comportamento e arte no prefab");
        verify(prefab.GetComponent<Health>() != null && prefab.GetComponent<VisibilityTarget>() != null, "Vida e visibilidade no prefab");
        verify(prefab.GetComponent<Monster>() == null, "Sem IA antiga concorrendo com plantas");
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PlantSetup.SheetPath);
        verify(texture != null && texture.width == 1306 && texture.height == 1205, "PNG aprovado sem redimensionamento");
        foreach (PlantKind kind in Enum.GetValues(typeof(PlantKind)))
            verify(PlantVisual.SpriteFor(kind) != null, "Recorte de sprite " + kind);
        Debug.Log($"Plantas: {count} verificacoes passaram. Teste de rede e combate deve ser feito em Play com servidor/cliente.");
    }
}
