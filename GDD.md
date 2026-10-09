# Cyber Extraction - Documento de Design (MVP)

Versao 0.1 - 08/10/2026

Atualizacao do prototipo: servidor dedicado no PC, partidas de 5 participantes com preenchimento por bots apos 30 segundos e acampamento pessoal jogavel. Madeira (M), pregos (P) e bandagens (B) substituem o loot generico. Reparo do pier, pesca e buff Bem alimentado definem o inicio da meta-progressao. Regras e limites atuais estao em CAMP_ITEMS.md e SERVER_SETUP.md; as secoes originais abaixo documentam o MVP anterior.

O servidor permite ate tres salas simultaneas. A partida encerra assim que nao resta humano ativo: morte, extracao ou desconexao do ultimo humano impedem que a partida continue somente com bots.

## 1. Visao
Jogo de extracao multiplayer para Android, vista de cima (2D), com cenario pos-apocaliptico e fortes raizes em Darkwood: escuridao, visao limitada, tensao constante. Voce entra num mundo hostil, coleta loot, sobrevive a monstros e a outros jogadores e precisa extrair vivo.

## 2. Pilares
1. **Escuridao e visao limitada:** voce so ve o que esta no seu cone de luz. O resto e ameaca.
2. **Tensao, nao poder:** jogadores sao frageis. Parar para mirar e arriscado, mas preciso.
3. **Mundo hostil:** o monstro e o ambiente sao tao perigosos quanto os outros jogadores. Barulho atrai problemas.
4. **Risco e recompensa:** o loot so vale se voce extrair. Morreu, o loot cai no corpo.

## 3. Loop da partida
1. Jogador entra na fase (nasce em uma casa).
2. Procura loot.
3. Tenta sobreviver aos monstros e aos outros players.
4. Extrai.
5. Fim.

## 4. Regras do MVP
- Partida de **5 minutos**.
- **1 jogador vs 1 jogador vs 1 monstro** (co-op contra jogadores e PvPvE vem depois).
- **Um unico ponto de extracao.** O jogador precisa ficar nele por 3 segundos.
- Nao ha zona segura. A casa so e segura no inicio porque ninguem inimigo nasce nela.
- Ao morrer, o jogador **perde o loot, que cai no corpo** e pode ser pego por outro.
- Fim para cada jogador: **extraiu** (vitoria), **morreu** ou **tempo esgotado** (derrota).
- A partida so comeca quando os dois jogadores estao conectados.

## 5. Mapa
Vista de cima, 30 x 46 unidades.
- **Casa A** (topo) e **Casa B** (base), cada uma com uma porta virada para o centro. Cada jogador nasce numa casa.
- **Monstro** no centro, entre duas paredes paralelas. Ha caminhos livres dos dois lados, entao os jogadores podem enfrentar o monstro, enfrentar um ao outro ou nem se ver.
- **Loot:** 4 pontos nos cantos (valor 10 cada). O monstro tambem solta loot ao morrer (valor 30).
- **Extracao:** area no lado direito, equidistante das duas casas.
- **Cobertura:** 4 caixas espalhadas.

## 6. Jogador
| Atributo | Valor |
|---|---|
| Vida | 100 |
| Velocidade | 4 u/s |
| Dano por tiro | 20 |
| Cadencia | 1 tiro a cada 0,35 s |

## 7. Combate: cones de visao e mira (inspirado em Darkwood)
- **Cone de visao (grande):** 100 graus, alcance 9. Fora dele, o mundo fica escuro. Ha um pequeno raio de percepcao (1,8) ao redor do jogador. Paredes bloqueiam a visao.
- **Cone de mira (pequeno):** duas linhas dentro do cone de visao. Meio angulo de **5 graus parado** e **15 graus andando**.
- **Tiro:** ignora colisores trigger, incluindo loot e area de extracao; paredes, caixas e alvos continuam bloqueando. A direcao e **sorteada dentro do cone de mira**. Quanto mais aberto o cone, mais impreciso. O servidor faz o raycast e aplica o dano.
- **Inimigos fora do cone de visao ficam escondidos.**
- **Barulho:** cada tiro emite barulho (raio 14) que atrai o monstro.

## 8. Monstro (MVP)
- Vida 150. Fica perto do centro, vagando.
- Se ouve um tiro, vai investigar o local por alguns segundos.
- Se ve um jogador (raio 6, sem parede no meio), persegue e ataca (15 de dano a cada 0,8 s).
- Navegacao A* no mapa fixo: contorna paredes e caixas considerando o raio do corpo. Recalcula a rota a cada 0,5 s quando o caminho direto esta bloqueado.
- Ao perder um jogador de vista, investiga a ultima posicao vista por ate 6 segundos; nao acompanha sua posicao escondida.

## 9. Controles (Android)
- **Metade esquerda da tela:** joystick de movimento.
- **Metade direita da tela:** joystick de mira. Arrastar aponta o cone sem atirar.
- **Botao ATIRAR (canto inferior direito):** segurar dispara na direcao da mira atual. E possivel soltar a mira e disparar depois; com movimento, mire simultaneamente para manter a direcao desejada.
- No editor: WASD, mouse para mirar e botao esquerdo para atirar.

## 10. Rede
- Unity 6 + Netcode for GameObjects, Unity Transport (UDP).
- Um jogador cria a partida (Host) e o outro entra pelo IP (mesma rede Wi-Fi).
- Movimento: autoritativo no proprio jogador (simples, para o MVP).
- Tiro, dano, loot, extracao e regras da partida: autoritativos no servidor.
- Risco conhecido: o movimento pode ser trapaceado. Em fase posterior, movimento passa a ser validado no servidor e a visibilidade filtrada no servidor.

## 11. Roteiro
1. Um jogador, sem rede. (feito)
2. Dois jogadores em rede.
3. Cone de visao e mira.
4. Vida e tiro (servidor).
5. Monstro com IA simples.
6. Extracao e fim de partida.
(os passos 2 a 6 estao no pacote atual; falta testar um a um)

Depois do MVP: base entre expedicoes, crafting, mais monstros, mais armas, inventario, matchmaking online, arte e som pos-apocalipticos.

## 12. Fora de escopo (por enquanto)
Base e meta-progressao, crafting, inventario com grade, varias armas, mais de um monstro, servidor dedicado, matchmaking, anti-cheat, arte final e audio.

## 13. Decisoes em aberto
- Numero final de jogadores por partida (squads de 2 a 4).
- Como os jogadores cooperam dentro do squad.
- Estilo de arte e paleta final.

## 14. Bots jogadores — implementacao inicial

O menu do host permite Sem bot, Facil, Medio e Dificil. Sem bot conserva a partida entre humanos. Com bot, um host humano joga contra um bot que nasce na casa B; a partida comeca sem esperar outro humano. Bot e participante, nao monstro. Pode coletar, lutar com humanos/monstros, morrer e deixar loot, ou extrair. O monstro tambem detecta e ataca bots.

A IA roda no servidor e usa as regras existentes de vida, velocidade, dano, dispersao, cadencia minima, visao e extracao. Busca caminhos A*, observa pelo cone/raio proximo com paredes bloqueando a visao, ouve tiros por distancia e guarda apenas informacoes percebidas. Conhece o mapa fixo e os pontos possiveis de loot/extracao; so conhece a existencia e valor de um loot depois de observa-lo. Loot removido fora da visao so e esquecido quando o bot volta a observar o local.

| Nivel | Reacao | Erro adicional de mira | Vida para recuar | Memoria | Objetivo de loot |
|---|---|---|---|---|---|
| Facil | 0,90 s | +/-14 graus | 20% | 2 s | 10 |
| Medio | 0,55 s | +/-7 graus | 35% | 4 s | 20 |
| Dificil | 0,25 s | +/-3 graus | 45% | 6 s | 30 |

Facil combate de forma mais direta e ignora mais sons. Medio alterna combate, coleta e posicoes proximas de cobertura. Dificil prioriza posicoes de tiro com cobertura proxima, reage antes e recua com mais cautela. Nenhum nivel ganha dano, vida, velocidade ou visao extra. A dispersao normal da arma continua somada ao erro de mira da IA.

Prazos e sobrevivencia podem antecipar a extracao. Monstro em alcance perigoso de melee provoca recuo. Sons nao permitem atirar em alvos invisiveis; alvo perdido vira investigacao da ultima posicao vista. Sao sete estados: Idle, Explore, Collect, Investigate, Engage, Retreat, Extract.

Os numeros sao iniciais para balanceamento em testes, nao garantia de dificuldade competitiva final. A navegacao usa o mapa estatico atual; paredes movidas em execucao precisariam de atualizacao da grade.
