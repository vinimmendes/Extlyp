# Etapa 3 — silhueta das placas

## Entrega

`working-v3/warrior_v3-step03-armor.blend`, derivado da cópia inicial `warrior_v3.blend`. A v2 e a cópia inicial da v3 permanecem preservadas.

## Alterações

- Duas cascas de ombreira: topo mais amplo e baixo, com transição definida para a saia, mantendo o contorno de encaixe com os aros existentes.
- Quatro lâminas inferiores das ombreiras: sobreposição e borda inferior ligeiramente aberta para distinguir as peças.
- Duas cotoveleiras e duas joelheiras: volumes esféricos substituídos por cascas com campo central elevado e contorno recortado; abertura traseira para a articulação.
- Duas placas das coxas: formas alongadas e afuniladas, abertas atrás para revelar a malha.
- Oito rebites das ombreiras reposicionados para acompanhar a nova superfície.
- Espessura e bisel das placas ajustados; vínculos rígidos aos mesmos ossos mantidos.

Foram mantidos objetos, transformações de montagem, materiais, luzes, câmeras, equipamentos, rig, Actions e keyframes. Elmo e espada pertencem à etapa 4 e não foram alterados.

## Revisão e limites

Inspeção visual estática do quadro 1 no viewport do Blender 5.2.2 LTS. A visualização por câmera e materiais é apenas uma configuração do editor; não equivale ao render final. Não foi alterada a câmera de jogo.

Não foram reproduzidos ou recalculados os 36 quadros. Folgas, interseções e estabilidade de leitura durante o movimento ainda precisam da revisão prevista na etapa 9. Não foram feitos renders, testes Unity ou builds.

Registro detalhado: `stage03-change-log.json`. Script de autoria local: `Tools/Blender/WarriorV3Stage03.py`. Nenhum script fornecido no ZIP foi executado.
