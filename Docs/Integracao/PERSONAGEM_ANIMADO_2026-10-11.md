# Personagem rústico animado — integração

O visual 3D aprovado foi exportado para sprites transparentes e integrado à apresentação 2D existente. Unity 6000.0.84f1, URP 2D, Netcode 2.13.3 e Transport 2.7.4 permanecem como configurados.

## Comportamento

- Expedição: player local, jogadores remotos e bots humanos usam o mesmo personagem. A pose acompanha a arma equipada: desarmado, pistola, Mosin ou escopeta de dois canos.
- Base: avatar usa a versão desarmada, com braços em repouso. Mantém o giro pela direção de movimento e a escala compatível com o colisor existente.
- O ciclo avança somente com deslocamento real observado no mundo, proporcional à distância. Segurar movimento contra uma parede sem deslocamento ou apenas girar a mira não avança as pernas.
- Ao parar, retorna ao quadro 9 do Blender (índice 8), pose central com pernas ocultas sob o corpo. O corpo físico parado retorna imediatamente; clientes com posições recebidas pela rede conservam o último quadro por até 80 ms entre atualizações, sem avançar a animação nesse intervalo.
- Espera/fim da partida, morte e grandes saltos de posição usam pose parada. Reativação do avatar reinicia a medição de posição, evitando interpretar a troca de ambiente como caminhada.
- A simulação de movimento, colisão, tiro e inventário continua autoritativa no servidor. Nenhum novo RPC ou estado persistente foi acrescentado.

## Integração

`PlayerShooter.Awake` já instalava `WeaponVisual`; esse ponto passa a instalar `PlayerCharacterVisual`, que usa o SpriteRenderer do ator. O avatar local da base instala a mesma apresentação em um filho com Rigidbody2D no pai. Os prefabs e a cena existentes não foram recriados.

O material continua sendo o shader de sprites URP 2D usado pelo projeto. A animação troca apenas `SpriteRenderer.sprite`, preservando a ocultação de `VisibilityTarget` e a cor de morte do inventário. O círculo provisório e o marcador branco de direção deixam de compor o personagem. Servidores sem dispositivo gráfico não carregam os sprites.

## Assets e origem

Quatro folhas em `Assets/Resources/PlayerCharacter/`, com 32 quadros de 256×256 por folha, organização 8×4, textura 2048×1024, bilinear, alpha e sem mipmaps. Câmera, enquadramento, pivô central e ciclo de pernas são os mesmos entre os equipamentos. Sprites criados e armazenados em cache uma vez por processo. Texturas sem compressão para preservar o recorte; conjunto completo ocupa aproximadamente 32 MiB de textura RGBA, carregado conforme uso.

Pistola: renders aprovados em `Prototypes/PlayerWalk3D/VisualV1/topo/`. Demais poses: exportadas do mesmo `.blend` por `export_game_character.py`, sem modificar os ossos ou as chaves das pernas. `package_game_character.py` monta as folhas e metadados de importação. Os `.blend` e prévias anteriores continuam preservados fora de Assets.

## Verificação

Revisão estática da instalação do componente, seleção de equipamento, pivôs, ordem dos quadros, pose parada, material, visibilidade e integração local/remota. As poses exportadas foram inspecionadas visualmente durante a produção dos assets. Conforme a preferência do projeto, não foram executados testes automatizados, Play Mode Unity, sessões de clientes ou builds de validação. A integração ainda precisa da avaliação prática do usuário.

Para essa avaliação: andar/parar na base e expedição; girar parado; pressionar contra uma parede; trocar armas andando e parado; observar outro cliente e bots; conferir ocultação pela visão, morte e retorno à base.
