# Guerreiro 3D v2

Versão separada da v1. Espada na mão direita e tocha na esquerda, voltado para a direita.

## Melhorias executadas

- Elmo com coroa afunilada e crista, ombreiras em cascas de metal com lâminas sobrepostas, peitoral com faces definidas, placas das botas e detalhes das luvas.
- Câmera ortográfica de jogo recalibrada; câmera vertical a 90 graus e vista de perspectiva preservadas.
- Rig com 23 ossos, IK nas duas pernas, controles de pés e direção dos joelhos, rolagem dos pés e esqueleto conectado.
- Apoio com recuo uniforme correspondente a aproximadamente 0,639 m/s de avanço; retorno em curva de quinto grau, elevação dos pés e curvas periódicas com tangentes contínuas.
- Ciclo principal mais pesado: 36 quadros / 30 fps / 1,2 s. Alternativa mais rápida: 30 quadros / 30 fps / 1 s.
- Tecido vermelho escuro com menor brilho, metal com rugosidade mais alta e menos ruído, couro com detalhes nas bordas.
- Chama alongada com gradiente amarelo-laranja e cintilação; capa e pano frontal com pesos suaves e movimento secundário.
- Pano frontal ajustado após auditoria detectar interseções com as coxas.

## Uso

Abra `warrior_v2.blend` e pressione Espaço. Em Pose Mode, a coleção `Controles` contém controles de pés e joelhos com formas próprias. A Action principal se chama `Walk_Heavy_36f_IK`.

Para trocar a cadência, abra `switch_cadence.py` no Text Editor do Blender, defina `CADENCE` como `heavy` ou `alert` e execute. O script seleciona as Actions correspondentes do corpo e da chama e ajusta o intervalo.

Os GIFs de jogo, perspectiva e vista vertical mostram o mesmo ciclo. `cadence_alert_preview.gif` mostra uma alternativa de reprodução mais rápida do ciclo completo para comparação.

## Verificação e limites

`validation/rig_and_contact.json` registra erro de IK, conexão das articulações, altura das solas, desvio durante o apoio e continuidade na emenda. O teste de apoio soma avanço temporário da raiz ao ciclo no lugar para medir deslizamento.

`validation/cloth_collision.json` testa interseções entre as partes livres do tecido e seis superfícies das pernas em todos os 36 quadros. As raízes presas ao corpo são excluídas. Isso não equivale a uma prova de ausência de colisões entre todas as peças da cena ou em todos os subquadros.

Folhas de contato, renders neutros, comparação com a v1 e leitura em 64, 128 e 256 px ficam em `validation/`. A sobreposição de contornos normaliza os limites das imagens; não afirma alinhamento exato de pose ou perspectiva.

O modelo permanece uma interpretação estilizada da referência única. O pano usa deformação por ossos, e a chama usa malhas e shape keys. Os materiais procedurais permanecem no Blender; não foi prometida compatibilidade direta com um motor específico.

## Skills e reprodução

Skills do repositório https://github.com/RobLe3/cc-blender-skill, catálogo skills.sh, em `.agents/skills/`. Aplicadas: harmonização, modelagem, materiais, iluminação, animação, renderização e avaliação por folha de contato. Métodos de segmentação usam regiões manuais com alpha porque OpenCV não está instalado.

Execução real no Blender 4.3.2 com `bpy` em background. Nenhum servidor MCP foi configurado. A renderização usa Cycles sem denoiser, com amostras adicionais e materiais menos ruidosos; a compilação disponível não inclui OpenImageDenoise.

`build_v2.py` recria o modelo a partir de `base_geometry.py`; `finalize_controls.py` adiciona os widgets e as Actions alternativas da chama; `render_v2.py` gera os quadros; `package_v2.py` monta as prévias e diagnósticos.

## Tamanho dos sprites

A revisão em 64, 128 e 256 px está em `validation/readability_64_128_256.png`. Para preservar os detalhes desta versão, prefira 128 ou 256 px. Em 64 px, os detalhes das mãos e da armadura ficam reduzidos. O atlas principal conserva quadros de 640 px e o pivô projetado da raiz nos metadados.
