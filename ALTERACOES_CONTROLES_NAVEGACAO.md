# CyberExtraction — atualizacao pontual

Escopo: apenas tiro atravessando loot/extracao, mira silenciosa e navegacao do monstro.

## Instalacao
Substitua Assets/Scripts pelo conteudo atualizado. A nova classe MonsterNavigation.cs tambem deve ser copiada. Nenhum componente adicional precisa ser colocado na cena: Monster cria a navegacao no servidor. A cena e os prefabs existentes podem ser mantidos.

## Controles
- Celular: esquerda move; direita mira sem disparar; segurar ATIRAR dispara.
- Soltar ATIRAR ou arrastar o dedo para fora do botao interrompe os tiros.
- Para mover, ajustar a mira e disparar simultaneamente, sao necessarios tres toques. Com dois dedos, pode-se mover e mirar, ou mover e disparar na direcao previamente ajustada.
- Enquanto o botao esta pressionado, o movimento nao substitui a direcao da mira.
- PC: WASD move; mouse mira; botao esquerdo dispara.

## Navegacao
A* em grade de 0,5 unidade, limitada ao mapa fixo atual (30 x 46). Considera o raio do CircleCollider2D e sua escala. Arestas e atalhos usam CircleCast contra WallMask. Sem rota livre, o monstro para em vez de atravessar paredes. Caminhos bloqueados sao recalculados no maximo duas vezes por segundo, com nova tentativa ao detectar falta de progresso. A grade e os trechos entre celulas sao armazenados para os obstaculos estaticos deste MVP; paredes/caixas movidas ou adicionadas durante a partida exigiriam reconstruir essa grade.

Ao perder visao, o monstro navega ate a ultima posicao vista por ate 6 segundos, preservando a regra de nao ver atraves de paredes. Selecione o monstro na Scene View para ver a rota em ciano com Gizmos ativados.

## Verificacao no Unity (pendente)
1. PlayersToStart=1: mirar no celular deve girar o cone sem tiro, dano ou evento de barulho. Segurar ATIRAR dispara; soltar para. Testar movimento e mira juntos, cancelamento de toque e retorno do aplicativo ao foco.
2. Atirar contra um alvo com loot no meio. O alvo recebe dano. Repetir passando pela area de extracao (antes de concluir os 3 s). Testar tambem com Physics2D.queriesHitTriggers ativado e desativado: o resultado do tiro deve ser o mesmo.
3. Repetir com parede ou caixa na frente: o alvo nao recebe dano. Coleta e extracao devem continuar funcionando normalmente.
4. Produzir barulho do outro lado de uma parede central ou caixa: monstro deve escolher um caminho livre, sem atravessar o obstaculo ou prender na quina.
5. Atrair o monstro para uma casa: deve contornar a parede e entrar pela abertura. Esconder-se atras de uma quina apos ser visto: ele investiga a ultima posicao vista.
6. Repetir com host e cliente em dois aparelhos, observando os mesmos impactos e movimento do monstro.

Revisao do codigo e integridade do ZIP verificadas fora da Unity. Compilacao, fisica em execucao, navegacao visual e multitouch em aparelhos ainda precisam ser validados no editor/aparelhos.
