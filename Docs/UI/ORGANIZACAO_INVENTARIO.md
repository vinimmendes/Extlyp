# Organização da mochila e do baú seguro

O botão **Organizar** atua apenas no recipiente indicado. A mochila pode ser organizada na base, na expedição e enquanto a janela de saque está aberta. O baú seguro tem seu próprio botão somente na base; recipientes de saque não recebem organização automática. Comércio e personagem não exibem esses botões.

## Regra

Armas → equipamentos → munição → cura → alimentos/peixes → ferramentas/chaves → tesouros → materiais. Dentro da categoria, usa-se `ItemId`, preservando a ordem relativa das variantes. Pilhas compatíveis por `ItemStack.Same` são reunidas até o limite, com vazios ao final. A operação copia os valores existentes, preservando identidade, qualidade, dados, origem e bônus; não gera itens ou novas identidades. Equipamentos vestidos e arma ativa não são reorganizados.

## Autoridade e confirmação

Na base, `CampActionKind.Organize` utiliza a revisão, o lease e a persistência atuais. A ação não entrega itens legados pendentes em outros recipientes. Na expedição, o RPC recebe a fotografia esperada dos 12 slots e organiza somente se ela ainda corresponder ao estado do servidor. O proprietário aguarda `OrganizationReceipt`; a confirmação é consumida no Update, após a aplicação das atualizações de rede. Não há rearranjo otimista no cliente.

Organização fica indisponível durante arraste ou seleção de transporte por controle, confirmação de descarte, sincronização pendente e ações incompatíveis de combate/interação. A janela bloqueia movimentação, descarte e troca de equipamento enquanto aguarda o servidor. Após a confirmação, seleções por índice são limpas. Textos: “Mochila organizada”, “Baú organizado” ou “Já está organizado”; recusas orientam uma nova tentativa.

Os botões são autorados em `Inventory.prefab`, usam os estilos atuais e uma faixa própria antes da grade. Layout compacto e normal compartilham os dois temas, área segura e orientação horizontal. O protocolo passa a 41: cliente e servidor precisam de builds compatíveis. O formato dos perfis não muda.

## Validação prática pendente

Não foram iniciados testes automatizados, builds nem sessões Unity para validar esta entrega. Na primeira avaliação prática, conferir:

- Recipientes vazios, cheios, com buracos e já organizados; repetição idempotente.
- Pilhas com variantes, limites e itens únicos/vinculados; conservação de quantidades e atributos.
- Separação entre mochila, baú e equipamentos; persistência ao reconectar na base.
- Recebimento de itens simultâneo à organização, rejeição de fotografia desatualizada e recuperação após desconexão.
- Bloqueios durante ações, confirmação, arraste e descarte; limpeza da seleção anterior.
- Mouse, toque e navegação por controle, temas Simples/Sombria, base/expedição/saque e layout compacto.
