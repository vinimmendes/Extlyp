# Auditoria completa — etapas e acompanhamento

Data: 16/10/2026. Escopo: projeto inteiro, com revisão funcional/técnica e oportunidades de redução. Correções de gameplay e exclusões são trabalhos posteriores, não parte da auditoria.

## Etapas

| Etapa | Trabalho | Entrega | Situação |
| --- | --- | --- | --- |
| 1 | Preparar e mapear o projeto | Versão registrada, inventário, histórico e cópia isolada | Concluída |
| 2 | Revisar dados, segurança e economia | Achados sobre rede, identidade, autoridade, inventário, equipamento, saves, loot, comércio e XP | Concluída — revisão estática |
| 3 | Revisar gameplay e interface | Achados sobre combate, movimento, animação, IA, mapas, visão, luz, controles e menus | Concluída — revisão estática |
| 4 | Identificar possíveis reduções | Candidatos a remover, arquivar, consolidar, atualizar, manter ou investigar | Concluída — levantamento estático |
| 5 | Executar provas fora da Unity | Provas C# agrupadas, usando as regras reais e adaptadores revisados | Concluída — 66 PASS / 6 FAIL |
| 6 | Validar Unity e jogo integrado | Importação/compilação/build e campanha com servidor/dois clientes; repetições necessárias registradas | Executada — Windows, cobertura e limitações registradas |
| 7 | Consolidar resultados | Relatório, matriz de cobertura e fila de correções/limpeza por prioridade e dependência | Reexecutada e concluída — revisão documental 2, pendências preservadas |

## Base da etapa 1

Execução `2026-10-16_083542_382c7a`, a partir de `main` / `a80a701a471b7004ce37db864b346505f56ba429`, incluindo o conteúdo local modificado/não rastreado. Unity 6000.0.84f1; protocolo 30; perfil 4; documento compartilhado 6.

- [Relatório de preparação e mapeamento](ETAPA_1_PREPARACAO_2026-10-16.md).
- Cópia independente: `Builds/Auditoria/2026-10-16_083542_382c7a/Projeto`.
- Evidências locais: `Logs/auditoria/2026-10-16_083542_382c7a`.
- 5.047 arquivos, 1,513 GiB; hashes individuais e hash agregado no manifesto. Os diretórios de cópia/evidências são ignorados pelo Git: este documento não os substitui como backup.
- 178 scripts de runtime, 25 scripts de editor e 27 achados históricos a reavaliar.

## Resultado da etapa 2

- [Relatório de dados, segurança e economia](ETAPA_2_DADOS_SEGURANCA_ECONOMIA_2026-10-16.md).
- 7 achados: 2 P1 dependentes de cenários de falha e 5 P2. Evidência estática; nenhuma exploração, perda ou duplicação reproduzida nesta etapa.
- 46 arquivos conferidos por hash contra o manifesto e o projeto atual, todos correspondentes. Evidências em `Logs/auditoria/2026-10-16_083542_382c7a/etapa2`.
- Reavaliados A01–A07 sem reutilizar testes antigos como validação. Casos e critérios de aceite preparados para os lotes das etapas 5 e 6.
- Nenhuma correção, exclusão, compilação, teste, abertura da Unity ou alteração de save. Próxima execução: etapa 3, gameplay e interface.

## Resultado da etapa 3

- [Relatório de gameplay e interface](ETAPA_3_GAMEPLAY_INTERFACE_2026-10-16.md).
- 6 achados: 4 P2 e 2 P3, com evidência estática e cenários de confirmação. Nenhum teste em partida foi realizado.
- 66 arquivos conferidos por hash contra o manifesto e o projeto atual, todos correspondentes. Evidências em `Logs/auditoria/2026-10-16_083542_382c7a/etapa3`.
- Reavaliados A08–A19 e A26, sem encerrar achados por logs antigos. Relatos de sombras, câmera, caminhada, minimapa e texto contextual mantêm validação visual pendente.
- Nenhuma correção ou exclusão. Próxima execução: etapa 4, possíveis reduções e conteúdo obsoleto, sem remover arquivos.

## Resultado da etapa 4

- [Relatório de reduções e conteúdo obsoleto](ETAPA_4_REDUCOES_2026-10-16.md).
- 14 decisões com dependências, confiança, benefício e verificação antes de alteração. Nenhuma exclusão ou migração executada.
- 5.047 arquivos do manifesto comparados por hash ao projeto atual. A conferência final detectou uma alteração externa em `Monster.cs`, preservada e registrada sem modificar a base congelada. Encontrados 194 grupos idênticos de pelo menos 4 KiB, em grande parte cópias deliberadas de produção/importação.
- Medições separadas de fontes, disco local e potenciais impactos de build/memória. ServerValidation foi recriada e ocupa 4,24 GiB; limpeza é condicional e não foi autorizada por este levantamento.
- Coletor estático em `Tools/Audit/Collect-ReductionInventory.py`; evidências em `Logs/auditoria/2026-10-16_083542_382c7a/etapa4`. Nenhum teste, Unity ou build iniciado.
- Próxima execução: etapa 5, adaptar e executar provas C# agrupadas fora da Unity, sem aplicar as reduções durante a auditoria.

## Resultado da etapa 5

- [Relatório das provas C#](ETAPA_5_PROVAS_CSHARP_2026-10-16.md).
- 72 casos únicos: 66 PASS, 6 FAIL e zero ERROR no consolidado. Falhas ligadas a E2-01 (sequência de desconexão/posse), E2-05 (quatro validações) e E2-03 (backup com adaptador JSON).
- 28 fontes reais congelados compilados com .NET 6.0.21, mais driver/adaptadores explícitos; 35 fixtures de autoria com hashes conferidos. IO/mutex/buffers Windows executados com clusters e dados próprios.
- Falha de preparação do leitor YAML e uma fixture de atomicidade foram diagnosticadas. O caso corrigido foi repetido isoladamente; seis provas adicionais de conservação passaram. Logs originais preservados.
- Evidências em `Logs/auditoria/2026-10-16_083542_382c7a/etapa5`; verificadores próprios em Tools/Audit. Nenhuma correção no jogo, exclusão, sessão Unity, build de Player ou servidor iniciado por esta etapa.
- Próxima execução: etapa 6, revisar diferenças da cópia, validar Unity e campanha integrada com processos/builds reutilizados. Falhas de regras não foram marcadas como corrigidas.

## Resultado da etapa 6

- [Relatório de Unity e jogo integrado](ETAPA_6_VALIDACAO_INTEGRADA_2026-10-16.md).
- Build final Windows com zero erros/avisos; campanha com servidor, dois clientes, dois humanos e seis bots.
- Provas nativas/rede confirmaram validações ausentes, sigilo de baú e conflito de mira; backup reavaliado com JsonUtility e mensagem medida com FastBufferWriter reais.
- Bloqueios de compilação/serialização da base, já corrigidos externamente, incorporados somente à cópia; texto global de objetivo ainda distante registrado. Nenhuma correção de gameplay no projeto de trabalho.
- Dois builds bem-sucedidos preservados após repetições justificadas. Capturas válidas com servidor e um cliente visível; 14 asserções sobre observações satisfeitas, incluindo reprodução de bugs, sem aprovação geral do jogo.
- Fault injection, dois servidores, vários cenários de IA/UI, qualidade visual completa e Android continuam pendentes na matriz. Conferência final: nenhum processo do executável de auditoria remanescente. Próxima etapa: 7, consolidação.

## Resultado da etapa 7

- Reexecutada a pedido do usuário, como revisão documental 2. A etapa 6 já executada foi preservada e não foi repetida; nenhum novo teste/build foi iniciado.
- Classificação conferida: seis registros com comportamento observado no escopo indicado, um com incompatibilidade medida, sete estáticos/sem cenário específico completo e dois com correções externas exercitadas na cópia. Dos 16 registros históricos, 14 permanecem a reavaliar/corrigir e dois exigem reconhecimento da correção/integração atual.
- [Consolidação e próximos trabalhos](ETAPA_7_CONSOLIDACAO_2026-10-16.md): 16 registros históricos (quatro P1, dez P2, dois P3), matriz de evidências/limites e 14 decisões de redução. Esses números não representam 16 falhas confirmadas ativas na revisão atual.
- Correções externas de compilação/serialização separadas dos problemas ainda abertos. Fila priorizada com blocos pequenos e critérios de aceite, sem reaplicar mudanças já presentes.
- Nenhum teste, build, Unity, exclusão ou correção de gameplay nesta consolidação. Auditoria entregue com pendências explícitas, sem homologação geral.
- Próximo bloco proposto: conferir apenas E2-01 no código atual. A pedido do usuário, próximas execuções delimitam um achado/mudança; implementação, build, rede e verificação visual ficam em blocos separados.

## Regras para continuidade

Roteiro vigente após a auditoria: [correções em etapas pequenas](PLANO_CORRECOES_POS_AUDITORIA_2026-10-16.md). Por solicitação do usuário, etapas C1–C19 sem testes; verificações somente na fase T, após as correções. Próxima execução: C1. As indicações de “próxima execução” nos resultados anteriores são históricas.

1. Auditar a base registrada, sem confundir alterações posteriores com o conteúdo capturado. Registrar qualquer nova revisão e o impacto nas evidências.
2. Revisão estática não confirma execução. Classificar resultados como reproduzido, estático, hipótese ou não verificado.
3. Não executar testes nas etapas 1–4. As provas agrupadas entram na etapa 5; Unity, builds e campanha integrada entram na etapa 6, conforme o planejamento solicitado pelo usuário.
4. Revisar verificadores antigos antes de usá-los. Não executar `MvpSetup.Build` ou reconstruções de assets para preparar testes.
5. Nos testes, usar somente cópia, cluster, identidades, armazenamento e portas próprios. O contexto é preparatório: nenhum servidor, identidade ou save de teste foi iniciado na etapa 1.
6. Na etapa 6, conferir porta livre e configuração de conexão da cópia para loopback. Não lançar os scripts copiados com parâmetros padrão antes de revisar seus destinos; `-cluster-id`, `-camp-data` e `-identity-file` devem apontar para os dados isolados da execução.
7. Reaproveitar um par de builds Windows e os processos ao longo dos cenários. Repetir apenas falhas e dependências afetadas; Android real fica pendente se não houver aparelho.
8. Para reduções, verificar referências por GUID/nome, carregamento dinâmico, dependências de ferramentas e reprodução de arte. Separar redução de repositório, disco, build e memória. Não excluir nada durante a auditoria.
9. Não reutilizar logs antigos como validação atual nem alterar retroativamente os registros históricos. Os achados de 09/10 e a limpeza de 12/10 serão reavaliados.

## Critério final

Todas as áreas devem ter resultado ou limitação explícita. Cada achado deve indicar evidência, impacto, causa conhecida ou investigação necessária e critério de aceite. Cada candidato de redução deve indicar caminho, dependências, confiança, benefício medido/estimado e verificação antes de uma eventual remoção.
