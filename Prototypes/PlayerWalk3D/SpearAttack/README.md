# Estocada de lança — 21/10/2026

Produzida no Blender 5.2.2 LTS pelo MCP oficial, usando o personagem rústico original
em `../VisualV1/personagem-rustico.blend`, preservado sem alterações. Referência artística:
`Assets/Resources/Items/IconsV2/Spear.png`, empacotada na fonte Blender.

## Entrega

- `protagonista-estocada-lanca.blend`: fonte editável com o rig original das pernas,
  ação de braços/tronco, dois controles de empunhadura e modelo refinado da lança.
- `Upper/001.png` a `040.png`: tronco e braços separados das pernas, em RGBA de 256 × 256.
- `Weapon/001.png` a `040.png`: arma renderizada nas mesmas poses, separada do corpo.
- `SpearGrip.json`: empunhadura traseira, mão de apoio e posição da ponta por quadro.
- `lanca-detalhe.png` e `lanca-chao.png`: apresentação isolada e sprite de mundo.
- `estocada-lanca.gif` e `estocada-lanca-suave.gif`: composição dos atlas reais, 40 quadros de
  20 ms, com pernas em repouso e duração de 0,80 s. `estocada-lanca-lenta.gif`: os mesmos quadros
  a 80 ms cada, em um quarto da velocidade, sem interpolação artificial.
- `poses-lanca.png`, `pose-*.png` e `leitura-tamanho-jogo.png`: pranchas e leitura em tamanho reduzido.
- `manifest.json`: enquadramentos, impacto, timings e GUID do sprite de mundo.
- `transicoes-lanca-suave.png`: quadros consecutivos da aproximação ao impacto e retorno.
- `motion-review.json`: revisão numérica dos contatos, trajetória, deslocamentos e duração do GIF.
- `AntesCorrecao40Quadros/`: fonte, renders, recursos e scripts anteriores à correção.
- `AntesGolpeIntenso/`: versão de trajetória reta antes do aumento de intensidade.
- `AntesGolpeIntensoV2/`: primeira revisão de intensidade, preservada para comparação.
- `estocada-lanca-intensa-v2.gif`: prévia atual com recuo ampliado e maior impulso do tronco.

## Movimento e modelo

Mão direita atrás conduzindo o impulso, esquerda à frente guiando a haste. Os contatos são fixos,
a 0,60 e 1,00 unidade de jogo da extremidade traseira, respectivamente. A arma mede aproximadamente
2,25 unidades; no impacto, a mão direita está em x≈0,50349 e a ponta em x≈2,15349.
Toda a trajetória usa y local=-0,45, ao lado direito do personagem. A distância da ponta ao centro
no impacto continua aproximadamente 2,20 unidades. Altura, inclinação e direção da haste são fixas.
Esses valores descrevem a geometria exportada, não uma validação de alcance em execução Unity.

Quadros 1–8: recuo mais profundo; 9–15: impulso; 16: impacto em 0,30 s; 17: reação curta;
18–34: recolhimento; 35–40: acomodação. Reprodução a 50 quadros/s, com recuperação total de 0,50 s.
Primeiro e último quadros compartilham a guarda. Rampas suaves de velocidade mantêm um avanço
rápido sustentado, com aceleração e desaceleração contínuas. O tronco acompanha com giro e transferência
de peso mais marcados; a haste só se desloca longitudinalmente. A altura permanece fixa em 1,70
unidade Blender, elevando a guarda ao peito para mostrar as duas mãos no recuo maior.
Não existe avanço automático do personagem.

O modelo possui haste de madeira, ponta larga em folha com nervura central e fio chanfrado,
encaixe de ferro, amarração escura e proteção simples na extremidade traseira. Os objetos
`GEO-refined-spear-*` são filhos do controle traseiro; a mão dianteira deriva do mesmo eixo da haste.

## Exportação e integração

Corpo: 3,4 unidades por célula de 256 pixels. Arma: 5,1 unidades por célula, ou 50,196078 pixels
por unidade. Ambos os atlas têm 2048 × 1280, oito colunas, 40 quadros e pivô central.
Os quadros da arma já incluem posição e orientação; usar transformação local identidade.
Corpo e arma seguem o mesmo índice amostrado pelo relógio de rede. O tronco fica acima da arma.

Arte do chão em `Assets/Resources/Items/Weapons/SpearWorld.png`. Somente `WorldIcon` da lança
é substituído no catálogo; o ícone original de inventário permanece. O sistema anterior continua
como alternativa quando faltam recursos, incluindo arma procedural se faltar apenas o atlas da arma.

Produção: `Tools/Blender/SpearAttackAnimation.py` via MCP sobre a fonte rústica original;
`build()` recusa sobrescrever uma entrega existente. `revision=True` é reservado a revisões
explícitas desta ação após preservar uma cópia incremental. A v1 está preservada nesta pasta.
Para retomar a ação existente sem reconstruir o modelo, usar `animate()` na fonte correta:
substitui somente os canais gerados dos braços/tronco e das empunhaduras; preserva malhas,
materiais e rig das pernas. Depois renderizar ambos os conjuntos de 40 quadros e empacotar.
`render_frames('Upper'/'Weapon',first,last)` gera as camadas; `render_isolated(hero=True/False)`
gera os isolados. `Tools/Blender/PackageSpearAttack.py` empacota recursos e gera prévias dos atlas.
O empacotamento mantém os GUIDs existentes e não modifica o catálogo automaticamente.
O sprite de chão existente não é regravado; o modelo isolado e sua arte permanecem preservados.

## Evidência e avaliação pendente

Poses inspecionadas visualmente, incluindo recuo, impulso e impacto. Revisão numérica: deslocamento
máximo por quadro de 0,098300 unidade (na primeira correção: 0,081235; antes: cerca de 0,7004);
y constante em -0,45; distância entre
as mãos de 0,40; altura constante; diferença entre a guarda inicial e final igual a zero.
O GIF foi relido e contém 40 quadros de 20 ms, total de 800 ms. Esses são dados de produção dos
assets, não validação do jogo. Integração revisada estaticamente. Sem testes automatizados, sessões Unity ou builds; não validado
em execução. Primeira avaliação pelo usuário: atacar parado/andando, ajustar a mira, repetir ataques,
observar outro jogador, trocar de arma, conferir neblina, pernas e aparência da lança no chão.

## Primeiro aumento de intensidade solicitado pelo usuário

Recuo ampliado de 0,28 para 0,45 unidade desde a guarda; a mão traseira chega a x=-0,05.
Percurso até o impacto ampliado de aproximadamente 0,3835 para 0,5535 unidade, com pico de
deslocamento por quadro cerca de 22% maior. Recuo máximo no quadro 9 e impacto no 16,
com mesmo alcance e duração total. Maior rotação e leve transferência visual de peso do tronco,
sem deslocamento automático do personagem. A elevação constante das mãos evita que o ombro
encubra a mão traseira. Contatos na haste, distância entre mãos e linha lateral preservados.

## Segundo aumento de intensidade

Recuo ampliado novamente, de 0,45 para 0,62 unidade desde a guarda; mão traseira chega a x=-0,22.
Percurso de estocada de aproximadamente 0,72349 unidade, cerca de 31% maior que na primeira revisão
de intensidade. Recuo máximo no quadro 8, impulso até o impacto no 16 e reação de recolhimento
de 0,035 unidade no 17. Rampas de velocidade mais curtas sustentam o avanço, sem saltos acima
do limite de 0,10 por quadro. Maior rotação e transferência visual de peso do tronco; sem deslocar
o personagem pela animação. Trajetória, altura, contatos, alcance e duração mantidos.
