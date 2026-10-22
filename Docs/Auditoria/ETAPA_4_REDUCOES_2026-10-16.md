# Etapa 4 — reduções, obsolescência e consolidação

Data: 16/10/2026. Situação: **levantamento estático concluído**. Foram registradas **14 decisões de manutenção**, com destinos e verificações. Nenhum arquivo existente foi removido, movido ou alterado para implementar essas reduções; nenhum teste, build, Unity ou Blender foi iniciado por esta etapa.

## Resultado principal

- O projeto contém **194 grupos idênticos por hash**, considerando arquivos de pelo menos 4 KiB e excluindo `.meta`. O limite superior matemático de cópias redundantes é **357,54 MiB**. Isso **não é espaço aprovado para apagar**: muitos pares são fonte de produção e cópia importada no jogo.
- Há **7 grupos com duas cópias dentro de Assets**, somando **13,51 MiB** de bytes redundantes. Identidade do PNG não garante equivalência de importador, GUID, sprite ou função.
- `Assets/Art/Player/WalkTest` tem **3,365 MiB** e é candidato a arquivamento fora da árvore importada, sujeito à conferência final de dependências. Mover dentro do mesmo repositório não reduz seu tamanho total.
- A prancha aprovada `EnvironmentSelectionApproved.png` e seu `.meta` ocupam **2,005 MiB** em Resources. É candidata a permanecer como referência fora de Resources, com ajustes nas ferramentas que a utilizam. Não descartar a aprovação do usuário.
- `Builds/ServerValidation` foi recriada após a limpeza de 12/10 e ocupa **4,24 GiB**. É cópia/cache utilizado pelo atualizador; só é candidata a limpeza quando estiver sem uso e após preservar dados necessários. Limpá-la também remove o benefício de compilação incremental.
- As reduções mais úteis de código são coordenadas: apresentação IMGUI duplicada, compatibilidade de pesca desativada na expedição e parâmetros antigos de loot. Não equivalem a apagar classes inteiras.

**Não somar esses números como economia garantida.** Há conjuntos sobrepostos, fontes que devem permanecer, cópias fora do build e tamanhos de origem que não correspondem a bytes de Player nem à memória carregada.

## Base e método

Base: `Builds/Auditoria/2026-10-16_083542_382c7a/Projeto`, hash agregado `fcdf78077dca71119a05d701dad579149239ccedd107a3d16fe11c0ca32241cd`. Os **5.047 arquivos do manifesto foram comparados por SHA-256 ao projeto atual**. A primeira coleta não encontrou divergências; a conferência final encontrou **uma alteração externa em `Assets/Scripts/Enemies/Monster.cs`**. Arquivos novos produzidos pelas etapas da auditoria não estão nesse manifesto e não foram confundidos com fontes históricas.

O diff externo troca duas chamadas de `InVision(...,local)` por `InVision(...,local.GetComponent<PlayerInventory>())` nos avisos de investida. Foi preservado, sem incorporar a alteração à base congelada nem alegar compilação. Não muda a classificação dos candidatos de redução; a revisão que será compilada/testada na etapa 6 precisará registrar explicitamente essa diferença. Evidência adicional em `external-drift.json` e `monster-external-drift.patch`.

O coletor [Collect-ReductionInventory.py](C:/Users/Vinni/Documents/GitHub/Extlyp/Tools/Audit/Collect-ReductionInventory.py) lê metadados, hashes e textos da base; grava somente evidências em `Logs/auditoria/2026-10-16_083542_382c7a/etapa4`. Não importa nem executa código do jogo.

Foram analisados GUIDs em formatos textuais conhecidos de Assets/ProjectSettings, incluindo cenas, prefabs, assets, materiais, atlas, animações e dependências de importadores em `.meta`. A declaração do GUID no próprio `.meta` não conta como referência. Foram identificados **88 locais textuais** com `Resources.Load`, `Shader.Find` ou `AssetDatabase.LoadAsset`; isso não resolve automaticamente todas as expressões dinâmicas ou referências por caminho em outras APIs. Leituras por nome em ferramentas e documentos foram conferidas nos candidatos.

Limites: textos acima de 4 MiB, formatos não incluídos pelo coletor, dependências internas de arquivos binários, scripts externos ao projeto, reflexão e convenções de caminho podem escapar ao grafo estático. Não foi obtido um grafo nativo `AssetDatabase.GetDependencies` nem BuildReport atual. Ausência de referência é indício, não autorização para excluir.

Para disco local, a coleta soma **tamanhos lógicos**, sem ler conteúdo privado de saves/logs e sem seguir links/reparse points. Pastas locais variam enquanto ferramentas trabalham. Não mede alocação física, compressão, objetos Git, LFS remoto ou deduplicação do sistema de arquivos.

Versões verificadas: Unity 6000.0.84f1, NGO 2.13.3, Transport 2.7.4, URP 17.0.4. Skills de C#, game-ui-ux, performance-optimization e unity-build-pipeline usadas para distinguir lógica, apresentação, assets e impacto de build. Nenhuma mudança de pacote ou arquitetura foi feita.

## Inventário de tamanho

### Conteúdo da base de fontes

| Raiz / recorte | Tamanho lógico | Interpretação |
| --- | ---: | --- |
| Art | 548,78 MiB | Referências, propostas e fontes de arte; não é automaticamente conteúdo do Player |
| Prototypes | 511,63 MiB | Fontes, renders e ferramentas de produção, além de experimentos |
| Assets | 476,89 MiB | Árvore importada; tamanho de origem, não tamanho de build |
| Assets/Resources | 405,02 MiB | Subconjunto de Assets; investigar inclusão no Player antes de prometer redução |
| Assets/Resources/Environment | 346,75 MiB | Subconjunto de Resources; principal massa de arte nessa árvore |
| Assets/Art | 62,92 MiB | Fontes/propostas importadas, algumas também referenciadas em runtime |
| Prototypes/PlayerWalk3D | 190,96 MiB | Fonte e processo de animação do personagem |
| Prototypes/MonsterAnimationBatch | 199,57 MiB | Fonte e exportações, incluindo baú usado pela ferramenta de preparação |
| Art/PlayerProposals | 134,82 MiB | Propostas e fontes com versões; não tratar toda a pasta como descartável |
| Docs | 10,54 MiB | Histórico e evidências, além de documentação ativa |

Linhas com prefixos sobrepostos não devem ser somadas. Manifesto completo: 1,513 GiB registrados na etapa 1.

### Conteúdo local fora da base de fontes

| Caminho | Tamanho lógico medido | Decisão atual |
| --- | ---: | --- |
| Library | 11,18 GiB | Manter durante auditoria; cache, não código obsoleto |
| Library/Bee | 7,97 GiB | Parte de Library; investigar retenção após entrega, não limpar durante compilação |
| Builds/ServerValidation | 4,24 GiB | Limpeza condicional futura, R4-10 |
| Builds/Auditoria | 1,513 GiB | Preservar base desta auditoria até conclusão/retencão definida |
| Builds/Server | 637,52 MiB | Preservar entrega; não confundir com cópia de validação |
| Builds/Android | 129,51 MiB | Preservar entrega |
| Builds/Client | 98,04 MiB | Preservar entrega |
| .cache | 1,24 GiB | Ferramentas, incluindo Blender portátil; não classificar como lixo |
| Logs | 84,00 MiB | Evidências e possíveis dados; separar por política de retenção |
| Backups/Mapa | 16,60 MiB | Backup deliberado; manter até comparar cobertura com o estado atual |
| .utmp | 9,05 MiB | R4-11: distinguir saída de compilação de revisão de arte |

O acesso CIM a linhas de comando dos processos não estava disponível. A consulta alternativa por nome não identifica qual workspace está em uso. Portanto **não foi comprovado que as pastas estejam livres para uma limpeza**; essa conferência deve ser refeita imediatamente antes de qualquer exclusão futura. Nenhum processo foi interrompido.

## Decisões por candidato

### R4-01 — arquivar o experimento WalkTest fora de Assets

**Caminho:** `Assets/Art/Player/WalkTest`, incluindo os `.meta`: 20 arquivos, 3.528.578 bytes (**3,365 MiB**).

**Evidência/dependências:** frames `walk-01` a `walk-08`, folha e preview GIF não tiveram arestas por GUID no grafo; a busca por WalkTest e nomes específicos não encontrou consumidor nos fontes/ferramentas/documentos pesquisados. O personagem atual carrega `Resources/PlayerCharacter` dinamicamente em `PlayerCharacterVisual`, não essa pasta.

**Decisão:** candidato de confiança **alta para retirar da importação**, média para eliminar definitivamente. Preferir arquivo fora de Assets mantendo a referência do experimento. Sem apagar apenas por parecer antigo.

**Benefício:** reduz material a importar/manter no Editor; disco e repositório ficam iguais se apenas mover. Remoção definitiva teria teto de 3,365 MiB de origem. Ganho de build/memória não demonstrado.

**Antes de executar:** verificar referências nativas e confirmar que não é fonte manual de uma exportação; arquivar com manifesto/hash; preservar pares de metadata conforme o destino. **Aceite:** personagem e ferramentas atuais não dependem do experimento e o arquivo arquivado continua identificável.

### R4-02 — retirar a prancha aprovada de Resources, preservando-a como fonte

**Caminho:** `Assets/Resources/UI/Art/EnvironmentSelectionApproved.png` + `.meta`: **2,005 MiB**.

**Evidência/dependências:** sem aresta de referência direta para a imagem na configuração atual examinada. A UI utiliza camadas separadas. Entretanto `Tools/UI/Author-ApprovedEnvironmentArtwork.py:5` usa o caminho antigo, e `Separate-EntryMenuLayers.py:14` usa seu `.meta` como template. `PlayerUiAssetPreparation.cs:32` também pode adicionar toda a pasta de arte ao atlas quando o cria.

**Decisão:** confiança **alta** de que é referência/fonte a preservar; **média** para retirar do runtime sem carregar assets. Mover para Art/Docs de referência e atualizar ferramentas em conjunto. Não apagar a imagem aprovada.

**Benefício:** retirar 2,005 MiB de origem da árvore Resources e possivelmente da inclusão/empacotamento de arte; economia de Player depende de importação/atlas/BuildReport. Não há redução do repositório ao mover.

**Antes de executar/aceite:** conferir dependências de atlas e prefab na Unity, ajustar os dois scripts e manter a imagem aprovada acessível; tela de entrada continua usando as camadas corretas e o pipeline de autoria não depende do caminho antigo.

### R4-03 — consolidar sete pares de PNG dentro de Assets

**Conjuntos:** seis pares de cabin props/personagem em `Assets/Art/Cabin`/`Assets/Art/Player` e `Assets/Resources/Cabin`, mais `Assets/Art/Items/wood-icon-v1.png` / `Assets/Resources/Cabin/Firewood.png`. Lista integral e hashes em `inventory.json`. Teto de cópias redundantes: **14.168.851 bytes / 13,51 MiB**.

**Dependências que impedem exclusão direta:** o ícone de madeira em Assets/Art é referenciado por `Assets/Resources/ItemIcons.asset`; `ItemIconSetup.cs:19` constrói seus caminhos em `Assets/Art/Items`. A cabana usa nomes de Resources, inclusive `BenchBrokenAngled`. Os importadores podem ter pixels por unidade, recortes e tipos diferentes apesar de PNG idêntico.

**Decisão:** **consolidar**, confiança média. Definir fonte canônica, remapear referências e ajustar preparação de ícones antes de retirar cópias. Não mover Assets/Art inteiro para fora de Assets.

**Benefício:** até 13,51 MiB de arquivos de origem se cada par puder compartilhar uma cópia; build e RAM não determinados. Não basta trocar os GUIDs no texto sem comparar subassets/importação.

**Aceite:** ícone no inventário e prop da cabana preservam escala, recorte e material; ferramentas conseguem reconstruir o catálogo a partir da fonte canônica. A validação pertence à etapa de alteração posterior, não foi executada aqui.

### R4-04 — consolidar apresentação IMGUI duplicada da interface comum

**Caminhos:** `GameHud.cs`, `Quickbar.cs`, trechos `OnGUI` de InventoryWindow, ExpeditionMenu, GameOptionsMenu, Minimap, HudLayoutEditor, CampController e TouchControls.

**Evidência:** esses caminhos possuem saídas por `UguiGameHud.Active`; `NetworkBootstrap` instala uGUI no cliente. Porém classes mistas ainda recebem entrada e mantêm estado/serviços: Quickbar processa atalhos, InventoryWindow é controlador do inventário, Minimap gera atlas, GameOptionsMenu controla o menu e CampController mantém a base/minigames. GameHud também é adicionado por MvpSetup e pode existir serializado na cena.

**Decisão:** confiança **alta** na duplicação de apresentação; **média** para sua retirada após validação de uGUI. Migrar/remover apenas desenho comum duplicado e definir comportamento explícito se prefab uGUI estiver ausente. Preservar minigames e ferramentas de teste IMGUI enquanto forem sua interface ativa.

**Benefício:** menos regras de desenho/layout para manter. `GameHud.cs` + `.meta` têm 8.339 bytes; `Quickbar.cs` + `.meta`, 5.302 bytes, mas isso não é soma removível: há lógica ativa. Sem promessa de FPS/GC ou tamanho final de assembly.

**Aceite:** entrada, HUD, menus, inventário, mapa e retorno funcionam em ambos os temas; atalhos e toque continuam ativos; não há Missing Script; ferramentas/minigames preservados. Integrar esta decisão aos casos E3-01/E3-04/E3-05 antes da simplificação.

### R4-05 — planejar retirada coordenada da pesca desativada na expedição

**Caminhos:** campos e RPCs de pesca em `PlayerInventory.cs:19–24,153–168`, consumidores de `FishingPhase` em combate, controle, inventário, HUD, locks e laboratório; campos legados de CampJoinRequest e helpers sem efeito.

**Evidência:** `RequestFishing` não executa ação e Reel RPC está vazio; comentário mantém compatibilidade de rede/cena/diagnóstico. Estado ainda é consultado por diversos componentes. Em paralelo, `CampActivity` utiliza FishingGame e FishingPresentation para a pesca da cabana; esses sistemas são ativos.

**Decisão:** confiança **alta** na desativação da pesca em expedição; remoção **coordenada**, não isolada. Mapear API, dados, diagnóstico e protocolo antes. Métodos vazios como `CampRepository.Apply(CampJoinRequest)` devem ser avaliados junto aos chamadores e testes, preservando a rejeição de comandos antigos.

**Benefício:** menos estados e verificações sem função atual. Não quantificado: linhas removidas não se convertem diretamente em memória/tráfego; campos privados de rede e frequência de alteração precisam de medição.

**Aceite:** clientes/servidor compatíveis com protocolo deliberadamente atualizado quando necessário, nenhum diagnóstico usa API antiga, e pesca/cozinha da base continuam intactas. Não remover IDs de peixes nem renumerar enums.

### R4-06 — retirar parâmetros de loot substituídos da configuração ativa

**Caminho:** `GameplayDefinition.cs:14–18,35–38` e campos correspondentes em `Assets/Resources/Gameplay.asset`.

**Evidência:** sete campos antigos de chance/quantidade foram marcados como legados. As buscas encontraram declaração, validação e serialização; sorteio atual usa LootTables/LootTableDefinition/LootRoller. `GameplayDefinition.Valid` ainda considera esses valores, embora eles não controlem o novo sorteio.

**Decisão:** **remover ou ocultar/migrar configuração legada**, confiança alta para obsolescência funcional desses sete campos. Manter os demais parâmetros ativos, incluindo receitas, encontros, chance de mímico e escala de loot.

**Benefício:** evita editar valores sem efeito e reduz ambiguidade do Inspector/validação. Economia de bytes é pequena e não foi apresentada como relevante.

**Aceite:** somente tabelas novas controlam loot; assets antigos recebem política explícita de migração/limpeza; validador e documentação concordam com a fonte vigente de balanceamento. Não substituir todo GameplayDefinition nem apagar Gameplay.asset.

### R4-07 — arquivar ou atualizar guias que ainda parecem atuais

**Caminhos:** `CAMP_ITEMS.md` e trechos dos guias de requisitos; usar README/SERVER_SETUP e documentos recentes de integração como entrada para estado atual.

**Evidência:** CAMP_ITEMS descreve protocolo 4, doze pontos fixos de loot, pedidos econômicos na entrada e progresso perdido ao encerrar salas, divergindo da base auditada. Tem **3.613 bytes**. BOTS_SETUP é mais recente e útil, mas sua introdução fixa cinco participantes, enquanto Catacumbas usam oito. README ainda aponta EXPANSION_SETUP como design/histórico; não deve ser removido sem rever esse link.

**Decisão:** confiança **alta** na necessidade de atualização ou rótulo histórico. Arquivar guias antigos com indicação de substituto; não editar retroativamente fatos de uma entrega datada como se fossem validação atual. A25 permanece uma questão de navegação documental, não apenas tamanho.

**Benefício:** reduzir instruções conflitantes; bytes de disco irrelevantes. Mover sem excluir não reduz repositório.

**Aceite:** guia operacional tem uma entrada clara; protocolo, capacidade por mapa, save e economia concordam com o código; referências históricas continuam consultáveis e links não ficam quebrados.

### R4-08 — consolidar e atualizar provas antigas antes de utilizá-las

**Caminhos:** `Tools/Audit/AuditProbe.cs`, CookingProbe, CookingInventoryProbe, FishingProbe e respectivos runners; verificadores de Assets/Editor consultados por nome de método/menus.

**Evidência:** o runner AuditProbe lista fontes reais antigas, mas não inclui várias novas dependências de inventário/equipamento/progressão. Os adaptadores replicam tipos Unity e têm versões próprias de dados. Isso torna necessária sua adaptação; não foi executado compilador para afirmar qual diagnóstico ocorreria. As provas de comandos forjados já contêm expectativas de rejeição: não foram classificadas equivocadamente como vulnerabilidades ainda esperadas.

**Decisão:** **consolidar infraestrutura e atualizar contratos**, confiança alta. Reaproveitar casos que verificam regras reais; manter diferenças de serialização explícitas. Checks invocados por `-executeMethod`, menus ou reflexão não são mortos por não terem referências de GUID.

**Benefício:** menos fixtures divergentes e execuções repetidas; tamanho/frequência de build não medidos. Não apagar todos os verificadores como limpeza.

**Aceite:** na etapa 5, runner documenta fontes reais incluídas, versão de adaptadores e casos; rejeições e invariantes refletem o jogo atual. Serialização/NGO/física não são simulados como se fossem Unity. Relaciona-se a A22/A23; nenhuma prova foi rodada nesta etapa.

### R4-09 — manter fontes de arte; arquivar intermediários apenas com cadeia de reprodução

**Caminhos:** Art, Prototypes, Tools de preparação e Blender. Destaques: PlayerWalk3D **190,96 MiB**, MonsterAnimationBatch **199,57 MiB**, Art/PlayerProposals **134,82 MiB**.

**Evidência:** `Prepare-ChestAnimation.py` usa `Prototypes/MonsterAnimationBatch/Chest`; scripts de pisos/detalhes/arquitetura/extração usam `Art/Catacumbas/...`; fontes de caminhada/recarga possuem exportadores. Há duplicata exata de 4.913.980 bytes entre `WarriorArtV3/baseline-v2/warrior_v2.blend` e `working-v3/warrior_v3.blend`; nomes representam papéis de baseline e trabalho, não prova de que um possa sumir.

**Decisão:** **manter** fontes e entregas aprovadas; investigar quais renders/previews/intermediários podem ir para arquivo externo depois de documentar entradas/saídas e reproduzir exportação. Confiança alta na necessidade das pastas; baixa para remoção em bloco. Nenhuma revisão de geometria/cena Blender foi feita nesta tarefa.

**Benefício:** arquivo fora do checkout pode reduzir disco/volume de fontes ativas, mas não Player: essas pastas estão fora de Assets. Dentro do mesmo repositório, mover só organiza. O total de 357,54 MiB de duplicatas inclui cópias intencionais desse processo.

**Aceite:** cada sprite/atlas aprovado tem fonte editável e receita recuperável; nada depende de uma pasta arquivada sem índice. Não perder prompts, licenças, referências aprovadas ou imagens necessárias à autoria.

### R4-10 — limpeza condicional da cópia ServerValidation

**Caminho:** `Builds/ServerValidation`: 45.232 arquivos e **4.546.819.388 bytes / 4,24 GiB** na conferência final. A primeira leitura tinha 45.104 arquivos / 4.547.208.690 bytes; a pasta mudou durante o levantamento, reforçando que não é um alvo parado ou comprovadamente livre para limpeza.

**Evidência/dependências:** `Update-Server.ps1:23` usa essa cópia para não disputar o projeto aberto; reutiliza cache e gera saída antes de promover para Builds/Server. A pasta foi recriada desde a limpeza registrada em 12/10.

**Decisão:** candidato **alto para recuperação de disco quando sem uso**, não código obsoleto. Não incluir Builds/Server, Client, Android ou Auditoria. Não limpar imediatamente antes da campanha agrupada se isso apenas provocar reimportação e repetição de trabalho.

**Benefício:** até 4,24 GiB lógicos locais; **zero redução comprovada do repositório, Player ou RAM**. Custo: perda do cache e tempo de reconstrução, ainda não medido.

**Antes de executar/aceite:** comprovar processos sem uso, validar caminho absoluto dentro de Builds, recusar links, preservar logs/saves/builds necessários e conferir que o fluxo recria a cópia. A indisponibilidade de linhas de comando nesta sessão impede afirmar que a exclusão já seria segura.

### R4-11 — separar temporários, cache de ferramentas e evidências

**Caminhos:** `.utmp` (**9,05 MiB**), Library (**11,18 GiB**), `.cache` (**1,24 GiB**), Logs (**83,79 MiB**), Backups/Mapa (**16,60 MiB**).

**Evidência:** `.utmp` mistura Debug (~4,41 MiB) e `warrior-v2-review` (~4,60 MiB), além de ferramentas. Library/Bee é cache de compilação. `.cache/blender` é instalação portátil útil. Logs/Backups podem ser a única cópia de evidências/dados e o baseline de auditoria está deliberadamente protegido.

**Decisão:** **investigar retenção por subpasta**, confiança alta na diferença de papéis; baixa para exclusão em bloco. Não somar o tamanho inteiro como lixo recuperável. `.utmp` e fontes não rastreadas também precisam de política de versionamento/ignore consciente.

**Benefício:** apenas disco local para caches; nenhum ganho de build final/RAM garantido. Apagar Library custa reimportação e prejudica o reaproveitamento planejado.

**Aceite:** classificar dados privados, evidências, revisões únicas e conteúdo recriável; guardar manifesto e local de recuperação; limpar só a seleção autorizada quando fora de uso. Não ampliar `.gitignore` para esconder trabalho existente como forma de limpar.

### R4-12 — manter ferramentas de autoria, restringindo reconstrução inicial

**Caminhos:** `Assets/Editor/MvpSetup.cs`, PlantSetup, EnvironmentSetup, ItemIconSetup, PlayerUiAssetPreparation e scripts Tools de produção.

**Evidência:** MvpSetup cria cena/prefabs e referencia ShapeVisual/GameHud; não é manutenção pontual. Outros preparadores geram catálogos, importadores, fontes e atlas atualmente utilizados. PlayerUiAssetPreparation ainda tem inicialização automática e busca prefabs da UI.

**Decisão:** **manter/restringir ou arquivar a montagem inicial em trabalho separado**, confiança alta. Não executar MvpSetup para testar a limpeza nem apagar ferramentas por não existirem em runtime. Se separar ferramentas de bootstrap do fluxo atual, rever chamadas, menus e guias em conjunto.

**Benefício:** menor risco de sobrescrever assets e caminho operacional mais claro. MvpSetup + metadata têm somente 9.595 bytes; economia de espaço não é motivação.

**Aceite:** autoria necessária continua disponível e a manutenção normal não reconstrói cena/prefabs. Métodos de build/teste não dependem implicitamente da montagem inicial.

### R4-13 — investigar pacotes opcionais sem alterar os essenciais

**Caminhos:** Packages/manifest.json e packages-lock.json. Candidatos de investigação: Visual Scripting, Timeline, IDE não utilizado, Collab Proxy e ferramentas de multiplayer/editor, conforme uso real da equipe.

**Evidência:** não foram encontrados usos diretos de `Unity.VisualScripting`, `UnityEngine.Timeline`, `PlayableDirector` ou `ScriptMachine` nos scripts pesquisados, nem arquivos `.playable`, `.vfx`, `.assetgraph` ou `.asmdef` na busca por nomes de Assets. Isso não cobre menus, dependências transitivas, estado do Editor ou ferramentas externas. `feature.2d` também é agregador, não lixo presumido.

**Decisão:** **investigar**, confiança baixa para remoção sem resolução de dependências. Manter NGO/Transport/URP/Input System/uGUI/TextMeshPro e módulos usados. Test Framework/PlayMode podem servir à etapa 6. Não atualizar Unity nem substituir rede para reduzir pacotes.

**Benefício:** possível redução de cache/importação/dependências; zero ganho quantificado de Player/RAM. O tamanho de Library/PackageCache não é economia automaticamente atribuível aos candidatos.

**Aceite:** lista explícita de funcionalidades usadas, revisão de dependências e comparação de resolução/compilação na cópia; build e ferramentas essenciais funcionam após remoção proposta. Mudança só em manutenção posterior, com rollback do manifesto/lock.

### R4-14 — investigar separação de conteúdo visual no servidor

**Caminhos:** ServerBuild usa Player Windows comum para servidor e cliente; grande parte da arte está em Resources (**405,02 MiB de origem**), sobretudo Environment (**346,75 MiB**).

**Evidência:** `ServerBuild.cs:59–71` mantém `StandaloneBuildSubtarget.Player`; servidor usa batchmode/nographics em runtime. Evitar carregar/desenhar em batchmode não demonstra exclusão do conteúdo do executável nem alocação de toda a arte em memória.

**Decisão:** **investigar como trabalho de pipeline**, confiança média na oportunidade, baixa no ganho até BuildReport. Pode envolver inclusão por alvo, carregamento e arte por contexto. Dedicated Server é opção arquitetural separada, não requisito implícito ou módulo a instalar nesta auditoria.

**Benefício:** potencial de distribuição/disco do servidor; teto de origem não é previsão de economia comprimida. Não prometer RAM ou FPS sem medir carregamento real.

**Aceite:** comparar relatórios e execução de rede conservando prefabs/IDs/componentes exigidos por NGO; cliente recebe toda arte necessária e servidor permanece autoritativo. Não mover Resources para fora do build sem revisar os 88 locais de carregamento e suas expressões dinâmicas.

## Proteções contra falsos candidatos

| Conteúdo | Evidência de uso / motivo para manter |
| --- | --- |
| ShapeVisual | GUID referenciado por Player, Bot, Monster, Loot e Esporifero; não é classe sem uso |
| InputSystem_Actions.inputactions | Referência em EditorBuildSettings, mesmo com GameInput criado por código |
| URP2DSceneTemplate.unity | Referência em Lit2DSceneTemplate.scenetemplate; template não é cena de jogo esquecida |
| `Environment/AbandonedProps.mat` e `.png` | Compartilham chave de Resources, mas são carregados por tipos diferentes em EnvironmentSprites; não são duplicata indevida por nome |
| PanelSimple, Wind e PlayerUi.spriteatlas | Podem não ter arestas diretas por arquivo, mas UiSprites monta nomes e a preparação usa pasta de arte; não remover por grafo vazio |
| TrainingHead a TrainingRing | IDs legados 26–31 são persistidos; retirar nomes/renumerar deslocaria os demais IDs. Reservar valores e planejar migração antes de aposentá-los |
| FishingGame/FishingPresentation/CampActivity | Pesca da cabana ativa; não confundir com a expedição desativada |
| Arquivos partial | Integram classes principais mesmo sem referência pelo nome do arquivo |
| Catálogos, atlas, fontes, shaders e JSONs de arte | Consumidos por Resources e manifestos dinâmicos, inclusive arrays de nomes e pastas LoadAll |
| Docs de auditoria, integração e imagens do usuário | Evidência datada; nunca atualizar retroativamente como se comprovasse a versão atual |
| Builds/Auditoria e entregas existentes | Baseline necessário às etapas seguintes e artefatos entregues, sem autorização de limpeza nesta tarefa |

## Reavaliação da limpeza de 12/10

Os caminhos removidos SampleScene e ExpeditionFishingUI continuam ausentes. SkillsValidation, SkillsBaseline, ItemsTest, ItemsFinalTest e RenderCheck também estavam ausentes; não há novo ganho a contabilizar nesses nomes. Os protótipos antigos continuam no arquivo histórico previsto, em vez de suas antigas pastas de origem. Evidência de existência em `previous-cleanup-paths.json`.

ServerValidation é a exceção relevante: foi regenerada, pois o atualizador ainda a utiliza. Não foi somada novamente a estimativa antiga de 20,18 GiB recuperados. A redução de 12/10 é história; a tabela desta etapa contém a medição atual.

A22/A23: provas antigas requerem adaptação, mas não remoção em bloco. A24: parâmetros legados de loot ainda geram ambiguidade apesar da adoção de tabelas. A25: CAMP_ITEMS e capacidades precisam de navegação documental clara. Não houve execução dos verificadores para fechar esses achados históricos.

## Sequência sugerida para manutenção posterior

1. Preservar fontes e evidências; confirmar referências dos dois candidatos de arte R4-01/R4-02 e da deduplicação R4-03.
2. Concluir provas da auditoria antes de simplificar estados de rede ou UI. Corrigir os P1 da etapa 2 em trabalhos próprios, sem misturar economia/segurança com limpeza de espaço.
3. Atualizar guias e adaptadores; registrar decisões de compatibilidade e fonte canônica de configuração.
4. Consolidar apresentação duplicada e compatibilidade de pesca em alterações pequenas, com testes dos fluxos afetados e protocolo coordenado.
5. Só então considerar limpeza local seletiva e investigação de pacotes/build do servidor, comparando benefícios reais.

## Evidências e próxima etapa

Evidências locais: `inventory.json`, `summary.json`, `process-usage.json` e `previous-cleanup-paths.json` em `Logs/auditoria/2026-10-16_083542_382c7a/etapa4`. O coletor é fonte versionável em Tools/Audit; arquivos de Logs são ignorados pelo Git e precisam de retenção própria. O relatório não substitui seus manifests.

Próxima etapa: **5 — provas agrupadas das regras C# fora da Unity**, na base isolada e com adaptação dos probes antes de executá-los. Nenhuma das 14 decisões autoriza exclusão automática ou afirma ganho de desempenho. Limpeza/remoção continua sendo trabalho posterior à auditoria.
