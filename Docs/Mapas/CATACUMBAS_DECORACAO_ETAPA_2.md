# Decoração — etapa 2: arte e posições dos quebráveis

Continuação implementada: [etapa 3 — quebra e sincronização](CATACUMBAS_DECORACAO_ETAPA_3.md). A descrição abaixo registra a preparação da segunda etapa.

Preparados nove modelos (três urnas, três caixotes, três barris pequenos), cada um com estado intacto e destroços: 18 estados visuais. Nenhum objeto de gameplay é instanciado nesta etapa. Acertos, sincronização, drops e a seleção de 48 dos 64 candidatos pertencem às etapas seguintes.

## Arte

- Urnas: reutilizados CAT-P05_v01–03 e destroços CAT-P10_v01–03 com os recortes do manifesto original. Sem sobrescrever assets existentes.
- Caixotes e barris: seis PNGs RGBA novos, um por variante, com duas células iguais lado a lado: intacto à esquerda, destroços à direita. Arte original vista de cima, materiais úmidos, madeira gasta, ferro fosco, sem brilho de loot e sem cenário.
- Preferência confirmada pelo usuário durante a produção: manter a leve inclinação das versões originais, que mostra um pouco do volume lateral. Revisões com vista totalmente vertical não foram selecionadas. Esta preferência prevalece sobre a câmera estritamente ortográfica descrita nos prompts iniciais.
- Geração pela ferramenta integrada image_gen. Fontes e prompts em `Art/Catacumbas/Quebraveis`; cópias de runtime e catálogo em `Assets/Resources/Environment/Dungeon/Breakables`. Referências: abandono de Darkwood, legibilidade de Bullet Echo e materiais próprios das Catacumbas.
- Catálogo registra família, variante, estado, recurso e recorte normalizado (origem inferior esquerda). Caixote/barril: recortes (0,0,0.5,1) e (0.5,0,0.5,1). Pivô de apresentação previsto: centro da célula. Importação limita os atlas a 512 px, bilinear, Clamp e alpha, sem mipmaps, mantendo as fontes completas.

## Posições candidatas

64 posições fixas: oito nas entradas (uma por sala), 48 nas salas internas (três por sala) e oito nos corredores. Cada entrada tem ID, grupo, sala, célula, posição mundial, família, variante e rotação. Pegada prevista de 0,9 unidade.

Mapa de revisão das posições: `Art/Catacumbas/Quebraveis/Posicoes.svg`, com IDs correspondentes ao catálogo. Representa a geometria e os candidatos; não substitui a avaliação visual em cena.

O script de autoria `Tools/Author-DungeonBreakables.py` lê a geometria fixa atual e produz o catálogo. Exclui água, pilares, coberturas, aproximações de portas, spawns, loot, inimigos, mecanismos, pedestais, extrações, percurso do barco e os decalques de cerâmica conhecidos. Afastamento mínimo de 2,6 unidades entre candidatos. Se o mapa ou as reservas mudarem, revisar e regenerar os candidatos; não é um distribuidor aleatório de runtime.

`DungeonBreakableLayout.Load()` expõe os dados para as próximas etapas. O distribuidor DungeonDetails já preserva a pegada desses candidatos, evitando sobreposição dos detalhes de piso e dos agrupamentos da etapa 1. Isso pode reduzir a quantidade de decoração estática onde não houver outra posição segura; as reservas têm prioridade. A camada de decoração já informa as contagens abaixo do alvo.

## Revisão e avaliação

Revisão estática da geometria de autoria, composição 8/48/8, manifestos, arquivos e sintaxe C#. Não foram iniciados Unity, testes automatizados ou builds. O script de autoria gera dados; não reproduz serialização, colisão, rede ou apresentação nativa da Unity. Checagem final de escala, iluminação, encaixe visual com a arquitetura e posições em cena permanece pendente.

Nesta etapa os novos caixotes/barris ficam preparados no catálogo; não aparecem no mapa como objetos falsamente interativos. A etapa 3 adicionará prefab, apresentação e quebra autoritativa. A etapa 4 ativará a seleção de 48 objetos e a chance de loot aprovada de 10%.
