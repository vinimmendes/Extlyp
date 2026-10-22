# Etapa 5 — provas C# agrupadas fora da Unity

Data: 16/10/2026. Situação: **execução concluída**, com falhas de regras registradas e limitações explícitas. Não foram aplicadas correções no jogo, reduções, migrações de dados reais ou exclusões.

## Resultado

**72 casos únicos: 66 PASS, 6 FAIL e 0 ERROR no consolidado.** As seis falhas representam **três achados**, não seis defeitos independentes. Nenhuma falha conhecida foi transformada em PASS apenas por reproduzir o problema: o teste continua exigindo o comportamento seguro desejado.

| Achado | Casos com FAIL | Resultado observado |
| --- | ---: | --- |
| E2-01 — posse de sessão / desconexão antiga | 1 | A sequência de chamadas ao repositório da desconexão antiga encerrou a expedição ativa do novo owner |
| E2-05 — validação de perfil | 4 | Foram aceitos grid de um slot, pilha com 999 metais, pistola com 999 tiros carregados e ItemId 255 |
| E2-03 — recuperação de backup | 1 | Documento principal com Records nulo impediu tentar um backup válido; teste usa adaptador JSON, exigindo confirmação do parser Unity |

Os casos aprovados demonstram regras no escopo executado, sem comprovar Unity, física, serialização nativa, IL/RPCs, transporte, interface ou sensação de jogo. **O jogo não está liberado nem integralmente validado por esta etapa.**

## Base e isolamento

- Fontes congelados de `Builds/Auditoria/2026-10-16_083542_382c7a/Projeto`, base da etapa 1, com hash agregado `fcdf78077dca71119a05d701dad579149239ccedd107a3d16fe11c0ca32241cd`.
- **28 scripts C# reais** compilados diretamente da cópia, sem copiar regras para a implementação do teste. Dois arquivos adicionais são driver/adaptadores. Hashes dos fontes usados conferidos contra o manifesto da etapa 1.
- **35 fixtures numéricas de autoria** extraídas dos YAMLs congelados: 18 definições de equipamento, 6 tesouros, 10 tabelas de loot e Gameplay. Seus hashes também foram conferidos contra o manifesto nos lotes finais. Não foram carregados assets nativos, ícones, materiais ou GUIDs na engine.
- Compiler Roslyn e runtime .NET **6.0.21** já instalados com Unity **6000.0.84f1**; apenas `dotnet.exe`/`csc.dll` foram usados. Nenhum processo Unity, build de Player, servidor ou cliente foi iniciado por esta etapa.
- Dados de teste e saídas somente sob `Logs/auditoria/2026-10-16_083542_382c7a/etapa5`. Armazenamento compartilhado utilizou clusters novos `audit5_<GUID>` e pasta `isolated-data` própria de cada execução. Não usou cluster, identidade ou save de produção.
- Login gerou chave de teste em memória; o adaptador de PlayerPrefs é um dicionário do processo. Nenhuma chave existente do aparelho foi lida ou modificada, e a chave privada não foi registrada nos resultados.
- Os 28 fontes testados ainda correspondiam aos respectivos fontes atuais na conferência final. A alteração externa de `Monster.cs` registrada na etapa 4 permanece fora deste conjunto; a etapa 6 deverá registrar a revisão que efetivamente compilará o jogo completo.

Skills usadas: investigação sistemática, C# e dados/configuração por ScriptableObjects. A autorização para executar este lote vem da etapa 5 do plano; a preferência de não iniciar Unity/builds continua respeitada.

## Lotes executados

| Grupo | PASS | FAIL | Cobertura efetiva |
| --- | ---: | ---: | --- |
| Inventário/equipamento | 10 | 0 | Capacidade, remoção negativa, fusão parcial, GUID, exemplar único, transferência carregada, slots e duplicidade entre perfis |
| Comércio | 7 | 0 | Preço do servidor, arma comprada vazia, mochila cheia, oferta inválida, exemplar esperado, venda, overflow de moedas e redistribuição |
| Recuperação | 6 | 0 | Kit condicionado à ausência de arma, não repetição, vínculo, venda/descarte, morte e unload misto/atômico |
| Progressão/atributos | 5 | 0 | Limiares e teto de nível/XP, orçamento de pontos, inteligência reservada, valores efetivos, visão 80°–110° e dispersão da espingarda |
| Ciclo de expedição | 2 | 0 | Reserva/rollback com identidade e buff; banco com conservação e repetição recusada |
| XP | 6 | 0 | Deduplicação, 120% na extração, 50% na perda, teto de exploração, cooldown PvP bidirecional, teto PvP e ID de expedição |
| Identidade | 4 | 0 | Assinatura válida/adulterada, janela de tempo/nonce, payload econômico indevido e replay de login |
| Migração | 4 | 0 | Identidade em arma legada, munição recuperada v3, versão futura e perfil legado com expedição ativa |
| Validação persistente | 0 | 4 | Quatro estados inválidos aceitos: E2-05 |
| Posse de sessão | 1 | 1 | Lease válida bloqueia concorrente; sequência antiga de desconexão falha em preservar nova expedição |
| Loot/configuração | 5 | 0 | Catálogos completos nos dados extraídos, bônus válidos, bundles, guarda de servidor e pesos inválidos |
| Atividades | 7 | 0 | Captura/cozinha forjadas, replay após janela de IDs, tempo de cozinha, prêmio único, overflow, fisgada antecipada e observação de crescimento do chão |
| Armazenamento Windows | 2 | 1 | Reabertura durável e exceção antes de commit preservam estado; fallback semântico falha |
| Fila/política de bot | 1 | 0 | Limites temporais da fila e planejamento/efetivação de extração |
| Conservação adicional | 6 | 0 | Três receitas de munição, crafting cheio, extração normaliza só retorno e banco recusa outra expedição |
| **Total** | **66** | **6** | **72 casos únicos, zero erro de execução no consolidado** |

Dentro dos casos agrupados foram gerados **1.080 exemplares de equipamento** (18 modelos × 3 raridades × 20) e **1.920 bundles** (10 tabelas × 3 riscos × 4 temas × 16). São amostras para invariantes, **não teste estatístico das probabilidades do RNG Unity**.

## Falhas reproduzidas e seus limites

### E2-01 — sequência antiga perde a expedição do novo owner

Caso `lease/stale_disconnect_must_not_lose_new_expedition`:

1. Repositório real concede lease para A.
2. A fixture força apenas o vencimento de `LeaseExpires` por reflexão, sem esperar 90 segundos e sem substituir as regras de Claim.
3. B assume a lease e inicia/confirma nova expedição usando Begin/Commit reais.
4. A prova executa a sequência de chamadas observada em `NetworkBootstrap.Disconnected`: Get do perfil atual, Lose com o ID obtido e Release com owner antigo.
5. A expectativa de a expedição de B continuar ativa falha: o estado foi limpo.

**Classificação:** reproduzido no repositório C# com adaptador de cópia JSON; o callback de rede foi representado pela sequência explícita de suas chamadas. Não houve dois servidores nem desconexão de cliente real. Essa prova confirma a falta de conferência de posse no caminho contábil; a frequência e o disparo pelo NGO permanecem para a etapa 6.

**Prioridade mantida:** P1. Aceite futuro: operação/callback de owner antigo não modifica estado do novo owner, inclusive ao expirar/assumir lease.

### E2-05 — quatro entradas inválidas passam pelo validador real

Casos `validation/wrong_grid_length_must_be_rejected`, `oversized_material_pile_must_be_rejected`, `overloaded_normal_weapon_must_be_rejected` e `unknown_item_id_must_be_rejected` construíram perfis em memória e chamaram `CampProfileMigration.ValidateOwnership` real.

**Resultado:** não houve exceção de rejeição para os quatro perfis inválidos. O driver registrou FAIL para cada expectativa não atendida. A validação de GUID duplicado, em contrapartida, rejeitou o caso correspondente no grupo inventário.

**Classificação:** reproduzido nas regras C# de validação. Não demonstra que um cliente remoto consiga submeter um perfil arbitrário, nem valida como JsonUtility desserializa cada entrada em arquivos reais. O defeito é a ausência das verificações no validador chamado.

**Prioridade mantida:** P2. Aceite futuro: essas entradas são rejeitadas antes de publicar/gravar, preservando documentos inválidos para diagnóstico em vez de converter corrupção em itens válidos.

### E2-03 — formato semanticamente inválido não tenta o backup

Caso `storage/semantic_primary_failure_must_try_valid_backup`:

1. SharedCampMemory real cria um documento válido em pasta/cluster de teste.
2. A prova fecha o handle, preserva documento válido em `.bak` e grava principal com `Version=6` e `Records=null`.
3. Nova instância tenta carregar o principal. A validação lança InvalidOperationException; o backup válido não é carregado.

**Classificação:** reproduzido o fluxo de fallback de SharedCampMemory com IO, mutex e memória compartilhada Windows reais, porém **JsonUtility foi substituído por adaptador System.Text.Json**. O documento usado é JSON válido, mas ainda é necessário confirmar seu comportamento no parser da Unity e a política para versões incompatíveis. Não foi injetada corrupção em save real.

**Prioridade mantida:** P2. Aceite futuro: validar por completo cada candidato de recuperação, sem downgrade silencioso de formato futuro e preservando evidência da falha.

## O que permanece pendente

| Achado/tema | Situação após etapa 5 |
| --- | --- |
| E2-02 — falha depois do banco | Banco idempotente passou nas regras; **não** confirma a fronteira entre persistência, atualização de atributos, Status e drop do participante. Teste de falha na Unity continua necessário. |
| E2-04 — crescimento e snapshot | Ações reais criaram 128 itens no chão; snapshot do adaptador gerou arquivo de **76.474 bytes UTF-8**. É observação de crescimento, não medida do payload NGO ou JsonUtility. Não promover esse PASS de observação a aprovação do limite de transporte. |
| E2-06 — conteúdo de baú fechado | Depende de serialização/observadores NGO; não exercitado. |
| E2-07 — XP após falha de armazenamento | Deduplicação e confirmação passaram; morte/despawn, fila de retentativa e fronteiras de falha não foram exercitados. |
| E3-01/E3-04/E3-05 — controles e foco | Não há dispositivo, EventSystem ou Input System real no executável destas provas. |
| E3-02 — reação do mímico ao melee | Health/Monster/ciclo Unity não foram compilados neste lote; continua evidência estática. |
| E3-03 — aviso versus dano | Não foi criada uma cópia das fórmulas como se fosse prova do jogo; renderização e física continuam pendentes. |
| E3-06 — animação de teia cancelada | Depende de apresentação, snapshots e movimento real; não exercitado. |
| Mapas, spawns e ferramentas | Sem travessia, Physics2D, disputa de mecanismos ou clientes reais; casos espaciais ficam para a campanha integrada. |
| Desempenho e memória | Nenhum frame time, RTT, tráfego, GC em Unity, FPS, APK ou aparelho validado. |
| Reduções R4-01–R4-14 | Nenhuma aplicada; os resultados não autorizam limpeza de arquivos ou mudança de protocolo. |

## Adaptação dos verificadores e rastreabilidade

Foram adicionados, preservando os probes antigos:

- [Run-Stage5Probe.ps1](C:/Users/Vinni/Documents/GitHub/Extlyp/Tools/Audit/Run-Stage5Probe.ps1): compila fontes reais congelados, verifica hashes, cria saída única e executa o lote. Aceita `-Filter` para repetir só casos afetados; o código de saída 2 indica expectativa de regra falhando, 3 indica erro/compilação, 0 indica lote selecionado aprovado.
- [Stage5Probe.cs](C:/Users/Vinni/Documents/GitHub/Extlyp/Tools/Audit/Stage5Probe.cs): casos com comportamento seguro esperado, sem alterar regra de produção para fazer passar.
- [Stage5Adapters.cs](C:/Users/Vinni/Documents/GitHub/Extlyp/Tools/Audit/Stage5Adapters.cs): fronteiras explícitas da engine, JSON, RNG, Resources e tipos de rede.
- [Prepare-Stage5Fixtures.py](C:/Users/Vinni/Documents/GitHub/Extlyp/Tools/Audit/Prepare-Stage5Fixtures.py): leitor limitado dos campos numéricos/string dos assets presentes; recusa formato não previsto e registra os hashes.

Os fontes de driver/adaptadores de cada lote foram preservados em `driver` e conferidos pelos hashes registrados. O driver do primeiro lote foi reconstruído após a correção da fixture e sua identidade **confirmada por SHA-256 contra o hash anterior à compilação**. A versão atual do runner passa a arquivar automaticamente seus próprios fontes antes de executar novos lotes. Assemblies, resposta do compilador e logs das execuções permanecem locais.

### Fronteiras dos adaptadores

| Adaptador | Papel | O que não comprova |
| --- | --- | --- |
| Mathf | Operações usadas pelas regras, incluindo arredondamento para par | Identidade bit a bit com todas as funções/precisões da engine |
| JsonUtility → System.Text.Json | Cópia por campos para transações em memória e armazenamento de fixtures | Semântica nativa de campos ausentes, UnityEngine.Object, ulong, versões ou arquivos de jogadores |
| Resources | Instâncias C# alimentadas por 35 YAMLs extraídos e verificados | Importação, referências por GUID/subassets, ícones, atlas, shaders, layout e configurações por plataforma |
| Random → System.Random | Sequência repetível para amostrar invariantes | Distribuição/sequência específica do Unity.Random e balanceamento estatístico |
| Netcode | Tipos para compilar e indicador IsServer de fixture | Geração IL/RPC, autorização remota, rede, observadores e custo de serialização |
| PlayerPrefs | Dicionário efêmero da identidade de teste | Persistência/proteção da chave no aparelho |
| MapBuilder | Enum/mapa de contexto para regras puras | Geometria, colisão, navegação ou disponibilidade real de saídas |

SharedCampMemory, arquivos, File.Replace, mutex e buffers Windows foram executados de verdade em dados isolados; não foram simulados pelo adaptador. Não houve queda de processo no intervalo entre gravação durável e troca de buffer. Exceção antes do commit não equivale a essa queda.

## Histórico de execução desta etapa

Pastas sob `Logs/auditoria/2026-10-16_083542_382c7a/etapa5`:

| Execução | Resultado | Uso no consolidado |
| --- | --- | --- |
| `20261016-153405-884202` | Preparação interrompida ao ler `SecondaryPool: []` dos tesouros; nenhum C# compilado/executado | Erro de ferramenta preservado; não conta como falha de jogo |
| `20261016-153414-7fa061` | Compilou; 66 casos, 59 PASS / 7 FAIL / 0 ERROR | Lote completo original, incluindo uma falha de preparação de fixture investigada |
| `20261016-153519-bef514` | Compilou; somente atomicidade de unload, 1 PASS | Substitui resultado do mesmo caso após normalizar contadores derivados da fixture |
| `20261016-153524-d28ae2` | Compilou; 6 casos adicionais de conservação, 6 PASS | Acrescenta receitas, crafting cheio e fronteiras de banco |

**Falha da fixture:** o caso de unload comparava JSON completo antes de `Ensure` sincronizar contadores derivados de madeira/pregos/bandagens. O inventário real não foi alterado pela recusa, mas a comparação incluía cache inicial não normalizado. A preparação agora chama `Ensure` antes de capturar o estado. Só esse caso foi repetido; não se mudou produção nem se afrouxou a expectativa de conservação.

`consolidated-results.json` conserva o último resultado por grupo/nome e aponta a execução de origem; arquivos originais não foram sobrescritos. `current-source-comparison.json` registra a comparação dos 28 fontes reais com o projeto atual. Cada execução concluída contém `run.json`, `compile.log`, `compile.rsp`, `source-hashes.json`, `fixtures.json`, `results.json`, `execution.log` e saídas de teste correspondentes. Logs são ignorados pelo Git: preservar estes diretórios como evidência, não tratá-los como temporários descartáveis.

## Continuidade

Próxima etapa: **6 — validação na Unity e campanha integrada**, com revisão explícita da diferença externa de Monster.cs, compilação/importação real e um par de builds Windows reutilizado nos cenários. Usar cluster, portas, armazenamento e identidades próprios. Não lançar scripts operacionais com destinos padrão nem executar MvpSetup.Build.

Priorizar confirmar as fronteiras P1 E2-01/E2-02, serialização real E2-03/E2-05, privacidade de baús, morte/XP, controles e os relatos visuais do usuário. Não confundir concluir a execução da auditoria com corrigir as seis expectativas que continuam falhando.
