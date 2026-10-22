# Loot por fonte, mapa e risco — etapa 8

Implementado no código em 16/10/2026, com revisão estática. Não foram iniciados testes, compilação, sessões Unity ou builds. Importação dos assets, sincronização e apresentação em jogo aguardam a avaliação prática do usuário.

## Tabelas e sorteio

`LootTableDefinition` configura presença, número de pilhas, pesos de categoria, itens elegíveis, quantidades, temas e raridade por risco. Há **10 assets** em `Assets/Resources/Items/Loot/Tables`: cinco fontes para a Floresta e cinco para as Catacumbas. A Área de testes usa explicitamente as tabelas da Floresta.

| Fonte | Presença | Pilhas | Materiais | Suprimentos | Equipamento | Armas | Tesouros |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Chão | 70% | 1 | 55 | 35 | 0 | 0 | 10 |
| Suprimentos | 100% | 2 | 30 | 70 | 0 | 0 | 0 |
| Comum | 90% | 2–3 | 30 | 30 | 20 | 5 | 15 |
| Valioso/fechado | 100% | 3–4 | 20 | 30 | 1 garantido | 10 | 40 |
| Monstro | 60% | 1 | 60 | 25 | 0 | 0 | 15 |

Os pesos das categorias são relativos, não chances independentes por item. Intervalos de 2–3 e 3–4 pilhas têm distribuição uniforme, 50/50. O equipamento garantido já ocupa uma pilha e a cota de equipamento do conjunto valioso; as demais pilhas usam os pesos indicados. Materiais e munições mantêm os intervalos-base anteriores, removendo o acréscimo ligado à qualidade do recipiente.

`LootRoller` exige um servidor ativo. O contexto contém mapa, fonte, risco e tema; não recebe nível, atributos ou slots vazios do jogador. Cada conjunto contém no máximo uma arma e um equipamento. A mesma definição não é selecionada duas vezes: os pesos são renormalizados entre categorias/itens ainda elegíveis. Sem opções, o sorteio encerra sem escolher um item arbitrário. Como não há repetição e cada quantidade respeita o limite da pilha, não são geradas pilhas duplicadas de materiais no mesmo conjunto; o inventário continua consolidando pilhas compatíveis nas transferências.

As 18 peças têm peso igual entre si. Os seis modelos de arma são elegíveis nos recipientes comuns e valiosos. A distribuição de armas por modelo é:

| Mapa | Pistola | Rifle | Espingarda | Espada | Machado | Lança |
| --- | --- | --- | --- | --- | --- | --- |
| Floresta | 30 | 15 | 15 | 18 | 10 | 12 |
| Catacumbas | 15 | 10 | 10 | 30 | 15 | 20 |

Munição de pistola/rifle/espingarda usa pesos 50/30/20, com bandagens e ferramentas concorrendo no pool de suprimentos. Não há adaptação à arma carregada. Os recipientes da Floresta incluem alicate, pé de cabra e serra; as Catacumbas usam suprimentos sem essas ferramentas florestais. As receitas existentes foram preservadas.

## Fontes e riscos no mapa

Os pontos, posições e quantidades de espaços de loot existentes foram mantidos. Na Floresta, chão e recipientes comuns usam risco baixo. Armários baixos destrancados usam a tabela de suprimentos; guarda-roupas destrancados, a comum. Recipientes gerados trancados usam a tabela valiosa com risco médio. A condição fica definida na criação: destrancar ou abrir não muda a tabela nem sorteia novamente. A possibilidade existente de um ponto de baú gerar um mímico permanece.

Nas Catacumbas, os pontos comuns continuam como saque solto de risco baixo. Os baús valiosos das salas 12/19 têm risco médio, e os das salas 20–23 têm risco alto. O risco foi registrado em cada `LootSlot`, sem cálculo pela distância ou pelo nível do jogador.

| Risco | Comum | Incomum | Raro |
| --- | --- | --- | --- |
| Baixo | 80% | 19% | 1% |
| Médio | 60% | 35% | 5% |
| Alto | 35% | 50% | 15% |

Essas chances aplicam-se somente à raridade das peças. `ExpeditionRolls.ContainerGrade` mantém a qualidade visual/tempo de abertura e a dificuldade já existente das fechaduras; esse valor não determina a raridade dos exemplares. Um baú valioso pode entregar uma peça comum.

Materiais de monstros usam tema: couraçados selecionam metal/pregos; aranhas, esporíferos e plantas-mãe selecionam madeira; corrosivos selecionam pólvora/componentes; os demais usam o pool geral. Essa restrição vale para a categoria de materiais, mantendo suprimentos/tesouros elegíveis. Plantas-filhas continuam sem recompensa própria. Não foram acrescentados materiais, chefes ou elites.

## Objetivos e persistência dos resultados

Manivela e chave do canal mantêm seus pedestais e mecanismos existentes. Chaves de serviço/depósito saem do sorteio geral e ficam garantidas nos dois primeiros pontos já existentes próximos aos spawns da Floresta: índices `2 × Sites.Length` e `2 × Sites.Length + 1` da lista de saque. Esses dois pontos substituem o sorteio comum; não são criados pontos adicionais. As passagens opcionais e suas regras de consumo permanecem.

O conteúdo é preenchido no servidor durante a criação do recipiente. `LootPickup` guarda uma marca de inicialização local ao objeto para não preencher o conteúdo novamente. Abrir, cancelar, destrancar, alterar visibilidade ou reconectar não chama o sorteador do recipiente já criado. Chão e monstros sorteiam somente na criação/drop e replicam os resultados. Cada peça conserva identidade, raridade e bônus nas transferências, no descarte, na extração e no save.

Cadáveres de participantes não usam tabelas: o caminho existente derruba os itens reais da mochila e do equipamento. `ParticipantCorpse` é rejeitado pelo registro de tabelas. Não há reposição ou melhoria de conteúdo ao saquear.

Essa conservação é relativa ao ciclo da expedição existente; não foi criado um sistema novo de recuperação do mundo após reinício do processo do servidor.

## Tesouros

Seis definições editáveis em `Assets/Resources/Items/Treasure/Definitions`, IDs novos **53–58**, sem renumerar os anteriores. Todos têm pilha máxima de 5 e ícones originais desenhados em código, substituíveis pelo campo `Icon` do asset.

| Mapa | Tesouro | Valor por unidade | Peso no pool |
| --- | --- | --- | --- |
| Catacumbas | Moeda antiga | 10 moedas | 75 |
| Catacumbas | Relicário | 30 moedas | 20 |
| Catacumbas | Ídolo funerário | 80 moedas | 5 |
| Floresta | Sucata valiosa | 10 moedas | 75 |
| Floresta | Objeto antigo | 30 moedas | 20 |
| Floresta | Documento de coleção | 80 moedas | 5 |

Os drops iniciais têm uma unidade. Os valores aparecem na descrição do inventário. São itens físicos para extrair e guardar; **não creditam moedas automaticamente**. A venda será implementada na etapa 9. O laboratório existente inclui os seis itens na lista temporária.

## Compatibilidade e limites

- Protocolo **29**: atualizar cliente e servidor juntos. O schema do perfil permanece 3, e o documento compartilhado permanece 5, pois os metadados necessários já existem.
- `ExpeditionRolls.Loot` e o pool universal foram removidos. Campos antigos de porcentagem em `GameplayDefinition` permanecem somente para compatibilidade serializada e estão identificados como legados; editar os novos assets para ajustar loot.
- `Tools/Items/create_loot_definitions.py` registra o autoramento inicial das tabelas e tesouros, preservando arquivos e GUIDs existentes. Nenhum setup de cena/prefab foi executado.
- O laboratório de fechaduras mantém seu conteúdo fixo de teste. As novas regras aplicam-se aos recipientes reais dos mapas.
- A implementação segue a proposta de risco/recompensa e mantém circulação, cobertura e atmosfera dos mapas existentes, orientadas por Bullet Echo/Darkwood. Não houve redesenho de geometria ou aumento de spawns.

Próxima entrega: **etapa 9 — comércio e redistribuição de atributos**. Probabilidades, rendimento e equilíbrio econômico ainda dependem da avaliação prática; revisão estática não confirma importação Unity, RPCs ou apresentação em execução.
