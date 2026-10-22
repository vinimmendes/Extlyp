# T3c2 — Reconexão após extração

Data: 2026-10-17. **Concluída. T3 encerrada nos cenários delimitados pelos relatórios T3a, T3b, T3c1 e T3c2.** Próxima rodada: T4, recuperação de backup.

## Resultado

37 registros de conferência aprovados em uma conexão inicial e duas reconexões com a mesma identidade. Código de saída 0, `Success: true`, marcador `POST_AUDIT_T3C2_OK` e nenhum erro inesperado. Não foi necessária correção de código de produção.

Play Mode na Unity 6000.0.84f1, NGO 2.13.3 e Unity Transport 2.7.4. Servidor e cliente usaram NetworkManagers distintos e transporte loopback real no mesmo processo Editor. As 184 fontes C# de runtime do projeto isolado conferiram com as do projeto principal.

## Fluxo exercitado

O cliente entrou com uma chave efêmera em memória e login assinado. Os callbacks de aprovação/conexão e o CampNetwork reais executaram o fluxo de sincronização. O prefab do participante foi criado como PlayerObject no servidor e replicado ao cliente, permitindo executar o fluxo real de ServerBeginExpedition, Commit e ServerBankExtraction.

A amostra retornou três unidades de madeira, um equipamento raro e 20 XP pendentes. O banco confirmou os itens e 24 XP finais, incluindo o bônus existente de extração. Três chamadas adicionais de extração mantiveram o conteúdo econômico idêntico.

O servidor enviou o perfil salvo pelo CampNetwork; o cliente recebeu, remontou e validou o snapshot pela rotina de produção. A prova aguardou sincronização completa e confirmação, sem fabricar o perfil recebido ou substituir os handlers de rede.

Depois disso, o servidor desconectou o cliente pelo NGO. O participante foi removido, o request antigo foi limpo, nenhum drop adicional apareceu e o perfil salvo permaneceu intacto. O repositório foi fechado e reaberto a partir do disco antes da primeira reconexão.

Nas duas reconexões, um novo NetworkManager cliente entrou com a mesma chave e uma nova assinatura de login. Em ambas, o snapshot efetivamente recebido manteve:

- Três unidades de madeira e um equipamento raro.
- A identidade e os bônus originais do equipamento, assim como o restante do inventário/equipamento.
- 24 XP confirmados, uma única recompensa de extração e a referência da expedição concluída.
- Estado da expedição encerrado e ausência de comando pendente após a confirmação de sincronização.

A comparação incluiu o JSON completo de CampProgress, normalizando somente Revision para zero nas cópias comparadas, pois uma nova aquisição de sessão incrementa legitimamente essa revisão. A prova não alterou a revisão do perfil real.

## Evidências e ferramenta

Pasta: `Logs/pos-auditoria/T3c2/2026-10-17-attempt1/`. Contém `t3c2-result.json`, `editor.log`, código de saída/PID, `run-summary.json`, `source-hashes.json`, `driver.cs` e checkpoints exclusivos em `store/`.

Ferramenta adicionada: `Tools/Audit/PostAuditT3ReconnectChecks.cs`, copiada somente para o Editor do projeto isolado da T2c. O comando explícito `PostAuditT3ReconnectChecks.Run`, com `-t3c2-output` apontando para pasta nova, seleciona apenas esta prova. Não faz parte dos assets/builds do jogo principal. Saves e chaves reais não foram abertos.

## Limites

O bootstrap ficou desabilitado para evitar seu Start completo; seus callbacks e serviços de rede foram registrados explicitamente. O login foi de acampamento e o participante de extração foi preparado pela fixture, sem percorrer menus ou carregar o mapa inteiro. A reconexão retornou ao acampamento, sem iniciar outra expedição.

Servidor e cliente eram instâncias distintas no mesmo processo, não dois executáveis/aparelhos. Isso não valida isolamento de todas as variáveis estáticas entre processos; a posse do armazenamento entre processos foi conferida na T2b. Não houve verificação visual, Android, perda física de rede, estresse ou todas as variedades de loot. Não se investigou falha ambígua antes de Bank retornar sucesso.

Próxima: **T4 — recuperação de backup**, em solicitação separada. Não repetir a campanha de T1–T3 ou a auditoria antiga.
