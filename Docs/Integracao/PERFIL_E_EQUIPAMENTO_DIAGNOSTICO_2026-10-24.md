# Criaturas, contador e duplo clique — 24/10/2026

## Lista de criaturas

A seção Criaturas enumerava todas as espécies e imprimia zero quando não havia registro. Agora omite linhas sem abates, assistências e mortes sofridas. Uma espécie com apenas assistência ou morte continua visível. Sem nenhuma atividade, apresenta uma mensagem explicando que estatísticas vêm de expedições normais e não do treino/teste. Os totais e os dados salvos não são apagados; nenhuma migração ou mudança de protocolo foi introduzida.

## Contador

Rastreado o caminho `Health.ServerTakeDamage` → `RecordProfileDeath` → `QueueProfile` → `ApplyPendingProfile` → `PlayerStatistics` → apresentação do perfil. As espécies de monstros e plantas correspondem aos enums de estatísticas; Esporífero possui identificação própria. A origem do ataque usa sessão e expedição elegíveis; sandbox/laboratório não geram essa progressão oficial. O usuário não tinha certeza de qual ambiente havia observado.

Não foi identificada uma falha de incremento na regra examinada. A prova `Tools/Audit/Run-ProfileCounterProbe.ps1` compila os fontes reais `PlayerProfile.cs` e `ProfileStatistics.cs`, com fixtures para repositório e consulta de arma. Passaram 17 verificações: espécies zeradas/ativas, abate oficial nos três agregados, replay, humanos/bots, assistência única, causa de morte, 14 espécies, validação dos dados, encerramento, expedição divergente, lease vencido/ausente, ausência de expedição oficial e métricas sem abates inventados.

Essa prova não valida física, autoria real do ataque na partida, NGO, credenciais reais, serialização nativa da Unity, disco ou sincronização até o cliente. Não permite afirmar que o contador foi validado de ponta a ponta. Históricos anteriores ao início da coleta não são reconstruídos.

## Equipar com dois cliques

O gesto existente exige o mesmo slot/item, até 350 ms entre toques/cliques e deslocamento de até 32 vezes a escala da interface. O reconhecimento envia o pedido imediatamente; não há coroutine esperando os 350 ms se esgotarem.

Na expedição, `PlayerEquipment.ExchangeEquipmentServerRpc` agenda explicitamente a conclusão para dois segundos depois. Movimento acima de 0,05 unidade, dano e outras condições incompatíveis interrompem a ação. Essa regra documentada foi preservada. Antes, o início da troca disparava `Changed`, apagando a mensagem de estado; agora o detalhe apresenta contagem regressiva baseada no horário replicado do servidor e informa a necessidade de ficar parado. A atualização ocorre por décimos enquanto a troca está ativa.

Na base, o caminho é `CampSession.Act` → mensagem ao servidor → transação → snapshot completo → aplicação/ack no cliente. Não há o temporizador de dois segundos. O snapshot tem envio parcelado a cada 100 ms, até quatro partes de 8192 bytes por rodada; o custo depende do tamanho e da fila. Isso é uma possibilidade a medir, não uma causa comprovada do atraso observado. O usuário confirmou demora nos dois contextos.

Foram adicionadas medições nos builds de desenvolvimento/Editor, sem botões novos:

- `[Equipamento] Segundo clique/toque fora da janela`: intervalo e deslocamento quando o gesto não é reconhecido.
- `[Equipamento] Duplo clique/toque reconhecido`: instante de envio pelo inventário.
- `[Equipamento] Servidor`: tempo de processamento do comando e preparação da resposta na base, com aceitação/recusa.
- `[Equipamento] Base: confirmacao aplicada`: tempo desde o pedido até aplicar o snapshot; inclui transporte, fila, validação e aplicação do perfil, sem incluir a renderização seguinte.

Os dois últimos registros compartilham o ID do pedido para correlação, sem registrar nome, sessão, identidade ou inventário. Os logs devem ser capturados no cliente e no servidor de desenvolvimento com estes fontes compilados. A demora da base ainda não foi reproduzida nem considerada resolvida; não foi introduzida previsão visual de inventário que pudesse ocultar recusa do servidor.

## Verificação

Prova focada: 17 verificações aprovadas. Compilação C# dos scripts de runtime aprovada com o compilador e referências locais da Unity 6000.0.84f1. A compilação isolada não faz importação nativa nem processamento de RPCs e não equivale a build do jogo. Nenhuma sessão Unity, cliente conectado ou build de servidor/APK foi iniciado nesta investigação; os servidores existentes foram preservados.
