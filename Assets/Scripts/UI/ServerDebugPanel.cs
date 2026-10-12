using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using CyberExtraction.Core;

namespace CyberExtraction.UI
{
    // Controle temporario do processo local. Nao existe RPC de reinicio remoto.
    public class ServerDebugPanel : MonoBehaviour
    {
        static ServerDebugPanel instance;
        public static bool Available=>NetworkBootstrap.Instance!=null && NetworkBootstrap.Instance.IsTestEnvironment && instance!=null && GameConfig.ShowTemporaryServerRestart && !Application.isBatchMode && instance.projectRoot!=null &&
            (NetworkManager.Singleton==null || !NetworkManager.Singleton.IsServer) && instance.IsLocalConnection();
        public static bool ContainsScreenPoint(Vector2 point)=>false;
        void Awake() { instance=this; }
        string projectRoot, message = "";
        bool requested;
        System.Diagnostics.Process restartProcess;
        readonly HashSet<string> localAddresses = new HashSet<string>();
        void Start()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            var folder = new DirectoryInfo(Application.dataPath);
            for(int i=0;i<6 && folder!=null;i++,folder=folder.Parent)
                if(File.Exists(Path.Combine(folder.FullName,"Restart-Server.ps1")) && File.Exists(Path.Combine(folder.FullName,"Start-Server.ps1")))
                { projectRoot=folder.FullName; break; }
            try { foreach(var address in Dns.GetHostAddresses(Dns.GetHostName())) localAddresses.Add(address.ToString()); }
            catch { }
#endif
        }
        bool IsLocalConnection()
        {
            var manager=NetworkManager.Singleton;
            if(manager==null || !manager.IsConnectedClient) return true;
            var transport=manager.GetComponent<UnityTransport>();
            string address=transport!=null ? transport.ConnectionData.Address : "";
            if(string.Equals(address,"localhost",StringComparison.OrdinalIgnoreCase)) return true;
            if(!IPAddress.TryParse(address,out var target)) return false;
            if(IPAddress.IsLoopback(target)) return true;
            return localAddresses.Contains(target.ToString());
        }
        void Update()
        {
            if(restartProcess==null || !restartProcess.HasExited) return;
            bool success=restartProcess.ExitCode==0;
            restartProcess.Dispose(); restartProcess=null; requested=false;
            message=success ? "Servidor reiniciado. Voce pode entrar novamente." : "Falha no reinicio. Consulte Logs/server-restart.log.";
            PlayerPrefs.SetString("connection_message",message);
        }
        public static void DrawControl(Rect rect)
        {
            if(!Available) return;
            bool before=GUI.enabled; GUI.enabled=!instance.requested;
            if(GUI.Button(rect,"Reiniciar servidor · PC")) instance.Restart();
            GUI.enabled=before;
        }
        void Restart()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            try
            {
                string script=Path.Combine(projectRoot,"Restart-Server.ps1");
                var info=new System.Diagnostics.ProcessStartInfo("powershell.exe",
                    "-NoProfile -ExecutionPolicy Bypass -File \""+script+"\"")
                { WorkingDirectory=projectRoot,UseShellExecute=false,CreateNoWindow=true,WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden };
                restartProcess=System.Diagnostics.Process.Start(info);
                if(restartProcess==null) throw new InvalidOperationException("Nao foi possivel iniciar o reinicio.");
                requested=true;
                message="Reiniciando o servidor local. Aguarde a atualizacao antes de entrar novamente.";
                PlayerPrefs.SetString("connection_message",message);
                NetworkBootstrap.Instance?.PrepareForLocalServerRestart();
            }
            catch(Exception error) { message="Falha ao reiniciar: "+error.Message; }
#endif
        }
        void OnDestroy() { restartProcess?.Dispose(); if(instance==this) instance=null; }
    }
}
