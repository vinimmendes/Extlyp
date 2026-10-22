# Decoração narrativa — etapa 4: distribuição pelo mapa

Distribuição integrada nas Catacumbas, mantendo os três pilotos. Catálogo autorado com **77 composições e 160 peças preferenciais**, cobrindo as 24 salas e incluindo 28 grupos nos corredores e na galeria do canal. Estes são envelopes disponíveis na fonte, não uma contagem comprovada em execução: a arte existente ainda pode impedir um encaixe.

Veja a [planta das composições](planta-etapa4.svg) e o [levantamento completo por grupo](distribuicao-etapa4-levantamento.json). A planta representa os envelopes preferenciais, não uma captura do jogo.

## Distribuição

| Área | Tratamento |
|---|---|
| Oito entradas | Conjuntos pequenos e temáticos junto às bordas: velas, tecido, cerâmica, ossos, palha ou sedimentos. Centro e aproximação do spawn livres. |
| Galerias funerárias | Memoriais, mortalhas e cerâmica; lápides e caixões somente sobre alvenaria inacessível. |
| Ossários | Raízes rasas, ossos contidos e detritos; nichos de ossos em superfícies já bloqueadas. |
| Claustro e capela | Incensários, velas consumidas e cera; bancos nas áreas de alvenaria. |
| Prisão | Palha, utensílios e correntes nas bordas, com mobiliário apoiado fora da circulação. |
| Contrapesos | Piloto preservado; padiola adicional em alvenaria ao sul, com alternativas fixas. |
| Áreas de água | Madeira, sedimentos e umidade em piso seco. A reserva da água e os mecanismos permanecem livres. |
| Pátio das Vigílias e Sacristia | Únicas duas composições de expedição perdida: acampamento frio e mochila inutilizável, sem interação. |
| Corredores | Detalhes pequenos nas margens, fora das salas, portas e cruzamentos. Os caminhos compartilhados por mais de uma ligação não recebem grupos sobrepostos. |
| Canal | Piloto R13 preservado e duas composições laterais autoradas na galeria longa. Ponte e rio de saída sem preenchimento decorativo. |

São 49 grupos de sala (24 principais, 14 complementos sobre alvenaria e 11 secundários opcionais) e 28 de corredor/galeria. Todas as 11 famílias N01–N11 aparecem no catálogo de distribuição. Não foram produzidas novas imagens nesta etapa.

As 28 ligações da planta original não são 28 corredores independentes: há sobreposição de trechos e ligações curtas já ocupadas por salas. **22 metas opcionais de corredor foram omitidas no levantamento**, por falta de envelope seguro; IDs e motivos constam no JSON. Não reduzir reservas ou adicionar grupos em cruzamentos para atingir uma cota.

## Encaixe das composições

`Tools/Distribute-DungeonNarrative.py` lê a geometria atual, o levantamento da etapa 1, os pilotos e o catálogo de arte. Produz `NarrativeDistribution.json`, a planta e o relatório. As alternativas de cada grupo são posições fixas, ordenadas e verificadas na fonte; não existe sorteio por cliente. Os pilotos usam suas âncoras originais.

O componente existente `DungeonNarrativePilot` foi ampliado para ler essa distribuição. O nome da classe/arquivo foi mantido para preservar seu GUID; a hierarquia criada passa a se chamar `DungeonNarrativeDressing`. A chamada única em `DungeonMapGeometry` permanece após arquitetura, props, marcos e extrações, antes de Details.

Cada grupo novo tem até seis posições alternativas distintas. Antes de instanciar, o componente verifica todas as peças do grupo, reservando seus envelopes temporariamente. Se uma delas falhar, desfaz somente a reserva temporária e tenta a alternativa seguinte. Se nenhuma couber, omite o grupo inteiro, sem deixar metade da composição. As únicas sobreposições intencionais são madeira sobre sedimentos e os suportes explícitos sobre sarcófago/alvenaria existentes.

Os bounds da arte anterior e a ocupação das composições já aceitas são respeitados. Details inclui os novos renderizadores CAT na própria ocupação antes de acrescentar detalhes pequenos. Nenhum objeto anterior é removido ou desativado para forçar um encaixe. A falta de espaço é informada como mensagem normal, com IDs dos grupos omitidos; falhas de arquivo/manifesto continuam sendo avisos e preservam o cenário anterior.

## Reservas e regras preservadas

- Piso livre para peças rasas; caixões, padiola, bancos e lápides apenas sobre suporte inacessível existente. Não acrescentar colisores, cobertura ou bloqueios de visão.
- Reservas dos 59 pontos de conteúdo e dos 64 candidatos de quebráveis, incluindo os inativos da partida.
- Spawns, loot, encontros, pedestais, mecanismos e extrações; aproximações de portas, cruzamentos e áreas centrais das salas.
- Correntes, embarque, ponte da comporta, água permanente e varredura da barca para sul.
- `CanalReserved` permanece nos distribuidores automáticos. As exceções são restritas aos envelopes secos da sala 13 e às duas composições explicitamente identificadas dentro de `CanalDetour`. Os grupos da galeria não podem invadir salas.
- Os 48 quebráveis ativos, seleção/autoridade do servidor, chance de loot, saves e protocolo não foram alterados. As cenas de expedição perdida não fornecem recursos nem segurança.

## Revisão realizada

- Levantamento estático de todas as alternativas: suporte, água, limites de sala, portas, pontos protegidos, candidatos de quebráveis, sobreposição entre peças, separação entre composições e cruzamentos dos corredores.
- Referências Resources existentes, IDs de grupo únicos, catálogo das 11 famílias e manifesto de distribuição com GUID próprio.
- Compilação C# isolada usando as referências atuais da Unity 6000.0.84f1, sem iniciar o Editor: nenhum erro.
- Planta SVG e JSON conferidos como documentos legíveis. Nenhuma geometria, regra de rede ou HUD alterada.

Não foram iniciados Unity, testes automatizados, builds ou capturas em execução. O levantamento não confirma quantos grupos serão aceitos pelos bounds nativos de cada asset, nem avalia iluminação/fog ou desempenho.

## Avaliação prática seguinte

Iniciar uma nova expedição e observar entradas, salas internas, corredores e canal. Conferir se as composições são reconhecíveis, os móveis parecem apoiados e as peças rasas não parecem obstáculos. Verificar combate, interações e extrações, incluindo a saída da barca. Se uma área permanecer vazia, usar os IDs dos grupos omitidos para ajustar suas âncoras na etapa 5, sem relaxar as reservas.
