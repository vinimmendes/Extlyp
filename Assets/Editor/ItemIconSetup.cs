using System;
using UnityEditor;
using UnityEngine;
using CyberExtraction.Items;

public static class ItemIconSetup
{
    public static readonly string[] Files = { "wood", "nails", "bandage", "metal", "gunpowder", "ammo-component",
        "pistol", "pistol-ammo", "fish-common", "fish-rare", "fish-large", "cooked-fish", "burnt-fish", "raw-fish",
        "bolt-rifle", "rifle-ammo", "double-barrel-shotgun", "shotgun-shell" };

    [MenuItem("Cyber Extraction/Itens/Preparar icones")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do Play antes de preparar os icones.");
        var sprites = new Sprite[Files.Length];
        for (int i = 0; i < Files.Length; i++)
        {
            string path = "Assets/Art/Items/" + Files[i] + "-icon-v1.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new Exception("Icone ausente: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 512;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spritePivot = new Vector2(.5f, .5f);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprites[i] == null) throw new Exception("Importacao falhou: " + path);
        }
        const string assetPath = "Assets/Resources/ItemIcons.asset";
        var library = AssetDatabase.LoadAssetAtPath<ItemIconLibrary>(assetPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<ItemIconLibrary>();
            AssetDatabase.CreateAsset(library, assetPath);
        }
        library.icons = sprites;
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        Debug.Log("ITEM ICONS PREPARED: 18 sprites; original PNGs preserved.");
    }
}
