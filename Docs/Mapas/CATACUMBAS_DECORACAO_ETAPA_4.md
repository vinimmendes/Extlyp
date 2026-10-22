# Decoração — etapa 4: distribuição variável e loot

As Catacumbas mantêm 48 quebráveis por expedição: oito nas entradas, 32 nas salas internas (dois de três candidatos por sala) e oito nos corredores. O servidor sorteia um candidato omitido por sala usando uma sequência própria derivada do LayoutSeed da expedição. Posições, variantes e arte continuam as do catálogo aprovado. A seleção pode coincidir parcialmente entre partidas; não há promessa de alternância obrigatória.

## Recompensas

Cada objeto ativo tem 10% de chance de uma única pilha comum. Recompensa sorteada no servidor antes do spawn e armazenada em campos privados do objeto, sem NetworkVariable de conteúdo, sem persistência e sem alteração do ScriptableObject. A sequência de loot tem seed local do servidor, independente da seleção de posições e do seed público do mapa. Mudar o balanceamento das recompensas não muda a seleção de candidatos.

Configuração editável: `Assets/Resources/Environment/Dungeon/Breakables/BreakableLoot.asset`, ScriptableObject DungeonBreakableLoot. ChancePercent controla presença; pesos são relativos entre os oito recursos. Quantidades são inteiras inclusivas.

| Recurso | Peso inicial | Quantidade |
| --- | --- | --- |
| Madeira | 20 | 2–4 |
| Pregos | 15 | 2–4 |
| Metal | 15 | 1–2 |
| Munição de pistola | 15 | 2–4 |
| Pólvora | 10 | 1 |
| Componentes | 10 | 1 |
| Bandagem | 10 | 1 |
| Munição de rifle | 5 | 1–2 |

Resultado esperado: 4,8 pilhas por expedição em média, sem mínimo garantido. Não inclui armas, equipamentos, tesouros, chaves, moedas ou progressão. Configuração valida os oito recursos distintos, pesos positivos limitados, quantidades válidas e chance de 0 a 100. Configuração ausente/inválida mantém os objetos e desabilita suas recompensas com aviso.

## Entrega única

ConfigureReward aceita configuração uma única vez, antes do spawn, somente no servidor. ServerBreak confirma objeto intacto em partida Running, grava o estado quebrado e marca a recompensa como consumida antes de criar o pickup. Pellets/acertos seguintes e mudança de observador não refazem o sorteio nem a entrega.

A entrega usa MatchManager.ServerDropStack, sem ignoreObject e sem ruído adicional. LootPickup mantém as regras existentes de alcance e inventário: o pickup só é removido após ServerAddStack bem-sucedido; mochila cheia conserva o recurso no mapa. Nenhum item é enviado à base ou à reserva de migração. Objetos e pickups seguem a limpeza existente no reset da sala.

O catálogo de candidatos também valida IDs contíguos, posição/célula, grupos e composição por sala, impedindo indexação inválida e distribuições incompletas. Nenhum novo dado foi acrescentado ao protocolo ou ao save; mantém protocolo 37 da etapa 3.

## Revisão e avaliação pendente

Revisão estática: sintaxe dos quatro scripts, vínculo asset/script, tabela aprovada, contagem de candidatos e caminhos de geração/consumo/coleta. Não foram iniciados Unity, testes automatizados ou builds. Isso não valida importação nativa, probabilidades observadas em partidas, RPCs gerados ou execução em rede.

Aceite prático: 48 objetos por expedição; duas posições distintas por sala interna; variação entre expedições; loot somente na primeira quebra; acertos simultâneos sem duplicação; ausência de drops proibidos; mochila cheia preservando pickup; reentrada na visão sem nova recompensa; reset limpando objetos/pickups. Não avaliar os 10% por uma única partida: a chance é independente, não uma cota.

A etapa 5 permanece pendente para revisão final de apresentação, referências, documentação consolidada e ajustes a partir da avaliação prática.
