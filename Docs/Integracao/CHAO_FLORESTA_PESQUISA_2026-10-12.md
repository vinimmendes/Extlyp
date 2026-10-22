# Chão da floresta — pesquisa e proposta

Pesquisa em 12/10/2026. Proposta de arte e apresentação; nenhuma alteração de mapa, pacote, shader ou colisão foi feita. Sem sessões Unity, testes ou builds.

## Referências

- Darkwood: https://www.darkwoodgame.com/presskit/ — referência de atmosfera, vegetação e abandono. Imagem específica: https://www.darkwoodgame.com/presskit/images/dw_scr6.png
- Bullet Echo: https://www.bulletecho.game/ — galeria oficial e trailer para estudar leitura de personagens, obstáculos e espaços de combate. Referência complementar de captura: https://gamingonphone.com/guides/bullet-echo-beginners-guide-and-tips/

Direção proposta para Cyber Extraction: solo de baixo contraste, em marrons frios e verde oliva dessaturado; caminhos distinguíveis pela superfície mais limpa; detalhes agrupados nas bordas. Essa direção é nossa interpretação das referências, não uma afirmação sobre as técnicas usadas nesses jogos. Não copiar texturas, mapas ou sprites.

## O que o projeto já tem

- Unity 6000.0.84f1, URP 17.0.4.
- packages-lock.json resolve SpriteShape 10.1.1, Tilemap 1.0.0 e Tilemap Extras 4.1.1, como dependências da feature 2D. Não precisam ser instalados para esta proposta.
- Mapa grande: 400 × 600 unidades. Ground é um quadrado ampliado; ForestTrail usa retângulos rotacionados com cor lisa. Rotas já fornecem pontos e larguras. A geometria de deslocamento e os limites já existem.

## Skills encontradas e lidas

- create-game-assets: https://github.com/gamedev-skills/awesome-gamedev-agent-skills/blob/main/skills/disciplines/create-game-assets/SKILL.md — útil para definir uma família de materiais, escala e critérios de aceitação. Recomenda conferir repetição em mosaico 3×3 e leitura no tamanho real.
- unity-tilemap-2d: https://github.com/gamedev-skills/awesome-gamedev-agent-skills/blob/main/skills/unity/unity-tilemap-2d/SKILL.md — útil para camadas de solo e seleção de bordas por RuleTile. A fonte atual mira Unity 6000.3, portanto exemplos devem ser conferidos contra 6000.0 e Extras 4.1.1.
- level-design já está local: preservar corredores, cobertura, rotas alternativas e leitura dos encontros.

As externas foram consultadas como referências; não foram instaladas. Skills organizam o trabalho, mas a qualidade visual depende dos assets e da composição.

## Solução recomendada

1. Solo base: família pequena de terra com folhas e terra úmida, textura repetível, detalhes miúdos discretos. Usar Tilemap por regiões ou superfícies divididas em regiões; escolher na implementação segundo a integração com o renderizador atual.
2. Variação ampla: manchas irregulares de solo exposto, musgo e umidade, distribuídas com a semente do layout. Evitar uma textura igualmente ruidosa em todo lugar.
3. Estradas: construir áreas fechadas ao redor dos pontos atuais das rotas com SpriteShape. Aplicar preenchimento repetível e bordas de terra/vegetação. Desativar Stretch UV e considerar World Space UV para manter a escala de textura entre áreas. Isso exige contornos válidos: bifurcações precisam de junções próprias ou união de contornos, sem autointerseções e sem acumular bordas internas.
4. Detalhes locais: folhas agrupadas, raízes e pedras pequenas nas margens; serragem na serraria, cascalho no depósito e lama perto do lago. Marcas de rodas acompanham a estrada; não girar arbitrariamente texturas direcionais.
5. Preservar as larguras transitáveis e a geometria autoritativa existente. A apresentação do terreno não adiciona colliders. Integrar o material ao sistema atual de visão/fog e verificar sorting antes de expandir.

SpriteShape suporta contornos e preenchimento com UV em coordenadas do mundo: https://docs.unity3d.com/Packages/com.unity.2d.spriteshape@10.0/manual/SSController.html . A API final deve ser conferida na versão instalada 10.1.1. RuleTile é uma alternativa para terrenos em grade: https://github.com/Unity-Technologies/2d-extras/blob/master/Documentation~/RuleTile.md . Para estradas diagonais e orgânicas do mapa atual, a preferência por SpriteShape é uma decisão de projeto.

## Primeiro lote de arte proposto

- 3 superfícies: terra/folhas, terra compactada e terra úmida.
- 2 famílias de borda: solo/folhagem e solo/lama, com variações de contorno.
- 6 detalhes sobrepostos: folhas, musgo, pedrinhas, serragem, sulcos de roda e poça rasa.
- Uma composição representativa da serraria incluindo trecho reto, curva, bifurcação, toras e personagem na escala real.

## Critérios visuais para avaliação pelo usuário

- Mosaico 3×3 sem costuras nem manchas reconhecíveis repetidas em grade.
- Solo com menos contraste que personagem, loot e obstáculos; leitura também sob o campo de visão atual.
- Folhas e pedras com escala coerente com pés do personagem e diâmetro das toras.
- Sem sombra direcional forte embutida na textura de chão.
- Sem estradas com bordas retas de retângulo, serrilhado de grade ou vegetação cobrindo corredores.
- Julgar com câmera e resolução do jogo, incluindo celular horizontal. Não aprovar só pela imagem ampliada.
- Dimensões, compressão e quantidade de regiões são parâmetros de implementação; não há ganho de desempenho medido nesta pesquisa.

Conforme a preferência do projeto, o usuário faz a primeira avaliação prática. Executar testes Unity/builds apenas quando solicitados ou acionados por um problema relatado.
