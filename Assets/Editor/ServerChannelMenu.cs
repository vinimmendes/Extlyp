using UnityEditor;
using UnityEngine;

public static class ServerChannelMenu
{
    [MenuItem("Cyber Extraction/Servidor/Editor conecta na estavel (7777)", false, 10)]
    public static void Stable() => Select(7777);

    [MenuItem("Cyber Extraction/Servidor/Editor conecta no desenvolvimento (17777)", false, 11)]
    public static void Development() => Select(17777);

    static void Select(int port)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Encerre o Play antes de trocar o servidor do Editor.");
            return;
        }
        EditorPrefs.SetInt("CyberExtraction.ServerPort." + Application.dataPath, port);
        Debug.Log("Editor conectara na porta " + port + " no proximo Play. Builds e contas permanecem separados pelo servidor escolhido.");
    }
}
