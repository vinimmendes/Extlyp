# C18 — Origem e destino no inventário por controle

Implementada e revisada estaticamente em 17/10/2026. **Sem compilação, testes, Unity ou builds.** Referência E3-05; depende de C17. Aceite em execução pendente da T10.

## Causa

O prefab de slot já era navegável e implementava Submit, mas Submit encaminhava a lógica de toque: coleta rápida de loot e seleção para equipamento. Mover entre mochila/baú, depositar e reorganizar dependia de Begin/Drag/Drop. Não havia origem/destino acessíveis somente pelo controle.

## Fluxo implementado na uGUI

- Navegar pelos slots com controle/teclado e confirmar um item seleciona a origem. Um contorno na cor do tema e o texto de instrução indicam a seleção. A comparação de equipamento continua disponível.
- Confirmar outro slot solicita a movimentação usando o fluxo existente: mochila ↔ baú/recipiente, reorganização no mesmo inventário, empilhamento/troca aceitos pelo servidor e mochila ↔ equipamento. Confirmar novamente a origem cancela a seleção. Equipamento ↔ baú exige passar pela mochila; destinos de equipamento incompatíveis conservam a seleção e orientam escolher outro slot.
- B/Esc cancela a seleção/arraste antes de fechar a janela. O cancelamento registra o frame consumido para que o Cancel da uGUI e o leitor de Back não cancelem e fechem na mesma pressão. Com nenhuma seleção ativa, o comportamento anterior de fechar permanece.
- Depois de selecionar, botão oeste do controle (X/Quadrado) ou Delete solicita descarte. Não descarta peça ainda equipada nem item vinculado; continua usando as permissões existentes de descarte. Sem seleção, esse atalho não descarta o item apenas focado.
- A seleção guarda o exemplar original; confirmação e descarte comparam o item atual antes de solicitar a operação. Mudança de dados, recipiente/contexto, perda de acesso, fechamento, troca para comércio/personagem ou perda de foco cancelam a seleção. Não há modificação local otimista dos inventários.

## Arquivos e preservação

- `Assets/Scripts/UI/UguiInventoryPresenter.Controller.cs` e `.meta`: seleção, confirmação de destino, comparação, cancelamento e descarte pelo controle.
- `Assets/Scripts/UI/UguiInventorySlot.cs`: separa Submit de clique/toque, encaminha Cancel e apresenta o contorno da origem. O prefab existente já contém Selectable/navegação; não foi reconstruído.
- `Assets/Scripts/UI/UguiInventoryPresenter.cs`: integração com carregamento/dados, instruções e limpeza; clicar/arrastar cancela a seleção do controle e segue o caminho de ponteiro existente. Comércio mantém sua confirmação própria.
- `Assets/Scripts/UI/InventoryWindow.cs`: consulta o cancelamento da seleção antes de fechar por Back. `Assets/Scripts/Players/GameInput.cs`: adiciona ação UI `DiscardItem`, restrita ao contexto estável de inventário, com botão oeste e Delete. Não altera reload no gameplay.

Mantidos mouse/toque, layout, safe area, dois temas, contratos do servidor, proteção do baú seguro fora da base e carregamento autorizado da C11. As operações passam por Transfer/ExchangeEquipment/Discard já existentes. Sem alteração de save, rede, protocolo, assets de jogo ou regras de limite dos itens. A implementação por controle é para o inventário uGUI ativo; o fallback IMGUI não foi convertido em outro sistema de navegação.

## Revisão e pendências

Consultados slot/prefab existentes, presenter, comércio, personagem, APIs da janela e contexto de C17. Confirmado estaticamente que o Button do prefab não possui callbacks persistentes de clique que dupliquem Submit. Aplicadas skills de UI/UX, Input System e uGUI, preservando a direção de arte. Não houve validação visual/prática.

Na T10, executar sem mouse: base com mochila/baú, saque do mundo, reorganização, pilhas, troca e slot vazio, depósito/coleta, equipar/guardar peça e destino incompatível. Conferir cancelamento por B e depois fechamento em outra pressão, inclusive nas duas ordens de Update/EventSystem. Conferir origem alterada enquanto selecionada, item vinculado/equipado, descarte intencional, carregamento/perda de acesso e confirmação pendente. Alternar com clique, toque e arraste; conferir contorno/instruções nos dois temas e foco entre as grades. Verificar comparação de atributos, comércio/personagem e proteção do gameplay no botão oeste. A navegação geométrica e legibilidade continuam pendentes de execução.
