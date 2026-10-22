# IA dos monstros — 12/10/2026

Implementação dos nove pontos aprovados. Regras no servidor, sem recriar cenas/prefabs ou alterar os atributos básicos dos arquétipos. Charger permanece em 38 unidades/s.

1. Projéteis alertam o monstro para a origem recebida em ProjectileDamage, inclusive armas silenciosas. Guarda apenas uma posição; não identifica nem acompanha o atacante escondido. Mimic atingido se revela antes de receber o alerta.
2. Percepção visual de 140 graus, proximidade omnidirecional de até 2 unidades (limitada ao alcance próprio), paredes/armários/portas bloqueiam visão. Listener conserva alcance visual curto e audição ampliada. Histerese favorece o alvo atual, com preferência maior no primeiro segundo; Stalker mantém seleção por isolamento e iluminação observados. Movimento e habilidades usam snapshots da percepção.
3. Perseguição limitada em torno de casa: 34 unidades normalmente, 55 para Listener e 75 para Stalker. Investiga a última posição, busca por 3 segundos com direção de fuga observada e volta. Retorno não cura, teleporta ou revela o jogador. Alvos além da área não são adquiridos.
4. Dois segundos sem progresso iniciam nova navegação e tentativa lateral. Falha repetida num intervalo curto limpa a perseguição e inicia retorno. Preparação de habilidades, golpes, recuperação e dormência não contam como travamento.
5. Golpe comum prepara por 0,3 segundo, trava sua direção e exibe mancha laranja/vermelha à frente. No impacto exige alvo vivo, alcance, direção e caminho físico livre. Errar consome cooldown. Charger inicia investida apenas entre 5 unidades e o alcance calculado (19 atualmente), com varredura do corpo até o alvo; revalida no fim da preparação. De perto usa golpe comum. Colisões durante o avanço preservadas.
6. Separação leve entre monstros na movimentação comum, limitada e sem acrescentar velocidade. Não altera o avanço do Charger nem remove colisões. A direção corrigida respeita obstáculos; não é coordenação de esquadrão.
7. Investigação/busca/retorno usam 70% da velocidade de perseguição em todos os arquétipos. Patrulha usa patrolSpeed.
8. Percepção em 8 Hz com início aleatório. Ativação por proximidade verificada a cada 0,5 segundo. Eventos de alerta podem antecipar a próxima observação. Movimento, validação de golpes e colisão da investida permanecem em FixedUpdate. Nenhum ganho de desempenho medido.
9. Stalkers e 35% dos Listeners patrulham entre casa e uma ruína próxima, de 8 a 50 unidades, quando existe uma elegível. Outros conservam ronda local. Navegação existente segue rotas/passagens. Dormência longe de participantes continua vigente.

## Integração

`MonsterBehaviour.cs` reúne percepção, memória, busca, retorno, golpes e movimentação. `Monster.cs` e `MonsterArchetypes.cs` usam essa lógica preservando suas habilidades. `MonsterAppearance.cs` apresenta o aviso de golpe apenas a clientes que enxergam o monstro; o aviso respeita a escuridão.

Novo protocolo **17** por inclusão de variáveis de rede do golpe. Cliente e servidor precisam ser recompilados juntos. Não houve atualização do executável instalado, builds ou reinício de salas.

Revisão estática dos fluxos e `git diff --check`; nenhum teste automático ou sessão Unity executado, conforme preferência do usuário. A alteração cobre os componentes Monster; ataques da colônia vegetal (PlantCreature) são um sistema separado e permanecem como estavam. Em partida, observar esquiva, retorno após perseguição, cantos estreitos com grupos, patrulhas e cancelamento de investida por cobertura.
