# C10 — Conteúdo de baús autorizado no servidor

Atualização: o fluxo de janela e a correlação de respostas foram implementados na [C11](C11_JANELA_DE_SAQUE_2026-10-17.md). O protocolo atual do par passou para 33. As pendências de C11 abaixo descrevem a revisão intermediária; a validação em execução permanece pendente.

Implementada e revisada estaticamente em 17/10/2026. **Sem compilação ou testes.** Referência E2-06. C11 continua necessária para completar o comportamento da janela de saque; não publicar o par pela metade.

## Causa identificada

`LootPickup.Contents` era uma `NetworkList<ItemStack>` com leitura padrão para todos os observadores e preenchida ao nascer. As operações de saque verificavam abertura, distância e estado do jogador, mas essas regras não protegiam a leitura dos itens. Um cliente observador podia receber conteúdo de baú fechado sem iniciar o saque.

## Alterações

- `Assets/Scripts/Combat/LootContents.cs` e `.meta`: armazenamento local de slots, sem herança de NetworkVariable/NetworkList e sem replicação no spawn. No servidor contém o inventário autoritativo; no cliente contém apenas a cópia enviada após autorização. Preserva acesso por índice, Count e o evento de atualização consumido pela interface existente. Escrita é interna ao assembly; não há operação remota que aceite uma lista arbitrária do cliente.
- `Assets/Scripts/Combat/LootContentsAccess.cs` e `.meta`: autorização e envio por ClientRpc direcionado a um cliente. Exige recipiente aberto (`OpenState==2`), destrancado, objeto observado e as regras existentes de `Sender`: jogador vivo, partida em andamento, sem ação de mecanismo/melee conflitante, até três unidades e linha de interação livre. Observar o baú sozinho não autoriza ler seus itens.
- O acesso é reavaliado a cada 0,2 segundo para recipientes abertos ou com leitores anteriores. Novos leitores recebem os nove slots completos; leitores autorizados recebem atualizações quando o conteúdo muda. Perda de autorização envia uma cópia vazia, quando o objeto ainda está visível; hide/despawn limpa a cópia local. Recipientes fechados sem leitores não percorrem os jogadores.
- Pedido de conteúdo no spawn/reexibição passa pelas mesmas regras. Repetições de um leitor conhecido aguardam o próximo ciclo de atualização; não ignoram autorização. Um novo pedido permite repor a cópia após hide/show mesmo quando a transição ocorreu entre dois ciclos do servidor.
- `Assets/Scripts/Combat/LootPickup.cs`: inicialização continua sorteando o mesmo loot no servidor, mas na coleção local. `TransferRpc`, `ReorderRpc` e `DropContentsRpc` passam pela mesma autorização do envio. Receber uma cópia não concede permissão permanente para modificar o baú.
- `Assets/Scripts/Core/GameConfig.cs`: protocolo de conexão 31 → 32, pois o formato replicado do NetworkBehaviour mudou. Configuração e manifesto de build já usam essa constante. Não foram gerados executáveis; cliente e servidor precisam da mesma revisão/protocolo.

## Preservação e limites

Mantidos sorteio, inventário de nove slots, restrições de itens de recuperação, abertura/fechadura e informações públicas de raridade/aparência. As alterações do mecanismo de recuperação de ferramentas em `DungeonExpedition` continuam acessando o inventário real no servidor e marcam a cópia para atualização.

O servidor/host é confiável e já possui o estado autoritativo. Ocultar dados do processo que hospeda o servidor não faz parte da proteção contra clientes remotos. Uma cópia já recebida legitimamente não pode ser apagada da memória de um cliente modificado; a revogação interrompe os próximos envios/ações e limpa a apresentação do cliente normal. Pode haver até um ciclo de 0,2 segundo, além da entrega de rede, para a limpeza visual; os RPCs de alteração sempre reavaliam a autorização imediatamente.

Nenhum save, conteúdo de item ou asset de jogo foi modificado. Não há truncamento de loot nem envio global do conteúdo de baús abertos. A elegibilidade de saque autoriza o envio mesmo sem a janela aberta; fechar a janela não equivale a perder a permissão de saque dentro da distância/linha permitidas.

## C11 e verificação pendente

A interface existente continua recebendo eventos compatíveis, mas C11 deve usar `HasContents` e `RequestContents` para coordenar carregamento, abertura/reabertura, fechamento ao perder acesso e descarte de seleção/arraste. A janela ainda pode apresentar uma grade temporariamente vazia entre abertura e entrega; não considerar o fluxo de saque aceito visualmente nesta etapa.

Revisados estaticamente produtores/consumidores de Contents, recuperação de ferramenta, visibilidade, RPC direcionado já usado em fechaduras e o evento de lista do NGO local 2.13.3. Não foram executados testes, Unity, builds, servidor ou cliente. Não há promessa de ganho de desempenho.

Na T1/T7, verificar compilação/geração RPC e cliente remoto observando baú fechado sem receber IDs, quantidades ou bônus. Abrir legitimamente e conferir conteúdo integral; outro observador distante/atrás de parede não recebe a lista. Conferir dois saqueadores autorizados e atualização de transferência/reordenação/descarte, morte/extração/linha bloqueada, perda de alcance, revogação, entrada tardia e hide/show rápido. Conferir host, recuperação da manivela/chave em recipiente, conteúdo fechado após reconexão e recusa do protocolo antigo. Repetir o fluxo de janela após C11. Evidência de execução permanece pendente.
