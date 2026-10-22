# Etapa 6 — Unity e jogo integrado

Data: 16/10/2026. **Campanha Windows executada, com cobertura parcial e limitações explícitas.** Não constitui homologação de todo o jogo nem encerra os achados anteriores. Nenhuma correção de gameplay foi aplicada ao projeto de trabalho.

## Resultado

- Build Windows final **Succeeded, zero erros e zero avisos**, 668.175.776 bytes; BuildPipeline em 19,34 s, sem contar importação e tentativas anteriores. Não é benchmark.
- Servidor e dois clientes distintos passaram pelo acampamento, matchmaking real de 30 s e Catacumbas: dois humanos, seis bots e oito posições iniciais distintas.
- Tiro, recarga, descarte, movimento e reconexão usaram caminhos reais de rede. Banco de extração e dano letal foram acionados administrativamente no servidor; não comprovam interação com zona, corrente ou manivela.
- Confirmados problemas de validação, conteúdo de baú fechado e mira mouse/gamepad. Backup reavaliado com JsonUtility real e tamanho de mensagem medido com FastBufferWriter real.
- Capturas válidas de cabana, Catacumbas e mochila obtidas com servidor e um cliente visível. As tentativas anteriores de captura não servem como evidência visual.

## Base e diferenças

Base congelada preservada: Builds/Auditoria/2026-10-16_083542_382c7a/Projeto. Seus 5.047 arquivos foram conferidos por SHA-256. Execução em ExecucaoEtapa6, no mesmo diretório pai.

A preparação encontrou 25 diferenças entre arquivos do manifesto e o projeto de trabalho, incluindo código e equipamentos. Alterações externas continuaram ocorrendo; não confundir a revisão testada com todo o projeto atual.

| Overlay na cópia | Motivo |
| --- | --- |
| Monster.cs: dois argumentos InVision passam PlayerInventory | Correção de tipo já existente no projeto de trabalho, necessária à compilação. |
| PlayerEquipment.cs: dois métodos e chamadas com sufixo ServerRpc | Pós-processador NGO rejeitou os nomes antigos; correção já existente no projeto de trabalho. |
| 18 definições de equipamentos: SecondaryPool em dois bytes | Array de enum byte estava gravado como dois inteiros de quatro bytes; mudança equivalente já existente no projeto de trabalho. |
| Driver runtime e script de editor, com metas/pasta | Instrumentação da cópia, com fontes em Tools/Audit. Não integra Assets do projeto de trabalho. |

Inventário final: **20 arquivos existentes diferentes e cinco adicionais**. As demais alterações externas de CampProgress, SharedCampMemory, NetworkBootstrap, fontes e gerador não foram incorporadas. Engine e pacotes preservados: Unity 6000.0.84f1, NGO 2.13.3, Transport 2.7.4, URP 17.0.4, Input System 1.20.1.

Diferenças/hashes completos em etapa6/execution-source-differences.json; hashes de binários em observations-summary.json. O manifesto de preparação original foi preservado sem atualização retroativa.

## Isolamento e tentativas

Evidências em Logs/auditoria/2026-10-16_083542_382c7a/etapa6.

- Loopback UDP 19177, conferida por bind antes das campanhas; cluster audit-stage6-20261016, runtime-data próprio e identidades client1/client2 separadas. Provas de backup usam clusters GUID distintos.
- Um executável Windows Development comum nos modos servidor/cliente, reutilizado nos cenários; não exige módulo Dedicated Server.
- Preparação administrativa: congelar IA após início, criar baú, adicionar metal, teleportar para checar visibilidade, causar dano e chamar banco.
- Movimento/corrida/mira por dispositivos virtuais do Input System; tiro, recarga e descarte pelos métodos públicos que enviam os RPCs reais.
- Sem MvpSetup.Build, reconstrução de cena, exclusões de limpeza ou uso dos saves normais.

O objetivo de uma única abertura precisou ser revisto. Houve espera/interrupção na importação, tentativa concorrente rejeitada por projeto já aberto, falhas de compilação do verificador/base e falha nativa dos assets. Foram gerados **dois builds bem-sucedidos**: primeiro em campaign1/Player; segundo corrige o coletor e incorpora a serialização corrigida. O segundo binário foi reutilizado nas demais campanhas. Logs e códigos de saída foram preservados.

Problemas do verificador diagnosticados, sem tratá-los como bugs do jogo:

1. Contadores BuildReport são int nesta versão, não uint: corrigido após CS0266.
2. Bots não têm PlayerController: primeiro coletor parou de atualizar ao acessar o componente; somente o driver foi corrigido.
3. Primeiro teste de backup sem marcador era inconclusivo. A mensagem “backup recovered count=0” não prova recuperação; o reteste usa registro identificável e o original permanece preservado.
4. Primeira consolidação comparava o descarte com o inventário anterior à adição de metal. A falsa falha foi corrigida usando o resultado da adição imediatamente anterior; ambos os consolidados permanecem disponíveis.
5. Capturas ocultas ficaram pretas. Dois clientes visíveis encontraram Win32 IO 1450 no coletor. A captura com um cliente visível funcionou. Sem causa suficiente para atribuir a limitação ao jogo ou prometer solução de desempenho.

## Evidência por achado

| Achado | Observação nesta etapa | Conclusão / limite |
| --- | --- | --- |
| E2-01 — posse/desconexão entre salas | Dois clientes em uma sala | Sem reprodução entre dois servidores/lease expirado; mantém prova da etapa 5 e risco estático. |
| E2-02 — falha após banco | Banco normal atingiu Extracted no servidor/proprietário; checkpoint marcou extração | Sem falha injetada entre Bank e Refresh. P1 permanece aberto. |
| E2-03 — backup | JsonUtility, MMF e arquivos reais, backup com marcador | Records:null carrega conjunto vazio e ignora backup. O checkpoint pode substituir o primário e sobrescrever .bak com o primário inválido. Versão 999 lança em ValidateFormat sem tentar backup. Não repetir a exceção do adaptador como comportamento nativo. |
| E2-04 — mensagem grande | 128 itens sintéticos: **101.366–101.368 bytes** nas duas execuções do FastBufferWriter | Acima dos 32.768 do receptor. Não houve sequência completa de 128 descartes/acks em rede. |
| E2-05 — validação | Grade de dez slots, ItemId 254, Metal 100.000 e pistola Data 100.000 aceitos por ValidateOwnership na Unity | Quatro rejeições ausentes confirmadas; não usados para corromper perfis dos clientes. |
| E2-06 — baú fechado | Baú 51, OpenState 0: client1 recebeu FighterMail, AncientCoin e Wood; client2 distante não recebeu o objeto | Reproduzido em rede. Em outra rodada, baú 47 sumiu ao afastar e reapareceu fechado com mesmo conteúdo. Visibilidade funciona; sigilo do conteúdo não. |
| E2-07 — XP sob falha | Sem falha injetada | Risco estático mantido. |
| E3-01 — mira | Stick norte 90°, cursor à esquerda; servidor e proprietário em 180° | Reproduzido por Input System e replicação reais; sem controle físico. |
| E3-02 — mímico/melee | Não executado | Evidência estática mantida. |
| E3-03 — aviso/setor melee | Não executado | Cenário físico/visual pendente. |
| E3-04 — Interact/Submit | Sem foco de botão exercitado | Conflito estático, sem confirmar ação dupla. |
| E3-05 — inventário por controle | Mochila abriu/fechou e foi capturada | Não valida transferência/reordenação por Submit. |
| E3-06 — teia cancelada | Não executado | Transição específica de IA/animação pendente. |

## Registros adicionais

### E6-01 — compilação bloqueada na base

**P1 na base; correções externas já presentes no projeto de trabalho.** Dois argumentos InVision têm tipo incompatível e dois RPCs de equipamento não têm sufixo ServerRpc. O pós-processador NGO confirmou o segundo bloqueio em execução. A cópia gerou build após os overlays.

Aceite: compilar/gerar a revisão atual com Netcode real. O build desta cópia não valida as demais mudanças externas.

### E6-02 — serialização de atributos dos equipamentos

**P1 na base; serialização corrigida externamente e exercitada no overlay.** AttributeId usa byte; os 18 SecondaryPool congelados gravam 16 dígitos hexadecimais para dois valores, lidos como oito entradas pela Unity. EquipmentCatalog rejeita a definição, começando por BreathTalisman. O erro apareceu em TestLabPanel.Awake e na criação de loot/bots, inclusive em ambiente Normal.

O adaptador YAML da etapa 5 lia dois inteiros e não representava essa serialização nativa. Após gravar os dois bytes equivalentes na cópia, entraram oito participantes e loot de equipamentos sem a exceção anterior.

Aceite: todos os modelos carregam via Resources/EquipmentCatalog na Unity com dois secundários distintos e válidos; conferir também o gerador na revisão atual.

### E6-03 — instrução de objetivo continua longe do mecanismo

**P2, imagem e código.** A captura exibe “1 · Erguer grade · 6 s” com a extração a aproximadamente 81 u. UguiGameHud atribui a nota por ExitStatus da saída selecionada, sem filtro de distância. É outro caminho além do botão de interação contextual, que tem proximidade.

A atribuição também contempla a corrente, embora a captura mostre a grade. O relato original não está resolvido por ocultar somente o botão de interação.

Aceite: instruções de operar mecanismo desaparecem ao afastar, conforme solicitado; orientação global pode manter destino/estado sem repetir instrução contextual.

## Cobertura e pendências

| Área | Executado | Sem aceite nesta etapa |
| --- | --- | --- |
| Build | C#, geração RPC, importação, Player Windows | Totalidade das alterações externas atuais; release/entrega. |
| Rede | Identidades distintas, camp/expedição, matchmaking, RPCs, reconexão | Salas concorrentes, latência/perda, abuso de RPC e falhas de disco. |
| Itens/loot | Tiro 12→11, recarga 11→12, descarte do slot abastecido com três metais, baú/hide/show | Coleta do descarte/conservação ponta a ponta, comércio, crafts, troca/cancelamento de equipamento. |
| Persistência/XP | Checkpoint próprio com perfil extraído e perfil morto | Banco/XP sob falha, concorrência e todos os saves históricos. |
| Movimento | Caminhada replicada; corrida reduziu stamina 100→aprox.73 | Calibração de passada, interpolação, colisões de todas as rotas e equilíbrio. |
| IA/combate | Bots/monstros presentes antes de congelar administrativamente | Estados especiais, áreas/veneno, mímico, teia, navegação e justiça dos encontros. |
| Mapa/spawn | Oito posições distintas, Catacumbas presentes | Distribuição estatística do sorteio, Floresta, mecanismos e rotas completos. |
| Visão/câmera | 80°, câmera 7,5, cor dentro da visão e cinza fora na captura | Serrilhado em quinas, percepção máxima em runtime, conforto do zoom. |
| HUD/minimapa | Cabana, HUD e mochila em 1280×720 | Marcador durante deslocamento contínuo, outros aspectos/safe areas, dois temas completos. |
| Controles | Teclado/gamepad/mouse virtuais | Hardware físico, foco/Submit e multitouch. |
| Plataformas/desempenho | D3D11, Radeon integrada registrada no log | Android/aparelho e benchmark/Profiler comparativo. Sem alegação de ganho. |

O coletor final contém **14 asserções satisfeitas sobre observações**, incluindo asserções que esperam a reprodução de bugs. Isso não significa 14 funcionalidades livres de falha e não substitui as provas nativas.

## Capturas válidas

- Cabana: etapa6/client1/1792174820869244600-capture.png.
- Catacumbas: etapa6/client1/1792174852765280400-capture.png.
- Mochila: etapa6/client1/1792174868691172900-capture.png.

Imagens pretas preservadas como tentativas inválidas. Não foram usadas para avaliar aparência ou desempenho.

## Encerramento

Sinal de parada/comandos emitidos. Conferência final por caminho exato encontrou **zero processos do executável de auditoria remanescentes**; nenhuma outra Unity/servidor foi encerrada. Ver cleanup-processes.json. Builds, dados e logs preservados.

Próxima etapa: **7, consolidação**, unindo achados, correções externas a reavaliar, reduções e pendências em fila priorizada. Os cenários não executados continuam abertos; esta campanha não é liberação geral.
