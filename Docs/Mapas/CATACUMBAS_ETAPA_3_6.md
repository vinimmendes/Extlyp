# Catacumbas — etapa 3.6: extrações e mecanismos

Lote CAT-E01 a CAT-E17: 23 PNGs selecionados, com 37 regiões de sprites, incluindo oito quadros de grade e oito de travamento. A escadaria possui uma revisão adicional preservada nas fontes. Arte medieval original, com pedra, madeira, ferro e corda; matéria e abandono de Darkwood e legibilidade de circulação de Bullet Echo.

## Extrações

- **Escadaria do Cortejo:** escada de pedra com degraus ascendentes para um patamar superior, grade deslizante e cabrestante manual. A revisão removeu a leitura de descida para um poço. O servidor continua exigindo 6 s para preparar a grade e 3 s para extrair.
- **Poço dos Coveiros:** borda de pedra com centro transparente sobre o fundo escuro existente, plataforma de tábuas, guincho com encaixe de manivela, cordas nas duas bordas do poço e contrapeso decorativo em alvenaria livre. A manivela instalada aparece quando o servidor confirma a preparação. Permanecem 4 s de instalação e 3 s de extração.
- **Canal dos Defuntos:** cais composto pelas regiões esquerda/inferior do embarcadouro, formando um L e preservando a água sob a barca. A proa aponta para +X, acompanhando o movimento existente. A corrente troca de presa para solta após a confirmação do servidor. Permanecem 4 s para soltar a corrente e 3 s para extrair, sem requisito de chave.
- Relevos de escadaria, poço e barca ficam próximos aos destinos, sobre uma célula de piso livre, evitando loot e mecanismos. Não acrescentam seta global, nova HUD ou informação sobre jogadores.

## Comporta e ferramentas

A chave do canal continua abrindo somente o atalho da cisterna. O travamento recebe oito quadros locais, sem alterar os 4 s exigidos pelo servidor. A passarela aberta tem exatamente 4 × 8 células (4,24 × 8,48 u) e acompanha o objeto existente. A peça recolhida ocupa 4 × 2 células na margem sul, fora da água bloqueada, encostada à aresta de montagem da versão aberta. Não esticar a peça recolhida sobre a travessia fechada.

`DungeonToolArt` fornece arte física de manivela/chave aos pickups e ícones próprios ao inventário. A apresentação no chão usa dimensão máxima de 0,8 u, compensando a escala do pai sem alterar o objeto de rede, o collider ou sua coleta. A manivela montada no guincho usa redução para encaixe. Ícones têm limite de importação de 128 px, sem borda ou texto rasterizado, com o mesmo significado nos temas A e B. As fontes RGBA mantêm resolução integral. Não há redesenho de layout, controles ou temas da interface.

## Integração e autoridade

`DungeonExtractionArt` monta a camada depois das demais artes. `DungeonPresentation` mantém os movimentos, o fade da partida, a ativação da ponte e a atualização de colisores já existentes; apenas encaminha à arte sua máscara de preparação, estado do atalho e fração de partida da barca. Os filhos visuais da plataforma e da barca seguem seus transforms atuais.

Oito quadros de grade/trava são selecionados por apresentação local após a mudança confirmada, com transição curta de aproximadamente 0,6 s; não prolongam a preparação, não acionam interação e não liberam passagem. Na primeira sincronização, o visual assume diretamente o estado atual, inclusive para quem entra após a preparação. A corrente e a manivela instalada também derivam do estado confirmado.

Não foram alterados RPCs, protocolo, consumo/recuperação de ferramentas, requisitos, gatilhos, tempos, banking, monstros ou máscaras de navegação. Não há novo collider ou nova escrita em estado de rede. O gerenciamento existente da água permanece em `DungeonPresentation`.

O carregamento da camada exige os recursos antes da confirmação. Ausência de recurso ou falha de montagem cancela a nova camada e preserva os renderers anteriores. Filhos anexados aos suportes móveis são incluídos no descarte e na recuperação de falha. Ícones/pickups têm fallback para a apresentação anterior se a arte não estiver disponível. Servidor sem dispositivo gráfico não cria a camada de extrações.

## Arquivos

- Fontes RGBA intactas, prompts individuais e revisão: `Art/Catacumbas/Extracoes/{Fontes,Prompts}`.
- Manifesto consolidado de prompts e galeria: `Art/Catacumbas/Extracoes/{prompts-v1.json,index.html}`. A galeria apresenta fontes isoladas, não uma captura de jogo.
- Recursos do mapa: `Assets/Resources/Environment/Dungeon/Exits`, incluindo `ExtractionsArt.json` com recursos e retângulos normalizados.
- Ícones: `Assets/Resources/Items/Dungeon/{CAT-E15,CAT-E16}.png`.
- Preparação: `Tools/Prepare-DungeonExtractions.py`. Lê o alpha de cada célula dos dois conjuntos 2 × 4, registra os recortes e copia os originais sem modificar pixels. Os batentes definem o corpo de cada quadro da grade. Na trava, a faixa superior do suporte de pedra define a âncora: os oito quadros usam enquadramento de largura constante e pivôs registrados no manifesto, sem normalizar a lingueta móvel como se fosse o suporte. A trava é dimensionada dentro de 0,8 u, preservando a proporção do enquadramento; não muda de escala ao girar. O encaixe visual entre quadros ainda requer avaliação em execução.

Geração com imagegen integrado, uma chamada por variante e uma edição dirigida para a escadaria; prompts exatos guardados no projeto. Importação sRGB, bilinear, Clamp, sem mipmaps e sem compressão. Limite de textura considera o corpo e os quadros para a referência de 128 px por célula; ícones usam limite próprio de 128 px. Skills de mapa, C#, UI e imagegen aplicadas à Unity 6000.0.84f1, sem atualização da engine, URP ou rede.

## Verificação e limites

Revisão estática de carregamento, recortes, descarte, escala dos filhos e encaminhamento de estado; inspeção visual dos 23 PNGs selecionados e da revisão adicional da escadaria. Não foram executados testes automatizados, sessões Unity ou builds nesta entrega, conforme a preferência do projeto. Encaixes, ordem de desenho, transições, late join, interrupção/retomada da extração e leitura sob fog/Android aguardam avaliação prática do usuário. Não há evidência de desempenho em execução nesta etapa.
