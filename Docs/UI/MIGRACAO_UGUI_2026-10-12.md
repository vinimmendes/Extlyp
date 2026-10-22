# HUD em uGUI — 12/10/2026

Migração da HUD principal para uGUI (Unity UI 2.0.0 já presente no projeto), mantendo Unity 6000.0.84f1, Input System 1.20.1, Netcode for GameObjects 2.13.3 e Transport 2.7.4.

## Implementação

`UguiGameHud` constrói um Canvas de sobreposição com área segura, CanvasScaler e GraphicRaycaster. Cada entrada de `HudControl` tem um GameObject independente, RectTransform, desenho e alvo de interação. A árvore aparece como `CyberExtraction HUD Canvas` durante a execução; a cena e os prefabs existentes não são recriados.

Migrados para componentes uGUI:

- Vida, bem alimentado, esporos, tempo, indicação de extração e progresso.
- Arma/munição/reserva, cura, troca, recarga, mochila e interação contextual.
- Movimento, mira e disparo, com eventos separados por ponteiro.
- Minimap e mapa ampliado, com RawImage, RectMask2D, marcadores filhos e rotação por RectTransform. Terreno/rotas usam o atlas existente e marcadores preservam o conhecimento local das saídas. Não há câmera adicional nem revelação de inimigos.
- Menu, opções de tema, ajuda, confirmação de saída e resultados da partida.
- Organizador da HUD com arraste, seleção, slider de tamanho, restaurar, salvar, cancelar e restauração do padrão.

## Preservação do trabalho aprovado

`HudLayoutDefaults.json`, `hud_layout_v1` e `hud_theme` permanecem as fontes do layout padrão e das preferências por aparelho. Temas A e B reutilizam a arte e a fonte Barlow Condensed. `HudArt` fornece texturas para os elementos do Canvas; caches de novas dimensões têm limites para o editor de tamanho.

`HudLayout` continua calculando os retângulos na área segura de altura virtual 600. Esses valores agora são aplicados a RectTransforms, com arte e área de toque separadas dentro do mesmo controle. O PC apresenta o layout do celular.

Saída da partida, cura, troca, recarga, inventário e interações seguem os pedidos existentes para o servidor. A migração não muda autoridade, balanceamento, colisão ou persistência de progresso.

## Entrada e atualização

`UguiHudPointer` recebe IPointerDown, IPointerUp e IDrag, captura o ID de cada ponteiro e libera o controle ao desativar. `TouchControls` usa essa entrada na expedição; o processamento anterior de toques não roda simultaneamente. No organizador, esses eventos ajustam o layout sem enviar comandos de gameplay.

O EventSystem usa InputSystemUIInputModule; quando não existe um EventSystem na cena, a HUD cria um. Menu recebe foco inicial e botões passivos não entram na navegação. Saúde, contaminação, inventário, status e tempo disparam atualização por eventos do Netcode; cooldowns e dados espaciais são atualizados periodicamente ou por frame conforme necessário.

Os caminhos antigos de OnGUI da HUD, quickbar, minimapa, menu e editor ficam suspensos enquanto o Canvas está ativo, evitando duplicação.

## Transição dos outros fluxos

Inventário/saque, fechaduras, menu de expedição, atividades da cabana e ferramentas de laboratório ainda usam suas interfaces IMGUI atuais. Ao abrir essas janelas, a HUD do Canvas oculta os controles para evitar sobreposição ou comandos atravessando a janela. O controle flutuante da cabana também permanece no caminho anterior.

Isso constitui a primeira etapa da migração gradual solicitada: HUD principal e menus associados. Não representa a conversão de toda a interface do jogo. Os fluxos restantes devem ser migrados preservando suas regras e sem reconstruir a cena.

## Verificação

Revisão estática dos vínculos, APIs locais dos pacotes, callbacks e duplicação de renderizadores. Conforme a preferência do projeto, não foi iniciada Unity, suíte automática, build ou validação em aparelho para esta alteração. Os testes antigos de minimapa e bordas não validam o novo Canvas.
