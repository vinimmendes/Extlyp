using UnityEngine;
using CyberExtraction.Players;
namespace CyberExtraction.UI
{
    // Help is rendered by the single game menu, without another floating HUD button.
    public class GameHelp : MonoBehaviour
    {
        public static bool Contains(Vector2 point)=>false;
        public static string Controls => (GameInput.TouchActive
            ? "Celular somente na horizontal.\nEsquerda: mover. Direita: arrastar para mirar. ATIRAR: disparar. Use as acoes para curar, trocar arma, recarregar e abrir a mochila."
            : "WASD/setas: mover. Mouse: mirar. Clique: disparar.\nQ/2: trocar arma. R/3: recarregar. I: mochila. E: interagir. 1: bandagem. M: mapa. Esc: voltar/menu.")
            +(TestLabPanel.Available && !GameInput.TouchActive ? "\nF2: laboratório de testes." : "")
            +"\n\nFique 3 segundos na zona de extracao. Morrer perde a mochila; o bau seguro fica no acampamento.\n\nMenus nao pausam partidas online.";
    }
}
