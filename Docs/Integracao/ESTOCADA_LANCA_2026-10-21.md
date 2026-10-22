# Estocada de lança — 21/10/2026

Implementada apresentação de estocada com duas mãos no personagem atual, modelo refinado da arma
e sprite do item no chão. Fonte e prévias em `Prototypes/PlayerWalk3D/SpearAttack/README.md`.

## Integração e regras

- `PlayerMeleeActionArt` registra `SpearAttack`, `SpearGrip` e `SpearWeapon`, com 40 quadros,
  impacto no quadro 16 e enquadramento da arma de 5,1 unidades. Corpo e arma usam o mesmo índice
  calculado pelos timestamps replicados. Ataques recebidos em andamento mostram a fase atual.
- A lança entra em `Supports`, recebendo a redução para 60% no caminho de movimento autoritativo
  já existente. Modificadores de equipamento continuam multiplicativos; corrida continua bloqueada.
- `PlayerCharacterVisual` mantém o tronco da lança acima da arma, como no machado, para mostrar
  os dedos sobre a haste. As pernas permanecem vinculadas à distância percorrida.
- `MeleeWeaponVisual` usa o atlas com transformação identidade. Se faltar apenas esse atlas,
  a arma procedural usa os metadados da ação e uma empunhadura adaptada ao cabo original.
  Se faltar a ação, continua a apresentação genérica anterior.
- `ItemVisualCatalog` altera somente `WorldIcon` da lança para `Items/Weapons/SpearWorld.png`.
  Ícone de inventário preservado. Espadas e machado mantêm seus atlas e definições.
- Mantidos os tempos de 0,30/0,50 s, dano, stamina, alcance de 2,2 unidades, setor de impacto,
  limite de alvos, obstrução por paredes, comandos, direção ajustável até o impacto e travamento
  na recuperação. Sem RPC novo, dano por animação ou avanço automático do personagem.
- Morte, cancelamento, troca e fim da partida removem a apresentação pelos caminhos existentes;
  material, cor, neblina e arco do dono permanecem vinculados aos sistemas atuais.

## Revisão e avaliação pendente

Modelo isolado e poses compostas inspecionados. Ajustado o recuo para mostrar as duas mãos
fora da cabeça. Ponta exportada em aproximadamente 2,2 unidades no impacto, sem alterar a regra
de alcance. Revisão estática realizada; nenhum teste automatizado, sessão Unity ou build executado,
conforme preferência do projeto. Não validado em execução no jogo ou na rede.

Primeiro teste do usuário: atacar parado e andando, ajustar a mira na preparação, repetir
estocadas, tentar correr, observar outro jogador, trocar para espada/machado, conferir neblina
e aparência da lança no chão. Avaliar empunhadura, continuidade das pernas, impacto e retorno.

## Correção do movimento lateral e dos saltos

Relato do usuário: a lança se movia de lado e parecia pular quadros. Evidência nos dados
anteriores: y mudava de aproximadamente -0,539 para zero entre os quadros 8 e 9; a empunhadura
saltava 0,7004 unidade nessa passagem. O GIF possuía os 20 quadros: a descontinuidade estava
nas poses exportadas, seguida de quatro amostras praticamente estacionárias na extensão.

Corrigida a trajetória para y local=-0,45 constante, altura e direção fixas, com movimento
somente longitudinal. Ponta em aproximadamente (2,15349;-0,45) no impacto, distância 2,20
ao centro. Corpo/arma renderizados novamente em 40 quadros: 15 anteriores ao impacto e
25 a partir dele. O amostrador existente resulta em 50 quadros/s nos tempos de 0,30/0,50 s.
Nenhuma outra definição de arma ou regra de combate mudou.

Revisão dos dados de exportação em `motion-review.json`: maior passo 0,081235, abaixo de 0,10;
distância entre mãos 0,40 constante; altura fixa; guarda final igual à inicial. Prévia normal
com 40 quadros de 20 ms (800 ms) e lenta com os mesmos quadros a 80 ms (3,20 s). Prancha
`transicoes-lanca-suave.png` permite inspecionar as posições próximas ao impacto.

Versão de 20 quadros preservada em `AntesCorrecao40Quadros/`, incluindo fonte, renders,
atlas, metadados e scripts. Correção feita sobre os controles existentes, sem reconstruir
o modelo da lança, regravar a arte do chão ou alterar espada/machado.
Inspeção de renders e revisão estática concluídas; sem Unity, testes automatizados ou builds.

## Revisão de intensidade

Por solicitação do usuário, aumentado o recuo de 0,28 para 0,45 unidade e o percurso de
estocada de aproximadamente 0,3835 para 0,5535. Recuo máximo no quadro 9, seguido de avanço
mais rápido até o impacto no 16; mantidos 40 quadros, 0,30/0,50 s, trajetória y=-0,45 e alcance.
Pico de deslocamento entre quadros de 0,098837, dentro do limite de 0,10 e cerca de 22%
acima da versão anterior. Tronco e ombros participam mais; a transferência de peso é apenas visual.

Guarda elevada a uma altura fixa de 1,70 unidade Blender para manter a mão traseira legível
quando recuada junto ao ombro. Distância entre mãos preservada em 0,40 unidade de jogo.
Renders e prévias atualizados; código C# e regras não precisaram mudar nesta revisão.
Versão anterior preservada em `AntesGolpeIntenso/`. Prévia atual: `estocada-lanca-intensa.gif`.
Inspeção visual e dos dados de produção concluída; sem Unity, testes automatizados ou builds.

## Segunda revisão de intensidade

Usuário solicitou uma estocada ainda mais intensa. Recuo desde a guarda ampliado de 0,45 para
0,62 unidade; percurso até o impacto ampliado de aproximadamente 0,55349 para 0,72349.
Maior rotação e transferência visual de peso do tronco, recuo máximo no quadro 8 e breve
reação de recolhimento no 17, após o impacto do 16. Não há avanço automático do personagem.

Mantidos 40 quadros a 50 fps, tempos de 0,30/0,50 s, y=-0,45 constante, altura fixa e ponta
a 2,2 unidades do centro no impacto. Maior passo de 0,098300 unidade, abaixo do limite de 0,10.
Atlas e prévias atualizados; nenhuma regra ou código C# alterado nesta revisão. Versão anterior
preservada em `AntesGolpeIntensoV2/`; prévia atual `estocada-lanca-intensa-v2.gif`.
Poses e dados exportados conferidos. Sem Unity, testes automatizados ou builds.
