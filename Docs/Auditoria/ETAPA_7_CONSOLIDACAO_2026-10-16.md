# Etapa 7 — consolidação e próximos trabalhos

Data: 16/10/2026. Consolidação documental das etapas 1–6, sem novos testes, builds, cópias ou alterações de gameplay. A auditoria termina com uma fila de trabalho e limitações explícitas; **não é uma liberação do jogo**.

Reexecução solicitada pelo usuário: revisão 2 da consolidação. Conferidos novamente os registros e classificações dos relatórios das etapas 2–6, sem alterar seus resultados históricos. **A etapa 6 já foi executada e não foi repetida.** Esta revisão não inspeciona novamente o código atual nem comprova correções posteriores.

## Conclusão

O jogo iniciou e completou cenários básicos Windows com servidor e dois clientes. Permanecem riscos de integridade dos itens/progresso, exposição de conteúdo e problemas de controles/interface. A prioridade é proteger dados e economia antes de limpar código ou ampliar conteúdo.

Há **16 registros: quatro P1, dez P2 e dois P3**, além de 14 decisões de redução. Esses números incluem riscos ainda não reproduzidos e defeitos da base já corrigidos externamente: **não representam 16 bugs confirmados ativos no projeto atual**.

Para decidir o próximo trabalho, separar a contagem histórica da fila: **dois registros P1 têm correções externas incorporadas à cópia testada** (E6-01/E6-02). Os outros **14 registros precisam de reavaliação ou correção** na revisão atual: dois P1, dez P2 e dois P3. Não foram declarados resolvidos, mas também não foram todos reproduzidos.

A referência é a base congelada 2026-10-16_083542_382c7a. A etapa 6 usou uma cópia com dois scripts corrigidos e 18 assets ajustados, mais instrumentação. Outras alterações posteriores no projeto de trabalho não foram integralmente auditadas. Antes de corrigir qualquer item, conferir se a revisão atual já o resolveu; não reaplicar patches da base por cima do trabalho existente.

## Registro único de achados

P1: integridade ou bloqueio de execução; P2: problema funcional/segurança relevante; P3: melhoria funcional de menor urgência. Evidência estática indica um caminho de código, não um problema observado em partida.

| ID | Prioridade | Problema e efeito | Melhor evidência disponível | Próximo aceite |
| --- | --- | --- | --- | --- |
| E2-01 | P1 | Sala antiga pode alterar a expedição da nova sala após perder posse | Sequência reproduzida nas regras C#; duas salas reais pendentes | Escritas e callbacks antigos não alteram dados do novo proprietário. |
| E2-02 | P1 | Falha após salvar extração pode deixar itens também disponíveis no mundo | Ordem de operações estática; banco normal funcionou; falha não injetada | Banco concluído impede ações/drops, mesmo se a atualização seguinte falhar. |
| E2-03 | P2 | Recuperação de backup pode ignorar cópia válida ou carregar vazio | JsonUtility, arquivos e memória compartilhada reais | Validar candidato inteiro, preservar documento inválido e recuperar sem perder progresso; versão futura exige política explícita, sem downgrade silencioso. |
| E2-04 | P2 | Acampamento gera mensagem maior que o receptor aceita | Serialização real: cerca de 101 KB para limite de 32 KB | Limitar/paginar/sincronizar dados sem perder itens ou deixar confirmação pendente; fluxo completo ainda precisa de cenário próprio. |
| E2-05 | P2 | Perfil aceita grades, IDs, pilhas e munição inválidos | Quatro entradas inválidas aceitas nas regras e na Unity | Rejeitar cada formato inválido antes de persistir, preservando o estado anterior. |
| E2-06 | P2 | Cliente conhece conteúdo do baú fechado | Replicação real antes de abrir, inclusive após esconder/reexibir | Cliente não recebe conteúdo antes da autorização de saque; abertura e visibilidade continuam funcionando. |
| E2-07 | P2 | Falha ao salvar XP de morte pode perder a recompensa | Revisão estática; falha de persistência não exercitada | Recompensa recuperável e idempotente, sem perda nem duplicação. |
| E3-01 | P2 | Mouse parado vence mira do gamepad | Stick norte e cursor à esquerda resultaram em mira para esquerda no servidor | Stick ativo controla a mira; uso do mouse retoma controle sem tiro/salto involuntário. |
| E3-02 | P2 | Melee fere mímico disfarçado sem revelá-lo | Caminhos de dano/estado revisados; partida específica pendente | Golpe válido revela o mímico uma vez e mantém as demais reações de dano. |
| E3-03 | P2 | Aviso do ataque não cobre todo o setor que causa dano | Divergência geométrica estática | Aviso e dano concordam nas bordas, distâncias e bloqueios. |
| E3-04 | P2 | Botão do gamepad pode interagir e submeter botão da HUD | Bindings/contextos; ação dupla não reproduzida | Um pressionamento gera somente a ação do contexto ativo. |
| E3-05 | P3 | Inventário não oferece todas as ações por seleção/Submit | Fluxo de interface revisado; mochila capturada | Transferir e reorganizar sem depender de arraste, mantendo teclado/toque. |
| E3-06 | P3 | Aranha pode mostrar lançamento de teia após cancelar | Transição visual estática | Cancelamento não exibe liberação de ataque que não aconteceu. |
| E6-01 | P1 na base | Tipos e nomes de RPC impediam compilação | Erro NGO; build passou com correções externas incorporadas à cópia | Reconhecer correção atual e conferir integração específica, sem repetir correção já aplicada. |
| E6-02 | P1 na base | Assets de atributos eram lidos com quantidade inválida de entradas | Exceção nativa; campanha passou após serialização corrigida | Conferir os 18 modelos e seu gerador na revisão atual; não confiar apenas no adaptador YAML. |
| E6-03 | P2 | Instrução do mecanismo aparece longe do objetivo | Imagem a aproximadamente 81 u e caminho global da HUD | Texto de operar mecanismo desaparece ao afastar, além do botão contextual. |

Os detalhes de causa, caminhos de arquivos e critérios completos permanecem nos relatórios de [dados](ETAPA_2_DADOS_SEGURANCA_ECONOMIA_2026-10-16.md), [gameplay](ETAPA_3_GAMEPLAY_INTERFACE_2026-10-16.md) e [execução integrada](ETAPA_6_VALIDACAO_INTEGRADA_2026-10-16.md). Os IDs acima substituem contagens duplicadas por teste; não alteram retroativamente os registros históricos.

## Situação das evidências na revisão 2

Cada registro aparece em exatamente uma categoria abaixo. “Confirmado” se refere à base/revisão exercitada e ao alcance indicado, não a uma exploração remota nem ao código atual inteiro.

| Categoria | Quantidade | IDs | Significado |
| --- | ---: | --- | --- |
| Comportamento observado | 6 | E2-01, E2-03, E2-05, E2-06, E3-01, E6-03 | E2-01 foi reproduzido como sequência nas regras, sem duas salas reais. Os demais têm prova nativa, rede ou imagem descrita no registro. |
| Incompatibilidade medida, fluxo completo pendente | 1 | E2-04 | O tamanho ultrapassa o limite real do receptor; não foi executado todo o crescimento do acampamento pelo cliente. |
| Evidência estática ou lacuna de operação, sem cenário específico completo | 7 | E2-02, E2-07, E3-02, E3-03, E3-04, E3-05, E3-06 | Banco normal, imagem da mochila ou presença de monstros não confirmam os cenários de falha correspondentes. |
| Correção externa presente e exercitada na cópia | 2 | E6-01, E6-02 | Não reaplicar; conferir integração da revisão atual quando esse bloco for acionado. |

Nenhum desses grupos equivale a “validado no Android”. Os sete casos sem cenário específico continuam abertos, sem transformar hipótese em falha reproduzida ou teste ausente em aprovação.

## Cobertura final

| Recorte | O que sustenta a conclusão | Limitação que permanece |
| --- | --- | --- |
| Regras de economia/progressão | Etapa 5: 72 casos, 66 PASS, seis FAIL relativos a três achados | Adaptadores não equivalem à Unity. O comportamento nativo de backup e assets foi refinado na etapa 6. |
| Unity/Windows/rede | Build final sem erros/avisos; dois humanos e seis bots; tiro, recarga, descarte, movimento e reconexão | Cópia com diferenças registradas; não valida toda a revisão atual nem redes adversas/salas concorrentes. |
| Persistência | Checkpoints próprios, banco normal e provas nativas de backup | Falha após banco e falha de XP não reproduzidas em partida. |
| Mapa/visão/HUD | Spawns distintos, câmera 7,5 e visão inicial 80°; capturas válidas com cor dentro da visão e cinza fora | Serrilhado, conforto do zoom, passada, marcador em movimento, variedade/desafio e mecanismos completos sem aceite. |
| Combate/IA/controles | Conflito mouse/gamepad reproduzido; entidades presentes; stamina consumida | IA congelada para cenários controlados; melee, habilidades, navegação, foco, controle físico e multitouch incompletos. |
| Plataformas/desempenho | Execução Windows gráfica e servidor | Sem Android/aparelho nem benchmark comparativo. Não há promessa de ganho de memória/FPS. |

As 14 asserções satisfeitas do coletor da etapa 6 incluem reprodução de bugs. Não somar esses números aos 66 PASS para apresentar uma taxa de aprovação geral.

## Fila de correções em blocos pequenos

**Roteiro atualizado por solicitação posterior do usuário:** seguir o [plano de correções pós-auditoria](PLANO_CORRECOES_POS_AUDITORIA_2026-10-16.md), com etapas C1–C19 e testes apenas na fase final T. A fila abaixo permanece como registro da consolidação anterior, não como numeração vigente para execução.

Cada linha é um trabalho separado, encerrado com entrega curta. A lista é planejamento, não autorização para executar tudo na próxima solicitação.

Ordem de decisão: **posse entre salas → conclusão da extração → recuperação de progresso → validação de dados → mensagens/baús/XP → controles e combate → manutenção**. Antes de cada bloco, conferir somente o achado correspondente na revisão atual; se já corrigido, registrar a evidência e a validação ainda necessária, em vez de editar novamente.

| Ordem | Bloco | Entrega / dependência |
| --- | --- | --- |
| 1 | Conferir E2-01 no código atual | Identificar se as alterações externas já protegem posse e desconexão; parecer e patch proposto, sem Unity. |
| 2 | Corrigir E2-01, se ainda necessário | Uma alteração focada de autoridade; revisão das mutações/callbacks afetados. Depende do parecer anterior. |
| 3 | Tratar a conclusão da extração, E2-02 | Separar banco confirmado de falha de publicação e impedir drops posteriores; revisão de idempotência. |
| 4 | Tratar recuperação de backup, E2-03 | Carga/validação/preservação dos candidatos, sem restaurar versão antiga silenciosamente. |
| 5 | Fortalecer validação de perfil, E2-05 | Grade, IDs, pilhas e carregadores; considerar compatibilidade de saves antes de recusar dados históricos. |
| 6 | Reduzir snapshots, E2-04 | Definir limite/protocolo de sincronização e comportamento quando o acampamento cresce, sem apagar itens. |
| 7 | Proteger conteúdo de baús, E2-06 | Autorizar conteúdo separadamente da visibilidade do objeto. Mudança de rede coordenada entre servidor/cliente. |
| 8 | Recuperar recompensa de XP, E2-07 | Repetição segura da persistência sem premiar duas vezes. |
| 9 | Ajustar prioridade de mira, E3-01 | Um único conflito de dispositivo; preservar mouse e toque. |
| 10 | Ajustar texto distante, E6-03 | Remover instrução contextual da orientação global fora de alcance. |
| 11 | Corrigir mímico/melee, E3-02 | Reação de revelação consistente no caminho de dano. |
| 12 | Alinhar aviso/dano, E3-03 | Geometria de um ataque, com regra e apresentação coerentes. |
| 13 | Separar Interact/Submit, E3-04 | Contexto de entrada/foco; não misturar com toda a navegação do inventário. |
| 14 | Completar inventário por controle, E3-05 | Transferência/reordenação como tarefa própria. |
| 15 | Ajustar cancelamento visual da aranha, E3-06 | Exibir liberação somente quando ataque ocorreu. |

E6-01/E6-02 ficam como correções externas a reconhecer, não patches a reaplicar. Conferência de código/dados e validação prática são blocos distintos. Depois dos itens de integridade, reservar sessões separadas para sombras/visão, caminhada/câmera, minimapa e mecanismos; não agrupá-las numa campanha única.

## Reduções e conteúdo obsoleto

Nenhuma exclusão foi executada. Referências, confiança, dependências e aceites detalhados estão na [etapa 4](ETAPA_4_REDUCOES_2026-10-16.md). As medições são históricas e precisam ser conferidas antes de uma limpeza.

| ID | Destino consolidado | Condição antes de alterar |
| --- | --- | --- |
| R4-01 | Arquivar WalkTest fora de Assets | Conferir referências/fonte de exportação e preservar arquivo recuperável; 3,365 MiB de origem. |
| R4-02 | Retirar prancha aprovada de Resources, preservando referência | Ajustar ferramentas e conferir atlas/prefab; 2,005 MiB de origem, sem ganho de build garantido. |
| R4-03 | Consolidar sete pares de PNG | Comparar importadores/subassets e remapear consumidores; até 13,51 MiB de origem. |
| R4-04 | Consolidar apresentação IMGUI/uGUI | Preservar lógica ativa, minigames e ferramentas; verificar fluxo antes de retirar desenho duplicado. |
| R4-05 | Retirar compatibilidade da pesca desativada em expedição | Mapear campos/RPCs/protocolo; manter pesca da cabana e rejeição de comandos antigos. |
| R4-06 | Remover/migrar parâmetros antigos de loot | Conferir referências e migrar configuração, preservando parâmetros ativos. |
| R4-07 | Rotular/arquivar guias antigos | Indicar substituto e preservar fatos históricos. |
| R4-08 | Consolidar verificadores antigos | Atualizar contratos; não reproduzir a diferença YAML/serialização nativa como validação equivalente. |
| R4-09 | Manter fontes de arte; arquivar intermediários seletivamente | Preservar fonte editável, receita, licenças e aprovações. Não apagar Art/Prototypes em bloco. |
| R4-10 | Limpeza condicional de ServerValidation | Conferir uso, caminho e dados a preservar; 4,24 GiB medidos na etapa 4, só disco local/cache. |
| R4-11 | Definir retenção de temporários/cache/evidências | Inventário por pasta e política explícita; preservar auditoria e não limpar cache antes de recompilar. |
| R4-12 | Manter/restringir ferramentas de autoria inicial | Rever chamadas/menus/guias; não executar MvpSetup para conferir limpeza. |
| R4-13 | Investigar pacotes opcionais | Resolver dependências antes de remover; manter engine/rede/pipeline essenciais. |
| R4-14 | Investigar separação visual no servidor | Trabalho específico de build/inclusão de assets; medir antes de prometer economia. |

Ordem sugerida para manutenção: documentação antiga; parâmetros sem efeito; um experimento/asset por vez; duplicação de apresentação; compatibilidades de rede por último. Limpeza de disco pode ser independente quando necessária, com conferência própria. Não somar candidatos sobrepostos nem chamar fonte de arte/cache de “lixo”. Os 194 grupos idênticos não autorizam apagar 357,54 MiB.

## Regra de execução daqui em diante

Por solicitação do usuário após o custo da etapa 6, cada próxima execução deve ter **um achado ou uma mudança delimitada**, com resultado e ponto de parada definidos. Se a investigação crescer, registrar a descoberta e separar a continuação; não disparar uma campanha completa automaticamente.

Por padrão, implementar/revisar sem iniciar testes automatizados, Unity ou builds de validação. A validação prática do usuário vem primeiro; testes solicitados ou acionados por relato devem cobrir apenas o problema e suas dependências diretas. Compilação/build, rede e verificação visual devem ter blocos separados. Não repetir suites por rotina nem usar logs antigos como aprovação de código novo.

**Próximo bloco vigente:** C1 do plano de correções pós-auditoria, conferência das correções existentes sem testes. A proposta anterior começava por E2-01; o novo roteiro trata posse em C2/C3. A consolidação está entregue; correções e limpezas continuam trabalhos futuros.

## Encerramento da reexecução

- Conferência cruzada manteve 16 IDs únicos, prioridades históricas e 14 decisões de redução; não foram criados novos achados de gameplay.
- A revisão 2 explicita as categorias de evidência e separa as duas correções externas dos 14 registros a reavaliar, evitando apresentar todos como bugs ativos confirmados.
- A etapa 6 permanece executada com limitações. Seus testes pendentes serão blocos futuros específicos, não condição para repetir automaticamente a campanha inteira.
- Alterados somente este relatório e o acompanhamento da auditoria. Nenhum código, asset, save, teste, build ou processo de jogo foi alterado/iniciado nesta reexecução.
