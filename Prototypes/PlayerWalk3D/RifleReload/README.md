# Protagonista — recarga do rifle

Status posterior em 12/10/2026: recarga integrada ao código/recursos por autorização do usuário, com variantes de um a cinco tiros e duração autoritativa. Validação prática pendente; referências anteriores à implementação pendente abaixo são históricas. Detalhes em Docs/Integracao/ANIMACOES_IMPLEMENTADAS_2026-10-12.md.

Animação Blender 4.5 do rifle de ferrolho atual, capacidade cinco tiros. Novo arquivo protagonista-recarga-rifle.blend; personagem, roupas, mochila, pose de mira e pernas paradas preservados.

78 frames a 24 fps, 77 intervalos = 3,208 segundos, aproximação menor que um frame ao tempo atual de 3,2 segundos em WeaponCatalog. Futura integração deve normalizar pela duração real autoritativa e lidar com cancelamento, quantidade de munição e pernas em movimento.

- Inclinar arma, levantar manopla e puxar ferrolho, até 22.
- Buscar e alinhar pente de cinco munições, 29–38.
- Apoiar e pressionar munições para o depósito interno, 44–51.
- Retirar pente vazio, 52–60.
- Avançar e travar ferrolho, 61–72.
- Retornar à mira, 73–78.

Pente e cinco munições modelados separadamente; munições orientadas ao longo do cano e empilhadas verticalmente no pente. Elas são ocultadas ao entrar no depósito interno. Clipe representa recarga completa com pente; não é uma variante de reposição parcial por cartuchos individuais. Controles editáveis de rifle, ferrolho e pente. Não inclui animação de ciclagem após cada disparo, que é uma ação separada da recarga.

Renders Blender alpha 512 x 512. recarga-rifle.gif e etapas-recarga-rifle.png. Atlases de 256 por célula divididos para não ultrapassar 2048 pixels: sprite-sheet-recarga-rifle-1.png contém frames 1–64 (2048 x 2048), -2.png frames 65–78 (2048 x 512, duas células finais vazias). GIF acrescenta pausas apenas para avaliação.

Fontes ../build_rifle_reload.py e ../package_rifle_animations.py. Animação para avaliação; implementação no jogo pendente junto às outras animações. Nenhum teste/build Unity executado; nenhum código de combate, inventário ou rede alterado.
