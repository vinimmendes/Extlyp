# T5a — estrutura e identidade dos perfis

Concluída em 2026-10-17 no escopo abaixo. T5 foi dividida para manter as rodadas pequenas: **T5b, quantidades e munição, continua pendente**.

## Resultado

**38 cenários e 149 verificações aprovados**, usando o código real no Editor Unity 6000.0.84f1, sem Play, build ou partida. Marcador `POST_AUDIT_T5A PASS cases=38 checks=149`, resultado `Success=true`, processo encerrado com código 0.

Nenhuma correção de produção foi necessária nesta rodada. Foram acrescentados o verificador e este registro, além da atualização do roteiro. Os 184 scripts de execução do projeto isolado têm hashes iguais aos do projeto principal na preparação da rodada.

## Cobertura executada

| Grupo | Cenários | Evidência |
| --- | ---: | --- |
| Estruturas e valores de identificação | 19 | Recusa de contêineres/slots nulos; grades com 8 ou 10 slots; equipamento com 7 ou 9 slots; slot ativo inválido; chão nulo, entrada nula ou ID vazio/duplicado; item inexistente; arma no slot de cabeça; arma sem identidade; reserva sem depósito; versão de perfil futura. |
| Identidade e posse | 11 | Seis pares entre baú, mochila, arma equipada e chão; duplicação entre participantes; duplicação entre posse ativa e depósito reservado; duplicação interna entre mochila/equipamento históricos; dois controles válidos. |
| JSON bruto | 7 | Campos Chest, Ground e Equipment ausentes; Pack nulo; grade curta; equipamento longo; stack nulo. Recusa antes do callback da transação, preservação do original e ausência de backup vazio. |
| Entrada real Save do repositório | 1 | Com posse válida da sessão, duas tentativas inválidas: grade curta e identidade repetida. Ambas preservam integralmente perfil e revisão anteriores. |

Nos casos transacionais recusados, foram comparados o checkpoint principal, o backup e o perfil compartilhado relido. Cada fixture recebeu dois checkpoints antes da tentativa para tornar a preservação do backup observável.

Os controles positivos confirmam que quatro armas com identidades distintas são aceitas e que o depósito de uma expedição **confirmada** pode manter historicamente a identidade de uma arma na mochila de outro participante. Já a reserva ainda não confirmada participa da verificação de posse ativa.

## Evidências e reprodução

- Fonte: `Tools/Audit/PostAuditT5Checks.cs`, copiada somente para `Assets/Editor` do projeto isolado.
- Projeto isolado reutilizado: `Builds/PosAuditoria/T2Shared/20261017-123552-50c3a1`.
- Execução aprovada: `Logs/pos-auditoria/T5a/2026-10-17-attempt3/`.
- Arquivos: `result.json`, `run-summary.json`, `editor.log`, `editor-exit.txt`, `editor.pid`, `driver.cs`, `source-hashes.json`, estados Git anterior/posterior e fixtures por cenário.
- Entrada: `-batchmode -nographics -quit -executeMethod PostAuditT5Checks.Run -t5-output <pasta nova>`, com `-projectPath` apontando ao projeto isolado.
- Nenhum erro/aviso C# encontrado no log da tentativa aprovada.

As tentativas anteriores foram preservadas. A primeira falhou na compilação do próprio verificador: o ID inexistente escolhido excedia o tipo numérico do enum; foi substituído por 255. A segunda revelou um erro da fixture: a serialização nativa mantinha um depósito vazio presente, portanto ela não representava depósito ausente. A versão final define `Escrow=null` explicitamente. Não houve alteração de produção para acomodar esses exemplos.

## Limites e próxima rodada

Esta execução usa serialização nativa, validação de domínio, armazenamento compartilhado/arquivos temporários e a entrada Save real; não equivale a validar uma partida, rede, interface ou aparelho. Só dados de teste nas pastas de evidência foram usados.

A matriz de identidade usa armas; não cobre todas as definições de armaduras/acessórios nem todas as combinações possíveis. Migração e reinício pertencem à T4c e não foram repetidos aqui. Quantidades, munição carregada, munição de recuperação, limites numéricos do JSON e divisão de pilhas ficam para **T5b**. A T5 inteira ainda não está concluída.
