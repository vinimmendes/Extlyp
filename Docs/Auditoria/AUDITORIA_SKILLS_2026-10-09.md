# Auditoria do Cyber Extraction com as skills recomendadas

**Data:** 09/10/2026, America/Sao_Paulo. **Resultado:** 27 achados — 4 P1, 19 P2 e 4 P3.

A base tem bons controles de autoridade na expedicao, inventario com operacoes atomicas e politica de bots separada da percepcao. As prioridades sao fechar as brechas do acampamento, preparar identidade/visibilidade para jogadores externos, corrigir fluxos de interface e validar o mapa grande em dispositivos reais.

Esta etapa instalou e aplicou as nove skills a revisao; **nao implementou as correcoes de gameplay**. Foram adicionados orientacoes, relatorio, evidencias e provas reproduziveis. As alteracoes preexistentes no jogo foram preservadas.

## Escopo, metodo e limites

- Revisao dos **41 scripts de runtime**, **5 scripts de editor**, scripts operacionais, cena/prefabs de rede, configuracoes e documentos de design.
- Unity 6000.0.84f1; Netcode for GameObjects 2.13.3; Transport 2.7.4; Input System 1.20.1; URP 17.0.4.
- As nove skills e suas referencias pertinentes foram usadas como criterios. Mapeamento em [SKILLS_APLICADAS.md](SKILLS_APLICADAS.md).
- Compilacao C# atual de runtime e editor passou, usando Roslyn e referencias do cache local da Unity. Isso **nao executa a geracao de RPCs/IL post-processing nem constitui build de Player**.
- **16 provas de regras C# confirmaram os comportamentos esperados da auditoria**, incluindo defeitos existentes. Elas executam os fontes reais de inventario, acampamento, politica de bots e fila. Adaptadores minimos substituem tipos Unity/Netcode somente para compilar; serializacao, engine e transporte nao sao exercitados.
- Sintaxe dos cinco scripts PowerShell operacionais validada. A instalacao usa o helper oficial da skill-installer.
- Nao foram executados Play Mode, partida completa, APK, teste de toque, rede sob latencia, Profiler ou reinicio de salas. Nenhum valor de FPS ou ganho de desempenho e afirmado.
- Logs anteriores serviram de contexto historico. Eles nao comprovam o estado atual, especialmente os testes antigos com dez participantes.

**Classificacao:** P1 deve ser tratado antes de disponibilizar a clientes nao confiaveis; P2 afeta robustez, experiencia ou validacao; P3 melhora manutencao e evolucao. Lacunas documentadas do prototipo continuam sendo lacunas, e nao sao apresentadas como regressoes novas.

**Evidencia:** “reproduzido” significa prova C# executada; “estatico” significa fluxo diretamente observado no fonte; “candidato” exige teste/medicao para confirmar impacto em execucao.

## P1 — integridade e confianca

### A01. Pesca e cozimento do acampamento confiam em resultados declarados pelo cliente

**Evidencia reproduzida:** `Assets/Scripts/Camp/CampModels.cs:86` aceita CatchFish por `PierRepaired` e Duration entre 4 e 120. `:91` aceita CookFish por duracao/slot, sem validar fisgada, tensao ou calor no servidor. `:97` transforma qualquer Result diferente de 0/1 em peixe assado; Result=999 produz qualidade 3. `CampProgress.cs:78` aplica essas acoes recebidas na conexao.

**Impacto:** um cliente modificado pode declarar capturas e preparo perfeito sem realizar os minigames. `CatchItem` usa o Id escolhido pelo cliente como semente, permitindo procurar Ids favoraveis. O limite de 64 acoes por pedido limita o lote, mas nao prova que a atividade aconteceu. A documentacao ja reconhece a validacao incompleta desses minigames.

**Melhoria:** servidor cria identificador/semente e controla inicio, progresso, conclusao e recompensa da atividade; cliente envia entradas. Rejeitar enums, indices, Duration/Result invalidos e comandos fora do estado correto. Se o acampamento precisar funcionar offline, definir explicitamente como conciliar essa escolha com a economia online.

**Aceite:** comandos forjados, qualidade 999 e tentativa de escolher recompensa pelo Id nao geram itens; atividades legitimas continuam funcionando. Skill: systematic-debugging, unity-csharp-scripting.

### A02. Historico de 256 acoes permite repetir recompensas antigas

**Evidencia reproduzida:** `CampModels.cs:70` rejeita Ids presentes em AppliedActions, mas `:112` remove o primeiro ao ultrapassar 256. A prova executou uma captura, 256 transferencias legitimas e a mesma captura novamente: a recompensa foi creditada outra vez.

**Impacto:** a idempotencia vale apenas para a janela recente. A brecha pode atravessar varios pedidos de conexao; o limite de 64 por pedido nao resolve o historico.

**Melhoria:** sequencia monotona e revisao do perfil, ou registro duravel de operacoes aceitas, vinculados a identidade e emissao pelo servidor. Nao apenas aumentar o tamanho da lista.

**Aceite:** o mesmo comando nao volta a produzir efeito depois de outras acoes, reconexao ou reinicio. Skill: systematic-debugging.

### A03. Identidade do acampamento e um identificador fornecido pelo cliente

**Evidencia estatica:** `UI/NetworkBootstrap.cs:123` valida o formato de Session e `Camp/CampProgress.cs:101` le/persiste o GUID em PlayerPrefs. Nao existe autenticacao nem comprovacao de titularidade.

**Impacto:** quem obtiver outro Session pode apresentar-se como aquele acampamento. O GUID tem alta entropia; este achado nao afirma que seja facil adivinha-lo. A lease impede uso simultaneo, mas nao comprova quem e o dono. Ausencia de contas e um limite conhecido do prototipo.

**Melhoria:** vincular o perfil a uma sessao autenticada e validar credenciais no servidor; manter o modo local anonimo como ambiente de testes, se desejado.

**Aceite:** um identificador alheio, sem credencial valida, nao autoriza acesso ao perfil. Skill: systematic-debugging; extensao da auditoria de rede existente.

### A04. Esconder sprites nao impede o cliente de receber informacoes escondidas

**Evidencia estatica:** `Players/PlayerVision.cs:167` e `Combat/VisibilityTarget.cs:19` controlam renderizadores locais. Nao ha NetworkHide/NetworkShow nem CheckObjectVisibility no jogo. A biblioteca instalada usa SpawnWithObservers=true por padrao. Slots de inventario tambem usam permissoes padrao de leitura para observadores.

**Impacto:** um cliente modificado pode consultar posicoes/estado de objetos ocultos; replicar todo o mapa tambem amplia trafego e custo de sincronizacao. O GDD ja previa filtragem pelo servidor como etapa futura.

**Melhoria:** definir interesse e visibilidade por cliente no servidor; restringir inventario privado ao proprietario quando o design permitir. Eventos audiveis precisam de fluxo espacial separado para preservar pistas sonoras de inimigos invisiveis.

**Aceite:** inspecionar os dados recebidos por um cliente e confirmar que nao revelam alvos ocultos nem inventarios privados; medir bytes/s antes/depois. Skills: performance-optimization, systematic-debugging.

## P2 — robustez e experiencia

### A05. Progresso nao sobrevive ao encerramento de todas as salas

**Evidencia estatica:** `Camp/SharedCampMemory.cs:30` usa MemoryMappedFile; `CampProgress.cs:23` usa dicionarios locais no modo sem memoria compartilhada. Nao ha armazenamento duravel. Somente a identidade e salva em PlayerPrefs.

**Impacto:** reinicio completo perde equipamentos, melhorias e alimento. E comportamento documentado do prototipo, mas limita testes longos e progressao real.

**Melhoria:** armazenamento versionado com gravacao atomica, recuperacao e backup. ScriptableObject serve para definicao de itens/receitas, nao para salvar progresso.

**Aceite:** reiniciar todas as salas conserva perfis; uma escrita interrompida nao corrompe o ultimo estado valido. Skills: unity-scriptableobjects, systematic-debugging.

### A06. Acampamento abre sem obter primeiro o estado atual do servidor

**Evidencia estatica:** `CampProgress.cs:89` inicializa Progress com um perfil novo a cada processo; a reconciliacao chega pela conexao da expedicao (`Players/PlayerInventory.cs:162`). `CampSession.Accept` limpa todas as PendingActions sem resultados individuais; `CampRepository.Apply` ignora o retorno de cada acao.

**Impacto:** apos reabrir o cliente, o jogador pode ver equipamento inicial em vez do perfil guardado no servidor. Acoes locais podem ser rejeitadas na entrada sem explicar qual falhou. A simulacao local esta documentada, mas a experiencia de confirmacao e incompleta.

**Melhoria:** sincronizar o perfil ao entrar no acampamento e devolver revisao e resultado por comando; mostrar estado pendente/confirmado e motivos de rejeicao.

**Aceite:** reabrir o cliente mostra o perfil correto antes de editar mochila; recusas preservam uma explicacao e reconciliam a interface. Skills: game-ui-ux, systematic-debugging.

### A07. Transferencias entre acampamento e expedicao precisam de transacao de ciclo completo

**Evidencia estatica:** `PlayerInventory.cs:153` esvazia o perfil e consome buff antes da conclusao de StartMatch; `:159` banca itens antes de `MatchManager.ServerParticipantExtracted` alterar Status. Falhas nas operacoes seguintes nao possuem rollback desse fluxo. Trata-se de candidato a teste de falha, nao perda/duplicacao observada em partida.

**Melhoria:** registrar expedicao e revisao, separar preparar/confirmar e definir recuperacao ao falhar spawn, sincronizacao ou armazenamento. Nao depende apenas da atomicidade de InventoryGrid.

**Aceite:** injetar falhas em cada fronteira e provar que a mochila tem um unico dono e destino contabil.

### A08. Paineis misturam altura proporcional com deslocamentos fixos

**Evidencia estatica/geometrica:** `CampController.cs:156` cria painel com metade da altura, mas seus botoes usam deslocamentos ate 285 pixels. Em 800x360, o painel mede 180 pixels: controles da bancada excedem o painel e se sobrepoem ao botao Voltar. `UI/ExpeditionFishingUI.cs:26` usa painel de 162 pixels nessa tela e botao da linha em y+150 com altura 60. Em `InventoryWindow.cs:59`, a terceira linha pode disputar espaco com o rodape em alturas pequenas. Nenhuma captura atual confirma a aparencia final.

**Melhoria:** um modelo de escala, containers/ancoras e rolagem onde necessario, com tamanhos minimos para toque. Migrar telas gradualmente para Canvas, preservando os fluxos existentes.

**Aceite:** revisar 800x360, 1280x720, 1920x1080 e telefone de tela alongada; nenhum controle fica sobreposto, cortado ou inacessivel. Skill: game-ui-ux.

### A09. Interface e controles nao aplicam safe area

**Evidencia estatica:** nenhum dos 41 scripts consulta Screen.safeArea. Toque, inventario, minimapa e HUD posicionam elementos por Screen.width/height e margens fixas.

**Impacto:** recortes, bordas arredondadas e barras do sistema podem ocupar controles. Tamanho em pixels tambem nao garante area fisica adequada para toque.

**Melhoria/aceite:** painel raiz de area segura, recalculado quando dimensao/orientacao mudar, e teste em aparelho com recorte. Habilitar tamanho de texto/controles ajustavel. Skills: game-ui-ux, unity-input-system.

### A10. Entradas de jogo e overlays possuem regras de bloqueio diferentes

**Evidencia estatica:** `PlayerController.cs:74` bloqueia mochila/pesca e exclui Quickbar do clique de tiro, mas nao o botao Sair do HUD. `TouchControls.cs:89` exclui regioes enumeradas manualmente. `InventoryWindow.cs:45` impede atalho I durante atividade no camp, enquanto `:70` ainda permite abrir pelo botao. `CampController.Update` continua atualizando cozimento quando a mochila esta aberta.

**Impacto:** clicar Sair pode tambem solicitar um tiro antes de OnGUI encerrar a conexao; abrir mochila por caminhos diferentes pode deixar atividade/controles ativos por tras. Os caminhos estao no codigo; a ordem exata deve ser verificada em Unity.

**Melhoria/aceite:** um estado de interface decide quem recebe entrada; botao e atalho usam a mesma regra. Abrir/fechar overlays nao dispara, nao movimenta e nao altera uma atividade escondida. Skills: game-ui-ux, unity-input-system.

### A11. Cancelar a janela nao cancela uma abertura de bau pendente

**Evidencia estatica:** `InventoryWindow.cs:30` Close limpa open, dragSlot e worldChest, mas nao pendingChest. `:49` abre a janela automaticamente quando o bau pendente passa a OpenState=2.

**Impacto:** depois de solicitar um bau e pressionar Esc/fechar, a grade pode abrir posteriormente. Nao e uma falha de autorizacao do servidor, que valida distancia; e um fluxo de cancelamento incompleto.

**Melhoria/aceite:** cancelar a intencao local, limpar referencias e definir se Esc deve cancelar tambem a abertura no servidor. Pedido cancelado nao reabre a UI. Skills: game-ui-ux, systematic-debugging.

### A12. Pacote Input System esta presente, mas gameplay usa dispositivos diretamente

**Evidencia estatica:** existe `Assets/InputSystem_Actions.inputactions`, porem nao ha consumidores InputActionReference/InputActionAsset/PlayerInput nos scripts do jogo. PlayerController, CampController e outros consultam teclas/dispositivos diretamente; IsTouch depende somente da existencia de Touchscreen.current.

**Impacto:** trocar de dispositivo, remapear, separar contexto de UI e oferecer acessibilidade exige alterar varios locais. A presenca de touchscreen pode escolher toque mesmo quando outro dispositivo esta sendo usado.

**Melhoria/aceite:** mapas Gameplay/UI/Camp com acoes compartilhadas; escolha do dispositivo ativo; deadzones e sensibilidade ajustaveis. Preservar o modo atual de mira silenciosa e tiro separado. Gamepad e opcao de produto, nao requisito novo imposto pela auditoria. Skill: unity-input-system.

### A13. Movimento depende da ida e volta ao servidor sem previsao local

**Evidencia estatica:** `PlayerController.cs:95` envia RPC a cada FixedUpdate de 0,02 s; simulacao e transformacao sao do servidor. O RPC legado usa entrega confiavel por padrao, e a cena usa tick de rede 30 Hz. Nao existe sequencia/ack de entradas, previsao e reconciliacao.

**Impacto candidato:** atraso percebido e fila de comandos sob perda/latencia; isso nao foi medido. 50 chamadas de entrada por segundo nao equivalem necessariamente a 50 pacotes UDP, pois pode haver agrupamento.

**Melhoria:** primeiro testar RTT 50/100/200 ms e perda 1/3/5%; depois avaliar snapshots de entrada com sequencia, transporte adequado e previsao/reconciliacao mantendo autoridade.

**Aceite:** registrar atraso de resposta, correcao de posicao e bytes/s; soltura para movimento/tiro em prazo definido. Skills: performance-optimization, systematic-debugging.

### A14. Bot escolhe loot sem considerar capacidade ou possibilidade de coleta

**Evidencia reproduzida/estatica:** BotSituation nao tem capacidade; `BotController.cs:170` pontua memoria por valor/distancia. `LootPickup` nao despawna quando ServerAddStack falha. A politica real continua Collect com KnownLoot, sem possibilidade de comunicar mochila cheia.

**Impacto candidato:** um bot pode permanecer tentando coletar um item que nao cabe, ate mudar de prioridade. A recorrencia em partida deve ser reproduzida.

**Melhoria/aceite:** considerar item/quantidade/capacidade e descartar temporariamente destinos sem progresso. Mochila cheia nao prende o bot no mesmo loot. Skill: game-ai.

### A15. Bots iniciam busca de extracao tarde para algumas regioes do mapa grande

**Evidencia calculada:** BotDecision libera Extract com 60 segundos restantes. Perto dos abrigos `(0,600)` e `(0,-600)`, a saida mais proxima tem centro a 400,125 unidades. A 4 unidades/s, descontando generosamente a area da zona e o raio do jogador e somando os 3 s de permanencia, sao **102,168 s**, antes de obstaculos, combate ou lentidao.

**Impacto:** um bot nessas regioes no ultimo minuto nao consegue chegar a tempo. Isso nao afirma que todo bot permanecera no abrigo; demonstra que a politica nao considera a distancia restante. A diferenca entre abrigos tambem merece playtest de equilibrio.

**Melhoria:** manter a proibicao de concluir extracao antes do ultimo minuto, se essa continuar sendo a regra, mas permitir deslocamento antecipado com base no custo da rota e margem. Separar planejar saida de efetivar extracao.

**Aceite:** testes de partida partindo de regioes distantes; bots chegam quando a rota e viavel, sem extrair cedo. Skills: game-ai, level-design.

### A16. Navegacao local pode selecionar destino final em outra regiao conectada

**Evidencia estatica:** `MonsterNavigation.cs:144` limita objetivo a janela local de 30x30; `NearestNode(localGoal, false)` escolhe o no livre mais proximo sem testar componente conectado ao inicio. O algoritmo para sem alternativa caso nao encontre caminho ao no escolhido.

**Impacto candidato:** objetivos em obstaculos, blocos desconectados ou desvios maiores que a janela podem deixar a IA parada, embora haja outro destino aproximado acessivel. Nao foi observado em Play Mode nesta auditoria.

**Melhoria/aceite:** selecionar objetivo alcancavel, registrar falha e ter rota global por pontos/regioes quando necessario. Testar parede longa, alvo dentro de obstaculo, quina e mudanca de objetivo. A heuristica octile e heap existentes estao adequados ao movimento diagonal. Skill: game-ai.

### A17. IA e geracao do mapa sao os primeiros candidatos a medir no servidor

**Evidencia estatica:** cada navegacao tem 3.721 celulas e pode consultar fisica para reconstruir a grade. Bots percorrem Health.All/LootPickup.All; monstros verificam participantes a cada FixedUpdate. StartMatch instancia a populacao do mapa grande de uma vez; parar IA a distancia nao elimina objetos de rede e visuais.

**Impacto candidato:** picos no inicio e concentracao de custo com varios agentes ativos. Nenhum gargalo ou ganho esta confirmado por Profiler.

**Melhoria/aceite:** medir inicio de partida e pior combate nas tres salas, ms por tick, GC, RSS e sincronizacao. Se necessario, repartir trabalhos, compartilhar dados de navegacao por raio, limitar buscas por frame e usar indice espacial. Skill: performance-optimization.

### A18. HUD, consultas e efeitos geram trabalho/alocacoes repetidos

**Evidencia estatica:** 11 scripts possuem OnGUI; varios criam GUIStyle e strings por chamada. PlayerInventory.Count cria InventoryGrid/array; Quickbar consulta Reserve durante desenho. Tiros e investidas usam RaycastAll/CircleCastAll; cada som de pistola cria GameObject/AudioSource e depois destroi.

**Impacto candidato:** GC e custo de UI/efeitos, principalmente com mais entidades. A frequencia de chamadas OnGUI varia por evento; nao foi medida.

**Melhoria/aceite:** medir GC.Alloc e tempo dos metodos; substituir os maiores custos por eventos, caches, buffers adequados e pool de audio. Nao trocar todas as estruturas apenas por estilo. Skills: performance-optimization, game-ui-ux.

### A19. Mesh e recursos da memoria compartilhada nao possuem liberacao explicita

**Evidencia estatica:** PlayerVision cria `new Mesh()` e OnDestroy destroi GameObjects, sem Destroy(mesh). SharedCampMemory mantem MemoryMappedFile, accessor e Mutex sem IDisposable; EnableShared substitui a referencia sem liberar a anterior. Recursos podem durar pelo processo, mas isso e inadequado a ciclos de editor/reinicializacao.

**Melhoria/aceite:** liberar mesh criado e implementar ciclo de encerramento da memoria compartilhada; considerar tambem caches de materiais/texturas criados por SpriteFactory em Play Mode. Repetir 20 entradas/saidas e comparar memoria nativa/handles. Skills: unity-csharp-scripting, performance-optimization.

### A20. Reinicio interrompe salas antes de saber se a atualizacao compila

**Evidencia estatica:** Restart-Server.ps1 chama Stop antes de Start; Start chama Update. Update sozinho preserva a versao anterior ate compilar, mas esse beneficio e perdido pelo caminho de reinicio. Start tambem nao aguarda prontidao real de cada porta antes de informar inicio.

**Impacto:** uma compilacao falha durante reinicio deixa salas paradas e pode perder a memoria de progresso. E diferente do caminho normal de atualizacao, que ja tem uma boa protecao.

**Melhoria/aceite:** compilar/preparar primeiro, fazer troca controlada e verificar prontidao. Em falha de compilacao, a versao em execucao continua acessivel; evitar executar reinicio real como teste sem ambiente isolado. Skills: unity-build-pipeline, systematic-debugging.

### A21. Builds de teste e release precisam de configuracoes distintas

**Evidencia estatica:** ServerBuild fixa versao 1.1/code 2 e Android sem keystore customizado; Windows servidor e build Player comum. ShowTemporaryServerRestart=true e selecao de teste rapido aparecem no cliente atual. Os scripts usam BuildOptions.None e checam BuildReport corretamente.

**Impacto:** o APK atual e adequado a teste, mas nao estabelece processo de assinatura/versionamento de release. Um cliente tambem pode pedir modo rapido na primeira conexao, sujeito apenas a compatibilidade com quem ja esta na sala.

**Melhoria/aceite:** perfis Teste/Release, incremento de versao, assinatura externa segura, modo de sala decidido pelo servidor e controles de diagnostico removidos de release. O modulo Dedicated Server e melhoria opcional, nao requisito para o servidor atual. Skill: unity-build-pipeline.

### A22. Smoke test de itens ainda espera comportamentos do inventario anterior

**Evidencia estatica:** `Diagnostics/ItemSmokeServer.cs:40` espera exatamente +3 pickups ao matar bot, embora o bot agora tambem carregue pistola, pilhas de municao e possivelmente outros itens. `ItemSmokeClient.cs:41` espera deposito sem bandagens depois de equipar, mas a nova extracao preserva a mochila, e o proprio arquivo anuncia validacao de pesca sem executar CatchFish.

**Impacto:** pode produzir falso negativo e mensagens de sucesso que nao descrevem o que foi testado. Os logs antigos nao resolvem essa divergencia.

**Melhoria/aceite:** validar conservacao do conteudo exato da mochila antes/depois, atualizar expectativas de Pack/Chest e testar realmente captura/cozimento. Executar em servidor/clientes isolados com o protocolo atual. Skill: systematic-debugging.

### A23. Verificacao atual nao cobre os principais riscos novos

**Evidencia estatica:** checks de editor cobrem regras puras e alguns prefabs, mas nao ha suites dedicadas com asmdef de testes do projeto. Smokes usam esperas fixas, cenarios pequenos e versoes anteriores. Faltam evidencias atuais de multitouch, minigames, nove monstros, rotas longas, desconexao/extracao sob falha e latencia.

**Melhoria/aceite:** matriz por risco, com EditMode para regras e PlayMode/processos/aparelhos para engine e rede. Pesca deve incluir segurar/soltar, foco perdido e baixa taxa de quadros; UI precisa de capturas atuais em resolucoes distintas. Smokes devem ter deadlines e condicoes, sem depender de uma espera arbitraria para provar readiness. Skills: systematic-debugging, game-ai, game-ui-ux.

## P3 — manutencao e produto

### A24. Balanceamento e definicoes estao espalhados no codigo

**Evidencia estatica:** GameConfig, ItemCatalog, BotProfile e MonsterArchetypes usam constantes/tabelas; valores de municao, recarga e custos tambem aparecem diretamente em scripts/UI. Indices numericos representam varias fases.

**Melhoria/aceite:** criar definicoes editaveis de arma, item, receita, inimigo e modo; enums explicitos para fases. Compartilhar dados imutaveis, mantendo HP atual, mochila e estado de rede por instancia. Comecar pela pistola e receitas, com validacao de valores e estabilidade dos IDs/protocolo. Skill: unity-scriptableobjects.

### A25. Documentacao mistura requisitos atuais e historicos

**Evidencia estatica:** GDD ainda tem trecho de movimento autoritativo no jogador e lista servidor/crafting fora de escopo, enquanto secoes posteriores descrevem o contrario. EXPANSION_SETUP registra protocolo 6 e NetworkBootstrap usa 10. SERVER_SETUP inclui validacao historica de dez clientes apesar da capacidade atual de cinco.

**Melhoria/aceite:** resumo unico do estado atual, historico identificado por data/versao e tabela de validacao com fingerprint. Nenhuma instrucao atual deve exigir capacidade/protocolo antigos. Skills: systematic-debugging, unity-build-pipeline.

### A26. Escala do mapa e ensino das mecanicas precisam de playtest estruturado

**Evidencia estatica:** mapa 900x1380, velocidade 4, partida 300 s, pontos gerados com semente fixa e muitos tipos de monstro introduzidos no mesmo modo. Nao ha fluxo explicito de tutorial ou telemetria de aprendizado. Resultados de interface continuam usando abreviacoes e instrucoes de teclado no Android.

**Melhoria/aceite:** medir tempo ate primeiro loot, primeiro confronto, chegada a saida, mortes por tipo e tempo gasto sem objetivo. Ensinar mira, extracao, mochila e minigames em etapas com risco controlado; tornar prompts adequados ao dispositivo. Planejar tambem sinais sonoros de preparacao das habilidades, alem do tiro existente. Preservar os pilares de tensao/escuridao; nao alterar numeros sem evidencia. Skills: level-design, game-ui-ux.

### A27. Pipeline compila, mas nao torna verificacoes parte da entrega

**Evidencia estatica:** ServerBuild verifica resultado de build e Update usa fingerprint, ambos positivos. Nao ha workflow de CI do projeto nem chamada aos checks antes de publicar o executavel atualizado; o caminho automatico nao gera APK nem roda testes, como documentado.

**Melhoria/aceite:** processo opt-in de verificacao/entrega com regras, checks de prefabs, build por plataforma, smoke isolado e registro de versao/fingerprint. Comparar cliente e servidor pelo mesmo estado do codigo. Nao adicionar novos testes automaticamente a todo inicio cotidiano do servidor se esse nao for o fluxo desejado. Skill: unity-build-pipeline.

## O que vale preservar

- Movimento, dano, recarga e bandagem sao decididos no servidor; movimento rejeita NaN/Infinity e limita magnitude.
- RPCs de bau e Imitador conferem remetente, distancia, vida, estado da partida e paredes.
- InventoryGrid rejeita operacoes invalidas e mantem conservacao em transferencias; receitas devolvem materiais se faltar espaco.
- A fila nao reinicia a espera a cada entrada; smokes e regras existentes ja oferecem uma base util.
- Bots usam percepcao limitada e memoria de loot; politica separada facilita teste sem engine. A* tem heuristica octile, heap e validacao do corpo nas arestas.
- BuildReport e checado; a atualizacao normal usa copia de compilacao, verifica fingerprint e prepara o build antes de parar a versao antiga.

## Ordem proposta de melhorias

| Etapa | Entrega concreta | Achados | Validacao |
| --- | --- | --- | --- |
| 1 | Comandos de camp validados e idempotentes | A01–A02 | Provas de rejeicao e minigames legitimos |
| 2 | Identidade, perfil sincronizado e persistencia | A03, A05–A07 | Reabrir cliente, reconectar, reiniciar e injetar falhas |
| 3 | Entrada de UI unificada e layout para celular | A08–A12 | Capturas, aparelho, multitouch e foco |
| 4 | Bots com destinos viaveis e extracao planejada | A14–A16 | Mochila cheia, rota longa, ultimo minuto |
| 5 | Visibilidade e desempenho medidos | A04, A13, A17–A19 | Dados recebidos, RTT/perda, Profiler e memoria |
| 6 | Entrega repetivel e documentacao atual | A20–A25, A27 | Smokes atualizados e perfis de build |
| 7 | Iteracao de balanceamento e aprendizado | A26 | Playtest com jogadores e metricas |

Sem estimativa de FPS, dificuldade final ou prazo de refatoracao antes de medir/implementar. Skills fornecem metodo e referencias; nao certificam que o jogo esta pronto.

## Evidencias e reproducao

- [rules-evidence.json](rules-evidence.json): 16 provas executadas, zero resultados inesperados. Algumas provas confirmam vulnerabilidades; nao significa que essas vulnerabilidades foram corrigidas.
- [source-evidence.json](source-evidence.json): hashes dos fontes/skills, inventario e limites geometricos de trajeto.
- `Tools/Audit/Run-AuditProbe.ps1`: compila e executa regras reais, deixando a saida em `Logs/skills-audit/`. Usa o runtime Roslyn/.NET da Unity instalada; pode receber outro UnityRoot compativel.
- `Tools/Audit/Collect-SourceEvidence.py`: atualiza inventario/hashes e calculos. A margem da zona de extracao e deliberadamente otimista; nao simula paredes.
- `Logs/skills-audit/current-runtime.dll` e `current-editor.dll`: resultado local da checagem C#. Os response files locais foram derivados dos caches anteriores de compilacao com saidas novas e referencia ao runtime atual. Dependem do cache deste workspace; nao substituem uma pipeline limpa.

Uma nova mudanca nos fontes exige revalidar os achados afetados. Para fechamento real de A01/A02, converter as provas em expectativas de rejeicao; para desempenho, anexar capturas/medidas de aparelho. Nao fechar itens so porque a compilacao passou.


