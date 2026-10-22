# Preparação da expedição no celular — 2026-10-18

## Implementação

Etapas 1 a 4 implementadas em código. A revisão visual da etapa 5 está pendente da avaliação do usuário; não foram executados testes, Unity, capturas ou builds.

- Composição compacta em plataforma móvel, dispositivo de toque disponível no Input System ou largura virtual inferior a 1280. Usa a área segura e a referência existente de altura 600, sem alterar a escala da HUD.
- Dois painéis iguais com intervalo de 16, arte contínua ao fundo e nome do destino no painel da direita. Telas maiores sem toque mantêm a disposição anterior.
- Cabeçalho compacto com ícones e quantidades de moedas, madeira e pregos. Toque, foco por teclado/controle ou ponteiro sobre cada indicador revelam o nome junto do valor. Marca e título redundantes ficam ocultos.
- Cinco abas com altura 56, textos 26 e destaque da seleção. Ações com altura 64; Partir tem altura 72 e texto 30. Texto informativo usa pelo menos 24 e títulos usam 30, sem redução automática no modo compacto.
- Mochila, capacidade, bandagens e munição mantêm prioridade. Organizar mochila permanece; o atalho duplicado de Personagem fica oculto somente no modo compacto.
- Alterar destino e Bots compartilham uma linha apenas no ambiente de testes. Estado e aviso permanecem acima da ação final; falha de conexão reserva uma linha própria para Voltar, separada de Tentar novamente.
- Navegação direcional da preparação recalculada para controles ativos/interagíveis. Outras páginas mantêm a navegação automática para alcançar os controles externos ao hub.
- Inventário já acompanha ContentBounds; atividades passaram a usar a mesma posição superior para evitar conflito com a navegação maior.

Os comandos de partida, sincronização, persistência, bloqueios de ações e regras de testes permanecem os existentes. Não há novos assets de arte ou alterações de rede.

## Avaliação prática pendente

1. Conferir 640 × 360 e proporções 16:9, 18:9 e 20:9, com área segura nas duas orientações horizontais.
2. Comparar Simples e Sombria, nomes longos de destino, quantidades grandes, mochila cheia e estado bem alimentado.
3. Conferir sincronização inicial, comando pendente, conexão em andamento e falha com Tentar novamente/Voltar.
4. Conferir modo normal e teste, troca de destino e configuração de Bots.
5. Usar toque, mouse, teclado e controle; conferir retorno do foco ao ocultar/desativar controles e abertura das cinco abas.
6. Redimensionar de compacto para amplo e voltar: título, nomes dos recursos e atalho de Personagem devem acompanhar o modo, preservando a composição ampla.

Revisão desta entrega: leitura estática dos fluxos e limites de layout e conferência de whitespace pelo Git. Sem confirmação de execução ou de aparência no aparelho.

## Correção após avaliação: Personagem e inventário

O usuário enviou uma captura mostrando sobreposição do título do personagem com o título da mochila, além de textos pequenos nos atributos e slots. A primeira adaptação havia tratado a preparação e a navegação, mantendo a composição antiga da ficha interna.

- O modo compacto da aba Personagem agora alterna entre Equipamentos e Atributos. Equipamentos mantém mochila e peças equipadas lado a lado; Atributos usa linhas próprias em duas colunas, com controles de investimento de 64 unidades. A troca preserva a aba principal e as regras econômicas existentes.
- Cabeçalho do personagem tem área própria, separado dos títulos das grades. O cabeçalho amplo também foi limitado à largura das colunas que lhe pertencem.
- Slots de inventário/equipamento da base compacta mostram ícones e quantidades/raridade com fonte 24. Os pequenos nomes internos foram ocultados; selecionar o item mostra seu nome e descrição no painel inferior. Slots vazios de equipamento mantêm seus ícones com contraste maior.
- Ações inferiores de descarte, cancelamento, desequipar e redistribuição têm áreas separadas e altura 64. Confirmação de descarte coloca os dois botões lado a lado.
- Descrições usam fonte 24 e uma área de rolagem por toque/roda, sem reduzir a fonte para acomodar atributos longos. O painel de detalhes retorna ao layout anterior ao sair da composição compacta.

Conferir na próxima avaliação: alternar Equipamentos/Atributos, investir pontos, comparar/equipar/desequipar, descrições longas com rolagem, descarte com confirmação e troca de tema/resolução. A loja e as telas de atividades ainda precisam de avaliação visual específica; esta correção não representa validação de todas as abas. Revisão estática e diff-check realizados; sem nova sessão Unity ou build nesta correção.

## Extensão: Comerciante, Atividades e Opções

Tratamento compacto aplicado também às três áreas solicitadas posteriormente:

- **Comerciante:** alternância Ver compra/Ver venda. Na compra, mercadorias à esquerda; na venda, mochila ou baú à esquerda. A área da direita concentra saldo, detalhes com preço unitário e estoque, quantidade, descarregamento e confirmação. Fonte informativa 24 ou maior, botões 26 e alturas 64/72. As ofertas usam ícones e estoque; nome/preço completos aparecem ao selecionar, sem microtexto em cada célula. As verificações de revisão, cotação, saldo, estoque e confirmação continuam existentes.
- **Atividades:** títulos dos atalhos da base ampliados. Na bancada compacta, receitas em cartões verticais de largura completa, recursos/custos legíveis e botões de fabricação de 64. Melhorar reciclador e adquirir componente ficam na mesma lista rolável. Na cozinha, a grade de peixes rola quando necessário. Voltar permanece fixo fora da rolagem. Foco de teclado/controle revela o controle selecionado; arrastar slots de atividade encaminha a rolagem e suprime o clique de preparo.
- **Opções/menu uGUI:** painel compacto mais amplo, fonte 24 para explicações, 26 para ações e 30 para títulos. Temas permanecem lado a lado; organizar controles e voltar usam linhas próprias. Menu principal em duas colunas para comportar ferramentas exclusivas de teste sem ultrapassar a área segura. Ajuda longa tem rolagem e Voltar fixo. Layout é refeito ao redimensionar ou trocar o modo compacto.
- O modo amplo e as regras de rede/economia permanecem existentes. Foi adicionado um construtor compartilhado de rolagem vertical em UguiGeometry; não há novos assets de arte.

Avaliação pendente: comprar/vender com mochila e baú, quantidade máxima, estoque zero, saldo insuficiente, descarregar arma, comando pendente e textos longos; fabricar/reciclar/cozinhar e rolar com toque/controle; trocar tema, abrir ajuda, voltar e confirmar saída; comparar 16:9/18:9/20:9 e retorno ao layout amplo. Revisão estática e diff-check realizados, sem iniciar Unity, testes ou builds.

## Extensão aos menus dentro da expedição

A captura posterior do usuário mostrou a ficha antiga durante a partida. O seletor compacto estava limitado ao hub da base. A composição legível agora se aplica também à mochila, ficha e saque da expedição.

- Janela compacta da expedição ocupa a área segura com margens de 16 nas laterais e 24 no topo/rodapé, conservando a referência virtual de altura 600. A superfície mais opaca evita o cenário atravessando as letras; não muda visão ou colisão no mundo.
- Personagem usa a mesma separação Equipamentos/Atributos. Dentro da partida, há controles separados para mudar a seção, voltar à mochila e fechar, sem disputar a área do título.
- Nível, pontos e XP da ficha vêm do PlayerInventory conectado; os atributos continuam usando a apresentação do equipamento atual. XP pendente fica identificado separadamente. Investimento e redistribuição continuam exclusivos da base.
- Mochila e saque têm grades maiores, cabeçalhos com espaço próprio, ícones/quantidades legíveis e descrições com rolagem. A janela de saque continua limitada à mochila e ao recipiente autorizado, sem acesso ao baú seguro.
- Fechar, Personagem, Mochila, alternância de seção e Desequipar usam altura 64 e fonte 26. O painel de resultados da expedição também foi ampliado no modo compacto.
- O botão flutuante Ferramentas do laboratório não é desenhado nem tratado como alvo de ponteiro sobre o inventário fechado do laboratório; o acesso existente por menu/F2 permanece.

Avaliação pendente: abrir/fechar mochila na partida, alternar ficha/seções, conferir os atributos atuais, comparar/equipar/desequipar, saquear e sair do alcance do recipiente, ler descrições longas, conferir resultados e alternar temas/resoluções. Revisão estática e diff-check; sem execução Unity ou build nesta entrega.

## Duplo toque/clique para equipamento

- Dois toques/cliques primários no mesmo slot e item, em até 0,35 s, acionam o atalho de equipamento. A tolerância de posição usa a escala da interface. Mudança do item, fechamento, perda de foco e início de arraste interrompem a sequência.
- Na mochila, armaduras/acessórios vão ao slot compatível, trocando com a peça anterior. Armas priorizam Arma A vazia, depois Arma B vazia; com ambas ocupadas, substituem a ativa, guardando a anterior no slot de origem da mochila.
- Na peça equipada, o atalho procura um slot vazio na mochila. Mochila cheia gera aviso, sem descarte de itens.
- Usa InventoryWindow.ExchangeEquipment e as validações existentes do servidor. Na expedição, mantém os dois segundos da troca e suas condições de interrupção. No acampamento, aguarda confirmação normal do comando.
- Seleção simples, arraste, navegação por controle, saque e comércio mantêm seus fluxos; o atalho não equipa diretamente do baú, recipiente de saque ou ofertas do comerciante. A seleção informa o gesto disponível.

Avaliação prática pendente: mouse e toque na base/expedição, substituir peça, duas armas ocupadas, desequipar com mochila cheia, repetir gesto durante confirmação/troca e arrastar sem disparar duplo toque. Revisão estática e diff-check, sem iniciar Unity, testes ou builds.

## Comparadores na descrição dos equipamentos

- Ao lado de cada atributo, a descrição apresenta a diferença efetiva: (+N) em verde, (-N) em vermelho e (0) em branco. Os sinais complementam as cores nos dois temas.
- A comparação usa a peça atualmente equipada no slot compatível, identificada no texto. Slot vazio equivale a nenhum bônus. Selecionar a própria peça equipada produz comparação neutra.
- Atributos presentes só na peça anterior também são mostrados, com bônus +0 no item selecionado e a redução correspondente. A proteção física recebe diferença em pontos percentuais.
- As diferenças de atributos respeitam o limite efetivo de 20. Um bônus maior sem ganho efetivo aparece como neutro e indica o limite.
- Disponível nas descrições compartilhadas por mochila, ficha, baú/saque e comerciante, com seleção por toque, clique ou controle. A referência de slot de arma é compartilhada com o atalho de equipar: slot vazio prioritário, depois arma ativa.
- Não altera bônus, balanceamento, preços ou comandos de rede. A descrição do equipamento anterior, quando exibida como referência, omite comparadores duplicados.

Avaliação pendente: peça melhor/pior/igual, slot vazio, perda de atributo secundário, item já equipado, limite 20, proteção física e temas Simples/Sombria. Revisão estática e diff-check, sem iniciar Unity, testes ou builds.

## Nome completo do destino

A captura do usuário mostrou “CATACUMBAS…”: o título compacto tinha altura fixa de 76 unidades e herdava Ellipsis do estilo genérico, apesar da quebra de linha no nome. A altura agora usa a medida preferida do texto com a fonte final, mantendo 30 unidades no compacto e 32 no amplo, sem redução automática ou reticências. No compacto, Alterar destino/Bots, conexão e aviso acompanham a altura do título; as ações finais permanecem fixas. Conferência visual das Catacumbas e dos outros destinos ainda pendente; revisão estática e diff-check realizados, sem iniciar Unity ou build.

## Aviso do TextMeshPro sobre reticências

O usuário trouxe um aviso real de ausência de U+2026 no BarlowCondensed-Regular-SDF durante um rebuild de Canvas disparado pela rolagem. O gerador original não preparava esse caractere; os assets são dinâmicos e limpam o atlas no build. A cópia atual de Regular já possui U+2026 serializado, portanto isso não comprova o estado do atlas que produziu o aviso.

UiFontAssets agora garante U+2026 no carregamento das fontes, antes de instanciar os prefabs ativos da interface. Quando a fonte não consegue disponibilizá-lo, usa como fallback o LiberationSans SDF já incluído no projeto. O preparador de assets também inclui reticências na criação e repara sua ausência nos assets existentes, no próximo ciclo normal de preparação do Editor. Não modifica PackageCache, não desativa avisos e não substitui indiscriminadamente Ellipsis por Truncate.

Revisão estática e diff-check realizados. Avaliação pendente: recompilar normalmente, reabrir inventário/descrições/rolagem, conferir o Console e a exibição de reticências nos dois temas. Nenhum build ou sessão Unity de teste foi iniciado nesta correção.

## Aproveitamento do título e leitura da HUD em partida

Após novas capturas do usuário, o título passou a usar fonte 36 e quebra automática por palavras, sem a quebra fixa após CATACUMBAS. A altura compacta segue a medida do texto, inclusive ficando menor quando o nome cabe em uma linha; os controles abaixo continuam acompanhando o título.

No modo compacto da HUD:

- Vida/stamina usam texto 24 com área própria acima da barra. Relógio usa 36, sem reduzir para 14 no ambiente de testes; informações de extração e distância usam 20, e avisos contextuais usam 22.
- Nome da arma e reserva usam 22, munição/dano usa 30; o painel acomoda as três linhas sem disputar a mesma área.
- Rótulos das ações usam 20/22, Atirar usa 24, Mapa/Menu usam 24 e os estados ativos usam 20. Recarga é o rótulo curto do botão no modo compacto.
- Rótulos externos das ações têm fundo escuro, sem capturar toques; ações indisponíveis mantêm o estado desabilitado com texto de maior contraste. Avisos da extração ganham fundo apenas enquanto houver conteúdo contextual.
- Minimapa usa título 22, legenda 20 e marcadores maiores. No mapa expandido, os nomes dos setores usam 22. As áreas do atlas foram ajustadas para separar cabeçalho e legenda.
- O limite inferior reserva espaço para os novos rótulos, sem apagar ou migrar o layout salvo. Geometria acompanha troca de tema, área segura e modo compacto.

Revisão estática e diff-check realizados. Sem execução Unity ou build. Avaliação visual pendente em tela baixa: ambos os temas, vida/stamina, armas corpo a corpo e de fogo, ações ativas/desabilitadas, cura com quantidade/recarga, aviso contextual da extração, mapa aberto/fechado, layout personalizado e nomes dos três destinos.

## Reticências na SemiBold: resolução de pesos

O usuário relatou novo aviso, agora na SemiBold. A investigação adicional encontrou U+2026 no asset atual, textos de HUD com peso 600 e tabelas FontWeightTable vazias. No TMP instalado, GetCharacterFromFontAsset_Internal entra na resolução de peso/estilo e retorna null quando não há face correspondente, sem chegar à consulta do glifo da fonte base. HasCharacter, usado na primeira correção, não percorre esse mesmo caminho de resolução de peso; por isso apenas preparar o caractere não cobriu o problema.

O projeto agora registra as faces para pesos 600/700 antes de criar a interface: Regular aponta para a SemiBold; a própria SemiBold aponta para sua face física. Fontes de título carregadas dinamicamente e a fonte reserva também recebem mapeamento próprio quando não existe uma variação explícita. Variações explicitamente configuradas são preservadas. O preparador do Editor aplica o mesmo mapeamento aos assets persistentes Barlow, sem alterações em PackageCache ou desativação de avisos.

Verificação desta entrega: leitura dos ramos reais do TMP instalado, metadados dos prefabs/fontes e diff-check. Sem nova sessão Unity ou build. Reabrir o Play Mode após a recompilação para conferir HUD regular/semibold, troca de tema e descrições com rolagem; o resultado em execução ainda está pendente.
