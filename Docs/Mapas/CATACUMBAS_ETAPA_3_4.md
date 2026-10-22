# Catacumbas — etapa 3.4: sepulturas e coberturas

Lote CAT-P01 a CAT-P10 produzido e integrado: 28 variantes de bases/tampas de sarcófagos, lápides, nichos com ossos, urnas, bancos tombados, mesas funerárias, blocos caídos, entulho e cerâmica quebrada. Referências permanentes: matéria e abandono de Darkwood; legibilidade de cobertura e circulação de Bullet Echo. Arte medieval original, em vista superior, compatível com a direção das Catacumbas.

## Aplicação no mapa

`DungeonProps` monta apenas apresentação local, depois de pisos e arquitetura:

- Cada retângulo de `DungeonLayout.Covers` recebe arte dentro da sua pegada atual. Sarcófagos predominam; a capela usa blocos/entulho e a câmara dos contrapesos usa blocos. Não foram criados, removidos, ampliados ou reclassificados colisores.
- Bases de sarcófagos têm variantes abertas e fechadas estáticas, sem loot ou interação. Tampas usam a variante correspondente, pivô central e escala de 88% da base, mantendo sua borda visível. Nenhuma tampa é largada sobre caminho livre.
- Ossos ficam contidos nos nichos A08 dos ossários. Outros nichos recebem lápides ou urnas pequenas; parte permanece vazia. Inserções se ajustam ao interior da moldura, sem deslocar a parede. Ossos não formam tapetes claros junto à aranha.
- Bancos e mesas ocupam somente trechos de alvenaria já inacessíveis, junto às salas apropriadas, como mobiliário recolhido em recessos. Um grupo por sala, quando há encaixe; recusa colisão visual com nichos/painéis/grades/portas. Não são novas coberturas baixas no meio da sala: a parede original continua responsável por bloquear passagem e visão.
- Cerâmica quebrada é decalque plano de uma célula, junto a cantos, sem collider. Evita proximidade de loot, spawns, ferramentas, mecanismos e extrações. Não tem brilho ou marcador de coleta.

As pegadas de 2,12 × 2,12 u das coberturas permanecem iguais. As dimensões nominais do catálogo orientam os módulos; peças inseridas em nichos são reduzidas para caber no vão interno. A arte nova não altera os três tipos de monstro, as regras de combate/visão, a ponte, as extrações ou a autoridade do servidor.

Os renderers antigos de sarcófagos e tampas são ocultados somente após a montagem completa. Qualquer recurso ausente cancela a camada e mantém os placeholders. Servidor sem dispositivo gráfico não cria essa apresentação; sprites temporários são destruídos junto com o mapa.

## Arquivos e reprodução

- Fontes RGBA intactas e prompts individuais: `Art/Catacumbas/Sepulturas/{Fontes,Prompts}`.
- Manifesto consolidado e galeria: `Art/Catacumbas/Sepulturas/{prompts-v1.json,index.html}`. A galeria apresenta assets isolados, não uma captura da Unity.
- PNGs e `PropsArt.json` de runtime: `Assets/Resources/Environment/Dungeon/Props`.
- Preparação: `Tools/Prepare-DungeonProps.py`. Analisa o alpha, registra retângulos normalizados e copia as fontes sem editar pixels; preserva arquivos existentes diferentes.

Imagens produzidas com `imagegen` integrado, uma chamada por variante. A margem transparente é excluída no Sprite.Create, sem alterar o PNG. Importação sRGB, bilinear, Clamp, sem mipmaps, alpha preservado e sem compressão. Max-size escolhido explicitamente em potência de dois para manter pelo menos 128 px por célula no corpo recortado. Fontes de geração mantêm resolução completa.

Skills aplicadas: `level-design`, `unity-csharp-scripting` e `imagegen`, com APIs compatíveis com Unity 6000.0.84f1. Não foram adicionados shaders, pacotes, serviços ou alterações de engine/rede.

## Verificação e limites

Revisão estática do carregamento, da preservação das máscaras e dos encaixes; inspeção visual das 28 imagens produzidas. Inspeção dos PNGs e metadados não substitui avaliação em partida: contraste sob fog, leitura das tumbas abertas, combinação das tampas, junções com nichos e escala no Android aguardam o primeiro teste prático do usuário. Não foram iniciados Unity, testes automatizados ou builds nesta entrega.
