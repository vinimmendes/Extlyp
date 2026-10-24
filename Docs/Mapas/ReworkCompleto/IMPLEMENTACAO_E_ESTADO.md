# Catacumbas — implementação e estado da produção

As oito pranchas revisadas e as ideias das 24 salas foram aprovadas pelo usuário. A autoria implementada usa esses arquivos como referências. Uma sala com afastamentos pendentes permanece em produção; não é encerrada por conter objetos com os mesmos nomes.

A implementação e a revisão estática foram concluídas, com publicação local dos dados e da arte para o jogo. Identificação atual: `eca1a6cdd1f3670564eab094ec7f12caf4090c1c1d7c8afb64d3bbf68e106439`. A primeira avaliação prática continua pendente do usuário.

**Orientação posterior do usuário — manter por enquanto:** a versão atual permanece como está. O usuário identifica diferenças visuais claras em relação aos conceitos; o encerramento da revisão estática acima não significa aprovação de equivalência visual. Permanece registrado o objetivo de aproximar o resultado no jogo dos conceitos aprovados, especialmente em profundidade da arquitetura, desgaste, composição e integração dos detalhes. Retomar esse objetivo quando houver trabalho pertinente no visual das Catacumbas, comparando, identificando causas, replanejando e corrigindo os desvios. Este registro não inicia outra rodada de alterações nem substitui os conceitos pela montagem atual.

## Abrir e editar

1. Na Unity **6000.0.84f1**, fora do Play, abrir **Cyber Extraction > Mapas > Catacumbas — autoria fora do Play**.
2. Abrir a cena de autoria pela janela. Fonte: `Assets/Scenes/Catacumbas_Autoria.unity`.
3. Escolher sala e aba. A paleta oferece busca por nome, setor, família e estado, colocação direta, variantes completas e conjuntos desmontáveis.
4. Sprites usam as ferramentas normais da Scene View e Inspector para mover, girar, espelhar, dimensionar, duplicar e remover. Undo/Redo acompanha a edição. Novos sprites persistentes do Project podem ser registrados na paleta.
5. Pintar terreno físico na aba própria. Paredes pintadas recebem sua pegada de pedra. Bases classificadas como cobertura baixa são colocadas com uma pegada vinculada, alinhada à grade; os demais objetos decorativos não recebem colisão automática. Para mover uma cobertura completa, selecionar seu pai “mover cobertura/conjunto”. Dimensões e rotações da pegada devem continuar na grade.
6. Selecionar uma extração e usar sua alça ou o campo “Mover extração”. O conjunto inclui seus pontos vinculados. O canal leva margens, comporta, embarque, barca e ponto final do percurso. Escadaria e poço também têm seus conjuntos.
7. **Salvar** conserva a cena nativa, inclusive trabalho incompleto. **Publicar mapa** compila e verifica a definição usada pelo jogo; erros e desvios pendentes impedem substituir a última publicação.

Os tiles, sprites e estados funcionais estão em `Assets/Resources/Environment/Dungeon/ReworkComplete/CatacombArt.catacombkit`. As imagens de produção ficam ao lado. Fontes pintadas e prompts estão em `Art/Catacumbas/ReworkCompleto/Fontes` e nos JSONs de prompts da mesma árvore. Os recortes e metadados são descritos em `CatacombArt.catacombkit` e reproduzidos por `Tools/Author-CatacombComplete.py`.

Os prefabs da produção atual ficam na subpasta indicada por `PrefabFolder` na raiz de autoria. Cada revisão gera outra pasta; prefabs anteriores e eventuais edições não são sobrescritos. A paleta mostra os conjuntos da revisão atual. O banco de estados oferece também variantes intactas/quebradas, mecanismos e peças móveis, inclusive estados não visíveis na montagem inicial.

A cena `Assets/Scenes/Catacumbas_Referencia_Encaixes.unity` reúne vizinhanças e acabamentos. Não requer Play. As transições de piso usam máscara de encaixe e material em escala global contínua; girar ou espelhar o tile modifica a máscara, preservando a fase compartilhada do material. Os sete materiais `Surface_*.mat` são persistentes e editáveis no Project. A família de piso alagado é independente da superfície do rio.

## Dados e publicação

A cena é a fonte editável. `DungeonAuthoringCompiler` captura células, salas, regiões, superfícies, composições e pontos funcionais. A publicação gera `Assets/Resources/Environment/Dungeon/ReworkComplete/Published.json` por substituição atômica. Antes dessa substituição, conserva uma cópia própria dos sprites, tiles, texturas, materiais e shaders usados em `Publications/<revisão>/Catalog.asset`. Rascunhos inválidos e alterações nos assets de autoria não substituem nem modificam a última publicação. A próxima publicação reúne novamente dados e arte.

Partes de extração têm identidades vinculadas preservadas. Para retirar sua aparência, usar `Hidden` no componente de autoria; apagar uma parte obrigatória mantém o rascunho salvável, mas bloqueia a publicação. Isso evita que uma peça apagada reapareça silenciosamente pela apresentação padrão do jogo.

`DungeonLayout` e `DungeonContent` consultam essa definição para física, navegação, coberturas, minimapa, loot, encontros e extrações. A apresentação usa as mesmas posições e o catálogo persistente. Cliente e servidor comparam a identificação do conteúdo no ingresso; o servidor continua autoritativo e não aceita uma autoria enviada pelo cliente. Os outros mapas mantêm seu fluxo anterior.

Alterações em arte, materiais e fontes invalidam a identificação do catálogo. Mudanças numa sala invalidam sua comparação. A revisão precisa ser refeita antes de publicar. A rotina de produção também recusa sobrescrever uma cena editada desde sua última captura, gerando arquivos de conflito para conciliação.

Os 17 ajustes anteriores foram preservados com metadados originais e representações editáveis em arquivo inativo na cena, além do asset original intacto. A janela mostra suas correspondências e permite recuperar peças. Não são reaplicados por cima do rework. Relatório: [MIGRACAO_EXECUTADA.txt](MIGRACAO_EXECUTADA.txt).

## Etapas e encerramento

| Etapa | Entrega | Estado |
| --- | --- | --- |
| 1 — Referência | Oito conceitos aprovados, ideias, proporções e fichas por sala | Registrada |
| 2 — Dados | Máscaras atuais, 24 salas e pontos funcionais compartilhados | Publicados |
| 3 — Editor | Cena isolada, paleta, autoria física/visual, Undo, salvar/publicar e estados | Implementado; avaliação prática pendente |
| 4 — Kit | 188 paredes, 557 tiles de piso/transição, peças e estados persistentes | Produzido e disponível na paleta |
| 5 — R17 | Ossuário, ruptura, abrigo abandonado e urnas funcionais; leito corrigido | Montagem editável e comparação revisadas |
| 6 — R18 | Raízes, nichos, vestígios e conjunto real do poço | Montagem editável e comparação revisadas |
| 7 — Galerias | R08, R09, R14 e R15: armazenamento, apagamento, cortejo e relevos | Montagens e comparações revisadas |
| 8 — Claustro | R16 e R20: apoios, cera, tecidos e circulação | Montagens e comparações revisadas |
| 9 — Prisão | R10 e R11: grades, fixações, permanência e chave real | Montagens e comparações revisadas |
| 10 — Capela | R21 e R22: rito, altar nordeste, piso e armazenamento violado | Montagens e comparações revisadas |
| 11 — Contrapesos | R19: roldanas, ligações, ferragens e trabalho interrompido | Montagem editável e comparação revisadas |
| 12 — Cisternas | R12, R13 e R23: depósitos, grelhas, cais e transporte | Montagens e comparações revisadas |
| 13 — Entradas e conexões | R00–R07, passagens e continuidade dos materiais | Montagens e comparações revisadas |
| 14 — Fechamento | 24 revisões estáticas, migração preservada e cópia publicada da arte | Publicação concluída; avaliação prática pendente |

[Fichas e peças por sala](FICHAS_DE_PRODUCAO.md) · [Galeria de montagens nativas](../../../Art/Catacumbas/ReworkCompleto/Galeria-montagens-Unity.png) · [Registro de comparação](../../../Art/Catacumbas/ReworkCompleto/Comparacoes/revisao.json).

A revisão, com causas e ajustes por sala, está em [REVISAO_MONTAGEM_ATUAL.md](REVISAO_MONTAGEM_ATUAL.md). O kit atual reúne 912 entradas principais e 19 adicionais/migradas; a montagem tem 580 peças decorativas, além de terreno, pontos e conjuntos funcionais. As peças recentes de arte foram produzidas com imagegen integrado; os prompts e as fontes locais estão preservados junto à produção. As aplicações das correções de escala e dos componentes reais estão registradas em [TRADUCAO_DOS_CONCEITOS.md](TRADUCAO_DOS_CONCEITOS.md).

O registro de comparação identifica a captura por SHA-256. A rotina de publicação da produção exige 24 revisões resolvidas e autoria idêntica à captura; ela não aprova as salas automaticamente. A revisão estática foi registrada pelo Codex após examinar as comparações. Mudanças posteriores reabrem a revisão; enquanto houver pendências ou erro de compilação, permanece a última versão publicada. O registro não substitui a avaliação prática do usuário.

## Evidência e limites

Correções posteriores aos relatos do teste prático: [coberturas, cais e apoios de vigília](CORRECOES_DE_SPRITES_2026-10-24.md). A correção de apresentação dos apoios conserva a definição publicada e passa a valer também na prévia da autoria; as imagens originais da produção permanecem como registro histórico.

As cenas, sprites, tiles, materiais e prefabs foram produzidos na Unity instalada, fora do Play. As imagens de `MontagensUnity` são capturas nativas estáticas com os pontos e componentes funcionais reais. Não são provas de funcionamento em partida, iluminação na câmera do jogador ou desempenho.

A implementação inicial não incluiu testes automatizados, sessões de Play ou builds de verificação. Depois dos relatos do usuário, foram feitas verificações pontuais de montagem e capturas isoladas para os sprites afetados, conforme a preferência registrada. A avaliação prática de salvar/reabrir, edição das variantes, colisão e navegação, minimapa, estados das urnas, escadaria, poço, comporta, barca e incompatibilidade de conteúdo continua sendo do usuário. Qualquer desvio encontrado reabre causa → replanejamento → ajuste → comparação. Nenhuma cena de partida foi substituída, nenhum asset antigo foi apagado e `MvpSetup.Build` não foi executado.
