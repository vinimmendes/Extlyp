# Espreitador V3 — caminhada e golpe de garras

Modelo V3 aprovado como base; rig e animações produzidos no Blender 5.2.2 LTS. A fonte estática foi preservada. Integrado ao jogo em 13/10/2026; execução em Unity e validação prática pendentes. Detalhes em [registro da integração](../../../Docs/Integracao/ESPREITADOR_V3_2026-10-13.md).

## Arquivos

- `espreitador-v3-animado.blend`: rig, pesos, materiais, câmeras e ações editáveis `Walk` e `ClawAttack`. Selecionar `RIG_Espreitador_V3` e a ação desejada no Action Editor.
- `walk.gif` e `claw-attack.gif`: duas vistas simultâneas. Existem também prévias separadas por vista.
- `Top/` e `Perspective/`: 32 PNGs de caminhada e 20 de ataque por vista, com alpha, 384 pixels.
- `walk-sprite-sheet.png`: 2048 × 1024, oito colunas, 32 células de 256 pixels.
- `claw-attack-sprite-sheet.png`: 2048 × 768, oito colunas, 20 células de 256 pixels; quatro células finais transparentes.
- `poses-animacao.png`: apoios da caminhada e quatro etapas do ataque.
- `animation.json` e `export-summary.json`: formato, tempos e resumo da exportação.

## Movimento

`Walk`: quadros 1–32 a 24 fps; quadro 33 repete o primeiro e não entra no atlas. Passos alternados, contato baixo e balanço discreto de ombros/braços. Pés movem-se de frente para trás durante apoio; retornam elevados durante balanço. Raiz imóvel para que o jogo forneça o deslocamento real. Oscilação secundária de cabeça e abas, sem simulação de tecido.

`ClawAttack`: quadros 1–20. Repouso em 1, braço recolhido em 7, máxima extensão das garras em 12, retorno ao repouso em 20. Pés permanecem apoiados; não há salto ou avanço de raiz. Torção do tronco e braço esquerdo fazem contrapeso. Dedos abrem e tecido acompanha a recuperação.

O Blender trabalha a 24 fps; o jogo normaliza os quadros 1–12 aos 0,3 segundo de preparação autoritativa. A prévia GIF foi temporizada para iniciar o quadro 12 em 300 ms. Recuperação segue aproximadamente 24 fps. A implementação atual carrega este golpe de garras no lugar da mordida anterior.

## Rig

Controles de raiz, quadril, peito e cabeça; IK de duas articulações para pernas e braços, controles de pés/punhos e poles calibrados à pose estática. Ossos de mão, quatro dedos e polegar por lado; ossos individuais de abas. Pesos e keyframes editáveis. Curvas decorativas convertidas em malha apenas na fonte animada.

## Reprodução e revisão

Na pasta superior, `animate_model.py -- --rig-only` recria o rig e as ações a partir da V3 estática; `render_animation.py` exporta os quadros; `package_animation.py` monta GIFs/atlas com Python e Pillow. Executar scripts Blender com `--background --threads 4 --python`.

Revisão visual de poses, apoios, recuperação e margens do enquadramento. Exportação usa câmera/pivô fixos, frente +X e alpha. A integração troca os atlas e sua configuração visual; regras do servidor e prefabs foram preservados. RPCs, física, visibilidade, dano e execução das animações em Unity permanecem sem validação prática nesta entrega.
