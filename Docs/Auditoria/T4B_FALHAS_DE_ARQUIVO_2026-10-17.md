# T4b — Falhas de acesso, preservação e escrita

Data: 2026-10-17. **Concluída nos seis cenários delimitados abaixo; T4 ainda parcial.** Próxima rodada: T4c, migração e reinício real.

## Resultado

Uma execução aprovada na Unity 6000.0.84f1, Editor em batchmode, sem Play, build ou repetição da T4a. Resultado `Success: true`, código de saída 0 e marcador `POST_AUDIT_T4B_OK`. Foram registradas seis exceções esperadas de sistema de arquivos, diretamente ou como causa interna da recusa do repositório. Não foi necessário corrigir código de produção.

Os checkpoints foram criados pela rotina nativa de SharedCampMemory/JsonUtility, em pastas e nomes exclusivos do teste. As 184 fontes C# de runtime do worker conferiram com o projeto principal. Saves reais e permissões do Windows não foram alterados.

## Cenários aprovados

| Cenário | Obstáculo real | Resultado |
| --- | --- | --- |
| Leitura do principal bloqueada | FileStream exclusivo | Recusa antes do callback econômico; principal/backup intactos; carrega após liberar o bloqueio. |
| Principal ausente e leitura do backup bloqueada | FileStream exclusivo no backup | Recusa sem criar principal vazio; restaura o backup após liberar o bloqueio. |
| Promoção do arquivo de preservação bloqueada | Diretório no caminho destinado à cópia preservada | Interrompe antes do callback; limpa temporário e mantém os documentos originais; recupera após mover o obstáculo. |
| Colisão de conteúdo no arquivo de preservação | Arquivo com nome de hash esperado, mas conteúdo diferente | Recusa sem sobrescrever a colisão ou os documentos; após mover o obstáculo, preserva os bytes corretos e recupera. |
| Substituição do checkpoint bloqueada | Principal aberto sem compartilhamento de exclusão | Callback chega a modificar a cópia da transação, mas File.Replace falha. Disco e estado compartilhado continuam anteriores; uma nova tentativa grava a alteração uma vez. |
| Substituição do principal durante recuperação bloqueada | Principal inválido aberto para leitura, permitindo decodificar/preservar mas impedindo substituição | Backup é decodificado e ambos os documentos são preservados; a falha não publica um checkpoint parcial/vazio. A tentativa posterior restaura o backup completo. |

Os arquivos anteriores foram comparados byte a byte. Nenhum temporário de gravação ficou pendente após as falhas. Para verificar a publicação na memória, foram conferidos os cabeçalhos nativos do mapeamento e, no caso de alteração recusada, o conteúdo previamente confirmado, com três unidades de madeira. A tentativa posterior passou a sete, e esse valor permaneceu após descartar/reabrir o mapeamento.

Os obstáculos de preservação criados pela fixture foram movidos para caminhos de evidência, sem apagar seu conteúdo; origem e destino foram conferidos dentro da pasta exclusiva de saída antes da movimentação.

## Evidências e reprodução

Pasta: `Logs/pos-auditoria/T4b/2026-10-17-attempt1/`. Contém `t4b-result.json`, as exceções esperadas, `editor.log`, código de saída/PID, `run-summary.json`, `source-hashes.json`, `driver.cs`, sementes e arquivos de cada cenário.

Foi ampliada somente `Tools/Audit/PostAuditT4Checks.cs`, copiada para o Editor do projeto isolado. O comando explícito `PostAuditT4Checks.Run`, com `-t4b` e `-t4-output` apontando para pasta nova, executa apenas os seis casos desta etapa. A ferramenta não integra assets/builds do jogo principal.

## Limites e próxima etapa

As recusas de acesso foram produzidas por compartilhamento de arquivos, não por ACLs de usuário. Não se simulou disco cheio, falha de hardware, queda de energia ou interrupção abrupta de processo no meio da escrita. O bloqueio de gravação exercitou a promoção/substituição do checkpoint, não todas as falhas possíveis na criação ou no flush do temporário.

Descartar e reabrir o mapeamento no mesmo processo não equivale a reinício real. Migração legada e recuperação de expedição não foram executadas neste bloco.

**Próxima: T4c — migração e reinício real**, em rodada própria. T4 permanece parcial; T5 não foi iniciada.
