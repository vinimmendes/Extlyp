# Aranha Pálida — modelo e animações Blender

Modelo original baseado na proposta Pálida escolhida pelo usuário. Abdômen marfim manchado, cefalotórax, oito olhos, duas mandíbulas pequenas e exatamente oito pernas com duas articulações ósseas cada. Materiais procedurais; sem cabelos, partículas ou texturas externas.

Arquivo editável: aranha-palida.blend, Blender 4.5.0. Rig: RIG - Aranha Palida. Os nomes das patas D1–D4 e E1–E4 permitem editar cada par. A solução analítica das articulações foi gravada em keyframes de ossos; os empties registram trajetórias/guias, não são controladores IK ao vivo. Fonte: build_spider.py.

- Caminhada: frames 1–32 a 24 fps; frame 33 fecha o ciclo. Dois grupos de quatro patas alternam apoio e avanço. Durante apoio, pé percorre frente → trás linearmente; na recuperação retorna com elevação suave. Tronco estável.
- Preparação de teia: frames 101–119, aproximadamente 0,8 s; ergue as patas dianteiras e o abdômen.
- Lançamento/retorno: marcador 120; relaxamento até 128. O projétil de teia pertence ao sistema de efeitos do jogo, não foi colocado dentro do sprite.
- Pose parada: frame 128 e pose-parada.png.
- Câmera de prévia: vista superior, cabeça para cima. Antes de integração, exportar com cabeça para a direita para seguir a orientação +X do jogo.

Prévias reais: walk.gif, web.gif e poses.png. PNGs alpha de 384 pixels em Walk e Web. Atlases para avaliação: walk-sprite-sheet.png (32 frames) e web-sprite-sheet.png (28 frames, últimas quatro células vazias), células de 256, 8 colunas. package_spider.py monta as prévias a partir desses renders.

Renderização Blender concluída e poses inspecionadas. Corrigidos desalinhamentos da primeira solução de articulação antes da entrega. Integração ao jogo concluída em MonsterAppearance e WeaverVisual; regras de gameplay e prefabs preservados. package_runtime.py gira os frames para +X e instala os atlases em Resources/Monsters/Weaver. Detalhes em Docs/Integracao/ARANHA_PALIDA_2026-10-12.md.
