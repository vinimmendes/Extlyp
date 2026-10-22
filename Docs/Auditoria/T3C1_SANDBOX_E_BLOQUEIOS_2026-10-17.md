# T3c1 — Sandbox e bloqueios após extração

Data: 2026-10-17. **Concluída no escopo abaixo; T3 ainda parcial.** Próxima rodada exclusiva: T3c2, reconexão de cliente.

## Resultado

40 registros de conferência aprovados em dois casos: extração normal e Sandbox. Código de saída 0, `Success: true`, marcador `POST_AUDIT_T3C1_OK` e nenhum erro inesperado na rotina. Não foi necessária alteração de código de produção.

Play Mode na Unity 6000.0.84f1, NGO 2.13.3 e Transport 2.7.4, em host nativo no projeto isolado da T2c. As 184 fontes C# de runtime conferiram com as do projeto principal. Não houve build, mapa completo ou repetição dos casos T3a/T3b.

## Conferências

Cada participante recebeu PlayerShooter, PlayerController, Rigidbody2D e NetworkTransform antes do spawn. A fixture manteve o Update automático de tiro/movimento desabilitado e chamou os caminhos reais explicitamente, para controlar a entrada e a ordem do teste.

Antes de extrair, os controles positivos confirmaram velocidade e corrida ativas, tiro consumindo uma munição e incrementando ShotSequence, e recarga iniciada com reserva disponível. A recarga foi cancelada antes da extração.

Depois da confirmação:

- Tiro e recarga foram recusados, com arma, munição de reserva e espaço no carregador disponíveis. O cooldown de tiro foi removido na fixture antes da tentativa negativa, para não confundi-lo com o bloqueio terminal.
- Nova entrada de movimento/corrida, encaminhada pelo caminho do host em FixedUpdate, resultou em velocidade zero e Sprinting falso; CanSprint e gasto de stamina também foram recusados.
- Coleta foi recusada. Três chamadas repetidas de extração mantiveram o perfil idêntico.
- Dano letal, despawn nativo e o callback de desconexão não criaram drops nem repetiram recompensas. Um controle positivo anterior confirmou o caminho real de criação de loot.
- Reabrir o repositório recuperou o mesmo perfil do disco.

Na extração normal, os três itens de madeira e 24 XP foram persistidos. No Sandbox, ServerBeginExpedition não associou progressionSession ao perfil, e o perfil persistente de teste permaneceu idêntico ao obtido antes da expedição, inclusive após extração, repetição, dano e remoção.

## Evidências e ferramenta

Pasta: `Logs/pos-auditoria/T3c1/2026-10-17-attempt1/`. Contém `t3c1-result.json`, `editor.log`, código de saída/PID, `run-summary.json`, hashes das fontes, `driver.cs` e checkpoints exclusivos.

Foi ampliada somente `Tools/Audit/PostAuditT3Checks.cs`. Copiada para o Editor do projeto isolado, a opção `-t3c1` seleciona apenas os casos 6–7 de `PostAuditT3Checks.Run`; `-t3-output` deve apontar para uma pasta nova. O shader VisionSurface e seu meta foram copiados para satisfazer a dependência de inicialização de PlayerVision no worker, sem validar renderização. Assets do jogo principal não foram alterados.

## Limites e próxima etapa

A prova verifica os ramos de servidor e a velocidade definida no Rigidbody2D sob entrada controlada; não mede deslocamento por vários frames, colisões do mapa, conforto dos controles ou desempenho. Não exercita melee, todos os tipos de arma ou Android. A execução foi headless e não valida arte, som ou visão.

O cliente deste bloco é o cliente local do host. O callback de desconexão foi invocado diretamente após despawn; reabrir o repositório não equivale à reconexão de um cliente. A desconexão real continua documentada na T2c.

**T3c2 — reconectar cliente após a extração e conferir itens/XP carregados**, em solicitação separada. T3 permanece parcial até essa verificação; T4 não foi iniciada.
