using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using CyberExtraction.Combat;
using CyberExtraction.Match;
using CyberExtraction.Players;
using CyberExtraction.Core;

namespace CyberExtraction.UI
{
    // HUD simples em IMGUI: tempo, vida, loot e mensagens de estado.
    public class GameHud : MonoBehaviour
    {
        private void OnGUI()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsListening || !nm.IsClient) return;

            float s = Mathf.Max(1f, Screen.height / 600f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float w = Screen.width / s, h = Screen.height / s;

            var small = new GUIStyle(GUI.skin.label) { fontSize = 20 };
            small.normal.textColor = Color.white;
            var center = new GUIStyle(GUI.skin.label) { fontSize = 30, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            center.normal.textColor = Color.white;
            var button = new GUIStyle(GUI.skin.button) { fontSize = 20 };

            if (GUI.Button(new Rect(w - 100, 10, 90, 40), "Sair", button))
            {
                nm.Shutdown();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }

            var mm = MatchManager.Instance;
            if (mm == null) return;

            var po = nm.SpawnManager != null ? nm.SpawnManager.GetLocalPlayerObject() : null;
            var inv = po != null ? po.GetComponent<PlayerInventory>() : null;
            var hp = po != null ? po.GetComponent<Health>() : null;

            if (mm.CurrentState != MatchState.Waiting)
            {
                int sec = mm.SecondsLeft.Value;
                GUI.Label(new Rect(w * 0.5f - 60, 10, 120, 30), $"{sec / 60}:{sec % 60:00}", small);
            }
            if (hp != null) GUI.Label(new Rect(10, 10, 200, 30), $"Vida: {hp.Current.Value}/{hp.Maximum.Value}", small);
            if (inv != null) GUI.Label(new Rect(10, 40, 280, 30), $"M {inv.Wood.Value}   P {inv.Nails.Value}   B {inv.Bandages.Value}", small);
            var shooter = po != null ? po.GetComponent<PlayerShooter>() : null;
            if (shooter != null && shooter.Weapon != null)
            {
                string state = shooter.Reloading ? " - Recarregando" : shooter.CyclingBolt ? " - Ferrolho" : "";
                GUI.Label(new Rect(10, 100, w - 20, 30), $"{CyberExtraction.Items.ItemCatalog.Name(shooter.Weapon.Item)}: {shooter.Magazine}/{shooter.Weapon.Capacity} + {shooter.Reserve}{state}", small);
            }
            GUI.Label(new Rect(10, 70, 150, 30), $"Sala {mm.RoomNumber.Value}", small);

            string msg = null;
            if (mm.CurrentState == MatchState.Waiting)
                msg = $"Sala {mm.RoomNumber.Value}: {mm.HumanCount.Value}/{GameConfig.MatchCapacity}\nInicio em {mm.WaitingSeconds.Value}s\nAs vagas restantes serao preenchidas com bots.";
            else if (inv != null)
            {
                switch (inv.CurrentStatus)
                {
                    case PlayerStatus.Extracted: msg = $"Voce extraiu!\nM {inv.Wood.Value}   P {inv.Nails.Value}   B {inv.Bandages.Value}\nMateriais enviados ao deposito."; break;
                    case PlayerStatus.Dead: msg = "Voce morreu"; break;
                    case PlayerStatus.TimeUp: msg = "Tempo esgotado"; break;
                }
            }
            if (mm.CurrentState == MatchState.Ended)
                msg = (msg ?? "Partida encerrada") + $"\nVoltando ao menu em {mm.ResultsLeft.Value}s";
            if (msg != null) GUI.Label(new Rect(20, h * 0.35f, w - 40, 170), msg, center);
            if (inv != null && !inv.IsAlive && GUI.Button(new Rect(w * 0.5f - 130, h * 0.68f, 260, 50), "Voltar ao acampamento", button))
            {
                nm.Shutdown();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
