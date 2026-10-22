# Protagonista — recarga da shotgun

Status posterior em 12/10/2026: implementação autorizada e concluída no código/recursos. Inclui variante de um cartucho em ShotgunReloadSingle, escolhida pela quantidade autoritativa da recarga. Validação prática pendente; o adiamento abaixo é histórico. Detalhes em Docs/Integracao/ANIMACOES_IMPLEMENTADAS_2026-10-12.md.

Em 12/10/2026, o usuário aprovou a animação e pediu para mantê-la pendente de implementação junto às outras animações.

Animação original Blender 4.5 a partir da pose aprovada no modelo ShotgunSwitch/troca-pistola-shotgun.blend. Mantém roupas, mochila, empunhadura de mira e pernas paradas. Novo arquivo protagonista-recarga-shotgun.blend; não substitui assets em uso.

Timeline 1–64, 24 fps: 63 intervalos = 2,625 segundos, aproximação de um frame ao tempo atual de 2,6 segundos em WeaponCatalog. Na futura integração, normalizar pela duração real da recarga autoritativa, combinar com pernas do runtime se o jogador andar e respeitar cancelamentos. Status: pronta para avaliação, implementação no jogo pendente junto às outras animações.

- Mira inicial, 1–4.
- Inclinar arma e abrir báscula, até 14.
- Retirar dois estojos e levar a mão à cintura, 15–25.
- Buscar/alinha primeiro cartucho e inserir, 28–38.
- Buscar/alinha segundo cartucho e inserir, 41–51.
- Apoiar guarda-mão, fechar canos e voltar à mira, 52–64.

Controles editáveis CTRL - Shotgun recarga e CTRL - Bascula dos canos. Cartuchos e braços animados por keyframes. Os cartuchos ficam ocultos ao entrar nas câmaras; estojos guardados, sem descarte ou itens no chão. A animação representa uma recarga completa de dois cartuchos, não uma variante de reposição de apenas um. Essa variante e ligação à quantidade real de munição precisam ser tratadas na integração futura.

Renders transparentes 512 x 512 em frames/. recarga-shotgun.gif tem pausas inicial/final adicionais para avaliação. Atlas sprite-sheet-recarga-shotgun.png: 2048 x 2048, 8 x 8, 64 células de 256 pixels. etapas-recarga-shotgun.png para comparação. Frente +X, câmera superior do personagem atual.

Fontes ../build_shotgun_reload.py e ../package_shotgun_reload.py. Renderização e inspeção de artefatos Blender; nenhum teste ou build Unity executado. Combate, inventário, rede e servidor não foram alterados.
