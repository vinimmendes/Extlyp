# Catacumbas — etapa 3.7: detalhes e atmosfera

Lote CAT-D01 a CAT-D12 produzido e integrado: 37 variantes de raízes, musgo, teias, casulos, ossos esparsos, velas, umidade, marcas antigas de arrasto, correntes, tecidos e detritos. Arte medieval original com materiais abandonados e infestação orgânica, seguindo Darkwood; densidade e distribuição seguem a leitura de combate e circulação de Bullet Echo.

## Distribuição

`DungeonDetails` é a última camada estática do mapa, após pisos, arquitetura, sepulturas, marcos e extrações. Usa a mesma geometria autorada e as reservas dos elementos já existentes.

- Somente as 16 salas internas recebem o lote; antecâmaras e corredores mantêm a apresentação anterior.
- Limite de três grupos de parede e dois decalques de piso por sala: no máximo 80 instâncias adicionais, com quantidade efetiva menor quando não há encaixe. Não força a colocação de todas as variantes no cenário.
- Raízes grandes, teias, casulos, velas, correntes e tecidos ocupam exclusivamente células de alvenaria já bloqueadas, adjacentes ao piso da sala. A família pode girar em 90° para acompanhar uma borda lateral. Não cria raízes altas, mobiliário, casulos ou faixas suspensas sobre trajetos caminháveis.
- Raízes de piso, musgo, ossos, umidade, arrasto e detritos ficam na primeira faixa de células junto à alvenaria, sobre piso integralmente livre e seco. O centro da sala e as bocas de corredores não recebem esses decalques. Nenhuma peça invade água ou cobertura baixa.
- Há separação entre os retângulos das peças novas e sprites existentes de arquitetura, props, marcos e extrações, inclusive a passarela inicialmente inativa. Áreas sem encaixe simplesmente ficam vazias.
- Reservas são medidas até a borda do retângulo do detalhe: 5 u em torno das extrações, 3 u nos spawns/pedestais, 2,5 u nos mecanismos e 2 u em torno de loot/posições iniciais de encontros. A faixa de partida já existente da barca também fica reservada.

Ossário e cisternas concentram raízes e teias. Casulos aparecem somente em nichos A08 vazios dos ossários, com no máximo um por sala e corpo reduzido a 0,45 célula para caber na moldura. A montagem preserva os nichos já preenchidos por ossos, lápides ou urnas e omite o casulo se não houver vaga. Galerias, claustro e capela recebem tecidos e velas em pequenas doses; a prisão e os contrapesos recebem correntes. Cada setor usa duas famílias de piso compatíveis: ossário com raízes/ossos, cisterna com musgo/umidade, prisão com marcas antigas/umidade, capela com fragmentos/cera. Não são oito biomas novos.

## Leitura e atmosfera

Os sprites de parede usam ordem -3 e permanecem fora do piso. Os decalques usam ordem -7, abaixo dos atores e coberturas. Opacidade é limitada por família: raízes de piso 0,38; musgo 0,45; ossos 0,42; umidade 0,28; marcas antigas 0,25; detritos 0,38. Teias decorativas usam 0,30, casulos 0,55 e demais materiais de parede 0,65; velas usam 0,90. A transparência reduz a disputa com silhuetas, mas o resultado sob fog precisa de avaliação prática.

Teias são estáticas, assimétricas e em alvenaria, sem collider, dano ou desaceleração. Não substituem a teia ativa da aranha. Ossos são pequenos e esparsos, afastados dos pontos iniciais dos monstros; os três monstros mantêm sua arte e comportamento.

Velas têm variantes apagada, acesa e consumida; a acesa possui uma chama pequena no sprite. Não há halo embutido, luz dinâmica, bloom, partícula ou alteração da visão/fog. Tecidos não têm cores ou símbolos de equipe. Marcas de arrasto são narrativa fixa: não registram jogadores reais e não indicam movimento recente.

Não foram acrescentados RPCs, estado de rede, colisores, hazards, coleta, loot, requisitos ou tempos. A montagem não escreve nas máscaras do mapa, nem reage ao progresso do jogador. Não há `Update`, timers ou consultas por frame nesta camada. Não há medição de desempenho nem promessa de ganho.

## Arquivos e preparação

- Fontes RGBA intactas e prompts individuais: `Art/Catacumbas/Detalhes/{Fontes,Prompts}`.
- Manifesto consolidado de prompts e galeria: `Art/Catacumbas/Detalhes/{prompts-v1.json,index.html}`. A galeria mostra arte isolada, não uma captura da Unity.
- Recursos: `Assets/Resources/Environment/Dungeon/Details`, incluindo `DetailsArt.json` com retângulos normalizados do corpo.
- Preparação: `Tools/Prepare-DungeonDetails.py`, derivada do empacotamento anterior. Analisa alpha e copia as fontes sem editar pixels, preservando arquivo existente diferente e GUIDs existentes.

Geração com imagegen integrado, uma chamada por variante, com os prompts exatos salvos no projeto. Importação sRGB, bilinear, Clamp, sem mipmaps ou compressão; limite de textura calculado para a referência de 128 px por célula no corpo recortado. A margem transparente é excluída por `Sprite.Create`, sem alterar a fonte.

O carregamento exige todas as variantes antes da montagem. Falha de recurso cancela apenas a camada nova, sem ocultar renderers anteriores. Servidor sem dispositivo gráfico não cria os detalhes. Sprites temporários pertencem ao componente e são descartados com o mapa; fontes e texturas de Resources permanecem sob gerenciamento da Unity.

Skills de mapa, C# e imagegen aplicadas à Unity 6000.0.84f1, sem alteração de engine, pacotes, URP, materiais ou rede.

## Verificação e limites

Revisão estática das reservas, máscaras, densidade, ordem de construção e descarte; inspeção visual das 37 fontes produzidas. Sem testes automatizados, sessão Unity ou build nesta entrega, conforme a preferência do projeto. Quantidade efetiva de instâncias, contraste, distinção de teia ativa, escala e leitura no Android aguardam avaliação prática. A próxima etapa 3.8 revisa o conjunto; não acrescenta outro lote de sprites por padrão e não equivale a validação em execução sem um teste solicitado pelo usuário.
