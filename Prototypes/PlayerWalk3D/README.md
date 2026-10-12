# Boneco 3D — prova de caminhada

Abra `preview.html` diretamente no computador. `boneco-caminhada.blend` é o arquivo editável, `caminhada-topo.gif` a vista do jogo e `caminhada-lateral.gif` a vista de inspeção. PNGs transparentes em `topo/` e `lateral/`; `poses.png` compara quatro poses.

## Movimento

- Direita laranja e esquerda azul, sem troca de identidade.
- Dois ossos por perna, IK de dois segmentos, controle de pé e direção de joelho.
- Deslocamento horizontal triangular linear entre -0,48 e +0,48, pernas opostas. Inversão somente nos extremos.
- Pequena elevação de 0,10 no pé que avança, sem alterar sua trajetória horizontal.
- Tronco, cabeça, mochila, braços e pistola permanecem imóveis. Pernas com volumes reais sofrem encurtamento na câmera ortográfica e oclusão pelo tronco.
- 32 quadros a 24 fps, cerca de 1,33 segundo. Quadros 9 e 25 são passagens centrais; quadro 9 é a pose de parada da prévia. São parâmetros do protótipo, não medições de Darkwood.
- Loop contém quadros 1–32; chave de fechamento no quadro 33. As curvas dos controles usam interpolação linear e repetição cíclica.

## Reproduzir geração

Blender portátil oficial 4.5.0, baixado de https://download.blender.org/release/Blender4.5/blender-4.5.0-windows-x64.zip para `.cache/blender/`. Não altera instalação da Unity ou assets existentes.

Execute o Blender com `--background --python Prototypes/PlayerWalk3D/build_walk.py`. Depois execute `package_preview.py` com Python e Pillow para empacotar os renders em GIF e prancha. O script Blender limpa apenas a cena do processo em que ele é executado; use um processo novo, como indicado.

As formas são deliberadamente simples. Não é o personagem final nem um rig de produção. Não foram feitas alterações ou validações de gameplay na Unity.
