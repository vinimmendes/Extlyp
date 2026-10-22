# Catacumbas — etapa 3.2: pisos e água

Atualização: a superfície da cisterna e do canal recebeu animação discreta por shader. Consulte [Água — etapa 1](CATACUMBAS_AGUA_ETAPA_1.md). A descrição de água estática abaixo registra a entrega original.

Lote visual CAT-F01 a CAT-F10: **40 variantes produzidas e integradas**. Referências: matéria e abandono de Darkwood; leitura do combate e circulação de Bullet Echo. Arte original, medieval, vista superior ortográfica.

## Integração

`DungeonTerrain` cria Grid e Tilemaps cosméticos a partir da mesma máscara de `DungeonLayout`. Células de 1,06 u; pisos de uma célula, água de duas células, marcas/degraus de duas por uma. Tilemaps separados para piso, água, transições, margens, marcas e degraus. Uso de `SetTilesBlock` nas superfícies, seleção determinística das variantes e material de sprites já usado pelo projeto.

Calcário nos vestíbulos, galerias, claustro e contrapesos; pedra úmida na cisterna/prisão; lajes quebradas na capela; terra, raízes e pequenas inclusões de osso no ossário. Nenhum perigo ativo ou cobertura pintado no piso. As margens seguem as células bloqueadas de água; degraus decorativos ficam submersos, sem anunciar uma passagem adicional. Água estática também substitui a cor lisa do canal funerário.

As regras de navegação, colisores, coberturas, extrações e ponte continuam nos componentes existentes. A ponte mantém sua apresentação sobre a água. Paredes, arquitetura, mecanismos e sepulturas continuam em placeholder até suas respectivas etapas. Servidor sem dispositivo gráfico não cria a nova apresentação. Recursos incompletos cancelam a nova camada antes de ocultar o piso anterior.

## Assets e reprodução

- Fontes e prompts: `Art/Catacumbas/Pisos`.
- PNGs de runtime: `Assets/Resources/Environment/Dungeon/Floors`.
- Preparação de cópias e metadados: `Tools/Prepare-DungeonFloors.ps1`, com manifesto de origem e IDs. Não sobrescreve imagens existentes diferentes.
- Imagens geradas com a ferramenta integrada `imagegen`, uma chamada por variante, sem serviços adicionais.
- Fontes mantêm a resolução de geração. A importação de runtime é **explicitamente reduzida** a 128 px por célula: max-size 128 para módulos quadrados de uma célula; 256 para água e decalques de duas células. Bilinear, sRGB, sem mipmaps, sem compressão; Repeat apenas nas superfícies opacas, Clamp nos decalques. A escala visual considera as dimensões efetivamente importadas.

Skill `unity-tilemap-2d` instalada em `C:/Users/Vinni/.codex/skills/unity-tilemap-2d`, origem `gamedev-skills/awesome-gamedev-agent-skills`, pasta `skills/unity/unity-tilemap-2d`. Aplicados Grid, camadas explícitas, pintura em bloco e coordenadas de célula. Exemplos adaptados à Unity 6000.0.84f1. Sem adicionar RuleTile, pacotes, shaders ou atualizar a engine.

## Limites da entrega

Revisão de código e inspeção dos arquivos de arte não substituem avaliação prática. PNGs opacos nas superfícies e RGBA com transparência nos decalques conferidos; fontes de uma célula e água em 1254 × 1254 px, marcas/degraus em 1774 × 887 px, preservadas. Encaixe entre variantes, leitura sob fog, contraste dos monstros e aparência no Android precisam da primeira avaliação do usuário. A tentativa de abrir a galeria pelo navegador interno não acessou o servidor local; a inspeção visual foi feita diretamente nos arquivos. Não foram iniciados Unity, testes automatizados ou builds.
