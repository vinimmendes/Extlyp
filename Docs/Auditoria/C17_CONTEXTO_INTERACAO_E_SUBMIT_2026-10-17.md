# C17 — Interação do mundo e Submit da interface

Implementada e revisada estaticamente em 17/10/2026. **Sem compilação, testes, Unity ou builds.** Referência E3-04; aceite em execução pendente da T9.

## Causa

O botão sul estava associado tanto à interação do gameplay como ao Submit padrão do InputSystemUIInputModule. A HUD mantinha alguns botões navegáveis/selecionados, e desabilitar o mapa de gameplay em overlays não controlava o módulo separado da uGUI. Uma mesma entrada podia interagir no mundo e confirmar um botão retido. Abrir a janela com essa entrada também exigia impedir seu reaproveitamento como Submit no mesmo frame.

## Correção

- `Assets/Scripts/UI/UiScreenCoordinator.cs`: registra o contexto de entrada na primeira consulta de cada frame e o mantém até o próximo. A entrada de uma tela não é reaproveitada quando um handler abre/fecha outra tela. A tela de resultados passou a ter contexto próprio, sem perder sua navegação por controle.
- `Assets/Scripts/Players/GameInput.cs`: habilitação de gameplay respeita esse contexto, além dos bloqueios existentes. `InteractPressed` exige gameplay tanto no contexto capturado como no contexto atual. O Submit das telas legadas exige contexto de UI estável no frame. Teclas/bindings não foram trocados.
- `Assets/Scripts/UI/ContextUiInputModule.cs` e `.meta`: módulo uGUI derivado que decide a navegação imediatamente antes do processamento de eventos, sem depender da ordem de Update entre HUD e EventSystem. Durante gameplay, desabilita temporariamente eventos de navegação/Submit/Cancel e limpa seleção anterior; o processamento de ponteiro continua ativo. Restaura a configuração de sendNavigationEvents depois de Process.
- Navegação uGUI é permitida nos contextos de menu, inventário, expedição, organizador e resultados quando a tela do frame ainda é a atual. Minigames e ferramentas legadas mantêm sua própria leitura de entradas. O módulo descarta foco fora da raiz da janela ativa e escolhe um controle elegível quando ela não tem foco.
- A instalação reaproveita o EventSystem e copia ações de ponteiro/navegação, rastreamento e opções públicas usadas pelo módulo anterior. O módulo antigo fica desativado, sem apagar componente/asset; somente o módulo contextual fica habilitado. Quando não há HUD uGUI ativa, ele usa o comportamento base.
- `Assets/Scripts/UI/UguiGameHud.cs`: instala o módulo contextual e fornece raízes das janelas. Todos os botões de gameplay da HUD e marcadores de saída ficam fora da navegação automática; mouse e toque continuam podendo clicá-los. Isso evita navegar de uma janela para ações de gameplay. Não foram refeitos Canvas, prefabs, layout ou temas.

Mantidos interação por teclado, ponteiro, toque, minigames e ações dedicadas do controle. Completar ações de inventário por controle continua sendo a C18: esta etapa não declara navegação/transferência de itens pronta. Sem alterações de servidor, combate, save, rede ou protocolo.

## Revisão e validação pendente

Conferidos estaticamente mapa de ações, leitores legados de Submit, instalação de EventSystem, foco e transições de tela. Consultado o fonte local do Input System 1.20.1: `ProcessNavigation` respeita sendNavigationEvents antes de Move/Submit/Cancel, e `Process` mantém processamento separado de ponteiros. Conferidas propriedades copiadas; a API obsoleta com erro trackedDeviceSelect não é utilizada. Aplicadas as skills de Input System, C# e UI/UX, com as regras de layout existentes preservadas.

Na T9, deixar um botão de cura/troca/recarga selecionado e usar botão sul perto de baú/mecanismo: somente interação deve ocorrer. Conferir interação que abre saque no mesmo frame, confirmação que troca/fecha tela, menu/expedição/resultados com foco correto e retorno sem reaproveitar Submit no mundo. Verificar EventSystem novo e preexistente, ações configuradas, mouse/toque na HUD e marcadores, minigames/ferramentas e teclado. A navegação e as transições sob ordens diferentes de Update ainda precisam de observação real. Nenhum teste foi executado nesta etapa.
