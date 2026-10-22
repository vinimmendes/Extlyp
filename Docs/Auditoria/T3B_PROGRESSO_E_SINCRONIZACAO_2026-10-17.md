# T3b — Falhas de progresso e sincronização após o banco

Data: 2026-10-17. **Concluída nos quatro casos abaixo; T3 ainda parcial.** Próxima: T3c.

## Resultado

92 registros de conferência aprovados, código de saída 0, `Success: true` e marcador `POST_AUDIT_T3B_OK`. Não houve alteração nas regras do jogo. Foi ampliada somente a ferramenta de teste em `Tools/Audit/PostAuditT3Checks.cs`.

Execução em Play Mode na Unity 6000.0.84f1, NGO 2.13.3 e Unity Transport 2.7.4, com host real, prefabs de teste válidos e armazenamento durável isolado. As 184 fontes C# de runtime da cópia conferiram com o projeto principal. Não houve build, carregamento do mapa, repetição da T3a ou da auditoria antiga.

## Casos e injeção

| Caso | Falha | Desfecho conferido |
| --- | --- | --- |
| 2 | Callback de atualização de XP confirmado lança uma exceção uma vez | Update retoma a publicação automaticamente. |
| 3 | Consulta do perfil durante ServerSyncCamp lança uma exceção | Update retoma a sincronização automaticamente. |
| 4 | Mesma falha de progresso do caso 2 | Despawn antes da tentativa seguinte, com publicação ainda pendente. |
| 5 | Mesma falha de sincronização do caso 3 | Despawn antes da tentativa seguinte, com publicação ainda pendente. |

Nos casos de progresso, a exceção foi lançada por um observador de ConfirmedExperience.OnValueChanged, dentro da atualização nativa do participante, depois da confirmação do banco. Nos casos de sincronização, o observador do estado Extracted retirou temporariamente a identificação de sessão do request da fixture: a consulta real do repositório lançou ArgumentNullException, capturada pelo ramo de sincronização. A identificação foi restaurada antes da recuperação ou da limpeza. Nenhum método de produção foi substituído nem recebeu hooks.

Cada falha deixou somente sua confirmação local pendente; a outra publicação concluiu. Os quatro avisos esperados ficaram registrados em `ExpectedWarnings`, sem erros inesperados.

## Garantias observadas

- Os três itens de madeira e 24 XP finais, incluindo bônus de extração, foram creditados uma vez por cenário.
- O marcador terminal bloqueou ações/coleta; novos objetivos, finalizadores da expedição e chamadas repetidas de banco não alteraram o JSON completo do perfil.
- Nos casos 2/3, a repetição automática concluiu as confirmações, atualizou os campos de XP do participante e o snapshot do acampamento do host, mantendo o perfil persistido idêntico.
- Dano letal não produziu drops ou recompensas repetidas. Um controle positivo confirmou que o caminho real de drops estava disponível.
- Nos casos 4/5, foi conferida a publicação pendente imediatamente antes do despawn nativo. A remoção não criou loot, e o callback de desconexão, chamado explicitamente, limpou o request sem desfazer a extração.
- Fechar e reabrir o repositório recuperou um perfil idêntico do disco nos quatro casos.

## Evidências e reprodução

Pasta: `Logs/pos-auditoria/T3b/2026-10-17-attempt1/`. Contém `t3b-result.json`, `editor.log`, `editor-exit.txt`, `editor.pid`, `run-summary.json`, `source-hashes.json`, `driver.cs` e checkpoints exclusivos em `store/`.

A ferramenta canônica continua em `Tools/Audit/PostAuditT3Checks.cs`, copiada apenas para o Editor do projeto isolado da T2c. O comando `PostAuditT3Checks.Run`, com `-t3b` e `-t3-output` apontando para uma pasta nova, seleciona somente os casos 2–5. Sem `-t3b`, permanece a seleção da T3a. A versão executada foi preservada no driver da evidência, assim como a versão anterior na evidência da T3a.

## Limites e próxima etapa

Esta rodada exercitou exceções locais na atualização do participante e na consulta do perfil para sincronização. Não simulou falha física de leitura do disco, disco cheio ou interrupção ambígua antes de Bank retornar sucesso. O snapshot conferido foi o do cliente local do host, não a entrega a um cliente remoto. Reabrir o repositório no mesmo processo não equivale a reconectar um cliente.

O callback de desconexão foi invocado diretamente após o despawn; não se repetiu a prova de transporte da T2c. Não houve verificação visual, Android, Sandbox ou exercício completo de tiro/movimento.

**Próxima: T3c — Sandbox, reconexão e bloqueios específicos de combate/movimento**, em rodada delimitada. T3 permanece parcial; T4 não foi iniciada.
