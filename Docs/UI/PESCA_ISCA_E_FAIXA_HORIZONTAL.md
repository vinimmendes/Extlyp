# Pesca — isca, visitantes e faixa horizontal

Implementação de 24/10/2026, após aprovação da prévia Blender v03. A revisão do usuário substitui a disputa de tensão por manter o peixe dentro de uma faixa horizontal. Segurar move a faixa para a direita; soltar move para a esquerda. Não há tensão, desgaste ou rompimento de linha nesta versão.

## Fluxo entregue

Abrir pesca no píer reparado → escolher Sem isca ou Minhoca → lançar manualmente → espera de 1–2 s → visitante aproxima/investiga → recusa e sai, ou morde → jogador precisa pressionar para fisgar em até 1,6 s → disputa de faixa → resultado confirmado pelo servidor → novo lançamento manual ou fechar.

Puxar antes da mordida espanta o visitante sem consumir a isca. Manter o botão antecipadamente não fisga automaticamente. Um novo peixe é sorteado a cada visita, após outra espera curta. Cancelar antes da mordida não gasta minhoca. A mordida consome exatamente uma unidade, mesmo se o jogador perder a fisgada ou a disputa. O nome da espécie é apresentado no resultado.

Entrada: toque/mouse no mesmo retângulo usado para desenhar o controle, Espaço ou A do controle. Na seleção, setas esquerda/direita ou direcional do controle escolhem a isca; a ação principal lança. Ao perder foco ou abrir outro menu, libera o recolhimento e exige soltar o botão antes de uma nova entrada. Os controles congelam quando deixam de receber snapshots recentes; nenhuma captura é inventada no cliente.

## Dados e economia

- `Worm` foi acrescentado ao final de `ItemId`, ID 62; os IDs antigos não foram renumerados. Pilha máxima 20; nome, ícone de interface/mundo e descrição próprios.
- Floresta e Catacumbas: somente tabelas de recipientes comuns, Suprimentos, peso 10, quantidade 1–3, todos os temas. Preservados presença, pilhas e demais entradas. Não foram adicionados compra, venda, fabricação ou drops de monstros.
- Sem isca: comum 75%, grande 15%, raro 10%. Com minhoca: comum 60%, grande 15%, raro 25%. Probabilidade aplicada por visitante; mordida independente de 70%.
- Estoque da base inclui mochila e baú, seguindo a política existente `CampProgress.Take` (baú primeiro). A seleção fica travada durante o lançamento; estoque ausente nunca muda silenciosamente para Sem isca.
- Antes de lançar, o servidor exige capacidade na mochila para qualquer uma das três capturas possíveis. Captura não é enviada ao baú e sucata deixa de ser recompensa de pesca.
- Configuração em `Assets/Resources/Fishing/Definition.asset`: espera, mordida, janela de fisgada, limites, perfis e probabilidades. Perfis: faixa 28%/23%/20%, ganho 0,19/0,11/0,105 por segundo dentro da faixa para comum/grande/raro; perda 0,12 fora. Limites de 20 s na disputa e 90 s por lançamento. Tempos reais dependem da habilidade e aguardam avaliação prática.

## Servidor e compatibilidade

O servidor mantém o estado de cada tentativa, sorteia o visitante e a mordida, valida os comandos, consome a isca e entrega uma captura uma única vez. O consumo incrementa a revisão do perfil e é salvo no instante da mordida, pelo fluxo de persistência já existente. Cancelamento/desconexão posteriores não devolvem a unidade; erro de salvamento encerra a atividade. Estado transitório não é salvo em ScriptableObject.

Comandos novos foram acrescentados ao final de `CampCommandKind`: seleção de isca, lançamento e fisgada. `Reel/Held` permanece como entrada contínua. Snapshot informa fase detalhada, visitante, idade/duração da fase, isca, consumo, resultado e `FishingBar` explícito. O campo antigo `Tension` não foi reinterpretado como a nova posição.

Protocolo de rede **45** (antes 44). Cliente e servidor precisam de versões compatíveis para validar esta mudança. Não houve build, publicação, promoção ou reinício de servidor; canais estável e desenvolvimento permanecem intactos. Perfis existentes continuam na mesma versão de schema, pois a alteração do catálogo apenas acrescenta um ID.

## Arte e apresentação

Sprites originais renderizados no Blender 5.2.2 LTS a partir de `Art/Fishing/Preview-v03/fishing-preview-v03.blend`, câmera ortográfica, exportação em processo separado sem modificar a cena aprovada. Peixe e boia usam atlas de 12 quadros; água/píer, ondulação, carretel e minhoca são camadas independentes. A Unity posiciona peixe/boia, linha e ondas a partir das fases autoritativas. Não é reprodução do storyboard como um vídeo.

Recursos em `Assets/Resources/Fishing`. Autoria/exportação: `Tools/Blender/ExportFishingGameSprites.py`; empacotamento e metadados: `PackageFishingGameSprites.py`. Fontes, cores e controles seguem `HudTheme` e a área segura de `UiLayout`. Simples e Sombria compartilham posições, barra horizontal e regras. Não foram recriados cena/prefabs com `MvpSetup.Build`.

## Evidência e pendências de validação

Revisão estática das transições, consumo/revisão do perfil, concessão única, catálogo, tabelas e recursos. PNGs isolados do Blender inspecionados visualmente; atlases preservam transparência e quadros separados. `git diff --check` executado nas alterações pertinentes.

**Não executados:** compilação, testes automatizados, Play Mode, clientes separados, aparelho Android ou builds de validação, conforme preferência do projeto. Esta entrega não equivale a validação em execução nem aprovação automática de fidelidade visual na Unity.

As chamadas do verificador de atividades dentro de `Assets/Editor/ImplementationChecks.cs` foram adaptadas à nova API, sem executá-lo. Probes históricos fora da Unity (`Run-FishingProbe`, `Run-CookingProbe`, `Run-CookingInventoryProbe` e Stage5 contra fontes atuais) usam adaptadores/expectativas anteriores; seus resultados antigos não validam este rework e seus adaptadores precisam acompanhar as novas fases antes de serem usados nas fontes atuais.

Primeira avaliação prática: selecionar/zerar minhocas; verificar recusa sem gasto e mordida com gasto; perder e acertar fisgada; acompanhar faixa horizontal; concluir/falhar/cancelar; reconectar após mordida; conferir mochila cheia, dois temas, safe area e toque capturado até soltar. Não iniciar novas rodadas de testes sem solicitação ou problema relatado.
