# CyberExtraction — bots jogadores com tres dificuldades

O fluxo atual usa servidor dedicado e salas de 5 participantes. Consulte SERVER_SETUP.md: o servidor espera ate 30 segundos e preenche as vagas restantes com bots medios. O menu antigo de Host/Sem bot foi substituido por Iniciar partida. As instrucoes de instalacao abaixo descrevem a versao anterior.

## Atualizar e testar
1. Aguarde a Unity recompilar os scripts. A cena Game existente ja tem os prefabs; nao precisa recria-la.
2. Execute Cyber Extraction > Verificar regras dos bots. As 50 verificacoes conferem prioridades, limite de extracao e visao; nao substituem testes de partidas.
3. Gere e inicie o servidor seguindo SERVER_SETUP.md. Conecte um cliente pelo botao Iniciar partida.
4. A sala aguarda ate 30 segundos e preenche as vagas restantes ate chegar a 5 participantes. Com um humano, nascem quatro bots.
5. A dificuldade padrao e Medio. Para testes de outras dificuldades, ajuste botDifficulty no componente MatchManager do prefab antes de gerar o servidor.

O modo atual usa uma sala por processo. O menu antigo de Host/Sem bot nao faz parte deste fluxo. O projeto nao adiciona inventario persistente, armas novas, audio, matchmaking entre varias salas ou squads.
## Como a IA joga
- Explora pontos possiveis de loot do mapa conhecido, observa e memoriza recursos, escolhe trajetos com A*.
- Nao busca loot por coordenadas de objetos escondidos. Um recurso visto pode ser lembrado, mesmo fora de visao.
- Detecta participantes e monstro somente no cone ou raio proximo, sem parede no meio.
- Reage apos o atraso do nivel, aponta gradualmente e atira pela mesma logica de PlayerShooter.
- Conhece a posicao de um tiro ouvido, mas nao identifica nem acompanha um inimigo escondido. Sons podem ser ignorados de acordo com o nivel.
- Procura distancia segura do monstro, recua com pouca vida e valoriza cobertura. Medio/dificil escolhem posicoes com linha de tiro e obstaculo proximo; coberturas de recuo podem esconder o bot.
- So pode extrair quando faltam 60 segundos ou menos. Antes disso, continua coletando/patrulhando mesmo com a meta de loot atingida, pouca vida ou mapa explorado. Combate e recuo continuam conforme as ameacas. Usa os mesmos 3 segundos na zona, contados apenas depois de liberar a extracao.
- Ao morrer, deixa o loot no chao. Ao extrair ou encerrar a partida, para de agir.
- Bots nao controlam camera nem entrada de teclado/touch. O corpo usa o mesmo circulo/material ja validado para players.

## Inspecao
Selecione Bot(Clone) durante o Play para ver BotController, State e Difficulty. No servidor, a rota/destino podem ser vistos com Gizmos na Scene View. O bot usa Rigidbody2D dinamico no servidor e cinemático nos clientes.

## Regressao em Play Mode
- Cada dificuldade deve sair da casa, contornar paredes/caixas, coletar e conseguir extrair.
- Player e monstro devem causar dano ao bot. Morte do bot deve soltar seu loot, sem encerrar a partida enquanto um humano estiver ativo.
- Extraia enquanto o bot esta vivo: o bot deve continuar; extraindo ele tambem, a partida termina.
- Humano e bot podem estar juntos na zona: os temporizadores precisam ser independentes apesar de compartilharem OwnerClientId do servidor.
- Atraves de parede ou fora do cone, o bot nao deve atirar no alvo escondido. Um tiro ouvido deve levar a investigacao da posicao ou ser ignorado; nao a mira automatica atraves da parede.
- Tire loot fora da visao do bot: ele deve verificar o local lembrado antes de perceber que sumiu.
- Compare reacao e precisao dos tres niveis; ajuste numeros em BotDifficulty.cs apos observar partidas.
- Com dez humanos: repita os testes de tiros, coleta, extracao e fim por tempo, sem bots.
- Para observar bots com clientes remotos, conecte de um a nove humanos; cada participante recebe uma vaga de nascimento separada.

## Validacao atual
A compilacao Windows e as 50 verificacoes das regras dos bots passaram na Unity. Conexao local, preenchimento com nove bots e movimento replicado foram verificados com cliente separado. Testes de partidas completas, balanceamento das tres dificuldades e desempenho Android continuam necessarios.

