using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using CyberExtraction.Bots;
using CyberExtraction.Core;
using CyberExtraction.Match;
using CyberExtraction.Camp;

namespace CyberExtraction.UI
{
    public class NetworkBootstrap : MonoBehaviour
    {
        public static NetworkBootstrap Instance { get; private set; }
        public GameObject playerPrefab, monsterPrefab, lootPrefab, matchManagerPrefab, botPrefab;
        readonly Dictionary<ulong, int> slots = new Dictionary<ulong, int>();
        readonly HashSet<ulong> readyClients = new HashSet<ulong>();
        readonly Dictionary<ulong, CampJoinRequest> requests = new Dictionary<ulong, CampJoinRequest>();
        NetworkManager manager;
        ushort port = 7777;
        string ip = "127.0.0.1";
        string message = "";
        bool reconnectMenu;
        bool searching, attemptEnded, cancelSearch;
        string attemptReason, clusterId;
        readonly string instanceId = Guid.NewGuid().ToString("N");
        double nextLeaseRefresh;
        public int RoomNumber { get; private set; } = 1;
        static string cachedIp;
        public static string LocalIp => cachedIp ?? (cachedIp = DetectIp());
        public int SlotFor(ulong client) => slots.TryGetValue(client, out int slot) ? slot : -1;
        public bool ClientReady(ulong client) => readyClients.Contains(client);
        public CampJoinRequest RequestFor(ulong client) => requests.TryGetValue(client, out var request) ? request : null;

        void Start()
        {
            Instance = this;
            Application.runInBackground = true;
            gameObject.AddComponent<Quickbar>();
            manager = NetworkManager.Singleton;
            if (manager == null) { Debug.LogError("NetworkManager nao encontrado."); return; }
            Register(playerPrefab); Register(monsterPrefab); Register(lootPrefab);
            Register(matchManagerPrefab); Register(botPrefab);
            manager.NetworkConfig.PlayerPrefab = playerPrefab;
            manager.NetworkConfig.ProtocolVersion = 5;
            manager.NetworkConfig.ConnectionApproval = true;
            manager.ConnectionApprovalCallback = ApproveConnection;
            manager.OnClientDisconnectCallback += Disconnected;
            manager.OnClientConnectedCallback += Connected;
            ip = PlayerPrefs.GetString("server_ip", ip);
            string[] args = Environment.GetCommandLineArgs();
            bool smokeTest = Array.IndexOf(args, "-smoke-test") >= 0;
            bool itemsTest = Array.IndexOf(args, "-items-smoke-test") >= 0;
            bool server = (Application.isBatchMode && Array.IndexOf(args, "-client") < 0) || Array.IndexOf(args, "-server") >= 0;
#if UNITY_SERVER
            server = true;
#endif
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (args[i] == "-port" && ushort.TryParse(args[i + 1], out ushort configured) && configured > 0)
                    port = configured;
                if (args[i] == "-room" && int.TryParse(args[i + 1], out int room) && room >= 1 && room <= GameConfig.MaxConcurrentRooms)
                    RoomNumber = room;
                if (args[i] == "-cluster-id") clusterId = args[i + 1];
            }
            if (server)
            {
                StartDedicatedServer();
                if (Array.IndexOf(args, "-items-smoke-server") >= 0) gameObject.AddComponent<CyberExtraction.Diagnostics.ItemSmokeServer>();
            }
            else if (itemsTest)
            {
                gameObject.AddComponent<CyberExtraction.Diagnostics.ItemSmokeClient>();
                BeginExpedition("127.0.0.1");
            }
            else if (smokeTest)
            {
                manager.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", port);
                gameObject.AddComponent<CyberExtraction.Diagnostics.ServerSmokeTest>();
                PrepareJoin();
                manager.StartClient();
            }
            else
            {
                gameObject.AddComponent<CampController>();
                if (Array.IndexOf(args, "-camp-preview") >= 0) gameObject.AddComponent<CyberExtraction.Diagnostics.CampPreview>();
            }
        }

        void Register(GameObject prefab)
        {
            if (prefab != null && !manager.NetworkConfig.Prefabs.Contains(prefab)) manager.AddNetworkPrefab(prefab);
        }

        void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = false;
            response.CreatePlayerObject = false;
            var mm = MatchManager.Instance;
            if (mm == null || mm.CurrentState != MatchState.Waiting)
            { response.Reason = "ROOM_BUSY|Partida em andamento."; return; }
            int slot = 0;
            while (slot < GameConfig.MatchCapacity && slots.ContainsValue(slot)) slot++;
            if (slot == GameConfig.MatchCapacity)
            { response.Reason = "ROOM_FULL|Sala cheia."; return; }
            CampJoinRequest join;
            try
            {
                if (request.Payload == null || request.Payload.Length > 16000) throw new FormatException();
                join = JsonUtility.FromJson<CampJoinRequest>(Encoding.UTF8.GetString(request.Payload));
                if (join == null || !Guid.TryParseExact(join.Session, "N", out _)) throw new FormatException();
            }
            catch { response.Reason = "INVALID|Dados de entrada invalidos. Atualize o jogo."; return; }
            foreach (var existing in requests.Values)
                if (existing.Session == join.Session)
                { response.Reason = "DUPLICATE|Este acampamento ja esta em uma expedicao."; return; }
            try
            {
                if (!CampRepository.Claim(join.Session, LeaseOwner(request.ClientNetworkId)))
                { response.Reason = "DUPLICATE|Este acampamento ja esta em outra sala."; return; }
                CampRepository.Apply(join);
            }
            catch (Exception exception)
            {
                try { CampRepository.Release(join.Session, LeaseOwner(request.ClientNetworkId)); }
                catch (Exception releaseError) { Debug.LogError("Falha ao liberar acampamento: " + releaseError.Message); }
                Debug.LogError("Falha no acampamento: " + exception.Message);
                response.Reason = "INVALID|Nao foi possivel acessar o acampamento. Tente novamente.";
                return;
            }
            requests[request.ClientNetworkId] = join;
            slots[request.ClientNetworkId] = slot;
            response.Position = MapBuilder.ParticipantSpawn(slot);
            response.Rotation = Quaternion.Euler(0, 0, MapBuilder.ParticipantFacing(slot));
            response.CreatePlayerObject = true;
            response.Approved = true;
        }

        void Connected(ulong client)
        {
            if (!manager.IsServer) return;
            // Esta notificacao so chega depois que o cliente sincronizou a cena.
            if (MatchManager.Instance != null && MatchManager.Instance.CurrentState == MatchState.Waiting)
                readyClients.Add(client);
            else
                manager.DisconnectClient(client, "ROOM_BUSY|A partida ja comecou. Inicie uma nova busca.");
        }

        void Disconnected(ulong client)
        {
            if (manager.IsServer && requests.TryGetValue(client, out var leaving))
                CampRepository.Release(leaving.Session, LeaseOwner(client));
            slots.Remove(client);
            readyClients.Remove(client);
            requests.Remove(client);
            if (manager.IsServer) return;
            if (client == manager.LocalClientId)
            {
                attemptReason = manager.DisconnectReason;
                attemptEnded = true;
                message = string.IsNullOrEmpty(attemptReason)
                    ? "Conexao encerrada. Confira o endereco e se o servidor esta ligado."
                    : FriendlyReason(attemptReason);
                reconnectMenu = !searching;
            }
        }

        void Update()
        {
            if (manager != null && manager.IsServer && Time.realtimeSinceStartupAsDouble >= nextLeaseRefresh)
            {
                nextLeaseRefresh = Time.realtimeSinceStartupAsDouble + 20;
                foreach (var entry in requests) CampRepository.Refresh(entry.Value.Session, LeaseOwner(entry.Key));
            }
            if (searching) return;
            if (manager == null || !reconnectMenu || manager.IsListening || manager.ShutdownInProgress ||
                GetComponent<CyberExtraction.Diagnostics.ServerSmokeTest>() != null ||
                GetComponent<CyberExtraction.Diagnostics.ItemSmokeClient>() != null) return;
            PlayerPrefs.SetString("connection_message", message);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OnDestroy()
        {
            if (manager != null)
            {
                manager.OnClientDisconnectCallback -= Disconnected;
                manager.OnClientConnectedCallback -= Connected;
                manager.ConnectionApprovalCallback -= ApproveConnection;
            }
            if (Instance == this) Instance = null;
        }

        public void StartDedicatedServer()
        {
            if (manager.IsListening) return;
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            if (playerPrefab == null || matchManagerPrefab == null || botPrefab == null ||
                botPrefab.GetComponent<BotController>() == null)
            { Debug.LogError("Prefabs de servidor ausentes. Verifique a cena Game."); return; }
            if (!string.IsNullOrEmpty(clusterId)) CampRepository.EnableShared(clusterId);
            manager.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", port, "0.0.0.0");
            if (!manager.StartServer()) { Debug.LogError("Nao foi possivel iniciar o servidor."); return; }
            var match = Instantiate(matchManagerPrefab);
            match.GetComponent<MatchManager>().botPrefab = botPrefab;
            match.GetComponent<NetworkObject>().Spawn();
            Debug.Log($"Sala {RoomNumber} pronta em UDP {port}. Capacidade: {GameConfig.MatchCapacity}. Espera: {GameConfig.MatchmakingSeconds}s.");
        }

        void OnGUI()
        {
            if (manager == null || Application.isBatchMode) return;
            float scale = Mathf.Max(1f, Screen.height / 600f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;
            var label = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            var button = new GUIStyle(GUI.skin.button) { fontSize = 22 };
            float bw = Mathf.Min(420, width - 40), x = (width - bw) / 2;
            if (searching)
            {
                GUI.Label(new Rect(x, 160, bw, 90), "Procurando uma sala disponivel...", label);
                if (GUI.Button(new Rect(x, 260, bw, 50), "Cancelar busca", button)) cancelSearch = true;
                return;
            }
            if (manager.IsServer)
            {
                GUI.Label(new Rect(x, 80, bw, 100), $"Servidor ligado\nIP local: {LocalIp}\nPorta: {port}", label);
                return;
            }
            if (manager.IsListening)
            {
                if (!manager.IsConnectedClient) GUI.Label(new Rect(x, 180, bw, 80), "Conectando ao servidor...", label);
                return;
            }
            var camp = CampController.Instance;
            if (camp != null && !camp.ExpeditionPanelOpen) return;
            GUI.Box(new Rect(x - 15, 30, bw + 30, 390), "");
            GUI.Label(new Rect(x, 50, bw, 60), "CYBER EXTRACTION", new GUIStyle(label) { fontSize = 30 });
            GUI.Label(new Rect(x, 120, bw, 35), "Endereco do servidor:", label);
            ip = GUI.TextField(new Rect(x, 165, bw, 45), ip, 253, new GUIStyle(GUI.skin.textField) { fontSize = 22 });
            if (GUI.Button(new Rect(x, 230, bw, 60), "Iniciar partida", button))
            {
                string address = ip.Trim();
                if (address.Length == 0) { message = "Informe o endereco do servidor."; return; }
                PlayerPrefs.DeleteKey("connection_message");
                message = "";
                PlayerPrefs.SetString("server_ip", address);
                BeginExpedition(address);
            }
            GUI.Label(new Rect(x, 310, bw, 100), string.IsNullOrEmpty(message)
                ? PlayerPrefs.GetString("connection_message", "") : message, label);
            if (camp != null && GUI.Button(new Rect(x, 360, bw, 40), "Voltar ao acampamento", button))
                camp.ExpeditionPanelOpen = false;
#if UNITY_EDITOR
            if (GUI.Button(new Rect(x, 440, bw, 50), "Iniciar servidor de teste", button)) StartDedicatedServer();
#endif
        }

        void PrepareJoin() { manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(CampSession.Request())); }

        public void BeginExpedition(string address)
        {
            if (searching || manager.IsListening || manager.ShutdownInProgress) return;
            StartCoroutine(FindRoom(address));
        }

        IEnumerator FindRoom(string address)
        {
            searching = true; cancelSearch = false; reconnectMenu = false;
            bool reachedRoom = false;
            for (int room = 0; room < GameConfig.MaxConcurrentRooms && !cancelSearch; room++)
            {
                int candidatePort = port + room;
                if (candidatePort > ushort.MaxValue) break;
                attemptEnded = false; attemptReason = "";
                manager.GetComponent<UnityTransport>().SetConnectionData(address, (ushort)candidatePort);
                PrepareJoin();
                if (manager.StartClient())
                {
                    double deadline = Time.realtimeSinceStartupAsDouble + 7;
                    while (!cancelSearch && !attemptEnded && !manager.IsConnectedClient && manager.IsListening &&
                        Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                    if (manager.IsConnectedClient && !cancelSearch)
                    {
                        // Aguarda uma eventual recusa tardia apos a sincronizacao da cena.
                        yield return new WaitForSecondsRealtime(0.2f);
                        if (manager.IsConnectedClient && !attemptEnded && !cancelSearch)
                        { searching = false; reconnectMenu = false; message = ""; yield break; }
                    }
                }
                reachedRoom |= !string.IsNullOrEmpty(attemptReason);
                string reason = attemptReason;
                manager.Shutdown();
                while (manager.IsListening || manager.ShutdownInProgress) yield return null;
                if (reason != null && (reason.StartsWith("INVALID|") || reason.StartsWith("DUPLICATE|")))
                { message = FriendlyReason(reason); break; }
                message = reachedRoom ? "As salas estao ocupadas. Tente novamente em instantes." : "Servidor nao encontrado. Confira o endereco e se esta ligado.";
            }
            if (cancelSearch) message = "Busca cancelada.";
            searching = false; reconnectMenu = false;
            if (CampController.Instance != null) CampController.Instance.FocusCamera();
        }

        string LeaseOwner(ulong client) => instanceId + ":" + client;
        static string FriendlyReason(string reason)
        {
            int separator = reason.IndexOf('|');
            return separator >= 0 ? reason.Substring(separator + 1) : reason;
        }

        static string DetectIp()
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                { socket.Connect("8.8.8.8", 65530); return ((IPEndPoint)socket.LocalEndPoint).Address.ToString(); }
            }
            catch { }
            try
            {
                foreach (var address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address)) return address.ToString();
            }
            catch { }
            return "127.0.0.1";
        }
    }
}
