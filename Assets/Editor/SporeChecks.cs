using System;
using UnityEditor;
using UnityEngine;
using CyberExtraction.Enemies;
using CyberExtraction.Combat;
using CyberExtraction.UI;
using CyberExtraction.Match;

// Menu/batch checks; no NUnit dependency, consistent with BotChecks.
public static class SporeChecks
{
    [MenuItem("Cyber Extraction/Verificar Esporifero")]
    public static void Run()
    {
        int checks = 0;
        Action<bool, string> verify = (ok, name) =>
        {
            if (!ok) throw new Exception("Esporifero: " + name);
            checks++;
        };
        verify(SporeRules.DamageThreshold(150) == 38, "Round 37.5 damage up, never trigger early");
        verify(SporeRules.DamageThreshold(100) == 25, "Exact quarter health");
        verify(SporeRules.DamageThreshold(1) == 1, "Small health pools still require damage");
        float exposure = 0;
        for (int i = 0; i < 8; i++) exposure = SporeRules.Exposure(exposure, true, .25f);
        verify(Math.Abs(exposure - 40f) < .001f, "Two seconds reaches harmful exposure");
        verify(!SporeRules.Harmful(39.9f, true) && SporeRules.Harmful(40f, true),
            "Harm threshold boundary");
        verify(!SporeRules.Harmful(100f, false), "Leaving a cloud stops ongoing damage immediately");
        verify(SporeRules.Exposure(100f, true, 8f) == 100f, "Exposure caps at 100");
        verify(SporeRules.Exposure(40f, false, 3f) == 0f, "Fresh air fully recovers");
        verify(SporeRules.Exposure(0f, false, 1f) == 0f, "Recovery cannot go negative");
        verify(MapBuilder.IsSheltered(Vector2.zero) && !MapBuilder.IsSheltered(new Vector2(10, 10)),
            "Shelter bounds select still air");
        verify(!MapBuilder.IsSheltered(new Vector2(4, 0)), "Shelter edge is outdoors");

        var enemy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Esporifero.prefab");
        var cloud = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SporeCloud.prefab");
        var match = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MatchManager.prefab");
        verify(enemy != null && cloud != null && match != null, "Prefab assets exist");
        verify(enemy.GetComponent<Esporifero>().cloudPrefab == cloud.GetComponent<SporeCloud>(),
            "Creature references the registered cloud");
        verify(enemy.GetComponent<EsporiferoVisual>().idleSprite != null, "Sprite import resolves");
        verify(match.GetComponent<MatchManager>().esporiferoPrefab == enemy, "Match spawns creature");
        verify(enemy.GetComponent<Monster>().chaseSpeed < 2f, "Slow movement preserves distance counterplay");
        foreach (var name in new[] { "Player", "Bot" })
        {
            var participant = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + name + ".prefab");
            verify(participant != null && participant.GetComponent<SporeContamination>() != null,
                name + " supports server exposure");
        }
        var bootstrap = UnityEngine.Object.FindFirstObjectByType<NetworkBootstrap>();
        if (bootstrap != null)
            verify(bootstrap.esporiferoPrefab == enemy && bootstrap.sporeCloudPrefab == cloud,
                "Active scene registers both network prefabs");
        Debug.Log("Esporifero: " + checks +
            " checks passed. Run host/client and physics scenarios from ESPORIFERO.md in Play Mode.");
    }
}
