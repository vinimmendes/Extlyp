# Etapa 4 — elmo e espada

Entrega: `working-v3/warrior_v3-step04-equipment.blend`, derivada da etapa 3, preservada em arquivo separado.

## Alterações

- Casco do elmo com transições adicionais de perfil acima da testa, conservando o volume geral.
- Crista de aço e filete de latão refeitos como faixas de seção definida, ajustadas ao contorno do casco.
- Viseira com campos superior e inferior separados em torno da faixa escura dos olhos; nervura nasal com faces definidas e faixa dos olhos mais estreita.
- Lâmina da espada com planos de fio, faces centrais e canal longitudinal discreto; ponta única e comprimento preservado.
- Rotação de 9° somente na seção da lâmina, em torno de seu eixo longitudinal, para favorecer sua leitura de cima. Mão, eixo da arma e empunhadura mantidos.
- Guarda facetada, pomo em disco e enrolamento de couro no cabo, usando os materiais existentes.

Oito superfícies substituídas, faixa dos olhos ajustada e uma malha de enrolamento adicionada. Vínculos do elmo ao osso `head` e da espada a `hand.R` mantidos. Não houve alterações de rig, Actions, keyframes, equipamentos por mão, tocha, tecido, materiais, câmeras ou luzes.

## Revisão e limites

Revisão visual estática no viewport pela câmera de jogo, no quadro 1, em Blender 5.2.2 LTS. O ajuste da crista foi refinado após essa leitura para acompanhar continuamente a superfície.

Não houve reprodução de animação, teste de colisão, render novo, teste Unity ou build. A aparência final depende das etapas de materiais e iluminação; folgas e estabilidade ao longo dos 36 quadros continuam pendentes da etapa 9.

Registro: `stage04-change-log.json`. Fonte de autoria: `Tools/Blender/WarriorV3Stage04.py`. Nenhum script do ZIP foi executado.
