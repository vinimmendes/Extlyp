# Catacumbas — etapa 3.3: arquitetura

Lote CAT-A01 a CAT-A12, com **32 variantes produzidas e integradas**: paredes retas, cantos externos/internos, terminações, paredes quebradas, arcos, pilares, nichos, inscrições apagadas, soleiras, grades e portas. Direção medieval original, atmosfera de abandono orientada por Darkwood e leitura espacial orientada por Bullet Echo.

## Integração visual

`DungeonArchitecture` usa exclusivamente as máscaras e retângulos já existentes em `DungeonLayout`:

- Os módulos de parede seguem células sólidas adjacentes ao piso. Os lados expostos escolhem reta, canto ou terminação; os cantos diagonais usam o encaixe interno. O maciço escuro original permanece sob a arte e nas áreas profundas inacessíveis.
- Os pilares ocupam exatamente os retângulos dos pilares existentes, sem mudar colisão, altura de visão ou navegação.
- Os arcos e soleiras marcam bocas reais de corredores de quatro células, com aproximação livre e flancos sólidos. A soleira usa somente a parte plana central do sprite. O arco é dividido em apoios esquerdo/direito e faixa superior: apoios ocupam as células sólidas dos flancos, centro transparente conserva o vão de 4,24 u. A pegada visual total de A06 foi ajustada no catálogo para 6,36 × 1,06 u, incluindo os apoios; a geometria não muda. Instâncias próximas às extrações são excluídas. Não são portas interativas ou novas passagens.
- Nichos, inscrições, grades e portas ocupam pares de células de parede junto às salas. Nenhuma porta fechada aparece sobre uma célula caminhável. Variantes abertas/fechadas são decoração estática de recessos; não adicionam mecânica de abertura.
- As entradas em quadrados coloridos são ocultadas quando a respectiva soleira foi colocada; as antigas faixas de orientação são substituídas pelas entradas arquitetônicas. Sepulturas, coberturas, marcos de setor e mecanismos aguardam os próximos lotes.

Piso, água, banco de itens, NPCs, visão, temporizadores, saídas e ponte conservam suas regras. Os novos componentes não criam Collider2D, RPC, estado de rede ou configuração de jogo. Tilemap de contorno usa o material de sprites atual. As marcas de inundação permanecem acima da parede; os atores continuam acima da arquitetura.

Servidor sem dispositivo gráfico não monta essa apresentação. Recursos incompletos cancelam a nova camada antes de ocultar placeholders. Sprites e Tiles criados em memória são destruídos com o mapa.

## Fontes e preparação

- Originais RGBA e prompts individuais: `Art/Catacumbas/Arquitetura/{Fontes,Prompts}`.
- Manifesto consolidado e galeria: `Art/Catacumbas/Arquitetura/{prompts-v1.json,index.html}`.
- Recursos de runtime: `Assets/Resources/Environment/Dungeon/Architecture`.
- Preparação: `Tools/Prepare-DungeonArchitecture.py`. Lê o alpha para medir o corpo, copia os PNGs sem editar pixels e preserva arquivos existentes diferentes.

Os sprites são gerados com a ferramenta integrada `imagegen`, uma chamada por variante. A margem transparente é removida somente pelo retângulo de Sprite.Create, com pivô central e escala visual exata da pegada do catálogo. Fontes retas alongadas usam recorte central quadrado; terminações usam o extremo direito. Isso evita esticar o comprimento de uma parede em uma célula quadrada. Recortes normalizados e a correção de orientação do canto A02_v02 ficam em `ArchitectureArt.json`; o PNG original é preservado. Os 18% laterais de A06 são os apoios, com o trecho central de 64% sobre o vão; A10 conserva somente esses 64% planos centrais.

A importação reduz explicitamente o runtime ao menor max-size em potência de dois que preserve pelo menos 128 px por célula no corpo recortado. O cálculo considera a margem e os recortes; evita reduzir um trecho central a poucos pixels antes de ampliá-lo. Fontes completas permanecem no projeto. sRGB, bilinear, sem mipmaps, Clamp, alpha preservado, sem compressão. Nenhum shader, pacote, versão de engine ou serviço adicional foi introduzido.

Skills aplicadas: `level-design`, `unity-tilemap-2d`, `unity-csharp-scripting` e `imagegen`, adaptadas à Unity 6000.0.84f1.

## Verificação e limites

Revisão estática do carregamento, dos encaixes e do isolamento da apresentação. Foram inspecionadas as 32 imagens e preparados seus metadados RGBA. Isso não confirma a leitura em partida: repetição modular, junções dos cantos, altura visual, contraste sob fog e escala no Android dependem da primeira avaliação prática do usuário. Não foram iniciados Unity, testes automatizados ou builds para esta entrega.
