# Corrosivo — Papo inchado

Status posterior em 12/10/2026: o usuário autorizou implementar todas as animações. Caminhada e cuspe integrados em MonsterAnimationVisual e Resources/Monsters/Acid. Validação prática pendente; detalhes em Docs/Integracao/ANIMACOES_IMPLEMENTADAS_2026-10-12.md. O pedido de adiamento abaixo é histórico.

Status registrado em 12/10/2026 por orientação do usuário: manter a caminhada e o cuspe pendentes de implementação no jogo, para integrar posteriormente junto às outras animações. Modelo, renders e prévias preservados nesta pasta.

Modelo original construído no Blender 4.5, inspirado na opção 1 da prancha Art/MonsterProposals/Corrosivo/propostas-corrosivo-2026-10-12.png, escolhida pelo usuário. Corpo humanoide encurvado, cabeça calva, boca pendente e dois grandes papos amarelados. Materiais procedurais de pele, bolsas, veias, dentes e boca; sem texturas externas ou simulação.

Arquivo editável: corrosivo-papo-inchado.blend. Controles independentes para cabeça, mandíbula e inflação de cada papo. Esqueleto com dois ossos por braço/perna; trajetórias calculadas analiticamente e gravadas em keyframes. Guias de apoio registram os destinos, não são controles IK ao vivo.

- Caminhada: 1–32, 24 fps; frame 33 fecha o ciclo. Passos alternados, apoio frente → trás e retorno elevado, com braços em oposição aos passos.
- Preparação: 101–129, 1,2 segundo até o início do cuspe, acompanha o tempo de preparação atual da habilidade. Papos inflam e cabeça recua.
- Cuspe: 130–134. Compressão dos papos, projeção da cabeça e abertura da mandíbula.
- Recuperação: 135–145; frame 145 é a pose parada.
- Empty Saida visual do acido marca a boca para uma futura integração. Projétil e poça não estão incorporados ao sprite; pertencem aos efeitos do jogo.

Prévia de cima, cabeça para cima. No modelo, frente +X. Câmera de inspeção oferece perspectiva para conferir os volumes. GIFs têm pausas adicionais nos extremos do cuspe somente para facilitar avaliação. PNGs transparentes 512 x 512. Atlases de avaliação: Walk 2048 x 1024, 32 células; Spit 2048 x 1536, 45 células e três células vazias; células de 256.

Fontes: build_corrosive.py e package_corrosive.py; render_clips.py permite renderizar novamente a partir do .blend salvo. É um modelo 3D próprio com aproximação da referência; não usa a imagem gerada como sprite animado e não reproduz todos os detalhes da ilustração. Integração ao jogo pendente; nenhum código de combate, IA, rede ou asset ativo da Unity foi alterado. Nenhum teste ou build Unity foi executado.
