# Etapa 8 — iluminação e quadro para aprovação

Arquivo: `working-v3/warrior_v3-step08-lighting.blend`, derivado da etapa 7 preservada.

## Entregas visuais

- `stage08-approval/comparacao-v2-v3-quadro01.png`: comparação lado a lado, com os quadros completos em escala 1:1, sobre fundo neutro.
- `stage08-approval/warrior-v3-frame01.png`: quadro 1 da v3, PNG RGBA de 640 × 640.
- `stage08-approval/warrior-v2-frame01-same-blender.png`: quadro 1 da base v2, refeito no Blender atual para reduzir diferenças de versão na comparação.

## Iluminação

Principal mais ampla e direcional; preenchimento e recorte reduzidos para separar os planos sem apagar o volume. Intensidade do mundo ajustada para 0,24. Luz pontual quente de 42 W e raio de 0,11 m próxima à chama, presa ao osso `hand.L`, para acompanhar a tocha. A emissão das malhas da chama também participa do render Cycles.

Gerenciamento de cor final: Standard, look None, exposição -0,35. Uma primeira apresentação em AgX deixou a chama pálida; a entrega final preserva melhor amarelos e laranjas em Standard. Não foram alterados os materiais das etapas 6–7 para compensar a iluminação.

## Comparação e limites

Ambas as versões foram renderizadas no Blender 5.2.2 LTS, no quadro 1, com a câmera ortográfica de jogo original, 640 × 640, Cycles, 128 amostras e denoising. Sem corte, normalização por bounding box, deslocamento ou redimensionamento dos sprites no comparativo.

A v2 mantém seus materiais, luzes e exposição originais (-0,65). A v3 apresenta as alterações de arte e luz das etapas 3–8. Logo, esta é uma comparação do resultado visual total, não um ensaio isolado da geometria ou de um material. Não substituir os PNGs fornecidos pelo outro chat por estes renders reprocessados.

O arquivo v2 não foi salvo ou modificado. A cena temporária de comparação e seus dados foram descartados ao reabrir a versão salva da v3; nenhum script do pacote original foi executado.

As imagens são entregas da etapa 8 solicitada, não renderização de todos os quadros. Não houve reprodução do ciclo, simulação, teste Unity ou build. O novo vínculo da luz, continuidade das chamas, folgas e estabilidade dos materiais em movimento permanecem pendentes da etapa 9.

**Estado: aguardando avaliação artística do usuário.** Não interpretar conclusão da etapa como aprovação automática para renderizar a animação inteira.

Registro: `stage08-change-log.json`. Autoria: `Tools/Blender/WarriorV3Stage08.py` e montagem de comparação em `Tools/Blender/ComposeWarriorStage08.py`.
