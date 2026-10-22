using UnityEngine;

namespace CyberExtraction.Core
{
    [CreateAssetMenu(menuName="Cyber Extraction/Server connection")]
    public sealed class ServerConnectionSettings : ScriptableObject
    {
        public string address="127.0.0.1";
        [Tooltip("IPv4 do servidor usado no Android. Vazio utiliza o endereco padrao acima.")]
        public string androidAddress="";
        [Range(1,65533)] public int basePort=7777;
        public string ResolveAddress(bool android)
        {
            string selected=android && !string.IsNullOrWhiteSpace(androidAddress) ? androidAddress : address;
            return string.IsNullOrWhiteSpace(selected) ? "127.0.0.1" : selected.Trim();
        }
        public static ServerConnectionSettings Load() => Resources.Load<ServerConnectionSettings>("ServerConnectionSettings");
    }
}
