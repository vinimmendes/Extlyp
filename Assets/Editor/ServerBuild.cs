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

    public static void BuildWindowsBoth() { ServerChecks.Run(); BuildServer(); BuildClient(); }
    static bool Release => Array.IndexOf(Environment.GetCommandLineArgs(),"-release")>=0;
    static BuildOptions Options => Release ? BuildOptions.None : BuildOptions.Development;
    static void Configure()
    {
        PlayerSettings.productName="Cyber Extraction";
        string version=Environment.GetEnvironmentVariable("CE_BUILD_VERSION");
        PlayerSettings.bundleVersion=string.IsNullOrEmpty(version) ? "1.2" : version;
    }

    [MenuItem("Cyber Extraction/Servidor/Gerar APK Android para teste")]
    public static void BuildAndroid()
    {
        var connection=CyberExtraction.Core.ServerConnectionSettings.Load();
        string androidAddress=connection!=null ? connection.ResolveAddress(true) : "";
        if(!System.Net.IPAddress.TryParse(androidAddress,out var endpoint) ||
            endpoint.AddressFamily!=System.Net.Sockets.AddressFamily.InterNetwork ||
            System.Net.IPAddress.IsLoopback(endpoint) || endpoint.Equals(System.Net.IPAddress.Any) ||
            endpoint.GetAddressBytes()[0]>=224)
            throw new Exception("Configure um IPv4 de servidor acessivel pelo celular em ServerConnectionSettings.androidAddress antes de gerar o APK.");
        const string output = "Builds/Android/CyberExtraction.apk";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        Configure();
        PlayerSettings.Android.bundleVersionCode = int.TryParse(Environment.GetEnvironmentVariable("CE_ANDROID_VERSION_CODE"),out int code) ? code : 3;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.cyberextraction.game");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.forceInternetPermission = true;
        PlayerSettings.Android.useCustomKeystore = Release;
        if(Release)
        {
            string keystore=Environment.GetEnvironmentVariable("CE_KEYSTORE_PATH"),alias=Environment.GetEnvironmentVariable("CE_KEYSTORE_ALIAS");
            if(string.IsNullOrEmpty(keystore) || string.IsNullOrEmpty(alias)) throw new Exception("Release Android exige keystore e alias configurados no ambiente.");
            PlayerSettings.Android.keystoreName=keystore; PlayerSettings.Android.keyaliasName=alias;
            PlayerSettings.Android.keystorePass=Environment.GetEnvironmentVariable("CE_KEYSTORE_PASSWORD");
            PlayerSettings.Android.keyaliasPass=Environment.GetEnvironmentVariable("CE_KEY_PASSWORD");
        }
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Game.unity" },
            locationPathName = output,
            target = BuildTarget.Android,
            options = Options
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Falha ao gerar APK. Veja os erros no Console.");
        Debug.Log("APK pronto: " + Path.GetFullPath(output));
    }

    static void Build(string output)
    {
        Configure();
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        // Build Windows comum tambem funciona como servidor com -batchmode -nographics -server.
        // Nao exige instalar o modulo opcional Dedicated Server.
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Game.unity" },
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            subtarget = (int)StandaloneBuildSubtarget.Player,
            options = Options
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Falha ao gerar: " + output + ". Veja os erros no Console.");
        Debug.Log("Build pronto: " + Path.GetFullPath(output));
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"build-manifest.json"),"{\"version\":\""+PlayerSettings.bundleVersion+"\",\"protocol\":"+CyberExtraction.Core.GameConfig.NetworkProtocol+",\"release\":"+(Release ? "true" : "false")+"}");
    }
}
