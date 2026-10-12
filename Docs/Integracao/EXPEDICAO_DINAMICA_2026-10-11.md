# Expedição dinâmica — implementação inicial

## Como o loot funcionava

Todos os pontos de chão eram preenchidos. O índice do ponto determinava o item: rifle/munição/escopeta/cartuchos em posições recorrentes e recursos em sequência fixa. Perto dos abrigos do mapa grande, a repetição era ainda mais previsível.

Baús recebiam metal, munição de pistola, componentes, bandagem e gazua; incomuns/raros acrescentavam pólvora e uma pistola carregada. Raridade e fechadura vinham do índice da ruína. Monstros com `Monster` sempre deixavam pregos (seis com o valor padrão 30); mães vegetais sempre deixavam duas pólvoras. Itens largados por jogadores e inventários de mortos preservavam as pilhas originais.

## Regras implementadas

Tudo é sorteado no servidor quando a partida/objeto nasce ou quando o monstro morre. Abrir e fechar o mesmo baú não refaz o sorteio.

| Fonte | Chance de conter/deixar loot |
| --- | --- |
| Ponto do chão | 65% |
| Baú normal | 85% |
| Monstro derrotado / mãe vegetal | 60% |

Havendo loot: **55% uma pilha, 30% duas, 15% três**. Cada pilha tem um tipo diferente, com quantidade variável e respeito ao limite do inventário. Uma pilha de munição pode conter vários cartuchos; “três itens” significa três pilhas, não três unidades.

Por pilha: **10% arma, 30% munição, 60% suprimentos**. Baús incomuns usam 15% arma e raros 20%, mantendo munição em 30%. Suprimentos incluem madeira, pregos, metal, pólvora, componentes, bandagens e gazuas. Havendo arma, os pesos são pistola 60, Mosin 25, escopeta 15; havendo munição, pistola 50, rifle 30, cartuchos 20. Pesos são renormalizados para não repetir um tipo na mesma recompensa. Armas podem vir vazias ou com tiros até a capacidade real.

Raridade dos baús: comum 65%, incomum 25%, raro 10%. Incomuns e raros são trancados; comuns têm 30% de chance de fechadura. A chance independente de imitador permanece 10%. Baús do laboratório mantêm um conteúdo simples garantido para treino; depósito seguro da base e pilhas largadas por jogadores não passam pela tabela aleatória.

## Encontros e ruínas

- Cada um dos três pontos possíveis de encontro ao redor de uma ruína grande tem 45% de chance de monstro, com pequena variação de posição. O ponto natural do mapa de teste usa a mesma chance.
- Tipo sorteado: Investidor 12%, Ouvinte 20%, Couraçado 14%, Aranha 18%, Arauto 8%, Corrosivo 12%, Espreitador 12%, Devorador 4%. Imitadores são sorteados somente nos pontos de baú.
- Colônia vegetal: 45% por área candidata próxima de abrigo. Esporífero: 60%. Posições são sorteadas num anel de 24–38 unidades e verificadas contra colisões, bordas, abrigos e extrações. Sem espaço seguro, o encontro é omitido.
- Ruínas: casa, oficina, depósito e casa desabada; larguras, paredes quebradas, saídas e móveis diferentes, com versões espelhadas. O corredor central e o baú permanecem acessíveis em cada template. A leitura das rotas e das coberturas usa Bullet Echo como referência; materiais gastos, interiores e abandono usam Darkwood.
- A semente de layout muda por partida e é replicada pelo `MatchManager`. Cada cliente constrói as mesmas paredes/colisores do servidor. Os centros das ruínas e a distribuição macro do mapa continuam os existentes até a próxima etapa de planejamento.

## Loot legível e pesca

Ícones no chão ficam **50% maiores**, de aproximadamente 0,45 para 0,675 unidade no prefab padrão. O aumento ocorre no filho visual, preservando o raio de coleta e a escala de rede. Não foi aplicada mudança nos ícones do inventário.

Pesca pertence exclusivamente à cabana. A expedição não mostra a ação nem instala a interface de pesca; o servidor recusa/cancela a atividade e não concede capturas. Os contratos antigos de RPC e campos são mantidos inativos para referências existentes. O serviço da cabana continua usando o próprio sistema de pesca.

## Configuração e entrega

Valores em `Assets/Resources/Gameplay.asset` / `GameplayDefinition`. Regras de sorteio em `ExpeditionRolls`; layout em `RuinLayouts` e `MapDressing`. Cliente e servidor precisam da versão com protocolo **15**, pois a semente replicada acrescenta estado ao MatchManager. Nenhum servidor foi reiniciado e nenhum build foi solicitado/gerado.

Revisão estática de autoridade, sorteio único, limites, pilhas, corredores dos templates e remoção das rotas de pesca. Não foram executados testes automatizados, Play Mode, sessões Unity ou builds, conforme a preferência do projeto. Os percentuais são valores iniciais de balanceamento e precisam da avaliação prática do usuário.
