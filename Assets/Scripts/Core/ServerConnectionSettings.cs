using UnityEngine;

namespace CyberExtraction.Core
{
    [CreateAssetMenu(menuName="Cyber Extraction/Server connection")]
    public sealed class ServerConnectionSettings : ScriptableObject
    {
        public string address="127.0.0.1";
        [Range(1,65533)] public int basePort=7777;
        public static ServerConnectionSettings Load() => Resources.Load<ServerConnectionSettings>("ServerConnectionSettings");
    }
}
