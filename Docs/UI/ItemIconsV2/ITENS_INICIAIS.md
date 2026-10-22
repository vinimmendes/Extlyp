# Itens iniciais — besta, espada enferrujada e virotes

Atualização: a besta recebeu [animações Blender de caminhada/mira, disparo e recarga](../../../Prototypes/PlayerWalk3D/HandCrossbow/README.md). A seção de funcionamento abaixo descreve a entrega inicial anterior a essa integração. Os ícones aprovados permanecem inalterados.

## Revisão visual da besta

### Revisão 3 — comparação com referências e simetria

Referências: [Man Kung MK-80A1](https://www.mankungcrossbows.com/crossbows/MK-80A1.htm) e [Umarex Red Back, descrição ilustrada](https://www.umarex.com/fileadmin/manuals/2.2300.pdf). Usadas para a organização visual do arco transversal, trilho central e corda; materiais medievais próprios, sem copiar aparência industrial.

Na revisão 2, as curvas dos braços eram diferentes e o trecho inferior da corda atravessava visualmente a lateral da madeira. A perspectiva não apresentava um plano coerente. A revisão 3 substitui essa arte por vista superior ortográfica, apontando para cima, braços de contornos equivalentes, trilho central e dois trechos de corda convergindo no retentor sobre o corpo. A empunhadura fica vista de cima, sob a região traseira, sem forçar uma vista lateral incompatível.

Arte selecionada e prompt: `Art/ItemIconsV2/crossbow-revision-v3.json`; arquivo preservado: `Art/ItemIconsV2/Originals/ImprovisedHandCrossbow-v3.png`. Cópias canônicas de interface/mundo atualizadas mantendo GUID, importação e dimensões da HUD. Rotação visual na mão alterada para -90° para alinhar a nova frente ao sentido de mira. Texturas têm desgaste natural; simetria refere-se à construção e silhueta, não à identidade de cada pixel.

Inspeção dos PNGs confirmou RGBA, transparência, margens e as 62 referências do catálogo. Sem sessão Unity, testes automatizados de jogo ou build; apresentação no aparelho ainda depende de avaliação prática.

### Histórico da revisão 2 (substituída)

Após a crítica do usuário, a estrutura foi refeita: arco na frente, corda em V com ambos os trechos chegando ao retentor traseiro, trilho aberto apontando para a frente e empunhadura abaixo do mecanismo, em vez da extensão alinhada ao corpo. A versão selecionada usa eixo horizontal, materiais mais separados e menos amarrações. A correção preserva o estilo pintado e os tamanhos de importação/exibição existentes.

A arte anterior está preservada em `Art/ItemIconsV2/Originals/ImprovisedHandCrossbow-v1.png`; a selecionada também está em `ImprovisedHandCrossbow-v2.png`. O arquivo canônico de interface/mundo foi atualizado preservando o GUID, portanto as referências existentes passam a usar a nova imagem. O registro de prompts, tentativas e seleção está em `Art/ItemIconsV2/crossbow-revision-v2.json`, produzido com a ferramenta integrada `image_gen`. A galeria e o levantamento foram atualizados.

A orientação do sprite na mão e a referência de saída do disparo foram ajustadas à nova estrutura. Sem mudanças de atributos, munição, kit inicial ou controles. Inspeção de alpha/metadados aprovada e C# compilado sem erros; sem Unity, testes automatizados ou builds. Encaixe visual na mão e leitura no aparelho ainda aguardam avaliação prática.

## Entrega

Três itens novos, com IDs acrescentados ao final do enum, sem renumerar os 59 itens existentes:

| ID | Item | Tipo |
|---|---|---|
| 59 | Besta de mão improvisada | Arma à distância de uma mão; usa virotes |
| 60 | Espada enferrujada | Arma corpo a corpo |
| 61 | Virotes | Munição, pilha máxima 20 |

O kit inicial passa a conter uma besta descarregada, uma espada enferrujada e exatamente cinco virotes na mochila. Não há virote adicional carregado nem gazuas no kit padrão. O parâmetro explícito de gazuas de `InventoryGrid.Starter(true)` permanece disponível para consumidores que o solicitem.

Os novos perfis recebem o kit pelo fluxo existente. Ao iniciar a expedição, `FillWeapons` equipa a besta e a espada nos dois slots de arma. Perfis existentes não são apagados ou convertidos: armas, munições e progressão já adquiridas permanecem. O equipamento básico de recuperação também passa a usar esse conjunto, mantendo a condição atual de só conceder quando não houver qualquer arma no equipamento, mochila, baú ou reserva. A pré-verificação de espaço considera os três itens; itens vinculados continuam sujeitos às mesmas restrições.

## Arte e tamanhos

[Prévia em 32, 48, 64, 96 e 128 pixels](iniciais.html) e [levantamento dos PNGs](iniciais-levantamento.json).

- Três sprites pintados produzidos com `image_gen` integrado, fundo RGBA transparente, silhuetas grandes e sem textos, contadores, preços ou molduras embutidos.
- Originais de 1254×1254 preservados em `Art/ItemIconsV2/Originals/`. Prompts completos individuais na pasta `Art/ItemIconsV2/`; seleção e prompts de geração em `starter-selected.json` e `starter-prompt.json`.
- Cópias de runtime em `Assets/Resources/Items/IconsV2/`, com o mesmo metadado-base da pistola V2: Single Sprite, Bilinear, Clamp, sRGB, sem mipmaps/compressão e limite 512 nos perfis Default/Standalone/Android.
- `ItemVisualCatalog` tem agora 62 entradas de interface. Os três itens novos têm referências WorldIcon explícitas para seus sprites; os 59 WorldIcon anteriores permanecem inalterados.
- Não foram criadas texturas redundantes de 32–128 px: as variações de exibição usam o sprite importado nos mesmos retângulos atuais, preservando proporção. A galeria é uma prévia de arte, não simula a filtragem/Canvas do aparelho.
- Mochila, baú, equipamento, comércio, saque, arraste e mostrador de arma usam o resolvedor comum. HudMetrics, perfis, áreas de toque, posições e preferências de layout não foram alterados. Os placeholders de slots vazios também permanecem.

## Funcionamento e balanceamento inicial adotado

O usuário definiu os itens e quantidades, mas não seus atributos. Foram adotados os valores abaixo para uma primeira avaliação; não representam balanceamento aprovado em partida.

| Atributo | Besta de mão improvisada | Espada enferrujada |
|---|---|---|
| Dano | 30, com queda após 8 u | 16 |
| Alcance | 12 u | 1,4 u |
| Capacidade | 1 virote | — |
| Recarga | 2 s | — |
| Intervalo | 1 s | Preparação 0,3 s; recuperação 0,5 s |
| Stamina | Regras atuais de arma à distância | 15 |
| Ruído de disparo | Raio 4 u | Regras atuais de corpo a corpo |

Recarga, gasto de munição, dispersão, acertos, dano, restrições e temporizadores usam o fluxo autoritativo atual. A besta usa a resolução de disparo existente por raycast; não foi criado projétil físico, recuperação de virotes disparados ou novo sistema de trajetória. A trilha visual é mais fina e discreta. Não reutiliza áudio de tiro de pistola; áudio específico da besta permanece sem asset nesta entrega.

O reciclador de cartuchos ignora munições sem receita, evitando divisão por zero e geração de metal ao disparar virotes. Não foram acrescentadas receitas, ofertas ou alterações à rotação de 12 mercadorias. Vender os novos itens normais segue os preços genéricos atuais de arma/munição; recuperação vinculada continua invendável.

A espada usa seu sprite novo também na mão, com tamanho normalizado e correção da orientação diagonal. A besta é apresentada como sprite de arma sobre a pose sem arma, sem desenhar pistola no personagem. Não foram produzidas animações Blender específicas de segurar/armar a besta. A recarga mantém seu temporizador e indicadores atuais. As duas armas estão disponíveis nas ferramentas de laboratório existentes, somente em modo de teste.

## Compatibilidade e revisão

Protocolo atualizado de 37 para **38**, para impedir conexão de versões antigas que desconhecem os IDs novos. Atualizar cliente e servidor juntos. Estrutura de serialização e esquema de perfil 5 preservados; nenhum reset, mudança de identidade ou migração destrutiva de inventários existentes.

- C# compilado isoladamente com referências atuais da Unity 6000.0.84f1, sem erros.
- Inspeção somente de leitura dos três PNGs: RGBA, transparência, margem de corpo opaco, dimensões, referências, importação e identidade das cópias/originais.
- Revisão dos caminhos de inicialização, recuperação/previsão de espaço, validação de perfil, equipar/trocar, recarga, morte/extração, ícones de mundo e desenho da arma.
- Ferramentas de preparação e cobertura de ícones atualizadas para 62 itens; nenhuma arte antiga foi substituída.

Não foram iniciados Unity, testes automatizados do jogo ou builds. Leitura no celular, pose das armas, combate e recarga em rede ainda dependem da avaliação prática. Conferir um perfil novo ou o fluxo de recuperação sem apagar o perfil atual.
