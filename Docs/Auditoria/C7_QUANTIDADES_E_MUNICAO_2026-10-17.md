# C7 — Quantidades e limites de munição

Implementada e revisada estaticamente em 17/10/2026. **Não compilada nem testada.** Referência: E2-05; depende de C6.

## Causa identificada

`ItemStack.Empty` considera qualquer quantidade menor ou igual a zero como vazia. O validador persistente não recusava quantidades negativas nem aplicava universalmente `ItemCatalog.Limit`. A validação de recuperação verificava a relação entre munição vinculada e carregada, mas não a capacidade do carregador de armas normais. Valores de JSON também precisavam ser conferidos antes da conversão para os inteiros usados no perfil.

## Correção

- `Assets/Scripts/Camp/CampProfileMigration.cs`: a validação dos valores do item, usada na conferência estrutural, recusa quantidade negativa ou maior que `ItemCatalog.Limit(item)`. Essa conferência ocorre antes de `Ensure`, migração e recuperação; alcança baú, mochila, equipamento, chão e reservas históricas. A validação final das transações usa a mesma regra.
- Armas ocupadas exigem `Data` entre zero e `WeaponCatalog.Get(item).Capacity`, inclusive nos extremos. A capacidade de melee é zero. A pistola utiliza a definição do jogo já consumida pelo catálogo; rifle e espingarda usam as capacidades existentes de 5 e 2. Nenhum balanceamento foi alterado.
- A verificação existente de `RecoveryRules.Valid` continua exigindo munição vinculada não negativa e não superior à carregada. Com a nova regra de capacidade, a munição vinculada também fica limitada pelo carregador.
- `Assets/Scripts/Camp/SharedCampMemory.cs`: a quantidade é conferida no JSON antes da desserialização. `Data` e `Instance.RecoveryRounds`, quando presentes, precisam ser inteiros na faixa de `Int32`; campos históricos ausentes continuam com o padrão zero. Valores fracionários, nulos explícitos e números fora da faixa são recusados.

## Preservação e limites do escopo

Não há truncamento, divisão automática de pilhas históricas, redução de munição, descarte de excesso ou troca de identidade. Candidatos inválidos de disco seguem a política de preservação/backup da C5. Nenhum save real foi aberto ou alterado. A redução futura de uma capacidade ou limite poderá exigir revisão explícita de saves que excedam o novo valor; não existe adaptação silenciosa.

Quantidade zero permanece permitida para slots vazios; não foi imposta limpeza dos metadados residuais desses slots. O campo `Data` de itens que não são armas não é tratado como munição, pois também armazena informações de outros sistemas, como a espécie do peixe preparado.

`InventoryGrid.Add` continua podendo receber lotes maiores que uma pilha e distribuí-los entre slots. A regra nova verifica as pilhas efetivamente armazenadas, preservando os produtores existentes de lotes iniciais, receitas e recursos de teste. Nenhuma alteração no algoritmo de adição, catálogo, configuração, rede ou versão de save.

## Revisão e validação pendente

Revisão estática dos caminhos de validação antes/depois da recuperação, gravação de perfil e transação; consultados os produtores de inventário inicial, loot, receitas e munição, além das regras de equipamento e recuperação. Não foram executados testes, Unity, builds ou servidores/clientes.

Na T5, conferir quantidades -1, 0, 1, limite e limite+1 para materiais, tesouros, armas, equipamentos e ferramentas; carregadores em -1, 0, capacidade e capacidade+1; melee com `Data` diferente de zero; munição vinculada negativa/maior que a carregada. Repetir os casos relevantes em baú, mochila, chão, equipamento e reservas antes/depois do commit. Conferir números JSON acima de `Int32`, fracionários e nulos, campos históricos ausentes e preservação do documento recusado. Na T4/T5, verificar o backup quando o candidato principal falhar por essas regras. Confirmar ainda que lotes legítimos maiores que uma pilha continuam divididos por `InventoryGrid.Add`. Evidência de execução permanece pendente.
