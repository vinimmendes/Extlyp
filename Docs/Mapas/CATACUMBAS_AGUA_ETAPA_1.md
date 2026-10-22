# Água — etapa 1: superfície animada

Continuação implementada: [etapa 2 — margens e correnteza](CATACUMBAS_AGUA_ETAPA_2.md). A descrição abaixo registra a configuração da primeira etapa.

Implementada em `Assets/Resources/DungeonWater.shader`, integrada por `DungeonTerrain` à camada Water e ao sprite FuneralChannelSurface. Os sprites CAT-F07 existentes permanecem como base. Direção: água pesada e escura, movimento lento e contraste contido, seguindo abandono de Darkwood e leitura espacial de Bullet Echo, com apresentação própria.

Dois campos senoidais em direções diferentes modulam suavemente a luminosidade e produzem pequenos brilhos de superfície. A fase usa coordenadas mundiais, sem reiniciar em cada tile. A amostragem e a transparência dos sprites permanecem fixas: nenhuma deformação da margem ou leitura de pixels vizinhos de atlas. A etapa não introduz correnteza específica do canal, anéis ou goteiras.

Parâmetros no shader (valores iniciais):

| Parâmetro | Valor | Uso |
| --- | --- | --- |
| Wave scale | 1,4 | Frequência espacial das ondulações |
| Wave speed | 0,28 | Velocidade do movimento; zero congela |
| Surface variation | 0,045 | Variação multiplicativa de luminosidade |
| Reflection strength | 0,025 | Intensidade dos brilhos ilustrativos, sem reflexão de cena |

Um material compartilhado entre as duas superfícies é criado durante a montagem e destruído com os demais recursos do terreno. A animação usa o tempo do shader, sem Update, partículas, câmeras adicionais ou atualização de rede. Usa uma amostragem de textura por fragmento. Sem medição de desempenho nesta entrega. O servidor sem dispositivo gráfico já retorna antes da criação do terreno. Shader ausente ou não suportado preserva material estático e registra aviso.

Ordem de desenho, margens, degraus submersos, passarela, colisões, navegação e regras de extração não foram alterados. O efeito usa o caminho Universal2D existente, antes da composição de visão. A oclusão final precisa de avaliação em execução.

Revisão estática: sintaxe C#, vínculos de recurso, destruição do material e integração somente nos dois renderizadores de água. Não houve compilação de shader, sessão Unity, testes automatizados ou build. Na avaliação prática, conferir continuidade entre tiles, intensidade na câmera do jogo, ponte acima da água, oclusão pela escuridão e aparência no Android. Etapas 2–4 permanecem pendentes.
