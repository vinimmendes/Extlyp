# Câmera na entrada da instância — 2026-10-18

## Relato e evidência estática

O usuário relatou que algumas entradas na instância deixam a câmera distante, com captura dentro das Catacumbas. PlayerController.OnNetworkSpawn escolhia o zoom uma única vez usando MapBuilder.Current. Esse estado estático começa como Test e é atualizado separadamente por MatchManager.OnNetworkSpawn e pelos callbacks de MapSelection/LayoutSeed. Se o jogador chegasse antes dessa configuração, recebia ExpeditionCameraSize (11) e nunca revisava a escolha para DungeonCameraSize (5,5).

Isso identifica um caminho no código compatível com a intermitência. A ordem incorreta não foi reproduzida em uma sessão Unity nesta investigação.

## Alteração

- A câmera local aguarda MatchManager spawnado e concordância entre a seleção replicada e o mapa configurado.
- A configuração é revisada no spawn e no Update do proprietário, antes dos bloqueios de entrada de gameplay. O jogador pode receber o mapa antes ou depois de seu próprio spawn.
- O zoom da masmorra permanece 5,5; floresta/teste permanecem 11. Limites de câmera da masmorra são aplicados junto da configuração e removidos ao mudar de mapa pelo SetTarget existente.
- Não refaz o vínculo quando mapa, zoom, projeção e alvo já estão corretos. Se outro fluxo sobrescrever o zoom ou alvo, a câmera local reaplica a configuração da expedição.
- O acampamento não é assumido pelo jogador da expedição enquanto CampController.Active for verdadeiro. Não há alteração no zoom da base, mapas, visão, rede ou balanceamento.
- CameraFollow.Follows permite conferir o alvo sem reiniciar o amortecimento a cada frame.

## Verificação

Leitura estática dos escritores de orthographicSize, callbacks de sincronização e caminho da câmera; diff-check. Sem testes, builds ou nova sessão Unity.

Avaliação prática pendente: entrar repetidamente nas Catacumbas, alternar floresta/masmorra entre conexões, abrir menus logo no spawn, conferir limites nas bordas e voltar à base. Confirmar o comportamento em cliente separado do servidor, incluindo chegada do jogador antes da configuração do mapa.
