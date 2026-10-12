# Melhorias da auditoria e impacto na gameplay

Data: 10/10/2026. Unity 6000.0.84f1, sem migracao de engine ou substituicao do Netcode/Transport. Protocolo novo: **11**.

Os 27 achados receberam trabalho de codigo, verificacao ou preparacao de medicao. Isso nao significa que todos os criterios de aparelho, carga e playtest estejam encerrados. As pendencias reais estao identificadas abaixo.

## O que muda para quem joga

| Mudanca | Impacto na partida/experiencia |
| --- | --- |
| Acampamento confirmado online | Pesca, cozimento e movimentacao de itens dependem do servidor. Ha confirmacao/recusa; recompensas nao sao mais decididas pelo cliente. Sem servidor, ainda se pode visualizar/caminhar no acampamento, mas nao alterar sua economia. |
| Salvamento persistente | Reiniciar todas as salas nao apaga automaticamente o progresso salvo. Morte e desconexao durante a partida continuam perdendo a mochila; o bau permanece protegido. |
| Recuperacao apos queda | Uma expedicao nao liquidada pode devolver o equipamento reservado no inicio apos queda completa do servidor. Loot adquirido e nao salvo na expedicao nao e inventado nessa recuperacao. |
| Identidade por chave | Um ID de perfil sozinho nao autoriza login. Preservar a chave do aparelho e necessario para reencontrar o perfil; nao ha conta com recuperacao de senha nesta etapa. |
| Visibilidade na rede | Alvos ocultos e inventarios privados deixam de ser replicados indiscriminadamente. Sons de tiros ocultos continuam como pistas aproximadas; sua origem e quantizada e o impacto oculto nao e enviado. |
| Interface e entrada | Paineis usam uma referencia de 600 unidades dentro da area segura. Fechar um painel nao deve produzir um tiro acidental. Abrir mochila durante uma atividade bloqueada e cancelamento de bau passam por regras consistentes. |
| Bots e saidas | Bots ignoram loot que nao cabe. Podem iniciar o trajeto para a saida antes do ultimo minuto, mas continuam proibidos de concluir extracao cedo. Mais bots podem sobreviver; encontros perto das saidas podem acontecer antes. |
| Navegacao | Destinos aproximados alcancaveis e um desvio por cantos de paredes ajudam a sair de situacoes que excedem a janela local. Isso pode tornar perseguicoes mais persistentes; nao aumenta vida, dano ou velocidade. |
| Movimento em rede | Entradas passam a usar sequencia e entrega de estado recente, com parada por timeout. Nao foi adicionada previsao/reconciliacao local; atraso real em internet ainda precisa ser medido. |
| Consulta de inventario e efeitos | Contagens evitam reconstruir grades; views reutilizam buffers; audio/tracadores usam pool limitado e recursos criados sao liberados. Ganho global de FPS nao foi comprovado. |
| Ajuda | Ha instrucoes de controles e do objetivo; a partida continua enquanto a ajuda e consultada. Isso nao constitui tutorial completo nem teste de aprendizado com jogadores. |

Os valores padrao de dano, velocidade, duracao de cinco minutos, mira silenciosa, tiro separado, tempos/custos da pistola e receitas foram preservados. `Assets/Resources/Gameplay.asset` centraliza definicoes editaveis de pistola/receita, mantendo estado individual e progresso fora do asset.

## Cobertura dos 27 achados

| Achado | Trabalho realizado | Limite ou verificacao restante |
| --- | --- | --- |
| A01 | Minigames do camp simulados no servidor; comandos de recompensa forjados e resultados invalidos rejeitados. | Regras, atividade e cozimento verificados; toque real em Android pendente. |
| A02 | Sequencia monotona/revisao persistida protege contra repeticao alem da janela de 256 IDs. | Testado com 256 transferencias e tentativa de replay. |
| A03 | Login assinado vinculado a chave do aparelho; replay de login e alteracao de dados assinados rejeitados. | Nao equivale a conta recuperavel, criptografia de transporte ou autenticacao do servidor. |
| A04 | Interesse por cliente, NetworkShow/Hide, inventarios privados e efeitos filtrados; sons separados. | Teste de rede passou; inspecao adversarial/carga com clientes externos continua necessaria. |
| A05 | Arquivo atomico, flush, backup e carga ao reabrir o store. | Fechamento, corrupcao e recuperacao testados. Backup operacional externo depende da implantacao. |
| A06 | Camp conecta e carrega snapshot antes de aceitar economia; acks associados ao pedido, resposta de recusa e bloqueio de pendencias. | Fluxo de carregamento/reconexao testado em processos separados. |
| A07 | Reserva, commit, rollback, liquidacao idempotente e perda de expedicao; lease expirada permite recuperar reserva abandonada. | Falha de transacao, queda de store e extracao unica testadas; kill de uma sala com outras vivas requer teste prolongado. |
| A08 | Escala/altura de referencia e ajustes de painel/rodape; sem recriar cena. | Capturas de tela sao evidencia de layout; migracao completa para Canvas nao foi feita. |
| A09 | Safe area para UI e posicionamento do botao de tiro; transformacao comum para hit-test. | Aparelho com recorte, barras e densidades reais ainda precisa ser testado. |
| A10 | Contexto de Input Actions, bloqueio de overlays, latch de tiro ao voltar, regioes de UI protegidas. | Rede/movimento testados; combinacoes de multitouch em telefone pendentes. |
| A11 | Fechar limpa intencao pendente e solicita cancelamento ao servidor somente para o abridor. | Abrir/cancelar bau foi testado pela rede. |
| A12 | Acoes centralizadas, mapas Gameplay/UI e bindings de gamepad; toque nao selecionado apenas por existir um device em desktop. | Tela de rebinding/sensibilidade e navegacao completa de menus por gamepad nao foram implementadas. |
| A13 | Entradas recentes sequenciadas/unreliable, limitacao de envio quando estavel e timeout. | Previsao/reconciliacao e matriz RTT/perda 50/100/200 ms continuam pendentes. |
| A14 | Memoria de loot guarda stack; selecao confere capacidade da mochila. | Regras de capacidade verificadas; observacao longa de bots cheios em partidas continua util. |
| A15 | Planejamento de trajeto por estimativa de distancia e margem, mantendo trava da extracao no ultimo minuto. | Politica validada. Taxa de sucesso/equilibrio por abrigo exige playtest. |
| A16 | Destino parcial alcancavel e desvio limitado por cantos de obstaculos; meta movida invalida rota. | Parede longa testada na fisica. Graph tem limites de 1.024 nos/512 expansoes e pode nao achar todo caminho de mapas arbitrarios. |
| A17 | Markers de busca e medida de spawn; telemetria opt-in com counters disponiveis. | Nao houve conclusao de gargalo nem otimizacao global de IA/carga sem medicao. |
| A18 | Count direto, buffers de grade reutilizados e pool de apresentacao de tiros. | Microbenchmark de Count medido; GC da UI inteira, GPU e FPS sustentado ainda pendentes. |
| A19 | Destroy de mesh, Dispose do store/handles e reset de caches proprios de sprites/materiais. | Recursos de engine/URP e 20 ciclos com profiler precisam de avaliacao especifica. |
| A20 | Reinicio prepara build antes de parar e iniciador aguarda prontidao. | Sintaxe verificada; scripts reais de reinicio nao foram usados para interromper as salas do usuario. |
| A21 | Development/release, versao por ambiente, exigencia de keystore em release Android e controles de teste restritos. | Nao houve assinatura/publicacao de release de loja. |
| A22 | Smokes reescritos para protocolo/grade atuais; minigame real, transferencia, buff, extracao e reconexao. | Fixture desativa dano dos bots para isolar transacoes; nao comprova combate competitivo. |
| A23 | Provas de regressao, checks Unity reais, scripts de copia isolada e smoke entre executaveis. | Android fisico, multitouch, carga, RTT/perda e partidas longas continuam pendentes. |
| A24 | Asset de dados de pistola/receitas e consumidores principais atualizados. | Numeros/estados restantes nao foram todos migrados para assets/enums. |
| A25 | README, rede do GDD e servidor atualizados; instrucoes anteriores guardadas como historico. | Auditoria de 09/10 permanece um registro do estado anterior. |
| A26 | Ajuda de controles/objetivo e eventos opt-in para observacao. | Tutorial, sinais sonoros de todos os monstros e playtest/telemetria de aprendizado nao estao concluidos. |
| A27 | Pipeline local opt-in roda checks/build/smoke com manifest; copias isoladas preservam projeto aberto. | Runner CI remoto/licenciamento e publicacao nao foram configurados. |

## Testes e evidencias

- Unity: **294 verificacoes**, incluindo 256 transferencias para validar replay. Sao checks de regras, identidade, persistencia, falhas, asset de dados e navegacao com fisica; nao representam 294 cenarios completos de partida.
- Cliente e servidor Windows gerados pela Unity: smoke de protocolo 11 passou para carregamento de camp, cozimento perfeito calculado no servidor, transferencia, alimento, movimento, tiro, cancelamento de bau, extracao unica e reconexao.
- Dano de IA foi desativado somente na fixture opt-in do smoke para tornar a transacao deterministica. A gameplay normal conserva a IA.
- Provas fora da engine foram convertidas de comportamento vulneravel para expectativas de rejeicao. Usam adaptadores e receita default de fixture; o asset real foi validado na Unity.
- No microbenchmark do Editor, 100 mil chamadas de Count em inventario vazio passaram de **50,1774 ms** no codigo anterior para **0,4329 ms** na ultima medicao. Isso nao mede FPS, GPU, inventario cheio ou dispositivo Android. O contador usado de alocacao retornou zero nas duas versoes; portanto nao e evidencia numerica de reducao de bytes.
- APK Android ARM64/IL2CPP compilado. Inventario e portal capturados e inspecionados no Windows a 800x360 e 1280x720. Os ajustes visuais finais tambem foram incluidos no APK. Nao houve execucao em aparelho Android. Resultados e hashes constam em `implementation-verification.json`.
- Logs/evidencias geradas ficam em `Logs/skills-implementation/`, e resultados selecionados acompanham este documento. Chaves privadas de teste nao foram copiadas para a documentacao.

## Como revisar e testar

1. Abra o diff do projeto e mantenha Unity 6000.0.84f1.
2. Execute `Tools/Audit/Run-AuditProbe.ps1` para regras fora da engine.
3. Execute `Tools/Validate-Unity.ps1 -Build` para checks e builds na copia `Builds/SkillsValidation`.
4. Execute `Tools/Run-SkillsSmoke.ps1` para cliente/servidor em porta e store novos.
5. Para APK de teste, use `Tools/Validate-Unity.ps1 -Method ServerBuild.BuildAndroid`.
6. Em aparelhos reais, valide toque, recortes, paineis, pesca e desempenho sustentado. Use `-metrics` somente no contexto de teste.

As salas normais nao foram reiniciadas por esses testes. Para usar as mudancas no seu servidor usual, cliente e servidor precisam ser compilados a partir do mesmo estado de codigo. Os builds de validacao permanecem em pasta separada.


## Capturas verificadas

![Inventario a 800x360](Evidencias/inventory-800x360.png)

![Portal a 1280x720](Evidencias/portal-1280x720.png)
