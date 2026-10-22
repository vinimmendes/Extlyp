# Estocada de lança — 21/10/2026

Produzida no Blender 5.2.2 LTS pelo MCP oficial, usando o personagem rústico original
em `../VisualV1/personagem-rustico.blend`, preservado sem alterações. Referência artística:
`Assets/Resources/Items/IconsV2/Spear.png`, empacotada na fonte Blender.

## Entrega

- `protagonista-estocada-lanca.blend`: fonte editável com o rig original das pernas,
  ação de braços/tronco, dois controles de empunhadura e modelo refinado da lança.
- `Upper/001.png` a `020.png`: tronco e braços separados das pernas, em RGBA de 256 × 256.
- `Weapon/001.png` a `020.png`: arma renderizada nas mesmas poses, separada do corpo.
- `SpearGrip.json`: empunhadura traseira, mão de apoio e posição da ponta por quadro.
- `lanca-detalhe.png` e `lanca-chao.png`: apresentação isolada e sprite de mundo.
- `estocada-lanca.gif`: composição dos atlas reais, com pernas em repouso e duração de 0,80 s.
- `poses-lanca.png`, `pose-*.png` e `leitura-tamanho-jogo.png`: pranchas e leitura em tamanho reduzido.
- `manifest.json`: enquadramentos, impacto, timings e GUID do sprite de mundo.

## Movimento e modelo

Mão direita atrás conduzindo o impulso, esquerda à frente guiando a haste. Os contatos são fixos,
a 0,60 e 1,00 unidade de jogo da extremidade traseira, respectivamente. A arma mede aproximadamente
2,25 unidades; no impacto, a mão direita está em x=0,55 e a ponta em x=2,20, no eixo da mira.
Esses valores descrevem a geometria exportada, não uma validação de alcance em execução Unity.

Quadros 1–8: recuo de 0,30 s; quadro 9: impacto; 10–12: conclusão da extensão; 13–20: recolhimento.
Recuperação total de 0,50 s. Primeiro e último quadros compartilham a guarda. O recuo passa ao lado
direito do corpo para manter as duas mãos visíveis de cima. Não existe avanço automático do personagem.

O modelo possui haste de madeira, ponta larga em folha com nervura central e fio chanfrado,
encaixe de ferro, amarração escura e proteção simples na extremidade traseira. Os objetos
`GEO-refined-spear-*` são filhos do controle traseiro; a mão dianteira deriva do mesmo eixo da haste.

## Exportação e integração

Corpo: 3,4 unidades por célula de 256 pixels. Arma: 5,1 unidades por célula, ou 50,196078 pixels
por unidade. Ambos os atlas têm 2048 × 768, oito colunas, 20 quadros e pivô central.
Os quadros da arma já incluem posição e orientação; usar transformação local identidade.
Corpo e arma seguem o mesmo índice amostrado pelo relógio de rede. O tronco fica acima da arma.

Arte do chão em `Assets/Resources/Items/Weapons/SpearWorld.png`. Somente `WorldIcon` da lança
é substituído no catálogo; o ícone original de inventário permanece. O sistema anterior continua
como alternativa quando faltam recursos, incluindo arma procedural se faltar apenas o atlas da arma.

Produção: `Tools/Blender/SpearAttackAnimation.py` via MCP sobre a fonte rústica original;
`build()` recusa sobrescrever uma entrega existente. `revision=True` é reservado a revisões
explícitas desta ação após preservar uma cópia incremental. A v1 está preservada nesta pasta.
`render_frames('Upper'/'Weapon',first,last)` gera as camadas; `render_isolated(hero=True/False)`
gera os isolados. `Tools/Blender/PackageSpearAttack.py` empacota recursos e gera prévias dos atlas.
O empacotamento mantém os GUIDs existentes e não modifica o catálogo automaticamente.

## Evidência e avaliação pendente

Poses inspecionadas visualmente, com ajuste da mão traseira para não ficar sob a cabeça no recuo.
Integração revisada estaticamente. Sem testes automatizados, sessões Unity ou builds; não validado
em execução. Primeira avaliação pelo usuário: atacar parado/andando, ajustar a mira, repetir ataques,
observar outro jogador, trocar de arma, conferir neblina, pernas e aparência da lança no chão.
