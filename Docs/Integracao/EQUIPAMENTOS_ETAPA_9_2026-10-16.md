# Comércio e redistribuição — etapa 9

Implementação em 16/10/2026, com revisão estática. Não foram executados testes, compilação, sessões Unity ou builds. Funcionamento em execução, importação do prefab, navegação e apresentação aguardam avaliação prática do usuário.

## Comércio na base

Abra **Mochila > Comerciante**. O modo de venda mostra mochila e baú seguro; selecione um item, ajuste a quantidade com −/+/Tudo e confira o total antes de Confirmar venda. Peças equipadas precisam ser guardadas primeiro. Comprar mostra seis ofertas fixas; selecione uma e confira preço/quantidade antes de Confirmar compra. A entrega vai para a mochila, sem descarte automático no chão. Sem espaço ou saldo, a operação é recusada integralmente.

Os controles foram acrescentados ao prefab existente `Assets/Resources/UI/Prefabs/Inventory.prefab`. Não há criação da hierarquia pelo presenter nem janela sobreposta à mochila. A geometria usa os limites e a área segura existentes, com os mesmos controles nos temas Simples/Sombria, em orientação horizontal. `Tools/UI/Add-CampCommerce.py` registra o autoramento pontual e recusa uma segunda execução sobre o prefab já atualizado.

### Preços de venda por unidade

| Categoria | Moedas |
| --- | --- |
| Equipamento comum / incomum / raro | 8 / 20 / 45 |
| Arma de fogo ou melee | 12 |
| Madeira, pregos, metal | 1 |
| Pólvora, componentes | 3 |
| Munição | 1 por cartucho |
| Bandagem | 2 |
| Gazua, alicate, pé de cabra, serra | 5 |
| Peixe comum / raro / grande | 2 / 5 / 8 |
| Peixe cozido / cru / queimado | 3 / 1 / 0 |
| Tesouros de cada mapa | 10 / 30 / 80, conforme o modelo |

Objetivos de expedição, chaves de passagem e itens provisórios `Training*` não são vendáveis. Itens de recuperação vinculados também não são vendáveis. A raridade de armas não multiplica preço ou dano. Vender peixe queimado por zero é uma remoção explicitamente confirmada, sem crédito de moeda.

### Ofertas fixas

| Compra | Quantidade por operação | Preço |
| --- | --- | --- |
| Bandagem | 1 | 8 |
| Munição de pistola | 12 | 24 |
| Munição de rifle | 5 | 20 |
| Cartucho de espingarda | 4 | 20 |
| Espada curta básica, sem afixos | 1 | 30 |
| Pistola básica, vazia | 1 | 40 |

Cada confirmação compra um lote da oferta, com identidade nova para a arma. Não há estoque aleatório, compra das 18 peças, rerrolagem, compra de moedas ou conversão direta de metal em moeda. A troca existente de metal por componentes continua na bancada.

Moedas pertencem ao `CharacterProgress.Coins` persistente. Extrair tesouro não credita sua venda automaticamente. O saldo muda apenas após uma venda ou uma despesa aceita pelo servidor; comprar/vender no laboratório usa o perfil temporário existente, separado do normal.

## Armas carregadas e descarga

A descrição informa o preço fixo da arma. Ao selecionar uma arma carregada para venda, o painel avisa que a munição não aumenta esse preço e será perdida caso a venda seja confirmada sem descarregar. **Descarregar** transfere toda a carga para a mochila antes da venda; pode ser usado em uma arma guardada na mochila ou no baú. O item equipado precisa ser guardado primeiro.

A descarga trabalha em cópias dos inventários: se toda a munição não couber, inclusive as pilhas separadas por origem, nada muda. Se couber, preserva a identidade e os bônus da arma e zera sua carga. A marca de origem da munição é mantida; munição normal e de recuperação não se misturam numa mesma pilha.

## Redistribuição de atributos

Em **Mochila > Personagem**, o saldo aparece com os pontos. **Redistribuir** mostra o custo de `50 + 25 × nível` moedas, informa quantos pontos serão devolvidos e exige um segundo clique em Confirmar. O servidor recalcula o preço e exige base, saldo suficiente e pelo menos um ponto investido. No nível 1 sem investimento, o botão fica indisponível e não há cobrança.

A operação zera somente `Invested`, preservando XP e nível. `Normalize` recalcula os pontos livres: `nível − 1`. Bônus de equipamento permanecem. A redistribuição não ocorre durante expedição nem concede pontos adicionais. Alterar a revisão do perfil invalida a confirmação pendente; investimento continua seguindo o limite de dez pontos por atributo.

## Recuperação básica e origem da munição

A regra existente continua verificando armas na mochila, baú, equipamento e chão privado da base antes de conceder recuperação gratuita. A nova pistola e a munição dessa recuperação recebem `Origin.Recovery`. Equipamento normal anterior é preservado, sem marcação retroativa arbitrária.

Até uma extração bem-sucedida, itens de recuperação não podem ser vendidos, descartados ou colocados em recipientes compartilhados. Permanecem utilizáveis pelo dono e podem ser reorganizados entre seus inventários privados. Na morte, arma e reserva de recuperação não viram loot para outra conta. Rollback mantém a marca. A extração aceita pelo repositório remove a marca dos itens efetivamente retornados; itens guardados na base continuam vinculados até serem extraídos.

`ItemInstance.RecoveryRounds` acompanha quantos cartuchos de recuperação estão carregados, inclusive dentro de uma arma normal. A recarga remove as mesmas pilhas da reserva que contabiliza; o disparo consome os cartuchos vinculados primeiro. A descarga devolve munição normal e vinculada em pilhas distintas. Uma arma normal que ainda contém munição vinculada não pode ser vendida/transferida até descarregar, gastar essa munição ou extrair. Na morte, a arma normal pode cair, com os cartuchos vinculados retirados da carga.

Cartuchos de recuperação não geram metal pelo reciclador, impedindo converter a reposição gratuita em material vendável sem extração. Cartuchos normais mantêm o comportamento existente do reciclador, inclusive numa arma de recuperação. Munição carregada continua em `ItemStack.Data`; a nova contagem de origem fica em metadado separado, sem reutilizar o campo de carga.

## Transações e compatibilidade

- `CampTrade` define preços e ofertas no servidor. Comandos enviam intenção, item esperado, slot e quantidade, sem valor de venda/compra escolhido pelo cliente.
- `CampNetwork` mantém a autorização por sessão autenticada da base e bloqueia ações enquanto uma atividade estiver ocupada. `CampProgress.ApplyCommand` exige `ExpeditionState == 0`, próxima revisão e ID válido, rejeitando também IDs ainda registrados em `AppliedActions`.
- Venda e descarga comparam o `ItemStack` completo esperado, incluindo identidade, raridade, bônus, origem e carga. Comandos reenviados com sequência antiga são recusados. A UI aguarda a resposta do servidor e invalida a cotação se o perfil mudar.
- Compra e descarga verificam a capacidade antes de trocar os inventários; venda verifica estouro de `long` antes de creditar. O perfil rejeita moedas negativas. O repositório mantém seu commit persistente e validação de propriedade existentes.
- **Protocolo 30**, **schema de perfil 4**, **documento compartilhado 6**. Atualizar cliente e servidor juntos. O mecanismo de migração existente faz backup antes de converter o documento. O novo contador começa em zero para itens normais; uma arma legada já explicitamente marcada como recuperação tem sua carga conservadoramente marcada. Nenhum item normal é rerrolado ou convertido.
- Encerrar/resolver expedições anteriores antes de migrar perfis em memória; permanece a regra existente de não migrar perfil com expedição ativa. Não foram reiniciados servidores nem alterados saves reais nesta implementação.

Próxima entrega: **etapa 10 — resultados e balanceamento**. Os valores desta etapa são a configuração inicial do plano; nenhum ganho de equilíbrio ou validação de rede/UI é afirmado sem avaliação em execução.
