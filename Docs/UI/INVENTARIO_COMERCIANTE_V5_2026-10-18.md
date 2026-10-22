# Inventário, personagem e comerciante — v5

## Apresentação

- Inventário: mochila 4×3 (12 espaços) e baú seguro 4×4 (16 espaços), com células do mesmo tamanho, seleção e detalhes no rodapé.
- Personagem: oito slots ilustrados organizados por região do corpo, atributos e mochila de 12 espaços para equipar. Selecionar uma peça equipada mostra seus detalhes; Desequipar precisa de espaço na mochila. O controle também permite selecionar origem e destino.
- Preparação: resumo da mochila/status e atalhos Organizar mochila e Personagem. Equipamentos equipados aparecem exclusivamente na aba Personagem, sem a antiga coluna na aba Inventário.
- Comerciante: inventário do jogador à esquerda, vitrine 4×3 à direita. Ver baú/Ver mochila escolhe a origem da venda. Selecionar mercadoria prepara a compra; selecionar um item próprio prepara a venda. Quantidade, preço e confirmação ficam no mesmo painel.
- Temas, área segura e orientação horizontal mantidos. Os prefabs continuam editáveis, com controles separados.

## Capacidades e legado

`ItemCatalog.BackpackSlots = 12`, `SafeChestSlots = 16`. `InventorySlots = 9` permanece como capacidade dos recipientes existentes de saque e como formato legado. `InventoryGrid(int capacity)` permite capacidades explícitas; a cópia conserva o tamanho. A mochila de rede também possui 12 espaços durante expedições.

O perfil migra de esquema 4 para 5. Os nove slots antigos mantêm seus índices; os novos começam vazios. Identidades, munição e equipamento não são recriados. Perfis antigos passam pelas validações existentes antes da expansão; o armazenamento compartilhado conserva seu backup de migração.

A lista “Itens no chão” e sua instanciação foram retiradas do fluxo. O campo legado `Ground` existe apenas para migração, sem novas inserções pelo jogo. Seus itens são entregues no baú e depois na mochila, em pilhas completas. Excedentes ficam guardados internamente e são entregues após ações na base ou retorno de expedição conforme surgir espaço. Não existe janela para acessar essa reserva.

Arrastar para fora da interface da base cancela o movimento. Descartar exige seleção e confirmação com nome/quantidade; o servidor confere o exemplar esperado e destrói a pilha inteira. Itens vinculados de recuperação continuam protegidos. Na expedição, soltar itens no mundo mantém sua regra existente.

Cozinha verifica espaço para qualquer resultado antes de começar. A atividade bloqueia outras ações de inventário e entrega o preparo na mochila ou baú, sem chão. A preparação de equipamento básico também verifica espaço; a partida fica bloqueada com orientação para liberar espaço se necessário.

## Vitrine pessoal e reposição

`Assets/Resources/MerchantCatalog.asset` contém preços, estoques iniciais e grupos editáveis. O estado pessoal fica no perfil, não no ScriptableObject. Existem 32 modelos candidatos: quatro consumíveis, seis armas, 18 equipamentos e quatro ferramentas.

Cada vitrine possui 12 ofertas:

- Quatro fixas: bandagem e os três tipos de munição.
- Duas armas distintas.
- Quatro equipamentos comuns de categorias distintas, entre cabeça, torso, mãos, pés, amuleto e anel. Os atributos do exemplar são sorteados e persistidos antes da exibição.
- Duas ferramentas distintas. Não entram chaves de progressão, tesouros ou equipamentos de teste.

| Mercadoria | Preço unitário | Estoque inicial por oferta |
| --- | ---: | ---: |
| Bandagem | 8 | 5 |
| Munição de pistola | 2 | 36 |
| Munição de rifle | 4 | 15 |
| Cartucho de espingarda | 5 | 12 |
| Espada / machado / lança | 30 / 40 / 40 | 1 |
| Pistola / rifle / espingarda | 40 / 90 / 80 | 1 |
| Equipamento comum | 25 | 1 |
| Gazua / alicate / pé de cabra / serra | 15 / 25 / 25 / 25 | 1 |

A compra inclui ciclo, índice da oferta, exemplar esperado e quantidade. O servidor valida revisão, moedas, capacidade e estoque antes de confirmar tudo junto. Compras de equipamentos entregam a identidade e os atributos mostrados. Ofertas esgotadas permanecem visíveis; vender não abastece a vitrine e conserva os preços de venda anteriores.

Fechar/reabrir o menu, reconectar e reiniciar o servidor preservam a vitrine. A reposição acontece uma vez no encerramento de uma expedição confirmada: extração, morte ou perda. Cancelar busca e desfazer reserva de entrada não repõem. O identificador da última expedição reposta e o estado de expedição impedem repetição.

O estoque de testes é persistido separadamente em `TestMerchant`; recursos, moedas e equipamentos de teste continuam temporários. Compras de teste não alteram `Merchant`, o estoque normal. Uma expedição de testes confirmada repõe somente a vitrine de teste. Reset explícito nas ferramentas de desenvolvimento continua podendo reiniciar os dados de teste.

## Integração e implantação

- Nova configuração `MerchantCatalog`, estados `MerchantState`/`MerchantOffer` e campos pessoais `Merchant`/`TestMerchant`.
- Comandos de compra usam os campos existentes `Reference`, `Result`, `Quantity` e `ExpectedItem` para a nova validação; descarte também exige `ExpectedItem`.
- Interfaces usam arrays explícitos de mochila/baú e limites de cada grade, sem divisão de todos os controles em blocos de nove.
- `Tools/UI/Reform-Inventory.py` registra a autoria incremental dos slots, ofertas, ícones e controles adicionais. Não executa reconstrução de cena e preserva IDs existentes.
- Protocolo de rede aumentado de 35 para **36**. Cliente e servidor precisam desta mesma versão. Implantar com expedições encerradas; não substituir saves manualmente nem reduzir arrays.

## Verificação desta entrega

Correção após relato de login: `Logs/server.log` apontou `Estoque do comerciante invalido` na validação de `TestMerchant`, antes da migração. O save principal e o backup ainda estavam no esquema 4, sem campos de comerciante. `JsonUtility` materializou o campo opcional ausente como objeto vazio. `MerchantState.IsAbsent` agora reconhece exclusivamente essa forma vazia, tanto na validação/checagem de identidades quanto na primeira criação do estoque de teste. Estoque normal de perfil v5 continua obrigatório, e estados parcialmente preenchidos continuam inválidos. Nenhum save real foi alterado durante a correção. `Tools/Audit/Check-MerchantOptionalState.ps1` executou oito casos sobre o classificador C# real; não simula `JsonUtility` nem confirma login em Unity. Aplicar a correção exige atualizar o executável do servidor; não houve rebuild ou reinício nesta correção.

Revisão estática de referências internas e relações pai/filho dos prefabs, tamanhos dos arrays, definições do catálogo, limites de transferência, validação de compra/descarte e caminhos de encerramento. Não foram executados testes automatizados, Unity, compilação, builds ou migração dos saves reais durante a edição.

Validação prática inicial:

1. Conferir os dois temas, os 12/16 slots e os últimos índices por toque, mouse e controle.
2. Selecionar, equipar, desequipar e comparar na aba Personagem; testar mochila cheia.
3. Confirmar/cancelar descarte; tentar descartar item vinculado e arrastar para fora na base.
4. Comprar parcialmente, esgotar uma oferta, tentar comprar sem moedas/espaço e vender do baú e da mochila.
5. Conferir atributos do equipamento antes/depois da compra e estoque após reconexão/reinício.
6. Conferir reposição após extração/perda, ausência de reposição ao cancelar entrada e separação entre testes e progresso normal.
7. Conferir perfil migrado, reserva legada e armazenamento do preparo com inventário cheio.
8. Conferir saque na expedição: mochila com 12 espaços e recipiente com sua capacidade anterior.
# Correção de carregamento: reserva histórica da expedição

O log do servidor revelou uma segunda falha ao validar o perfil após a migração: o escrow ainda tinha nove espaços enquanto o esquema já era 5. A migração agora amplia também esse array para 12, preservando os índices antigos. A validação em memória e no JSON aceita históricos inativos de nove espaços, inclusive os materializados pelo JsonUtility para campos opcionais; reservas ativas do esquema 5 continuam exigindo 12. Nenhum save foi editado manualmente.

Verificação focada: `Tools/Audit/Check-EscrowCapacity.ps1`, 36 casos com os métodos reais de capacidade e estrutura (validação de conteúdo isolada por stub). O probe anterior do comerciante passou novamente em oito casos. Isso não confirma serialização nativa nem login em execução; requer atualizar o servidor para a nova fonte.

