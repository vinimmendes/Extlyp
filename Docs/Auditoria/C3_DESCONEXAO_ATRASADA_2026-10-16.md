# C3 — desconexão atrasada e perda de posse

Situação: **implementada e revisada estaticamente; não testada**. Este registro fecha a documentação da C3 antes da execução seguinte, C4. Nenhum teste, Unity, compilação ou build iniciado.

## Causa e mudança

O callback Disconnected fazia Get do perfil e usava o ID da expedição encontrado naquele momento para chamar Lose/Rollback. Essa leitura podia ser da conexão nova. Exceções também interrompiam a remoção de requests, slots e atividades. O retorno de Refresh era ignorado.

Alterações feitas na C3:

- **CampProgress.cs:** EndConnection confere proprietário esperado, token e validade, encerra apenas a expedição da posse válida e libera a lease na mesma transação. Não faz Get separado para descobrir qual expedição encerrar. Credencial local é removida em finally somente se pertencer ao proprietário que saiu, sem apagar a de outra conexão local. Foi acrescentada CampLeaseLostException e suporte a proprietário esperado nas operações usadas pelo acampamento.
- **NetworkBootstrap.cs:** Disconnected usa EndConnection e garante limpeza de atividades, vagas, prontidão e requests em finally. A renovação trabalha sobre cópia dos IDs, trata recusas/erros e desconecta a sessão comprometida sem interromper a renovação dos outros clientes. Conexão tardia sem request é recusada.
- **CampNetwork.cs:** leitura, edição e salvamento do acampamento usam o proprietário específico do cliente. Perda de posse interrompe sincronização/atividade; erros de sincronização são tratados sem chamar Sync recursivamente no catch. Remove cancela a atividade antes de retirar seus registros; Tick tolera remoção de outro cliente durante a iteração.
- **PlayerInventory.cs:** OnNetworkDespawn garante remoção da lista estática e dos eventos em finally; erro por perda de posse não gera drops do inventário antigo. Outros erros são registrados sem abandonar essa limpeza.

A leitura do código local do NGO 2.13.3 mostrou que o despawn do PlayerObject precede o callback de desconexão normal. Por isso, ao desconectar por perda de posse/indisponibilidade, o bootstrap marca o participante como TimeUp antes de solicitar a desconexão, impedindo drops de inventário cuja autorização já não é confiável. Na desconexão normal, a ordem de drops existentes permanece anterior ao encerramento da lease.

## Limites

Falhas do armazenamento e de DisconnectClient são registradas. A limpeza lógica tem fallback; isso não equivale a comprovar em execução o encerramento do transporte em todas as falhas. Não misturar salas antigas sem a proteção de posse com as novas no mesmo cluster.

E2-01 fica implementado nos escopos C2/C3, **sem aceite em runtime**. T2 deverá cobrir duas salas, proprietário antigo no mesmo processo, callback repetido/tardio, renovação recusada, falha de armazenamento e despawn, sem alterar a nova expedição nem deixar registros locais órfãos. Não usar os testes antigos como validação dessas mudanças.

Os quatro arquivos acima já tinham alterações locais, preservadas. Engine, pacotes, protocolo de RPC e saves reais não foram modificados nesta etapa.
