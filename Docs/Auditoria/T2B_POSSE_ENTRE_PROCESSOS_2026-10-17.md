# T2b — Posse entre processos e retomada

Data: 2026-10-17. **T2b concluída no armazenamento compartilhado; T2 ainda parcial.** Próxima rodada: T2c, callbacks de rede e limpeza.

## Resultado

27 registros de conferência aprovados: 17 no processo A, 5 no B e 5 em um processo novo de retomada. Todos produziram `Success: true`; o orquestrador terminou com código 0. Não foi necessária mudança no código de produção.

Foi executado o código atual do jogo na Unity 6000.0.84f1, com JsonUtility nativo, memória mapeada, mutex e arquivos reais. A e B rodaram em processos simultâneos com caches de credenciais separados. O processo de retomada começou somente após ambos encerrarem. Não houve substituição das APIs de armazenamento por adaptadores.

A cópia isolada contém os 184 arquivos C# de runtime, definições de equipamentos e Gameplay; os hashes das 184 fontes conferiram com os originais. Essa pasta é um projeto de teste, não um build de jogador. Não foram iniciados Play, transporte NGO, servidores de partida ou clientes.

## Comportamentos conferidos

- B não assume enquanto a posse de A está válida.
- Após vencer a posse, B assume e recupera a expedição antiga antes de iniciar a nova.
- A mantém a credencial antiga em seu processo. Edit, Save, Begin, Commit, Rollback, Bank, Lose e concessão direta de XP são recusados.
- Refresh e duas chamadas atrasadas de EndConnection de A são recusados. O registro completo de B permanece igual antes/depois dessas operações e B ainda consegue renovar a própria posse.
- XP enfileirado por A antes da transferência é descartado depois dela, sem alcançar a expedição de B. O aviso de recompensa encerrada sem concessão no log A é esperado neste caso.
- Um lote de XP com um participante autorizado e outro sem posse é recusado sem crédito parcial ao participante autorizado.
- Depois de fechar ambos os processos, um terceiro recupera a expedição confirmada do disco, limpa as credenciais transitórias, adquire novo token e consegue encerrar a conexão.

O vencimento é provocado somente na fixture, alterando LeaseExpires sob a transação/mutex real. Esta prova não mede o temporizador de renovação do NetworkBootstrap nem espera 90 segundos em tempo real.

## Preparo e tentativas preservadas

A primeira tentativa adquiriu a sessão antes de o segundo projeto terminar sua importação inicial. Quando B ficou pronto, o prazo de 90 segundos já havia vencido e a aquisição foi legitimamente permitida. Foi acrescentada uma barreira de prontidão antes de A adquirir a sessão.

Na reutilização, o comando de cópia de diretório gerou pastas aninhadas duplicadas na cópia de teste. A execução foi interrompida, as duplicatas foram preservadas fora de Assets e a cópia passou a ser feita arquivo por arquivo. A tentativa final passou. Nenhuma dessas correções alterou regras do jogo.

A importação inicial também acionou o API Updater da Unity para referências de um pacote no cache do projeto isolado; a compilação seguinte passou. Não se atualizou a engine ou o manifesto de pacotes do projeto principal.

## Evidências e ferramentas

Execução aprovada: `Logs/pos-auditoria/T2Shared/20261017-124258-bf05c8/`:

- `run-summary.json`: papéis, processos, contagem e resultado.
- `A-result.json`, `B-result.json`, `Restart-result.json`: asserções executadas.
- `A-editor.log`, `B-editor.log`, `Restart-editor.log`: logs atuais.
- `source-hashes.json`: fontes usadas.
- `store/`: checkpoints exclusivos da prova, com nome de cluster aleatório.

Tentativas de preparo: pastas `20261017-123552-50c3a1` e `20261017-124019-deef98` sob o mesmo diretório T2Shared. Projeto isolado reutilizável: `Builds/PosAuditoria/T2Shared/20261017-123552-50c3a1`.

Ferramentas adicionadas: `Assets/Editor/PostAuditT2SharedChecks.cs` e `Tools/Audit/Run-PostAuditT2Shared.ps1`. A rotina exige Editor em batchmode e repositório vazio, usa diretório explícito e cluster exclusivo, fecha o repositório ao terminar e registra falhas. Saves reais não foram abertos. Os callbacks são dirigidos por sinais de arquivo para ordenar o cenário, não por pacotes de rede.

## Limites e próxima T2c

Ainda não estão validados: callbacks reais de aprovação/desconexão no NetworkBootstrap, renovação recusada durante Update, falha de armazenamento nesse fluxo, despawn de PlayerInventory, ausência de drops indevidos e limpeza de requests/slots/atividades após falha. Tampouco houve teste de carga, estresse de corrida ou interrupção abrupta durante escrita; a retomada aqui foi após fechamento dos processos.

**T2c — callbacks de rede e limpeza após perda de posse**, em rodada separada. T2 permanece parcial até essa integração; T3 ainda não foi iniciada.
