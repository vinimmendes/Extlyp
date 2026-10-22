# T4a — Recuperação de backup e versões

Data: 2026-10-17. **Concluída nos 15 cenários abaixo; T4 ainda parcial.** Próxima rodada: T4b, falhas de acesso, preservação e escrita.

## Execução

Uma execução aprovada na Unity 6000.0.84f1, Editor em batchmode, sem Play ou build. Resultado `Success: true`, código de saída 0 e marcador `POST_AUDIT_T4A_OK`. Não foi necessária alteração de código de produção.

A ferramenta criou seus próprios checkpoints com SharedCampMemory e JsonUtility nativos, usando perfis válidos com marcadores distintos: principal com três unidades de madeira e backup com sete. Cada cenário recebeu pasta e nome de memória compartilhada exclusivos. Foram usados o validador, mutex, arquivos e rotinas de preservação reais. Saves reais não foram abertos.

## Cenários aprovados

| Entrada | Comportamento observado |
| --- | --- |
| Principal válido e backup válido distinto | Usa o principal. |
| JSON principal quebrado | Recupera o backup válido. |
| Records nulo | Recupera o backup válido; não aceita progresso vazio. |
| Records ausente | Recupera o backup válido. |
| Registro sem Progress | Recupera o backup válido. |
| Sessão duplicada | Recupera o backup válido. |
| Quantidade de item negativa | Recusa o candidato na validação e recupera o backup válido. |
| Principal ausente e backup válido | Recupera o backup. |
| Ambos ausentes | Inicializa armazenamento novo, explicitamente vazio. |
| Ambos inválidos | Recusa antes de chamar a operação da transação, mantendo os arquivos originais. |
| Documento principal de versão futura | Recusa explicitamente, sem restaurar backup antigo. |
| Perfil principal de versão futura | Recusa explicitamente, sem restaurar backup antigo. |
| Principal quebrado e backup de versão futura | Recusa explicitamente, sem substituir os documentos. |
| Principal com Records explicitamente vazio | Aceita o principal vazio, sem ressuscitar registros do backup antigo. |
| Propriedade JSON duplicada | Recusa o principal e recupera o backup válido. |

## Preservação e repetição

Nas recuperações, os bytes do backup permaneceram intactos na primeira restauração, e a cópia recovered-backup foi conferida byte a byte. Quando havia principal inválido, sua cópia rejected também foi conferida byte a byte, incluindo o caminho derivado de hash.

Depois de cada carga bem-sucedida, a memória mapeada foi descartada e reaberta para carregar o checkpoint novamente do disco. O conteúdo esperado permaneceu e não foram multiplicados os arquivos de preservação.

Os casos de recusa foram executados duas vezes, com mapeamentos descartados entre tentativas. A operação econômica não foi chamada, os bytes de principal/backup ficaram iguais e os arquivos de preservação não se multiplicaram.

## Evidências e ferramenta

Pasta: `Logs/pos-auditoria/T4a/2026-10-17-attempt1/`. Contém `t4a-result.json`, `editor.log`, código de saída/PID, `run-summary.json`, `source-hashes.json`, `driver.cs`, sementes e arquivos de cada cenário. As 184 fontes C# de runtime do worker conferiram com o projeto principal.

Ferramenta adicionada: `Tools/Audit/PostAuditT4Checks.cs`, copiada somente para o Editor do projeto isolado. Comando explícito: `PostAuditT4Checks.Run`, com `-t4-output` apontando para pasta nova. Os avisos de recuperação de backup no log são esperados. A ferramenta não integra os assets/builds do jogo principal.

## Limites e próximas partes

Descartar e reabrir o mapeamento neste bloco não equivale a reiniciar o processo. Não foram simulados acesso negado, falha ao preservar arquivos, disco cheio ou falha durante promoção do checkpoint. Não se executaram migrações legadas com expedição ativa nem a campanha completa de validação de inventário da T5.

- **T4b:** falhas de acesso, preservação e escrita, com arquivos de teste intactos após a recusa.
- **T4c:** migração/recuperação de expedição e reinício real, em rodada separada.

T4 permanece parcial. Não iniciar T5 ou repetir T1–T3 nesta rodada.
