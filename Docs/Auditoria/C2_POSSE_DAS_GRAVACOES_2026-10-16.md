# C2 — impedir gravações por sala sem posse

Situação: **implementada e revisada estaticamente; não testada**. Escopo: proteção das transações do repositório para E2-01. Nenhuma Unity, compilação, build, sessão de jogo ou prova automatizada foi iniciada.

## Causa conferida

Claim registrava Lease/LeaseExpires, mas Edit/Save não exigiam essa autorização. Begin, Commit, Rollback, Bank e Lose passam por Edit; XP usava transações diretas. Refresh também permitia renovar uma autorização já vencida. Isso deixava uma sala antiga escrever depois de outra assumir o jogador.

## Alterações

| Arquivo | Mudança desta etapa |
| --- | --- |
| Assets/Scripts/Camp/CampProgress.cs | Mantém credenciais das aquisições locais; OwnedRecord confere proprietário, token e prazo dentro da transação. Edit e Save exigem posse. Claim concede credencial local somente após sucesso da transação; nova aquisição gera outro token e incrementa a revisão. Refresh/Release retornam false para credencial inválida/vencida. Close/EnableShared descartam credenciais locais anteriores. |
| Assets/Scripts/Camp/SharedCampMemory.cs | Acrescenta LeaseToken ao registro interno; limpa esse token junto à lease na recuperação inicial do armazenamento. Não muda o formato de CampProgress enviado ao jogador. |
| Assets/Scripts/Camp/CampExperience.cs | Confere posse de todos os participantes antes de aplicar um lote de XP; PvP confere os dois perfis antes de modificar recompensas/cooldowns. |
| Assets/Scripts/Camp/CampNetwork.cs | Envia a revisão obtida antes de modificar a atividade para Save; o repositório recusa snapshot desatualizado ou sem avanço da revisão. |

A conferência acontece no callback da transação, sob o mutex quando o armazenamento compartilhado está habilitado. Uma falha de autorização lança erro antes da mutação econômica; no armazenamento em memória também é exigida uma aquisição válida. Renovação não reativa uma lease expirada: é necessária uma nova aquisição.

A autorização usa um token por aquisição, além do nome de proprietário, para distinguir gerações. Repetir Claim com a credencial ativa da própria sala preserva o token; uma aquisição após expiração cria outro. O token não é incluído no snapshot público de CampProgress.

Save agora exige expectedRevision. Os dois fluxos de gravação de atividades do CampNetwork capturam a revisão antes da alteração e a encaminham ao repositório. A leitura dos caminhos de cozinha/atividade mostra avanço de Revision antes da gravação; isso foi revisão estática, não execução desses fluxos.

## Limites e dependências

- **C3 permanece necessária.** A credencial local identifica a aquisição da sala/processo. Não identifica por si só cada callback antigo de cliente dentro do mesmo processo. A desconexão ainda precisa conferir o proprietário esperado e garantir limpeza mesmo quando a gravação é recusada.
- O chamador periódico ainda ignora o retorno de Refresh. A recusa bloqueia a renovação/escrita; o tratamento de conexão e limpeza após perder posse será integrado na C3, sem afirmar encerramento completo de E2-01 nesta etapa.
- Autenticação assinada anterior à aquisição, leitura de perfil e recuperação/migração inicial do armazenamento continuam caminhos de infraestrutura. Não foram substituídos por comandos econômicos sem autorização.
- LeaseToken é um campo adicional opcional no registro de armazenamento. Registros recuperados do disco têm as credenciais transitórias limpas. Não foram executadas migrações nem alterados saves reais. Não misturar servidores antigos sem essa proteção com os novos no mesmo cluster.
- Verificadores antigos que escrevem diretamente sem Claim precisarão adquirir uma lease no preparo da prova. Consumidores da assinatura interna antiga de Save também precisam fornecer a revisão. Os chamadores de runtime em Assets foram ajustados; as provas históricas da base congelada não foram reescritas nem executadas. Preparar essas fixtures na fase T sem enfraquecer a regra para fazê-las passar.
- Scripts e alterações locais anteriores foram preservados. Engine, pacotes, RPCs e configuração de cenas/prefabs não foram alterados.

## Verificação reservada para o final

T2 deve conferir: escrita antes de Claim recusada; dono ativo escreve; outro dono não assume antes do prazo; vencimento bloqueia escrita/renovação; transferência impede Edit, Save, Begin/Commit/Rollback/Bank/Lose e XP pela sala antiga; cópia antiga é rejeitada; lote de XP com participante sem posse não grava parcialmente. Confirmar também retomada após reinício e novo token ao readquirir com o mesmo nome de proprietário.

A integração de callbacks será revista separadamente em C3. Não usar o resultado da etapa 6, anterior a esta mudança, como validação do código novo.

Próxima execução: **C3 — proteger contra desconexão atrasada**, sem testes ou builds.
