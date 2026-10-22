# Plano — HUD da expedição fiel ao conceito

Planejamento solicitado pelo usuário; nenhuma mudança de código, layout salvo ou arte foi aplicada nesta etapa.

## Implementação autorizada posteriormente

O usuário autorizou a execução deste plano. A implementação atual:

- `HudLayout.AlignForRefinement` aplica o alinhamento uma única vez, preservando tamanhos e âncoras. Salva a preferência anterior em `hud_layout_before_alignment_v2` antes de atualizar `hud_layout_v1`; a marca `hud_alignment_v2` impede realinhamentos após futuras edições do usuário. O padrão distribuído também recebeu os alinhamentos. Controles com legenda externa reservam 46 unidades inferiores.
- Os quatro botões secundários compartilham centros verticais e intervalos regulares; arma fica centralizada sobre eles, Mira segue o centro horizontal de Atirar, joystick e status recebem os ajustes correspondentes.
- `HudFrameGraphic.HudStyle` substitui a textura linear dos painéis por superfície translúcida com detalhes discretos e moldura nativa. Contornos mantêm espessura em pixels físicos. Joystick tem indicadores direcionais; Atirar tem contorno secundário parcial. Textos e ícones continuam separados da geometria.
- Vida alinhada à esquerda, trilha delineada e vermelho terroso no tema Sombria; números de tempo e munição têm maior destaque. Status possuem cores próximas da referência. O relógio mantém sua indicação contextual do laboratório sem inventar tempo de partida.
- Trocar, Curar e Recarregar indisponíveis têm ícone/legenda e contorno acinzentados. Botões usam o frame visível como targetGraphic para estados de seleção/pressionado. Indisponibilidade não mostra seleção de gameplay; o organizador continua permitindo editar esses controles.
- A regra existente que limita o minimapa à Floresta Velha foi revisada e preservada: o atlas atual usa terreno, rotas e saídas da floresta. Não foi habilitado um minimapa enganoso no laboratório com dados de outro mapa. Não foram acrescentados inimigos ao mapa.

Verificação executada: inspeção estática da serialização, referências e hierarquia dos dez prefabs. Não houve abertura de sessão Unity, build ou suíte automatizada. A comparação visual e os testes práticos do roteiro abaixo devem ser feitos com a HUD renderizada; nenhuma porcentagem de fidelidade em execução foi afirmada.

### Refinamento de suavidade após as capturas

O usuário pediu que o bloco superior mostre somente tempo, direção e distância, incluindo o laboratório. A identificação “LABORATÓRIO” foi removida do valor principal; o relógio usa o valor real `SecondsLeft`, sem criar um tempo local fictício.

Molduras da HUD agora têm cantos arredondados com 48 pontos de contorno e faixa externa de alpha decrescente. Círculos usam 128 segmentos e faixas suaves tanto na borda externa quanto na interna. A espessura e a largura do feather usam pixels físicos, compensando Canvas e escala personalizada. O preenchimento recebe variação tonal discreta.

As barras existentes foram convertidas para o mesmo componente Image derivado, com `BarStyle`, preservando fileIDs e referências. Recebem cantos discretos e faixa de alpha externa; a trilha da vida também usa a geometria arredondada. Ícones e textos continuam componentes independentes; não foi aplicada uma imagem achatada nem um filtro de blur sobre a interface.

A inspeção estrutural dos dez prefabs passou após essa conversão. A suavidade percebida e a fidelidade à referência ainda precisam ser avaliadas na HUD renderizada, na mesma resolução/escala das capturas. Não foi afirmado que a implementação já atingiu 100%.

## Referência e escopo

A referência visual é `Docs/UI/Referencias/hud-sombria-aprovada.png`. A disposição organizada pelo usuário continua sendo a base, com ajustes de alinhamento. Não será usado o conceito inteiro como uma imagem sobre o jogo: todos os controles, ícones, textos e indicadores permanecem independentes em uGUI, com referências no Inspector.

O cenário do conceito não faz parte da mudança. Comparar a HUD pela arte e pela geometria, sem confundir a atmosfera do cenário com fidelidade dos componentes.

## Diferenças observadas

- Vida centralizada, barra sem acabamento interno e moldura com textura linear forte; o conceito apresenta texto alinhado à esquerda e preenchimento contido numa trilha delineada.
- Botões inferiores com centros verticais diferentes, espaços irregulares e legendas muito próximas da borda inferior.
- Contornos circulares e joystick excessivamente simples; faltam o desgaste discreto, indicadores direcionais e hierarquia de anéis da referência.
- Ícones e legendas pequenos, com pouca distinção de peso entre título, número principal e informação secundária.
- Troca já bloqueada por `WeaponCount > 1` na HUD e `WeaponCount < 2` no servidor. O estado visual atual apenas reduz a opacidade de ícone/texto; não expressa cinza neutro em todo o controle.
- A captura do laboratório não mostra minimapa ou botão Mapa. Revisar a regra de visibilidade nesse contexto e preservar os dados disponíveis, sem acrescentar marcadores de inimigos.

## Ordem de implementação

### 1. Fixar a geometria

Usar o layout atual do aparelho como ponto de partida. O arquivo exportado e o padrão aprovado são referências auxiliares; não pressupor que uma exportação antiga representa a última organização.

- Curar, Trocar, Recarregar e Mochila: alinhar os centros na mesma linha, manter os tamanhos escolhidos e regularizar os intervalos entre as bordas. Reservar a altura das legendas abaixo dos círculos, inclusive a quantidade de curas.
- Painel da arma: centralizar sobre o conjunto de ações secundárias, mantendo a região escolhida pelo usuário.
- Mira e Atirar: alinhar seus centros horizontalmente, com distância suficiente para os alvos de toque não se sobreporem.
- Joystick: preservar o canto e tamanho escolhidos; considerar também a legenda na margem inferior.
- Vida/status: alinhar as bordas esquerdas e colocar os dois status na mesma linha.
- Mapa/Menu/Minimapa: regularizar os alinhamentos na organização aprovada do usuário, sem restaurar automaticamente a distribuição original do desenho conceitual.
- Interação: manter a posição inferior aprovada, com margem e sem disputar os alvos de combate.

Manter altura virtual 600 e área segura. Aplicar as relações de alinhamento em coordenadas lógicas, preservando `hud_layout_v1` e o organizador. Evitar um LayoutGroup externo que volte a reposicionar os controles depois que o usuário os arrastar. O novo padrão deve refletir a organização alinhada; personalizações existentes não serão substituídas por um reset geral.

### 2. Reproduzir a família artística

| Elemento | Tratamento |
| --- | --- |
| Painéis | Oliva quase preto, transparência sobre o cenário, textura irregular muito discreta e borda fina de cobre/marfim envelhecido. Eliminar linhas repetidas que parecem uma placa de madeira. |
| Vida | Texto à esquerda, trilha interna delineada e preenchimento contido. Reproduzir a apresentação da referência Sombria, incluindo o vermelho terroso da barra, preservando os valores e avisos existentes; o tema Simples conserva sua apresentação própria. |
| Tempo/extração | Número dominante e linha de extração menor, com seta de centro fixo e giro suavizado. Não inventar contagem regressiva no laboratório quando esse contexto não a possui. |
| Arma | Ícone à esquerda; nome discreto, carregador dominante e reserva menor. Acabamento compacto e alinhamento interno como na referência. |
| Ações | Círculos com espessura estável em pixels físicos, desgaste leve e ícones com traço e escala coerentes. Legendas legíveis, sem textura sobre as letras. |
| Atirar | Cobre mais evidente, contorno secundário discreto, projétil diagonal e legenda interna, preservando área de toque independente. |
| Joystick | Anéis, núcleo com textura leve e pequenos indicadores direcionais. A área de arraste continua independente da decoração. |
| Minimapa | Moldura própria, norte acima, terreno recortado, jogador direcional, estados de saída e vento dentro do mapa. Nenhum elemento escapa para o resto da HUD. |

Usar sprites separados quando a textura/arte exigir raster e geometria nativa para contornos e estados. Não substituir a referência aprovada nem achatar botões/textos numa imagem. Preservar a mesma geometria para os temas Sombria e Simples.

### 3. Estados de interação

- Trocar sem outra arma: `interactable=false`, ícone, legenda e borda em cinza neutro com contraste suficiente; nenhuma animação de pressão ou comando de troca pelo botão. Reativar ao receber uma segunda arma válida no inventário. Duas armas do mesmo tipo também podem representar duas armas válidas, conforme as regras existentes.
- A restrição do servidor permanece. Não bloquear a ação apenas por estar sem munição nem inventar um cooldown diferente do gameplay existente.
- Organizador: manter o controle selecionável/arrastável mesmo quando indisponível em gameplay.
- Curar e Recarregar: aplicar a mesma família visual de indisponibilidade, preservando suas condições atuais.
- Normal, selecionado e pressionado: resposta discreta de cor/contorno, sem alterar o tamanho real do alvo de toque.

### 4. Comparação visual

Preparar uma composição estática para revisão, com os componentes separados e a geometria alinhada. Comparar recortes de vida, relógio, minimapa, arma, joystick e ações com a referência na mesma escala; usar sobreposição de contornos para conferir margens e centros. Avaliar desgaste, espessura de borda, tamanho do ícone e hierarquia tipográfica em cada região.

Não declarar “95%” ou “100%” de fidelidade sem uma comparação definida. O aceite é visual e componente a componente: o usuário deve reconhecer a referência na HUD renderizada, sem depender do cenário ilustrado.

### 5. Primeira conferência prática do usuário

Sem iniciar sessões Unity, builds ou suítes automaticamente. Na implementação, revisar o código e a serialização; entregar para a primeira validação prática do usuário.

Conferir: zero/uma/duas armas; retirada da segunda arma; pressionar Trocar indisponível; legendas na borda inferior; organizar/salvar/cancelar; área segura e proporções horizontais; PC e celular com a mesma HUD; abertura de saque/menu e liberação de ponteiros ao fechar. Testes adicionais ficam restritos aos problemas relatados ou ao pedido explícito de testes.

## Resultado esperado

HUD com arte próxima da referência em cada componente, mantendo a organização escolhida pelo usuário, alinhamento consistente, botões independentes e estados de ação claros. Unity, rede, regras de combate, minimapa sem inimigos e organização personalizável são preservados.
