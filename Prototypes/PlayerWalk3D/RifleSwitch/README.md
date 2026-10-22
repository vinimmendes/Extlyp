# Protagonista — troca para rifle

Status posterior em 12/10/2026: troca integrada ao código/recursos por autorização do usuário, junto às demais animações. Validação prática pendente; referências anteriores à implementação pendente abaixo são históricas. Detalhes em Docs/Integracao/ANIMACOES_IMPLEMENTADAS_2026-10-12.md.

Animação original Blender 4.5, baseada no personagem rústico e na troca para shotgun já aprovados. Rifle de ferrolho com coronha de madeira, cano único, mira e alavanca; mesmas proporções básicas do rifle usado no runtime.

Arquivo editável troca-pistola-rifle.blend. 32 frames a 24 fps. Guardar pistola até 10, sacar rifle em 15, apoiar duas mãos em 22, mira de rifle em 28–32. Pernas paradas; tronco e arma têm keyframes editáveis. A transição começa especificamente com pistola equipada; a futura integração precisa escolher o trecho adequado quando vier de outra arma ou de mãos vazias.

Renders alpha 512 x 512 em frames/. troca-rifle.gif com pausas para avaliação; etapas-troca-rifle.png; atlas sprite-sheet-troca-rifle-1.png com 32 células de 256, 2048 x 1024. Frente +X e câmera superior atual.

Fonte ../build_rifle_switch.py; empacotamento ../package_rifle_animations.py. Implementação no jogo pendente, animação para avaliação. Não altera recursos ativos, troca de arma, combate, rede ou servidor. Nenhum teste/build Unity executado.
