using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using CyberExtraction.Items;

public static class ItemIconChecks
{
    [MenuItem("Cyber Extraction/Itens/Verificar icones")]
    public static void Run()
    {
        var library = Resources.Load<ItemIconLibrary>("ItemIcons");
        if (library == null || library.icons.Length != Enum.GetValues(typeof(ItemId)).Length)
            throw new Exception("Catalogo incompleto.");
        for (int i = 0; i < library.icons.Length; i++)
        {
            var sprite = ItemIconLibrary.Get((ItemId)i);
            if (sprite == null || !sprite.texture.alphaIsTransparency) throw new Exception("Icone ausente/sem alpha: " + (ItemId)i);
            if (!AssetDatabase.GetAssetPath(sprite).EndsWith(ItemIconSetup.Files[i] + "-icon-v1.png"))
                throw new Exception("Mapeamento incorreto: " + (ItemId)i);
            var root = new GameObject("ItemIconColliderProbe");
            try
            {
                root.AddComponent<SpriteRenderer>();
                var collider = root.AddComponent<CircleCollider2D>();
                collider.radius = 1.5f; collider.isTrigger = true;
                root.transform.localScale = new Vector3(.45f, .45f, 1);
                if (!ItemIconLibrary.ApplyWorld(root, (ItemId)i) || !collider.isTrigger || collider.radius != 1.5f ||
                    root.transform.localScale != new Vector3(.45f, .45f, 1))
                    throw new Exception("Visual alterou coleta: " + (ItemId)i);
                var art = root.transform.GetChild(0).GetComponent<SpriteRenderer>();
                if (art.sprite != sprite || art.color != Color.white || root.GetComponent<SpriteRenderer>().enabled)
                    throw new Exception("Visual do loot incorreto: " + (ItemId)i);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        if (ItemIconLibrary.Get((ItemId)255) != null) throw new Exception("Item invalido deve retornar null.");
        Debug.Log("ITEM ICON CHECKS PASS: 18 mappings, alpha, world visuals and unchanged collection colliders.");
    }

    public static void BuildPreview()
    {
        // Only an isolated validation checkout may create this disposable scene/build.
        if (!Application.dataPath.Replace('\\', '/').Contains("/Builds/SkillsValidation/"))
            throw new InvalidOperationException("Execute pela copia isolada Tools/Validate-Unity.ps1.");
        Run();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("ItemIconPreview").AddComponent<CyberExtraction.Diagnostics.ItemIconPreview>();
        const string scenePath = "Assets/Scenes/ItemIconPreview.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Directory.CreateDirectory("Builds/ItemIconPreview");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { scenePath }, locationPathName = "Builds/ItemIconPreview/ItemIconPreview.exe",
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Falha no build da previa.");
        Debug.Log("ITEM ICON PREVIEW BUILD PASS");
    }
}
