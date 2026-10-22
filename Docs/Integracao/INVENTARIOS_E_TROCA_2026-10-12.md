# Inventários e bloqueio de tiro — 12/10/2026

## Troca de arma

PlayerInventory replica WeaponReadyAt, calculado pelo servidor conforme a transição já existente: 32 frames de pistola para arma longa, 22 de desarmado para arma longa e 36 entre armas longas ou de arma longa para pistola, a 24 fps. Equipagem inicial não inicia uma troca. PlayerShooter.CanAct rejeita tiro/recarga enquanto esse prazo não terminar, para jogadores e bots, sem gastar munição ou incrementar ShotSequence. O visual usa o mesmo relógio do servidor e o comprimento real do atlas, evitando liberar disparo enquanto ainda desenha a troca. Frame inicial limitado para tolerar pequenas diferenças do relógio replicado.

Reordenar um slot equipado preserva sua identidade antes de sincronizar a mochila; não inicia trocas intermediárias falsas ao validar os slots.

## Saque

InventoryWindow tinha `two=safeChest && camp` e começava no grid 1 quando havia recipiente, escondendo a mochila. Agora a janela do recipiente mostra mochila (0) e saque (1), lado a lado, com cabeçalhos próprios. Reutiliza RequestTransfer, RequestMove, RequestReorder e suas validações no servidor. Baú seguro continua exclusivo da base.

## Gazua

ItemCatalog.Limit permite pilhas de até 20. BeginLock aceita quantidade positiva; quebrar uma diminui a pilha em uma unidade e deixa as restantes. Data continua registrando o desgaste da gazua em uso na pilha e volta a zero quando ela quebra. Mistura de pilhas com Data/Quality diferentes continua impedida por ItemStack.Same, preservando o desgaste anterior. Não foi alterada a dificuldade do minigame.

## Fogão e pesca — somente inventário

Fogão mostra os nove slots reais de CampSession.Progress.Pack, com ícone/quantidade e índice original. Somente peixe/peixe cru/peixe cozido são selecionáveis; envio pendente ou perfil não sincronizado desabilitam a seleção. Peixe cru inicia cozimento, cozido mantém a ação de comer. Minigames e layout de pesca/cozimento não foram alterados. A política anterior de armazenamento do resultado do fogão continua vigente.

Pesca já concluía a captura em CampProgress.Pack, sem inventário separado nem fallback ao baú. Faltava verificar capacidade antes de iniciar. O servidor agora sorteia a captura e verifica se uma unidade cabe na mochila antes de ativar a atividade; caso contrário informa mochila cheia. Pilhas compatíveis ainda podem receber captura mesmo com todos os slots ocupados. CompleteFishing revalida a capacidade no término; nunca envia resultado a outro inventário.

## Verificação

Run-CookingInventoryProbe executado fora da Unity, usando as regras C# reais de ItemCatalog, InventoryGrid, CampActivity, CampModels e WeaponSwitchTiming. Todos os casos passaram, incluindo pilha de 20 gazuas, durações de troca, rejeição de pesca com mochila cheia apesar de baú vazio e armazenamento exclusivamente na mochila. Casos existentes de armazenamento/cozimento também passaram. Evidência: Logs/cooking-inventory-audit/after.txt.

Essa prova não valida RPCs, renderização/arraste, relógio entre aparelhos nem física. Interfaces e ligação do tiro ao prazo foram revisadas estaticamente; sem sessão Unity, builds ou atualização/reinício de servidor.

Protocolo **19**, pela nova variável replicada. Cliente e servidor precisam ser recompilados juntos. Preservadas as alterações locais anteriores de animações/recarga e o protocolo centralizado em GameConfig.
