# C8 — Envio de acampamentos grandes

Atualização: o receptor e a compatibilidade foram implementados na [C9](C9_RECEPCAO_DO_ACAMPAMENTO_2026-10-17.md). As pendências abaixo descrevem a revisão intermediária da C8; a validação em execução do par permanece pendente.

Implementação do lado servidor e revisão estática em 17/10/2026. **Sem compilação ou testes. Entrega coordenada com C9: não publicar esta revisão isoladamente.** Referência: E2-04.

## Causa observada

`CampNetwork.Sync` serializava todo o perfil dentro de um snapshot JSON e enviava uma única mensagem. O leitor do cliente descartava mensagens acima de 32768 bytes. O chão do acampamento pode crescer, portanto um perfil persistente aceito pode exceder esse limite; o cliente também pode ficar com uma ação pendente por não receber sua confirmação. A fragmentação do transporte não resolve o limite aplicado ao conteúdo remontado pelo leitor.

## Implementação

- `Assets/Scripts/Camp/CampSnapshotTransfer.cs` e seu `.meta`: contrato compartilhado de partes e fila do servidor. Cada transferência representa o snapshot completo, incluindo perfil, confirmação, mensagem e atividade; nenhum item do chão é truncado.
- `Assets/Scripts/Camp/CampNetwork.cs`: `Sync` coloca o snapshot na fila, `Tick` envia as partes e `Remove` limpa a fila ao desconectar. O registro de handlers limpa transferências antigas. Falhas ao preparar/enviar seguem o tratamento de desconexão já existente, sem editar o progresso persistido.
- Cada cliente mantém no máximo uma transferência atual e um snapshot seguinte. Atualizações de atividade substituem somente o seguinte, preservando uma confirmação ainda não enviada e sua mensagem. A transferência atual nunca é alterada no meio do envio. Confirmações distintas concorrentes no snapshot seguinte geram recusa/desconexão explícita, em vez de perder uma confirmação; o cliente normal já permite apenas um comando pendente além de Reel.
- Limite de quatro partes por cliente a cada 0,1 segundo, sem tentar compensar ticks atrasados. O envelope completo é limitado a 8 MiB UTF-8; excesso é recusado sem remover itens. Há no máximo dois payloads retidos por cliente, além das cópias temporárias de serialização. O limite é de segurança de rede, não uma autorização para reduzir saves.

## Contrato para C9

Canal `ce-camp-snapshot-parts-v1`, entrega NGO `ReliableFragmentedSequenced`. Payload máximo de cada parte: 8192 bytes. Cabeçalho fixo: 57 bytes; mensagem do contrato de no máximo 8249 bytes, antes do overhead do NGO/transporte.

Campos escritos nesta ordem por `FastBufferWriter`:

| Campo | Tipo/tamanho | Regra |
| --- | --- | --- |
| Versão | byte | 1 |
| Transferência | UInt64 | Crescente por cliente na instância da conexão; lacunas são permitidas por substituição do snapshot seguinte. |
| Bytes totais | Int32 | 1 até 8388608 |
| Índice | Int32 | Começa em zero |
| Número de partes | Int32 | Teto de bytes totais / 8192 |
| Bytes desta parte | Int32 | 8192, exceto a última; deve coincidir com o total e o índice |
| SHA-256 | 32 bytes brutos | Mesmo digest do payload completo em todas as partes |
| Conteúdo | Bytes brutos | Trecho do JSON completo codificado em UTF-8, sem prefixo adicional de array/string |

SHA-256 verifica integridade do conteúdo remontado. A autorização continua dependendo de receber exclusivamente do servidor. UTF-8 deve ser decodificado apenas após reunir os bytes, pois uma parte pode terminar no meio de um caractere. O ACK dentro do snapshot somente confirma a operação depois da aplicação completa no cliente.

**Pendência obrigatória C9:** registrar o novo canal, validar tamanho/versão/índice/contagem/ID/digest, montar com memória limitada, recusar estado parcial/antigo, aplicar perfil e ACK juntos, limpar montagem na reconexão e tratar timeout/repetição sem repetir operações de inventário. Ajustar a compatibilidade/protocolo da conexão na entrega conjunta. O servidor não envia mais o canal antigo de snapshot; o leitor antigo permanece temporariamente no código e não consome o canal novo. Portanto o acampamento ainda não sincroniza nesta revisão intermediária até C9; esta etapa não é uma entrega jogável isolada.

## Evidência e pendência de verificação

Revisados estaticamente os pontos de Sync, Tick, desconexão e registro. Conferidas assinaturas reais de `FastBufferWriter.WriteBytesSafe`, escrita de valores e validação de mensagens nomeadas no pacote local NGO 2.13.3. Unity 6000.0.84f1 e Transport 2.7.4 preservados. Não houve alterações de configuração de transporte, banco, save, assets de jogo ou conteúdo de inventário.

Na T6, após C9: perfis abaixo/acima dos antigos 32 KiB; conteúdo com caracteres multibyte nas fronteiras; transferência em várias partes com atividade concorrente e comando pendente; snapshot substituído; ID repetido/antigo e metadados inválidos; integridade divergente; desconexão/reconexão; timeout e nova solicitação de Sync; payload no limite e acima dele. Confirmar conteúdo integral e ausência de itens perdidos/ACK prematuro. Taxa, latência e uso de memória permanecem sem medição em execução. Nenhum teste, Unity, build, servidor ou cliente foi iniciado nesta etapa.
