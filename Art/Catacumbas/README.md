# Catacumbas — arte

A etapa 3.1 entrega a prancha esquemática `direcao-visual-v1.svg`, a direção visual e o catálogo em `Docs/Mapas`. Não contém sprites finais nem altera o cenário em execução.

A prancha foi construída em SVG com formas, texto e cores; não usa imagem gerada por IA nem apresenta um render como se fosse uma captura. Assets existentes consultados: atlas AbandonedProps e prancha aprovada do baú de viagem, além de metadados e carregadores dos recursos atuais.

As próximas subetapas produzem os lotes do catálogo. Guardar aqui fontes, prompts e versões adicionais; preservar os originais e integrar PNGs finais somente quando forem produzidos.

## Etapa 3.2

40 variantes de pisos e água em `Pisos/Fontes`; prompts exatos e manifesto reproduzível em `Pisos/prompts-v1.json`. Geradas com a ferramenta integrada imagegen, uma chamada por variante. Galeria de amostras repetidas: `Pisos/index.html` (não é captura do jogo). PNGs de runtime em `Assets/Resources/Environment/Dungeon/Floors`; integração cosmética por `DungeonTerrain`. Detalhes e limites em `Docs/Mapas/CATACUMBAS_ETAPA_3_2.md`.

## Etapa 3.3

32 variantes de arquitetura em `Arquitetura/Fontes`; prompts exatos em `Arquitetura/prompts-v1.json`; galeria em `Arquitetura/index.html` (arte isolada, sem captura Unity). Recursos em `Assets/Resources/Environment/Dungeon/Architecture`; carregamento por `DungeonArchitecture`, com recortes e orientações no manifesto `ArchitectureArt.json`. Fontes geradas com imagegen integrado, uma chamada por variante, preservadas sem edição de pixels.

## Etapa 3.4

28 variantes de sepulturas e coberturas em `Sepulturas/Fontes`; prompts exatos em `Sepulturas/prompts-v1.json`; galeria em `Sepulturas/index.html` (arte isolada, sem captura Unity). Recursos em `Assets/Resources/Environment/Dungeon/Props`; integracao cosmetica por `DungeonProps`, preservando as pegadas e colisoes existentes. Fontes RGBA intactas; preparacao por `Tools/Prepare-DungeonProps.py`. Detalhes e limites em `Docs/Mapas/CATACUMBAS_ETAPA_3_4.md`.

## Etapa 3.5

15 imagens de identidade em `Setores/Fontes`; prompts exatos em `Setores/prompts-v1.json`; galeria em `Setores/index.html` (arte isolada, sem captura Unity). Recursos em `Assets/Resources/Environment/Dungeon/Sectors`, incluindo `SectorsArt.json`; integracao cosmetica por `DungeonSectorArt`. Oito relevos planos de entrada e sete marcos sobre alvenaria existente, sem alterar rotas ou colisores. Fontes RGBA intactas; preparacao por `Tools/Prepare-DungeonSectors.py`. Detalhes e limites em `Docs/Mapas/CATACUMBAS_ETAPA_3_5.md`.

## Etapa 3.6

23 PNGs selecionados de extracoes, mecanismos, ferramentas e sinalizacao em `Extracoes/Fontes`, com revisao adicional da escadaria preservada. Prompts em `Extracoes/prompts-v1.json` e arquivos individuais de `Extracoes/Prompts`; galeria em `Extracoes/index.html` (fontes isoladas, sem captura Unity). Imagegen integrado, uma chamada por variante e uma edicao dirigida. Recursos em `Assets/Resources/Environment/Dungeon/Exits`, icones em `Assets/Resources/Items/Dungeon`; manifesto com 37 regioes, incluindo dois conjuntos de oito quadros. Integracao por `DungeonExtractionArt`/`DungeonToolArt`, preservando regras do servidor e movimento existente. Preparacao por `Tools/Prepare-DungeonExtractions.py`; detalhes em `Docs/Mapas/CATACUMBAS_ETAPA_3_6.md`.

## Etapa 3.7

37 variantes de detalhes e atmosfera em `Detalhes/Fontes`; prompts exatos em `Detalhes/prompts-v1.json`; galeria em `Detalhes/index.html` (arte isolada, sem captura Unity). Imagegen integrado, uma chamada por variante; fontes RGBA intactas. Recursos em `Assets/Resources/Environment/Dungeon/Details`, incluindo `DetailsArt.json`; montagem esparsa por `DungeonDetails`, com reservas para combate e objetivos. Preparacao por `Tools/Prepare-DungeonDetails.py`; detalhes e limites em `Docs/Mapas/CATACUMBAS_ETAPA_3_7.md`.


## Etapa 3.8

Produção e revisão estática concluídas: 175 PNGs selecionados, 189 regiões e 69 famílias. [Galeria consolidada](Revisao/index.html), com filtros por lote e recortes dos manifestos existentes. Inventário em `Docs/Mapas/CATACUMBAS_INVENTARIO_ARTE.json`; preparação por `Tools/Build-DungeonArtReview.py`. [Relatório da revisão](../../Docs/Mapas/CATACUMBAS_ETAPA_3_8.md). A galeria não é captura do jogo. Avaliação em partida pendente; sem testes automatizados, Unity ou builds nesta etapa.
