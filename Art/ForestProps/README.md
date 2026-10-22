# Toras da serraria — v1

Arquivo: `LogPile-v1.png`. Gerado com a ferramenta integrada image_gen, com fundo transparente. Arte preparada, ainda não aplicada ao mapa.

Destino visual: `LogPile` em `ForestMapGeometry.cs`, cuja área atual é 7 × 1,8 unidades. Na integração, recortar a margem transparente por UV e ajustar a área visível ao tamanho do objeto, preservando seu collider e layer. A proporção produzida é aproximada; conferir o ajuste visual antes da aplicação definitiva.

## Prompt utilizado

Use case: stylized-concept. Asset type: single transparent PNG sprite for a 2D top-down survival extraction game. A compact stack of 4 long felled timber logs lying parallel horizontally, seen from strictly overhead orthographic camera, no side view or isometric projection. The occupied silhouette must have width-to-height ratio exactly approximately 7:1.8 (3.89:1), matching a 7 by 1.8 world-unit rectangular low obstacle. Long logs extend left to right, uneven cut tips, coarse weathered dark brown bark, subtle cracks, occasional muted moss and pale exposed wood at broken tips. Original grim abandoned forest atmosphere, painterly textured game art with crisp readable silhouette at small mobile size, restrained earthy browns, clear separation between logs. Very soft neutral baked shading only within object, no dramatic highlights. Center one object on ample true transparent background, all logs fully visible, no ground, no leaves scattered outside footprint, no scene, no text, no border, no watermark. Avoid circles presented as front-facing log ends, avoid lumber planks, avoid tall pyramid stack. Output landscape canvas.

## Inventário estático de arte provisória

Leitura do código em 12/10/2026, sem execução Unity. Aqui “sem sprite” significa sem arte dedicada: esses objetos usam quadrados, polígonos ou peças primitivas.

- Terreno: Ground, ForestTrail, Trail, massas ImpenetrableForest e pedras BoundaryRock.
- Ruínas e casas iniciais: pisos, paredes, trechos quebrados e linhas WornFloorboard.
- Pequenos objetos: FallenDoor, BrokenChair, ChairBack e Debris.
- Marcos dos setores: LogPile (arte gerada nesta entrega), DeepLake, CropRow e CentralRelic.
- Condições de rota: Carcass, SpilledLogs, PassageBarrier, RoadBlock, WireMesh, BarrierDetail, SpilledCargo, FallenTimber e caminhão tombado.
- Transporte: caminhão, carroça/vagonete, barco e canoa, montados por ForestProps com formas básicas.
- Extrações: WornGround, Rail, OldTracks, BridgeStone, LeftPost, RightPost e BlockedExit.
- Passagens abertas: BrokenEdge.

Caixas SupplyCrate, WorkshopCrate, StoreCrate e Cover já passam por CoverCrateArt. Árvores, árvores mortas, camas, mesas, armários e gabinetes já têm caminhos de carregamento de arte; não entram como pendências confirmadas.

## Proposta de chão e caminhos

Combinar atmosfera de floresta abandonada (Darkwood) com leitura clara de caminhos, cobertura e combate (Bullet Echo), usando arte própria.

1. Base repetível de terra escura com folhas, em variações suaves; manchas maiores de musgo, solo exposto e umidade quebram repetição.
2. Caminhos principais de terra compactada ligeiramente mais clara e menos detalhada. Trilhas secundárias mais estreitas, com vegetação nas bordas.
3. Textura repetida ao longo dos segmentos existentes, com bordas irregulares transparentes, curvas e junções contínuas. Não esticar uma imagem única sobre cada segmento.
4. Decoração contextual: serragem perto da serraria, cascalho no depósito, lama perto do lago e terra arada na fazenda. Preservar corredores e silhuetas dos obstáculos.
5. Implementar apenas a apresentação sobre o plano atual. Reutilizar a semente do layout para variações estáveis; não criar colisões a partir de manchas decorativas. Começar com uma área da serraria para avaliação prática do usuário.
