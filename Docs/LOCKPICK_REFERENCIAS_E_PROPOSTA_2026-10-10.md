# Lockpick: referencias e proposta — 10/10/2026

Status: pesquisa e proposta de design. Nenhuma mecanica foi implementada ou testada nesta etapa. Os parametros abaixo sao pontos iniciais para validacao pratica pelo usuario.

## Referencias pesquisadas

| Referencia | Mecanica documentada | Aplicacao sugerida em Cyber Extraction |
| --- | --- | --- |
| [Skyrim, manual oficial, p. 14](https://steamcdn-a.akamaihd.net/steam/apps/72850/manuals/skyrim_gfw_manual-07.pdf#page=8) | Ajustar a gazua e tentar girar a fechadura; o giro permitido indica a proximidade da posicao correta. | Alternativa de busca por angulo e resistencia, com controles amplos de arrastar e segurar. |
| [Oblivion, manual oficial, p. 41](https://shared.steamstatic.com/store_item_assets/steam/apps/22330/manuals/obliv_goty_pc_man-v2.pdf#page=22) | Elevar e fixar pinos no instante correto; a quantidade de pinos varia com a dificuldade. | Progresso em etapas, quantidade de pinos como dificuldade legivel, sons de encaixe e mecanismo visivel. |
| [Elder Scrolls Online, suporte oficial](https://help.elderscrollsonline.com/app/answers/detail/a_id/3143/~/how-does-the-lockpicking-in-eso-work%3F) | Pressionar pinos e soltar na posicao correta, com pistas visuais/sonoras e limite de tempo. | Principal base proposta: selecionar um pino, segurar para pressionar e soltar ao perceber o sinal de encaixe. |
| [ESO, desgaste das gazuas](https://help.elderscrollsonline.com/app/answers/detail/a_id/1596/~/will-i-need-lockpicks-to-open-a-chest,-and-do-they-still-break-(a-lot)%3F-how-can) | Falhas podem quebrar gazuas, com risco relacionado a dificuldade. | Inspiracao para desgaste previsivel; a proposta abaixo usa erros acumulados, em vez de quebra aleatoria. |

As avaliacoes de adequacao e os parametros seguintes sao propostas proprias, nao caracteristicas confirmadas desses jogos. Darkwood e Bullet Echo continuam como referencias do projeto para atmosfera, leitura de combate e organizacao dos mapas; nao sao apresentados aqui como exemplos de lockpick.

## Mecanica recomendada: pinos e tensao

1. Interagir com um objeto trancado mostra dificuldade, necessidade de gazua e opcao de cancelar. Se houver a chave correta, ela abre diretamente.
2. O painel mostra o miolo da fechadura e de dois a quatro pinos. O jogador seleciona um pino e segura para pressiona-lo.
3. Perto do encaixe, o pino oscila e surge uma pista sonora/visual breve. Soltar nesse momento fixa o pino. A pista indica aproximacao; nao deve revelar permanentemente toda a solucao.
4. Fixar todos os pinos destranca o objeto. Uma pequena animacao de giro conclui a abertura.
5. Segurar alem do ponto seguro aumenta a tensao e produz ruido de metal. Um erro reinicia apenas o pino atual e desgasta a gazua. Sugestao inicial: tres erros quebram uma gazua.

No celular, tocar no pino seleciona e um botao grande serve para segurar/soltar, mantendo o dedo longe da pista visual. No PC, selecao por mouse ou setas e pressao por clique/espaco. No controle, direcional e um botao. Pistas por forma, movimento e som, sem depender apenas de cor ou audio; vibracao opcional.

O painel deve ocupar parte da tela e manter vida, entorno e sons perceptiveis. O mundo continua em tempo real. Deslocar-se, sofrer dano ou cancelar encerra a tentativa e devolve os controles imediatamente. A configuracao da fechadura permanece a mesma ao reabrir o painel, evitando rerrolar uma solucao mais facil. O desgaste ja aplicado nao e recuperado por cancelar.

## Dificuldade inicial sugerida

| Grau | Pinos | Duracao desejada para um jogador familiarizado | Exemplos iniciais |
| --- | --- | --- | --- |
| Simples | 2 | 3–5 s | Gavetas e pequenos armarios |
| Comum | 3 | 5–8 s | Baus e portas internas |
| Reforcada | 4 | 8–12 s | Armarios de armas e salas opcionais |

Essas duracoes sao metas de design, nao medidas. A dificuldade deve ser configurada por fechadura: uma porta pode ser simples e uma gaveta pode ser reforcada. Ajustar largura da janela de acerto e quantidade de pinos antes de introduzir tremores artificiais ou controles mais complexos.

Nao recomendo iniciar com contagem regressiva obrigatoria: o tempo da expedicao e os inimigos ja geram pressao. Um temporizador extra pode ser uma variacao futura para fechaduras especiais.

## Uso no mapa e impacto na gameplay

- Portas trancadas podem liberar atalhos e rotas alternativas, alem de salas de loot. A rota essencial de extracao nao deve depender exclusivamente de obter uma gazua.
- Armarios e gavetas permitem pequenas descobertas nas casas abandonadas. Usar a mesma mecanica com diferentes aparencias reduz a necessidade de aprender varios controles.
- O jogador decide entre procurar cobertura, arriscar uma abertura e abandonar loot ao ouvir um inimigo. Falhas ruidosas podem atrair criaturas pelo sistema de barulho existente.
- Destrancar e abrir sao estados distintos. Uma porta destrancada pode continuar fechada; um recipiente destrancado ainda precisa ser aberto para acessar seu conteudo.
- O estado destrancado e compartilhado na partida. Outros jogadores nao repetem o minigame na mesma fechadura ja vencida.
- Criar algumas alternativas: chave correspondente ou arrombamento ruidoso onde o objeto permitir. Uma fechadura simples nao deve bloquear permanentemente o jogador que perdeu suas gazuas.
- O bau seguro do acampamento permanece acessivel normalmente. Um objeto de treino separado pode ensinar o minigame sem colocar o deposito persistente atras de uma fechadura.

Direcao visual: metal gasto, madeira deteriorada, poucos detalhes de interface, sons secos e encaixes claros. A ambientacao segue a referencia Darkwood; a interface preserva a leitura do entorno e o ritmo de combate definidos para o projeto com Bullet Echo.

## Integracao futura ao projeto atual

Hoje os baus usam uma espera de abertura em `LootPickup`, com interrupcao por dano. Os moveis decorativos do mapa grande ainda sao obstaculos estaticos, e as casas usam aberturas nas paredes. Implementar lockpick tambem exige transformar os objetos escolhidos em recipientes/portas interativos; adicionar o painel sozinho nao torna esses objetos funcionais.

Reutilizar um estado de fechadura para portas, baus, armarios e gavetas. Configuracao por objeto: grau, chave, capacidade de arrombamento e estado inicial. Destravar a fechadura libera a acao especifica do objeto, preservando o inventario e o comportamento de abertura separados.

O servidor controla estado, tentativa ativa, distancia, interrupcoes, desgaste e desbloqueio. O cliente envia entradas do minigame; nao declara simplesmente que abriu a fechadura. A resposta visual deve ser imediata, com tolerancia de rede limitada e definida ao implementar. Cada objeto aceita uma tentativa ativa por vez; desconexao, dano e cancelamento liberam a tentativa. O progresso por partida nao deve ser guardado em assets compartilhados de configuracao.

Primeira implementacao sugerida: gazua, um bau e um armario trancados no laboratorio, dois graus de dificuldade e controles PC/toque. Depois, conectar portas e gavetas a mesma regra. A validacao pratica inicial fica com o usuario; testes automatizados/Unity somente quando pedidos ou apos um problema relatado, conforme AGENTS.md.
