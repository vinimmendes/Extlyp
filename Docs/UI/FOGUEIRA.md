# Fogueira — QTE de precisao

O preparo de peixe usa `CookingPresentation` para animar o indicador uma vez por quadro no `Update`. `MinigameView.CookingPanel` desenha um painel compacto proprio na area segura, com os temas Simples/Sombria de `HudTheme`.

O cursor cruza o trilho em 1,2 segundo e inverte nas extremidades. O jogador tem uma tentativa para parar na faixa pequena; pode acertar na ida ou na volta. Faixa boa: 46% a 80%; perfeita: 59% a 67%. A faixa perfeita ocupa 8% do trilho, aproximadamente 96 ms por passagem. O limite total e 8 segundos; o tempo esgotado queima o peixe. Antes da faixa boa o erro resulta em cru, depois resulta em queimado; as faixas boa/perfeita geram cozido/perfeito.

Referencia pesquisada: [guia da comunidade HoYoLAB](https://www.hoyolab.com/article/274584) e [cozimento manual de Genshin](https://www.hoyodex.com/wiki/genshin-impact/cooking). Essas referencias descrevem a avaliacao pela faixa em que o cursor e parado. O movimento rapido de ida e volta e a pequena faixa foram especificados pelo usuario para esta adaptacao. Nao ha sequencia de tres rodadas.

O servidor publica `CookStartedAt` usando seu relogio sincronizado do Netcode. O cliente calcula a mesma onda triangular de `CookingGame` em cada Update, incluindo as inversoes. Snapshots nao comandam o movimento. Sem snapshots por mais de 350 ms, a apresentacao para de avancar e bloqueia novas entradas ate receber dados.

Toque e mouse retiram ao pressionar dentro do mesmo retangulo usado para desenhar o botao; ESPACO/A usam `GameInput.HoldPressed`. A retirada e enviada no `Update`, sem esperar a soltura do botao. Quando aceita para envio, congela o indicador e mostra que esta aguardando confirmacao. O estado pendente impede envios duplicados; uma rejeicao permite tentar novamente. Menus, perda de foco e paineis sobrepostos bloqueiam a entrada.

A barra tem superficies distintas para erro, bom e perfeito. O indicador e as faixas usam as mesmas coordenadas normalizadas. A faixa ideal possui contorno fino e contraste alto. Tempo restante fica no cabecalho; orientacao e mensagens de recusa/confirmacao ficam acima da barra. Nao ha texto auxiliar abaixo do trilho. A fogueira aparece como um pequeno simbolo de chama junto ao titulo.

Refinamento visual: painel com largura maxima 520 e altura base 258 (antes: 560 x 520); barra de 22 unidades de altura (antes: 58); cursor de 2 unidades e contornos de 1 unidade. Os botoes Parar/Cancelar ficam na mesma linha. Parar tem largura maxima 230 e altura base 44, com ajuste para manter 44 pixels de altura de toque em telas menores. Desenho e hit-test usam o mesmo `CookingControlBounds`.

Referencias adicionais de composicao: [QTE de timed hits do blog oficial RPG Maker](https://www.rpgmakerweb.com/blog/eventing-a-qte-time-hits-minigame), com trilho, alvo e ponteiro como elementos centrais, e [mudancas de UI de cozinha discutidas no HoYoLAB](https://www.hoyolab.com/article/13502). O refinamento adapta a composicao e usa as paletas proprias do projeto. Nao importa assets nem muda a mecanica.

O comando carrega `CookingPressedAt`, nao uma qualidade ou posicao escolhida pelo cliente. O servidor valida ate 350 ms no passado e 50 ms no futuro, confere o instante contra o inicio da atividade e recalcula a qualidade. E uma compensacao limitada de atraso, nao uma garantia anticheat contra clientes modificados. O antigo minimo de 1 segundo foi reduzido para 100 ms, permitindo acertar ja na primeira passagem. Consumo de item e recompensa continuam autoritativos. Cliente e servidor precisam ser atualizados juntos por causa dos novos campos.

Depois do relato de clique sem concluir o preparo, a validacao e o timeout foram corrigidos para usar o mesmo relogio de rede que anima o cursor. Antes, o clique baseado em NetworkTime era comparado tambem com tempo decorrido de realtimeSinceStartup. Uma diferenca entre os relogios podia rejeitar um clique valido. Recusas agora aparecem no painel e permitem nova tentativa.

`Tools/Audit/Run-CookingProbe.ps1` passou 13 verificacoes focadas fora da Unity, com as classes reais de atividade/apresentacao e adaptadores de engine/inventario. O caso reproduz a recusa por divergencia de relogios e confirma a aceitacao com relogio unico, limites de timestamps, timeout, nao duplicacao e feedback da recusa. A compilacao C# dos scripts de runtime tambem passou. Essas provas nao validam o inventario real, transporte, RPCs, toque ou execucao do servidor instalado. Nenhum player/build foi gerado. A causa observada na sessao do usuario ainda precisa da confirmacao no servidor atualizado.

O refinamento visual posterior recebeu apenas revisao estatica. Nao foram repetidos testes, compilacao, sessoes Unity ou builds para essa alteracao de layout. A aparencia atual ainda requer avaliacao no jogo.

## Preparo em sequencia e falta de espaco

O relato de falha depois do primeiro peixe foi reproduzido com `CampTestResources.Create` e as classes reais de inventario/progresso/atividade. A mochila fornecida tinha apenas um slot vazio; o primeiro peixe assado ocupava esse slot. Outra especie ou qualidade precisava de uma pilha distinta, e a conclusao era recusada mesmo com um clique valido. A prova anterior da fogueira usava inventario adaptado e nao cobria esse problema.

Na base, o resultado agora vai para a mochila quando couber; depois tenta o bau seguro e, se ambos estiverem cheios, usa os itens no chao do acampamento ja existentes. A mensagem informa o destino. Um peixe e consumido, sua origem/qualidade sao preservadas, e a atividade termina uma unica vez. Nao ha mudanca de inventario/recompensas da expedicao.

`Tools/Audit/Run-CookingInventoryProbe.ps1` passou 20 verificacoes focadas, incluindo a sequencia com especies/qualidades diferentes, pilha compativel, bau cheio, ultimo ingrediente liberando slot, ausencia de duplicacao e rejeicao de conclusao forjada. A reproducao anterior esta em `Logs/cooking-inventory-audit/before.txt` e o resultado corrigido em `after.txt`. A compilacao C# do runtime passou. As provas executam o inventario real fora da engine e nao validam serializacao Unity, persistencia nativa, transporte ou execucao do servidor instalado. Nenhum build/player foi gerado nesta correcao.
