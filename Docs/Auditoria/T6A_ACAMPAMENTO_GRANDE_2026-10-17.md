# T6a — envio e aplicação integral de acampamento grande

Concluída em 2026-10-17 no escopo abaixo. **37 verificações aprovadas** em uma conexão local com servidor e cliente NGO/UTP reais. T6 permanece em andamento; filas concorrentes, dados inválidos e recuperação ficam em rodadas próprias.

## Resultado

| Fluxo | Envelope UTF-8 | Partes de até 8.192 bytes | Resultado |
| --- | ---: | ---: | --- |
| Sincronização inicial pequena | 11.840 bytes | Controle abaixo dos antigos 32 KiB | Perfil igual ao autoritativo. |
| Sync de perfil com 500 entradas no chão | 186.316 bytes | 23 | Perfil completo, confirmação própria e conexão preservada. |
| Resposta após transferência de inventário | 186.352 bytes | 23 | Quatro madeiras movidas uma vez da mochila ao baú; revisão incrementada uma vez e uma ação registrada. |

Não foi necessária correção de produção nesta rodada. A ferramenta, as evidências e o roteiro foram acrescentados/atualizados. Foram preservadas as alterações locais existentes, inclusive a correção de leitura de metadados ausentes da T5b.

## O que foi verificado

- Autenticação assinada com chave efêmera; aprovação e callbacks reais de conexão de `NetworkBootstrap`.
- Envio de comandos pelo canal NGO real e respostas pelo remetente/receptor de `CampNetwork`, sem aplicar snapshots diretamente no teste.
- Comparação integral do perfil enviado com o autoritativo e do perfil recebido/aplicado com aquele enviado. A fixture contém madeira, rifles carregados e equipamentos raros com identidade e bônus; as 500 entradas de chão chegam completas.
- Cada envelope grande contém ao menos uma fronteira de parte que corta um caractere UTF-8. A fixture usa IDs de chão únicos com caracteres multibyte e um prefixo longo, permitido pelo validador atual, para provocar essa condição de forma intencional.
- Durante a montagem incompleta, foram observadas 1.518 amostras de atualização no total: o perfil e o envelope anteriores permaneceram intactos e o comando seguiu pendente. Essas amostras não são cenários independentes nem medidas de desempenho.
- O ACK correto só liberou cada comando após a aplicação completa. Mensagem, perfil, estado sincronizado e ACK chegaram juntos aos observadores: duas publicações, uma por resposta grande.
- A transferência de inventário seguinte confirmou que uma ação normal também recebe sua resposta grande e sai da pendência, sem duplicar a operação.

O estado grande foi preparado no repositório autoritativo de teste entre a sincronização inicial e o comando Sync. O cliente recebeu esse estado antes de enviar a operação de inventário. Foram usadas as versões atuais: Unity 6000.0.84f1, NGO 2.13.3, Transport 2.7.4 e protocolo 34.

## Execução e evidências

- Fonte: `Tools/Audit/PostAuditT6NetworkChecks.cs`, copiada somente ao `Assets/Editor` do projeto isolado.
- Projeto reutilizado: `Builds/PosAuditoria/T2Shared/20261017-123552-50c3a1`.
- Pasta: `Logs/pos-auditoria/T6a/2026-10-17-attempt1/`.
- Resultado: `result.json`, `run-summary.json`, `editor.log`, `editor-exit.txt`, `editor.pid`, `driver.cs`, hashes das 184 fontes e estados Git.
- Envelopes reais capturados na fila de produção: `large-envelope.json` e `action-envelope.json`, com hashes em `envelope-hashes.json`.
- Encerramento com código 0; `Success=true`; marcador `POST_AUDIT_T6A PASS checks=37`. Nenhum erro/aviso C# encontrado no log da execução.
- Reprodução: `-batchmode -nographics -executeMethod PostAuditT6NetworkChecks.Run -t6a-output <pasta nova>`, com `-projectPath` apontando ao projeto isolado. A ferramenta entra em Play, executa e sai do Editor; não usar `-quit` na entrada, pois o fluxo é assíncrono.

Os 184 scripts de execução do projeto isolado tinham hashes iguais aos do principal. A sessão, a chave e o armazenamento são exclusivos da fixture; nenhum save real foi aberto. Não houve build nem alteração de cena/prefab do projeto principal.

## Limites e continuação

Servidor e cliente são dois `NetworkManager` distintos no mesmo processo Editor, com conexão real por loopback. Isso não equivale a testar executáveis separados, Internet, Wi-Fi, perda de pacotes, Android ou interface visual. Não foram medidas latência, alocação, memória ou desempenho.

Esta rodada cobre o caminho normal, um envelope pequeno e dois envelopes grandes de aproximadamente 186 KB. Não cobre o limite de 8 MiB, excesso de tamanho, envelopes corrompidos, reordenação/repetição, substituição concorrente, timeout ou reconexão.

Próximas rodadas pequenas:

1. **T6b — fila e atualizações concorrentes:** preservar transferência atual e confirmação pendente ao substituir o próximo snapshot por atualizações de atividade.
2. **T6c — contrato e integridade:** partes/metadados inválidos, repetição, fronteiras de tamanho e digest divergente; dividir novamente se necessário.
3. **T6d — recuperação:** timeout, novo Sync e reconexão, sem repetir a operação econômica original.
