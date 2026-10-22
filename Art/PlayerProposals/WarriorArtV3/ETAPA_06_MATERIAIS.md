# Etapa 6 — materiais e desgaste localizado

Entrega: `working-v3/warrior_v3-step06-materials.blend`, derivada da etapa 5, preservada em arquivo separado.

## Alterações

Oito materiais existentes refinados: aço da armadura, aço da lâmina, latão, couro, tecido vermelho, juntas escuras, madeira da tocha e cota de malha. Os três materiais da chama permanecem para a etapa 7.

- Aço com variação tonal estreita, rugosidade moderada e polimento localizado nas quinas e bordas identificadas da malha.
- Lâmina com grão direcional fino e brilho controlado, mantendo a geometria da etapa 4.
- Latão envelhecido com realce seletivo nas bordas.
- Couro mais fosco e bordas de uso discretamente mais claras.
- Vermelho profundo, rugosidade alta e brilho de fibras discreto no tecido.
- Juntas escuras para separação das peças.
- Madeira com grão fino; cota de malha mantém a construção Voronoi recebida com relevo menos pronunciado.

## Máscaras e coordenadas

Atributos adicionados a 162 malhas: `ArtSurfaceMask` contém máscara de bordas/quinas convexas no canal vermelho e aproximação de vincos côncavos no verde; `ArtRestPosition` armazena as posições originais locais dos vértices.

As máscaras usam bordas abertas e ângulos entre faces, com decaimento por distância ao longo das arestas. São máscaras de autoria aproximadas, não oclusão física, bake de contato entre objetos ou pintura manual. O escurecimento não detecta automaticamente todas as junções entre peças separadas. A máscara fica limitada pela resolução de cada malha.

As texturas usam o atributo de posição original para acompanhar a deformação sem depender de coordenadas de mundo ou câmera. Isso reduz uma causa de deslizamento aparente, mas estabilidade temporal e cintilação ainda precisam ser observadas na etapa 9. Não foi feito bake para Unity.

Posições, topologia, pesos, rig, Actions, keyframes, equipamentos, luzes, câmeras e gerenciamento de cor preservados. Não foram alterados materiais da chama.

## Revisão e limites

Inspeção estática do quadro 1 no viewport e atualização dos shaders. Não houve reprodução da animação, render novo, simulação, teste Unity ou build. A avaliação artística sob iluminação final permanece na etapa 8.

A skill dedicada `blender-materials` não está instalada entre as skills locais autorizadas; foram usadas as orientações de orquestração e fluxo de produção disponíveis, sem instalar ou aplicar skills do ZIP.

Fonte: `Tools/Blender/WarriorV3Stage06.py`. Registro: `stage06-change-log.json`.
