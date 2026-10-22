# C11 — Janela de saque autorizada

Implementada e revisada estaticamente em 17/10/2026. **Sem compilação, testes, Unity ou builds.** Conclui a implementação coordenada C10/C11; funcionamento e apresentação em execução continuam pendentes de T1/T7.

## Causa e correção

A janela anterior abria assim que o baú estava aberto, presumindo que a lista já estava replicada. A C10 removeu essa replicação pública: é preciso distinguir conteúdo aguardando autorização, conteúdo recebido e acesso perdido. A janela também mantinha referências de seleção/arraste e podia encerrar suas verificações cedo quando outro menu estava ativo.

- `Assets/Scripts/UI/InventoryWindow.cs`: abertura de saque confere o contexto local, pede uma cópia nova e mostra carregamento até receber os nove slots. Toque, arraste, equipamento, transferência e descarte ficam bloqueados enquanto o saque carrega. A limpeza e verificação de acesso ocorrem antes dos retornos por outros menus. Fechamento cancela a abertura pendente, remove a assinatura do recipiente e limpa arraste, cópias de apresentação e cache local do baú. Baú seguro continua restrito ao fluxo da base.
- A janela fecha ao perder recipiente, alcance, linha de interação, vida, partida em andamento ou elegibilidade de ação, e ao receber revogação depois de já ter recebido conteúdo. Espera inicial de conteúdo expira em dez segundos; espera por abertura pendente, em doze. A janela não reabre automaticamente depois de perda de acesso/hide: uma nova interação inicia uma solicitação nova.
- `Assets/Scripts/UI/UguiInventoryPresenter.cs`: distingue carregamento do saque, bloqueia ações antes do conteúdo e limpa item selecionado/arraste quando os dados ou contexto mudam. O caminho IMGUI segue o mesmo bloqueio e estado de carregamento. Grades auxiliares são limpas antes de copiar dados atuais, evitando reapresentar outro recipiente.
- `Assets/Scripts/Combat/LootContentsAccess.cs`: adiciona contexto local de apresentação, solicitação renovada e limpeza do cache. Cada solicitação recebe um GUID próprio, ecoado pelo servidor no RPC direcionado. O cliente ignora respostas de uma abertura anterior, inclusive depois de fechar/reabrir ou hide/show rápido. O token correlaciona a cópia; não substitui a autorização de saque no servidor. O servidor guarda somente o token atual de cada leitor e remove tokens revogados/desconectados.
- `Assets/Scripts/Combat/LootPickup.cs`: hide/despawn invalida o token local e a cópia; despawn do servidor limpa os tokens dos leitores preservando seu inventário autoritativo. `Assets/Scripts/Core/GameConfig.cs`: protocolo 32 → **33**, pela mudança de assinatura dos RPCs de conteúdo. Cliente e servidor devem usar a mesma revisão; executáveis não foram reconstruídos.

Mantidos layout, temas A/B, safe area, regras dos itens e autoridade do servidor. Host continua usando o inventário autoritativo, sem limpá-lo ao fechar a janela. Não houve alteração de save ou asset de jogo. O servidor pode manter a permissão de saque enquanto o jogador continua elegível mesmo com a janela fechada; o cliente normal invalida o token ao fechar e ignora respostas dessa visualização. Não é possível retirar de um cliente modificado o conhecimento de dados que recebeu legitimamente.

## Revisão e pendências

Revisão estática dos caminhos de abertura, fechamento, conteúdo inicial, evento de atualização, revogação, resposta tardia, descarte, arraste, host e hide/show. Aplicadas as skills de C# e UI/UX; consultada a direção de arte existente. Não há evidência de execução nem aceite visual nesta etapa.

Na T1/T7: conferir compilação/RPC e abrir baú com atraso de resposta; ações devem permanecer bloqueadas até os nove slots. Conferir fechar/reabrir o mesmo baú, alternar entre baús, receber resposta antiga, perder alcance/linha, morrer/extrair, esconder/reexibir, abrir outro menu durante carregamento, timeout e host. Conferir seleção/arraste durante atualização ou revogação, clientes simultâneos e transferência/reordenação/descarte legítimos. Conferir base, IMGUI e uGUI nos dois temas, reconexão e rejeição do protocolo antigo. Os testes continuam para a fase T, conforme solicitado.
