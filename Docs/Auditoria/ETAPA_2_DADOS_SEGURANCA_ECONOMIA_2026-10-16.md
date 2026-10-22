# Etapa 2 — dados, segurança e economia

Data: 16/10/2026. Situação: revisão estática concluída; validação em execução pendente das etapas 5 e 6. Nenhuma correção de gameplay foi aplicada.

## Resultado e prioridade

Foram registrados **7 achados: 2 P1 e 5 P2**. P1 indica prioridade para investigar e corrigir integridade; não significa exploração reproduzida. Os dois P1 dependem de falhas ou troca de posse entre salas. Não houve reprodução de perda, duplicação ou invasão nesta etapa.

| ID | Prioridade | Achado | Evidência disponível |
| --- | --- | --- | --- |
| E2-01 | P1 | Sala que perdeu a lease ainda pode alterar o perfil e encerrar a expedição da nova sala | Ausência de verificação de posse observada no código; cenário entre processos pendente |
| E2-02 | P1 | Falha depois de bancar a extração deixa o participante ativo com os mesmos itens | Ordem das operações observada; duplicação depende de falha injetada |
| E2-03 | P2 | Backup não é tentado quando o save principal passa pelo parser, mas falha na validação | Fluxo estático direto |
| E2-04 | P2 | Chão do acampamento cresce sem limite até ultrapassar o snapshot aceito pelo cliente | Limites incompatíveis observados; tamanho de um perfil real pendente |
| E2-05 | P2 | Validador persistente não cobre formato dos inventários e limites de todos os itens | Lacunas estáticas diretas; não demonstra entrada remota de stacks arbitrários |
| E2-06 | P2 | Conteúdo de baús fechados é replicado para observadores | Inicialização e permissão de leitura observadas; inspeção de cliente pendente |
| E2-07 | P2 | Erro ao salvar XP de morte encerra a tentativa sem recuperação | Fluxo de exceção observado; falha transitória pendente de reprodução |

## Base, método e limites

Base congelada: `2026-10-16_083542_382c7a`, incluindo alterações locais da etapa 1, em `Builds/Auditoria/2026-10-16_083542_382c7a/Projeto`. Hash agregado do manifesto: `fcdf78077dca71119a05d701dad579149239ccedd107a3d16fe11c0ca32241cd`.

Foram conferidos hashes SHA-256 de **46 arquivos**: 33 fontes C#, 10 tabelas de loot e 3 arquivos de configuração. Todos correspondem ao manifesto e aos respectivos arquivos atuais do projeto. Isso identifica a versão revisada; não confirma seu funcionamento. As leituras foram direcionadas aos fluxos desta etapa, não uma revisão integral de cada linha dos 178 scripts de runtime.

Evidências adicionais locais: `Logs/auditoria/2026-10-16_083542_382c7a/etapa2/source-hashes.json` e `loot-configuration.txt`. As linhas citadas abaixo pertencem à cópia congelada. A matriz de tabelas registra mapa, origem, presença, quantidades, garantia e raridades; não substitui carregar os ScriptableObjects na Unity.

Versões conferidas em `ProjectVersion.txt`, manifesto e lock: Unity 6000.0.84f1, NGO 2.13.3, Transport 2.7.4. Protocolo 30, perfil 4 e documento compartilhado 6. Para E2-06, também foi consultado o fonte local instalado do NGO, `Library/PackageCache/com.unity.netcode.gameobjects@277a288a1cea/Runtime/NetworkVariable/NetworkVariableBase.cs:220`: `DefaultReadPerm` é `Everyone`. Esse cache é evidência auxiliar fora da cópia congelada.

Skills aplicadas: investigação sistemática, C# Unity e ScriptableObjects. Nenhum teste automatizado, compilação, Unity, build, servidor ou save de jogador foi executado ou alterado. Não foram executadas as provas antigas. Desempenho, criptografia do transporte, Android e consistência sob falhas continuam sem validação prática.

## Cobertura e proteções observadas

| Área | Revisão realizada | Resultado estático / limite |
| --- | --- | --- |
| Identidade e entrada | Assinatura, vínculo entre chave e Session, janela temporal, repetição e aprovação | Login comprova posse de chave RSA de dispositivo; servidor registra o último instante aceito. Não equivale a conta com recuperação de identidade. Não houve teste criptográfico nem auditoria do transporte. |
| Comandos do acampamento | Origem, tamanho, enum, orçamento de entradas, sequência e estado | Comandos limitados a clientes de camp; revisão crescente e estado sem expedição para ações econômicas. Resultados legados de pesca/cozimento enviados como ação não são aceitos diretamente. |
| Pesca e cozinha | Início, entradas, conclusão e gravação pelo servidor | Recompensa deriva da atividade do servidor. Cozinha admite instante declarado dentro de janela limitada; resistência a automação e efeitos de latência ficam para teste. |
| Inventário e equipamento | Adição, transferência, troca, exemplares, slots e origem | Capacidade verificada antes de adicionar; equipamento valida compatibilidade, identidade e bônus. Troca em expedição usa contexto e revalidação. Serialização nativa e RPCs não exercitados. |
| Recuperação gratuita | Reserva, munição carregada, descarregar, venda, chão, morte e extração | Origem acompanha munição, inclusive em arma normal; regras bloqueiam venda/transferência pública de itens vinculados. Morte remove a parte vinculada; extração normaliza apenas o retorno. Precisa de prova de conservação. |
| Expedição e saves | Reserva, commit, rollback, perda, banco, migração e posse entre salas | Há escrow, IDs de expedição e gravação durável; E2-01, E2-02, E2-03 e E2-05 limitam a robustez. |
| Comércio e moedas | Compra, venda, quantidade, preço, descarregar e redistribuir pontos | Oferta e preço calculados no servidor; venda compara o exemplar esperado e protege overflow de moedas. Compra usa cópia da mochila; descarregar separa munição normal/vinculada em cópias antes de publicar. Não foi simulada uma economia de longo prazo. |
| Loot | Sorteio no servidor, registro de tabelas e validações de entradas | Mapa/origem/tema conferidos; máximo de uma arma e um equipamento por bundle; IDs não repetidos no bundle; garantia de equipamento nos valiosos. Não houve amostragem estatística. |
| XP e atributos | Origens de XP, rateio, deduplicação, confirmação, limites e investimento | Nível até 20; investimento limitado aos pontos obtidos; inteligência permanece reservada. XP pendente é limitado, com 120% na extração e 50% na perda; exploração até 40; PvP até 60 e par bloqueado por 24h. E2-07 cobre a falha de gravação. |
| Informação privada | Visibilidade de objetos, listas privadas e baús | Há filtragem de objetos no servidor e dados de inventário do jogador com leitura restrita ao dono. Baú observado ainda divulga conteúdo fechado: E2-06. Não foi inspecionado tráfego real. |

Fontes centrais das proteções: `CampIdentity.cs:41–59`, `CampNetwork.cs:68–115`, `CampModels.cs:130–191`, `CampTrade.cs:60–109`, `LootRoller.cs:12–58`, `LootTables.cs:55–83`, `CampExperience.cs:20–67`, `CharacterData.cs:58–90`, `PlayerEquipment.cs`, `RecoveryRules.cs` e `ServerVisibility.cs:31–63`. Nomes sem diretório nesta seção referem-se às pastas de sistema sob `Assets/Scripts` da base acima; o inventário exato de caminhos está em `source-hashes.json`.

## Achados detalhados

### E2-01 — alterações de perfil não exigem a posse atual da sessão

**Evidência:** [CampProgress.cs:35](C:/Users/Vinni/Documents/GitHub/Extlyp/Builds/Auditoria/2026-10-16_083542_382c7a/Projeto/Assets/Scripts/Camp/CampProgress.cs:35). `Edit` e `Save` recebem Session sem owner/token de posse; `Save` substitui uma cópia inteira sem comparar a revisão atual. `Claim` permite assumir a posse após 90 segundos; `Refresh` só renova se o owner ainda coincidir, mas não informa a perda da posse. Em `NetworkBootstrap.cs:344–347`, o chamador não desautoriza a sessão antiga.

Há um caminho mais direto em [NetworkBootstrap.cs:302](C:/Users/Vinni/Documents/GitHub/Extlyp/Builds/Auditoria/2026-10-16_083542_382c7a/Projeto/Assets/Scripts/UI/NetworkBootstrap.cs:302): a desconexão consulta o perfil atual e chama `Lose`/`Rollback` com o **ID de expedição encontrado nesse perfil**, antes de `Release`, sem conferir se a lease ainda pertence à conexão que saiu.

**Cenário a reproduzir:** sala A deixa de renovar por mais de 90 segundos; a mesma identidade entra na sala B, assume a lease e inicia uma expedição; A retoma e processa atividade ou desconexão antiga. A pode gravar uma cópia antiga ou executar `Lose` sobre a expedição de B. O bloqueio de conexão simultânea durante uma lease válida não cobre esse intervalo.

**Impacto:** alteração por uma sala sem posse e encerramento indevido de expedição; possível perda ou sobrescrita de progresso. Não há exploração remota reproduzida.

**Direção de correção:** conferir owner e geração da lease dentro da mesma transação de toda mutação; devolver falha explícita ao perder posse; impedir callbacks antigos de operar sobre a nova sessão. Comparar revisão em operações que gravam cópias obtidas antes, sem confiar apenas na checagem no chamador.

**Aceite:** após transferência de posse, A não pode gravar, renovar, perder, bancar nem reverter a expedição de B; uma desconexão atrasada não muda o perfil de B. Etapa 5: regras de posse e revisão. Etapa 6: duas salas e retomada/desconexão atrasada.

### E2-02 — banco confirmado precede a retirada do participante da partida

**Evidência:** [PlayerInventory.cs:214](C:/Users/Vinni/Documents/GitHub/Extlyp/Builds/Auditoria/2026-10-16_083542_382c7a/Projeto/Assets/Scripts/Players/PlayerInventory.cs:214). Depois de `CampRepository.Bank` retornar sucesso, a linha 219 executa `progressionFinished=true; ServerRefreshProgress(); Status=Extracted`. `ServerRefreshProgress` abre outra transação em `PlayerProgression.cs:25–32`, que pode lançar exceção. O campo `progressionFinished` bloqueia elegibilidade de XP; não bloqueia `CanAct` nem `DropItems`.

`CampProgress.cs:88–97` já copiou itens de volta ao perfil e limpou o ID/estado da expedição. `PlayerInventory.cs:58`, `:69` e `:142–149` permitem que um participante ainda Alive solte o inventário ao morrer/desaparecer. A extração também remove o timer antes de chamar o banco em `ExtractionZone.cs`.

**Cenário a reproduzir:** banco persiste com sucesso; a leitura seguinte falha uma vez, por exemplo por timeout do mutex; o participante continua Alive. Depois que o armazenamento volta, morte ou desconexão pode soltar itens que já estão no acampamento. Repetir o banco não resolve: o perfil já está sem expedição ativa. A validação global de identidades ajuda com exemplares únicos, mas não cobre duplicação de materiais empilháveis sem identidade.

**Impacto:** risco de itens contabilizados no camp e ainda disponíveis no mundo. É um candidato de integridade com fronteira de falha identificada, não duplicação comprovada em partida. Relaciona-se ao histórico A07.

**Direção de correção:** tratar o banco como ponto de conclusão irreversível do participante; impedir ações e drops após a confirmação mesmo que publicar/atualizar UI falhe; permitir repetir a publicação sem repetir recompensa. Não tentar devolver itens ao mundo como rollback depois de um banco já persistido.

**Aceite:** falha em cada operação entre banco e publicação do estado deixa exatamente um destino para itens e XP; morte/desconexão posterior não cria outro retorno. Etapa 5: idempotência contábil. Etapa 6: fronteira entre transação, NetworkVariables e despawn com falha controlada.

### E2-03 — recuperação do backup cobre parsing, mas não validação

**Evidência:** [SharedCampMemory.cs:75](C:/Users/Vinni/Documents/GitHub/Extlyp/Builds/Auditoria/2026-10-16_083542_382c7a/Projeto/Assets/Scripts/Camp/SharedCampMemory.cs:75). O `catch` que abre `.bak` envolve somente `ReadAllText` e `FromJson`. `ValidateFormat(saved)` está fora dele, na linha 80. Um documento que desserializa, mas tem `Records=null` ou registro sem progresso, falha sem tentar o backup.

**Impacto:** um principal estruturalmente inválido bloqueia a carga do cluster mesmo quando há backup íntegro. Isso é diferente de escrita parcial produzida pelo fluxo atual, que já usa arquivo temporário e substituição.

**Direção de correção:** carregar e validar cada candidato por completo antes de escolhê-lo; preservar o principal inválido para diagnóstico e registrar recuperação. Versão futura não deve provocar downgrade silencioso para backup antigo: exigir uma política explícita para incompatibilidade de versão.

**Aceite:** JSON que desserializa mas não representa documento válido permite recuperação segura do backup; erro é claro se ambos estiverem inválidos; versão futura é recusada de modo explícito. Etapa 5: fixtures de formato com serializador fiel. Etapa 6: confirmação com `JsonUtility` real e arquivos isolados.

### E2-04 — perfil permitido pelo servidor pode ultrapassar o limite do cliente

**Evidência:** [CampNetwork.cs:39](C:/Users/Vinni/Documents/GitHub/Extlyp/Builds/Auditoria/2026-10-16_083542_382c7a/Projeto/Assets/Scripts/Camp/CampNetwork.cs:39). `Sync` serializa o perfil inteiro dentro do snapshot e `Send` não impõe o limite correspondente. `ReceiveSnapshot`, linha 61, descarta qualquer mensagem com `reader.Length>32768` antes de aplicar perfil e acknowledgement.

`CampModels.cs:154–155` adiciona cada descarte a `Ground`, sem limite ou paginação; cozinhar sem espaço também adiciona ao chão em `:176`. A lista é persistida e enviada integralmente. O armazenamento compartilhado só rejeita quando **todos os perfis juntos** ultrapassam 2 MiB menos 4 bytes (`SharedCampMemory.cs:29`, `:99–100`), um limite diferente e muito maior. Não foi calculado um número fixo de itens para atingir o limite: tamanho depende dos metadados e da codificação.

**Impacto:** após crescimento legítimo, o servidor pode aceitar uma ação cujo snapshot/ack o cliente ignora; `Pending` permanece e uma reconexão pode continuar sem sincronizar o mesmo perfil. No limite global, um perfil que continua crescendo também prejudica operações que aumentam o documento do cluster. Sem medição de capacidade ou tráfego nesta etapa.

**Direção de correção:** orçamento explícito para chão e payload, resultado claro ao atingir capacidade e consulta paginada/segmentada. Alinhar limites antes de efetivar ações; só aumentar o limite do cliente adia o problema. Documentar também a capacidade global do cluster.

**Aceite:** perfil próximo/acima do orçamento continua recuperável; nenhuma ação aceita deixa o cliente permanentemente pendente; coleta/limpeza do chão permanece acessível; demais perfis não ficam presos por crescimento de um usuário. Etapa 5: crescimento e orçamento. Etapa 6: mensagem real, ack, reconexão e fragmentação.

### E2-05 — validação de propriedade não é validação completa do perfil

**Evidência:** [CampProfileMigration.cs:88](C:/Users/Vinni/Documents/GitHub/Extlyp/Builds/Auditoria/2026-10-16_083542_382c7a/Projeto/Assets/Scripts/Camp/CampProfileMigration.cs:88). `ValidateGrid` exige Slots não nulo, mas não os nove slots esperados. `ValidateStack` verifica recuperação, equipamento e identidade; não verifica universalmente `ItemCatalog.Valid`, limite de pilha ou capacidade de munição carregada de armas normais. `EquipmentCatalog.ValidInstance` retorna true imediatamente para itens fora do catálogo de equipamento; `RecoveryRules.Valid` não limita `Data` quando não há munição vinculada.

Exemplos de estados não barrados por esse validador: mochila com quantidade de slots diferente de nove; pilha de material acima de `ItemCatalog.Limit`; arma normal com GUID válido, quantidade 1 e `Data` maior que o carregador. São entradas de perfil inválidas, não comandos remotos arbitrários demonstrados.

**Impacto:** corrupção/importação defeituosa ou erro interno pode sobreviver à validação e afetar contagens, movimentação e cópia para os nove slots de rede; um registro problemático também pode bloquear transações que validam o conjunto inteiro.

**Direção de correção:** separar validação estrutural/semântica de unicidade e executá-las na carga e antes do commit. Não corrigir silenciosamente valores descartando itens; preservar documento inválido para recuperação. Incluir IDs, quantidade, metadados por tipo, formato dos grids, estado/escrow e identificadores de chão.

**Aceite:** fixtures inválidas são rejeitadas antes de publicar/gravar; fixtures legadas válidas migram conservando identidade, munição e quantidade; metadados impossíveis não são normalizados para recompensas válidas. Etapa 5: validação/migração e conservação. Etapa 6: desserialização real.

### E2-06 — baú fechado já envia seus itens ao cliente observador

**Evidência:** [LootPickup.cs:36](C:/Users/Vinni/Documents/GitHub/Extlyp/Builds/Auditoria/2026-10-16_083542_382c7a/Projeto/Assets/Scripts/Combat/LootPickup.cs:36). `Contents` é uma `NetworkList<ItemStack>` com leitura padrão para Everyone e preenchimento no spawn do baú (`:49–64`). A permissão de transferir exige `OpenState==2` (`:134–141`), mas a lista não espera esse estado para replicar. Visibilidade no servidor limita quem observa o objeto; não oculta campos de um objeto já observado.

**Impacto:** cliente modificado pode conhecer o conteúdo exato de um baú visível ainda fechado/trancado, antes de pagar o tempo/ferramenta de abertura. Não demonstra retirada de itens sem abrir nem conhecimento de todos os baús fora de visão.

**Direção de correção:** guardar conteúdo fechado apenas no servidor e publicar um snapshot autorizado quando o baú abrir; definir como novos observadores recebem o conteúdo aberto. Preservar apresentação pública de raridade/tipo se fizer parte da regra de jogo.

**Aceite:** ao observar um baú fechado, cliente não recebe IDs, quantidades e bônus internos; abertura legítima publica conteúdo e transferências continuam validadas no servidor. Etapa 6: cliente observador antes/depois da abertura, incluindo entrada tardia e hide/show. Uma prova fora da Unity não valida replicação NGO.

### E2-07 — XP de morte não tem recuperação após falha de armazenamento

**Evidência:** [Health.Experience.cs:27](C:/Users/Vinni/Documents/GitHub/Extlyp/Builds/Auditoria/2026-10-16_083542_382c7a/Projeto/Assets/Scripts/Combat/Health.Experience.cs:27). `experienceRewarded=true` é marcado antes de `GrantExperience`/`GrantPvp`; a exceção é capturada somente com log no fim do método. A morte prossegue em `Health.cs:54–56`; não há fila de eventos pendentes nem tentativa posterior neste fluxo. O repositório já dispõe de chave de evento para deduplicação.

**Cenário a reproduzir:** erro transitório ocorre antes de a transação de XP ser confirmada. A recompensa não é gravada, mas a instância considera a tentativa encerrada e pode desaparecer. Falha apenas ao atualizar o cliente depois de uma concessão persistida é um caso diferente: nessa situação, o XP já existe e não deve ser concedido de novo.

**Impacto:** perda de XP legítimo de morte em falhas transitórias; não significa perda em toda morte nem duplicação causada por retentativa atual. A conclusão de objetivos também precisa de caso de falha no lote, pois sua API não devolve o resultado da concessão ao mecanismo chamador.

**Direção de correção:** registrar evento estável e tratar confirmação persistente separadamente de atualização visual; reprocessar concessões pendentes com deduplicação, mesmo após despawn. Não basta mudar a flag se o objeto desaparece logo depois.

**Aceite:** falha antes do commit permite conceder posteriormente exatamente uma vez; falha após commit não duplica; rateio entre participantes conserva a recompensa; evento sobrevive ao ciclo necessário de morte/despawn. Etapa 5: repetição e falhas transacionais. Etapa 6: morte, despawn e atualização de cliente.

## Reavaliação do histórico de 09/10

Não foram reaproveitadas as antigas provas como resultado atual. Os 27 achados históricos continuam no inventário; aqui foram reavaliados os sete diretamente ligados a esta etapa.

| Histórico | Situação pela leitura da base atual | Pendência |
| --- | --- | --- |
| A01 — resultados de pesca/cozinha declarados pelo cliente | Caminho antigo bloqueado; atividades agora são conduzidas no servidor | Converter provas antigas em expectativas de rejeição e exercitar atividades válidas/latência |
| A02 — replay após sair da janela de 256 IDs | Sequência `Revision+1` acrescenta proteção além da janela de IDs | Repetição após reconexão/reinício e escritas concorrentes; E2-01 |
| A03 — GUID como identidade sem credencial | Login atual usa assinatura e vínculo da chave à Session | Provar chave errada, assinatura inválida, replay e política para identidades antigas |
| A04 — dados ocultos enviados a todos | Há interesse no servidor e inventário privado; não repetir o achado antigo integralmente | Confirmar tráfego por cliente; exposição residual E2-06 |
| A05 — ausência de armazenamento durável | Há checkpoint em disco, temporário, flush e backup | Reinício de todas as salas, recuperação e falhas; E2-03/E2-05 |
| A06 — camp editável antes de sincronizar | Pedido de ação exige sincronização e controla pending/ack | Falhas, recusas e payload grande E2-04; apresentação visual na etapa 3 |
| A07 — transação do ciclo camp/expedição | Há reserva, commit, rollback e ID da expedição | Fronteiras ainda exigem prova; E2-01/E2-02 |

Esses registros não são marcados como resolvidos em execução. Demais achados históricos pertencem às próximas etapas ou à consolidação.

## Lotes preparados para as etapas 5 e 6

| Lote | Etapa 5 — regras reais fora da Unity | Etapa 6 — confirmação integrada |
| --- | --- | --- |
| Identidade e posse | Login inválido/repetido, ações legadas rejeitadas, revisão e owner expirado/trocado | Duas salas, mesma identidade, retomada da sala antiga e callback atrasado |
| Conservação | Compra/venda, pilha cheia, unload normal/vinculado, troca entre slots, venda de recuperados recusada, morte/extração | RPCs, múltiplos jogadores e contabilidade entre perfil, inventário e chão |
| Saves e migração | Fixtures das versões suportadas, identidade duplicada, formato inválido, backup e limites | `JsonUtility`, arquivos reais isolados, fechamento/reabertura e commit interrompido |
| Loot e atributos | Validadores, categorias, garantias, raridade, pontos e metadados; usar adaptador RNG revisado se necessário | Carregar os assets reais e conferir replicação de exemplares e conteúdo de baús |
| XP | Deduplicação, rateio, tetos, PvP por par, confirmação, falha antes/depois do commit | Morte/despawn, extração, desconexão e atualização do cliente |
| Comunicação | Orçamentos estáticos e crescimento do perfil; sem alegar equivalência ao transporte | Fragmentação, snapshot acima do limite, ack e reconexão |

Revisar os adaptadores dos probes antigos antes de executar. Agrupar casos em uma campanha, sem abrir Unity por achado; preservar a estratégia da etapa 1 de builds/processos reutilizados e armazenamento isolado. Se o serializador/adaptador não reproduzir a semântica necessária, registrar o caso como pendente da Unity.

## Investigações adicionais e exclusões de escopo

- A gravação em disco precede a troca do buffer ativo de RAM (`SharedCampMemory.cs:101–118`). Queda exatamente nessa fronteira deve entrar no lote de falhas. Nesta leitura não foi demonstrada perda de uma operação já confirmada ao chamador; portanto essa janela não foi promovida a um oitavo defeito confirmado.
- A capacidade global de 2 MiB é um limite observável, não uma estimativa de jogadores suportados. Não houve medição de tamanho médio, disco, RTT ou custo de serializar o cluster.
- A assinatura de login não comprova confidencialidade do transporte, proteção da chave privada no aparelho, recuperação de conta ou resistência a volume de conexões. Esses pontos permanecem não verificados; não foi feita varredura completa de segredos.
- A disponibilidade das 18 definições de equipamento, 6 tesouros, GUIDs, prefabs e referências nativas precisa da carga de assets da etapa 6; a existência de validadores no código não prova que todos os assets passaram neles.
- Dificuldade, recompensas por hora, raridade percebida, inflação, balanceamento de atributos e mapa dependem de simulação/partida e decisões de produto. Não foram apresentados como bugs de integridade.
- Controles de laboratório versus release foram lidos apenas nas entradas de camp/expedição deste escopo. Builds operacionais e toda a superfície de diagnóstico ainda precisam da revisão correspondente; isso não constitui liberação de segurança para publicação.
- Candidatos a remover e consolidar pertencem à etapa 4. Nenhum arquivo ou asset foi excluído.

## Continuidade

Próxima etapa: **3 — revisão estática de gameplay e interface**, cobrindo combate, movimento/animação, IA, mapas, percepção, luz, controles e menus. Manter E2-01 e E2-02 no topo da investigação de integridade e carregar os sete achados para a consolidação, sem implementar correções no meio da auditoria.
