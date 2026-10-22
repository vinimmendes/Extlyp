# Caminhada linear com pistola

Abra `linear-walk.html` junto de `pistol-layers-v1.png`. Prévia independente; nenhuma alteração ao player Unity.

Regra solicitada: direita vai de trás até a frente, esquerda faz o contrário; ambas passam pela pose central sem inverter. Somente os extremos invertem a direção. Tronco, cabeça, braços e pistola ficam fixos. O botão Pose parada usa exatamente a passagem central.

Para fase p em [0,1): q = -1+4p até p=0,5; depois q=3-4p. Posição longitudinal dos pés: direita=330+220q pixels, esquerda=330-220q. Duração provisória: 1,2 segundo. 16 posições inspecionáveis pelo botão de avanço, interpolação linear contínua na reprodução. Não é uma medição de Darkwood.

Revisão de perspectiva: quadris fixos, comprimento projetado proporcional a |q|. Ao atravessar a vertical (q=0), a projeção longitudinal se anula e as pernas ficam ocultas sob o tronco. Ao se afastar desse ponto, a perna se estende progressivamente para frente ou para trás. Calça e bota são renderizadas em segmentos para manter a ponta da bota voltada para a frente em ambos os sentidos. A camada do tronco continua imóvel e cobre as coxas. É uma aproximação 2D da projeção, sem redesenho anatômico ou modelo 3D.

Causa do teste anterior: `Art/assemble-walk-test.py` reproduzia oito desenhos gerados sem impor monotonicidade das pernas. Esta prévia usa duas peças persistentes e deslocamentos opostos calculados, mantendo identidade e trajetória. As peças passam atrás da camada fixa do corpo.

Arte: ferramenta integrada image_gen, referência `Assets/Art/Player/player-rustic-pistol-east-v2.png`. Arquivo novo, sem substituir arte aprovada. A geração não respeitou a grade de três células iguais; a prévia usa retângulos de origem explícitos do atlas observado (2172 × 724). A arte é um estudo de recortes e pode precisar de ajuste artístico dos encaixes; a trajetória é determinada pelo código.

Verificação: assertions no HTML verificam progressão monotônica de cada meio ciclo, extremos, fechamento e pose central. Não foi executada validação Unity nem build.
