# Proposta — Refúgio sob as ruínas

Status: planejamento e conceito visual; sem alteração da interface em execução ou da direção aprovada. A solicitação atual propõe evoluir a identidade para medieval sombrio. Os detalhes góticos discretos abaixo são uma revisão proposta da restrição anterior contra ornamentação medieval, não uma mudança já aplicada.

## Direção

Preservar a composição Antes da descida: navegação persistente, Preparativos à esquerda, destino ao centro, Expedição à direita. Fazer o caráter medieval aparecer nos materiais, na tipografia dos títulos e nos pequenos acabamentos. Hierarquia funcional: destino e Partir primeiro, capacidade e suprimentos depois, recursos e ações secundárias em seguida.

Materiais: ferro escurecido, couro quase preto, bronze oxidado e marfim de papel envelhecido. Painéis com interior quieto e quase opaco; textura de baixa variação restrita às superfícies, sem sujeira sobre texto. Molduras finas com pequenos cantos inspirados em arcos ogivais. Evitar correntes, brasões, pergaminhos enrolados e caveiras repetidas.

Paleta candidata: fundo #101310; superfície #1A1B17; borda ferro #494940; texto #E6D9BA; secundário #B9AE95; destaque bronze #9D7448. São propostas para testar contraste em todos os estados, não cores já validadas. Ação principal em bronze; ações secundárias escuras. Verde de conexão acompanhado de texto; vermelho reservado a perigo/erro. Não usar vermelho decorativo no Partir.

Tipografia: serifada romana sóbria apenas em marca, destino e títulos. Manter Barlow nos números, mensagens, abas e botões. O projeto atualmente contém Barlow e Liberation Sans, sem uma serifada de títulos: selecionar fonte com licença compatível, acentos portugueses e atlas TMP adequado durante a implementação. Evitar fonte gótica no texto corrente.

## Organização por página

- Preparação: capacidade com 12 segmentos, suprimentos legíveis e somente status ativos. Organizar mochila e Personagem neutros. Centro com arte do destino e título sobre escurecimento controlado. Painel direito mantém destino, conexão, consequência da morte e Partir.
- Inventário: mochila 4×3 e baú 4×4, slots uniformes, fundo neutro discreto, quantidade dentro da borda e detalhes na faixa inferior. Nenhuma decoração invade o item ou sua quantidade.
- Personagem: oito equipamentos organizados no corpo, mochila ao lado, comparação e redistribuição existentes. Slots vazios continuam com sprites cinza a 18%; arma A vazia usa a besta de uma mão. Equipamentos equipados aparecem somente aqui.
- Comerciante: vitrine atual 4×3, estoque/preço legíveis, seleção e confirmação no painel. Preservar regras de rotação e exemplares.
- Atividades: bancada, cozinha e pesca com ilustrações originais discretas e nomes claros. Preparações e minigames mantêm o fluxo atual.

## Comportamento e leitura

Manter aproximadamente 24/46/30, altura virtual 600, controles de pelo menos 48 unidades e textos correntes 18–20. Reduzir primeiro a decoração central em telas estreitas. Conteúdo limitado a 1200, fundo expandido em ultrawide. Todos os controles respeitam área segura em paisagem esquerda/direita.

Abas persistentes com rótulo, sublinhado de seleção e foco separado visível. Não depender de hover. Retorno/Esc mantém o fluxo existente, fechando primeiro seleção/confirmação. Operações pendentes mostram estado e bloqueiam ações incompatíveis. Transições curtas, cerca de 150 ms; sem mover botões nem animar a leitura do texto.

Meta para texto corrente: contraste mínimo 4,5:1 contra a superfície composta mais desfavorável. Texturas, estados de foco, antialiasing e fundo precisam de avaliação prática posterior. Silhuetas dos slots vazios são sinais auxiliares e deliberadamente discretos; rótulos permanecem legíveis.

Tema Sombria recebe os materiais e acabamentos novos. Simples preserva superfícies limpas e destaque verde, compartilhando geometria, iconografia funcional e legibilidade.

## Pesquisa e aplicação

As aplicações artísticas são interpretações para o projeto, não recomendações declaradas pelos jogos.

- [Darkest Dungeon, mídia oficial](https://www.darkestdungeon.com/darkest-dungeon/media/): massas escuras, superfícies gastas e ilustração com peso. Adaptar a personalidade sem copiar desenho, símbolos ou seu grau de densidade.
- [Dark Souls Remastered, imagens oficiais](https://en.bandainamcoent.eu/dark-souls/news/new-screenshots-dark-souls-remastered): austeridade, ruínas e contraste entre pedra fria e luz quente como referência de atmosfera.
- [Blasphemous, The Game Kitchen](https://thegamekitchen.com/): arquitetura gótica e acabamento artesanal; usar detalhes contidos, sem importar sua iconografia religiosa.
- [Darkwood, press kit](https://www.darkwoodgame.com/presskit/): abandono, umidade, hostilidade ambiental. [Bullet Echo](https://www.zeptolab.com/games/bullet-echo): referência permanente de leitura tática compacta.
- [NN/g, princípios visuais](https://www.nngroup.com/articles/principles-visual-design/): escala, hierarquia, contraste e agrupamento. Traduzir em poucos tamanhos tipográficos e uma ação dominante.
- [NN/g, heurísticas](https://www.nngroup.com/articles/ten-usability-heuristics/): reconhecimento, visibilidade do estado e prevenção de erros. Abas e estados de conexão/operação ficam explícitos.
- [Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/full-list/): texto claro, controles espaçados, indicação de interatividade e informação além da cor.
- [W3C, contraste](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html): critério de leitura 4,5:1 para texto corrente, sem alegar certificação do jogo.

## Skills

Consultadas: game-ui-ux local para fluxo, foco e área segura; ui-ugui local para viabilidade e preservação da hierarquia; imagegen para o conceito raster. A referência pública de [ui-ugui da Unity](https://github.com/Unity-Technologies/skills/blob/main/skills/ui-ugui/SKILL.md) confirma o foco em Canvas, RectTransforms e mudanças incrementais.

[Impeccable](https://github.com/pbakaus/impeccable) foi pesquisada como complemento de crítica e acabamento visual. Sua implementação e verificações são voltadas a frontend: aproveitar critérios de hierarquia, espaçamento e consistência, sem importar ferramentas web para Unity. Não é necessária uma instalação para esta proposta; nenhuma skill foi instalada.

## Etapas de implementação futura

1. Fechar direção de títulos, paleta e detalhe de moldura; definir estados normal, foco, pressionado, indisponível e pendente.
2. Produzir sprites separados para painéis e botões com bordas escaláveis, divisores e ícones necessários; manter textos na Unity. O conceito inteiro nunca vira fundo clicável da UI.
3. Aplicar primeiro na Preparação via HudTheme/BaseMenuStyle/HudFrameGraphic e prefab existente, sem reconstruir a base.
4. Estender às outras quatro páginas, preservando slots, seleção, arraste e economia.
5. Revisar estaticamente referências, acentos, regras de tema e entrada. Avaliação prática inicial pelo usuário em celular horizontal, 16:9 e ultrawide; Unity, testes e builds somente conforme a preferência do projeto.

Sem alteração de save/protocolo, capacidades 12/16, estoque, rede, HUD de combate ou posições personalizadas. A nova imagem é um estudo de apresentação, não evidência de funcionamento implementado.
