# T2a — Posse e desconexão no repositório

Data: 2026-10-17. **T2a concluída; T2 ainda parcial.** A continuação T2b fica em uma solicitação separada para manter as rodadas pequenas.

## Execução e resultado

Executada `PostAuditT2Checks.Run` na Unity 6000.0.84f1, Editor em batchmode, sem Play, builds ou servidores. A rotina exige repositório inicialmente vazio e sem armazenamento compartilhado; usa somente memória e uma identidade aleatória. Limpa o estado em finally e restaura o gerador aleatório. Não abre nem modifica saves reais.

A segunda execução terminou com código 0, `Success: true`, marcador `POST_AUDIT_T2A_OK` e 15 registros de conferência. Sem erros/avisos C# nessa execução. Não foi necessária alteração de código de produção.

Casos aprovados pelo código C# real do repositório:

- Escrita antes da aquisição recusada; proprietário ativo escreve; outro proprietário não toma posse antes do prazo.
- Renovação da própria aquisição preserva token; prazo vencido impede escrita e renovação.
- Troca A → B após vencimento gera novo token. Duas chamadas atrasadas de `EndConnection` de A não mudam o registro da expedição nova nem apagam a credencial de B. Edição com proprietário esperado A também é recusada.
- Com a credencial do registro substituída para representar outra sala, Edit, Save, Begin, Commit, Rollback, Bank, Lose e concessão direta de XP recusam o token local antigo sem mudar o JSON completo do registro. Refresh e EndConnection antigos também são recusados.
- Nova aquisição com o mesmo nome gera outro token; Save com revisão obsoleta é recusado.
- Desconexão do proprietário atual libera dono, token, prazo e credencial local; callback repetido não encerra novamente.

## Ajuste do verificador

A primeira execução passou pelos 14 primeiros registros, mas falhou na última asserção porque exigia `Lease == null` após uma segunda transação de serialização. O verificador foi ajustado para `string.IsNullOrEmpty`, coerente com as regras existentes, exigindo também token vazio, prazo zero e ausência da credencial local. A repetição passou. O primeiro resultado e log foram preservados; não se alterou o jogo para fazer o teste passar.

## Evidências

- Resultado: `Logs/pos-auditoria/T2-2026-10-17/t2a-result.json`.
- Log aprovado: `Logs/pos-auditoria/T2-2026-10-17/editor-attempt2.log`.
- Código de saída: `Logs/pos-auditoria/T2-2026-10-17/editor-exit.txt`.
- Tentativa inicial: `t2a-attempt1-result.json` e `editor-attempt1.log` na mesma pasta.
- Rotina: `Assets/Editor/PostAuditT2Checks.cs`.

## Limites e continuação T2b

A troca de proprietário remoto foi uma fixture por reflexão no registro em memória, preservando o token local antigo; o vencimento também foi provocado na fixture. Isso exercita as regras reais, mas não comprova concorrência, mutex, persistência compartilhada ou passagem de tempo real. A chamada de EndConnection é direta, sem callback NGO.

**Próxima: T2b — integração entre salas e desconexão.** Conferir dois processos/salas com armazenamento isolado compartilhado, retomada após reinício, callback do NetworkBootstrap, renovação recusada, falha do armazenamento e limpeza de despawn/registros locais. Conferir também rejeição atômica de lote de XP com participante sem posse e descarte de recompensa pendente com credencial substituída. Preparar uma continuação delimitada se a integração exigir mais de uma rodada; não avançar para T3 tratando T2 como aprovada integralmente.
