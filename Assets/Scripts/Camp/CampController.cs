using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using CyberExtraction.Core;
using CyberExtraction.Players;
using CyberExtraction.Items;

namespace CyberExtraction.Camp
{
    // Acampamento pessoal fora da expedicao, montado sem sprites finais.
    public class CampController : MonoBehaviour
    {
        public static CampController Instance { get; private set; }
        public static bool Active => Instance != null && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening;
        public bool ExpeditionPanelOpen;
        public bool CanMove => Active && !ExpeditionPanelOpen && !depotOpen && !fishing;
        public static Rect InteractionBounds => new Rect(Screen.width * 0.2f, Screen.height * 0.82f, Screen.width * 0.6f, Screen.height * 0.12f);
        readonly Vector2 origin = new Vector2(2000, 0);
        readonly Vector2 depot = new Vector2(-5, -2), gate = new Vector2(0, 7), pier = new Vector2(7, 0);
        readonly List<SpriteRenderer> boards = new List<SpriteRenderer>();
        GameObject root;
        Rigidbody2D avatar;
        bool depotOpen, fishing;
        float fishingLeft;
        string message = "";

        void Awake()
        {
            Instance = this;
            root = new GameObject("PersonalCamp");
            Block("Ground", Vector2.zero, new Vector2(60, 50), new Color(0.16f, 0.22f, 0.16f), -10);
            Block("River", new Vector2(24, 0), new Vector2(34, 50), new Color(0.10f, 0.26f, 0.34f), -8);
            Block("Path", new Vector2(0, 2), new Vector2(2, 11), new Color(0.30f, 0.27f, 0.21f), -7);
            Block("Tent", new Vector2(-5, 4), new Vector2(3.8f, 3), new Color(0.43f, 0.42f, 0.25f), 1, true);
            Block("Depot", depot, new Vector2(2.5f, 1.8f), new Color(0.46f, 0.31f, 0.19f), 2, true);
            Block("GateLeft", gate + new Vector2(-2, 0), new Vector2(0.6f, 2), new Color(0.52f, 0.43f, 0.29f), 2, true);
            Block("GateRight", gate + new Vector2(2, 0), new Vector2(0.6f, 2), new Color(0.52f, 0.43f, 0.29f), 2, true);
            for (int i = 0; i < 5; i++)
                boards.Add(Block("PierBoard", pier + new Vector2(i * 0.7f, 0), new Vector2(0.6f, 2), Color.gray, 2).GetComponent<SpriteRenderer>());
            for (int i = 0; i < 9; i++)
            {
                Vector2 position = new Vector2(-11 + i * 2.5f, -7.5f);
                var tree = Block("Tree", position, new Vector2(1.3f, 1.3f), new Color(0.09f, 0.15f, 0.10f), 3, true);
                tree.GetComponent<SpriteRenderer>().sprite = SpriteFactory.Circle;
            }
            var player = Block("CampPlayer", new Vector2(0, 4), new Vector2(0.8f, 0.8f), new Color(0.2f, 0.75f, 0.65f), 5);
            player.GetComponent<SpriteRenderer>().sprite = SpriteFactory.Circle;
            player.AddComponent<CircleCollider2D>().radius = 0.5f;
            avatar = player.AddComponent<Rigidbody2D>();
            avatar.gravityScale = 0; avatar.freezeRotation = true;
            avatar.interpolation = RigidbodyInterpolation2D.Interpolate;
            FocusCamera();
        }

        public void FocusCamera()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                camera.orthographic = true; camera.orthographicSize = 8;
                camera.transform.position = new Vector3(avatar.position.x, avatar.position.y, -10);
                var follow = camera.GetComponent<CameraFollow>() ?? camera.gameObject.AddComponent<CameraFollow>();
                follow.SetTarget(avatar.transform);
            }
        }

        GameObject Block(string name, Vector2 position, Vector2 size, Color color, int order, bool solid = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform);
            go.transform.position = origin + position;
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            SpriteFactory.Setup(go.AddComponent<SpriteRenderer>(), SpriteFactory.Square, color, order);
            if (solid) { go.layer = GameConfig.WallLayer; go.AddComponent<BoxCollider2D>(); }
            return go;
        }

        void Update()
        {
            root.SetActive(Active);
            if (!Active) return;
            foreach (var board in boards)
                board.color = CampSession.Progress.PierRepaired ? new Color(0.59f, 0.41f, 0.23f) : new Color(0.30f, 0.27f, 0.24f, 0.65f);
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { ExpeditionPanelOpen = depotOpen = false; }
            if (CanMove && keyboard.eKey.wasPressedThisFrame) Interact();
        }

        void FixedUpdate()
        {
            if (!CanMove) { avatar.linearVelocity = Vector2.zero; return; }
            Vector2 movement = Vector2.zero;
            var touch = TouchControls.Instance;
            if (touch != null && touch.IsTouch) movement = touch.Move;
            else if (Keyboard.current != null)
            {
                var k = Keyboard.current;
                movement.x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
                movement.y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0);
            }
            avatar.linearVelocity = Vector2.ClampMagnitude(movement, 1) * GameConfig.PlayerSpeed;
            avatar.position = new Vector2(Mathf.Clamp(avatar.position.x, origin.x - 12, origin.x + 7.2f), Mathf.Clamp(avatar.position.y, -6.7f, 8));
        }

        int Nearby()
        {
            Vector2 local = avatar.position - origin;
            if (Vector2.Distance(local, depot) < 2.8f) return 1;
            if (Vector2.Distance(local, gate) < 2.4f) return 2;
            if (Vector2.Distance(local, pier) < 2.5f) return 3;
            return 0;
        }

        void Interact()
        {
            int near = Nearby();
            if (near == 1) depotOpen = true;
            else if (near == 2) ExpeditionPanelOpen = true;
            else if (near == 3)
            {
                if (!CampSession.Progress.PierRepaired)
                {
                    if (CampSession.Progress.RepairPier())
                    { CampSession.RepairPending = true; message = "Pier reparado! Agora voce pode pescar."; }
                    else message = "Faltam materiais para reparar o pier.";
                }
                else if (!CampSession.Progress.WellFed) StartCoroutine(Fish());
                else message = "Bem alimentado ja esta preparado para a proxima expedicao.";
            }
        }

        IEnumerator Fish()
        {
            fishing = true;
            fishingLeft = 5;
            while (fishingLeft > 0) { fishingLeft -= Time.deltaTime; yield return null; }
            CampSession.Progress.Fish(); CampSession.FishPending = true;
            fishing = false; message = "Bem alimentado: +20 de vida na proxima expedicao.";
        }

        void Label(Vector2 position, string text, GUIStyle style)
        {
            if (Camera.main == null) return;
            Vector3 screen = Camera.main.WorldToScreenPoint(origin + position);
            GUI.Label(new Rect(screen.x - 130, Screen.height - screen.y - 40, 260, 40), text, style);
        }

        void OnGUI()
        {
            if (!Active || ExpeditionPanelOpen) return;
            var old = GUI.matrix; GUI.matrix = Matrix4x4.identity;
            int font = Mathf.Clamp(Screen.height / 28, 18, 32);
            var label = new GUIStyle(GUI.skin.label) { fontSize = font, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            var button = new GUIStyle(GUI.skin.button) { fontSize = font, wordWrap = true };
            GUI.Box(new Rect(10, 10, Screen.width - 20, font * 3), "");
            var profile = CampSession.Progress;
            GUI.Label(new Rect(15, 15, Screen.width - 30, font * 2.7f),
                $"ACAMPAMENTO\nM {profile.Wood}   P {profile.Nails}   B {profile.Bandages}" + (profile.WellFed ? "   Bem alimentado" : ""), label);
            Label(depot + new Vector2(0, 1.8f), "DEPOSITO", label);
            Label(gate + new Vector2(0, 1), "EXPEDICAO", label);
            Label(pier + new Vector2(0, 1.8f), profile.PierRepaired ? "PESCA" : "PIER QUEBRADO", label);
            if (depotOpen)
            {
                Rect panel = new Rect(Screen.width * 0.10f, Screen.height * 0.24f, Screen.width * 0.80f, Screen.height * 0.5f);
                GUI.Box(panel, "");
                GUI.Label(new Rect(panel.x + 10, panel.y + 15, panel.width - 20, panel.height * 0.45f),
                    $"DEPOSITO\nM - Madeira: {profile.Wood}\nP - Pregos: {profile.Nails}\nB - Bandagens: {profile.Bandages}\nLevar bandagens: {CampSession.BringBandages}/3", label);
                var ammoTypes = new[] { ItemId.PistolAmmo, ItemId.RifleAmmo, ItemId.ShotgunShell };
                var recipeStyle = new GUIStyle(button) { fontSize = Mathf.Max(12, font - 4) };
                for (int i = 0; i < ammoTypes.Length; i++)
                {
                    int output = i == 0 ? 12 : i == 1 ? 5 : 4;
                    if (GUI.Button(new Rect(panel.x + 10 + i * (panel.width - 20) / 3, panel.y + panel.height * .43f,
                        (panel.width - 20) / 3 - 4, panel.height * .11f), $"{ItemCatalog.Icon(ammoTypes[i])} x{output}\nMT3 PO1 C1", recipeStyle))
                        message = CampSession.Act(new CampAction { Kind = CampActionKind.CraftAmmo, Result = (int)ammoTypes[i] })
                            ? "Municao criada no deposito ou mochila." : "Faltam materiais, espaco ou limite de acoes atingido.";
                }
                if (GUI.Button(new Rect(panel.x + 20, panel.y + panel.height * 0.55f, panel.width * 0.3f, panel.height * 0.15f), "-", button))
                    CampSession.BringBandages = Mathf.Max(0, CampSession.BringBandages - 1);
                if (GUI.Button(new Rect(panel.x + panel.width * 0.65f, panel.y + panel.height * 0.55f, panel.width * 0.3f - 20, panel.height * 0.15f), "+", button))
                    CampSession.BringBandages = Mathf.Min(3, Mathf.Min(profile.Bandages, CampSession.BringBandages + 1));
                GUI.Label(new Rect(panel.x + 10, panel.y + panel.height * .70f, panel.width - 20, panel.height * .07f), message, recipeStyle);
                if (GUI.Button(new Rect(panel.x + 20, panel.y + panel.height * 0.77f, panel.width - 40, panel.height * 0.15f), "Fechar", button)) depotOpen = false;
            }
            else
            {
                int near = Nearby();
                string text = fishing ? $"Pescando... {Mathf.CeilToInt(fishingLeft)}s" :
                    near == 1 ? "Abrir deposito [E]" : near == 2 ? "Preparar expedicao [E]" :
                    near == 3 ? (profile.PierRepaired ? "Pescar [E]" : "Reparar pier: M 20 + P 10 [E]") : "WASD / joystick: caminhar ate o deposito, pier ou portao";
                bool enabled = GUI.enabled; GUI.enabled = near != 0 && !fishing;
                if (GUI.Button(InteractionBounds, text, button)) Interact();
                GUI.enabled = enabled;
                if (!string.IsNullOrEmpty(message)) GUI.Label(new Rect(20, Screen.height * 0.7f, Screen.width - 40, Screen.height * 0.10f), message, label);
            }
            GUI.matrix = old;
        }

        void OnDestroy() { if (root != null) Destroy(root); if (Instance == this) Instance = null; }
    }
}
