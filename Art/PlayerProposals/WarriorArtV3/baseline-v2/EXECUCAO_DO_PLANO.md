# Execução do plano v2

## Base preservada

A versão original permanece em `/workspace/warrior-3d/`. Todas as mudanças do modelo e do rig foram feitas na pasta v2.

## Geometria e câmera

O elmo foi reconstruído por seções afuniladas, com crista e dobradiças. As ombreiras passaram a usar cascas com espessura e placas sobrepostas; o peitoral recebeu faces próprias; as placas das botas substituíram os volumes pequenos arredondados. Luvas receberam placa superior e dedos curvos. O modelo possui 23 ossos e duas cadeias IK.

A câmera de jogo foi inclinada aproximadamente 27 graus em relação à vertical para revelar parte do corpo, aproximando a leitura da referência. Uma câmera estritamente vertical e uma de perspectiva também foram mantidas. Os renders neutros foram revisados antes dos renders finais.

## Movimento

Foram implementados controles IK dos pés, alvos dos joelhos, conexão de canelas, pés, antebraços e mãos, movimento de bacia e tórax e orientação das armas com comprimentos de ossos constantes. As coxas acompanham a bacia, eliminando o abaixamento separado da versão anterior.

O apoio ocupa 60% do ciclo de cada perna, com recuo uniforme. O retorno usa uma curva de quinto grau e um arco de elevação. Há compensação do tornozelo durante a rolagem da ponta do pé. As curvas de animação têm tangentes periódicas calculadas. O ciclo principal tem 36 quadros e duração de 1,2 segundo a 30 fps; a alternativa tem 30 quadros e duração de 1 segundo.

## Materiais e efeitos

O tecido recebeu vermelho mais escuro e menos sheen. O metal recebeu rugosidade maior e relevo mais discreto; couro e costuras foram diferenciados. A chama recebeu forma alongada, gradiente amarelo-laranja e animação de shape keys. A capa e o pano frontal receberam influência suave de ossos adicionais.

## Problemas encontrados e corrigidos

- A primeira auditoria detectou a base do couro das botas abaixo das solas. O volume foi ajustado, e a rolagem foi compensada na ponta do pé.
- A primeira auditoria de tecido detectou interseções do pano frontal com as duas coxas. O pano foi estreitado e curvado para fora; bordas antigas foram removidas.
- A interpolação inicial era linear entre quadros calculados. Ela foi substituída por curvas Bezier com tangentes periódicas para suavizar a emenda do ciclo.

## Evidência de validação

Os relatórios em `validation/` contêm as medidas finais. O teste numérico do rig passou: erro máximo de alvo IK inferior a 0,1 mm, conexão das articulações dentro da precisão numérica, desvio de contato durante apoio inferior a 0,01 mm por quadro e poses coincidentes na emenda. A diferença de velocidades em uma sondagem fina da emenda ficou abaixo de 0,03 m/s; isso é uma aproximação numérica, não uma prova de derivada exata do solver IK.

A auditoria de tecido passou em todos os 36 quadros para as regiões livres da capa e do pano frontal contra seis superfícies das pernas. Não representa validação exaustiva de todas as possíveis colisões da cena.

O render usa mais amostras que a v1 na câmera principal. OpenImageDenoise não está disponível nesta compilação; não foi alegado uso de denoiser. A comparação de contornos usa normalização de limites, sem alegar correspondência exata de pose com a referência pintada.

## Skills aplicadas e lacunas

`blender-skill-harmonizer` definiu a ordem e a referência canônica; `blender-modeling`, `blender-materials` e `blender-lighting` orientaram geometria e aparência; `blender-animation` orientou curvas e Actions; `blender-rendering` orientou PNGs sequenciais; `animation-quality-gate` orientou folhas de contato e revisão visual. O método de regiões manuais da segmentação foi usado para mapear a referência.

As skills não fornecem um rig humano completo nem contato de pés pronto. Essas partes e seus verificadores foram implementados no projeto em `bpy`, sem alterar skills públicas. A conexão MCP permanece ausente; a execução foi feita diretamente no Blender local.

## Limitações artísticas restantes

Esta versão melhora o modelo estilizado e sua animação, mas não reproduz todos os detalhes e o acabamento pintado do sprite de referência. As costas e superfícies ocultas foram interpretadas. Tecido e fogo usam animação controlada por ossos e shape keys, sem simulação física.
