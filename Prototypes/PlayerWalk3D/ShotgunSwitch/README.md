# Troca da pistola para a shotgun

Animação construída no Blender 4.5 a partir do personagem aprovado, sem sobrescrever o modelo original ou os sprites usados pelo jogo.

- Modelo editável: troca-pistola-shotgun.blend.
- Timeline: 32 frames a 24 fps, aproximadamente 1,33 segundo; câmera de jogo em vista superior.
- Marcadores: pistola em mira (1), guardar (10), puxar shotgun (15), apoio das mãos (22), shotgun em mira (28).
- Pernas fixas na pose parada do rig aprovado; corpo e mochila mantidos. Armas e braços têm keyframes editáveis.
- Prévia: troca-shotgun.gif. As pausas inicial/final do GIF são para avaliação, não frames adicionais da animação.
- Etapas: etapas-troca-shotgun.png.
- Atlas: sprite-sheet-troca-shotgun.png, 32 células de 256 x 256, em 8 colunas x 4 linhas, alpha transparente.

Fontes: ../build_shotgun_switch.py e ../package_shotgun_switch.py. O primeiro roda no Blender em background; o segundo empacota os renders reais com Pillow. PNGs originais em frames/.

Renderização concluída e etapas inspecionadas visualmente. A integração ao runtime foi feita em PlayerCharacterVisual, com atlases separados para pernas e tronco da troca. A pose/caminhada da shotgun foi atualizada com a empunhadura aprovada; regras de combate mantidas. Detalhes em Docs/Integracao/SHOTGUN_ANIMACAO_2026-10-12.md.
