# Direcao de arte permanente — HUD Cyber Extraction

## Referencias aprovadas

- A: HUD Simples, baseada na proposta de paineis limpos aprovada pelo usuario.
- B: HUD Sombria, inspirada na atmosfera de Darkwood, com arte e identidade proprias. Imagem aprovada: [hud-sombria-aprovada.png](Referencias/hud-sombria-aprovada.png).
- O cenario da imagem e ilustrativo. Nao foi implementado como mapa e nao deve ser copiado automaticamente para o jogo.
- Os temas compartilham posicoes, controles e regras. A escolha e local e persistente em Menu > Opcoes > HUD; chave PlayerPrefs `hud_theme`.

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
- Na base, mochila e bau seguro lado a lado. Na expedicao, mochila individual; containers usam uma janela de SAQUE propria, sem acesso ao bau seguro.
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
- Molduras finas e discretamente gastas. Nada de bordas grossas, ouro brilhante, ornamentacao medieval ou elementos neon.
- Botoes de acoes podem ser circulares; caixas e menus continuam compactos e legiveis.
- Usar silhuetas simples de equipamento de sobrevivencia: bandagem, mochila, cartucho, arma, ferramenta, chave/gazua.
- Tipografia legivel, com titulos curtos em maiusculas. Nao transformar todo texto em letras pequenas ou muito espacadas.

## Prompt reutilizavel para novas geracoes

> Crie um asset original de interface para Cyber Extraction, jogo 2D de extracao com horror florestal visto de cima. Tema B — Sombria: oliva quase preto, marfim envelhecido e cobre fosco; textura muito sutil de papel ou madeira usada; borda fina, contraste alto e leitura imediata. O elemento solicitado e [DESCREVER BOTAO, ICONE OU PAINEL]. Preserve a familia visual da referencia aprovada da HUD Sombria. Sem logos, personagens, interfaces ou assets copiados de Darkwood. Sem neon, ornamentos medievais, borda grossa, decoracao excessiva ou textura sobre as letras. Celular somente na horizontal. Deixe a composicao funcional, com margem suficiente para toque. Nao incluir um cenario, telefone ou outros elementos quando o pedido for um asset isolado. Para asset isolado, fundo realmente transparente; para conceito de tela completa, usar formato horizontal e respeitar a area segura. Texto exato: [ROTULO], ou nenhum texto quando o rotulo for aplicado pela Unity.

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

O portão da cabana abre um painel horizontal dedicado à **Floresta Velha**, com captura do cenário à esquerda e preparação/partida à direita. Os dois temas usam a mesma geometria pela área segura. Escolha de mapa, bots, laboratório e recursos temporários pertencem exclusivamente ao ambiente de teste confirmado pelo servidor. Consulte [Menu de expedição — Floresta Velha](MENU_EXPEDICAO_FLORESTA_VELHA.md).

`HudTheme.cs` centraliza paletas, paineis, granulado discreto, botoes e estilos. `GameOptionsMenu.cs` salva a selecao no aparelho. Texturas basicas de interface sao produzidas por codigo; a imagem conceitual e uma referencia de direcao, nao um atlas pronto de sprites.

Sem mudanca de engine, rede ou regras de combate. Nenhum APK, build ou teste automatico foi executado nesta entrega. A validacao pratica cabe ao usuario primeiro.
