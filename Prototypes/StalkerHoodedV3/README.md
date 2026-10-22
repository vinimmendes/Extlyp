# Espreitador encapuzado V3 — revisão estática

Entrega posterior: rig, caminhada e golpe de garras disponíveis em [Animation/README.md](Animation/README.md). A fonte estática descrita abaixo continua preservada. As novas animações ainda não foram integradas ao jogo.

Produzido no Blender 5.2.2 LTS. Modelo novo baseado no conceito em `Art/MonsterProposals/Espreitador/espreitador-encapuzado-conceito-v2.png`; arquivos V2 preservados.

## Entrega

- `espreitador-encapuzado-v3.blend`: malhas editáveis, materiais procedurais, três câmeras e iluminação.
- `tres-quartos-v3.png`, `superior-v3.png`, `perfil-v3.png`: renders RGBA de 1200 pixels.
- `prancha-conceito-v3.png`: conceito, modelo e leitura superior em célula de 256 pixels.
- `comparacao-v2-v3.png`: comparação dos renders das duas versões.
- `build_model.py`: recria modelo e renders; `package_preview.py`: monta as pranchas com Pillow.

## Revisão

Corpo estreito com coluna curvada, pernas flexionadas, ombros assimétricos; pés com dedos; capuz com borda espessa, ponta elevada e cavidade escura; gola fechada; casaco aberto com painéis sobrepostos, comprimentos diferentes, furos, costuras e fibras nas bordas. Mãos com dedos curvos, articulações e palma fundidos por remesh; garras separadas com curva contínua e tendões de superfície. Malhas de roupa separadas para deformação posterior.

Materiais de tecido carvão/marrom, pele pálida e queratina, com variação de cor e relevo. Materiais Diffuse usados nesta entrega para obter a paleta no Blender instalado. A V2 apresentava cor clara apesar das conexões no Principled; não foi estabelecida uma causa completa desse comportamento na versão instalada. Sem afirmar que a V2 foi corrigida.

## Limites e próxima etapa

Interpretação 3D do conceito: não reproduz cada dobra e detalhe da ilustração. Ainda sem rig, animações, UV pintado ou atlas para o jogo. Frente +X. Revisão feita por inspeção dos renders; nenhum teste, build ou sessão Unity executado. Integração futura mantém as regras autoritativas atuais.

Reprodução: executar Blender com `--background --threads 4 --python Prototypes/StalkerHoodedV3/build_model.py`; em seguida executar `package_preview.py` com Python e Pillow. Mensagens sobre thumbnails/cache fora do workspace não impedem salvar o arquivo e os renders da entrega.
