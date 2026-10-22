# Direcao de arte permanente — HUD Cyber Extraction

## Referencias aprovadas

- A: HUD Simples, baseada na proposta de paineis limpos aprovada pelo usuario.
- B: HUD Sombria, inspirada na atmosfera de Darkwood, com arte e identidade proprias. Imagem aprovada: [hud-sombria-aprovada.png](Referencias/hud-sombria-aprovada.png).
- O cenario da imagem e ilustrativo. Nao foi implementado como mapa e nao deve ser copiado automaticamente para o jogo.
- Os temas compartilham posicoes, controles e regras. A escolha e local e persistente em Menu > Opcoes > Graficos; chave PlayerPrefs `hud_theme`. Controles de toque e organizacao dos botoes ficam em Menu > Opcoes > Jogabilidade.

## Regras de composicao

- Celular exclusivamente na horizontal, com paisagem esquerda e direita. Nunca produzir uma proposta vertical.
- Trabalhar sobre a area segura; considerar recortes, bordas arredondadas e polegares. Referencia de layout: altura virtual 600 e largura derivada da proporcao real.
- Centro da tela livre para o personagem, monstros, mira e indicadores de habilidades.
- Vida e apenas status ativos no alto a esquerda. Bem alimentado e contaminacao usam icone/nome/valor, nunca apenas cor.
- Tempo e orientacao para a extracao no alto ao centro; minimapa e menu no alto a direita. Vento pertence ao minimapa.
- Arma equipada, carregador e reserva junto das acoes de combate. Cura, troca, recarga e mochila agrupadas na parte inferior direita.
- No celular: movimento a esquerda, mira por arraste a direita, disparo dedicado no canto inferior direito. Acoes secundarias nao devem disputar o espaco do disparo.
- Interacao contextual: mostrar somente perto de um objeto utilizavel. Manter E no PC e alvo de toque no celular.
- Uma janela de interacao por vez. Menus bloqueiam comandos do jogador, mas partidas online continuam; isso deve estar claro.
- Na base, mochila e bau seguro lado a lado. Na expedição, a janela de SAQUE mostra mochila e recipiente lado a lado para arrastar itens; nunca dá acesso ao baú seguro da base.
- Laboratorio somente no modo de testes. PC: painel lateral e area de camera adaptada; celular: painel central. Abas Equipar, Monstros, Sessao e Fechaduras. Reiniciar servidor fica nas ferramentas, exclusivo do PC local.

## Tema A — Simples

- Superficies verde neutro escuro, texto claro, destaque verde suave.
- Paineis limpos, cantos discretamente arredondados, separacao por espaco.
- Icones claros e rotulos curtos. Sem texturas decorativas, ornamentos ou efeitos excessivos.
- Priorizar leitura e consistencia. Botoes, campos e janelas compartilham a mesma familia visual.

## Tema B — Sombria

- Superficie oliva quase preta: aproximacao `#131711` para paineis, `#1F2217` para botoes.
- Texto marfim envelhecido: aproximacao `#E6D9BA`; texto secundario `#B0A88A`.
- Destaque cobre envelhecido: aproximacao `#AB784A`. Vida em verde musgo; perigo em vermelho terroso. Cores de estado mantem o mesmo significado do tema A.
- Textura sutil de papel/madeira gasta, com variacao de luminancia muito pequena. Nunca sujar letras ou reduzir contraste.
- Molduras finas e discretamente gastas. A evolução medieval aprovada em 18/10 admite pequenos detalhes góticos nos cantos, ferro escurecido, couro e bronze fosco. Nada de bordas grossas, ouro brilhante, ornamentos excessivos ou elementos neon.
- Botoes de acoes podem ser circulares; caixas e menus continuam compactos e legiveis.
- Usar silhuetas simples de equipamento de sobrevivencia: bandagem, mochila, cartucho, arma, ferramenta, chave/gazua.
- Tipografia legivel, com titulos curtos em maiusculas. Nao transformar todo texto em letras pequenas ou muito espacadas.

## Prompt reutilizavel para novas geracoes

> Crie um asset original de interface para Cyber Extraction, jogo 2D de extracao com horror florestal visto de cima. Tema B — Sombria: oliva quase preto, marfim envelhecido e cobre fosco; textura muito sutil de papel ou madeira usada; borda fina, contraste alto e leitura imediata. O elemento solicitado e [DESCREVER BOTAO, ICONE OU PAINEL]. Preserve a familia visual da referencia aprovada da HUD Sombria. Sem logos, personagens, interfaces ou assets copiados de Darkwood. Sem neon, ornamentos excessivos, borda grossa, decoracao excessiva ou textura sobre as letras. Celular somente na horizontal. Deixe a composicao funcional, com margem suficiente para toque. Nao incluir um cenario, telefone ou outros elementos quando o pedido for um asset isolado. Para asset isolado, fundo realmente transparente; para conceito de tela completa, usar formato horizontal e respeitar a area segura. Texto exato: [ROTULO], ou nenhum texto quando o rotulo for aplicado pela Unity.

Para o tema A, substituir o trecho de textura/desgaste por superficies limpas verde neutro escuro, destaque verde suave e cantos discretamente arredondados. Nao redesenhar as posicoes nem criar novos controles sem necessidade.

## Regras para adicionar botoes e assets

1. Confirmar que a acao existe e pertence ao contexto mostrado; nao inventar barras de fome, stamina, inventarios maiores ou funcionalidades novas apenas para preencher espaco.
2. Reutilizar `HudTheme`, `HudLayout` e `UiLayout`; aplicar o tema via `UiLayout.Begin()` e preservar o hit-test na mesma geometria do desenho.
3. Manter iconografia e significado iguais entre temas. Rotulos da Unity separados da imagem sempre que possivel; manter os sprites existentes.
4. Botoes de toque devem ter alvo suficiente e nao sobrepor joystick, disparo, mapa ou janela de interacao.
5. Se gerar uma imagem nova, guardar uma versao adicional, com transparencia quando necessaria. Nunca substituir o asset aprovado implicitamente.
6. Documentar o prompt e a referencia usada junto da nova arte. Evitar exportar a tela conceitual inteira como fundo da HUD.
7. Respeitar a preferencia de verificacao do AGENTS.md: nao iniciar testes, builds ou sessoes Unity sem pedido ou problema relatado pelo usuario.

## Implementacao atual

Itens iniciais: [besta de mão improvisada, espada enferrujada e virotes](ItemIconsV2/ITENS_INICIAIS.md) integrados com a mesma importação e geometria dos ícones V2. Originais preservados, limite de importação 512 e prévias em 32–128 px. Nenhum tamanho, controle ou placeholder de slot vazio foi alterado.

Em 19/10/2026, o cenário recebe a opção local **Suave / Pixelado** em Opções · HUD e gráficos. Suave usa a captura de visão em resolução completa; Pixelado aplica blocos de dois pixels somente à cor do cenário. Os temas, controles e regras de visão permanecem independentes dessa preferência. Consulte [correção da captura e opção gráfica](GRAFICOS_SUAVE_PIXELADO.md). Compilação C# e prova focada concluídas, sem validação gráfica na Unity ou build do jogo.

Em 19/10/2026, a renovação dos ícones de itens concluiu os lotes A (recursos/peixes), B (armas/munições), C (ferramentas/chaves/extração), D (equipamentos/acessórios), E (moeda/tesouros) e F (treinamento): 59 de 59 ícones integrados. Interface e mundo têm resoluções visuais separadas; todos os objetos no mapa mantêm suas artes anteriores. Preservados os tamanhos recentes dos menus, os perfis HudMetrics e os símbolos funcionais dos controles. Consulte [catálogo, métricas e avaliação](ItemIconsV2/README.md), [lote A](ItemIconsV2/LOTE_A.md), [lote B](ItemIconsV2/LOTE_B.md), [lote C](ItemIconsV2/LOTE_C.md), [lote D](ItemIconsV2/LOTE_D.md), [lote E](ItemIconsV2/LOTE_E.md) e [lote F](ItemIconsV2/LOTE_F.md). A avaliação prática, compressão Android e fechamento dos fallbacks de interface permanecem pendentes. Sem Unity, testes ou builds; a avaliação prática continua com o usuário.

Em **19/10/2026**, a HUD da expedição passa à **Sombria refinada**, com sprites em camadas, símbolos editáveis e perfis em dp para celulares pequenos. A fileira horizontal permanece; o disparo concentra o cobre. `HudMetrics` converte densidade lógica Android para o espaço de `UiLayout`, sem alterar menus. Os padrões por perfil substituem os alinhamentos cumulativos; as preferências antigas continuam restauráveis no organizador. O minimapa mostra uma janela local de 64 unidades de mundo e o mapa ampliado oferece seleção das saídas em faixa rolável com nomes completos. Consulte [arte, pesquisa, integração e avaliação](HUD_SOMBRIA_REFINADA_2026-10-19.md) e [prévias com os atlas de produção](Previews/HudSombriaRefinada/index.html). Revisão estática e de documentos visuais, sem iniciar Unity, testes do jogo ou builds; aparência e toque em execução aguardam avaliação prática. Os registros abaixo descrevem as versões anteriores.

A evolução **Refúgio sob as ruínas** foi aprovada e aplicada à base e ao menu de opções: sprites originais com bordas escaláveis, títulos Cinzel, Barlow no conteúdo, capacidade em 12 segmentos e fundo de catacumbas derivado do conceito. Opções passa a um modal compacto com cartões de tema e bloqueio de cliques no cenário. Consulte [implementação medieval](BASE_MEDIEVAL_IMPLEMENTACAO.md). A semelhança visual em execução ainda depende da avaliação do usuário; a imagem de conceito não é uma captura da Unity.

O menu da base adota o conceito **Antes da descida**: ilustração do destino em fundo amplo, abas com sublinhado, Preparativos com capacidade/suprimentos independentes e painel de expedição compacto. Organizar mochila fica neutro e Partir concentra o preenchimento de destaque. Simples/Sombria compartilham geometria; fundo cobre a tela e controles respeitam a área segura. Consulte [implementação e roteiro de avaliação](BASE_ANTES_DA_DESCIDA.md). Revisão estática e semântica, sem iniciar Unity ou builds; aprovação visual em execução pendente.

Ainda em 18/10/2026, a reformulação do inventário remove a lista “Itens no chão”, amplia a mochila para 12 espaços e o baú para 16, e reserva a apresentação de equipamento equipado à aba Personagem. A preparação usa resumo de mochila/status; o comerciante passa a uma vitrine pessoal 4×3 com estoque finito e reposição após expedição confirmada. Consulte [Inventário e comerciante v5](INVENTARIO_COMERCIANTE_V5_2026-10-18.md) para preços, migração, persistência normal/teste e roteiro de avaliação. Revisão estática; aparência e interação aguardam validação prática.

Em 18/10/2026, a base passa a usar uma **central de preparação em menus**, sem caminhar pela cabana. Preparação, Inventário, Personagem, Comerciante e Atividades compartilham navegação persistente; bancada, cozinha e pesca mantêm suas regras. Equipamento à esquerda, floresta ao centro e partida/conexão à direita, respeitando os dois temas e a área segura. Os assets da cabana permanecem guardados; a representação física fica restrita ao preview explícito. Consulte [Central de preparação](CENTRAL_PREPARACAO_2026-10-18.md). As descrições abaixo sobre portão e caminhada registram o fluxo anterior. Esta alteração recebeu revisão estática, sem validação em execução.

Em 16/10/2026, a etapa 9 acrescenta Comerciante ao inventário da base e Redistribuir à ficha do personagem. Compra e venda têm seleção, preço e confirmação no mesmo painel, sem sobrepor janelas. Os controles são autorados no prefab existente e compartilham os dois temas, a orientação horizontal e os limites de `UiLayout`. Consulte [Comércio e redistribuição](../Integracao/EQUIPAMENTOS_ETAPA_9_2026-10-16.md). Apresentação e navegação aguardam avaliação prática; não houve sessão Unity nesta entrega.

O portão da cabana abre um painel horizontal dedicado à **Floresta Velha**, com captura do cenário à esquerda e preparação/partida à direita. Os dois temas usam a mesma geometria pela área segura. Escolha de mapa, bots, laboratório e recursos temporários pertencem exclusivamente ao ambiente de teste confirmado pelo servidor. Consulte [Menu de expedição — Floresta Velha](MENU_EXPEDICAO_FLORESTA_VELHA.md).

`HudTheme.cs` centraliza paletas, paineis, granulado discreto, botoes e estilos. `GameOptionsMenu.cs` salva a selecao no aparelho. Texturas basicas de interface sao produzidas por codigo; a imagem conceitual e uma referencia de direcao, nao um atlas pronto de sprites.

A aproximação visual de 12/10/2026 usa `HudArt` para símbolos e molduras e configura cada botão separadamente por `HudControl`/`HudPlacement`. A posição e o tamanho de cura, troca, recarga, mochila, movimento, mira, disparo, mapa e menu devem permanecer independentes para a futura personalização. Consulte [HUD Sombria — fidelidade e configuração individual](HUD_SOMBRIA_FIDELIDADE_2026-10-12.md).

Menu > Opções > Organizar botões e tamanhos permite arrastar e redimensionar esses elementos. PC e celular usam o mesmo layout; o PC serve para observar e organizar a HUD do celular. Preferências são locais. Ao salvar na Unity, `Logs/hud-layout-custom.json` registra a organização candidata ao padrão do jogo.

O layout organizado e aprovado pelo usuário em 12/10/2026 está incorporado em `Assets/Resources/UI/HudLayoutDefaults.json`. Ele define os 16 elementos nos dois temas e em PC/celular. Aparelhos sem personalização usam esse padrão; Padrão no organizador restaura essa composição, e Salvar confirma a preferência. Personalizações existentes são preservadas.

Em 16/10/2026, a etapa 3 adiciona STAMINA abaixo dos status no alto à esquerda e CORRER ao lado direito do joystick. São dois controles independentes no organizador, totalizando 18 elementos; os 16 anteriores mantêm seus IDs e posições. O botão usa uma silhueta original de corredor desenhada pelo sistema de ícones existente, sem imagem gerada. Os dois temas compartilham geometria e significado. Consulte [Stamina e corrida](../Integracao/EQUIPAMENTOS_ETAPA_3_2026-10-16.md).

Por decisão do usuário, a HUD principal passa a usar uGUI: `UguiGameHud` aplica `HudLayout` a RectTransforms independentes em um Canvas, com recorte nativo do minimapa e eventos do Input System. Arte, temas e layout aprovado permanecem. Consulte [Migração da HUD para uGUI](MIGRACAO_UGUI_2026-10-12.md) para o escopo e os fluxos ainda em transição.

A interface comum agora é autorada em prefabs editáveis de `Assets/Resources/UI/Prefabs`, incluindo inventário, expedição, cabana e preparação de atividades. Minigames e ferramentas de teste permanecem em IMGUI. Textos usam TextMeshPro/Barlow; o preparo não interativo de fontes SDF e atlas ocorrerá na próxima abertura normal do Editor. Consulte [Transição completa da interface comum](TRANSICAO_COMPLETA_UGUI_2026-10-12.md) para arquitetura e roteiro de validação.

A prancha de menus aprovada também foi implementada em componentes independentes: expedição com ilustração ambiental separada, inventário com quantidades próprias nos slots, seleção compacta dos peixes elegíveis e bancada com três cartões de receita. Os paineis principais usam superfícies quase opacas para separar a leitura do cenário. Consulte [Menus do conceito em uGUI](MENUS_CONCEITO_IMPLEMENTADOS_2026-10-12.md). A prancha não é usada como uma imagem de interface com áreas transparentes de clique.

Sem mudanca de engine, rede ou regras de combate. Nenhum APK, build ou teste automatico foi executado nesta entrega. A validacao pratica cabe ao usuario primeiro.
