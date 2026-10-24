# Escolha do nome da conta — planejamento

Status: planejamento histórico, substituído pelo plano aprovado e pela [implementação de 23/10/2026](PERFIL_IMPLEMENTACAO_2026-10-23.md). Decisões pendentes abaixo foram fechadas nessa entrega.

A escolha e a alteração de nome integrarão a aba descrita em [Perfil e estatísticas](PERFIL_E_ESTATISTICAS_PLANO_2026-10-22.md).

## Decisões confirmadas pelo usuário

- O jogador escolhe seu nome visível.
- Nomes podem se repetir; a identidade interna continua única.
- Será possível trocar o nome com limite ou custo, ainda a definir.

## Situação atual

`CampIdentity` identifica o perfil pela chave pública, com credencial local por aparelho. `CampRepository` autentica e persiste o progresso; `CampSession` recebe o perfil sincronizado. `PlayerInventory.DisplayName` apresenta humanos como `Jogador N`, derivado da conexão, e preserva os indicativos próprios dos bots. O combate já consulta essa propriedade para identificar o responsável pela morte.

Escolher ou alterar nome não deve gerar chave, trocar Session, recuperar conta por nome nem criar outro perfil. A recuperação e a transferência entre aparelhos são outro escopo.

## Fluxo proposto

1. Após autenticar e sincronizar um perfil sem nome, apresentar na base um painel de escolha com campo, regras curtas e botão Confirmar. Perfil existente passa pelo mesmo fluxo, preservando itens e progresso.
2. A primeira escolha é gratuita. A proposta é exigir a escolha antes de partir para uma expedição; a base e as opções continuam acessíveis. Esse requisito precisa existir também no servidor, não apenas no botão.
3. Enviar a escolha como ação autenticada. Durante a gravação, impedir envio duplicado; só mostrar sucesso após confirmação do servidor. Em erro, conservar o texto digitado e permitir nova tentativa.
4. Exibir o nome confirmado na central da base e na ficha. Na expedição, usar o nome nas referências a jogadores já existentes, incluindo identificação do atacante. Não acrescentar rótulos sobre personagens ou revelar jogadores ocultos.
5. Oferecer Alterar nome na base. Mostrar a regra, eventual preço e nome final antes da confirmação. A troca durante a expedição fica indisponível.

Os dois temas compartilham o fluxo. Usar uGUI/TMP e o pipeline de prefabs existente, com teclado Android, limite de texto e ajuste à área segura em paisagem. Cancelamento do teclado não confirma a operação. A interface deve distinguir Nome do jogador de identificador de acesso, sem exibir chave pública ou Session.

## Regras propostas para o texto

- De 3 a 20 caracteres, com letras, acentos, números, espaços internos, hífen e sublinhado. Confirmar esses limites antes da implementação.
- Normalizar Unicode para NFC, remover espaços nas extremidades e reduzir espaços consecutivos antes da validação. Contagem deve considerar caracteres visíveis, com limite adicional em bytes para a representação de rede.
- Rejeitar nome vazio, quebras de linha, controles, caracteres invisíveis de direção e marcação de texto. Não truncar silenciosamente nem aceitar somente porque o campo local aceitou.
- Renderizar como texto literal, com rich text desativado nos campos de nome. Conferir a cobertura da fonte para o conjunto permitido.
- A validação definitiva pertence ao servidor; o cliente repete as regras apenas para orientar o jogador. Duplicidade de nomes é permitida.
- Confirmação do mesmo nome já salvo é uma operação sem cobrança ou alteração de prazo. Reenvio de uma mesma ação não pode cobrar novamente.

## Integração técnica prevista

1. Acrescentar os dados do nome a `CampProgress`, incluindo o necessário para aplicar a futura política de troca. Atualizar cópia, validação, migração de schema e snapshots; perfis antigos começam sem nome escolhido.
2. Criar uma regra C# de normalização/validação compartilhada. Separar a política de troca do texto; preços e prazos são configurações do servidor, nunca valores aceitos do cliente.
3. Acrescentar a ação de nome ao final de `CampActionKind`, sem renumerar ações existentes. Usar campo explícito para o nome em `CampAction`, preservando revisão, identificação da ação, posse da sessão e transação do repositório. Verificar os limites do envelope ao ampliar o comando.
4. Aplicar nome, eventual débito e prazo na mesma transação persistida. Consultar o horário do servidor. Se a gravação falhar, nada deve ser cobrado ou confirmado.
5. Fazer o servidor inicializar o nome replicado do jogador a partir do perfil autenticado. Usar string de tamanho limitado compatível com NGO 2.13.3; dimensionar os bytes após fechar o conjunto permitido. O cliente não escreve diretamente o nome replicado.
6. Fazer `PlayerInventory.DisplayName` preferir o nome confirmado dos humanos, conservando os bots e um fallback durante sincronização. Clientes que entram depois também precisam receber o nome. A mudança do prefab/protocolo exige entrega coordenada de cliente e servidor.
7. Implementar o painel de primeiro nome e a edição na base, atualizar a ajuda e localizar os consumidores de `DisplayName`. A navegação da base deve reagir à confirmação sincronizada, inclusive após reconexão.

## Política de troca pendente

Antes de concluir a implementação, escolher prazo, custo ou combinação dos dois. Como ponto de partida para discussão: primeira escolha gratuita e troca gratuita a cada 7 dias. Essa sugestão ainda não foi aprovada e não deve virar regra implicitamente.

Se houver preço, definir moeda e valor; se houver prazo, definir duração e se começa na primeira escolha. O painel deve informar a próxima disponibilidade. Não criar moeda, item de renomeação ou loja nova apenas para esse recurso.

## Ordem de entrega e avaliação

1. Fechar regras de texto, exigência antes da expedição e política de troca.
2. Implementar persistência, migração e comando autoritativo.
3. Implementar primeiro acesso e edição na base.
4. Integrar nome replicado, ficha e mensagens de combate.
5. Revisar estaticamente e entregar para a primeira avaliação prática do usuário, sem iniciar testes, Unity ou builds de validação por padrão.

Roteiro para a avaliação prática: perfil novo e antigo; nomes iguais em dois celulares; acentos e limite; rejeição de texto inválido; reinício e reconexão; cancelamento do teclado; confirmação repetida; falha ao salvar; restrição de troca e eventual débito; clientes entrando em momentos diferentes; preservação dos bots e da ocultação por visão. Testes automatizados ou execução assistida somente quando solicitados ou acionados por problema relatado, conforme AGENTS.md.
