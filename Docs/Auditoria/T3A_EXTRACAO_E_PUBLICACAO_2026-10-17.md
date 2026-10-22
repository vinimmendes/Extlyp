# T3a — Extração normal e falha na publicação de estado

Data: 2026-10-17. **T3a concluída; T3 ainda parcial.** Próxima rodada: T3b, falhas na atualização de progresso e sincronização após o banco.

## Resultado

44 registros de conferência aprovados em dois cenários: extração normal e falha controlada de publicação do estado. A execução aprovada terminou com código 0, `Success: true` e `POST_AUDIT_T3A_OK`. Nenhuma mudança no código de produção foi necessária.

Play Mode na Unity 6000.0.84f1, com host NGO real e armazenamento compartilhado/durável exclusivo do teste. Foi reutilizado o projeto isolado da T2c, sem carregar o mapa nem criar builds. As 184 fontes C# de runtime conferiram com os originais atuais.

## Preparo e falha injetada

A fixture usou os prefabs válidos da T2c e uma cena vazia. O bootstrap ficou desabilitado para evitar Start e inicialização completa da sala. Uma sessão exclusiva foi adquirida e associada ao cliente local do host. O participante foi criado por SpawnAsPlayerObject; ServerBeginExpedition e ServerCommitExpedition prepararam a expedição real.

Cada cenário retornou três unidades de madeira e 20 XP pendentes. A extração confirmou 24 XP, incluindo o bônus existente de 20%, e encerrou o estado ativo da expedição.

No cenário de falha, um observador de Status.OnValueChanged, executado na publicação de Extracted, restaurou Alive uma única vez e lançou `T3_STATUS_FAULT`. Isso forçou uma falha real no callback da publicação depois da confirmação do banco, mantendo temporariamente o status Alive. Nenhum método de produção foi substituído. O aviso esperado ficou registrado no resultado.

## Comportamentos conferidos

- Banco retorna sucesso e guarda exatamente os três itens e 24 XP esperados.
- Mesmo com Status ainda Alive, extractionBanked e progressionFinished já são verdadeiros; IsAlive e CanAct são falsos e ServerAddItem recusa coleta.
- Concessão tardia de XP por objetivo e chamadas de Lose/Rollback/Commit não alteram o perfil bancado.
- Três novas chamadas de ServerBankExtraction retornam sucesso terminal sem repetir efeitos em itens, XP ou revisão. A prova compara o JSON completo do perfil; não usa contador artificial dentro de Bank.
- Dano letal aplicado depois do banco não gera drops ou novas recompensas. Um controle positivo anterior criou loot pela rotina real de ServerDropStack, para confirmar que o caminho estava disponível.
- A rotina Update do inventário retoma a publicação automaticamente, sem forçar seu temporizador. Após a janela de aproximadamente dois segundos, o estado é Extracted e as três confirmações locais de publicação estão completas.
- A repetição da publicação não altera o perfil persistido; setters de Alive, Dead e TimeUp não reativam a extração terminal.
- Despawn nativo remove o participante sem criar loot. O callback Disconnected, chamado explicitamente depois, libera o request sem alterar o perfil bancado.
- Fechar e reabrir o repositório no mesmo processo recupera do disco um perfil idêntico ao que foi bancado.

## Evidências

Execução aprovada: `Logs/pos-auditoria/T3/2026-10-17-attempt2/`:

- `t3a-result.json`: conferências e aviso esperado.
- `editor.log`, `editor-exit.txt`, `editor.pid`: execução atual.
- `run-summary.json`, `source-hashes.json`, `driver.cs`: escopo e código usado.
- `store/`: checkpoints exclusivos da prova.

Ferramenta canônica: `Tools/Audit/PostAuditT3Checks.cs`, copiada somente para o Editor do projeto isolado. Comando explícito: `PostAuditT3Checks.Run`, com `-t3-output` apontando para pasta nova. Requer os prefabs de teste já preparados na T2c; não pertence aos assets/builds do jogo principal.

A tentativa inicial foi preservada em `2026-10-17-attempt1`. A conferência esperava incorretamente 20 XP finais e foi corrigida para 24 após verificar o checkpoint e a regra de bônus existente. O jogo não foi alterado para satisfazer o teste.

## Limites e próximas partes

Esta prova começa quando Bank retorna sucesso; não cobre falha ambígua da gravação antes desse retorno. O host tem cliente local, não um cliente separado. O callback de desconexão foi chamado diretamente; a desconexão real e o despawn de rede foram exercitados na T2c, mas não foram repetidos aqui como reconexão de um cliente após extração.

O teste cobre material e XP de exemplo, não todas as identidades/raridades de equipamento. CanAct e coleta foram conferidos dinamicamente; tiro e movimento não foram exercitados com seus componentes completos. A apresentação visual e a entrega da atualização a outro cliente não foram verificadas. O dano letal ocorreu antes da recuperação da publicação, e o despawn ocorreu depois dela.

- **T3b:** injetar separadamente falhas na atualização do progresso e na sincronização após o banco, incluindo remoção enquanto a publicação ainda está pendente.
- **T3c:** conferir Sandbox, reconexão com cliente e bloqueios específicos de combate/movimento, em cenário delimitado.

T3 permanece parcial até essas partes. Não iniciar T4 nem repetir a auditoria inteira nesta rodada.
