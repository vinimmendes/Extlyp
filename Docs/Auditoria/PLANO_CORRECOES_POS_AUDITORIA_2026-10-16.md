# Correções após auditoria — etapas pequenas

Planejamento solicitado pelo usuário. Referência: [consolidação da auditoria](ETAPA_7_CONSOLIDACAO_2026-10-16.md). Este documento substitui a fila anterior como roteiro de execução; os relatórios históricos continuam preservados.

## Regras

- Executar somente uma etapa por solicitação e encerrar com o resultado daquela etapa.
- Conferir o código atual antes de alterar: a auditoria usou uma base anterior e algumas correções já existem. Se o problema já estiver tratado, registrar isso sem reaplicar a mudança.
- Nas etapas C, implementar e revisar estaticamente. **Não abrir Unity, executar testes automatizados, iniciar servidor/cliente ou gerar builds para testar. Todos os testes ficam na fase T, depois das correções.**
- Cada entrega registra arquivos alterados, regra corrigida, dependências e pendência de validação. “Implementado” não significa “testado”.
- Preservar alterações locais, saves, assets, engine, rede existente e base congelada. Não executar MvpSetup.Build.
- Se uma etapa exigir mais mudanças independentes, separar a continuação antes de expandir o trabalho. Não executar as próximas etapas automaticamente.

## Fase C — corrigir e revisar, sem testes

**C1 concluída por revisão estática**, sem alterações de código/assets nem testes: [registro da C1](C1_COMPILACAO_E_EQUIPAMENTOS_2026-10-16.md). **C2–C19 implementadas e revisadas estaticamente, não testadas:** [C2](C2_POSSE_DAS_GRAVACOES_2026-10-16.md), [C3](C3_DESCONEXAO_ATRASADA_2026-10-16.md), [C4](C4_EXTRACAO_DEFINITIVA_2026-10-16.md), [C5](C5_RECUPERACAO_DE_BACKUP_2026-10-16.md), [C6](C6_ESTRUTURA_E_IDENTIDADE_2026-10-17.md), [C7](C7_QUANTIDADES_E_MUNICAO_2026-10-17.md), [C8](C8_ENVIO_DO_ACAMPAMENTO_2026-10-17.md), [C9](C9_RECEPCAO_DO_ACAMPAMENTO_2026-10-17.md), [C10](C10_SIGILO_DOS_BAUS_2026-10-17.md), [C11](C11_JANELA_DE_SAQUE_2026-10-17.md), [C12](C12_RECUPERACAO_DE_XP_2026-10-17.md), [C13](C13_DISPOSITIVO_DE_MIRA_2026-10-17.md), [C14](C14_TEXTO_CONTEXTUAL_DO_MECANISMO_2026-10-17.md), [C15](C15_MIMICO_E_MELEE_2026-10-17.md), [C16](C16_AVISO_E_AREA_DE_DANO_2026-10-17.md), [C17](C17_CONTEXTO_INTERACAO_E_SUBMIT_2026-10-17.md), [C18](C18_INVENTARIO_POR_CONTROLE_2026-10-17.md), [C19](C19_TEIA_CANCELADA_2026-10-17.md). Os pares C8/C9 e C10/C11 estão implementados. A fila da C12 cobre a sessão em execução, sem garantia de recuperação após crash com armazenamento indisponível. A revisão atual exige cliente/servidor juntos no protocolo 34, com verificação em execução pendente. Fase C encerrada; fase T não iniciada. A conferência da versão atual faz parte da própria etapa; não exige uma auditoria completa adicional.

| Etapa | Objetivo | Entrega objetiva | Referência / dependência |
| --- | --- | --- | --- |
| C1 | Reconhecer correções de compilação e assets já existentes | Conferir tipos, nomes de RPC e os pools dos 18 equipamentos/gerador; corrigir apenas lacunas restantes e registrar o que já estava resolvido. Sem compilar. | E6-01/E6-02 |
| C2 | Impedir escritas de uma sala sem posse | Conferir posse/geração dentro das transações de alteração; recusar operação antiga sem alterar o progresso atual. | E2-01 |
| C3 | Proteger contra desconexão atrasada | Callback da sala antiga não encerra, libera nem reverte a expedição de outro proprietário. | E2-01; depende de C2 |
| C4 | Tornar a extração concluída definitiva | Após o banco confirmado, impedir ações e drops; falha ao atualizar/publicar estado não devolve os itens ao mundo. | E2-02 |
| C5 | Recuperar progresso pelo backup com segurança | Validar documento inteiro antes de escolhê-lo; preservar o inválido; não substituir progresso por vazio nem fazer downgrade silencioso de versão futura. | E2-03 |
| C6 | Validar estrutura e identidade dos itens | Recusar tamanho de grade errado, IDs inexistentes e estruturas inválidas; definir tratamento conservador para saves históricos. | E2-05 |
| C7 | Validar quantidades e munição | Recusar pilhas/carregadores acima dos limites, sem apagar silenciosamente dados históricos. | E2-05; depende de C6 |
| C8 | Preparar sincronização do acampamento dentro do limite | Definir e implementar o contrato do servidor para limitar/paginar os dados e preservar todos os itens; sem truncar o chão para caber na mensagem. | E2-04 |
| C9 | Receber e confirmar a sincronização completa | Cliente aplica o contrato de C8 sem aceitar estado parcial ou deixar operação eternamente pendente; coordenar compatibilidade servidor/cliente. | E2-04; depende de C8 |
| C10 | Proteger conteúdo do baú no servidor | Visibilidade do baú não autoriza receber seus itens; transmitir conteúdo somente para saque autorizado. | E2-06 |
| C11 | Ajustar a janela de saque e suas atualizações | Cliente trata abertura, fechamento, perda de acesso e esconder/reexibir sem reter conteúdo indevido nem quebrar o saque legítimo. | E2-06; depende de C10 |
| C12 | Preservar XP quando salvar falha | Tornar a recompensa recuperável e repetível com segurança, sem premiar duas vezes. | E2-07 |
| C13 | Fazer o dispositivo de mira ativo prevalecer | Mouse parado não sobrescreve o analógico; uso do mouse e toque continuam possíveis. | E3-01 |
| C14 | Ocultar instrução distante do mecanismo | Retirar o texto de operar grade/corrente do aviso global fora do contexto; manter orientação útil para a saída. | E6-03 |
| C15 | Revelar o mímico atingido por melee | Unificar a reação de revelação do golpe válido sem duplicar dano ou eventos. | E3-02 |
| C16 | Fazer o aviso do ataque representar seu dano | Alinhar alcance, ângulo e bloqueios entre apresentação e regra do ataque identificado. | E3-03 |
| C17 | Separar interação do mundo e confirmação da interface | Um botão do controle executa somente a ação do contexto ativo. | E3-04 |
| C18 | Completar as ações do inventário por controle | Permitir selecionar, transferir e reorganizar sem arraste obrigatório, preservando teclado/mouse/toque. | E3-05; depende de C17 |
| C19 | Corrigir a apresentação da teia cancelada | Aranha só mostra liberação quando o ataque realmente acontece. | E3-06 |

C8/C9 e C10/C11 são divisões de mudanças coordenadas, não entregas independentes para jogadores. Manter servidor/cliente na mesma revisão e não publicar uma metade antes da outra. Se uma solução menor resolver ambos os lados, registrar a segunda etapa como atendida sem inventar trabalho adicional.

Os relatos de serrilhado, câmera, passada e minimapa ainda não têm aceite visual completo. Não inventar uma causa ou uma correção: manter a revisão estática disponível e verificar cada relato na fase final. Problema confirmado nessa fase gera uma correção curta e repetição somente do teste afetado.

## Fase T — testar somente após C1–C19

Executar em solicitações separadas, com a revisão corrigida identificada. Não repetir a campanha inteira da antiga etapa 6. Preparar fontes e cenários de verificação quando necessário, mas executá-los apenas nesta fase.

| Etapa | Verificação delimitada | Resultado esperado |
| --- | --- | --- |
| T1 | Compilação Unity e carregamento das definições corrigidas | Compilação/geração RPC passam; os equipamentos carregam no formato nativo. Não iniciar a campanha de jogo neste bloco. |
| T2 | Posse de sessão e desconexão antiga | Sala anterior não altera a nova expedição. Se precisar de dois servidores, reservar esse cenário como continuação exclusiva. |
| T2a | Regras do repositório em memória — concluída em 2026-10-17 | Escritas sem posse, token antigo e desconexão atrasada recusadas; ver relatório T2a. |
| T2b | Posse no armazenamento entre processos — concluída em 2026-10-17 | 27 registros aprovados com memória/mutex/disco reais, dois processos e retomada em um terceiro; ver relatório T2b. |
| T2c | Callbacks de rede e limpeza — concluída em 2026-10-17 | 83 registros aprovados em quatro cenários com UTP local real em Play Mode; ver relatório T2c para fixtures e limites. T2 encerrada nos cenários delimitados. |
| T3 | Falha depois do banco de extração | Um único destino para itens/XP, inclusive após morte/desconexão; sem duplicação. |
| T3a | Extração normal e falha na publicação do estado — concluída em 2026-10-17 | 44 registros aprovados no host Unity e armazenamento isolado; ver relatório T3a. |
| T3b | Falhas de atualização de progresso e sincronização — concluída em 2026-10-17 | 92 registros aprovados em quatro casos: recuperação e remoção com publicação pendente; ver relatório T3b para injeções e limites. |
| T3c | Sandbox, reconexão e bloqueios específicos — concluída | T3c1 e T3c2 concluídas nos escopos delimitados dos relatórios. T3 encerrada. |
| T3c1 | Sandbox e bloqueios específicos — concluída em 2026-10-17 | 40 registros aprovados com componentes reais de tiro, recarga e movimento; ver relatório T3c1. |
| T3c2 | Reconexão após extração — concluída em 2026-10-17 | 37 registros aprovados em uma conexão inicial e duas reconexões com snapshots/ACK reais; ver relatório T3c2. |
| T4 | Recuperação de backup — concluída no escopo delimitado | T4a/T4b/T4c aprovadas; consultar limites nos relatórios. |
| T4a | Recuperação e versões — concluída em 2026-10-17 | 15 cenários com JSON nativo, preservação de bytes e recusa de versões futuras; ver relatório T4a. |
| T4b | Falhas de acesso, preservação e escrita — concluída em 2026-10-17 | Seis cenários aprovados com bloqueios/colisões reais; bytes anteriores e estado compartilhado preservados. Ver relatório T4b. |
| T4c | Migração e reinício real — concluída em 2026-10-17 | Cinco cenários em três processos, com 94 registros e estabilidade exata do perfil/arquivos de migração após reinício. |
| T5 | Validação de perfil — concluída no escopo delimitado | T5a e T5b aprovadas; consultar cobertura e limites nos relatórios. |
| T5a | Estrutura e identidade — concluída em 2026-10-17 | 38 cenários e 149 verificações aprovados; recusa de estruturas/identidades inválidas e preservação transacional. Ver relatório T5a para cobertura e limites. |
| T5b | Quantidades e munição — concluída em 2026-10-17 | 567 casos/1.218 verificações aprovados; corrigida munição herdada do inicializador quando Data está ausente no JSON. Ver relatório T5b. |
| T6 | Sincronização de acampamento grande — em andamento | Dividida em T6a–T6d para manter rodadas pequenas. |
| T6a | Envio e aplicação integral — concluída em 2026-10-17 | 37 verificações aprovadas por NGO/UTP local: controle pequeno, dois envelopes de aproximadamente 186 KB/23 partes, 500 entradas e ACKs sem aplicação parcial. Ver relatório T6a. |
| T6b | Fila e atualizações concorrentes — próxima | Conferir transferência atual, substituição do próximo snapshot e preservação de confirmação/mensagem. |
| T6c | Contrato e integridade | Conferir metadados/partes inválidos, repetição, digest e fronteiras de tamanho; subdividir se necessário. |
| T6d | Recuperação da sincronização | Timeout, novo Sync e reconexão sem repetir a operação econômica original. |
| T7 | Sigilo e saque do baú | Cliente sem autorização não recebe conteúdo; abrir/fechar/esconder/reexibir funciona. |
| T8 | Falha de persistência de XP | Recuperar recompensa uma vez, inclusive em nova tentativa. |
| T9 | Mira e conflito Interact/Submit | Dispositivos/contextos respondem corretamente; não executar toda a interface no mesmo bloco. |
| T10 | Inventário por controle | Transferência e reordenação completas sem arraste. |
| T11 | Mímico e melee | Golpe válido revela o mímico; dano não duplica. |
| T12 | Aviso e setor de dano | Bordas do aviso coincidem com a área perigosa. |
| T13 | Cancelamento de teia | Não aparece lançamento de ataque cancelado. |
| T14 | Texto do objetivo | Aparece no contexto certo e some ao afastar. |
| T15 | Sombras e visão | Conferir serrilhado, cor/cinza e abertura por Percepção. |
| T16 | Caminhada e câmera | Conferir passada/velocidade e conforto do zoom, sem alterar balanceamento por impressão isolada. |
| T17 | Minimap e extrações | Conferir marcador em movimento; percurso de mecanismo/extração fica em continuação própria se necessário. |
| T18 | Android/toque, se houver aparelho disponível | Cenário definido por rodada; sem prometer validação de aparelho por teste no Windows. |

Não produzir builds por rotina: usar evidências da compilação e reutilizar os executáveis enquanto a revisão não mudar. Só gerar o build necessário à verificação correspondente. Falha encontrada exige diagnóstico/correção do problema e repetição das dependências afetadas, sem disparar todas as etapas T novamente.

## Manutenção e reduções — fila separada

As 14 decisões R4 são propostas de manutenção, não 14 exclusões aprovadas. Não atrasam os testes das correções para investigar pacotes opcionais ou reconstruir o pipeline do servidor. Executar somente quando o bloco de manutenção correspondente for solicitado, um candidato por vez, conforme [dependências e aceites da etapa 4](ETAPA_4_REDUCOES_2026-10-16.md).

| Bloco | Trabalho |
| --- | --- |
| M1 / R4-07 | Atualizar ou rotular guias obsoletos. |
| M2 / R4-06 | Retirar/migrar parâmetros de loot sem efeito. |
| M3 / R4-01 | Arquivar WalkTest preservando fonte e referências. |
| M4 / R4-02 | Retirar a prancha aprovada de Resources, preservando a referência e ajustando ferramentas. |
| M5 / R4-03 | Consolidar um par de imagens por execução, após conferir importadores e consumidores. |
| M6 / R4-04 | Retirar apenas apresentação duplicada, preservando lógica e ferramentas ativas. |
| M7 / R4-08 | Atualizar um verificador antigo por vez; não executar testes durante a alteração. |
| M8 / R4-05 | Planejar e retirar compatibilidade de pesca desativada, preservando cabana/protocolo. |
| M9 / R4-09 | Catalogar fontes/intermediários; manter arquivos necessários à reprodução da arte. |
| M10 / R4-10 | Conferir uso e preservar dados antes de eventual limpeza de ServerValidation. |
| M11 / R4-11 | Definir retenção por pasta, sem apagar evidências/cache em bloco. |
| M12 / R4-12 | Restringir reconstrução inicial sem remover ferramentas necessárias. |
| M13 / R4-13 | Investigar um pacote opcional e suas dependências; não atualizar engine/rede. |
| M14 / R4-14 | Estudar separação visual do servidor, sem instalar módulos ou prometer ganho. |

Mudanças de manutenção têm sua própria verificação ao final do bloco de alterações correspondente. Não considerar seguro remover um asset somente por busca textual, nem substituir dependências nativas ausentes por adivinhação.

## Próxima execução

**T6b: fila e atualizações concorrentes.** [T6a concluída](T6A_ACAMPAMENTO_GRANDE_2026-10-17.md) com 37 verificações de envio/aplicação integral e ACKs por NGO/UTP local. T6 permanece em andamento; T6c e T6d seguem em rodadas separadas. T5 encerrada nos escopos de [T5a — estrutura e identidade](T5A_ESTRUTURA_E_IDENTIDADE_2026-10-17.md) e [T5b — quantidades e munição](T5B_QUANTIDADES_E_MUNICAO_2026-10-17.md). T5b corrigiu a leitura de munição ausente que herdava o carregador inicial. T4 encerrada nos cenários delimitados: T4a (15 cenários/101 registros), T4b (seis/52) e T4c (cinco/94 em três processos). Ver os relatórios `T4A_RECUPERACAO_E_VERSOES_2026-10-17.md`, `T4B_FALHAS_DE_ARQUIVO_2026-10-17.md` e `T4C_MIGRACAO_E_REINICIO_2026-10-17.md` para evidências e limites. T3 e T2 encerradas nos cenários delimitados de seus relatórios, sem equivaler a uma partida completa ou validação de aparelhos. C1 foi concluída por revisão estática; C2–C19 foram implementadas e revisadas. T1 concluída: ver `T1_COMPILACAO_E_DEFINICOES_2026-10-17.md`. As próximas etapas continuam em solicitações separadas, sem repetir a auditoria inteira.
