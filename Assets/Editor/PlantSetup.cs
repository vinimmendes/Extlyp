using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using CyberExtraction.Combat;
using CyberExtraction.Enemies;

public static class PlantSetup
{
    public const string SheetPath = "Assets/Resources/Plants/PlantMonsterSheet.png";
    public const string PrefabPath = "Assets/Resources/Plants/PlantCreature.prefab";
    [MenuItem("Cyber Extraction/Plantas/Preparar sprites e prefab")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Saia do Play antes de preparar as plantas."); return; }
        var importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
        if (importer == null) { Debug.LogError("PNG aprovado das plantas nao encontrado em " + SheetPath); return; }
        importer.textureType = TextureImporterType.Default;
        importer.isReadable = true; importer.sRGBTexture = true;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
        if (texture.width != 1306 || texture.height != 1205) { Debug.LogError("Nao redimensione o PNG: os recortes usam o tamanho aprovado 1306x1205."); return; }
        var go = new GameObject("PlantCreature");
        go.AddComponent<NetworkObject>();
        var body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0; body.freezeRotation = true; body.interpolation = RigidbodyInterpolation2D.Interpolate;
        go.AddComponent<CircleCollider2D>().radius = PlantRules.Radius(PlantKind.Mother);
        var nt = go.AddComponent<NetworkTransform>(); nt.AuthorityMode = NetworkTransform.AuthorityModes.Server;
        go.AddComponent<Health>().maxHealth = PlantRules.MotherHealth;
        go.AddComponent<PlantCreature>(); go.AddComponent<PlantVisual>(); go.AddComponent<VisibilityTarget>();
        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath); Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();
        Debug.Log("Plantas preparadas: sprites aprovados, prefab unico registrado pelo NetworkBootstrap. Cenas existentes preservadas.");
    }
}
