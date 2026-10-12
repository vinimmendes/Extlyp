# Adaptação visual rústica — v1

Referência: `Assets/Art/Player/player-rustic-pistol-east-v2.png`. Modelo editável `personagem-rustico.blend`; animações e inspeção em `preview.html` (abrir diretamente, sem servidor).

O script `../dress_character.py` abre o boneco aprovado e adiciona materiais procedurais, cabelo, volumes de roupa, remendos, mochila de lona, correias, fivelas, botas, cadarços e uma pistola com ferrolho estreito visto de cima. Não utiliza geração independente de quadros ou projeção do sprite sobre uma superfície plana. É uma primeira interpretação 3D simplificada da pintura, com detalhes geométricos e materiais próprios.

Esqueleto, controles dos pés, curvas, duração (32 quadros / 24 fps) e câmeras são os do `.blend` aprovado. Apenas a apresentação foi modificada. O original continua disponível na pasta acima. Não integrado à Unity e sem testes de gameplay.

Renderização: Blender 4.5.0, Cycles CPU, 24 amostras com denoising, 640×640, PNG RGBA transparente. GIFs usam fundo escuro e paleta reduzida. `poses.png` é uma prancha de comparação. Renderizar novamente com Blender `--background --factory-startup --python Prototypes/PlayerWalk3D/dress_character.py -- --full`; empacotar com Python `Prototypes/PlayerWalk3D/package_preview.py Prototypes/PlayerWalk3D/VisualV1`.
