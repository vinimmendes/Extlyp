using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using CyberExtraction.Core;
using CyberExtraction.Combat;
using CyberExtraction.Enemies;
using CyberExtraction.Match;
using CyberExtraction.Players;
using CyberExtraction.UI;
using CyberExtraction.Bots;

// Menu: Cyber Extraction > Montar MVP (prefabs + cena)
// Cria os 5 prefabs de rede, a cena Game com tudo ligado e a coloca no Build Settings.
public static class MvpSetup
{
    const string PrefabDir = "Assets/Prefabs";
    const string ScenePath = "Assets/Scenes/Game.unity";

    [MenuItem("Cyber Extraction/Montar MVP (prefabs + cena)")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets", "Scenes");
        NameWallsLayer();
        CreateUnlitMaterial();
        CreateSpriteMaterial();

        var player = MakePlayer();
        var monster = MakeMonster();
        var sporeCloud = MakeSporeCloud();
        var esporifero = MakeEsporifero(sporeCloud);
        var loot = MakeLoot();
        var bot = MakeBot();
        var matchManager = MakeMatchManager(monster, loot, bot);
        var match = matchManager.GetComponent<MatchManager>();
        match.esporiferoPrefab = esporifero;
        EditorUtility.SetDirty(matchManager);
        BuildScene(player, monster, loot, matchManager, bot);
        var bootstrap = Object.FindFirstObjectByType<NetworkBootstrap>();
        bootstrap.esporiferoPrefab = esporifero;
        bootstrap.sporeCloudPrefab = sporeCloud;
        EditorSceneManager.SaveScene(bootstrap.gameObject.scene, ScenePath);

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Cyber Extraction", "Pronto! A cena Assets/Scenes/Game foi criada.\nGere o servidor pelo menu Cyber Extraction > Servidor. Veja SERVER_SETUP.md.", "OK");
    }

    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }

    static void NameWallsLayer()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;
        var tm = new SerializedObject(assets[0]);
        var layers = tm.FindProperty("layers");
        if (layers == null || layers.arraySize <= GameConfig.WallLayer) return;
        var l = layers.GetArrayElementAtIndex(GameConfig.WallLayer);
        if (string.IsNullOrEmpty(l.stringValue))
        {
            l.stringValue = "Walls";
            tm.ApplyModifiedProperties();
        }
    }

    static void CreateUnlitMaterial()
    {
        const string path = "Assets/Resources/UnlitSprite.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
        var sh = Shader.Find("Sprites/Default");
        if (sh == null) { Debug.LogError("Shader Sprites/Default nao encontrado."); return; }
        AssetDatabase.CreateAsset(new Material(sh), path);
    }

    [MenuItem("Cyber Extraction/Atualizar material dos sprites")]
    public static void UpdateSpriteMaterial()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Cyber Extraction", "Saia do Play antes de atualizar o material.", "OK");
            return;
        }
        EnsureFolder("Assets", "Resources");
        if (!CreateSpriteMaterial()) return;
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Cyber Extraction", "Material dos sprites atualizado.\nInicie novamente o Play nas duas janelas. A cena nao foi recriada.", "OK");
    }

    static bool CreateSpriteMaterial()
    {
        const string path = "Assets/Resources/UnlitSprite2D.mat";
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            Debug.LogError("Shader de sprites URP 2D nao encontrado. Verifique se o projeto usa o template Universal 2D.");
            return false;
        }
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) AssetDatabase.CreateAsset(new Material(shader), path);
        else if (material.shader != shader)
        {
            material.shader = shader;
            EditorUtility.SetDirty(material);
        }
        return true;
    }

    static GameObject SavePrefab(GameObject go, string name)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/" + name + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject MakePlayer()
    {
        var go = new GameObject("Player");
        go.AddComponent<NetworkObject>();
        var sv = go.AddComponent<ShapeVisual>();
        sv.shape = ShapeVisual.Shape.Circle;
        sv.color = Color.white;
        sv.sortingOrder = 5;
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        var nt = go.AddComponent<NetworkTransform>();
        nt.AuthorityMode = NetworkTransform.AuthorityModes.Server;

        var health = go.AddComponent<Health>();
        health.maxHealth = GameConfig.PlayerMaxHealth;
        go.AddComponent<PlayerInventory>();
        go.AddComponent<SporeContamination>();
        go.AddComponent<PlayerShooter>();
        go.AddComponent<PlayerController>();
        go.AddComponent<VisibilityTarget>();
        return SavePrefab(go, "Player");
    }

    static GameObject MakeMonster()
    {
        var go = new GameObject("Monster");
        go.AddComponent<NetworkObject>();
        var sv = go.AddComponent<ShapeVisual>();
        sv.shape = ShapeVisual.Shape.Circle;
        sv.color = new Color(0.85f, 0.25f, 0.25f);
        sv.sortingOrder = 5;
        go.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        go.AddComponent<NetworkTransform>();
        var health = go.AddComponent<Health>();
        health.maxHealth = 150;
        go.AddComponent<Monster>();
        go.AddComponent<VisibilityTarget>();
        return SavePrefab(go, "Monster");
    }

    static GameObject MakeLoot()
    {
        var go = new GameObject("Loot");
        go.AddComponent<NetworkObject>();
        var sv = go.AddComponent<ShapeVisual>();
        sv.shape = ShapeVisual.Shape.Square;
        sv.color = new Color(0.95f, 0.7f, 0.2f);
        sv.sortingOrder = 2;
        go.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 1.5f;
        col.isTrigger = true;
        go.AddComponent<LootPickup>();
        return SavePrefab(go, "Loot");
    }

    static GameObject MakeSporeCloud()
    {
        var go = new GameObject("SporeCloud");
        go.AddComponent<NetworkObject>();
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<SporeCloud>();
        return SavePrefab(go, "SporeCloud");
    }

    static GameObject MakeEsporifero(GameObject cloud)
    {
        var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Monster.prefab"));
        go.name = "Esporifero";
        var monster = go.GetComponent<Monster>();
        monster.patrolSpeed = .8f;
        monster.chaseSpeed = 1.7f;
        monster.attackRange = 1.5f;
        monster.attackCooldown = 1.2f;
        var spores = go.AddComponent<Esporifero>();
        spores.cloudPrefab = cloud.GetComponent<SporeCloud>();
        go.AddComponent<EsporiferoVisual>().idleSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Esporifero/Esporifero.png");
        return SavePrefab(go, "Esporifero");
    }

    static GameObject MakeBot()
    {
        var go = new GameObject("Bot");
        go.AddComponent<NetworkObject>();
        var sv = go.AddComponent<ShapeVisual>();
        sv.shape = ShapeVisual.Shape.Circle;
        sv.color = new Color(0.60f, 0.55f, 0.95f);
        sv.sortingOrder = 5;
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        go.AddComponent<CircleCollider2D>().radius = 0.5f;
        go.AddComponent<NetworkTransform>(); // servidor decide o movimento do bot
        go.AddComponent<Health>().maxHealth = GameConfig.PlayerMaxHealth;
        go.AddComponent<PlayerInventory>();
        go.AddComponent<PlayerShooter>();
        go.AddComponent<BotController>();
        go.AddComponent<SporeContamination>();
        go.AddComponent<VisibilityTarget>();
        return SavePrefab(go, "Bot");
    }

    static GameObject MakeMatchManager(GameObject monster, GameObject loot, GameObject bot)
    {
        var go = new GameObject("MatchManager");
        go.AddComponent<NetworkObject>();
        var mm = go.AddComponent<MatchManager>();
        mm.lootPrefab = loot;
        mm.monsterPrefab = monster;
        mm.botPrefab = bot;
        return SavePrefab(go, "MatchManager");
    }

    static void BuildScene(GameObject player, GameObject monster, GameObject loot, GameObject matchManager, GameObject bot)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0, 0, -10);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 7f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<CameraFollow>();

        var nmGo = new GameObject("NetworkManager");
        var nm = nmGo.AddComponent<NetworkManager>();
        var utp = nmGo.AddComponent<UnityTransport>();
        if (nm.NetworkConfig == null) nm.NetworkConfig = new NetworkConfig();
        nm.NetworkConfig.NetworkTransport = utp;

        var game = new GameObject("Game");
        var boot = game.AddComponent<NetworkBootstrap>();
        boot.playerPrefab = player;
        boot.monsterPrefab = monster;
        boot.lootPrefab = loot;
        boot.matchManagerPrefab = matchManager;
        boot.botPrefab = bot;
        game.AddComponent<MapBuilder>();
        game.AddComponent<TouchControls>();
        game.AddComponent<GameHud>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }
}

