# Direção visual — guerreiro medieval v3

## Objetivo

Melhorar a aparência do guerreiro animado v2 como skin do protagonista de Cyber Extraction: armadura medieval gasta, elmo, tecido vermelho profundo, espada na mão direita e tocha na esquerda, preservando a animação existente.

Este documento fixa o alvo de trabalho das etapas seguintes, não representa aprovação de um resultado v3 já produzido. As etapas 1–8 foram autorizadas e executadas; a apresentação da etapa 8 aguarda avaliação artística. Etapas 9–10 permanecem planejadas.

## Hierarquia das referências

1. **Principal — `references/01-primary-warrior-reference.png`:** fonte para equipamento, distribuição dos materiais, formas das placas, desgaste e combinação de aço, couro e vermelho. Elmo e tocha pertencem à proposta do pacote atual; não substituí-los pela cabeça descoberta das propostas antigas.
2. **Base animada — `baseline-v2/warrior_v2.blend` e suas imagens:** fonte para rig, pose animada, ordem dos quadros, cadência, câmera e enquadramento. Esta versão será refinada, não reconstruída do zero.
3. **Identidade — `references/02-protagonist-identity-and-camera.png`:** apoio para postura compacta, caráter de sobrevivente, materiais gastos e leitura superior. Não usar a espingarda como equipamento desta skin.
4. **Diagnóstico — `references/03-source-comparison.png` e folhas de quadros da v2:** evidência das diferenças visuais, sem tomar seus contornos normalizados como alinhamento exato de câmera e pose.

Preservar arte própria do projeto. A atmosfera sombria orienta apresentação e desgaste, sem copiar personagens de outros jogos.

## Aparência pretendida

3D com acabamento pictórico, volumes claros e detalhes seletivos. O personagem deve parecer um sobrevivente equipado com armadura usada. A câmera superior do jogo guia as decisões; a vista de perspectiva é complementar.

| Área | Alvo | Evitar |
| --- | --- | --- |
| Silhueta | Postura compacta da base; elmo, ombreiras e equipamentos distinguíveis | Alongamento do corpo, aumento arbitrário da cabeça ou mudança para um cavaleiro genérico |
| Armadura | Placas forjadas com bordas, espessura e sobreposição; articulações livres | Esferas aparentes, peças infladas e repetição do mesmo volume em todas as juntas |
| Elmo | Viseira, crista e faces definidas; detalhes legíveis de cima | Calota lisa com brilho circular dominante |
| Aço | Meio-tom frio, planos diferenciados, bordas polidas e juntas escuras | Cromado, plástico, manchas de ruído uniformes ou ferrugem cobrindo tudo |
| Couro | Marrom gasto, tiras com espessura, fivelas e marcas nas áreas de uso | Cilindros lisos e detalhes que desaparecem no tamanho de jogo |
| Tecido | Vermelho profundo, poucas dobras grandes, espessura e bordas desgastadas | Folha rígida, vermelho luminoso, brilho plástico ou excesso de microdobras |
| Espada | Guarda e cabo definidos; planos da lâmina separados do fio | Lâmina sumindo por ficar de perfil; alterar empunhadura ou mão equipada |
| Chama | Núcleo claro, corpo amarelo e bordas laranja; contorno assimétrico | Gota sólida, brilho branco cobrindo toda a chama ou fumaça que oculte a silhueta |
| Luz | Direção clara, contato entre peças, reflexo quente local da tocha | Reflexos brancos que apaguem as formas ou contorno luminoso em todo o corpo |

## Paleta orientadora

As cores abaixo são referências visuais aproximadas em sRGB, não valores a copiar diretamente para os shaders sem avaliar iluminação e gerenciamento de cor.

- Aço em sombra: `#30383D`; aço médio: `#737C80`; borda: `#BBC0BA`.
- Latão envelhecido: `#927047`, usado com moderação em aros e fivelas.
- Couro: `#493022`, com desgaste mais claro próximo de `#795338`.
- Tecido vermelho: `#651E22`, sombras `#301317`, dobras iluminadas `#923A36`.
- Chama: núcleo marfim quente `#FFF0B3`, amarelo `#FFC443`, laranja `#F36B18`.

## Preservação da base

Preservar rig, nomes de ossos e controles, Actions, IK, keyframes, cadência principal e alternativa, equipamentos por mão e identidade da pose. Não reconstruir o personagem ou aplicar transformações globais como atalho para ajustar a silhueta.

Registrar qualquer futura alteração de pesos que seja necessária para acomodar novas placas ou dobras. O inventário técnico completo está em `baseline-scene-settings.json`.

Manter a câmera de jogo como referência de comparação. Se alguma câmera ou enquadramento precisar mudar futuramente, salvar como variante explícita, sem substituir a base silenciosamente. O registro recebido usa atlas 6 × 6, 36 quadros de 640 px e pivô `[257.123, 423.602]`, em pixels a partir do canto superior esquerdo. Esses metadados são restrições de continuidade, não uma integração Unity já verificada.

## Critérios de avaliação visual futura

1. Comparar o mesmo quadro, mesma câmera, escala e fundo entre v2 e v3; usar o quadro 1 como âncora inicial. Não comparar apenas imagens com recortes diferentes.
2. Ler o conjunto antes do detalhe: separar elmo, torso, braços e equipamentos; a tocha não deve ser o único elemento reconhecível.
3. Em 128 e 256 px, preservar leitura de armadura, tecido, espada e chama. Em 64 px, priorizar silhueta e separação dos equipamentos; não exigir leitura de riscos e rebites.
4. Detalhes pequenos devem ser seletivos e estáveis ao longo do ciclo, sem cintilação ou ruído de textura.
5. Desgaste deve corresponder a bordas, contato e junções, sem transformar aço em superfície de pedra.
6. Mudanças de geometria não devem bloquear articulações nem comprometer capa, pernas e mãos durante os passos; a observação do ciclo pertence à etapa 9.

## Ordem de execução após esta preparação

Primeiro placas e silhueta; depois elmo e espada; tecido e couro; materiais; chama. Ajustar iluminação e produzir o quadro comparativo apenas na etapa 8. A aprovação artística desse quadro antecede a renderização do ciclo completo. Não adicionar animações, cenários ou equipamentos novos para preencher a apresentação.
