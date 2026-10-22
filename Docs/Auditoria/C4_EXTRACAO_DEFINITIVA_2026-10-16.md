# C4 — extração confirmada é definitiva

Situação: **implementada e revisada estaticamente; não testada**. Escopo: E2-02, falha entre a confirmação do banco e a publicação/atualização do participante. Sem testes, Unity, compilação, builds ou sessões de jogo.

## Causa conferida

ServerBankExtraction chamava Bank, depois ServerRefreshProgress e somente então escrevia Status=Extracted. Se a leitura de progresso falhasse, o participante podia continuar Alive apesar dos itens já devolvidos ao acampamento. O campo progressionFinished impedia XP adicional, mas não bloqueava ações e DropItems.

## Implementação

Alterado somente **Assets/Scripts/Players/PlayerInventory.cs** nesta C4, preservando as mudanças de C3 nesse arquivo.

1. Após Bank retornar sucesso, um marcador local do servidor, extractionBanked, é definido imediatamente, junto de progressionFinished, antes de qualquer publicação ou nova leitura que possa lançar exceção.
2. IsAlive considera esse marcador: regras de ação, tiro, movimento, coleta e início de extração que usam IsAlive deixam de considerar o participante ativo, mesmo se o status de rede ainda não chegou a ser publicado.
3. DropItems tem proteção direta contra inventário já bancado. Commit/Lose/Rollback também não voltam a operar essa expedição após a confirmação. ServerSetStatus não consegue transformar o participante bancado em Alive, Dead ou TimeUp.
4. Chamadas repetidas de ServerBankExtraction retornam sucesso sem chamar Bank novamente. Itens e XP não são reprocessados para tentar atualizar a interface.
5. Estado Extracted/apresentação, atualização de progresso e sincronização do acampamento têm confirmações locais separadas. Falha de uma parte não impede tentar as demais. Update repete somente as partes pendentes a cada dois segundos, enquanto o objeto estiver no servidor e spawned. Uma nova tentativa nunca executa o banco outra vez.
6. ApplyStatus é aplicado também na tentativa de publicação, permitindo repetir a atualização de apresentação se um callback tiver falhado depois da atribuição de Status. Campos transitórios são reiniciados ao começar outra expedição.
7. Extração Sandbox mantém seu fluxo sem salvar itens/XP no repositório; usa o estado terminal local e a publicação de status, sem introduzir leituras de progresso persistente nesse caminho.

## Alcance e limitações

- A garantia implementada começa quando **CampRepository.Bank retorna sucesso**. Falhas internas ambíguas do armazenamento antes desse retorno não foram diagnosticadas nem declaradas resolvidas nesta tarefa.
- A repetição cobre exceções locais nas operações de publicação/atualização. Não é um protocolo novo de confirmação de entrega da interface; reconexão continua carregando o perfil salvo pelo fluxo existente.
- Morte/despawn não devem criar drops depois da confirmação; slots podem permanecer no objeto para apresentação até sua remoção, mas ficam fora do ciclo ativo no servidor.
- Não foram alterados timers, regras de manivela/corrente, critérios geométricos da zona ou balanceamento de extração. Falhas anteriores à confirmação do banco não foram agrupadas nesta correção.
- Não foi reaproveitado o resultado da antiga etapa 6 como aprovação do código novo.

## Verificação final reservada para T3

Injetar, em separado, falha após confirmação na publicação do estado, leitura do progresso e sincronização. Em cada cenário, conferir: ação/tiro/coleta recusados, ausência de drops na morte e despawn, banco executado uma vez, XP/itens contabilizados uma vez, atualização retomada quando o erro desaparecer e estado coerente após reconexão. Conferir também extração normal, repetição da chamada e Sandbox.

Próxima etapa: **C5 — recuperação de progresso por backup**, sem testes ou builds.
