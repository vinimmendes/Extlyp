# Lista de sprites pendentes

Atualização de 10/10/2026: os 18 ícones de itens abaixo foram gerados, aprovados e ligados ao código do jogo. Consulte `Docs/Integracao/ICONES_ITENS_2026-10-10.md` para o estado de verificação. Charger v1 e Carapaça v1 também foram aprovados como propostas, ainda sem integração. O restante do documento registra o levantamento original.

Data: 10/10/2026. Levantamento estático dos PNGs, prefabs, importadores e código atual. Não houve execução em Play Mode; referências de carregamento indicam integração no código, não confirmação visual em execução. Nenhum prefab, cena ou regra de jogo foi alterado.

“Sem sprite próprio” significa que o elemento usa círculos, quadrados, polígonos, linhas ou letras provisórias, sem arte dedicada encontrada. Isso não obriga que todo efeito ou controle receba uma imagem: alguns podem continuar procedurais.

## 1. Monstros sem arte própria — nove tipos

Fonte: `Assets/Scripts/Enemies/MonsterArchetypes.cs`, `MonsterAppearance.cs` e `Assets/Prefabs/Monster.prefab`. Todos têm comportamento existente. Os nomes abaixo seguem os rótulos do código; podem ser revistos na direção de arte.

| Pendente | Identificador | Visual atual |
| --- | --- | --- |
| [ ] Monstro de investida | Charger | Círculo vermelho do prefab |
| [ ] Escutador | Listener | Corpo circular, antenas e olhos geométricos |
| [ ] Carapaça | Shell | Círculo, casco e placas poligonais |
| [ ] Tecelã | Weaver | Corpo circular e pernas de segmentos |
| [ ] Berrante | Herald | Círculos para corpo e garganta |
| [ ] Imitador revelado | Mimic | Polígono, mandíbula, dentes e pernas geométricas |
| [ ] Corrosivo | Acid | Corpo triangular e boca circular |
| [ ] Espreitador | Stalker | Polígono alongado e olhos |
| [ ] Devorador | Devourer | Círculos para corpo/boca e dentes triangulares |

O disfarce do imitador precisa usar a mesma arte do baú comum para preservar o comportamento existente. Preparação de habilidades, transformação, alimentação e crescimento são estados já presentes; suas variantes visuais podem ser planejadas junto dos sprites, sem assumir agora uma quantidade de quadros.

## 2. Itens sem ícones próprios — 18 entradas

Fonte: `Assets/Scripts/Items/ItemCatalog.cs`. `Icon()` retorna abreviações de texto. O inventário e a barra rápida usam essas abreviações. Loot solto usa o quadrado do prefab com cor; itens largados no acampamento também são blocos coloridos (`LootPickup.cs` e `CampController.cs`).

| Pendente | Identificador |
| --- | --- |
| [ ] Madeira | Wood |
| [ ] Pregos | Nails |
| [ ] Bandagem | Bandage |
| [ ] Metal | Metal |
| [ ] Pólvora | Gunpowder |
| [ ] Componentes | AmmoComponent |
| [ ] Pistola isolada | Pistol |
| [ ] Munição de pistola | PistolAmmo |
| [ ] Peixe comum | FishCommon |
| [ ] Peixe raro | FishRare |
| [ ] Peixe grande | FishLarge |
| [ ] Peixe assado | CookedFish |
| [ ] Peixe queimado | BurntFish |
| [ ] Peixe cru | RawFish |
| [ ] Rifle de ferrolho isolado | Mosin |
| [ ] Munição de rifle | RifleAmmo |
| [ ] Espingarda de dois canos isolada | DoubleBarrelShotgun |
| [ ] Cartucho de espingarda | ShotgunShell |

As armas seguradas pelo personagem já têm versões aprovadas, mas isso não fornece automaticamente um ícone isolado para inventário ou uma arma vista de cima para o chão. Os 18 itens precisam de representação de inventário e de loot; decidir depois quais imagens podem ser reutilizadas entre as duas funções. O desenho de bandagem da folha de status existente também pode ser avaliado para reaproveitamento.

## 3. Baús e cadáveres

- [ ] Baú comum, incluindo representação de aberto/fechado caso a direção de arte escolha distinguir esses estados.
- [ ] Variante de baú de maior raridade, atualmente diferenciada por cor. Pode reutilizar a base.
- [ ] Disfarce do imitador: reutilizar exatamente a arte do baú comum, sem criar diferença que revele o monstro.
- [ ] Cadáver no chão: atualmente um círculo vermelho escuro compartilhado pelos eventos de morte. Decidir se humanos e monstros terão imagens distintas.

Fontes: `ChestAppearance` em `MonsterAppearance.cs`, `LootPickup.OnNetworkSpawn()` e `MonsterWorld.Draw()`. Estados de abertura são regras existentes; não há sprite dedicado encontrado para eles.

## 4. Cenário da expedição ainda geométrico

Fontes: `Assets/Scripts/Match/MapBuilder.cs` e `MapDressing.cs`.

- [ ] Solo de floresta.
- [ ] Trilha de terra.
- [ ] Água dos lagos e transição para a margem.
- [ ] Piso de casas/ruínas e tábuas desgastadas.
- [ ] Paredes de casas, ruínas e trechos quebrados; cantos e passagens podem fazer parte de um conjunto modular.
- [ ] Barreiras do mapa e do cercado de teste, atualmente retângulos cinza. Definir depois o material visual.
- [ ] Caixotes e coberturas sólidas.
- [ ] Cadeira quebrada.
- [ ] Porta caída.
- [ ] Entulho e fragmentos espalhados.
- [ ] Representação do ponto de extração, hoje uma área retangular verde translúcida. Não há um objeto físico definido no código; escolher sua aparência é uma decisão de produto, não evidência de que exista um portão ou portal na expedição.

Armário, cama, mesa, gabinete, árvore com folhas e árvore morta já têm sprites na folha de cenário. Não entram como arte ausente. A decoração com esses sprites está ligada ao mapa grande; o mapa de teste mantém seu cenário simples.

## 5. Acampamento ainda geométrico

Fonte: `Assets/Scripts/Camp/CampController.cs`, que cria o cenário em `Awake()` com blocos e círculos.

- [ ] Barraca.
- [ ] Depósito/baú de armazenamento: avaliar reaproveitamento do baú comum com identificação visual adequada.
- [ ] Bancada de fabricação.
- [ ] Fogueira.
- [ ] Entrada/postes de saída do acampamento.
- [ ] Píer danificado e reparado: hoje apenas muda a cor das tábuas.
- [ ] Solo e caminho: avaliar reaproveitamento do conjunto de terreno da expedição.
- [ ] Rio e margem: avaliar reaproveitamento do conjunto de água.

As árvores do acampamento continuam círculos, mas existem sprites de árvores no projeto: é pendência de integração/reaproveitamento. O avatar do acampamento também é um círculo e pode usar a base aprovada do player; não precisa automaticamente de outro desenho.

## 6. Efeitos visuais sem textura própria

Os efeitos abaixo existem e usam formas/linhas. Esta é uma lista para avaliar acabamento, não uma exigência de gerar um sprite para cada efeito.

- [ ] Nuvem de esporos — círculo translúcido (`SporeCloud.cs`).
- [ ] Projétil de teia — círculo (`MonsterWorld.Draw()`).
- [ ] Teia no chão — polígono com fios de retângulos.
- [ ] Projétil de ácido — círculo.
- [ ] Poça de ácido — círculo.
- [ ] Onda do grito do berrante — círculo que aumenta.
- [ ] Partículas de cura/maturação das plantas — círculos, quadrados e linhas (`PlantVisual.cs`); os corpos das plantas já têm arte.

Rastro de tiro (`ShotPresentation.cs`), indicador de investida (`Monster.cs`), cone de visão e neblina (`PlayerVision.cs`) são procedurais e podem permanecer assim. Flash de disparo, impactos ou sangue adicionais seriam propostas de acabamento, não sprites faltantes de objetos encontrados neste levantamento.

## 7. Interface: pendências de arte e reaproveitamento

- [ ] Peixe e boia do minijogo de pesca: atualmente círculos e retângulos (`MinigameView.cs`). O peixe poderá reutilizar arte de item.
- [ ] Avaliar pictogramas para os botões de ação por toque, hoje desenhados com formas/texto (`TouchControls.cs`), somente se desejarmos esse acabamento.

Inventário/quickbar: os 18 ícones faltantes estão na seção de itens, sem duplicar a contagem. Barras, painéis, marcadores do minimapa e controles simples não precisam automaticamente de sprites.

## 8. Arte que já existe — integração separada

Atualização em 2026-10-11: o personagem 3D rústico e seu ciclo de caminhada foram aprovados e integrados por `PlayerCharacterVisual` para player, bots humanos e base. O conjunto em uso está em `Assets/Resources/PlayerCharacter/`, com poses desarmada, pistola, Mosin e escopeta. O quadro central é a pose parada. A tabela e os comentários abaixo registram o levantamento anterior; os PNGs estáticos antigos e o teste de caminhada rejeitado continuam preservados como referências. Detalhes em `Docs/Integracao/PERSONAGEM_ANIMADO_2026-10-11.md`.

| Arte existente | Estado encontrado |
| --- | --- |
| Player desarmado — `player-rustic-idle-east-v1.png` | Base existente; prefab ainda usa `ShapeVisual` |
| Player com pistola — `player-rustic-pistol-east-v2.png` | Versão aprovada nesta conversa, ainda sem referência no gameplay |
| Player com rifle — `player-rustic-bolt-rifle-east-v2.png` | Versão aprovada nesta conversa, ainda sem referência no gameplay |
| Player com espingarda — `player-rustic-shotgun-east-v4.png` | Versão aprovada nesta conversa, ainda sem referência no gameplay |
| Esporífero — `Assets/Art/Esporifero/Esporifero.png` | Referenciado por `idleSprite` no prefab do esporífero |
| Planta-mãe, bulbo atacante, bulbo curador, filho — `PlantMonsterSheet.png` | Quatro recortes carregados por `PlantVisual.SpriteFor()` |
| Árvore com folhas, árvore morta, armário, cama, mesa, gabinete — `AbandonedProps.png` | Seis recortes no importador e carregamento por `EnvironmentSprites`/`MapDressing` |
| Sete ícones de status — `Art/StatusIcons/status-icons-v1.png` | Proposta existente fora de Assets; README indica ainda não integrada ao HUD. Avaliar adequação ao visual rústico atual |

Bots ainda usam um círculo e armas procedurais. Como são humanos, podem reutilizar o conjunto do player; uma aparência exclusiva de bot é uma decisão posterior. `WeaponVisual.cs` ainda monta as armas com retângulos; substituir esse visual por sprites já aprovados é integração, não falta de arte do personagem armado.

As versões antigas das armas não são as escolhidas. O teste de caminhada foi rejeitado e deve permanecer fora do conjunto aprovado e da integração. Nenhuma nova animação foi encomendada por este levantamento.

## Uso futuro desta lista

As maiores lacunas concretas de desenho são os nove monstros, os 18 ícones de itens, os baús e os objetos/terrenos geométricos do cenário e do acampamento. Reutilização de árvores, player, ícones e mobiliário deve ser resolvida como integração para evitar geração duplicada. Quantidades de variações, animações e efeitos finais ficam para a definição de cada conjunto.
