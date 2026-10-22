# Revisão de arquivos obsoletos — 12/10/2026

Revisão estática de fontes, referências por nome/GUID em Assets, ProjectSettings, Packages e Tools, configurações de build e tamanho das pastas locais. Não houve exclusão, execução de testes, Unity ou builds. Ausência de referência estática é indício, não prova de que um arquivo nunca é usado por ferramentas externas ou carregamento dinâmico.

## 1. Candidatos concretos para remover ou arquivar

| Arquivo/pasta | Evidência | Recomendação |
| --- | --- | --- |
| Assets/Scenes/SampleScene.unity + .meta | Sem referência de GUID encontrada; só Game.unity está na lista de cenas do build. | Remover o exemplo do template se não for usado manualmente. |
| Assets/Scripts/UI/ExpeditionFishingUI.cs + .meta | Awake desativa o componente; não foram encontrados consumidores por nome nem referências por GUID. Pesca da expedição desativada no PlayerInventory. | Candidato a remoção. Preservar a pesca da cabana e FishingPresentation. |
| SETUP_PASSO2.md | Orienta fluxo Host, editar PlayersToStart e recriar a cena com Montar MVP; incompatível com operação atual por servidor dedicado. | Arquivar em Docs/Historico; deixar SERVER_SETUP como guia operacional. |
| Prototypes/BotsIA_v2_Source | Cópia do ZIP anterior à adaptação, fora de Assets. Seu código não representa a integração atual. | Arquivar como referência; não copiar novamente por cima da IA atual. |
| Prototypes/PlayerWalk | Proposta 2D anterior ao personagem 3D aprovado. | Arquivar como experimento anterior; não participa do runtime Unity. |
| Prototypes/LargeMapPreview, Prototypes/Lockpick, Sites/hud-preview | Demonstrações/propostas separadas da execução Unity. | Arquivar se não forem mais consultadas. Não confundir com assets do jogo. |

## 2. Arquivos ativos com conteúdo ultrapassado

### Assets/Editor/TestLabChecks.cs — prioridade alta

- Linhas 41–43 exigem um armário, mesa e cama em todas as ruínas. Isso contradiz as variantes atuais de RuinLayouts.
- Linha 49 exige CircleCollider2D e WallLayer em todas as árvores. A floresta atual inclui árvores visuais e obstáculos baixos que não bloqueiam visão.
- Linhas 53–55 verificam móveis por coordenadas fixas sem transformação da orientação da ruína.
- O método Build ainda encadeia essas verificações antes do build. Portanto, mantê-las desatualizadas pode bloquear esse fluxo com falhas que não representam defeitos do jogo.

Atualizar expectativas e cobertura, não apagar toda a ferramenta. Os checks de acesso ao laboratório e equipamento continuam úteis.

### Documentação e evidências

- GDD.md:9 ainda descreve mapa 900 x 1380; o código atual usa 400 x 600 e 600 segundos. Há conteúdo histórico explicitado mais abaixo, mas a apresentação inicial ainda pode induzir erro.
- ALTERACOES_CONTROLES_NAVEGACAO.md:21 usa PlayersToStart=1 como procedimento de teste antigo. Separar histórico dos controles ainda válidos.
- Tools/Run-SkillsSmoke.ps1 grava `protocol=11` no resultado, embora o código atual configure 17. O número da evidência está ultrapassado; isso não significa que o roteiro inteiro seja descartável.
- Docs/Auditoria/*.json e Docs/Auditoria/Evidencias registram versões anteriores. Preservar como evidência datada; não apresentar como validação das alterações atuais.
- Docs/Integracao com protocolos 14, 15 e 16 descrevem entregas passadas. Não atualizar números retroativamente: manter histórico e consultar SERVER_SETUP para o protocolo atual.

## 3. Espaço local recuperável — não é código obsoleto

Medições em MiB, somando tamanho lógico dos arquivos; espaço efetivamente liberado pode variar.

| Pasta | MiB aproximados | Uso |
| --- | ---: | --- |
| Builds/SkillsValidation | 9411,7 | Cópia isolada de validação; pode ser recriada. |
| Builds/ServerValidation | 9146,0 | Cópia de compilação usada por Update-Server; pode ser recriada, com custo de reimportação. |
| Builds/SkillsBaseline | 1812,6 | Baseline anterior da auditoria; preservar evidências necessárias antes de limpar. |
| Builds/ItemsTest | 98,4 | Build de teste anterior. |
| Builds/ItemsFinalTest | 98,4 | Build de teste anterior. |
| Builds/RenderCheck | 98,4 | Build de inspeção visual anterior. |
| .cache/blender | 1270,7 | Blender portátil usado na produção do personagem; ferramenta útil, não asset obsoleto. |

As três cópias de validação somam aproximadamente **19,9 GiB**. São o maior candidato à limpeza de espaço. Antes de uma limpeza, confirmar que nenhum processo as usa e preservar dados, relatórios ou builds que ainda se queira recuperar. Não apagar Builds/Server, Builds/Client ou Builds/Android como parte dessa recomendação: são entregas existentes. Library/Temp/obj são caches gerados, não defeitos de organização do código; limpar Library implica reimportação.

## 4. Preservar

- ShapeVisual.cs: referenciado por Player, Bot, Monster, Loot e Esporifero; parece antigo, mas ainda é ativo.
- InputSystem_Actions.inputactions: GameInput cria ações por código, mas o asset ainda está registrado em EditorBuildSettings. Não excluir sem revisar/remover essa configuração e dependências do Input System.
- Assets/Settings/Scenes/URP2DSceneTemplate.unity: referenciado por Lit2DSceneTemplate.scenetemplate. É um template, não uma cena da partida.
- Prototypes/PlayerWalk3D: contém modelo e scripts de exportação que geram o personagem aprovado. Cerca de 43,7 MiB; manter como fonte editável.
- Restart-Server.ps1: continua chamado pelo ServerDebugPanel; o novo console não tornou esse script dispensável.
- PlantSetup, EnvironmentSetup, ItemIconSetup: ferramentas de produção/importação dos assets atuais, mesmo que não rodem em partidas.
- MvpSetup.cs: ferramenta de montagem inicial, não manutenção rotineira. Recria cena/prefabs; manter restrito ao uso deliberado ou arquivar em etapa separada, revisando suas dependências.
- FishingPresentation, FishingGame, serviços de pesca/cozimento da cabana: ativos. A desativação se aplica à pesca na expedição.
- Campos/RPCs antigos de pesca em PlayerInventory: compatibilidade explicitamente mantida, ainda consultada por UI/diagnósticos/regras. Remover exige migração coordenada, não exclusão isolada.
- Arquivos partial como MonsterBehaviour, MonsterAppearance, MonsterArchetypes, ExtractionKnowledge, LootLock, MapDressing, ForestMapGeometry e TestLab: integram classes principais. Não precisam ser referenciados pelo nome do arquivo para serem usados.
- LockpickModel define tipos consumidos pela gazua; o nome do arquivo não é o nome de todos os tipos que contém. O mesmo cuidado vale para diagnósticos definidos em arquivos com nomes diferentes.

## Ordem sugerida

1. Corrigir os checks de mapa e os guias operacionais ultrapassados.
2. Arquivar documentos/experimentos anteriores, mantendo as fontes do personagem.
3. Limpar cópias de validação e builds de teste depois de preservar evidências/dados úteis.
4. Remover SampleScene e a UI desativada de pesca, com seus .meta, numa alteração específica.

Esta revisão identifica candidatos e dependências; não afirma que toda a árvore de assets foi validada em execução.
