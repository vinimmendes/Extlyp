using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor.U2D;
using CyberExtraction.UI;

// Asset authoring on the user's next normal Editor opening. No scene generation or tests.
[InitializeOnLoad]
public static class PlayerUiAssetPreparation
{
    static bool importing;
    static PlayerUiAssetPreparation() { EditorApplication.delayCall+=Prepare; }
    [MenuItem("Cyber Extraction/Avançado/Preparação de assets/Fontes e atlas da interface")]
    public static void Prepare()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall+=Prepare; return; }
        if(AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset")==null)
        {
            if(importing) return; importing=true;
            AssetDatabase.importPackageCompleted+=Imported;
            AssetDatabase.importPackageFailed+=ImportFailed;
            TMP_PackageResourceImporter.ImportResources(true,false,false); return;
        }
        bool changed=false;
        var regular=Font("BarlowCondensed-Regular",ref changed);
        var bold=Font("BarlowCondensed-SemiBold",ref changed);
        EnsureFontWeights(regular,bold,ref changed);
        EnsureFontWeights(bold,bold,ref changed);
        const string atlasPath="Assets/Resources/UI/Art/PlayerUi.spriteatlas";
        if(AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath)==null)
        {
            var atlas=new SpriteAtlas(); atlas.Add(new Object[] {AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Resources/UI/Art")});
            AssetDatabase.CreateAsset(atlas,atlasPath); changed=true;
        }
        bool failed=false;
        foreach(string asset in AssetDatabase.FindAssets("t:Prefab",new[] {"Assets/Resources/UI"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(asset);
            GameObject root=null;
            bool loadFailed=false;
            void CaptureError(string message,string stack,LogType type)
            {
                if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)
                    loadFailed=true;
            }
            Application.logMessageReceived+=CaptureError;
            try
            {
                root=PrefabUtility.LoadPrefabContents(path);
                if(loadFailed || root==null)
                    throw new System.InvalidOperationException("A Unity reportou falha ao carregar a hierarquia; o prefab nao sera salvo.");
                bool fontChanged=false;
                foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                    if(text.font==null || text.font.sourceFontFile!=null && text.font.sourceFontFile.name.StartsWith("BarlowCondensed"))
                    {
                        var desired=text.fontWeight>=FontWeight.SemiBold ? bold : regular;
                        if(text.font!=desired) { text.font=desired; fontChanged=true; }
                    }
                if(fontChanged)
                {
                    PrefabUtility.SaveAsPrefabAsset(root,path,out bool saved);
                    if(!saved || loadFailed) throw new System.InvalidOperationException("Falha ao salvar as referencias de fonte.");
                    changed=true;
                }
            }
            catch(System.Exception error)
            {
                failed=true;
                Debug.LogError("Preparacao uGUI falhou em "+path+": "+error.Message);
            }
            finally
            {
                Application.logMessageReceived-=CaptureError;
                if(root!=null) PrefabUtility.UnloadPrefabContents(root);
            }
        }
        if(changed) AssetDatabase.SaveAssets();
        if(!failed && changed) Debug.Log("UI ASSETS PREPARED: fontes Barlow SDF, prefabs editaveis e atlas; cenas preservadas.");
    }
    static void Imported(string package)
    { AssetDatabase.importPackageCompleted-=Imported; AssetDatabase.importPackageFailed-=ImportFailed; importing=false; EditorApplication.delayCall+=Prepare; }
    static void ImportFailed(string package,string error)
    { AssetDatabase.importPackageCompleted-=Imported; AssetDatabase.importPackageFailed-=ImportFailed; importing=false; Debug.LogError("Importacao TMP nao concluida: "+error); }
    static TMP_FontAsset Font(string name,ref bool changed)
    {
        string path="Assets/Resources/UI/Fonts/"+name+"-SDF.asset";
        var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if(existing!=null)
        {
            if(!existing.HasCharacter('\u2026',false,false) && existing.TryAddCharacters("\u2026",out _))
            { EditorUtility.SetDirty(existing); changed=true; }
            return existing;
        }
        var source=Resources.Load<UnityEngine.Font>("UI/Fonts/"+name);
        var font=TMP_FontAsset.CreateFontAsset(source); font.name=name+"-SDF";
        AssetDatabase.CreateAsset(font,path);
        if(font.material!=null) AssetDatabase.AddObjectToAsset(font.material,font);
        foreach(var texture in font.atlasTextures) if(texture!=null) AssetDatabase.AddObjectToAsset(texture,font);
        font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789ÀÁÂÃÉÊÍÓÔÕÚÜÇàáâãéêíóôõúüç ×—‹›[]:/.,!?+-()…_",out _);
        EditorUtility.SetDirty(font); changed=true; return font;
    }
    static void EnsureFontWeights(TMP_FontAsset font,TMP_FontAsset face,ref bool changed)
    {
        foreach(int index in new[] {6,7})
        {
            var pair=font.fontWeightTable[index];
            if(pair.regularTypeface!=null && pair.regularTypeface!=font) continue;
            if(pair.regularTypeface==face) continue;
            pair.regularTypeface=face; font.fontWeightTable[index]=pair;
            EditorUtility.SetDirty(font); changed=true;
        }
    }
}
