using UnityEngine;
using CyberExtraction.Players;
namespace CyberExtraction.UI
{
    // Help is rendered by the single game menu, without another floating HUD button.
    public class GameHelp : MonoBehaviour
    {
        public static bool Contains(Vector2 point)=>false;
        public static string Controls => (GameInput.TouchActive
            ? "Celular somente na horizontal.\nMover: controle esquerdo. "
                +(TouchAimSettings.AimAnywhere ? "Mirar: arraste em qualquer área livre da tela, fora dos botões e do joystick. " : "Mirar: arraste no controle de mira. ")
                +(TouchAimSettings.FollowMovement ? "Ao soltar a mira, ela acompanha o movimento." : "A direção da mira permanece ao soltar e andar.")
                +"\nMenu > Opções > Jogabilidade: Mira ao andar, Trocar Mira e Atirar, Tiro automático no toque, Mirar em área livre e Recarga automática no toque. Temas e aparência do cenário ficam em Gráficos. ATIRAR / ATACAR: botão principal.\n"
                +(TouchAimSettings.AutoFire ? "Tiro automático ativo: dispara em alvo vivo e visível na linha central da mira, mesmo sem arrastar. Corpo a corpo continua manual.\n" : "Tiro automático desativado.\n")
                +(TouchAimSettings.AutoReload ? "Recarga automática ativa: carregador vazio recarrega com munição compatível na mochila.\n" : "Recarga automática desativada; use o botão de recarga.\n")
                +"Correr: segure o ícone do corredor.\nCurar: bandagem; o número indica quantidade ou espera em segundos.\nTrocar arma: setas. Recarregar: virotes. Mochila: ícone de mochila.\nMapa: toque no minimapa. Menu: engrenagem ao lado do minimapa."
            : "WASD/setas: mover. Mouse: mirar. Clique: disparar.\nQ/2: trocar arma. R/3: recarregar. I: mochila. E: interagir. 1: bandagem. M: mapa. Esc: voltar/menu.")
            +(TestLabPanel.Available && !GameInput.TouchActive ? "\nF2: laboratório de testes." : "")
            +"\n\nHUD: munição carregada acima, munição na mochila abaixo. Armas corpo a corpo mostram só o ícone.\nMinimapa: norte fixo; seta indica você, porta indica saída. + pronta/aberta, × fechada, ? incerta. Toque para ampliar e escolher a saída.\nOrganizar HUD: tamanhos respeitam o mínimo de toque. Vida e stamina se movem juntas. Padrão restaura o perfil atual; Restaurar organização anterior recupera o layout antigo antes de Salvar.\n\nFique 3 segundos na zona de extracao. Morrer perde a mochila; o bau seguro fica no acampamento.\n\nMenus nao pausam partidas online.";
    }
}
