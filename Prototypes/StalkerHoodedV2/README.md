# Espreitador encapuzado V2 — etapa 1

Modelo estático novo e editável, produzido no Blender 5.2.2 LTS instalado neste ambiente. Base visual: `Art/MonsterProposals/Espreitador/espreitador-encapuzado-conceito-v2.png`. Não usa o modelo rastejante anterior.

- `espreitador-encapuzado-v2.blend`: corpo bípede, capuz aberto, casaco dividido em abas, mangas, mãos e garras em malhas separadas.
- `pose-tres-quartos.png` e `pose-superior.png`: renders RGBA de 1024 pixels. Frente do modelo em +X.
- `build_static.py`: geração reproduzível. Executar com Blender em modo background e quatro threads.

Esta é uma interpretação 3D simplificada do conceito para aprovação de proporções e silhueta, não uma reprodução de todos os detalhes pintados. Rig, poses de ataque, caminhada, ataque e atlas permanecem pendentes, conforme a escolha de aprovação por etapa. Não foi integrada à Unity e não foram executados testes ou builds Unity.

Próxima etapa após aprovação: rig bípede, pernas IK, controles de coluna/cabeça/braços/dedos e ossos para abas e capuz; prancha de repouso, preparação, extensão e recuperação.
