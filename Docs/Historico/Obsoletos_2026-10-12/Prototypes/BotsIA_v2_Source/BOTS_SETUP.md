# CyberExtraction — bots jogadores com tres dificuldades

O fluxo atual usa servidor dedicado e salas de 5 participantes. Consulte SERVER_SETUP.md: o servidor espera ate 30 segundos e preenche as vagas restantes com bots medios. O menu antigo de Host/Sem bot foi substituido por Iniciar partida. As instrucoes de instalacao abaixo descrevem a versao anterior.

## Atualizar e testar
1. Aguarde a Unity recompilar os scripts. A cena Game existente ja tem os prefabs; nao precisa recria-la.
2. Execute Cyber Extraction > Verificar regras dos bots. As 76 verificacoes conferem prioridades, limite de extracao e visao; nao substituem testes de partidas.
3. Gere e inicie o servidor seguindo SERVER_SETUP.md. Conecte um cliente pelo botao Iniciar partida.
4. A sala aguarda ate 30 segundos e preenche as vagas restantes ate chegar a 5 participantes. Com um humano, nascem quatro bots.
5. A dificuldade padrao e Medio. Para testes de outras dificuldades, ajuste botDifficulty no componente MatchManager do prefab antes de gerar o servidor.

O modo atual usa uma sala por processo. O menu antigo de Host/Sem bot nao faz parte deste fluxo. O projeto nao adiciona inventario persistente, armas novas, audio, matchmaking entre varias salas ou squads.
## Como a IA joga (versao 2)
Os tres niveis agora diferem em conhecimento, planejamento e disciplina. Nenhum recebe vida, dano, velocidade ou visao extras.

| | Iniciante | Mediano | Experiente |
|---|---|---|---|
| Conhecimento do mapa | conhece ~30% das ruinas; descobre o resto ao passar perto | conhece ~85% | conhece tudo |
| Plano | vaga numa direcao por 12-20 s ate achar ruinas; troca de ideia a cada 2 s | escolhe a ruina mais proxima; mantem 5 s | planeja a rota considerando a volta a extracao; mantem 8 s; evita nascimentos rivais nos primeiros 90 s |
| Saida para extrair | sai com 5 s de folga (pode nao chegar) | 15 s | 30 s |
| Combate | atira andando, nao usa cobertura, mira demora a assentar | para para atirar, rajadas de 4, desloca-se de lado de vez em quando | rajadas de 3, desloca-se entre elas, cobertura quando machucado, mira assenta rapido |
| Sons | ignora a maioria, nao olha para o barulho | olha na direcao do tiro | olha na direcao do tiro; machucado nao investiga |
| Cura | so com a vida baixa | quando seguro | quando seguro |

Regras comuns:
- Escolhe um destino e **mantem por um tempo** (compromisso). Entre tarefas calmas nao muda de estado mais de uma vez por segundo.
- Cada bot tem **gostos proprios por ruina**, fixos na partida: dois bots do mesmo nivel nao fazem a mesma rota.
- A extracao so abre com 60 s restantes, mas o bot **calcula o tempo de caminhada** ate a saida (distancia x 1,35 / velocidade + folga) e parte a tempo. Ao chegar, espera a ~10 m da zona e so entra quando a extracao abre. Depois de decidir sair, nao volta atras.
- Mira: o erro comeca no nivel do perfil e cai para 40% conforme o bot permanece no alvo. O cone de mira do jogo (abre andando, fecha parado) vale para os bots do mesmo jeito.
- Detecta participantes e monstro somente no cone ou raio proximo, sem parede no meio. Nao conhece a posicao de loot escondido nem de inimigos fora de vista.
- Se ficar preso por 1,5 s, desiste do alvo atual (ponto, loot, som ou saida) e replaneja.
- Ao morrer, deixa o loot no chao. Ao extrair ou encerrar a partida, para de agir.
- Nomes no jogo: Easy = Iniciante, Medium = Mediano, Hard = Experiente (BotProfile.DisplayName).

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
A compilacao Windows e as verificacoes das regras dos bots (v1) passaram na Unity; a v2 passa 76 verificacoes fora da Unity (compilacao com stubs) e precisa ser validada no editor. Conexao local, preenchimento com nove bots e movimento replicado foram verificados com cliente separado. Testes de partidas completas, balanceamento das tres dificuldades e desempenho Android continuam necessarios.

