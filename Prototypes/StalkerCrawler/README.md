# Espreitador — Rastejante

Status posterior em 12/10/2026: o usuário autorizou implementar todas as animações. Caminhada e mordida integradas em MonsterAnimationVisual e Resources/Monsters/Stalker. Validação prática pendente; detalhes em Docs/Integracao/ANIMACOES_IMPLEMENTADAS_2026-10-12.md. O pedido de adiamento abaixo é histórico.

Status registrado em 12/10/2026: caminhada e mordida aprovadas pelo usuário; implementação no jogo adiada por orientação dele, para integrar posteriormente junto às outras animações. Modelo, renders e prévias preservados nesta pasta.

Modelo original Blender 4.5 baseado na opção Rastejante aprovada: cabeça pálida/calva, tronco estreito, braços alongados, pernas dobradas, pele escura e ossos sob a pele. Quatro membros; mãos com cinco dedos simplificados. Materiais procedurais, sem imagens externas, cabelo ou simulação de tecido.

Arquivo editável: espreitador-rastejante.blend. Rig RIG - Rastejante: dois ossos por membro, keyframes editáveis. Trajetórias dos apoios resolvidas analiticamente e gravadas nos ossos. Empties Guia contato registram posições; não são controladores IK ao vivo.

- Caminhada: frames 1–32, 24 fps; frame 33 fecha o ciclo. Braço direito/perna esquerda alternam com braço esquerdo/perna direita; apoios percorrem frente → trás e retornam elevados, sem inversão no meio do percurso.
- Preparação da mordida: 101–108, aproximadamente 0,3 s; recolhe a cabeça e abre a mandíbula.
- Mordida: 109–112; avança com a cabeça e fecha a mandíbula. Mãos e pés permanecem apoiados.
- Recuperação: 113–120; frame 120 é a pose parada.
- Câmera de prévia: ortográfica superior, cabeça para cima. A integração futura deve exportar/girar para +X, como os outros atores.

Prévias walk.gif e attack.gif; poses.png para comparar etapas; pose-parada.png. PNGs alpha de 384 pixels em Walk/Attack. Atlases de avaliação 2048 x 1024, células 256: caminhada 32 frames e golpe 20 (células restantes vazias).

Fontes: build_crawler.py e package_crawler.py. A mordida substitui o golpe dos braços, com controles separados para cabeça e mandíbula e dentes simples modelados. O ciclo de caminhada foi preservado. Integração ao jogo pendente; IA, combate e servidor não foram alterados.
