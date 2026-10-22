# Base medieval — Refúgio sob as ruínas

Implementação da direção aprovada em [conceito](Referencias/base-medieval-sombria-conceito.png), preservando as cinco páginas, dois temas, capacidades 12/16 e regras existentes. A imagem conceitual continua sendo referência; não foi usada como uma interface achatada.

## Apresentação

Sombria recebe três PNGs originais: MedievalPanel (couro quase preto e pequenos cantos góticos), MedievalButton (neutro) e MedievalPrimary (bronze). MedievalMenuArt cria sprites com recorte das margens transparentes e bordas para nine-slicing: canto de painel com 24 unidades e botão com 12. Os pixels originais não foram editados. Textos e imagens continuam componentes separados. Simples compartilha a geometria e mantém superfícies limpas e destaque verde.

Cinzel é usada nos títulos da base e de Opções, nome do destino e títulos das grades. Números, rótulos e mensagens permanecem Barlow. Cinzel foi obtida do [repositório oficial Google Fonts](https://github.com/google/fonts/tree/main/ofl/cinzel), com SIL Open Font License arquivada junto ao TTF. A importação e rasterização TMP ainda precisam da primeira avaliação Unity pelo usuário.

MedievalCatacombsBackdrop recria somente o ambiente do conceito, sem texto, botões ou painéis. A arte anterior foi preservada. A mochila possui novo sprite de couro; capacidade mostra doze segmentos preenchidos pela quantidade real de slots ocupados. O componente CapacityTrack foi substituído pontualmente por CapacitySegmentsGraphic no prefab BaseHub, mantendo seu fileID e referências.

O estilo aplica-se aos painéis e ações das páginas existentes. Slots continuam compactos, com contorno de no mínimo um pixel físico e pequeno recuo para reduzir perda das arestas em escalas fracionárias. Ofertas do comerciante usam o estilo de slot, com quantidade/preço ancorados dentro da borda, nomes com quebra e imagem proporcional. O nome completo permanece nos detalhes da seleção.

## Menu de opções

O menu antigo tinha altura fixa de 460 e quatro ações empilhadas no alto. Agora usa altura conforme o conteúdo, dois cartões de tema lado a lado, botão de organização e retorno separados, alvos de 48 ou mais, foco explícito por controle e títulos legíveis. Ajuda, saída e ferramentas existentes foram preservadas; ferramentas continuam condicionadas à disponibilidade do ambiente de teste.

A base anteriormente ocultava toda a sua representação ao abrir Opções, revelando o cenário de teste ao fundo. Agora mantém a base atrás do modal e pausa a atualização/interação da página. Um scrim de Canvas cobre a tela física, inclusive além da área segura, e intercepta cliques; navegação usa apenas o menu ativo. Em expedição, o cenário fica escurecido, sem pausar a partida.

BaseMenuFocus acrescenta uma faixa fina de foco aos botões texturizados, com raycast desativado. A seleção das abas continua usando sublinhado. Estado do tema permanece salvo pela lógica existente; personalizações de controles não foram alteradas.

## Assets e prompts

Todos os PNGs abaixo foram gerados com imagegen integrado e inspecionados individualmente. Importação com alpha preservado, clamp, sem mipmaps e sem compressão. Texturas de UI limitadas a 1024; fundo a 2048. Originais da geração permanecem no diretório generated_images da sessão.

| Asset em Assets/Resources/UI/Art | Fonte gerada |
| --- | --- |
| MedievalPanel.png | exec-10608021-a7f9-4a2e-9ccf-75050e74d921.png |
| MedievalButton.png | exec-338b507e-85e5-4ee9-b213-a47d28090a76.png |
| MedievalPrimary.png | exec-e9b58baa-43e3-4d81-b8e7-d4205f770d63.png |
| MedievalCatacombsBackdrop.png | exec-abe9878f-eedb-4601-ad86-a58018462f80.png |
| MedievalBackpack.png | exec-0e52d0c6-4a71-4ee9-9fae-1cce50fb5daf.png |

Prompts de produção: painel isolado vertical de couro quase preto com moldura fina de bronze e pequenos detalhes góticos nos quatro cantos; botão secundário horizontal escuro e botão primário horizontal de bronze, ambos sem texto, com bordas contínuas e transparência externa, preparados para nine-slicing; ambiente das catacumbas recriado a partir do conceito aprovado removendo toda a interface; mochila medieval de couro em três quartos com transparência e sem elementos modernos. Textos, campos e ações não foram incorporados às imagens.

## Revisão

Revisão semântica de 202 fontes C# com referências da versão instalada: zero erros, sem emitir assembly. Revisão estática do BaseHub: referências locais completas e GUID do novo CapacityTrack correspondente ao script. Cinco PNGs originais com metadados de importação presentes. Não foram iniciados Unity, testes automatizados ou builds para esta entrega.

O resultado não é declarado 100% idêntico sem avaliação em execução. Conferir primeiro Preparação e Opções em Sombria; depois Simples, últimas ofertas do comerciante, foco de controle, telas horizontais com recorte e ultrawide. Observar particularmente importação da Cinzel, espessura final das molduras, nomes em duas linhas e bloqueio de cliques atrás do modal.
