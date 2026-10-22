# Equipamentos — etapa 4: combate corpo a corpo

Implementação em 16/10/2026, com revisão estática e validação prática pendente. Unity 6000.0.84f1, NGO 2.13.3 e Transport 2.7.4 preservados.

## Armas

| Arma | Dano | Alcance | Arco total | Preparação | Recuperação | Stamina |
| --- | --- | --- | --- | --- | --- | --- |
| Espada curta | 22 | 1,5 u | 70° | 0,25 s | 0,45 s | 15 |
| Machado | 32 | 1,4 u | 85° | 0,40 s | 0,65 s | 23 |
| Lança | 20 | 2,2 u | 30° | 0,30 s | 0,50 s | 14 |

Os três IDs foram acrescentados ao final de `ItemId`, preservando IDs anteriores, identidade dos exemplares e compatibilidade do formato de inventário. São armas dos slots A/B, com limite de uma unidade por espaço. Não usam munição ou recarga. Entram no fluxo existente de equipar, guardar, derrubar e extrair. A distribuição em loot normal permanece na etapa 8.

Trocas envolvendo uma arma melee têm intervalo de 0,45 s. A troca normal não pode interromper preparação ou recuperação para acelerar o ataque seguinte.

No laboratório, abra **Equipar**, escolha uma das seis armas e use **Equipar arma + suprimentos**. Esse comando substitui o conjunto de armas e mochila de teste. Para testar A/B com duas armas, use o seletor de itens para entregar outra arma e equipe-a pelo inventário. **Restaurar** também repõe stamina. As ferramentas continuam exclusivas da sessão de testes.

## Comando e resolução

O mesmo clique/gatilho/botão de ataque usa a arma ativa. Segurar permite repetir após o intervalo próprio da arma. O servidor aceita o início apenas com personagem vivo, partida em andamento, arma válida, stamina suficiente e sem outra ação bloqueante. Pedidos inválidos não consomem stamina. Atacar encerra a corrida antes de iniciar o golpe.

Ao aceitar, o servidor consome o custo, captura slot e exemplar e publica início, impacto, fim e direção. A direção pode acompanhar entradas novas durante a preparação; pacotes de movimento anteriores ao pedido de ataque não substituem sua direção inicial. Ao terminar a preparação, a direção fica travada até acabar a recuperação. O movimento normal continua, mas corrida, recarga, troca A/B e troca de equipamentos ficam bloqueadas durante o golpe.

O impacto ocorre uma única vez após a preparação. Distância entre centros, arco e linha física até o centro do alvo são reavaliados na posição atual; paredes e obstáculos baixos bloqueiam o dano. Entre alvos elegíveis, atinge no máximo três entidades, priorizando proximidade e usando ID de rede como desempate. Um conjunto de IDs impede dano repetido à mesma entidade, independentemente da quantidade de colliders/componentes. Jogadores vivos, monstros e plantas hostis usam a mesma regra. Mantém o PvP existente; o projeto não possui facções de participantes que exijam um filtro adicional de aliados nesta etapa.

Morte, saída da partida ou mudança forçada do exemplar cancela o golpe sem devolver stamina nem eliminar o cooldown já reservado. A recuperação impede corrida e regeneração; permanece também a espera de 1,5 s desde o gasto. Mecanismos, fechaduras, saque e extração não avançam em paralelo ao ataque. Golpes errados ou bloqueados por parede continuam gastando o custo aceito.

## Apresentação

- HUD existente muda **ATIRAR** para **ATACAR**, com ícone da arma; o painel mostra dano e custo em stamina. Recarga fica indisponível para melee. Preserva layout, área segura e temas Simples/Sombria.
- Inventário identifica armas corpo a corpo sem exibir carregador 0/0; comparação também informa dano, alcance e custo.
- Mira usa alcance e arco fixos da arma, sem dispersão de tiro. Ao golpear, um arco local indica a preparação e desaparece na recuperação, com raios limitados pelos obstáculos.
- Silhuetas originais e provisórias, desenhadas por código, servem como ícone, item no chão e arma na mão. A arma faz preparação, corte/estocada e recuperação pelos tempos replicados. Visibilidade acompanha o corpo e a neblina existentes.
- Os sprites de corpo existentes são preservados. Esta entrega não inclui novos atlas de animação corporal no Blender; o acabamento de braços/corpo dos três golpes fica pendente de produção visual.

Sem esquiva, bloqueio, combos, empurrão ou atributos adicionais nesta etapa. O dano ainda é o valor-base; Força e os demais atributos entram na etapa 5. Bots conservam seu equipamento e comportamento de armas de fogo; não foi criada uma nova IA melee.

## Compatibilidade e revisão

Protocolo **24 → 25**, pelos novos campos replicados de ataque. Cliente e servidor precisam ser atualizados juntos. Não houve migração de perfis reais, reinício de servidor ou criação de build.

Revisão estática de tempos, custo, cancelamento, limites por alvo, parede, slots, recarga, corrida, UI e catálogo. O diagnóstico existente de armas do laboratório foi adaptado para não exigir munição nas três armas novas; não foi executado. Nenhum teste automatizado, compilação manual, sessão Unity ou build de validação iniciado, conforme AGENTS.md. A implementação não está validada em execução.

Avaliação prática sugerida: testar cada arma no laboratório, comparar preparação e recuperação, tentar repetir sem stamina, errar, golpear através de parede/caixa, atingir um grupo de quatro inimigos, girar durante preparação/recuperação, trocar A/B, morrer preparando e conferir cliente separado e celular. Confirmar também que as armas de fogo mantêm disparo, munição e recarga.
