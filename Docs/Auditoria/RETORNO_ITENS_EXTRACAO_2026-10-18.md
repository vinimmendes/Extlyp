# Retorno de itens da expedição — investigação de 2026-10-18

## Resultado e limite

Revisão estática do código atual: não foi identificada perda ou duplicação no caminho normal de extração com vida. Isso não confirma uma partida real nem o executável atualmente em uso. Não foram iniciados testes, builds ou sessões Unity; nenhum save foi alterado.

## Caminho normal

1. `MatchManager.ServerParticipantExtracted` exige sucesso de `PlayerInventory.ServerBankExtraction` antes de finalizar a extração do jogador.
2. `CampRepository.Bank` exige a expedição correta e confirmada (`ExpeditionState == 2`), copia os 12 slots da mochila e o equipamento completo para o perfil. O baú seguro existente permanece separado: o loot volta à **mochila**, sem depósito automático no baú.
3. `RecoveryRules.Extracted` remove apenas as marcações de recuperação. A identidade, raridade, atributos e os dados do item, incluindo munição carregada, permanecem na cópia.
4. O banco remove da mochila apenas itens marcados como exclusivos da expedição: `DungeonCrank` e `DungeonKey`, conforme `ItemCatalog.ExpeditionOnly`.
5. O repositório encerra a reserva anterior e incrementa a revisão. Em `SharedCampMemory.Transaction`, a escrita durável precede a publicação na memória compartilhada; falhas anteriores à conclusão não retornam sucesso.
6. Após o sucesso do banco, `extractionBanked` e `progressionFinished` impedem que morte, desconexão ou nova chamada de extração repitam os efeitos ou retirem os itens já confirmados. A publicação de estado/progresso/perfil tem retentativa.
7. O botão de retorno encerra a conexão e recarrega a cena. Com o ambiente ainda selecionado, o bootstrap reconecta ao acampamento; `CampNetwork` envia o perfil do servidor. O cliente verifica estrutura, versão e revisão do snapshot.
8. `Start-Server.ps1` fornece o mesmo cluster às salas, permitindo que o acampamento leia o perfil confirmado em outra sala.

## Ambiente de testes: comportamento diferente

- `MatchManager.Sandbox` corresponde a `QuickTest`.
- Nesse modo, `ServerBeginExpedition` não reserva a mochila normal do perfil: usa equipamento inicial de teste.
- `ServerBankExtraction` encerra a extração de teste **sem chamar `CampRepository.Bank`**. O loot dessa partida não retorna ao perfil normal.
- `CampNetwork.Profile` cria um acampamento temporário para a conexão de teste. Ao remover a conexão, esse perfil temporário sai da memória; o comerciante possui tratamento próprio.
- A interface uGUI atualmente apresenta “Extração concluída” tanto no normal quanto no teste (`UguiGameHud.Menus.RefreshResults`). A versão anterior de HUD distingue o teste. Há ambiguidade de apresentação, sem evidência de falha do banco normal.

## Evidências disponíveis

Relatórios de 2026-10-17 (`T3A_EXTRACAO_E_PUBLICACAO_2026-10-17.md` e `T3C2_RECONEXAO_APOS_EXTRACAO_2026-10-17.md`) registram provas anteriores com material, equipamento raro, preservação da identidade, reconexão e releitura do disco. São evidências históricas, não validação do código ou build atual.

Na inspeção de hoje, `Logs/server.log`, `server-room-2.log` e `server-room-3.log` tinham tamanho zero. A busca delimitada no Editor.log não encontrou mensagens das falhas de publicação da extração. Não há rastreio suficiente nesses logs para afirmar quais itens retornaram em uma partida específica.

## Conferência prática que falta

No ambiente normal, comparar a mochila e os equipamentos imediatamente antes de uma extração com vida com os mesmos campos após retornar à base e reconectar. Usar itens com quantidades conhecidas e um equipamento identificável; verificar o baú separadamente. Essa etapa ainda não foi executada nesta investigação.
