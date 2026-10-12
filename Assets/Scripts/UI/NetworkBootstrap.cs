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
        bool reconnectMenu, previewOnly;
        bool searching, attemptEnded, cancelSearch;
        bool intentionalDisconnect, changingConnection;
        public bool Searching => searching;
        public bool ReadyForMenus => manager!=null && !manager.IsServer && !Application.isBatchMode && !previewOnly;
        public bool ConnectionBusy => searching || changingConnection;
        public bool ConnectionUnavailable => ReadyForMenus && ClientEnvironment.Chosen && !ConnectionBusy && !manager.IsConnectedClient;
        public string ConnectionMessage
        {
            get
            {
                string value=FriendlyReason(string.IsNullOrEmpty(message) ? PlayerPrefs.GetString("connection_message","") : message);
                return string.IsNullOrEmpty(value) && ConnectionUnavailable ? "Não foi possível conectar ao servidor. Tente novamente." : value;
            }
        }
        public bool TestEnvironmentSelected => ClientEnvironment.Selected==CampEnvironment.Test;
        public bool IsTestEnvironment => GameConfig.AllowQuickTest && manager!=null && manager.IsConnectedClient &&
            (CampConnection ? CampSession.Synchronized && CampView.TestMode : MatchManager.Instance!=null && MatchManager.Instance.Sandbox);
        public bool InTestCamp => CampConnection && IsTestEnvironment;
        public ExpeditionMap TestMap => selectedMap;
        public int TestBots => quickBots;
        string attemptReason, clusterId;
        readonly string instanceId = Guid.NewGuid().ToString("N");
        double nextLeaseRefresh;
        CampNetwork campNetwork;
        bool campRegistered;
        bool campSandbox;
        float nextEffectSync;
        [Serializable] class EffectSnapshot { public CyberExtraction.Enemies.MonsterEffect[] Effects; }
        public bool CampConnection { get; private set; }
        public CampSnapshot CampView => campNetwork != null ? campNetwork.Snapshot : new CampSnapshot();
        public bool IsCampClient(ulong client) => requests.TryGetValue(client,out var join) && join.CampOnly;
        public void SendCampSnapshot(ulong client)=>campNetwork.Sync(client);
        public bool SendCamp(CampCommand command) => campNetwork != null && campNetwork.Request(command);
        ExpeditionMap selectedMap = ExpeditionMap.Large;
        bool quickTest;
        int quickBots = 1;
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
            gameObject.AddComponent<ServerVisibility>();
            gameObject.AddComponent<GameHelp>();
            gameObject.AddComponent<GameOptionsMenu>();
            gameObject.AddComponent<TestLabPanel>();
            gameObject.AddComponent<LockpickWindow>();
            gameObject.AddComponent<CampTestPanel>();
            gameObject.AddComponent<CyberExtraction.Diagnostics.MatchTelemetry>();
            gameObject.AddComponent<Quickbar>();
            gameObject.AddComponent<InventoryWindow>();
            gameObject.AddComponent<Minimap>();
            gameObject.AddComponent<ServerDebugPanel>();
            manager = NetworkManager.Singleton;
            if (manager == null) { Debug.LogError("NetworkManager nao encontrado."); return; }
            Register(playerPrefab); Register(monsterPrefab); Register(lootPrefab);
            Register(matchManagerPrefab); Register(botPrefab);
            Register(CyberExtraction.Enemies.PlantEncounter.Prefab);
            Register(CyberExtraction.Enemies.SporeEncounter.Prefab);
            Register(Resources.Load<GameObject>("Spores/SporeCloud"));
            manager.NetworkConfig.PlayerPrefab = playerPrefab;
            manager.NetworkConfig.ProtocolVersion = 16;
            campNetwork=new CampNetwork(manager,this);
            manager.NetworkConfig.ConnectionApproval = true;
            manager.ConnectionApprovalCallback = ApproveConnection;
            manager.OnClientDisconnectCallback += Disconnected;
            manager.OnClientConnectedCallback += Connected;
            var connectionSettings=ServerConnectionSettings.Load();
            ip=connectionSettings!=null && !string.IsNullOrWhiteSpace(connectionSettings.address) ? connectionSettings.address.Trim() : "127.0.0.1";
            port=(ushort)Mathf.Clamp(connectionSettings!=null ? connectionSettings.basePort : 7777,1,65533);
            gameObject.AddComponent<ExpeditionMenu>();
            if(!GameConfig.AllowQuickTest && !ClientEnvironment.Chosen) ClientEnvironment.Select(CampEnvironment.Normal);
            ConfigureEnvironment();
            string[] args = Environment.GetCommandLineArgs();
            bool smokeTest = Array.IndexOf(args, "-smoke-test") >= 0;
            bool itemsTest = Array.IndexOf(args, "-items-smoke-test") >= 0 || Array.IndexOf(args,"-skills-smoke")>=0;
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
                if (Array.IndexOf(args, "-items-smoke-server") >= 0 || Array.IndexOf(args,"-skills-smoke-server")>=0) gameObject.AddComponent<CyberExtraction.Diagnostics.SkillsSmokeServer>();
            }
            else if (itemsTest)
            {
                ClientEnvironment.Select(CampEnvironment.Normal); ConfigureEnvironment();
                gameObject.AddComponent<CampController>();
                gameObject.AddComponent<CyberExtraction.Diagnostics.SkillsSmokeClient>();
                BeginCamp("127.0.0.1");
            }
            else if (smokeTest)
            {
                ClientEnvironment.Select(CampEnvironment.Normal); ConfigureEnvironment();
                manager.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", port);
                gameObject.AddComponent<CyberExtraction.Diagnostics.ServerSmokeTest>();
                PrepareJoin();
                if(manager.StartClient()) RegisterCamp();
            }
            else if(Array.IndexOf(args,"-lab-smoke")>=0)
            {
                ClientEnvironment.Select(CampEnvironment.Test); ConfigureEnvironment();
                selectedMap=Array.IndexOf(args,"-lab-large")>=0 ? ExpeditionMap.Large : ExpeditionMap.Test; quickTest=true; quickBots=0;
                gameObject.AddComponent<CampController>();
                gameObject.AddComponent<CyberExtraction.Diagnostics.TestLabSmoke>(); BeginCamp("127.0.0.1");
            }
            else
            {
                gameObject.AddComponent<CampController>();
                if (Array.IndexOf(args, "-camp-preview") >= 0)
                { previewOnly=true; ClientEnvironment.Select(CampEnvironment.Normal); ConfigureEnvironment(); gameObject.AddComponent<CyberExtraction.Diagnostics.CampPreview>(); }
                else if(ClientEnvironment.Chosen)
                {
                    CampConnection=true;
                    if(string.IsNullOrEmpty(PlayerPrefs.GetString("connection_message",""))) BeginCamp(ip);
                }
            }
        }

        void Register(GameObject prefab)
        {
            if (prefab != null && !manager.NetworkConfig.Prefabs.Contains(prefab)) manager.AddNetworkPrefab(prefab);
        }

        void RegisterCamp()
        {
            if(campRegistered) return;
            campNetwork.Register();
            manager.CustomMessagingManager.RegisterNamedMessageHandler("ce-shot",ReceiveShot);
            manager.CustomMessagingManager.RegisterNamedMessageHandler("ce-effects",ReceiveEffects);
            campRegistered=true;
        }
        void ReceiveEffects(ulong sender,FastBufferReader reader)
        {
            if(!manager.IsClient || sender!=NetworkManager.ServerClientId || reader.Length>32768) return;
            reader.ReadValueSafe(out string json); var data=JsonUtility.FromJson<EffectSnapshot>(json); var mm=MatchManager.Instance;
            if(mm==null || data==null) return; mm.VisibleEffects.Clear(); if(data.Effects!=null) mm.VisibleEffects.AddRange(data.Effects);
        }
        void BroadcastEffects()
        {
            var mm=MatchManager.Instance; if(!manager.IsServer || mm==null || Time.unscaledTime<nextEffectSync) return;
            nextEffectSync=Time.unscaledTime+.15f;
            foreach(var client in manager.ConnectedClientsList)
            {
                if(client.PlayerObject==null || IsCampClient(client.ClientId)) continue;
                var player=client.PlayerObject; var visible=new List<CyberExtraction.Enemies.MonsterEffect>();
                foreach(var e in mm.MonsterEffects)
                {
                    Vector2 point=e.Kind==CyberExtraction.Enemies.MonsterEffectKind.WebShot || e.Kind==CyberExtraction.Enemies.MonsterEffectKind.AcidShot
                        ? Vector2.Lerp(e.From,e.To,(float)((manager.ServerTime.Time-e.Start)/Math.Max(.001,e.End-e.Start))) : e.To;
                    if(ServerVisibility.CanSee(player.transform.position,player.transform.eulerAngles.z,point))
                    {
                        var view=e;
                        if(e.Kind==CyberExtraction.Enemies.MonsterEffectKind.WebShot || e.Kind==CyberExtraction.Enemies.MonsterEffectKind.AcidShot)
                        { view.From=point; view.Start=manager.ServerTime.Time; view.End=view.Start+.3; view.To=point+(e.To-e.From)/(float)Math.Max(.001,e.End-e.Start)*.3f; }
                        visible.Add(view);
                    }
                }
                string json=JsonUtility.ToJson(new EffectSnapshot { Effects=visible.ToArray() });
                using(var writer=new FastBufferWriter(json.Length*2+8,Unity.Collections.Allocator.Temp))
                { writer.WriteValueSafe(json); manager.CustomMessagingManager.SendNamedMessage("ce-effects",client.ClientId,writer,NetworkDelivery.ReliableFragmentedSequenced); }
            }
        }
        void ReceiveShot(ulong sender,FastBufferReader reader)
        {
            if(!manager.IsClient || sender!=NetworkManager.ServerClientId) return;
            reader.ReadValueSafe(out Vector2 from); reader.ReadValueSafe(out Vector2 to); reader.ReadValueSafe(out bool visual);
            reader.ReadValueSafe(out bool sound); reader.ReadValueSafe(out float noiseRadius);
            CyberExtraction.Combat.ShotPresentation.Play(from,to,visual,sound,noiseRadius);
        }
        public void BroadcastShot(Vector2 from,Vector2 to,bool sound=true,float noiseRadius=GameConfig.ShotNoiseRadius)
        {
            if(!manager.IsServer) return;
            foreach(var client in manager.ConnectedClientsList)
            {
                if(client.PlayerObject==null || IsCampClient(client.ClientId)) continue;
                var player=client.PlayerObject;
                bool visual=ServerVisibility.CanSee(player.transform.position,player.transform.eulerAngles.z,from);
                if(!visual && (!sound || Vector2.Distance(from,player.transform.position)>noiseRadius)) continue;
                // Hidden shooters emit a coarse sound cue, without endpoint or object identity.
                var origin=visual ? from : new Vector2(Mathf.Round(from.x/2)*2,Mathf.Round(from.y/2)*2);
                using(var writer=new FastBufferWriter(48,Unity.Collections.Allocator.Temp))
                {
                    writer.WriteValueSafe(origin); writer.WriteValueSafe(visual ? to : origin); writer.WriteValueSafe(visual);
                    writer.WriteValueSafe(sound); writer.WriteValueSafe(noiseRadius);
                    manager.CustomMessagingManager.SendNamedMessage("ce-shot",client.ClientId,writer,NetworkDelivery.Unreliable);
                }
            }
        }
        void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved=false; response.CreatePlayerObject=false;
            CampJoinRequest join;
            try
            {
                if(request.Payload==null || request.Payload.Length>4096) throw new FormatException();
                join=JsonUtility.FromJson<CampJoinRequest>(Encoding.UTF8.GetString(request.Payload));
                if(join==null || join.Map>(byte)ExpeditionMap.Large || join.QuickBots<0 || join.QuickBots>=GameConfig.MatchCapacity || !CampRepository.Authenticate(join)) throw new FormatException();
            }
            catch { response.Reason="INVALID|Identidade ou versao invalida. Atualize o cliente e confira o relogio do aparelho."; return; }
            if(join.CampSandbox && (!join.CampOnly || !GameConfig.AllowQuickTest)) { response.Reason="INVALID|Acampamento de teste desativado neste servidor."; return; }
            var mm=MatchManager.Instance;
            if(mm==null) { response.Reason="ROOM_BUSY|Sala iniciando."; return; }
            int slot=0;
            if(!join.CampOnly)
            {
                if(!join.QuickTest && join.Map!=(byte)ExpeditionMap.Large)
                { response.Reason="INVALID|A expedicao normal disponivel e Floresta Velha. Atualize o cliente."; return; }
                if(mm.CurrentState!=MatchState.Waiting) { response.Reason="ROOM_BUSY|Partida em andamento."; return; }
                int capacity=mm.QuickTest.Value ? 1 : GameConfig.MatchCapacity;
                while(slot<capacity && slots.ContainsValue(slot)) slot++;
                if(slot==capacity) { response.Reason="ROOM_FULL|Sala cheia."; return; }
                if(slots.Count>0 && (mm.MapSelection.Value!=join.Map || mm.QuickTest.Value!=join.QuickTest || join.QuickTest && mm.QuickBots.Value!=join.QuickBots))
                { response.Reason="MODE_MISMATCH|Sala usa outro mapa ou modo."; return; }
                if(!GameConfig.AllowQuickTest && join.QuickTest) { response.Reason="INVALID|Modo de teste desativado neste servidor."; return; }
            }
            foreach(var existing in requests.Values) if(existing.Session==join.Session)
            { response.Reason="DUPLICATE|Seu acampamento ja esta conectado."; return; }
            try
            {
                if(!CampRepository.Claim(join.Session,LeaseOwner(request.ClientNetworkId)))
                { response.Reason="DUPLICATE|Seu acampamento esta conectado em outra sala."; return; }
            }
            catch(Exception error) { Debug.LogError("Acampamento: "+error.Message); response.Reason="INVALID|Falha ao carregar progresso."; return; }
            requests[request.ClientNetworkId]=join;
            if(join.CampOnly) { response.Approved=true; return; }
            if(slots.Count==0)
            {
                mm.QuickTest.Value=join.QuickTest; mm.QuickBots.Value=join.QuickBots; mm.MapSelection.Value=join.Map;
                MapBuilder.Configure((ExpeditionMap)join.Map,mm.LayoutSeed.Value);
            }
            slots[request.ClientNetworkId]=slot;
            response.Position=MapBuilder.ParticipantSpawn(slot); response.Rotation=Quaternion.Euler(0,0,MapBuilder.ParticipantFacing(slot));
            response.CreatePlayerObject=true; response.Approved=true;
        }

        void Connected(ulong client)
        {
            if (!manager.IsServer) { if(CampConnection) SendCamp(new CampCommand { Kind=CampCommandKind.Sync }); return; }
            if(IsCampClient(client)) { campNetwork.Sync(client); return; }
            // Esta notificacao so chega depois que o cliente sincronizou a cena.
            if (MatchManager.Instance != null && MatchManager.Instance.CurrentState == MatchState.Waiting)
                readyClients.Add(client);
            else
                manager.DisconnectClient(client, "ROOM_BUSY|A partida ja comecou. Inicie uma nova busca.");
        }

        void Disconnected(ulong client)
        {
            if (manager.IsServer && requests.TryGetValue(client, out var leaving))
            {
                if(!leaving.CampOnly && !leaving.QuickTest)
                {
                    var profile=CampRepository.Get(leaving.Session);
                    if(profile.ExpeditionState==2) CampRepository.Lose(leaving.Session,profile.ExpeditionId);
                    else if(profile.ExpeditionState==1) CampRepository.Rollback(leaving.Session,profile.ExpeditionId);
                }
                CampRepository.Release(leaving.Session, LeaseOwner(client));
            }
            campNetwork?.Remove(client);
            slots.Remove(client);
            readyClients.Remove(client);
            requests.Remove(client);
            if (manager.IsServer) return;
            if (client == manager.LocalClientId)
            {
                CampSession.Disconnected();
                // NGO also shuts down internally (stop Play, scene unload, or
                // cleanup after a prior failure). Its typed shutdown event is
                // cleanup, even when it did not pass through our coroutine.
                var transport=manager.NetworkConfig?.NetworkTransport;
                if(intentionalDisconnect || transport!=null &&
                    transport.DisconnectEvent==NetworkTransport.DisconnectEvents.TransportShutdown)
                { attemptEnded=true; return; }
                attemptReason = manager.DisconnectReason;
                attemptEnded = true;
                if(!string.IsNullOrEmpty(attemptReason)) Debug.LogWarning("Conexao encerrada: "+attemptReason);
                message = string.IsNullOrEmpty(attemptReason)
                    ? "Conexão encerrada. Tente novamente."
                    : FriendlyReason(attemptReason);
                reconnectMenu = !searching && !CampConnection;
            }
        }

        void Update()
        {
            if(manager!=null && !manager.IsListening) campRegistered=false;
            campNetwork?.Tick();
            if(manager!=null && manager.IsListening) BroadcastEffects();
            if (manager != null && manager.IsServer && Time.realtimeSinceStartupAsDouble >= nextLeaseRefresh)
            {
                nextLeaseRefresh = Time.realtimeSinceStartupAsDouble + 20;
                foreach (var entry in requests) CampRepository.Refresh(entry.Value.Session, LeaseOwner(entry.Key));
            }
            if (searching || changingConnection) return;
            if (manager == null || !reconnectMenu || manager.IsListening || manager.ShutdownInProgress ||
                GetComponent<CyberExtraction.Diagnostics.ServerSmokeTest>() != null ||
                GetComponent<CyberExtraction.Diagnostics.ItemSmokeClient>() != null || GetComponent<CyberExtraction.Diagnostics.SkillsSmokeClient>() != null || GetComponent<CyberExtraction.Diagnostics.TestLabSmoke>() != null) return;
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
            if(manager!=null && manager.IsServer) CampRepository.Close();
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
            string dataDirectory=null;
            var serverArgs=Environment.GetCommandLineArgs(); int dataIndex=Array.IndexOf(serverArgs,"-camp-data");
            if(dataIndex>=0 && dataIndex+1<serverArgs.Length) dataDirectory=serverArgs[dataIndex+1];
            CampRepository.EnableShared(string.IsNullOrEmpty(clusterId) ? "local-"+port : clusterId,dataDirectory);
            manager.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", port, "0.0.0.0");
            if (!manager.StartServer()) { Debug.LogError("Nao foi possivel iniciar o servidor."); return; }
            RegisterCamp();
            var match = Instantiate(matchManagerPrefab);
            match.GetComponent<MatchManager>().botPrefab = botPrefab;
            match.GetComponent<NetworkObject>().Spawn();
            Debug.Log($"Sala {RoomNumber} pronta em UDP {port}. Capacidade: {GameConfig.MatchCapacity}. Espera: {GameConfig.MatchmakingSeconds}s.");
            int readyIndex=Array.IndexOf(serverArgs,"-ready-file");
            if(readyIndex>=0 && readyIndex+1<serverArgs.Length) System.IO.File.WriteAllText(serverArgs[readyIndex+1],System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
        }

        void ConfigureEnvironment()
        {
            campSandbox=TestEnvironmentSelected && GameConfig.AllowQuickTest;
            quickTest=campSandbox;
            selectedMap=campSandbox ? (ExpeditionMap)Mathf.Clamp(PlayerPrefs.GetInt("test_expedition_map",0),0,1) : ExpeditionMap.Large;
            quickBots=campSandbox ? Mathf.Clamp(PlayerPrefs.GetInt("test_quick_bots",1),0,GameConfig.MatchCapacity-1) : 0;
        }
        void SaveTestPreferences()
        {
            if(!TestEnvironmentSelected) return;
            PlayerPrefs.SetInt("test_expedition_map",(int)selectedMap);
            PlayerPrefs.SetInt("test_quick_bots",quickBots);
        }
        public void ChooseEnvironment(CampEnvironment environment)
        {
            if(!ReadyForMenus || ClientEnvironment.Chosen || ConnectionBusy || manager.IsListening ||
                environment==CampEnvironment.None || environment==CampEnvironment.Test && !GameConfig.AllowQuickTest) return;
            ClientEnvironment.Select(environment); ConfigureEnvironment();
            PlayerPrefs.DeleteKey("connection_message"); message="";
            BeginCamp(ip);
        }
        public void ConfigureTestExpedition(ExpeditionMap map,int bots)
        {
            if(!InTestCamp || ConnectionBusy) return;
            var nextMap=map==ExpeditionMap.Large ? ExpeditionMap.Large : ExpeditionMap.Test;
            int nextBots=Mathf.Clamp(bots,0,GameConfig.MatchCapacity-1);
            if(selectedMap==nextMap && quickBots==nextBots) return;
            selectedMap=nextMap; quickBots=nextBots; SaveTestPreferences();
        }
        public void BeginExpedition() { BeginExpedition(ip); }
        public void CancelConnectionSearch() { if(searching) cancelSearch=true; }
        public void RetryConnection()
        {
            if(!ReadyForMenus || ConnectionBusy || manager.IsListening || manager.ShutdownInProgress) return;
            PlayerPrefs.DeleteKey("connection_message"); message="";
            // Reload confirmed camp state before another expedition attempt.
            CampConnection=true; BeginCamp(ip);
        }
        public void ReturnToEnvironmentSelection()
        {
            if(ConnectionBusy) { cancelSearch=true; return; }
            SaveTestPreferences(); ClientEnvironment.ReturnToStart();
            PlayerPrefs.DeleteKey("connection_message"); message="";
            if(manager!=null) manager.Shutdown();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void PrepareJoin()
        {
            bool testing=TestEnvironmentSelected && GameConfig.AllowQuickTest;
            var request = CampSession.Request(); request.CampOnly=CampConnection; request.CampSandbox=CampConnection && campSandbox && testing;
            request.Map=(byte)(testing ? selectedMap : ExpeditionMap.Large); request.QuickTest=!CampConnection && testing && quickTest;
            request.QuickBots=testing ? quickBots : 0;
            CampIdentity.Sign(request);
            manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(request));
        }

        public void BeginCamp(string address)
        {
            if(searching || changingConnection || manager.IsListening || manager.ShutdownInProgress) return;
            CampConnection=true; CampSession.Disconnected(); StartCoroutine(FindRoom(address));
        }
        public void BeginExpedition(string address)
        {
            if(searching || changingConnection || CampSession.Pending) return;
            if(!CampSession.Synchronized || !manager.IsConnectedClient || !CampConnection) return;
            if(TestEnvironmentSelected!=CampView.TestMode)
            { message="O ambiente recebido do servidor não corresponde ao escolhido. Volte e entre novamente."; return; }
            quickTest=IsTestEnvironment;
            if(!quickTest) { selectedMap=ExpeditionMap.Large; quickBots=0; }
            StartCoroutine(ChangeToExpedition(address));
        }
        IEnumerator ChangeToExpedition(string address)
        {
            changingConnection=true;
            try
            {
                if(manager.IsListening)
                {
                    if(!CampConnection) yield break;
                    yield return StopClientConnection();
                }
                else if(manager.ShutdownInProgress) yield return StopClientConnection();
                CampConnection=false; campRegistered=false;
                yield return FindRoom(address);
            }
            finally { changingConnection=false; }
        }

        IEnumerator StopClientConnection()
        {
            intentionalDisconnect=true;
            try
            {
                manager.Shutdown();
                while(manager.IsListening || manager.ShutdownInProgress) yield return null;
            }
            finally { intentionalDisconnect=false; campRegistered=false; }
        }

        public void PrepareForLocalServerRestart()
        {
            cancelSearch = true;
            SaveTestPreferences();
            PlayerPrefs.Save();
            if(manager != null && manager.IsClient) StartCoroutine(StopClientConnection());
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
                    RegisterCamp();
                    double deadline = Time.realtimeSinceStartupAsDouble + 7;
                    while (!cancelSearch && !attemptEnded && !manager.IsConnectedClient && manager.IsListening &&
                        Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                    if (manager.IsConnectedClient && !cancelSearch)
                    {
                        // Aguarda uma eventual recusa tardia apos a sincronizacao da cena.
                        yield return new WaitForSecondsRealtime(0.2f);
                        if (manager.IsConnectedClient && !attemptEnded && !cancelSearch)
                        { searching = false; reconnectMenu = false; message = ""; PlayerPrefs.DeleteKey("connection_message"); yield break; }
                    }
                }
                reachedRoom |= !string.IsNullOrEmpty(attemptReason);
                string reason = attemptReason;
                yield return StopClientConnection();
                if (reason != null && (reason.StartsWith("INVALID|") || reason.StartsWith("DUPLICATE|")))
                { message = FriendlyReason(reason); break; }
                message = reachedRoom ? "Não há sala disponível para esta expedição. Tente novamente em instantes." : "Não foi possível conectar ao servidor. Tente novamente.";
            }
            if (cancelSearch) message = "Busca cancelada.";
            searching = false; reconnectMenu = false;
            if (CampController.Instance != null) CampController.Instance.FocusCamera();
        }

        string LeaseOwner(ulong client) => instanceId + ":" + client;
        static string FriendlyReason(string reason)
        {
            if(string.IsNullOrEmpty(reason)) return "";
            if(reason.IndexOf("TransportShutdown",StringComparison.OrdinalIgnoreCase)>=0 ||
                reason.IndexOf("NetworkConnectionManager was shutdown",StringComparison.OrdinalIgnoreCase)>=0)
                return "Conexao encerrada. Tente conectar novamente.";
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
