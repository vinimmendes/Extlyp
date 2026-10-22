# Iluminação — problema visual pendente

Registrado em 15/10/2026 a pedido do usuário. Status: aberto após novo teste do usuário; ainda há serrilhado na sombra mesmo após a correção aplicada em 15/10/2026.

## Retorno após a correção — serrilhado persistente

Em 15/10/2026, o usuário informou que ainda há um serrilhado na sombra que precisa ser corrigido. A nova captura mostra recortes angulares na transição de luz/sombra junto às laterais do corredor. A alteração anterior não encerra o problema visual.

![Serrilhado persistente na sombra após a correção](Auditoria/Evidencias/sombra-serrilhado-persistente-2026-10-15.png)

Pendente: investigar e corrigir o serrilhado restante, mantendo a oclusão pelas paredes e uma transição visual contínua. Evidência fornecida pelo usuário; causa residual ainda não determinada e comportamento não reproduzido pelo agente. Este retorno apenas atualiza o registro: nenhuma nova alteração de código, teste ou build executado.

## Faixas e recortes junto à parede

Na captura enviada pelo usuário, uma parede vertical separa uma área iluminada à esquerda de uma área mais escura à direita. Ao longo da parede aparecem várias faixas triangulares alternadas de luz/sombra projetadas para a direita, além de transições retas abruptas na iluminação. O resultado apresenta um padrão serrilhado/fragmentado junto à parede.

Comportamento esperado: oclusão coerente e contínua pela parede, sem faixas repetidas ou recortes artificiais na luz. Preservar a atmosfera escura e a legibilidade do cenário.

![Problema de luz junto à parede](Auditoria/Evidencias/luz-parede-faixas-2026-10-15.png)

Evidência: imagem do teste do usuário, preservada no repositório. Não reproduzido pelo agente; causa ainda não investigada. Mapa exato, posição do jogador, plataforma e condições para reprodução não foram informados. A imagem isolada não permite atribuir a falha ao shader, à malha de visão ou aos obstáculos.

Investigação posterior por leitura: `DungeonMapGeometry` monta alvenaria em blocos horizontais, um por linha. `PlayerVision` usava a saída do raio pelo bloco atingido; essa saída varia entre as bordas laterais e horizontais de cada linha, produzindo profundidades de luz diferentes ao longo de uma parede contínua. Nas Catacumbas, a oclusão agora começa na primeira face atingida. O shader e os valores dos demais mapas foram preservados.

Compilação C# concluída sem erros, sem execução Unity ou build de jogador. A causa é sustentada pela geometria/código; a imagem original ainda não foi reproduzida pelo agente. Conferir a mesma parede em partida, inclusive ao caminhar e girar a mira. Ver [registro da correção](Mapas/CATACUMBAS_RELATOS_2026-10-15.md).
