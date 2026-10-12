# Cabana — base visual integrada

Atualização após teste do usuário: fogão restaurado com corpo, bancada inclinada, proporções e contato no pé das paredes corrigidos. Consulte `CABANA_AJUSTES_VISUAIS_2026-10-11.md`; a verificação direcionada nova está documentada ali. O restante abaixo registra a montagem inicial.

Data: 11/10/2026. Escopo escolhido pelo usuário: **montagem visual primeiro; mecânicas depois**. Sem execução de Unity, testes automatizados, capturas do jogo ou builds. Revisão estática e preparação dos arquivos de arte somente.

## Solução de arte

Uma base grande e contínua (`Assets/Resources/Cabin/CabinBase.png`) contém o terreno, lago, chão de madeira, paredes da cabana sem teto, cerca, portão e caixa de correio. Não há junções entre tiles no piso/paredes: fazem parte da mesma pintura. Móveis, personagem, lenha e tronco são sprites separados para permitir mudanças de estado futuramente. A base não é a captura conceitual com os móveis pintados: esses objetos foram removidos da pintura para evitar duplicação.

Originais preservados em `Assets/Art/Cabin` e `Assets/Art/Player`; cópias de uso em `Assets/Resources/Cabin`. Seus importadores originais, inclusive recortes existentes, não foram alterados. `CabinArt` carrega texturas e cria sprites por retângulos de alpha registrados em `ArtBounds.json`; cada cópia tem metadados de importação próprios, sem precisar abrir uma sessão Unity para prepará-los. PNGs não foram redesenhados pelo script de preparação.

Referências: Darkwood para atmosfera/abandono/materiais, Bullet Echo para circulação/legibilidade; imagens do próprio projeto como referências diretas. Arte gerada com image_gen integrado.

## Montagem no código

- `CabinScenery` define base de 44 × 24,75 unidades, móveis e colisões, com entrada na parede inferior.
- `CampController` substitui barraca, rio e píer geométricos pela montagem da cabana; conserva o objeto `CampPlayer`, inventário pessoal, menus e comandos existentes.
- Cama, baú, fogão, bancada quebrada, lenha e personagem usam arte separada. A bancada improvisada também foi preparada, mas não aparece automaticamente como reparada nesta etapa.
- Baú da cabana recebeu sprite específico baseado no conceito; nenhuma alteração em loot ou imitadores da expedição.
- Colliders separados para paredes, móveis, cerca, portão e margem do lago; entrada da cabana permanece livre. Os contornos seguem coordenadas aproximadas da pintura, ainda sujeitos à conferência prática do usuário.
- Player usa a base desarmada e gira seu visual conforme o movimento; sem usar o teste de caminhada descartado.
- Câmera segue o player com limites da base. O limite é removido ao definir outro alvo, preservando o acompanhamento da expedição.
- Rótulos passam a cabana/fogão/acesso ao lago. Temas da HUD e áreas de interação existentes foram preservados.
- Âncoras dos fixtures antigos de acampamento foram atualizadas para o novo layout, mas os fixtures não foram executados.

## Regras mantidas por decisão de escopo

Dormir/curar, abastecer fogão, reparar bancada e cortar tronco com recompensa/quebra do machado **não foram implementados**. Cama é visual; fogão usa o cozimento existente; bancada quebrada ainda usa os comandos existentes de fabricação.

O campo persistente legado `PierRepaired` e a ação `Repair` continuam controlando a disponibilidade da pesca, com o custo antigo de 20 madeiras + 10 pregos. A interface agora chama essa ação de habilitar pesca. O tronco é cosmético, sem collider de bloqueio próprio, e desaparece se o estado antigo indicar pesca disponível. Não se apresenta esse comportamento como a nova mecânica de machado/tronco aprovada para uma próxima etapa. Nenhum progresso antigo foi migrado ou apagado.

## Arquivos de arte

`CabinBase`, `LakeLog` e `Chest` foram gerados nesta etapa. `Bed`, `Stove`, `BenchBroken`, `BenchImprovised`, `Player` e `Firewood` reaproveitam PNGs existentes. Tudo está em `Assets/Resources/Cabin`.

Preparador de cópias/metadados: `Art/CabinConcept/prepare-cabin-assets.cjs`. Os guids existentes são preservados em repetição. As dimensões/retângulos são de arte, não evidência de desempenho ou validação em runtime.

## Prompts novos

### CabinBase

Use case: precise-object-edit. Edit the referenced approved cabin game-view scene into ONE CONTINUOUS STATIC MAP BASE TEXTURE for the actual top-down Unity game. Keep rustic earthy woodland art style and the overall layout: roof-hidden cabin on LEFT, enclosed pond on RIGHT, yard below cabin, broken fence along left and lower property boundary, damaged gate and old mailbox at lower boundary. CRITICAL remove ALL movable/interactable objects from this base: REMOVE bed, chest, workbench, stove, fuel wood pile, chairs, barrels, cups, loose props, rugs, player, axe and fallen log. Repaint those removed interior areas as continuous EMPTY weathered wooden plank floor. Cabin must contain only floor and structural wall tops/windows/door opening. Pond and path to fishing bank remain clear WITHOUT log. No pier. Ground dirt, static rocks and foliage, pond water and shore, fence/gate/mailbox stay. Canvas landscape 16:9, painted to every edge, opaque. Strict directly overhead orthographic game map view, no perspective camera tilt and no visible skyline. Cabin rectangle outer extent approximately x=14% to51% of full canvas, y=10% to66% from TOP; floor inside is empty and rectangular. Sole doorway is a THREE-PERSON-WIDE opening centered in the BOTTOM wall around x=31% to38%, allowing movement into yard. Thick aged dark wood wall tops clearly distinct from floor. Yard/paths form uncluttered routes from doorway to gate and to pond bank. Keep pond water on right within its organic shore, with stones/reeds restricting bank except clear path approach. Generous visible walkable area. NO UI, NO text, NO arrows, NO characters, NO furniture, NO environmental interactable log. This is an empty static base to place individual furniture sprites over, NOT a finished composition containing those objects. Uniform scale for all features. Warm worn browns, muted olive, charcoal, subdued blue-green water. Avoid deep black shadows obscuring floor geometry.

### LakeLog

Use case: stylized-concept. Use the referenced scene only as material and design reference. Create ONE isolated top-down FALLEN TREE TRUNK with one RUSTY AXE EMBEDDED in a partially cut notch near its middle. This is a separate game prop for the narrow passage to the lake. Genuine directly overhead 90-degree orthographic view, long axis HORIZONTAL, broad heavy mossy bark trunk with pale chopped wood notch, split old brown handle and dark rusted iron axe head blade buried in notch, handle points diagonally upper-right so recognizable from above. Trunk still in ONE CONNECTED PIECE, cut is incomplete: do NOT show two severed halves, no detached logs. A few tiny wood chips immediately around cut can be included within compact silhouette. No rocks, brambles, dirt, pond, path, scenery or background. TRUE transparent alpha everywhere around trunk and axe, no brown gradient, no black/brown rectangular mat, no outside cast shadow. Original gritty painted timber style matching abandoned cabin, restrained olive moss and earthy bark, no people, no hands, no labels, no glow. Horizontal landscape 3:2 image, whole log visible centered with plenty of transparent margin.

### Baú exclusivo da cabana

Use case: stylized-concept. Create ONE isolated top-down wooden STORAGE CHEST sprite matching the chest near the bed in the attached cabin concept. Reference is style/design only. Strict directly overhead 90 degree orthographic plan view: rectangular wooden TOP LID visible, two dark rusted iron bands across lid, simple small lock fitting at front/bottom edge, no broad front-facing chest elevation, no open lid. Aged brown timber, subtle moss-staining, scarred plank seams, original gritty hand-painted survival horror art. Entire closed chest centered with generous margins on square canvas. True transparent alpha background, no floor or room, no outside cast shadow, no text, no label, no extra objects. This is a cabin-only furniture sprite, not a replacement for any existing loot/mimic system. Keep modest realistic proportions and clear silhouette.

