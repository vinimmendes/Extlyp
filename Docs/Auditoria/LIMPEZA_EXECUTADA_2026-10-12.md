# Limpeza executada — 12/10/2026

Autorizada pelo usuário após a revisão ARQUIVOS_OBSOLETOS_2026-10-12.md. Nenhum teste, build, início ou reinício de servidor foi executado.

## Removidos

- Assets/Scenes/SampleScene.unity e .meta: sem referências por GUID, fora dos builds.
- Assets/Scripts/UI/ExpeditionFishingUI.cs e .meta: desativado, sem consumidores ou referências por GUID.
- Builds/SkillsValidation, ServerValidation, SkillsBaseline, ItemsTest, ItemsFinalTest e RenderCheck: cópias/cache de validação e builds anteriores de teste.

Tamanho lógico recuperado estimado, descontando as evidências preservadas: **20,18 GiB**. Registro por pasta em Logs/cleanup-2026-10-12.json. Os caminhos absolutos foram verificados como descendentes diretos de Builds antes de cada remoção; diretórios com links/junctions seriam recusados.

## Arquivados

Em Docs/Historico/Obsoletos_2026-10-12, preservando caminhos relativos:

- SETUP_PASSO2.md e ALTERACOES_CONTROLES_NAVEGACAO.md.
- Prototypes/BotsIA_v2_Source, PlayerWalk, LargeMapPreview e Lockpick.
- Sites/hud-preview.

Logs e UserSettings das cópias removidas estão em Logs/Arquivados-Limpeza-2026-10-12. O baseline conserva também Assets, Packages e ProjectSettings, sem Library/cache/builds. Esses arquivos permanecem locais, sob o ignore de Logs, incluindo identidades e dados de teste.

## Atualizados

- TestLabChecks: verifica ruínas construídas e recipientes registrados, sem exigir móveis idênticos; árvores sólidas em LowObstacleLayer; pontos de acesso/baú passam por RuinPoint para respeitar rotação. Expectativas antigas de posição/distância fixas removidas.
- Run-SkillsSmoke: protocolo do relatório vem do manifesto do servidor compilado, ou null se indisponível, em vez de registrar 11 fixo.
- README e GDD: estado atual com protocolo 17, 400 x 600, 10 minutos e pesca na cabana. Etapa 900 x 1380 identificada como histórica.
- SERVER_SETUP: data atual e comandos do console; documento de integração BotsIA aponta para o pacote arquivado.

## Preservados

Foi constatada Unity aberta no projeto principal e três salas em Builds/Server, sem processo usando as pastas removidas. Não foram interrompidos.

Builds/Server, Client e Android, Prototypes/PlayerWalk3D, Art, assets atuais, fontes de personagem, .cache/blender, Library do projeto principal, dados persistentes externos, referências de arte aprovadas, ShapeVisual, InputSystem_Actions, Restart-Server e ferramentas de importação permanecem intactos.

As cópias de validação serão recriadas pelos scripts quando necessárias; a próxima compilação poderá precisar reimportar suas dependências. Verificação final limitada a arquivos/referências e git diff --check; checks de mapa alterados não foram executados.
