# Minimap — desenho contido e marcadores locais

## Problema relatado

A captura do usuário mostrou segmentos de rotas atravessando a moldura e uma seta solta perto do controle de mira. O código desenhava as rotas com `GUIUtility.RotateAroundPivot` dentro de `GUI.BeginGroup`; também desenhava os marcadores direcionais alterando `GUI.matrix` no grupo. Essas transformações misturavam coordenadas da área segura, do grupo e do pivô. A aparência da seta circulada é compatível com os marcadores direcionais desse caminho; não houve captura de uma partida para identificar individualmente aquele pixel.

## Referências pesquisadas

- [Unity 6000.0 — GUI.BeginGroup](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/GUI.BeginGroup.html): o grupo define coordenadas locais e recorta os filhos.
- [Unity — implementação de GUIUtility](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/IMGUI/GUIUtility.cs): `RotateAroundPivot` altera a matriz e converte o pivô pelo sistema interno de recorte. Referência consultada para entender a interação entre grupos e transformação; o projeto continua em Unity 6000.0.84f1.
- [Unity 6000.0 — Render textures](https://docs.unity3d.com/6000.0/Documentation/Manual/class-RenderTexture.html): alternativa usual para vistas geradas por câmera. Não foi adicionada uma segunda câmera, porque o minimapa existente usa cartografia esquemática e não deve revelar jogadores ou inimigos.
- A busca encontrou uma [skill pública de uGUI com seção de minimapa](https://github.com/majiayu000/claude-skill-registry/blob/main/skills/data/unity-game-ugui-design/SKILL.md). Ela pressupõe RawImage, Mask, RenderTexture e ferramentas uGUI. Não foi instalada nem executada. Não existe uma skill dedicada ao minimapa no catálogo local; foram aplicadas `game-ui-ux`, `unity-csharp-scripting` e `systematic-debugging` do projeto.

## Correção

- `BuildTerrain` rasteriza as rotas reais de `LargeMapPlan` na mesma textura do terreno. O desenho em `OnGUI` recorta uma textura única, sem girar segmentos ou a matriz do grupo.
- `MinimapPointer` gera o desenho rotacionado da seta em uma textura local. O retângulo onde ela é exibida permanece no lugar, e não modifica `GUI.matrix` ou o recorte.
- Jogador usa um marcador direcional com rotação suavizada em `Update`. A saída selecionada fora do recorte local mantém sua seta junto à borda do minimapa.
- Posições dos marcadores ficam contidas na área interna. Mapa mantém norte acima, proporção do recorte, geografia real, saídas conhecidas/incertas/indisponíveis e seleção de destino no mapa ampliado.
- O atlas é atualizado quando a geometria ou o tema mudam. Recursos gerados são liberados ao destruir o componente.
- Sem alteração no layout aprovado, na área segura, nos controles de combate, na rede ou nos critérios de conhecimento das saídas. Não foram adicionados marcadores de inimigos.

## Verificação focada

`MinimapChecks.Run` executado na Unity 6000.0.84f1 com GPU, sem servidor, build ou suíte completa. Resultado: PASS, código de saída 0.

- 72 orientações da textura real da seta mantêm as margens livres e o símbolo visível.
- Atlas real reduzido/lido pela GPU contém as rotas do mapa.
- Projeção mantém o centro do mundo no centro do retângulo e norte na borda superior.
- Raster de rota com extremos fora dos limites escreve apenas dentro do buffer e mantém o segmento que atravessa a área.

Evidências: `Logs/minimap-check.json`, `Logs/minimap-unity-check.log` e `Logs/minimap-atlas-check.png`. São verificações gráficas e de projeção isoladas; não comprovam a composição IMGUI completa durante uma partida, interação multitouch ou execução Android.
