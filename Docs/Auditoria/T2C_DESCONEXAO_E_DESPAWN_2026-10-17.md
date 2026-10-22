# T2c — Desconexão e despawn após perda de posse

Data: 2026-10-17. **Concluída nos cenários delimitados abaixo.** T2a, T2b e T2c encerram a T2; próxima rodada: T3.

## Resultado

83 registros de conferência aprovados em quatro cenários: 61 nos três primeiros e 22 no caso adicional de desconexão direta. As duas execuções aprovadas encerraram com código 0, `Success: true` e marcador `POST_AUDIT_T2C_OK`. Não foi necessária alteração do código de produção.

Verificação em Play Mode na Unity 6000.0.84f1, NGO 2.13.3 e Unity Transport 2.7.4, com servidor e cliente locais reais dentro do mesmo processo Editor. Foi reutilizado exclusivamente o projeto isolado `Builds/PosAuditoria/T2Shared/20261017-123552-50c3a1`, sem carregar o mapa completo ou criar um build. As 184 fontes C# de runtime dessa cópia conferiram com o projeto principal.

## Cenários executados

| Caso | Acionamento | Evidência |
| --- | --- | --- |
| 0 | Update do bootstrap detecta renovação recusada após troca de posse | TimeUp antes do despawn, desconexão real e limpeza; nova expedição preservada. |
| 1 | Outra thread mantém o mutex real do armazenamento ocupado | Os dois timeouts esperados, na renovação e na finalização, são tratados. A conexão termina e os registros locais são limpos mesmo sem concluir a transação de encerramento naquele instante. |
| 2 | Sync do CampNetwork encontra proprietário esperado antigo | O catch de CampLeaseLostException aciona a desconexão, bloqueia o participante e limpa seu estado. |
| 3 | DisconnectClient do próprio NGO, sem DisconnectCampSession | O participante ainda vivo entra no despawn com credencial local obsoleta; a exceção real de perda de posse é capturada antes dos drops, sem impedir a limpeza. |

Em cada caso houve login assinado com chave efêmera, execução dos callbacks reais de aprovação/conexão do bootstrap e um callback real de desconexão emitido pelo NGO. Depois da desconexão, duas invocações adicionais do callback verificaram sua tolerância a notificações repetidas/tardias.

## Limpeza e proteção conferidas

- Conexão encerrada no servidor e no cliente.
- PlayerInventory e Health removidos de suas listas estáticas; assinatura de morte e assinatura de status do jogo removidas.
- Requests, slots e prontidão removidos; atividade cancelada e removida; estado de snapshot de saída removido. Registros de orçamento de input e perfil sandbox estavam ausentes ao final.
- Nenhum loot adicional criado ao desconectar. Um controle positivo anterior criou loot pelo caminho real de ServerDropStack, confirmando que o caminho de drops estava disponível no cenário.
- O registro completo da nova expedição permaneceu igual durante a desconexão antiga. A posse nova continuou renovável, a expedição continuou confirmada e os callbacks repetidos não alteraram o registro.

No caso 1, os logs de erro de timeout são deliberados e constam em `ExpectedErrors`; não são falhas inesperadas do jogo. No caso 3, o aviso de participante removido sem drops confirma o ramo de proteção em OnNetworkDespawn.

## Como a fixture foi limitada

O bootstrap ficou desabilitado para não executar seu Start completo, menus ou inicialização de sala. Seus callbacks reais foram registrados no NetworkManager; Update foi chamado explicitamente para acionar o ramo de renovação sem esperar o temporizador de 20 segundos.

O login aprovado foi de acampamento. A fixture então preparou os registros da expedição, slots/prontidão e um participante com prefab válido, Health e PlayerInventory. Esse objeto foi realmente criado/destruído pelo NGO com propriedade do cliente e associado a ConnectedClients.PlayerObject, mas sem replicá-lo ao cliente. O teste não executou o fluxo completo de criação de um PlayerObject aprovado para um mapa.

O vencimento da posse foi provocado sob a transação real do armazenamento. A aquisição nova usou Claim/Begin/Commit reais. No caso 3, a fixture restaurou no cache local a credencial anterior para representar a sala antiga, mantendo a nova credencial no armazenamento; depois da limpeza, a credencial nova foi restaurada apenas para conferir sua validade. A troca entre processos separados já foi exercitada na T2b.

A chave foi criada apenas em memória, e o armazenamento usou cluster e pasta exclusivos da prova. Não se abriram saves ou identidades reais. A cena vazia e os três prefabs pertencem somente ao projeto isolado.

## Evidências

Execução dos casos 0–2: `Logs/pos-auditoria/T2Network/2026-10-17-attempt2/`.

Caso 3, executado separadamente sem repetir os anteriores: `Logs/pos-auditoria/T2Network/2026-10-17-raw-disconnect/`.

As duas pastas contêm resultado estruturado, log do Editor, código de saída e PID. A primeira também contém `source-hashes.json` e `run-summary.json`; ambas preservam a versão do verificador usada em `driver.cs`. Os resultados estruturados listam cada conferência realizada.

A tentativa inicial, em `2026-10-17-attempt1`, parou no preparo porque o NGO recusou objetos gerados em memória com GlobalObjectIdHash zero. A fixture passou a criar prefabs nativos válidos antes de entrar em Play; nenhuma regra do jogo foi enfraquecida para contornar a recusa. O erro e o resultado inicial foram preservados.

Ferramenta canônica: `Tools/Audit/PostAuditT2NetworkChecks.cs`, copiada somente para o Editor do projeto isolado. O comando explícito `PostAuditT2NetworkChecks.Run`, com `-t2c-output` apontando para uma pasta nova, executa os casos 0–2. A opção `-t2c-raw-disconnect` executa somente o caso 3. A ferramenta não faz parte dos assets ou builds do jogo principal.

## Limites

O resultado cobre os callbacks, os ramos de autorização e a limpeza descritos, não uma partida completa. Não valida aparência do cliente, inventário replicado, Android, autenticação de expedição com mapa completo, perda física de rede, carga ou estresse concorrente. O cliente e o servidor deste bloco rodaram no mesmo processo; não se confunde este resultado com a prova entre processos da T2b.

A falha injetada foi indisponibilidade por timeout do mutex, não disco cheio ou quebra de conexão durante escrita. A limpeza local foi comprovada mesmo com falha de finalização; isso não significa que uma lease possa ser liberada duravelmente enquanto o armazenamento está indisponível.

Próxima execução: **T3 — falha depois do banco de extração**, em rodada própria, sem repetir T1/T2 ou a auditoria antiga.
