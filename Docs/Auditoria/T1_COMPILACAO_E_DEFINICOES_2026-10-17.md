# T1 — Compilação e definições

Data: 2026-10-17. Resultado: **concluída**, no escopo abaixo.

## Execução

Unity 6000.0.84f1, projeto atual, Editor em batchmode com `-nographics`, sem entrar em Play. Uma execução de `PostAuditT1Checks.Run`, com saída 0 e marcador `POST_AUDIT_T1_OK`. A compilação executou os pós-processadores do Netcode e terminou sem erros ou avisos C# no log desta execução. Não foi necessária correção de código de produção.

Foi adicionada uma rotina específica em `Assets/Editor/PostAuditT1Checks.cs`, acionada somente por comando explícito. Não chama os verificadores antigos, reconstrução de cena, persistência ou pipeline de build. O estado do gerador aleatório é restaurado após a conferência.

## Evidências obtidas

- As 18 definições nativas de equipamento carregaram por `Resources.LoadAll`, com IDs únicos e correspondência ao catálogo. Cada asset apresentou dois atributos secundários na serialização nativa.
- Foram geradas 54 combinações: 18 modelos nas três raridades. Todas passaram por `ValidInstance` e pela aceitação do slot correspondente.
- Handlers gerados pelo NGO encontrados nos assemblies carregados: LootPickup 10; Monster 1; PlayerInventory 12; PlayerShooter 2; PlayerController 1.
- ItemStack e ItemInstance implementam INetworkSerializable; o módulo contextual de UI carrega como subclasse do módulo do Input System.

Evidências locais: [resultado estruturado](../../Logs/pos-auditoria/T1-2026-10-17/t1-result.json), [log do Editor](../../Logs/pos-auditoria/T1-2026-10-17/editor-attempt1.log) e [código de saída](../../Logs/pos-auditoria/T1-2026-10-17/editor-exit.txt).

## Limites

Esta etapa confirma compilação, geração de handlers, metadados e carregamento de assets. Não demonstra entrega de RPC, round-trip de serialização, comportamento em rede, interface em uso, combate, renderização, desempenho ou Android. Não houve partida, servidor/cliente separado, build ou repetição da auditoria antiga. As combinações de equipamentos são uma amostra por modelo/raridade, não uma prova de todas as rolagens aleatórias.

O log contém um aviso inicial de token de licença indisponível, seguido da resolução e atualização bem-sucedidas da licença. O dispositivo gráfico nulo também informa falta de suporte a um shader; esta execução não verifica gráficos.

## Próxima etapa

**T2 — Posse de sessão e desconexão antiga.** Conferir se a sala anterior consegue alterar a nova expedição. Executar separadamente; caso exija dois servidores, manter esse cenário em uma continuação exclusiva.
