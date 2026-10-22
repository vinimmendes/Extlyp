# C19 — Animação de teia somente após emissão

Implementada e revisada estaticamente em 17/10/2026. **Sem compilação, testes, Unity ou builds.** Referência E3-06; verificação em execução pendente da T13. Esta é a última etapa da fase C; a fase T ainda não foi iniciada.

## Causa e correção

`WeaverVisual` iniciava os nove frames de liberação sempre que `AbilityState` saía da preparação. Cancelar a preparação por retorno ao território também fazia essa transição, sem gerar projétil. Apenas comparar o prazo estimado seria insuficiente para confirmar uma emissão real, incluindo recusas pela capacidade de efeitos.

- `Assets/Scripts/Enemies/MonsterWorld.cs`: `Add` informa sucesso somente depois de inserir o efeito autoritativo. Continua recusando emissões sem servidor/partida em andamento ou com a lista de efeitos cheia. Chamadores que não precisam do resultado continuam ignorando-o, preservando seu comportamento.
- `Assets/Scripts/Enemies/MonsterArchetypes.cs`: novo `WebReleasedAt` replicado, inicialmente -1, é atualizado apenas quando a emissão WebShot da aranha foi aceita. Cancelamentos, armadilhas colocadas no chão e ataques de outros tipos não geram esse sinal.
- `Assets/Scripts/Enemies/WeaverVisual.cs`: sai a dedução por `wasPreparing`; os frames 19–27 dependem de um instante novo de emissão real. O tempo já decorrido no relógio do servidor reduz a duração restante da animação, sem reiniciar nove frames ao receber uma confirmação antiga. Entrada/reexibição da apresentação toma o valor atual como referência e não repete disparos históricos. Morte, partida inativa e saltos de posição descartam a apresentação pendente. Caminhada continua com sua prioridade anterior e cancela a liberação visual.
- `Assets/Scripts/Core/GameConfig.cs`: protocolo de conexão 33 → **34**, pela adição do campo replicado no monstro. Cliente e servidor precisam da mesma revisão; nenhum executável foi reconstruído.

Mantidos atlas, frames de preparação, duração de liberação de 9/24 segundo, dano, alvo, quantidade de teias, cooldown, comportamento de ácido e regras da aranha. Não foram refeitos assets ou adicionados efeitos. A apresentação pode omitir uma liberação já antiga ou interrompida por caminhada; o objetivo é não apresentar uma emissão que não ocorreu.

## Revisão e pendências

Conferidos estaticamente preparação/cancelamento, ponto de criação de WebShot, limite de MonsterEffects, consumidores de MonsterWorld.Add, relógio de servidor e ciclo de ativação visual. Não foi executado teste nem produzida captura. Não há aceite em jogo ou garantia de comportamento visual sob latência nesta etapa.

Na T13, iniciar preparação e cancelar por retorno ao território enquanto parada: não deve mostrar liberação nem criar WebShot. Repetir com movimento, morte, fim de partida e capacidade de efeitos cheia. Conferir lançamento normal e confirmação atrasada, caminhada durante liberação, hide/show e entrada tardia sem repetição histórica. Verificar cliente separado e host, atlas/caminhada preservados e recusa do protocolo antigo. Compilação/serialização do campo novo fica na T1 antes desse teste.
