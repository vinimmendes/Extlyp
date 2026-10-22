# T5b — quantidades e munição

Concluída em 2026-10-17 no escopo abaixo. **T5 encerrada com T5a e T5b**, respeitando os limites de seus relatórios. Próxima etapa: T6, sincronização de acampamento grande, em rodada separada.

## Resultado

**567 casos e 1.218 verificações aprovados** no Editor Unity 6000.0.84f1, em batchmode sem Play ou build. Marcador `POST_AUDIT_T5B PASS cases=567 checks=1218`, `Success=true` e encerramento com código 0. São casos de fronteira executados por código, não 567 partidas ou sessões de jogo.

Foi encontrada e corrigida uma falha real na leitura de metadados ausentes. Os limites explícitos de quantidades e munição passaram nos casos executados.

## Falha reproduzida e correção

Um perfil com arma na primeira posição da mochila, sem `Data` no JSON, recebeu **12 balas** em vez do padrão histórico zero. O diagnóstico nativo isolado confirmou:

- `DataPresentInRaw=false` e `RecoveryPresentInRaw=false`;
- `NativeLoadedRounds=12`, igual a `StarterLoadedRounds=12`;
- `NativeRecoveryRounds=0`.

`CampProgress.Pack` possui inicializador `InventoryGrid.Starter()`. Ao preencher o array, `JsonUtility` pode conservar o valor já inicializado de um campo ausente. A validação aceitava a ausência conforme o contrato histórico, mas desserializava novamente o texto original, permitindo herdar o carregador inicial.

Em `Assets/Scripts/Camp/SharedCampMemory.cs`, o documento validado agora recebe zero explícito somente quando `Data` ou `Instance.RecoveryRounds` estão ausentes; a desserialização usa esse documento. Nulos explícitos, frações e números fora da faixa continuam recusados. Não houve mudança de capacidade, catálogo, formato/versionamento de save ou política de backup. O texto original continua disponível para preservação de arquivos recusados e arquivos de migração.

Após a correção, os casos de ausência desses campos nos schemas 3 e 4 resultaram em zero para munição carregada e vinculada, preservando a identidade da arma. O diagnóstico anterior e a primeira tentativa falha foram mantidos como evidência.

## Cobertura

| Grupo | Casos | O que foi executado |
| --- | ---: | --- |
| Quantidades | 159 | Valores -1, 0, 1, limite e limite+1, removidas repetições quando o limite é 1. Representantes: madeira, moeda antiga, alicate, munição de pistola, pistola e capuz. Baú, mochila, chão, reserva e histórico confirmado; arma/capuz também nos equipamentos ativos e reservados/históricos. |
| Munição e contratos adjacentes | 368 | Pistola, Mosin, espingarda, espada, machado e lança. Munição carregada e vinculada em -1, 0, capacidade e capacidade+1, sem repetir extremos iguais. Inclui munição vinculada acima da carregada, vínculo em material, `Data` extremo em peixe e resíduos de arma em slot vazio. |
| Rejeição transacional | 16 | Quantidade 2 e carregador excedido em pistola, em oito posições/estados: baú, mochila, chão, reserva, histórico confirmado, equipamento ativo, equipamento reservado e equipamento histórico. |
| JSON nativo | 21 | `Quantity`, `Data` e `RecoveryRounds` com excesso superior/inferior de Int32, fração, null e string; ausência histórica/atual de metadados; quantidade negativa; carregador excedido; recuperação pelo backup por excesso de quantidade e de munição. |
| Lotes válidos | 3 | Madeira, moeda antiga e munição de pistola: lote de duas pilhas completas mais três unidades, distribuído em três slots, sem perder unidades. Perfil resultante aceito. |

Os casos de domínio conferem aceitação/recusa e que o validador não modifica o candidato. Nos 16 casos transacionais, principal e backup foram comparados ao conteúdo anterior, e o perfil compartilhado foi relido para conferir preservação integral, incluindo revisão.

Os JSONs numéricos inválidos sem backup foram recusados antes do callback, mantendo o original e sem criar backup vazio. Nos dois casos com backup válido, a recuperação preservou seus bytes e arquivou exatamente o principal inválido. Slots com quantidade zero e metadados residuais continuam permitidos conforme a regra da C7; `Data` de itens que não são armas não é tratado universalmente como munição.

## Evidências e reprodução

- Verificador: `Tools/Audit/PostAuditT5QuantityChecks.cs`, copiado somente para `Assets/Editor` do projeto isolado.
- Projeto isolado reutilizado: `Builds/PosAuditoria/T2Shared/20261017-123552-50c3a1`.
- Execução aprovada: `Logs/pos-auditoria/T5b/2026-10-17-attempt2/`.
- Diagnóstico da causa: `Logs/pos-auditoria/T5b/2026-10-17-diagnosis/native-missing-fields.json`, log e cópia da fonte anterior.
- Primeira execução: `Logs/pos-auditoria/T5b/2026-10-17-attempt1/`; parou no caso de metadados históricos ausentes, após as matrizes anteriores.
- Resultado aprovado: `result.json`, `run-summary.json`, `editor.log`, `editor-exit.txt`, `editor.pid`, `driver.cs`, `SharedCampMemory-after.cs`, hashes de fontes e fixtures.
- Entrada: `-batchmode -nographics -quit -executeMethod PostAuditT5QuantityChecks.Run -t5b-output <pasta nova>`, apontando `-projectPath` ao projeto isolado. `DiagnoseLegacy` reproduz somente o comportamento nativo sem a normalização.

Os 184 scripts de execução do projeto isolado tinham hashes iguais aos do principal após a correção. Não foram encontrados erros ou avisos de compilação C# no log aprovado. Nenhum save real foi aberto; os dados usados pertencem às fixtures nas pastas de evidência.

## Limites

Não houve partida, física, RPC, interface ou aparelho nesta rodada. A matriz cobre representantes das famílias de itens, e não cada definição/raridade de equipamento. Os testes de munição usam as capacidades atuais do catálogo; não avaliam balanceamento de combate. Migrações e reinícios completos da T4 não foram repetidos. T5a permanece documentada no relatório próprio, executada antes desta correção pontual de valores ausentes.
