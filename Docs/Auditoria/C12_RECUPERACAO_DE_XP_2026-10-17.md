# C12 — XP recuperável após falha transitória

Implementada e revisada estaticamente em 17/10/2026. **Sem compilação, testes, Unity ou builds.** Referência E2-07. Aceite em execução pendente da T8.

## Causa identificada

`Health.Experience` marcava a morte como recompensada antes de tentar gravar. Uma exceção encerrava o fluxo somente com log; após despawn, não restava uma tentativa recuperável. O rateio e a chave de deduplicação já existiam, mas não havia fila independente da entidade. Objetivos também tinham chamada única e exploração dependia de permanecer/reentrar na região.

## Implementação

- `Assets/Scripts/Camp/PendingExperience.cs` e `.meta`: fila no repositório, independente de GameObjects. Captura cópias dos valores de cada concessão, chave estável da morte/objetivo/região, sessão/expedição e credencial da aquisição original. PvP captura também a expedição da vítima e ambas as credenciais. A fila não retém PlayerInventory/Health e não recalcula elegibilidade ou rateio após a morte. Notificações repetidas do mesmo evento pendente são deduplicadas.
- `Assets/Scripts/Combat/Health.Experience.cs`: calcula o rateio original e enfileira antes de encerrar a tentativa da entidade. A gravação imediata pode falhar sem apagar o evento. Despawn não remove a fila. Mortes sem recompensa/elegibilidade continuam sem criar concessão.
- `Assets/Scripts/Players/PlayerProgression.cs`: objetivos e exploração usam a mesma fila. Uma região é marcada como visitada quando o evento foi enfileirado; sair da região após a falha não impede a tentativa posterior. Falha antes de enfileirar continua sendo relatada como falha, não como XP salvo.
- `Assets/Scripts/Camp/CampProgress.cs`: cada transação processa uma cópia da fila antes da operação, inclusive antes de Bank/Lose/EndConnection/Recover. Se qualquer etapa ou persistência falhar, o lote continua pendente e a transação não é confirmada. Somente depois do retorno bem-sucedido do armazenamento a fila é removida. Mesmo uma leitura originalmente sem persistência passa a exigir gravação quando carregar concessões pendentes.
- O processamento antes da liquidação mantém a ordem: registrar XP e então confirmar o resultado de extração/perda. Incremento de revisão nas concessões novas impede que um `Save` com uma cópia anterior sobrescreva esse XP sem conflito de revisão. Não houve alteração dos multiplicadores ou do valor das recompensas.
- `Assets/Scripts/Camp/CampExperience.cs`: extração do corpo de PvP para uso dentro da mesma transação. Mantidos limites, cooldown entre pares e chaves do perfil. O caminho da fila usa a credencial capturada já conferida sob mutex; a API direta continua verificando a posse local normalmente.
- `Assets/Scripts/UI/NetworkBootstrap.cs`: tenta processar pendências a cada dois segundos enquanto o servidor está ativo. Uma geração de publicação separa commit e atualização dos jogadores. Se a atualização visual falhar, ela é tentada novamente sem reconstruir a concessão. Outras transações podem processar a fila antes desse intervalo.

## Deduplicação, posse e limites

A nova tentativa usa a mesma chave de recompensa gravada no perfil; não cria outro evento nem outro rateio. Lotes de participantes são processados atomicamente. Sucesso da gravação e falha posterior de atualização visual são estados distintos: o evento sai da fila após commit, e somente a publicação é repetida. Limites de exploração/PvP, recuperação de experiência e cooldown existentes foram mantidos.

A credencial capturada pode sobreviver ao descarte do cache da conexão local, mas deve continuar coincidindo com proprietário/token/validade no registro, dentro da transação. Posse expirada ou substituída não autoriza gravar por uma sala antiga. Eventos com posse ou expedição original inválida são encerrados com aviso após uma transação confirmada, sem serem aplicados à nova expedição. Se um participante de um lote perder essa autorização, o lote não é parcialmente redistribuído.

**A fila é em memória do processo, não um journal durável.** Sobrevive ao despawn e a falhas transitórias durante a sessão; não garante recuperação após crash/reinício enquanto o armazenamento estiver indisponível. `Close` faz uma tentativa final e registra erro explícito se ainda houver eventos não persistidos antes de limpar o contexto do repositório. Uma falha longa que atravesse a expiração/perda da posse também não concede licença para premiar depois pela credencial antiga. Não foi introduzido serviço ou formato persistente adicional.

Nenhum save real foi aberto/alterado e nenhuma versão de rede/save foi modificada nesta etapa. APIs diretas de concessão permanecem disponíveis; os produtores de gameplay encontrados (morte, objetivo, exploração) foram encaminhados para a fila. Provas antigas que chamam APIs diretas não demonstram recuperação da fila.

## Revisão e testes pendentes

Revisão estática de morte/despawn, rateio, objetivos, exploração, credenciais, transações compartilhadas/locais, liquidação da expedição, conflito de revisão, encerramento e publicação. Não foram executados testes nem tratados logs históricos como validação.

Na T8: injetar falha antes do commit, despawnar a entidade e recuperar uma única recompensa com rateio original; repetir após commit e antes da publicação, verificando ausência de duplicação. Conferir objetivo concluído e região abandonada durante a falha; fila seguida de extração/perda/desconexão, incluindo PvP antes de a vítima ser liquidada. Conferir cooldown/caps, token substituído/expirado, lote com participante inválido e `Save` antigo. Conferir recuperação da publicação com evento já removido e erro explícito no encerramento com armazenamento ainda indisponível. Persistência através de crash não faz parte do aceite implementado.
