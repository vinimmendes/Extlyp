using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ServerBuild
{
    [MenuItem("Cyber Extraction/Servidor/Gerar servidor Windows")]
    public static void BuildServer() => Build("Builds/Server/CyberExtractionServer.exe");

    [MenuItem("Cyber Extraction/Servidor/Gerar cliente Windows para teste")]
    public static void BuildClient() => Build("Builds/Client/CyberExtraction.exe");

    public static void BuildWindowsBoth() { BuildServer(); BuildClient(); }

    [MenuItem("Cyber Extraction/Servidor/Gerar APK Android para teste")]
    public static void BuildAndroid()
    {
        const string output = "Builds/Android/CyberExtraction.apk";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        PlayerSettings.productName = "Cyber Extraction";
        PlayerSettings.bundleVersion = "1.1";
        PlayerSettings.Android.bundleVersionCode = 2;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.cyberextraction.game");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.forceInternetPermission = true;
        PlayerSettings.Android.useCustomKeystore = false;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Game.unity" },
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Falha ao gerar APK. Veja os erros no Console.");
        Debug.Log("APK pronto: " + Path.GetFullPath(output));
    }

    static void Build(string output)
    {
        PlayerSettings.productName = "Cyber Extraction";
        PlayerSettings.bundleVersion = "1.1";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        // Build Windows comum tambem funciona como servidor com -batchmode -nographics -server.
        // Nao exige instalar o modulo opcional Dedicated Server.
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Game.unity" },
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            subtarget = (int)StandaloneBuildSubtarget.Player,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Falha ao gerar: " + output + ". Veja os erros no Console.");
        Debug.Log("Build pronto: " + Path.GetFullPath(output));
    }
}
