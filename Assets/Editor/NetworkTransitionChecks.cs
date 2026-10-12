using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using CyberExtraction.Camp;
using CyberExtraction.UI;

// Regression checks for the reported TransportShutdown message only.
public static class NetworkTransitionChecks
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int passed;
    static void Check(bool ok,string description)
    { if(!ok) throw new Exception("NETWORK TRANSITION FAIL: "+description); passed++; }

    [MenuItem("Cyber Extraction/Verificar transicao de conexao")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do Play para verificar isoladamente.");
        passed=0;
        var friendly=typeof(NetworkBootstrap).GetMethod("FriendlyReason",BindingFlags.Static|BindingFlags.NonPublic);
        string Friendly(string value)=>(string)friendly.Invoke(null,new object[]{value});
        Check(Friendly("")=="","Mensagem vazia preservada");
        Check(Friendly(null)=="","Mensagem nula segura");
        Check(Friendly("Local[TransportShutdown] NetworkConnectionManager was shutdown.")=="Conexao encerrada. Tente conectar novamente.","Diagnostico interno traduzido");
        Check(Friendly("INVALID|Perfil recusado.")=="Perfil recusado.","Recusa do servidor preservada");
        Check(Friendly("ROOM_BUSY|Sala ocupada.")=="Sala ocupada.","Sala ocupada preservada");
        bool synchronized=CampSession.Synchronized,pending=CampSession.Pending;
        var go=new GameObject("NetworkTransitionFixture");
        try
        {
            var manager=go.AddComponent<NetworkManager>();
            var bootstrap=go.AddComponent<NetworkBootstrap>();
            void Set(string field,object value)=>typeof(NetworkBootstrap).GetField(field,Private).SetValue(bootstrap,value);
            object Get(string field)=>typeof(NetworkBootstrap).GetField(field,Private).GetValue(bootstrap);
            Set("manager",manager);
            var callback=typeof(NetworkBootstrap).GetMethod("Disconnected",Private);
            Set("intentionalDisconnect",true); Set("message","Mensagem anterior"); Set("attemptReason","");
            callback.Invoke(bootstrap,new object[]{manager.LocalClientId});
            Check((string)Get("message")=="Mensagem anterior","Shutdown intencional nao sobrescreve mensagem");
            Check((string)Get("attemptReason")=="","Shutdown intencional nao cria falsa recusa");
            Check(!(bool)Get("reconnectMenu"),"Shutdown intencional nao solicita recarregar cena");
            var transport=go.AddComponent<UnityTransport>();
            manager.NetworkConfig.NetworkTransport=transport;
            var setTransportEvent=typeof(NetworkTransport).GetMethod("SetDisconnectEvent",Private);
            Set("intentionalDisconnect",false);
            setTransportEvent.Invoke(transport,new object[]{NetworkTransport.DisconnectEvents.TransportShutdown,"Fixture cleanup"});
            callback.Invoke(bootstrap,new object[]{manager.LocalClientId});
            Check((string)Get("message")=="Mensagem anterior","Shutdown interno do NGO nao vira erro");
            Check((string)Get("attemptReason")=="","Shutdown interno nao cria falsa recusa");
            Check(!(bool)Get("reconnectMenu"),"Shutdown interno nao solicita recarregar cena");
            // Use the real NGO connection-manager reason consumed by the callback.
            var connection=typeof(NetworkManager).GetField("ConnectionManager",Private).GetValue(manager);
            connection.GetType().GetField("m_DisconnectReason",Private).SetValue(connection,"INVALID|Perfil recusado.");
            setTransportEvent.Invoke(transport,new object[]{NetworkTransport.DisconnectEvents.ClosedRemoteConnection,"Fixture remote disconnect"});
            Set("intentionalDisconnect",false); Set("attemptEnded",false);
            callback.Invoke(bootstrap,new object[]{manager.LocalClientId});
            Check((string)Get("message")=="Perfil recusado.","Falha real continua visivel");
            Check((string)Get("attemptReason")=="INVALID|Perfil recusado.","Motivo original preservado para busca de salas");
            Check((bool)Get("attemptEnded"),"Falha real encerra tentativa");
            Check((bool)Get("reconnectMenu"),"Falha real permite reconectar");
            Set("reconnectMenu",false);
            setTransportEvent.Invoke(transport,new object[]{NetworkTransport.DisconnectEvents.TransportShutdown,"Fixture cleanup after failure"});
            callback.Invoke(bootstrap,new object[]{manager.LocalClientId});
            Check((string)Get("message")=="Perfil recusado.","Cleanup posterior preserva erro real anterior");
            Check((string)Get("attemptReason")=="INVALID|Perfil recusado.","Cleanup posterior preserva causa original");
            Check(!(bool)Get("reconnectMenu"),"Cleanup posterior nao cria novo pedido de recarregamento");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
            CampSession.Synchronized=synchronized; CampSession.Pending=pending;
        }
        Debug.Log("NETWORK TRANSITION CHECKS PASS: "+passed+" focused checks; no sockets or live-room restart.");
    }
}
