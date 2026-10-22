# Etapa 3 — gameplay e interface

Data: 16/10/2026. Situação: **revisão estática concluída**. Nenhuma correção foi implementada e nenhum teste, build ou sessão Unity foi iniciado.

## Resultado

**6 achados: 4 P2 e 2 P3.** Não foi identificado novo P1 neste recorte. Isso não encerra os P1 da etapa 2 nem constitui liberação do jogo. Todos os achados abaixo vêm de leitura de código/configuração; impacto em partida ainda precisa das provas previstas.

| ID | Prioridade | Achado | Tipo de evidência |
| --- | --- | --- | --- |
| E3-01 | P2 | Mouse conectado sobrescreve a mira do gamepad | Ordem direta das atribuições |
| E3-02 | P2 | Melee fere mímico disfarçado sem acionar sua revelação | Caminhos de dano e estado distintos |
| E3-03 | P2 | Aviso de melee dos monstros não cobre todo o setor que causa dano | Divergência geométrica entre desenho e regra |
| E3-04 | P2 | Botão de interação do gamepad também pode submeter um botão da HUD | Conflito estático de bindings/contextos; duplicidade depende do foco e da ordem de eventos |
| E3-05 | P3 | Inventário não oferece fluxo completo por seleção/Submit | Lacuna de navegação/ações sem arraste |
| E3-06 | P3 | Aranha pode exibir liberação de teia após cancelar preparação | Transição visual sem confirmação do término; cenário depende de permanecer parada |

Serrilhado, zoom, alinhamento visual do minimapa e sensação de caminhada continuam **pendências de validação visual**. Não foram reclassificados como resolvidos com base em logs ou capturas antigas.

## Base e rastreabilidade

Revisada a cópia `Builds/Auditoria/2026-10-16_083542_382c7a/Projeto`, preparada na etapa 1. Hash agregado do manifesto: `fcdf78077dca71119a05d701dad579149239ccedd107a3d16fe11c0ca32241cd`.

**66 arquivos conferidos por SHA-256**, todos iguais ao manifesto e aos respectivos arquivos atuais: 55 C#, 2 prefabs, 1 shader, 2 assets de configuração, 4 documentos, manifesto de pacotes e versão da Unity. Evidência: `Logs/auditoria/2026-10-16_083542_382c7a/etapa3/source-hashes.json`. A lista identifica fontes consultadas integralmente ou por trechos direcionados; não significa revisão integral de cada linha nem de todos os assets do projeto.

Versões mantidas: Unity 6000.0.84f1, NGO 2.13.3, Transport 2.7.4, Input System 1.20.1 e URP 17.0.4. Nenhuma atualização de engine/rede. Linhas abaixo pertencem à cópia congelada.

Skills aplicadas: investigação sistemática, C# Unity, game-ai, level-design, game-ui-ux, unity-input-system, ui-ugui e performance-optimization. A revisão de desempenho identifica pontos para medir; não promete ganho nem diagnostica gargalo sem Profiler. As regras locais de não executar testes nesta etapa prevalecem sobre receitas de verificação das skills.

## Matriz de cobertura

| Área | O que foi rastreado | Resultado / limite |
| --- | --- | --- |
| Movimento e stamina | Entrada, sequência de RPC, normalização, servidor, corrida, gasto e regeneração | Simulação permanece no servidor; entrada usa RPC não confiável com sequência e prazo de validade. Sem medição de responsividade/latência. |
| Caminhada e animação | Personagem, aranha e atlas de couraçado/corrosivo/espreitador | Fase avança pela distância renderizada; há tratamento de salto de posição. Calibração de passada e comportamento sob interpolação precisam de partida. Outros arquétipos ainda usam apresentação própria/provisória. |
| Combate à distância | Carregador, cooldown, recarga, saída do cano, dispersão, raio e teias | Tiro/munição resolvidos no servidor; há proteção entre corpo e cano e contra tiro de dentro de obstáculo. Não houve validação da física/RPC gerado. |
| Melee e proteção | Preparação, impacto, direção, stamina, cancelamento e alvos | Limite de três entidades, sem múltiplos hits por collider, e checagem de parede. E3-02/E3-03 cobrem reações e comunicação do alcance. Veneno/ambiente seguem categorias distintas do dano físico. |
| Monstros | Percepção, memória, ruído, retorno, stagger, investida e habilidades | Há busca, leash e recuperação de travamento; preparação de ataques e habilidades varia por arquétipo. Não foi observado comportamento em execução. |
| Efeitos de área | Projétil de teia/ácido, impacto, área, lentidão e dano periódico | Servidor mantém efeitos após morte do emissor, limita quantidade e verifica paredes para área. Teias podem ser removidas com tiro. Balanceamento e leitura visual pendentes. |
| Bots | Percepção, memória de loot, capacidade, planejamento e extração | Há filtro por capacidade, abandono de destino e ETA de extração; não repetir A14/A15 como se o código antigo permanecesse igual. |
| Navegação | A* local, diagonal, corpo, rotas de mapa e falha parcial | Heurística octile, CircleCast, fallback alcançável e desvio adicional presentes. Falha em quina, raio de corpo e comporta precisa de cenário físico real. |
| Catacumbas | Salas, conexões, cobertura, objetivos, ferramenta, spawns e encontros | Layout autorado com oito entradas, três saídas, chave/manivela, atalho e encontros. Rotas e justiça da distribuição não foram comprovadas por leitura. |
| Floresta | Pontos de entrada, mobiliário, saídas, planejamento de bots | Revisão direcionada das regras compartilhadas e trechos de ambientação; não foi revisado cada setor/prop da Floresta. Campanha espacial ainda necessária. |
| Visão e iluminação | Percepção, alcance, raios, lookup, composição cinza/cor e camada de captura | Catacumbas: 80° a 110°, alcance 10; shader usa textura capturada do mundo e mistura cor/luminância. Renderização e serrilhado não validados. |
| Câmera e minimapa | Tamanho, limites, projeção e marcadores | Câmera das Catacumbas em 7,5; marcadores usam a projeção do terreno, sem prender saída à borda do recorte. Sem captura atual. |
| Objetivos e contexto | Proximidade, parede, cancelamento por deslocamento/dano e rótulo da HUD | Texto é calculado pelo contexto; ação é cancelada no servidor quando inválida. Retorno do texto da corrente não foi reproduzido nesta etapa. |
| HUD e menus | Área segura, Canvas, tema, bloqueio, foco e eventos | Existe raiz uGUI com safe area e escala virtual de 600; dois temas compartilham geometria; menus avisam que partida continua. Contraste e toque precisam de avaliação visual. |
| Inventário/ficha/comércio | Janela de saque/base, equipamento, comparação, pontos, confirmação e arraste | Base e expedição seguem contextos distintos; comércio/ficha compartilham o painel com modos. E3-05 cobre operação por controle; dimensões/fontes reais pendentes. |
| Controles | Teclado, mouse, gamepad, toque, perda de foco e interfaces | Vocabulário de InputActions e captura de ponteiros presentes. E3-01/E3-04 revelam limites da escolha de dispositivo/contexto. Sem teste multitouch/aparelho. |

## Achados e critérios de aceite

### E3-01 — a mira do mouse vence a do gamepad mesmo sem movimento do mouse

**Evidência:** [PlayerController.cs:198](C:/Users/Vinni/Documents/GitHub/Extlyp/Builds/Auditoria/2026-10-16_083542_382c7a/Projeto/Assets/Scripts/Players/PlayerController.cs:198). `ReadInput` aplica `GamepadAim` e em seguida entra no bloco de `Mouse.current` quando há mouse e câmera. A linha 208 atribui novamente `facing` pela posição do cursor sempre que ele está fora da HUD e longe do centro do personagem. Não há teste de movimento recente do mouse nem prioridade do stick ativo.

**Impacto:** no PC com mouse conectado, mover o analógico direito pode não orientar a mira; o cursor estacionado continua determinando direção. É uma divergência direta no fluxo, sem observação prática nesta auditoria.

**Direção de correção:** escolher dispositivo de mira ativo com critério explícito e evitar sobrescrever stick ativo por cursor parado; manter transição para mouse quando ele for realmente usado.

**Aceite:** mouse conectado e parado não impede mira pelo stick; mover mouse retoma mira de mouse; abrir/fechar UI ou alternar para toque não produz salto/tiro involuntário. Etapa 6, no mesmo cliente da campanha de controles.

### E3-02 — o dano melee não revela o mímico

**Evidência:** [PlayerShooter.Melee.cs:88](C:/Users/Vinni/Documents/GitHub/Extlyp/Builds/Auditoria/2026-10-16_083542_382c7a/Projeto/Assets/Scripts/Players/PlayerShooter.Melee.cs:88). O golpe chama `Health.ServerTakeDamage` diretamente. `MeleeTarget`, linhas 94–104, admite Monster sem excluir disfarçados. A revelação por dano está em `MonsterArchetypes.cs:82–85`, dentro de `ProjectileDamage`, usado pelos tiros; a outra entrada é abrir o disfarce. A assinatura de dano em `Monster.cs:79` leva a `HandleStagger`, que não chama `Reveal`.

Enquanto o estado não muda, `StepArchetype` retorna cedo para `Disguised` (`MonsterArchetypes.cs:134`) e a aparência mantém o baú. O golpe não depende de raycast atingir o collider trigger: a seleção percorre `Health.All`.

**Impacto:** é possível, pelo fluxo atual, aplicar golpes não letais repetidos ao baú/mímico sem iniciar sua reação de revelação; uma morte pode ocorrer ainda disfarçado. Não foi reproduzido em partida.

**Direção de correção:** centralizar a reação ao dano aceito para que melee e projétil acionem a regra adequada do mímico. Preservar regras intencionais específicas de projétil, como placas frontais, sem aplicar automaticamente todo `ProjectileDamage` ao melee.

**Aceite:** primeiro golpe melee não letal revela o mímico e inicia reação válida uma única vez; golpe letal resolve morte/loot sem estado residual; tiro e interação continuam funcionando. Etapa 5: transição se houver seam fiel; etapa 6: objeto real e dois observadores.

### E3-03 — telegraph do melee não representa a área que pode causar dano

**Evidência:** [MonsterAppearance.cs:35](C:/Users/Vinni/Documents/GitHub/Extlyp/Builds/Auditoria/2026-10-16_083542_382c7a/Projeto/Assets/Scripts/Enemies/MonsterAppearance.cs:35). O aviso é um sprite circular esticado, centrado a metade do alcance à frente, com escala `(reach, reach*1.4)`. `SpriteFactory.cs:77–82` cria círculo de uma unidade. O impacto em `MonsterBehaviour.cs:166–167` aceita distância até `attackRange` e ângulo até **65° de cada lado**, com linha de visão física.

**Divergência geométrica:** em coordenadas do ataque, um ponto perto de `0,95 × alcance` e a `60°` está dentro do setor de dano, mas sua coordenada lateral é aproximadamente `0,823 × alcance`, maior que o semieixo lateral de `0,7 × alcance` do aviso. Isso é dedução das fórmulas, não teste de colisão executado.

**Impacto:** jogador pode estar fora da área colorida e ainda receber dano. A janela de aviso de 0,3 s também exige avaliação sob latência; isso não foi medido.

**Direção de correção:** desenhar a mesma regra espacial que resolve o golpe ou ajustar a regra aprovada em conjunto, compartilhando alcance/abertura. Definir se obstáculos recortam o aviso para manter leitura coerente.

**Aceite:** pontos no limite e nas laterais têm correspondência entre aviso e dano; paredes bloqueiam como anunciado; dois clientes conseguem perceber e evitar o golpe na janela pretendida. Etapa 5: geometria compartilhada; etapa 6: física, renderização e rede.

### E3-04 — interação de gamepad e Submit da HUD disputam o mesmo botão

**Evidência:** `GameInput.cs:30–42` associa interação e Submit ao botão sul; o contexto próprio desabilita Gameplay em overlays, mas não governa o módulo uGUI separado. [UguiGameHud.cs:93](C:/Users/Vinni/Documents/GitHub/Extlyp/Builds/Auditoria/2026-10-16_083542_382c7a/Projeto/Assets/Scripts/UI/UguiGameHud.cs:93) instala `InputSystemUIInputModule` com `AssignDefaultActions`. Botões Heal/Swap/Reload podem continuar selecionados durante Gameplay; não estão entre os desabilitados para navegação em `:69–70`, e `Click` encaminha suas ações em `:278–280`.

O pacote local Input System 1.20.1 confirma `UI.Submit` em `*/{Submit}` (`DefaultInputActions.inputactions:632–642`) e o usage Submit de `buttonSouth` (`Devices/Gamepad.cs:118`). É fonte auxiliar do cache instalado, fora da cópia congelada.

**Cenário pendente:** selecionar um botão de ação da HUD, permanecer em Gameplay e pressionar botão sul junto de objeto interagível. A mesma entrada pode acionar `InventoryWindow.Interact` e Submit do botão selecionado. Dependendo da ordem/contexto, uma ação pode bloquear a outra ou ambas serem encaminhadas. Não há reprodução que determine qual efeito aparece primeiro.

**Impacto candidato:** recarga, cura, troca ou interação inesperada ao usar controle. Não é falha de autorização do servidor; é ambiguidade de intenção de entrada.

**Direção de correção:** dar posse explícita da entrada ao contexto ativo e definir navegação da HUD durante Gameplay; coordenar EventSystem e mapas, sem depender apenas de bloquear comandos quando um menu está aberto.

**Aceite:** cada pressão produz somente a ação do contexto escolhido; foco não fica em ação de HUD de modo inesperado ao voltar de menus; PC, gamepad e toque continuam utilizáveis. Etapa 6: foco + mesma pressão, incluindo abrir/fechar inventário e menus.

### E3-05 — Submit não substitui arraste nas operações de inventário

**Evidência:** [UguiInventoryPresenter.cs:123](C:/Users/Vinni/Documents/GitHub/Extlyp/Builds/Auditoria/2026-10-16_083542_382c7a/Projeto/Assets/Scripts/UI/UguiInventoryPresenter.cs:123). `UguiInventorySlot.OnSubmit` chama `Tap`, como clique. `Tap` só trata grid 1 como coleta quando é recipiente do mundo; na base, selecionar um item do baú seguro não inicia transferência. Grid 0 seleciona peça para equipar e grid 2 executa equipamento; reorganização, depósito no recipiente e transferência privada dependem de Begin/Drag/Drop. O prefab ItemSlot já possui navegação e o presenter define foco inicial: o problema não é ausência total de suporte ao controle.

**Impacto:** suporte a gamepad/teclado fica incompleto para administrar mochila/baú sem ponteiro. Prioridade P3 por ser uma lacuna do suporte existente; não transforma gamepad em requisito novo do produto.

**Direção de correção:** definir seleção de origem/destino ou ações contextuais acessíveis por Submit/Cancel, preservando arraste e comparação de equipamento. Se gamepad não integrar o escopo de entrega, documentar a limitação em vez de anunciar suporte completo.

**Aceite:** mover entre mochila e baú, reorganizar, depositar/coletar e cancelar funcionam sem mouse, ou suporte parcial fica explicitamente delimitado. Etapa 6: campanha de navegação da base e saque.

### E3-06 — animação da aranha trata cancelamento como liberação de teia

**Evidência:** [WeaverVisual.cs:54](C:/Users/Vinni/Documents/GitHub/Extlyp/Builds/Auditoria/2026-10-16_083542_382c7a/Projeto/Assets/Scripts/Enemies/WeaverVisual.cs:54). Ao sair de preparação, `wasPreparing && active` inicia a sequência de liberação por nove frames sem verificar se chegou ao instante de emissão. Movimento limpa esse estado, mas, se permanecer parada, os frames de liberação são exibidos. `MonsterBehaviour.BeginReturn` cancela `AbilityState` e `AbilityEnds` antes de lançar projétil. O caso correspondente em `MonsterAnimationVisual.cs:71` guarda o instante previsto e distingue término de cancelamento.

**Cenário pendente:** preparação interrompida pelo retorno ao home/leash; corpo permanece parado por obstáculo/falha de rota; cliente recebe saída de preparação e apresenta liberação sem projétil. Não afirmar que isso acontece em toda interrupção: deslocamento logo em seguida suprime a sequência.

**Impacto candidato:** comunica ataque que o servidor cancelou, dificultando entender a janela de ameaça.

**Direção de correção:** diferenciar conclusão e cancelamento na apresentação, usando estado/instante ou evento de emissão confirmado. Não alterar habilidade apenas para encaixar a animação.

**Aceite:** conclusão normal mostra lançamento; cancelamento não mostra lançamento falso, inclusive com aranha imóvel e com atraso entre snapshots. Etapa 6, agrupada com habilidades e animação.

## Pedidos anteriores do usuário — situação nesta base

| Pedido/relato | Evidência estática atual | O que continua pendente |
| --- | --- | --- |
| Monstros e protagonista sem patinar | Personagem, Weaver e três atlas de monstros usam distância percorrida para fase; jogador anda a 4 e corre a 5,4 | Medir passada em mundo, marcha lenta, diagonal, parede, sprint e cliente remoto. Não generalizar a todos os monstros provisórios. |
| Câmera mais próxima | `DungeonCameraSize=7.5` e câmera com limites do mapa | Preferência de enquadramento no corredor, telas largas e contexto do combate |
| Percepção só aumenta abertura | Catacumbas: 80° a 110° em efetivo 1–20; alcance fixo 10. Floresta: 110° a 140° e alcance 16 | Conferir efeitos no dono, observadores, IA e shader; não assumir que pedidos sobre Catacumbas redefinem toda a Floresta |
| Camada cinza furada pela visão | Shader mistura cena e luminância conforme visão/oclusão; Renderer2D tem captura de camada habilitada | Ordem de desenho, textura, cor e oclusão no desktop e Android |
| Serrilhado na sombra | Lookup com 192 raios PC/128 celular, filtro e guarda por superfície; relato/imagens históricas preservados | Reproduzir mesmo corredor, parede reta, quina, rotação e movimento. Causa residual não foi determinada. |
| Símbolo desliza no minimapa | Projeção de saída e terreno é compartilhada; saída fora do recorte some em vez de ser presa à borda | Alinhamento durante movimento, recorte, mapa ampliado e ambos os temas |
| Texto de soltar corrente não some | Contexto usa proximidade/parede; ação ativa é cancelada por afastamento/dano, e HUD recalcula interação | Atraso entre cliente e servidor, sair de alcance e voltar, abrir menu, morte e conclusão |
| Spawn aleatório, sem dividir ponto | Aprovação sorteia entre slots livres; bots retiram pontos de lista sem reposição | Vários humanos, bots e reconexão; verificar corpos livres e distribuição em partidas repetidas |
| Mais ambientação e desafio | Catacumbas têm detalhes por setor, cobertura, 18 encontros autorados e objetivos; conteúdos não são apenas chão vazio | Densidade percebida, ensino, rota segura e custo dos desvios exigem playtest |
| Ataque à distância e em área | Sistema possui ácido com poça e teia com área; lista das Catacumbas usa Weaver, Stalker e Shell | Variedade de área/dano neste mapa ainda limitada; não confundir existência do Acid no catálogo com presença no mapa |
| Aranha com armadilhas/emboscada | Weaver lança uma teia e deixa teias locais; não há estado específico de esconder/esperar nem lançamento triplo nesta lógica | Proposta de produto a fechar: exemplo de três teias não foi tratado como lista rígida de implementação |
| Espreitador rápido fora de visão | Velocidade usa `observedLit` do **alvo escolhido**: 1,5 quando iluminado por ele, 4,6 caso contrário | Definir se visão de qualquer jogador deve desacelerar; hoje um terceiro observador não entra nessa regra. Validar coop/PvP antes de promover a bug de especificação. |
| Extração por manivela | Fluxo mantém item, operação e habilitação da saída | Usuário havia confirmado funcionamento; não desfazer essa evidência nem tratá-la como teste da revisão atual |

Critério de mapa: Darkwood orienta abandono e atmosfera; Bullet Echo orienta cobertura, leitura de ataque e rotas alternativas. Esses referenciais não substituem teste de percurso nem justificam copiar layout/arte. Não há recomendação de colocar armadilhas letais em todos os corredores: primeiro medir gargalos, visibilidade e contrajogo.

## Reavaliação histórica

Reavaliados A08–A19 e A26, total de **13 achados históricos**, por leitura direcionada. Sem executar as antigas provas.

| Histórico | Situação atual pela leitura | Verificação necessária |
| --- | --- | --- |
| A08 — painéis e proporções | Composição mudou para uGUI e escala virtual; os exemplos antigos não descrevem todos os painéis atuais | Geometria/textos em 800×360, 1280×720, 1920×1080, 4:3 e telefone alongado horizontal |
| A09 — safe area ausente | Há `UiLayout.SafeScreen` e raiz com anchors atualizados | Recortes e paisagem esquerda/direita em aparelho |
| A10 — bloqueio divergente | Coordenador e `BlocksGameplay` centralizam parte do fluxo | E3-04 e sequência de abrir/fechar overlays; não marcar resolvido em execução |
| A11 — baú reabre após cancelar | `Close` limpa pendingChest e solicita cancelar abertura | Esc, toque, afastamento, recusa e múltiplos clientes |
| A12 — dispositivos diretos | Há InputActionMaps próprios e módulo uGUI; ainda há leitura direta de mouse/stick | E3-01/E3-04/E3-05, troca de dispositivo e foco |
| A13 — entrada confiável sem sequência | RPC de movimento agora é não confiável, sequenciado e com validade; previsão local continua ausente | Latência e perda; não transportar métricas antigas para esta versão |
| A14 — bot ignora capacidade | Seleção usa `FreeFor` e há memória/abandono de loot | Mochila cheia, exemplar único, destinos inacessíveis e disputa de coleta |
| A15 — bot só inicia saída no último minuto | ETA e deslocamento antecipado presentes; concluir extração segue regra temporal | Regiões distantes, lentidão, combate e mecanismo fechado |
| A16 — destino local desconectado | Existe fallback parcial alcançável, desvio e rotas específicas dos mapas | Parede longa, quina, corpo grande, bloqueio e alteração da comporta |
| A17 — custo de IA/mapa | Marcador `CE.PathSearch`, timers de percepção e caches estão presentes | Medir geração, buscas, ms/tick e entidades; sem diagnóstico de gargalo |
| A18 — trabalho/GC de HUD e efeitos | HUD principal usa eventos e refresh, mas consultas, strings e casts com arrays continuam em caminhos revisados | GC.Alloc e custo real de UI, tiros, efeitos e visão; não otimizar só pela contagem de chamadas |
| A19 — liberação de recursos | `PlayerVision.OnDestroy` libera mesh/material/textura; SharedCampMemory implementa Dispose na etapa 2 | Ciclos de entrada/saída e handles/memória nativa; caches completos ainda não auditados |
| A26 — escala e aprendizado do mapa | Catacumbas possuem layout/setores/encontros/objetivos e Floresta possui rotas | Tempo até ameaça, até loot e saída; justiça de spawns, ensino e pausas de tensão |

Não fechar definitivamente esses IDs nesta etapa. A20–A25 e A27 ainda precisam da revisão correspondente e consolidação; a etapa 3 não substitui a revisão do pipeline e dos verificadores.

## Campanha agrupada preparada

| Lote | Cenários | Etapa / evidência esperada |
| --- | --- | --- |
| Combate | Tiro junto à parede, recarga parcial/cancelada, melee até três alvos, mímico, aviso lateral, stamina e categorias de dano | 5: regras e geometria onde fiel; 6: física, RPCs e dois observadores |
| Movimento e animação | Parar, andar, correr, diagonal, lentidão, colisão, morte, teleporte, interpolação e troca de arma | 6: mesma rota e observação local/remota; passada por distância, sem prometer perfeição pelo código |
| IA e habilidades | Teia/ácido, interromper preparação, ocultação, mudança de alvo, stalker com dois observadores, leash, quinas e corpo bloqueado | 6: estado, alvos, dano, avisos e recuperação |
| Mapas e objetivos | Oito entradas Catacumbas, cinco Floresta; bot/humano sem repetir; três saídas; chave/manivela perdida; atalho; tempo de rota | 5: contratos puros; 6: percurso real e mecanismos com concorrência |
| Visão e mapa | Percepção mínima/máxima, cor/cinza, parede/quina, rotação/movimento, símbolos e zoom | 6: capturas atuais com contexto, tema, resolução e plataforma; comparação visual com relatos do usuário |
| UI e entrada | Dois temas, safe area, ficha/comércio, inventário/saque, mouse+gamepad, Submit/contexto, toque simultâneo e perda de foco | 6: sequência completa de ações, foco e ausência de ações involuntárias; Android pendente se não houver aparelho |
| Rede e desempenho | Reaproveitar cenários com RTT/perda controlados, pior combate/visão e navegação | 6: medir resposta, ms/tick/frame, GC, memória e tráfego com hardware/configuração registrados; sem números inventados |

Executar esses lotes junto aos da etapa 2 usando o mesmo ambiente isolado e builds/processos reutilizados, conforme o plano. Não abrir Unity individualmente para cada achado. Se a física, renderização, IL/RPC ou Input System forem essenciais, um adaptador C# fora da Unity não confirma o caso.

## Limitações e próxima etapa

Esta revisão não inclui inspeção visual atual dos prefabs/atlas no Editor, travessia completa dos mapas, árvore completa de assets, orientação em aparelho, garantia de acessibilidade, frame time ou cobertura integral de todo inimigo/minigame. Há trechos de apresentação e estado ainda provisórios; ausência de arte final não é automaticamente bug de gameplay.

O desenho dos seis achados identifica causa ou fronteira concreta e o teste necessário, sem modificar o jogo. Próxima etapa: **4 — identificar possíveis reduções**, incluindo conteúdo obsoleto, duplicações e oportunidades de consolidar, com dependências e benefício separado por repositório/disco/build/memória. Não excluir arquivos durante essa etapa.
