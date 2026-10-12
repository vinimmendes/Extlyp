# Mapa grande e laboratorio de testes — 10/10/2026

## Como usar

Na tela da expedicao, escolha o mapa e marque **Laboratorio: ferramentas e itens temporarios**. Selecione de zero a quatro bots e inicie. O painel abre ao entrar; use **Ferramentas [F2]** para reabrir e **Esc** para fechar. Setas percorrem os controles, Enter confirma; no gamepad, Select abre, direcional/analogico navega, A confirma e B fecha. Ha botoes e rolagem para mouse/toque.

O laboratorio requer um servidor com testes permitidos (Editor, build de desenvolvimento ou argumento `-allow-test`). Cliente e servidor precisam usar a nova compilacao, com protocolo **13**. Os executaveis verificados ficam em `Builds/SkillsValidation/Builds/Client` e `Builds/SkillsValidation/Builds/Server`; os servidores habituais nao foram reiniciados por esta tarefa.

O cenario original do mapa de teste foi preservado. As ferramentas tambem podem ser usadas no mapa grande ao habilitar o laboratorio.

Para testar um inimigo isoladamente, use **Remover todos os monstros** antes de criar o escolhido, especialmente no mapa grande, que inicia com seus encontros habituais. A criacao pelo painel tem limite de 40 monstros/plantas presentes.

## Ferramentas

- **Equipar:** pistola, rifle de ferrolho e espingarda de dois canos; carregador cheio, 60 tiros de reserva e bandagens. Trocar arma substitui a mochila temporaria. Entrega dos 18 tipos de item existentes; se nao couber, o item cai no chao.
- **Monstros:** nove arquetipos originais, Esporifero, planta-mae, bulbo de ataque, bulbo de cura e servo vegetal. Bulbos e servo incluem uma mae porque seu comportamento depende dela. Criacao em espaco livre perto do jogador, preferindo a frente e procurando outras direcoes se estiver bloqueada; remocao de todos os monstros; teia/acido para testar perigos diretamente.
- **Sessao:** zero a quatro bots, invulnerabilidade, recuperar vida/municao e remover status, voltar ao abrigo/reviver, ir para combate, primeira ruina ou perto da extracao.

A partida continua durante o menu; os controles de movimento e disparo ficam bloqueados enquanto o painel esta aberto. O cronometro se renova ao chegar a zero no laboratorio, preservando o ultimo minuto em que bots podem extrair, e morrer/extrair permite continuar usando as ferramentas. A reserva so pode ser reposta ate a capacidade real da mochila.

As mutacoes sao executadas pelo servidor, que verifica o modo, o participante e os limites dos comandos. Clientes do acampamento nao podem usar as ferramentas. Uma partida normal nao aceita comandos de laboratorio.

## Progresso protegido

O laboratorio usa uma mochila temporaria, sem retirar equipamento/refeicao do perfil persistente. A extracao de teste nao deposita recompensas. Morte, saida e desconexao de teste nao descontam itens do acampamento. O retorno ao abrigo reativa a colisao e a visibilidade do jogador.

## Mudancas no mapa grande

As 78 ruinas receberam armarios, mesas, camas, cadeiras quebradas, porta caida, detritos e, em parte delas, gabinetes enferrujados. Foram adicionados trechos de parede danificada e marcas de tabuas no piso. A disposicao deixa o caminho central ate o bau livre.

Foram colocadas 312 arvores, incluindo arvores mortas, com posicoes deterministicas iguais no cliente e servidor. O posicionamento evita os pontos de nascimento, extracao, lagos e a area de circulacao das ruinas.

A arte de arvores e moveis e original, gerada com imagegen, em `Assets/Resources/Environment/AbandonedProps.png`. O PNG foi preservado e recortado por metadados do Unity. O material explicita a textura do atlas; a revisao visual detectou e corrigiu a renderizacao branca causada pela textura ausente no material compartilhado. O prompt esta em `Art/environment-generation.md`.

## Efeito na gameplay

- Moveis e paredes interrompem deslocamento, tiros e visao, oferecendo cobertura e exigindo contorno pelos inimigos.
- O tronco bloqueia a passagem/visao; a copa e visual, permitindo circular sob as extremidades dos galhos.
- A floresta cria encontros com mais obstrucao e pontos de referencia. As entradas e saidas continuam com areas livres.
- O laboratorio acelera a comparacao entre armas/inimigos e permite observar os ataques com protecao. Fora dele, as regras de dano, armas e progresso permanecem as mesmas.

## Verificacao

Unity **6000.0.84f1**, sem atualizar engine ou pacotes. Validacao em copia isolada, preservando cenas e prefabs existentes.

- Compilacao nativa, processamento de RPCs e builds Windows de cliente/servidor.
- 653 verificacoes de cenario/acesso/equipamento, incluindo colisoes, contagem dos objetos, afastamento das arvores, sprites importados e criacao quando a mira aponta para o cercado. Essa contagem inclui verificacoes por arvore; nao representa 653 partidas.
- Verificacoes existentes de inventario, bots, plantas, armas e atividades executadas pelo pipeline de build.
- Teste em dois processos: 26 afirmacoes sobre as tres armas, 14 escolhas de inimigos, bots, perigos, item, extracao, retorno e igualdade do perfil antes/depois. A versao grafica acrescenta tres verificacoes de navegacao por teclado, totalizando 29 por execucao. Evidencias em `Logs/test-lab/result.json` e `Docs/Auditoria/Evidencias/MapaLaboratorio/`.
- Capturas nativas das tres abas em 800 x 360 e 1280 x 720. O fundo do painel foi tornado opaco e os botoes de criar/remover monstros cabem junto da lista, sem exigir rolagem.
- Capturas do mapa renderizadas pelo Unity: `Logs/test-lab-house.png` e `Logs/test-lab-forest.png` na copia de validacao.

A primeira execucao restrita do Unity falhou na inicializacao dos servicos internos. A execucao com permissao passou. A rotina automatica de conexao foi ajustada para aguardar o fim da busca de sala antes de pedir a expedicao. Captura em janela oculta retornou imagem preta e nao foi usada como prova visual da interface. A execucao grafica identificou uma recusa legitima da criacao quando a mira apontava para o cercado; como o menu bloqueia a mira, foi adicionada uma busca alternativa por direcoes livres, sem uma parede entre jogador e ponto de criacao. A visibilidade efetiva continua dependendo do cone de visao do jogador.

Android fisico, multitouch, gamepad fisico, rede com latencia/perda e FPS/GPU no mapa grande continuam sem medicao nesta tarefa. Nao ha promessa de ganho de desempenho; os novos obstaculos e sprites precisam de perfilamento em aparelhos reais.

Repeticao: `Tools/Validate-Unity.ps1 -Method TestLabChecks.Build -Graphics` e `Tools/Run-TestLabSmoke.ps1`. A opcao grafica do segundo script permite capturas e navegacao automatica, usando entrada virtual apenas na rotina de teste.
