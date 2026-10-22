# C9 — Recepção completa do acampamento

Implementada e revisada estaticamente em 17/10/2026. **Sem compilação, testes, Unity ou builds.** Referência: E2-04; conclui o contrato iniciado na C8. O par servidor/cliente está implementado, com funcionamento em execução ainda pendente de T1/T6.

## Alterações

- `Assets/Scripts/Camp/CampSnapshotTransfer.cs`: receptor do canal `ce-camp-snapshot-parts-v1`. Confere cabeçalho, versão, ID crescente, total até 8 MiB, número/índice/tamanho das partes, ordem, digest consistente e SHA-256 final. Mantém uma única montagem; partes de transferências antigas ou já concluídas não são reaplicadas. UTF-8 é decodificado estritamente depois da montagem completa. O tamanho disponível usa `reader.Length-reader.Position`: no NGO 2.13.3, o hash do nome já foi consumido quando o handler recebe o leitor.
- `Assets/Scripts/Camp/CampNetwork.cs`: registra exclusivamente o novo leitor de snapshot; aceita mensagens apenas do servidor e em conexão de acampamento. Desserializa e valida o perfil completo com as regras atuais de estrutura, quantidade e identidade, exigindo a versão atual. Recusa revisão menor que a do estado sincronizado. Não aplica dados parciais.
- `Assets/Scripts/Camp/CampProgress.cs`: `CampSession.ApplySnapshot` publica perfil, atividade, mensagem e ACK antes de avisar os observadores. Falhas de ouvintes da UI são registradas individualmente, sem invalidar a transferência aplicada. O caminho RPC de perfil usado pela expedição continua separado.
- `Assets/Scripts/Core/GameConfig.cs`: protocolo de conexão avançado de 30 para 31. A configuração de conexão e o manifesto de build já consomem essa constante. Não foram alterados engine, pacotes, assets ou versões de save.

## Confirmação e recuperação

Uma ação pendente somente é liberada pelo seu próprio ID de confirmação após aplicar o snapshot completo. Erros de comando devolvem o ID daquele comando, quando conhecido; erros espontâneos de atividade não confirmam outro comando por meio do antigo ACK genérico `error`.

A espera inicial e a espera por confirmação têm limite de 90 segundos. Uma montagem também expira após 15 segundos sem parte nova ou 90 segundos totais. Na primeira expiração, o cliente desabilita ações, descarta a montagem e solicita **somente Sync**, com novo ID; não repete compras, transferências, preparo ou qualquer operação original.

Ao processar Sync, o servidor reinicia a fila daquele cliente, conservando o contador crescente de transferências. Durante a recuperação, o receptor permite a substituição de uma montagem antiga por uma transferência nova iniciada no índice zero e ignora envelopes cujo ACK não seja o Sync solicitado. Partes já colocadas no transporte podem ainda chegar, mas não liberam ações durante a recuperação. O estado autoritativo recebido resolve a sincronização; o resultado textual específico da operação original pode não ser recuperado.

Se a nova tentativa também expirar, ou se forem detectados dados inválidos, o cliente limpa a pendência e encerra a conexão com orientação para reconectar. Falha de envio de comando também segue esse encerramento. Não há espera infinita nem repetição automática de operações de inventário. Registro, saída da conexão e desconexão local limpam montagem, ID pendente e envelope de atividade; o último perfil em memória permanece marcado como não sincronizado até nova recepção.

## Compatibilidade e limites

Cliente e servidor precisam usar a revisão com protocolo 31. A revisão intermediária da C8 já tem seu receptor correspondente; o antigo descarte silencioso de snapshots acima de 32 KiB foi removido desse caminho. Executáveis existentes não foram reconstruídos e continuam usando o protocolo com que foram produzidos.

O limite de 8 MiB e o ritmo de envio definidos na C8 foram mantidos. Dados inválidos ou acima do limite não são truncados. Este trabalho não mede latência, desempenho ou alocação; snapshots grandes podem atrasar a atualização visual da atividade. Confirmações concorrentes fora do contrato de um comando pendente continuam sujeitas à recusa explícita definida na C8.

## Revisão e validação pendente

Revisados estaticamente envio, recebimento, timeout, nova solicitação, limpeza da conexão, publicação para observadores e consumidores do protocolo. Conferidos `NamedMessage.Deserialize`, `CustomMessageManager.InvokeNamedMessage` e as assinaturas/semântica de `ReadBytesSafe` na cópia local do NGO 2.13.3. Nenhum log antigo foi tratado como validação deste código; nenhum save real foi aberto ou alterado.

Na T1/T6, conferir compilação e geração, perfis pequenos e acima de 32 KiB, UTF-8 nas fronteiras, limites de tamanho, cabeçalho/digest inválidos, partes repetidas/antigas/ausentes, aplicação única e ACK correspondente. Verificar atividade concorrente, desconexão/reconexão e limpeza do receptor; expirar a primeira tentativa, recuperar apenas por Sync e confirmar que a operação original não foi repetida; expirar a segunda tentativa e confirmar encerramento sem pendência. Conferir recebimento no cliente separado e no host, incluindo o offset do hash do NGO, ouvintes de UI com exceção, rejeição do protocolo antigo e conservação integral do chão. Esses cenários continuam pendentes para a fase T.
