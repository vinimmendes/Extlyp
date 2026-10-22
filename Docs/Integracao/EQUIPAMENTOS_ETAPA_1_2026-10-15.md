# Equipamentos — etapa 1: dados e salvamento

Implementação em 15/10/2026. Escopo: preparar os dados para as próximas etapas, sem habilitar equipamentos novos, pontos de atributo, loot novo ou interface de slots.

## Estrutura criada

- `AttributeId` e `AttributeValues`: os seis atributos com IDs estáveis e serialização de rede. `CharacterProgress` armazena nível inicial 1, XP/moedas/pontos livres em zero e investimentos em zero; cada atributo-base começa em 1. Não concede XP, pontos ou efeitos de combate nesta etapa.
- `EquipmentSlot`, `ItemRarity` e `ItemOrigin`: identificadores para slots, raridade e origem normal/recuperação. São estrutura de dados; a marca de recuperação ainda não altera as regras econômicas atuais.
- `ItemInstance`: identidade de 128 bits, bônus, raridade e origem. Struct sem referências gerenciadas, compatível com `NetworkList<ItemStack>`. A identidade usa duas metades numéricas no JSON/rede.
- `ItemDefinition`: esquema de ScriptableObject para autorar as definições das futuras peças. Nenhuma definição de item novo ou tabela de loot foi ativada.
- `ItemStack.Instance` transporta os metadados junto do item. `Data` continua preservando munição carregada e os dados específicos existentes; `Quality` continua separado da raridade do equipamento. Itens individualizados não empilham.

Armas novas recebem identidade ao serem criadas/adicionadas. Descarte e coleta de pickups preservam os metadados; recarga continua alterando somente `Data`. A comparação das pilhas inclui os metadados. A adição recusa uma identidade já presente no mesmo inventário, e o repositório verifica duplicações entre os contêineres persistentes antes de confirmar transações.

O escrow de expedição reservada participa da verificação. Depois do commit, ele representa uma fotografia histórica e não é tratado como um segundo proprietário: o jogador pode ter largado ou transferido a arma durante a partida. Esta etapa não cria um registro global de todos os objetos vivos de todas as salas.

## Versões e migração

- Documento de armazenamento compartilhado: versão 2 → **3**.
- Perfil individual: legado sem versão → **SchemaVersion 1**.
- Protocolo de rede: 21 → **22**, pois ItemStack e pickups agora carregam metadados adicionais.

Ao carregar armazenamento legado, o servidor grava uma cópia durável `<cluster>.json.pre-v3-<guid>.json` no diretório de progresso antes de recuperar/migrar dados. Esse backup não é o `.bak` rotativo e não é sobrescrito em operações normais. A escrita principal continua usando arquivo temporário, flush e substituição antes de publicar o estado na memória compartilhada.

A migração percorre mochila, baú e itens no chão da base, atribuindo identidade às armas que não a possuíam. Preserva IDs de item existentes, posições, quantidades, munição, qualidade, comida e progresso de acampamento. Não move armas para slots novos: isso pertence à etapa 2. Repetir o carregamento de um perfil já migrado não cria outras identidades nem reinicia o personagem.

No carregamento frio, expedições pendentes passam primeiro pela recuperação já existente. Perfis legados ainda ativos na memória compartilhada são rejeitados para migração até que a expedição seja encerrada/recuperada. Formatos futuros não suportados ou armas legadas com quantidade inválida são rejeitados sem sobrescrever o progresso original. Backups podem existir mesmo quando a operação posterior falha; isso não confirma migração concluída.

Novos perfis recebem o esquema atual pelo repositório. A migração foi integrada tanto ao armazenamento compartilhado Windows quanto ao repositório em memória. Nos perfis já migrados, os dados de personagem são preservados.

## Uso nas próximas etapas

Atualizar todas as salas e clientes juntos para o protocolo 22, após encerrar as instâncias antigas; não misturar servidores antigos e novos sobre o mesmo armazenamento. Não foi feito deploy, reinício de servidor ou migração dos perfis reais durante esta tarefa. O backup automático será produzido quando o servidor atualizado carregar efetivamente dados legados.

Etapa 2: implementar os slots e as transações de equipar/desequipar sobre estes dados. Os atributos ainda não afetam mira, visão, vida ou movimento; o sistema de níveis, a distribuição de pontos e os sorteios continuam nas etapas previstas.

## Revisão e limites

Revisão estática dos caminhos de criação, cópia, transferência, descarte/coleta, escrow, persistência e compatibilidade de rede; `git diff --check` sem erros de espaço nas alterações verificadas. Não foram executados testes automatizados, compilação manual, Play Mode ou builds para validar esta etapa, conforme preferência do projeto. Serialização nativa/NGO e migração real ainda precisam de validação prática quando solicitada.
