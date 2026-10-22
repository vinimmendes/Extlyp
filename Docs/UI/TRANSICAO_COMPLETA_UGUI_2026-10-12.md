# Interface comum do jogador em uGUI

Implementação do plano aprovado em 12/10/2026. Unity 6000.0.84f1, uGUI 2.0.0, Input System 1.20.1, Netcode 2.13.3 e Transport 2.7.4 preservados. Nenhuma cena ou prefab de gameplay foi recriado por MvpSetup.

## Prefabs editáveis

Pasta: `Assets/Resources/UI/Prefabs`.

- `PlayerInterface.prefab`: Canvas, CanvasScaler, GraphicRaycaster, área segura e referências aos prefabs de telas.
- `Hud.prefab`: os 16 elementos independentes, com referências serializadas de arte, texto, área de toque e botão.
- `Menus.prefab`, `Organizer.prefab`, `Results.prefab`: menu/opções/ajuda/saída, organização e resultados.
- `Inventory.prefab`, `ItemSlot.prefab`: mochila, baú seguro e saque, com grades de nove slots e representação do item arrastado.
- `Expedition.prefab`: ambiente, conexão, erro/reconexão, imagem da Floresta Velha e preparação/partida. Mensagens usam ScrollRect.
- `Cabin.prefab`, `Activities.prefab`: recursos e instalações, bancada, receitas, melhorias e seleção de peixe para cozinhar/comer.

O runtime instancia os prefabs e usa suas referências. Não constrói mais a árvore inteira da interface com AddComponent. Os componentes menores de apresentação cuidam de inventário, expedição e cabana; HUD, minimapa, menus e organizador têm código separado. Os RectTransforms internos da HUD são autorados no prefab e redimensionados pelo contêiner visual, preservando a edição artística dos filhos.

## Arte, texto e recursos

`UI/Art` contém sprites reutilizáveis derivados das silhuetas originais já aprovadas; molduras usam Image sliced. `HudFrameGraphic` desenha os círculos nativamente no Canvas, com espessura em pixels e recorte padrão de MaskableGraphic. Não depende de reduzir uma textura de aro de 512 pixels para um botão pequeno.

Textos usam TextMeshPro com Barlow Condensed Regular/SemiBold. Na próxima abertura normal da Unity, `PlayerUiAssetPreparation` importa TMP Essentials pelo `TMP_PackageResourceImporter.ImportResources(true,false,false)` disponível no pacote, cria as fontes SDF e o SpriteAtlas e resolve suas referências nos prefabs. Não abre o importador interativo, não altera a cena e não executa testes. Esse preparo de assets ainda não foi executado nesta entrega; os recursos nativos serão produzidos pelo Editor.

O padrão continua vindo de `HudLayoutDefaults.json`; posições e tamanhos locais usam `hud_layout_v1`, e o tema usa `hud_theme`. PC e celular compartilham a mesma HUD. Área segura e altura virtual 600 preservadas.

## Regras e entrada

`UiScreenCoordinator` identifica a tela ativa e integra bloqueio de gameplay e raycast nativo. Menus e janelas usam os controladores existentes. EventSystem é criado somente quando não há um e usa InputSystemUIInputModule. Se existir apenas módulo antigo, ele é desativado e o módulo do Input System é integrado.

Transferência, reorganização, saque, descarte, fabricação, melhorias, preparação e partida usam os pedidos atuais. Dados por jogador, receitas, recompensas e persistência continuam autoritativos. O inventário não remove ou concede itens por previsão local; espera o estado confirmado.

Arraste é vinculado a um ponteiro, registra o item de origem e é cancelado se a janela fechar, o destinatário mudar, o jogador morrer, o recipiente desaparecer/sair do alcance ou ocorrer perda de foco. Um item alterado durante o gesto não é enviado como se ainda fosse o item capturado. Toque para saquear continua separado de arraste; não há segunda transferência no clique que sucede o gesto.

## IMGUI preservado

Pesca, cozinha em andamento, gazuas, laboratório, ferramentas de teste e diagnósticos continuam em IMGUI. Preparação da bancada/cozinha/pesca e interações comuns são uGUI. Ao entrar em minigame, as telas comuns são ocultadas pelo coordenador.

Os renderizadores IMGUI antigos das telas migradas ficam suspensos quando a nova interface está ativa. Conforme o plano, a remoção definitiva desses caminhos de compatibilidade fica para depois da primeira validação prática do usuário.

## Skills e verificações

Skill oficial `ui-ugui` instalada em `C:/Users/Vinni/.codex/skills/ui-ugui`, com a referência de ScrollView. Aplicadas também as skills locais de UI/UX, C# e Input System. APIs do importador TMP e dos componentes foram conferidas no pacote instalado.

Revisão estática de código, metadados e referências locais dos 10 prefabs realizada. Nenhuma Unity foi iniciada, nenhum player/build foi gerado e não houve suíte automática ou teste ingame. A inspeção textual dos prefabs não valida importação nativa, renderização, eventos, multitouch ou RPCs.

## Primeira validação prática

1. Abrir a Unity normalmente e aguardar importação/compilação e preparação dos recursos TMP. Verificar Console antes de Play.
2. Conferir os prefabs no Inspector e a HUD Sombria no layout aprovado; alternar para Simples.
3. Organizar um elemento, redimensionar, salvar, cancelar e restaurar o padrão. Conferir preferências existentes.
4. Testar movimento/mira/disparo simultâneos; fechar menus, perder foco e voltar sem disparo involuntário.
5. Conferir mochila individual, baú da base e saque; pilhas, mochila cheia, arraste para fora e recipiente que desaparece.
6. Conferir conexão, erro/reconexão, preparação, partida, morte, extração e retorno.
7. Conferir bancada, melhoria, componente, seleção de peixe e entrada/saída de pesca/cozinha/gazuas.
8. Repetir em paisagem esquerda/direita e telas com área segura. Validar multitouch em aparelho quando solicitado.

Falhas relatadas devem acionar somente as verificações relacionadas, sem reconstrução de gameplay ou repetição de suítes completas.
