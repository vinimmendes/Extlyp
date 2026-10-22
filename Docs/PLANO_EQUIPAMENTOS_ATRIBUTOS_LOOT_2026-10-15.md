# Equipamentos, atributos, níveis e loot

Plano de design e implementação — 15/10/2026. Este documento não representa sistemas já implementados.

Andamento em 15/10/2026: **etapa 1 implementada no código, com revisão estática e validação prática pendente**. Ver [dados e salvamento da etapa 1](Integracao/EQUIPAMENTOS_ETAPA_1_2026-10-15.md). As demais etapas continuam planejadas.

Atualização: **etapa 2 também implementada no código, com validação prática pendente**. Ver [slots e operações da etapa 2](Integracao/EQUIPAMENTOS_ETAPA_2_2026-10-15.md). Etapas 3–10 permanecem planejadas.

## 1. Decisões e objetivo

### Confirmado pelo usuário

- Seis slots: cabeça, torso, mãos, pés, amuleto e anel; dois slots de arma separados.
- Três especializações por slot de proteção/acessório: 18 modelos-base de equipamento.
- Exemplares com atributos sorteados, sem transformar cada raridade em uma simples substituição obrigatória.
- Progressão persistente com níveis; vantagem de poder moderada no PvP.
- Seis atributos: Força, Destreza, Vigor, Percepção, Vitalidade e Inteligência.
- Percepção modifica somente a abertura da visão: 80° a 110° nas Catacumbas, alcance de 10 unidades.
- Experiência por PvE, exploração, objetivos e PvP; morte preserva metade da experiência da expedição.
- Redistribuir atributos custa moeda, na base.
- Venda de itens e compra de suprimentos básicos; equipamentos superiores vêm da exploração.
- Equipados separados da mochila de nove espaços, sem sistema de peso nesta versão.
- Primeira entrega funcional inclui corrida com stamina e melee equipado. Magia fica para depois.

### Padrões propostos neste plano

Os números, nomes e limites abaixo são propostas iniciais de balanceamento, ainda sem avaliação prática. Para tornar a implementação concreta, o plano assume: sistema compartilhado entre Floresta e Catacumbas; troca de armadura/acessório em 2 segundos na expedição; Inteligência reservada e indisponível para investir/sortear até a magia funcionar. Essas três escolhas foram perguntadas anteriormente, mas ainda não receberam resposta.

Objetivo: cada expedição pode render equipamento útil, materiais, moeda e experiência; o jogador deve escolher o que carregar e quando extrair. Bons itens ampliam uma especialização, sem tornar personagens iniciantes incapazes de competir.

## 2. Slots, inventário e perda

| Área | Capacidade | Função |
| --- | --- | --- |
| Equipamento | 6 peças | Bônus apenas enquanto equipadas |
| Armas | 2 armas | Qualquer combinação das armas disponíveis; uma ativa por vez |
| Mochila | 9 espaços | Saque, armas extras, munição, ferramentas e consumíveis |
| Baú da base | 9 espaços atuais | Armazenamento seguro, acessível somente na base |

Equipamentos e armas ocupam um espaço cada quando guardados. Materiais e consumíveis preservam pilhas de até 20, exceto limites específicos existentes. Itens equipados não continuam duplicados na mochila. Armas guardadas na mochila não entram na alternância rápida.

Na base, equipar e desequipar é imediato. Na expedição, armaduras/acessórios exigem 2 segundos parado; movimento, dano, ataque ou fechamento da janela cancela. Não consumir ou transferir itens antes da confirmação. Troca direta substitui a peça no mesmo espaço da mochila; desequipar requer espaço livre. Não permitir equipar diretamente de um recipiente: primeiro transferir para a mochila. Alternar entre as duas armas mantém os tempos próprios de troca já existentes; alterar o conteúdo dos slots de arma exige a mesma canalização de 2 segundos.

Morte derruba mochila, peças equipadas e armas equipadas uma única vez. Extração devolve o conjunto carregado à base. Tempo esgotado segue a perda existente; desconexão durante partida segue a política atual de perda, sem proteção adicional. Baú da base, níveis, pontos e moeda já bancados permanecem. Preservar as exceções existentes das ferramentas exclusivas da expedição.

Ao mudar máximos de vida/stamina, conservar o valor atual absoluto, limitado ao novo máximo; aumentar o máximo não cura nem recupera energia. Bloquear mudança de equipamento durante ataque, recarga, extração ou outra interação em andamento. Sem durabilidade, reparo, peso ou bônus de conjunto nesta versão.

## 3. Os 18 modelos-base

Cada modelo fixa o atributo principal e permite um secundário dentre os dois da tabela. A quantidade e o valor dos bônus dependem da raridade. As especializações são alternativas, não degraus de evolução.

| Slot | Modelo-base | Principal | Secundários possíveis | Papel |
| --- | --- | --- | --- | --- |
| Cabeça | Capuz do Vigia | Percepção | Destreza / Vigor | Ler melhor os flancos |
| Cabeça | Elmo do Guardião | Vitalidade | Força / Vigor | Suportar confronto próximo |
| Cabeça | Máscara do Atirador | Destreza | Percepção / Vitalidade | Precisão e observação |
| Torso | Gibão do Batedor | Vigor | Destreza / Percepção | Sustentar deslocamento |
| Torso | Cota do Combatente | Força | Vitalidade / Vigor | Especialização melee |
| Torso | Couraça do Guardião | Vitalidade | Força / Percepção | Resistência |
| Mãos | Luvas do Atirador | Destreza | Percepção / Vigor | Estreitar a mira |
| Mãos | Manoplas do Combatente | Força | Vigor / Vitalidade | Dano corpo a corpo |
| Mãos | Luvas do Explorador | Percepção | Destreza / Vitalidade | Exploração e vigilância |
| Pés | Botas do Andarilho | Vigor | Percepção / Destreza | Reserva de energia |
| Pés | Botas do Duelista | Destreza | Força / Vigor | Precisão híbrida |
| Pés | Grevas do Guardião | Vitalidade | Força / Vigor | Sobrevivência |
| Amuleto | Medalhão do Olho | Percepção | Destreza / Vitalidade | Visão periférica |
| Amuleto | Talismã do Fôlego | Vigor | Força / Vitalidade | Corrida e melee |
| Amuleto | Presa do Combatente | Força | Vigor / Percepção | Especialização ofensiva |
| Anel | Anel da Mão Firme | Destreza | Percepção / Vigor | Precisão |
| Anel | Anel do Pulso Forte | Força | Vitalidade / Destreza | Combate híbrido |
| Anel | Anel da Persistência | Vitalidade | Vigor / Percepção | Sobrevivência |

Inteligência terá modelos/pools próprios quando magia entrar; não introduzir agora peças com bônus inativos. Os nomes são de trabalho. Não atribuir velocidade de movimento a equipamentos nesta versão: primeiro estabilizar a relação de mobilidade entre protagonista e monstros.

### Raridade e sorteio do exemplar

| Raridade | Bônus principal | Bônus secundário | Proteção da cabeça | Proteção do torso |
| --- | --- | --- | --- | --- |
| Comum | +1 ou +2 | Nenhum | 2% | 4% |
| Incomum | +2 ou +3 | +1 | 3% | 6% |
| Rara | +3 ou +4 | +1 ou +2 | 4% | 8% |

Sortear valores inteiros uniformemente nos intervalos, e o secundário com 50% para cada opção elegível. O exemplar mantém seus resultados ao ser movido, vendido, extraído ou roubado. Cabeça e torso somam redução de dano físico; máximo atual de 12%, com teto sistêmico de 15%. Demais slots não concedem proteção implícita. Veneno e dano ambiental ignoram essa proteção. Não haverá crítico, resistência elemental, roubo de vida, rerrolagem ou encantamento nesta entrega.

Armas preservam características próprias; sua raridade não multiplica o dano nesta versão. O sorteio de atributos começa nas 18 peças. Evitar escalar simultaneamente nível, dano-base da arma, raridade e múltiplos bônus ofensivos.

## 4. Atributos e limites

Todos começam em 1. Valor efetivo = base + pontos investidos + equipamentos, limitado a 20. Permitir no máximo 10 pontos investidos em cada atributo; somas acima de 20 aparecem como excedente sem efeito na comparação. Não existem requisitos mínimos de atributo/nível para equipar.

| Atributo | Efeito inicial proposto | Limite no valor efetivo 20 |
| --- | --- | --- |
| Força | +2% de dano melee por ponto acima de 1 | +38% |
| Destreza | Reduz a dispersão da arma em 1,5% por ponto acima de 1 | −28,5% |
| Vigor | Stamina máxima = 100 + 3 × (valor − 1) | 157 |
| Percepção | Catacumbas: 80° + 30° × (valor − 1) / 19 | 110° |
| Vitalidade | Vida máxima = 100 + (valor − 1), antes de comida | 119 |
| Inteligência | Reservada para magia; investimento indisponível inicialmente | Sem efeito ativo nesta entrega |

Destreza multiplica o cone calculado pela arma e pelo movimento; não remove a penalidade de andar, não altera alcance, velocidade de recarga, cadência ou espalhamento estrutural dos projéteis da espingarda. Distinguir a dispersão de apontamento do padrão de pellets antes de integrar o modificador. O servidor usa o mesmo cone mostrado pela mira.

Na Floresta, proposta de Percepção: abertura entre 110° e os 140° atuais, pela mesma fórmula normalizada; alcance continua em 16 unidades. Nas Catacumbas, alcance de 10 e raio próximo de 2,4 permanecem. O atributo não muda câmera, atravessa paredes, destaca inimigos ou amplia audição. Atualizar todos os consumidores da visão individual, incluindo alvos, baús, efeitos, sons visualizados, marcadores e lógica de monstros que depende de serem vistos.

Comida preserva o bônus de vida existente; para stamina, propor +15% de regeneração enquanto Bem alimentado. Não aumentar capacidade e regeneração simultaneamente por esse status. Inteligência aparece na ficha como futura, sem botão de investimento nem afixos elegíveis.

## 5. Níveis, experiência e redistribuição

- Nível inicial 1; teto inicial 20. Cada nível concede 1 ponto: 19 pontos distribuíveis ao todo. Não conceder vida, dano ou velocidade automaticamente ao subir de nível.
- XP para passar do nível L ao seguinte: `100 + 50 × (L − 1)`. Total até nível 20: 10.450 XP. XP excedente no teto não vira dinheiro nem pontos.
- Contabilizar XP durante a expedição em um saldo provisório. Na extração: 100% desse saldo + 20% de bônus. Na morte, tempo esgotado ou desconexão durante partida: 50%, arredondado para baixo. Não retirar XP já confirmado.
- Confirmar resultado uma única vez por ID de expedição. Se o servidor recuperar uma expedição abandonada, aplicar somente o saldo que já tiver sido registrado, sem inventar ações. Antes do início efetivo, rollback preserva equipamento e não concede XP.
- Redistribuição na base por `50 + 25 × nível` moedas. Preço exibido antes de confirmar; devolve somente pontos investidos e mantém os concedidos por nível. Sem redistribuição durante expedição.

| Fonte de XP | Valor proposto | Regra |
| --- | --- | --- |
| Monstro comum | 10 | Uma recompensa por morte autoritativa |
| Monstro de ameaça alta existente | 20 | Categoria autorada; não inferir pela vida restante |
| Primeira visita a sala/setor de exploração | 5 | Uma vez por jogador e região, até 40 por expedição |
| Preparar mecanismo de extração | 25 | Uma vez por mecanismo, para quem o conclui |
| Abrir atalho relevante | 15 | Uma vez por objetivo, para quem o conclui |
| Eliminar participante humano em PvP | 20 | Máximo 60 por expedição; mesmo par de contas recompensa uma vez a cada 24 h |

Monstros: dividir a recompensa proporcionalmente ao dano válido causado nos últimos 15 segundos por participantes vivos; usar arredondamento com distribuição do restante para não criar XP extra. PvP: XP para o golpe final, excluindo suicídio, ambiente sem autor e bots. Assistências não dão XP adicional nesta versão. Eventos de exploração/objetivo contam após o início da partida e não podem ser repetidos abrindo/fechando interfaces. O limite de PvP reduz repetição, mas não garante impedir combinação entre várias contas; esse risco permanece explícito.

Bots não acumulam progresso persistente. Seus pontos/equipamentos vêm de presets temporários; a dificuldade não recebe visão, dano ou atributos acima dos limites dos jogadores. Kills de bots não rendem XP PvP nem moedas garantidas.

## 6. Corrida e melee: primeira versão funcional

Stamina é autoritativa no servidor e compartilhada por corrida e melee. Proposta: caminhada preserva inicialmente a velocidade atual de 4 u/s; corrida 5,4 u/s, consumo de 20 stamina/s; regeneração de 15/s após 1,5 s sem gastar. Correr exige movimento e pelo menos 10 stamina para iniciar. Bloquear disparo e melee durante corrida; atacar encerra a corrida. Não permitir regeneração durante gasto ou preparação/recuperação de golpe. Vigor aumenta capacidade, não velocidade nem taxa-base de recuperação.

Essas velocidades são ponto inicial para avaliação, não resolução automática do relato de vantagem sobre monstros. A calibração de passo e a relação com cada perseguidor devem ser avaliadas antes de fechar os valores. Desenhar os frames pelo deslocamento real, incluindo lentidão e corrida.

| Arma melee | Dano-base | Alcance | Arco total | Preparação / recuperação | Stamina |
| --- | --- | --- | --- | --- | --- |
| Espada curta | 22 | 1,5 u | 70° | 0,25 / 0,45 s | 15 |
| Machado | 32 | 1,4 u | 85° | 0,40 / 0,65 s | 23 |
| Lança | 20 | 2,2 u | 30° | 0,30 / 0,50 s | 14 |

Comando de ataque usa a arma ativa, mantendo a escolha de melee equipado. Consumir stamina ao iniciar golpe aceito; impedir spam e ataques sem recurso. Direção trava após preparação. No impacto, revalidar distância, arco e parede; no máximo três alvos distintos por golpe, uma aplicação em cada, sem atravessar cobertura física. Não devolver stamina em erro do jogador/cancelamento; não consumir em comando rejeitado. A arma não acerta antes do frame de impacto. Sem esquiva, bloqueio, combos, empurrão ou dano a aliados de facção nesta versão; entre participantes, manter PvP existente, sem introduzir equipes novas.

## 7. Remodelagem do loot

### Categorias e motivos para explorar

1. Equipamento: os 18 modelos e seus exemplares sorteados.
2. Armas: três armas de fogo existentes e três melee; identidade mecânica fixa.
3. Suprimentos: cura, munição e ferramentas consumíveis existentes.
4. Materiais: madeira, pregos, metal, pólvora e componentes, preservando receitas atuais.
5. Tesouros: novos itens de venda — moeda antiga, relicário e ídolo funerário, valendo 10/30/80 moedas por unidade; pilha máxima 5.
6. Objetivos: manivela, chaves e itens de mecanismos seguem regras autoradas, fora do sorteio geral.

Remover a tabela universal como responsável por todas as fontes. Cada fonte aponta para um `LootTableDefinition`, com categorias elegíveis, presença, quantidade e raridades. Um contexto indica mapa, fonte, tema e risco autorado; não usar nível do jogador para melhorar os drops. O servidor sorteia uma vez ao criar/preencher a fonte. Abrir, ocultar ou reconectar não sorteia novamente.

| Fonte | Presença / quantidade | Categorias e pesos propostos |
| --- | --- | --- |
| Saque solto comum | 70%; 1 pilha | Materiais 55, suprimentos 35, tesouros 10 |
| Recipiente de suprimentos | 100%; 2 pilhas | Suprimentos 70, materiais 30 |
| Recipiente comum | 90%; 2–3 pilhas, 50/50 | Materiais 30, suprimentos 30, equipamento 20, armas 5, tesouros 15 |
| Recipiente valioso/fechado | 100%; 3–4 pilhas, 50/50 | Garantir 1 equipamento; demais: tesouros 40, suprimentos 30, materiais 20, armas 10 |
| Monstro | 60%; 1 pilha | Materiais temáticos 60, suprimentos 25, tesouros 15 |
| Cadáver de participante | Conteúdo real | Nunca sortear, elevar qualidade ou reabastecer |

Por bundle: no máximo uma arma e um equipamento; repetir sorteio somente entre categorias ainda permitidas, renormalizando pesos. Não repetir a mesma definição no bundle; se o pool elegível acabar, encerrar sem fallback de item arbitrário. No recipiente valioso, equipamento garantido já ocupa a cota. Evitar duplicação de material dentro do mesmo bundle consolidando pilhas compatíveis até o limite.

### Raridade por risco da fonte

| Risco autorado | Comum | Incomum | Rara |
| --- | --- | --- | --- |
| Baixo | 80% | 19% | 1% |
| Médio | 60% | 35% | 5% |
| Alto | 35% | 50% | 15% |

Sortear raridade somente para peças que a utilizam; a qualidade/tempo de abertura de um recipiente continua conceito separado. Um baú difícil de abrir não garante peça rara. A garantia é uma peça equipável, sem viés pela build ou por slot vazio do jogador.

Floresta: manter os pontos de loot e classificar saque solto/recipientes comuns como risco baixo; recipientes fechados existentes como médio. Catacumbas: pontos comuns de risco baixo; baús valiosos das salas 12/19 como médio; baús valiosos das salas 20–23 como alto. Registrar esses riscos como dados autorados, sem inferência de distância ao centro. Novos desafios poderão mudar a classificação após avaliação; não ampliar o número de spawns nesta reforma de loot.

Floresta privilegia materiais industriais e suprimentos; Catacumbas, equipamento e tesouros. Concretizar pela restrição de pools por mapa mantendo os pesos da fonte: tesouros funerários apenas nas Catacumbas; na Floresta, usar as mesmas três faixas de preço com sucata valiosa, objeto antigo e documento de coleção. Manter probabilidades de munição coerentes com armas disponíveis, sem loot obrigatório para uma única arma carregada. Quantidades dos materiais/munições preservam os intervalos atuais na primeira avaliação; raridade não aumenta automaticamente esses intervalos.

Não vender drops obrigatórios de chefes, crafting de equipamentos, novos materiais elementais ou distribuição de elites como parte implícita desta etapa. Esses recursos pertencem ao plano de encontros e terão tabelas próprias quando existirem.

## 8. Moeda e comércio da base

Moeda persistente pertence ao perfil; na expedição o jogador encontra itens físicos de venda, sem crédito automático na carteira. Vender exige estar na base, confirmar item/quantidade e executar uma transação no servidor. Materiais e suprimentos também podem ser vendidos; objetivos de expedição não podem.

| Item/operação | Preço inicial proposto |
| --- | --- |
| Vender equipamento comum/incomum/raro | 8 / 20 / 45 |
| Vender arma de fogo ou melee | 12 por unidade; sem bonificação por munição carregada |
| Vender madeira, pregos, metal | 1 por unidade |
| Vender pólvora ou componentes | 3 por unidade |
| Vender munição | 1 por cartucho |
| Vender bandagem | 2 por unidade |
| Vender ferramenta comum não obrigatória | 5 por unidade |
| Vender peixe comum/raro/grande | 2 / 5 / 8 |
| Vender alimento cozido/cru/queimado | 3 / 1 / 0 |
| Comprar bandagem | 8 por unidade |
| Comprar munição | Pistola: 12 por 24 moedas; rifle: 5 por 20; espingarda: 4 por 20 |
| Comprar espada curta básica sem afixos | 30 |
| Comprar pistola básica | 40 |

Manter a compra existente de componentes por metal; não criar conversão direta entre metal e moeda. Não oferecer as 18 peças à venda, sortear estoque ou vender rerrolagens. Preservar recuperação básica gratuita já existente quando o perfil estiver sem armas, contando também as armas equipadas nessa verificação. Itens de recuperação são vinculados à expedição até a primeira extração bem-sucedida: não podem ser vendidos, descartados para outra conta ou gerar recompensa em cadáver; remanescentes tornam-se normais somente ao extrair. Isso precisa de uma marca de origem preservada no save/rede, sem reutilizar o campo de munição. Repetir morte/recuperação não deve gerar dinheiro gratuito.

Na venda de item com munição carregada, exibir aviso de que a carga não aumenta o preço; permitir descarregar para a mochila se houver espaço, preservando a munição como item separado. Todos os cálculos usam preço do servidor, sem aceitar valores enviados pelo cliente.

## 9. Interfaces e experiência de uso

- Base: ficha do personagem com nível, XP, pontos livres, seis atributos e os oito slots; acesso ao inventário/baú e ao comerciante sem sobrepor janelas.
- Expedição: equipamento e mochila individual; ao saquear, mochila e recipiente na mesma janela existente. Baú seguro somente na base. Priorizar área central livre quando as janelas estiverem fechadas.
- Comparação: mostrar raridade, atributo principal/secundário, bônus efetivo e diferença ao substituir; indicar excedentes acima do teto. Exibir valor de venda e duração de troca.
- Itens sorteados recebem nome do modelo e descrição dos bônus; não codificar importância apenas por cor. Raridade tem nome e borda. Sem prefixos/sufixos textuais gerados nesta versão.
- HUD: acrescentar stamina e botão de corrida independente/personalizável; PC usa Shift segurado, celular botão segurado. Ataque existente muda ícone conforme arma ativa. Sem barra de mana enquanto magia não existir.
- Resultados: mostrar itens extraídos, XP de ações, parcela preservada/perdida, bônus de extração e níveis ganhos. A moeda cresce somente após vendas na base.
- Mesma estrutura nos temas Simples/Sombria, uGUI editável, área segura e paisagem esquerda/direita. Atualização por eventos; regras de equipamento e comércio não ficam na UI.

## 10. Arquitetura, dados e migração

### Interfaces de domínio previstas

- `EquipmentSlot`: os seis slots e WeaponA/WeaponB. `AttributeId`: os seis atributos. `ItemDefinition`: catálogo por ID estável com categoria, slot, modelo visual, pool de bônus e preço. `ItemInstance`: identidade única do equipamento, raridade, bônus efetivamente sorteados e marca de origem de recuperação. Consumíveis empilháveis continuam sem rolagens individuais.
- `CharacterProgress`: XP confirmado, nível derivado, pontos investidos/livres e moeda. `EquipmentLoadout`: oito referências a itens reais. `CharacterStats`: uma função de cálculo compartilhada recebe progresso/equipamento/status e devolve valores efetivos; não duplicar fórmulas na HUD e no servidor.
- `LootTableDefinition` e contexto de loot: dados de autoramento em ScriptableObjects, resultados no estado autoritativo. Progresso, exemplares e estado de expedição ficam no repositório de perfis, nunca em assets compartilhados.
- Comandos: equipar/desequipar, investir/redistribuir, vender/comprar e iniciar/parar corrida. Cliente envia intenção e referência/quantidade; servidor valida propriedade, fase, recurso, slot, revisão e distância quando aplicável. Usar revisão/ID de comando para reenvios não duplicarem compra, venda ou itens.

Não sobrecarregar `ItemStack.Data`, atualmente usado por munição de armas, com atributos aleatórios. Adicionar referência de exemplar e manter seus metadados no save e nos snapshots privados; transferir os metadados necessários junto do item quando ele muda de dono. Itens equipáveis não empilham, mesmo com bônus iguais. Reservar transações para impedir que o mesmo exemplar exista simultaneamente em mochila, equipamento, chão e baú.

Estender o escrow existente para incluir equipamento, origem dos itens e XP pendente; commit/rollback/extração/perda resolvem o conjunto atomicamente. Subir versão do perfil e protocolo de rede, exigir cliente/servidor atualizados e preservar IDs legados de itens. Salvar backup antes da migração e usar migração idempotente.

Perfis antigos começam no nível 1, 0 XP confirmado, 0 moedas e atributos em 1, sem perda de inventário. Migrar até duas armas da mochila para WeaponA/B pela ordem dos espaços, incluindo a arma ativa no primeiro slot quando identificável; extras ficam guardadas. Preservar munição carregada, comida, baú e chão da base. Gerar identidade de exemplar para armas legadas sem rerrolar seus dados. Não migrar perfis com expedição ativa: resolver o escrow anterior pelas regras existentes antes de converter a versão. Não transformar automaticamente os itens de jogadores antigos em peças aleatórias.

Preservar Unity 6000.0.84f1, NGO 2.13.3 e Transport 2.7.4. Fazer mudanças pontuais, sem regenerar cenas/prefabs via MvpSetup.Build. O laboratório usa estado temporário e não concede XP/moeda/itens ao perfil normal.

## 11. Ordem de implementação e critérios de entrega

| Etapa | O que fazer | Entrega e critério de conclusão |
| --- | --- | --- |
| 1 — Preparar os dados | Criar identidade dos equipamentos, estrutura de atributos e salvamento versionado; preparar migração e backup | Os itens atuais são preservados, inclusive munição, e cada equipamento tem identidade única. Sem mudar o loot ainda. |
| 2 — Criar os slots | Implementar seis slots de proteção/acessórios e dois de arma, separados da mochila; incluir ficha e transferências | Equipar, trocar, guardar, extrair e perder itens funciona pelo servidor sem duplicação. Usar peças provisórias no laboratório até a etapa 7. |
| 3 — Adicionar stamina e corrida | Implementar recurso, consumo, regeneração, comando de corrida e apresentação na HUD | Correr consome stamina; esgotamento interrompe a corrida; a animação acompanha a velocidade real. |
| 4 — Adicionar melee | Implementar espada curta primeiro; depois machado e lança, com alcance, impacto e custo próprios | As três armas podem ser equipadas e usadas; golpes respeitam stamina, paredes e os tempos do servidor. |
| 5 — Aplicar os atributos | Ligar Força ao melee, Destreza à mira, Vigor à stamina, Vitalidade à vida e Percepção à visão | Cada atributo tem efeito real e limites iguais no cliente/servidor; Inteligência permanece reservada conforme proposta. Ainda sem distribuição pública de pontos. |
| 6 — Criar níveis e XP | Implementar experiência, níveis, pontos de atributo, recompensas e regras de morte/extração | Jogador evolui, investe pontos e mantém a parcela correta de XP. Cada expedição confirma recompensas uma única vez. Redistribuição paga entra na etapa 9. |
| 7 — Criar as 18 peças | Cadastrar três especializações por slot, bônus sorteados, raridades e ícones; completar comparação visual | Cada peça tem função identificável; exemplares preservam seus bônus entre inventário, equipamento e save. Arte de corpo vestível não é requisito desta versão. |
| 8 — Refazer o loot | Substituir sorteios gerais por tabelas de fonte/mapa/risco; distribuir equipamento, armas, suprimentos, materiais e tesouros | Floresta e Catacumbas recebem recompensas coerentes conforme a proposta; reabrir/reconectar não rerrola e itens de objetivo continuam garantidos. |
| 9 — Criar comércio e redistribuição | Implementar moeda, venda, compra de suprimentos e redistribuição paga de atributos | Operações ocorrem na base, sem saldo negativo, recompensa duplicada ou geração de moeda pela recuperação básica. |
| 10 — Fechar resultados e balanceamento | Finalizar resumo de XP/loot e revisar valores a partir da avaliação prática do usuário | Fica claro o que foi extraído, perdido e ganho; especializações têm utilidade sem tornar nível/equipamento uma vitória automática no PvP. |

A sequência acima substitui a divisão anterior de seis etapas; o escopo detalhado das seções anteriores permanece. Concluir uma etapa por vez, revisar o código e registrar o que foi entregue antes de avançar. Não iniciar testes, sessões Unity ou builds automaticamente: seguir a preferência de avaliação prática do usuário e a seção abaixo.

Atualização em 16/10/2026: etapa 3 implementada no código, com revisão estática e validação prática pendente. Valores, controles, compatibilidade e limites estão em [Stamina e corrida — etapa 3](Integracao/EQUIPAMENTOS_ETAPA_3_2026-10-16.md). Próxima entrega: etapa 4, melee.

Atualização seguinte em 16/10/2026: etapa 4 implementada no código para espada curta, machado e lança, com revisão estática e validação prática pendente. Consulte [Combate corpo a corpo — etapa 4](Integracao/EQUIPAMENTOS_ETAPA_4_2026-10-16.md), incluindo o limite visual: silhuetas e movimento de arma provisórios, sem novos atlas de animação corporal. Próxima entrega: etapa 5, aplicar os atributos.

Atualização da etapa 5 em 16/10/2026: efeitos de Força, Destreza, Vigor, Vitalidade e Percepção implementados no código, com limites compartilhados, conservação dos recursos atuais e visão individual. Inteligência permanece reservada, sem distribuição pública de pontos. Consulte [Efeitos dos atributos — etapa 5](Integracao/EQUIPAMENTOS_ETAPA_5_2026-10-16.md), com revisão estática e validação prática pendente. Próxima entrega: etapa 6, níveis e XP.

Atualização da etapa 6 em 16/10/2026: níveis, XP provisório/confirmado, distribuição de pontos na base, recompensas e recuperação implementados no código. Consulte [Níveis, XP e pontos — etapa 6](Integracao/EQUIPAMENTOS_ETAPA_6_2026-10-16.md), com revisão estática e validação prática pendente. Próxima entrega: etapa 7, as 18 peças definitivas.

Atualização da etapa 7 em 16/10/2026: catálogo com 18 peças, três raridades, bônus persistentes, proteção física, ícones próprios e comparação implementados no código. As peças estão disponíveis no laboratório; a distribuição normal permanece para a etapa 8. Consulte [As 18 peças — etapa 7](Integracao/EQUIPAMENTOS_ETAPA_7_2026-10-16.md), com revisão estática e validação prática pendente. Próxima entrega: etapa 8, refazer o loot.

Atualização da etapa 8 em 16/10/2026: sorteio geral substituído por dez tabelas por fonte/mapa, riscos autorados, distribuição das 18 peças e seis tesouros físicos. Baús valiosos/fechados garantem uma peça; objetivos seguem fora do sorteio, e cadáveres mantêm itens reais. Consulte [Loot por fonte, mapa e risco — etapa 8](Integracao/EQUIPAMENTOS_ETAPA_8_2026-10-16.md), com revisão estática e validação prática pendente. Próxima entrega: etapa 9, comércio e redistribuição.

Atualização da etapa 9 em 16/10/2026: comércio na base com preços do servidor, saldo persistente, confirmação de venda/compra, descarga de armas, redistribuição paga e proteção da origem dos itens/munição de recuperação implementados no código e no prefab existente. Consulte [Comércio e redistribuição — etapa 9](Integracao/EQUIPAMENTOS_ETAPA_9_2026-10-16.md), com revisão estática e validação prática pendente. Próxima entrega: etapa 10, resultados e balanceamento.

Antes da etapa 1, confirmar as três propostas ainda sem resposta da seção 1: abrangência nos mapas, troca de peças em 2 segundos e Inteligência reservada. Nomes e números permanecem propostas de configuração inicial, não decisões adicionais já aprovadas.

Primeiro marco jogável de combate: etapas 1–4. Segundo marco: etapas 5–7, com atributos, níveis e equipamentos completos. Terceiro marco: etapas 8–10, com loot e economia conectados. Não liberar pontos investíveis em atributos sem efeito funcional. Produção da arte das peças na etapa 7 usa as skills adequadas e preserva assets existentes.

### Revisão e validação

Na implementação, seguir a preferência do projeto: revisão estática e avaliação prática do usuário primeiro; não iniciar automaticamente testes, Play Mode ou builds por causa deste plano. As capturas/testes de visão anteriores não validam esses sistemas futuros.

Quando houver pedido de testes ou relato de defeito, verificar de forma focada:

- Migração repetida e recuperação de escrow sem perder munição, equipamento ou comida.
- Transferências simultâneas, coleta por dois jogadores, inventário cheio e rejeição de exemplar inexistente/duplicado.
- Morte, extração, expiração e desconexão aplicando perda/XP uma vez; rollback antes da partida sem prêmio.
- Limites dos atributos, comparação com teto, redistribuição sem saldo e tentativa de usar Inteligência inativa.
- Visão com Percepção diferente em dois clientes, oclusão por paredes e máscara colorida coerente com replicação.
- Melee sem atravessar parede ou acertar o mesmo alvo duas vezes; stamina/velocidade não manipuláveis pelo cliente.
- Reabrir/reconectar sem rerrolar loot; tabelas sem objetivos perdidos e sem afixos de atributos inativos.
- PvP repetido entre o mesmo par, bots sem XP PvP e recuperação básica sem geração gratuita de moeda.
- UI horizontal nos dois temas, safe area, troca cancelada e resultado compreensível no celular.

Nomes, números, probabilidades e preços são a configuração inicial proposta. Medir depois: duração de luta/fuga, sobrevivência por equipamento, rendimento por expedição, tempo até o próximo nível e valor de venda por fonte. Ajustar dados com base nessas observações, mantendo as regras já aprovadas pelo usuário.
