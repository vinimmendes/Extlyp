# Aranha — entrada pelo teto, dash e cerco

Trabalho iniciado em 17/10/2026 e concluído em 18/10/2026. Revisão estática do código e inspeção dos sprites produzidos. **Sem compilação, sessão Unity, testes de gameplay ou builds nesta entrega**, conforme a preferência do projeto. Balanceamento e apresentação em partida aguardam avaliação prática.

## Entrada

Toda Weaver passa a nascer como uma teia destrutível no lugar de seu corpo. A teia tem 18 de resistência, separada dos 70 de vida da aranha. Dano à teia não concede XP nem loot e não é transferido à criatura.

Ao destruir a teia, o servidor inicia uma descida de 1,5 s. A apresentação usa um novo atlas com pernas recolhidas, abertura e apoio no pouso; o deslocamento vertical na tela e o fio são compostos no cliente. Sem ataque ou dano recebido durante a descida. O colisor é desativado nesse período. Desde a correção de 21/10/2026, um pouso ocupado procura espaço próximo em anéis de 0,25 unidade, até 2,5 unidades: respeita paredes, território e folga dos jogadores e monstros sólidos. Assim, permanecer parado sob a teia não impede a entrada em combate quando há espaço para pousar ao lado. Somente se não existir posição livre nessa busca a descida continua aguardando. Não há dano de queda no jogador. Somente a destruição da teia inicia essa entrada; proximidade, ruído ou alerta de outro monstro não a acionam.

## Dash

- Preparação de 0,7 s com direção travada e indicador existente de trajetória.
- Alcance máximo de 3 unidades em 0,25 s, interrompido por paredes, obstáculos e corpos.
- Dano 15, aplicado somente ao primeiro impacto válido; o dash termina no impacto e não acumula mordida no mesmo golpe.
- Recuperação de 0,6 s e intervalo de 5 s após terminar/cancelar o avanço.
- Dano durante a preparação interrompe o dash e inicia recuperação. O avanço iniciado mantém sua trajetória.
- Pode iniciar entre 1,5 e 5 unidades do alvo observado. Não acompanha alvo oculto durante o avanço.

A checagem de colisão é uma varredura com o raio real da aranha, executada no servidor. Decisões e deslocamento não dependem da animação do cliente.

## Cerco

O cerco substitui o disparo único e as armadilhas que a Weaver antes deixava sob o próprio corpo.

- Preparação de 0,8 s com a animação existente.
- Três áreas de teia em ferradura, a 3,3 unidades da posição observada do alvo.
- Aviso no chão por 0,8 s antes da lentidão; depois, teias ativas por 8 s.
- O plano busca uma passagem com folga de 0,55 unidade até além das áreas e rejeita posições sobre paredes/obstáculos. Também rejeita uma passagem coberta por teia ou ácido já existente.
- As áreas precisam ter linha livre até a aranha e espaço para seu raio. Se nenhuma disposição passar nas verificações, não há cerco.
- Avisos e teias podem ser destruídos por tiro, usando a regra existente. A lentidão continua sem acúmulo.
- Intervalo mínimo de 6 s; não cria outro cerco com teias/avisos em até 12 unidades do alvo.
- Aranhas a até 18 unidades compartilham a oportunidade de ataque especial: uma preparação, avanço ou recuperação por vez. Mordida comum e movimentação continuam disponíveis às demais.

Os avisos usam os efeitos da partida e o caminho existente de filtragem por visibilidade. Não são enviados como uma revelação de posições ocultas.

## Arquivos e animações

Fonte aprovada preservada: `Prototypes/SpiderPale/aranha-palida.blend`.

Novo arquivo editável: `Prototypes/SpiderPale/Actions/aranha-palida-acoes.blend`, produzido com Blender 5.2.2 LTS pelo MCP disponível. A cena aprovada foi anexada como cena adicional; não foi recriada a geometria da aranha. As skills extras de animação/render/harmonização não estão instaladas; foram utilizadas as quatro skills Blender disponíveis, sem instalar dependências.

- Dash: frames 201–232; preparação 201–212, avanço 213–218 e recuperação 219–232. Os segmentos são ajustados às durações replicadas.
- Descida: frames 301–332.
- Teia de teto: frame 401, geometria de fios original.
- Novos atlases `Dash.png` e `Descend.png`: 2048×1024, 32 células de 256×256, alpha e orientação +X.
- `Nest.png`: 256×256, alpha, na mesma escala de frame de 2,4 unidades dos assets existentes.
- Caminhada, lançamento, materiais e arquivos aprovados anteriores preservados.
- `build_actions.py`, `render_actions.py` e `package_actions.py` registram autoria, exportação e empacotamento. A autoria exige uma cena aprovada recém-anexada; não deve ser reaplicada sobre a cópia já animada.

Prévia da produção (não é captura Unity):

![Poses novas da aranha](../../Prototypes/SpiderPale/Actions/actions-preview.png)

## Rede e avaliação

Protocolo **34 → 35**, por novos estados replicados e novo tipo de aviso de teia. Cliente e servidor devem incorporar a mesma revisão. Nenhum executável foi reconstruído ou servidor reiniciado.

Avaliar primeiro: quebra da teia sem recompensa antecipada; descida sem ataques; pouso ocupado; dash bloqueado por cobertura; um dano por avanço; cancelamento da preparação por dano; passagem do cerco em corredores; vários indivíduos próximos; ocultação/reexibição sem repetir animações históricas; morte, retorno ao território e fim de partida.

Não há aceite visual em partida, comprovação de desempenho ou verificação de serialização/RPCs nesta entrega.
