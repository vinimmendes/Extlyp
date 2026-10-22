# Retirada das armas de fogo — 19/10/2026

Pistola, rifle de ferrolho, espingarda de dois canos e suas três munições foram desativados em todos os fluxos ativos. IDs 6, 7, 14, 15, 16 e 17 continuam reservados para interpretar perfis históricos. `Valid` significa ID reconhecido; `Available` significa item autorizado no jogo. As definições balísticas antigas são acessíveis internamente apenas para validar a migração; `WeaponCatalog.Get` retorna null para itens retirados.

## Conteúdo ativo

- Kit inicial permanece besta de mão improvisada descarregada, espada enferrujada e cinco virotes.
- As dez tabelas de loot e a tabela de quebráveis não contêm armas/munições retiradas. As entradas de pistola/munição de pistola deram lugar à besta/virotes; rifle, espingarda e munições foram retirados. Baú especial também entrega virotes.
- Comerciante: duas ofertas fixas (bandagem e virotes), duas armas entre espada curta, machado, lança e besta, quatro equipamentos e duas ferramentas: **dez ofertas**, sem preencher artificialmente os espaços restantes da grade. Besta herda preço 40/estoque 1; virotes herdam preço 2/estoque 36. Outros preços preservados.
- Laboratório filtra IDs retirados e oferece apenas as cinco armas medievais. Comandos antigos de entrega são recusados. Inventário, equipar, venda, compra e resolvedores de ícones também bloqueiam os itens.
- Receitas de munição de arma de fogo desativadas e seus controles ocultos. Nenhuma receita nova de virotes foi inventada. Recursos genéricos pólvora/componentes, seus preços e os demais sistemas da bancada não fazem parte desta retirada.
- Áudio de tiro/recarga de arma de fogo não é mais carregado. Resumos, ajuda, kit de fechaduras e exemplos da organização de HUD usam besta/virotes. Símbolos de recarga/disparo representam virotes, sem mudar geometria ou personalizações.

## Perfis e atualização

Esquema de perfil **5 → 6**, protocolo **38 → 39**. Atualizar cliente e servidor juntos, com as expedições encerradas. Servidores antigos não devem continuar escrevendo no mesmo armazenamento.

A migração retira os seis itens de mochila, baú, equipamento, reservas de expedição e reserva legada do chão. Os demais itens, moedas, atributos, capacidades e identidades são preservados. Não converte armas antigas em novos exemplares nem concede compensações. A recuperação inicial já existente continua disponível conforme suas regras.

Vitrines antigas são reconstruídas preservando ofertas ainda válidas, seus exemplares, preços e estoque restante. Só os espaços retirados recebem ofertas compatíveis. O ciclo recebe nova identidade para recusar compras pendentes antigas; `LastRestockedExpedition` é preservado. Vitrines já renovadas pela recuperação de uma expedição não são renovadas novamente pela migração. Ambiente normal/teste permanece separado.

O armazenamento compartilhado usa sua cópia de segurança de migração existente antes da gravação. Não houve edição direta ou exclusão dos saves nesta implementação. Arquivos de arte/fontes antigos permanecem preservados; os resolvedores ativos não os apresentam como itens disponíveis.

## Verificação

Revisão estática de geração, transações, migração, validação anterior/posterior à recuperação de expedição e referências serializadas. C# compilado isoladamente com referências da Unity 6000.0.84f1. Sem iniciar Unity, testes automatizados de jogo ou builds. A primeira validação prática permanece com o usuário.
