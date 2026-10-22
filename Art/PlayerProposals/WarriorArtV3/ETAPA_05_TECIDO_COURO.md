# Etapa 5 — tecido e couro

Entrega: `working-v3/warrior_v3-step05-cloth-leather.blend`, derivada da etapa 4 preservada.

## Alterações

- Capa com dobras amplas adicionais, pequenas irregularidades na barra e bainhas nas bordas laterais e inferior.
- Pano frontal com volume moderado nas dobras e acabamento nas bordas, conservando a região de fixação.
- Dobras da gola menos uniformes, sem ampliar a silhueta do pescoço.
- Cinto e correias das coxas com seção mais plana de tira, com bordas de construção.
- Fivela central refeita como moldura aberta com pino; inserto escuro original preservado.
- Luvas com ajuste leve somente na parte dorsal e costuras laterais; superfícies inferiores de empunhadura mantidas.
- Aba da bolsa com costura de contorno.

Foram modificados 12 objetos e adicionados 18 detalhes de construção. Materiais e suas cores não foram alterados: acabamento de superfície pertence à etapa 6.

## Deformação preservada

Capa e pano frontal mantêm os mesmos vértices, índices, topologia, grupos e pesos. Apenas suas posições foram ajustadas. As bainhas novas copiam os pesos dos vértices correspondentes das bordas. Detalhes de couro e fivela usam os mesmos vínculos rígidos de suas peças de origem.

Rig, Actions, keyframes, equipamentos, posição das mãos, geometria das etapas 3–4, câmeras e luzes foram mantidos. Nenhum script do pacote original foi executado.

## Revisão e limites

Revisão visual estática pela câmera de jogo, no quadro 1, usando o viewport do Blender 5.2.2 LTS. Não houve reprodução da animação, simulação, render novo, teste Unity ou build.

Manter os pesos não comprova ausência de interseções após alterar as formas. As novas folgas e a leitura do tecido ao longo dos 36 quadros permanecem pendentes da etapa 9.

Registro: `stage05-change-log.json`. Fonte: `Tools/Blender/WarriorV3Stage05.py`.
