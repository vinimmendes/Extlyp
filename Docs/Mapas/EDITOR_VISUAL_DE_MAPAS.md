# Editor visual de mapas na Unity

**Catacumbas:** use **Cyber Extraction > Mapas > Catacumbas — autoria fora do Play**, conforme [manual da autoria](ReworkCompleto/IMPLEMENTACAO_E_ESTADO.md). Essa cena permite editar terreno físico, arte, coberturas e conjuntos funcionais sem iniciar a partida. A ferramenta geral descrita abaixo continua atendendo os outros mapas e os ajustes anteriores.

Menu: **Cyber Extraction > Mapas > Editor visual**.

Ferramenta geral de correções de apresentação para mapas produzidos por `MapBuilder`. O seletor utiliza `ExpeditionMap`, atendendo Catacumbas, Floresta Velha e Área de testes; novos mapas integrados ao mesmo construtor também passam pelo carregamento de ajustes. As correções são independentes por mapa e não exigem editar seus geradores.

## Como usar

1. Fora de Play, abra a janela, escolha o mapa e clique em **Iniciar prévia offline (Play)**. A ferramenta entra em Play por sua solicitação, desativa a inicialização de rede dessa sessão e abre o mapa na Scene View em 2D. Se a cena não tiver MapBuilder, cria uma instância temporária. Não autentica conta, inicia servidor ou modifica o progresso.
2. Navegue na Scene View com as ferramentas normais da Unity. Para mudar de mapa, escolha outro e clique em **Carregar mapa para edição**.
3. Clique **diretamente na peça visível**, na Scene View. A ferramenta identifica tiles e objetos automaticamente, considerando sua posição, transformação e ordem de desenho. Ignora pixels transparentes quando a leitura da textura está disponível; se não estiver, usa os limites da peça.
4. A peça ganha contorno ciano e uma prévia ampliada na janela. Se houver sobreposição, aparecem miniaturas das peças naquele ponto: clique na desejada. Pressione **F** ou use **Aproximar / localizar** para enquadrar a seleção.
5. A paleta é uma **grade de miniaturas clicáveis**, com busca, tamanho ajustável e páginas. Não é necessário escolher camada ou criar um ajuste antes. Inclui os sprites usados e as variantes fornecidas pelos catálogos dos mapas (incluindo as famílias de arquitetura das Catacumbas). Clique numa miniatura para substituir a peça selecionada. Texturas geradas somente em memória permitem mover/girar/ocultar o original, mas não são aceitas como substitutas persistentes.
6. Clique e arraste diretamente a peça na Scene View para mover livremente, ou use as setas do manipulador. **Escape** cancela o movimento e cada arraste é uma operação única de Undo. **Mostrar grade** e **Encaixar na grade** são independentes: o padrão é grade visível e encaixe desligado. **Shift** inverte temporariamente o encaixe. O botão **Alinhar à grade** ajusta a posição de uma peça já movida. A grade utiliza a origem e o tamanho real da Tilemap; objetos sem Tilemap usam inicialmente uma unidade, com origem e espaçamento ajustáveis. Cópias de tiles mantêm a referência à grade de origem. Há passos de uma célula, meia célula e um quarto de célula. Essas opções são salvas por mapa nas preferências locais do editor. O encaixe só altera XY, preservando profundidade, escala, rotação e espelhamento. Os botões permitem girar 90°, espelhar, ocultar/mostrar e restaurar. Undo/Redo da Unity desfaz/refaz as operações. Os campos numéricos e a lista de correções ficam em **Avançado / correções salvas**; a lista também permite localizar e restaurar peças ocultas. A troca por Sprite importado do projeto fica nessa seção após criar uma alteração.
7. Clique em **Salvar**. O asset também é salvo ao sair do Play com a janela aberta. As edições são mantidas em `Assets/Resources/MapEdits/Map_<id>.asset` e reaplicadas ao gerar o mapa no jogo. Salve antes de fechar a janela; alterações normais feitas diretamente nos objetos temporários da Hierarchy não são capturadas.

## Escopo desta entrega

Edição **visual** de células de Tilemap e objetos com SpriteRenderer: substituir arte, reposicionar, girar, espelhar, deletar, duplicar, ocultar e restaurar. Preserva os tiles originais e usa representações visuais auxiliares para não deslocar colliders ou alterar navegação, máscaras de parede, cobertura, spawn e regras autoritativas. Ocultar uma parede não abre passagem. Esta ferramenta não é um editor de layout físico ou de regras da expedição.

Na paleta, a troca mantém a orientação e as dimensões visuais da célula original; os campos de rotação e espelhamento são ajustes adicionais. Os sprites importados permanecem referenciados como textura/recorte, sem salvar referências temporárias criadas durante Play.

O modo de edição também aceita Play offline ou host de laboratório sem outros clientes. Recusa clientes remotos e partidas normais. A prévia offline serve para autoria pela Scene View, sem simulação de combate ou validação de gameplay. Sair do Play encerra essa prévia; o próximo Play comum volta ao fluxo normal do jogo.

Os alvos são identificados por caminho de nomes e ordinal entre irmãos do mesmo nome, relativo à raiz gerada. Renomear/remover alvos ou alterar estruturalmente um gerador pode exigir revisar os ajustes; alvos ausentes emitem aviso e são ignorados. Ajustes são por mapa, não por semente: reveja-os se alterar uma geração que reposicione objetos conforme a semente. Publique os assets salvos junto às próximas builds. O carregamento no servidor sem gráficos é ignorado; colisões/rede continuam usando as regras existentes.

## Revisão desta entrega

Revisão estática e de diferenças, sem abrir Unity, iniciar Play, rodar testes automatizados ou gerar builds. A avaliação prática de seleção, arraste, paleta, Undo, troca de mapas, persistência após sair de Play e geração no jogo fica para a primeira utilização do usuário.

## Deletar e duplicar

- **Deletar / Delete**: oculta a representação visual de um original, sem abrir passagem. Use Mostrar peça ou Restaurar original para recuperá-lo. Excluir uma cópia remove somente essa cópia. Os atalhos são tratados na Scene View focada, sem capturar campos de texto.
- **Duplicar / Ctrl+D ou Cmd+D**: cria uma cópia com ID próprio, mesma arte, cor, dimensões, orientação, espelhamento e ordem de desenho, um passo da grade à direita. A cópia fica selecionada para arraste e pode ser duplicada novamente. Excluir o original não remove as cópias.
- Cópias são persistidas em `MapVisualEdits.Additions`, junto aos ajustes antigos em `Edits`. A coleção nova é opcional para assets antigos. A aparência fica registrada independentemente dos objetos de Play. Texturas originalmente geradas em memória são copiadas para `Assets/Resources/MapEdits/Generated` ao duplicar; o nome usa hash do conteúdo para reutilizar arquivos iguais. Undo não apaga esses arquivos compartilháveis.
- Alterar uma cópia mantém suas dimensões de apresentação; posição, rotação e espelhamento permanecem editáveis. A lista avançada permite localizar cópias e ajustes em peças ocultas.

## Tileset modular V2 das Catacumbas

O novo catálogo está em `Assets/Resources/Environment/Dungeon/ModularV2/CatacombWalls.wallset`. A Unity importa esse arquivo pelo `DungeonWallSetImporter`, gerando um atlas e subassets persistentes para **47 combinações de vizinhança**, mais três acabamentos repetíveis de portas. Não é necessário executar comandos de preparação. A importação ocorre quando a Unity carregar os scripts e os assets; não foi executada durante esta entrega.

A textura nova `LimestoneSurface.png` foi produzida com a ferramenta de geração de imagens. É um material comum; os encaixes, bordas e recortes são construídos pelo importador com coordenadas fixas. Módulos de 128 × 128 pixels, pivô central, 128 pixels/unidade e borda de 20 pixels; no mapa, a grade aplica a célula de 1,06 unidade. O atlas inclui extrusão de dois pixels para reduzir vazamento por filtragem. Não há recorte individual por transparência ou rotação corretiva por variante.

Os bits N/E/S/W indicam vizinhos sólidos; NE/SE/SW/NW só são aceitos quando os dois vizinhos laterais existem. Essa normalização produz 47 configurações: isolamento, pontas, retas, cantos externos/internos, T, cruz e preenchimento interno. A geração usa as máscaras existentes de pedra, preservando geometria e regras. A antiga camada oposta permanece como alvo compatível vazio; ajustes antigos não são apagados e células sem peça emitem aviso. Reveja correções antigas que compensavam erros de encaixe agora resolvidos.

A base de alvenaria, pilares e decorações independentes foram preservados. Aberturas de quatro células mantêm suas dimensões e recebem dois acabamentos laterais e dois módulos centrais. Os arquivos anteriores continuam no projeto.

## Composição de referência

Use **Cyber Extraction > Mapas > Referência do tileset modular**, ou **Encaixes** na janela. Antes de Play, o menu solicita a prévia offline pelo mesmo mecanismo do editor. A composição é temporária e fica fora da área do mapa, sem colliders. Ela mostra cômodo fechado, dois cômodos com parede compartilhada, portas nas quatro orientações, corredores, cantos, T, cruz, parede isolada e todas as 47 vizinhanças com seus identificadores. Usa `DungeonWallCatalog.MaskAt` e o mesmo catálogo do jogo. **Ver mapa inteiro** retorna o enquadramento ao mapa; carregar outro mapa remove a composição.

## Roteiro da primeira avaliação prática

1. Abrir o catálogo importado e a composição; conferir encaixes contínuos, espessura, escala, portas e as 47 amostras.
2. Nos três mapas, selecionar uma peça e arrastá-la livremente. Confirmar que não salta ao iniciar o movimento e que Escape desfaz o arraste.
3. Ligar o encaixe, mudar subdivisões, manter Shift durante o arraste e usar Alinhar à grade depois de mover livremente. Repetir com peça girada/espelhada e conferir que só XY muda.
4. Duplicar original e cópia, deletar originais/cópias, desfazer/refazer e restaurar. Conferir independência de cada peça e cor, tamanho e ordem de desenho preservados.
5. Salvar, sair de Play, recarregar e conferir cópias e exclusões; confirmar colisões e regras intactas.

Nenhuma sessão Unity, teste automatizado ou build foi iniciada nesta entrega. Revisão estática não confirma a execução do importador nem a aparência final em jogo.
