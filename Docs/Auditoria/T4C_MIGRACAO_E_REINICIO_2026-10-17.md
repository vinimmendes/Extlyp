# T4c — Migração e reinício real

Data: 2026-10-17. **Concluída. T4 encerrada nos cenários delimitados dos relatórios T4a/T4b/T4c.** Próxima rodada: T5, validação de perfil.

## Resultado

Cinco cenários aprovados em três processos Unity sequenciais e distintos: Prepare (5 conferências), Recover (42) e Restart (47), totalizando 94 registros. Todos terminaram com código 0, `Success: true` e seus marcadores `POST_AUDIT_T4C_<papel>_OK`. O manifesto final ficou em Verified. Não foi necessária alteração de código de produção.

Unity 6000.0.84f1, Editor em batchmode sem Play/build. A prova usou JsonUtility, validação/migração e SharedCampMemory nativos, com arquivos, mutex e memória mapeada reais. As 184 fontes C# de runtime do projeto isolado conferiram com as do projeto principal.

## Cenários aprovados

| Arquivo preparado | Resultado da recuperação |
| --- | --- |
| Documento v2, perfil schema 0 sem expedição | Migra para documento v6/schema 4. A pistola sem identidade recebe identidade e vai para o slot de arma; três unidades de madeira, sete munições de reserva e cinco balas carregadas permanecem. |
| Documento v2, perfil schema 0 com expedição reservada | Recupera a reserva antes de migrar. Quatro unidades de madeira e a arma/munição retornam; a identidade ausente da arma é criada uma única vez. |
| Documento v5, perfil schema 3 com expedição confirmada | Encerra a expedição como perda, sem devolver o escrow. Confirma 10 dos 20 XP pendentes, sem bônus de extração. |
| Documento v6/schema 4 com expedição reservada | Recupera oito unidades de madeira e arma/munição, preservando a identidade existente. Não cria arquivo de migração desnecessário. |
| Documento v5, perfil schema 3 com arma de recuperação | Preserva identidade, nove unidades de madeira, reserva e carregador. Migra a vinculação das cinco balas de recuperação. |

Todos os arquivos incluíam uma credencial transitória de sala antiga com prazo ainda válido. A carga a partir do disco limpou proprietário, token e prazo sem reutilizar essa autorização.

## Reinício e preservação

O primeiro processo somente serializou as fixtures representativas e seus originais, sem migrá-las. O segundo realizou a carga/migração e gravou o resultado. O terceiro começou após o encerramento do segundo e carregou os checkpoints novamente, com IDs de processo conferidos.

Na última carga, o JSON completo de cada CampProgress permaneceu idêntico ao produzido pela recuperação, incluindo revisão, identidades, itens e XP. Não houve nova concessão de XP, nova identidade ou avanço repetido da revisão.

Cada migração necessária preservou exatamente uma cópia pre-v6 com os bytes do arquivo de entrada. Os originais foram comparados byte a byte, e o reinício não acrescentou novas cópias de migração.

## Evidências e reprodução

Pasta: `Logs/pos-auditoria/T4c/2026-10-17-attempt1/`. Contém os resultados e logs de Prepare/Recover/Restart, códigos de saída, `manifest.json`, `run-summary.json`, `source-hashes.json`, `driver.cs` e as fixtures/checkpoints por cenário.

Ferramenta adicionada: `Tools/Audit/PostAuditT4RestartChecks.cs`, copiada somente para o Editor do projeto isolado da T2c. Executar `PostAuditT4RestartChecks.Run` em três processos sequenciais, com `-t4c-role Prepare`, depois Recover e Restart, usando o mesmo `-t4c-output` exclusivo. O comando exige batchmode e recusa Play. Nenhum save ou identidade real foi aberto.

## Limites

As fixtures foram construídas para representar os formatos e estados selecionados; não são um acervo de saves históricos de usuários. Não cobrem todas as combinações de schemas, itens e campos legados. O reinício foi após encerramento normal dos processos, sem simular queda de energia ou interrupção abrupta durante escrita.

As falhas de arquivo e políticas de versão futura continuam delimitadas pelos relatórios T4a/T4b; não se repetiram aqui. A prova não valida rede, interface, Android ou migração de cliente em uma partida completa.

Próxima: **T5 — validação de perfil**, em solicitação separada, sem repetir T1–T4 ou a auditoria antiga.
