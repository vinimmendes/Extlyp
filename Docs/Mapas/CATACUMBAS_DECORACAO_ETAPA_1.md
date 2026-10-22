# Decoração das Catacumbas — etapa 1

DungeonDetails acrescenta agrupamentos estáticos ao mapa existente, além dos detalhes anteriores. Alvo de 72 grupos: dois por sala (24 salas, incluindo as oito entradas) e 24 nas bordas dos corredores. Cada grupo contém dois ou três fragmentos em uma célula de 1,06 unidade, com variantes e rotações determinísticas.

## Arte reutilizada

Cerâmica quebrada CAT-P10 e famílias existentes CAT-D02/03/06/07/10/11/12: raízes rasteiras, musgo, ossos esparsos, velas, correntes, tecidos e pequenos detritos. A composição varia por setor: ossos/raízes no ossário; correntes e tecidos na prisão; cerâmica, cera e suportes nas galerias/capela; umidade e ferragens na cisterna; detritos e correntes nos contrapesos. Não foram geradas novas imagens, madeira ou caixas; esses volumes ficam para a arte dos quebráveis da etapa seguinte.

Peças pequenas, planas, dessaturadas, sem halo e sem aparência de pickup. Desenho em Default, ordem -7, acompanhando os detalhes de piso e a composição de visão atual. Direção de abandono de Darkwood e leitura espacial de Bullet Echo, usando a arte própria já integrada.

## Distribuição e reservas

Salas procuram posições laterais, visitando bordas opostas antes de outras bordas; os corredores usam uma varredura determinística intercalada no mapa e somente células adjacentes à pedra. Não há sorteio por partida nesta etapa. Espaçamento mínimo entre centros: três unidades nas salas e quatro nos corredores.

Cada grupo verifica a pegada completa de uma célula contra piso, água, coberturas baixas, bounds de decoração existente e reservas de interação. Portas recebem uma reserva adicional nos dois lados, com duas células de profundidade e tolerância lateral. Reservas existentes preservadas: spawns 3, extrações 5, ferramentas de progressão 3, inimigos 2 e trajeto de saída do barco. Loot e mecanismos usam agora 2,8 unidades.

Se uma sala ou corredor não comportar a quantidade prevista sem violar reservas, o grupo é omitido. Um aviso informa as contagens de salas e corredores se não alcançarem 48 e 24; não reduzir reservas para atingir a meta. A contagem real ainda não foi observada em execução.

## Limites e revisão

Somente GameObjects de apresentação e SpriteRenderers; sem colliders, componentes de rede, luzes, perigos, drops ou quebráveis. Não altera DungeonLayout, caminhos dos bots, extrações ou saves. Servidor com dispositivo gráfico Null continua sem criar a decoração. Sprites próprios são liberados junto ao componente; objetos filhos acompanham a destruição do mapa.

Revisão estática realizada: sintaxe C#, pegadas dos fragmentos dentro da célula, famílias/manifestos, reservas, espaçamento, ausência de colisão e ciclo de vida. Sem iniciar Unity, testes automatizados ou builds. Avaliação prática pendente: preenchimento por sala, contagem de grupos, rotas/portas livres visualmente, leitura de inimigos e loot, escuridão e escala no celular. Nenhuma medição de desempenho realizada.
