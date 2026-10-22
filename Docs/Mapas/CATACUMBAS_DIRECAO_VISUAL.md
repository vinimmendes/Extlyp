# Catacumbas — direção visual e catálogo

Etapa **3.1**, de 14/10/2026: direção visual, escala de produção e inventário de arte para as sete subetapas seguintes. Este documento define a direção a aplicar; não registra uma aprovação visual adicional do usuário. Nenhum sprite final, material Unity ou modelo Blender foi produzido nesta subetapa.

Arquivos de trabalho:

- `CATACUMBAS_CATALOGO_SPRITES.json` e sua tabela em `CATACUMBAS_CATALOGO_SPRITES.md`: fonte de verdade dos assets, variantes, dimensões e reaproveitamento.
- `Art/Catacumbas/direcao-visual-v1.svg`: prancha esquemática de paleta, materiais e identidade dos setores. Não é captura do jogo ou conceito pintado do cenário.
- `CATACUMBAS_ETAPA_2.md`: funcionamento atual, mecanismos e regras que a arte deve respeitar.

## 1. Identidade

**Uma necrópole medieval construída sobre água, lentamente retomada por raízes.** Calcário absorve umidade, madeira funerária escurece, ferro perde o brilho e placas com nomes ficam ilegíveis. O abandono aparece por acúmulo, desgaste e interrupção dos rituais: urnas vazias, velas apagadas, bancos arrastados e correntes sem carga.

Darkwood orienta atmosfera, matéria orgânica, interiores gastos e detalhes narrativos. Bullet Echo orienta silhuetas, contraste de combate, coberturas e reconhecimento dos caminhos. Criar formas e composições próprias; nenhuma imagem desses jogos será copiada.

- Visão superior ortográfica, coerente com a câmera 2D e com os personagens existentes. Objetos estáticos não recebem perspectiva isométrica ou ponto de fuga.
- Pintura com volume e materiais convincentes, grandes massas legíveis e poucos detalhes pequenos. Sem pixel art ou contornos de desenho animado.
- Arquitetura medieval utilitária: pedra assentada, argamassa, madeira entalhada, ferro forjado, couro, cerâmica, corda e contrapesos. Sem painéis elétricos, cabos, máquinas industriais, placas modernas, neon ou tecnologia ambiental.
- A arquitetura não transforma as armas ou os personagens existentes em versões medievais. Os equipamentos e os três monstros permanecem os atuais.
- Predomínio de formas pesadas e verticais na arquitetura; raízes, água e tecido quebram essa regularidade. Ossos funcionam como sinais de uso funerário, sem substituir todos os pisos por ruído branco.

## 2. Paleta de materiais

Valores abaixo são cores-base dos assets sob iluminação neutra, não limites de brilho final na tela. A visão/fog existente continua responsável pela escuridão do jogo.

| Material | Base | Variação clara | Aplicação |
|---|---|---|---|
| Maciço e cavidades | `#171A1C` | `#292D2E` | Fundos inacessíveis, nichos fundos e juntas. Não pintar passagens abertas dessa cor. |
| Calcário seco | `#68645B` | `#969082` | Piso e arquitetura comuns; desgaste de arestas. |
| Pedra úmida | `#444D4C` | `#717B75` | Cisternas e paredes junto à água. |
| Madeira funerária | `#45372B` | `#75614A` | Bancos, tampas, embarcações e guinchos. |
| Ferro oxidado | `#393C3C` | `#815C43` | Grades, correntes e fixações, com oxidação localizada. |
| Osso envelhecido | `#A99E83` | `#CDC0A2` | Relíquias, inscrições e ossário; usar com parcimônia. |
| Raízes e musgo | `#454B36` | `#747655` | Infestação natural, sem verde luminoso. |
| Água profunda | `#263B40` | `#526B6A` | Cisternas e canal; bordas reconhecíveis. |
| Cera e luz quente | `#A88A55` | `#D0B37B` | Pequenos focos junto a marcos e mecanismos. |
| Tecido funerário | `#665B50` | `#8B7B66` | Estandartes e mortalhas; sem cores de equipes. |

O cenário compartilha a mesma arte nos temas A e B da interface. Estados, botões, ícones de HUD e seus significados continuam em `HudTheme`/`UiLayout`; a aparência medieval do mundo não autoriza ornamentar ou redesenhar a HUD aprovada.

## 3. Identidade dos oito setores

| Setor | Composição, material e marca de orientação | Limite de leitura |
|---|---|---|
| Vestíbulos dos Enlutados | Piso mais seco, molduras de urnas, pedra de soleira em forma de arco. Oito entradas usam marcas funerárias diferentes sobre a mesma família arquitetônica. | Centros das antecâmaras livres; marcas não parecem itens coletáveis ou saídas já prontas. |
| Galeria dos Nomes Raspados | Faixas contínuas de inscrições apagadas, nichos retangulares e pares de pilares; pedra bege acinzentada. | Inscrições ficam nas bordas; eixo de circulação com pouco detalhe. |
| Ossário das Raízes | Pilhas contidas de ossos em nichos, raízes atravessando juntas e teias nos encontros entre piso e parede. | Separar osso decorativo da silhueta da aranha pálida; evitar grandes tapetes claros no trajeto dela. |
| Claustro das Velas Mortas | Repetição de arcos, bancos laterais, cera escorrida e áreas de pedra exposta. Âmbar discreto junto a marcos. | Sala de respiro; não preencher o centro com velas, partículas ou móveis. |
| Cisternas do Pranto | Pedra escura, marcas horizontais de inundação, bordas de água definidas, passarelas de tábuas e âncoras do canal. | Margem, água bloqueada e cruzamento aberto precisam ser distinguíveis por forma e textura. |
| Prisão dos Penitentes | Grades, argolas de parede, portas baixas e correntes recolhidas; nichos escuros e marcas de arrasto. | Arestas de pedra separam o espreitador escuro do fundo. Não iluminar todo o piso com recortes de grades. |
| Capela Invertida | Altar partido, relevo funerário central, estandartes gastos e lajes deslocadas apontando para o colapso. | Marco maior do mapa, com poucos volumes fortes. O altar não ocupa novas áreas caminháveis nesta etapa de arte. |
| Câmara dos Contrapesos | Madeira escura espessa, polias grandes, roldanas, correntes em feixes e blocos de pedra suspensos visualmente. | Identificar o pedestal da manivela. Decoração suspensa fica fora da silhueta dos atores e não cria cobertura nova. |

Usar identidade de material e marcos antes de variar a cor de cada sala. Os setores pertencem à mesma construção; não criar oito biomas ou oito conjuntos arquitetônicos completos.

## 4. Escala e entrega dos sprites

- Unidade geométrica atual: célula de **1,06 u**, raio do jogador **0,40 u**, corredores de **4,24 u**. O sarcófago baixo existente ocupa **2,12 × 2,12 u**. Esses valores são referência de encaixe, não autorização para alterar a geometria.
- Base de produção dos novos módulos: **128 pixels de corpo por célula de 1,06 u**. Um piso de uma célula tem 128 × 128 px; um módulo de duas células tem 256 px nessa direção.
- Novos sprites usam `Pixels Per Unit = 128`; o encaixe futuro aplica escala **no filho visual** para reproduzir a dimensão em unidades do catálogo. Para um módulo de uma célula, a escala visual é 1,06. Preservar a escala de rede, os colliders e o transform raiz.
- Para medidas que não são múltiplas da célula, usar `arredondar(medida_u / 1,06 × 128)` pixels no corpo e ajustar o filho visual à medida exata. Exemplos: barca 3 × 1,5 u e plataforma 2,5 × 2,5 u.
- Sprites de objetos: PNG RGBA transparente, corpo inteiro, margem transparente mínima de 16 px por lado. O corpo representa a projeção do objeto sobre o chão; a margem não aumenta a pegada física.
- Pisos: PNG opaco e repetível, sem borda transparente; decalques de umidade e musgo são arquivos separados em RGBA. Não incluir objetos de cobertura no piso.
- Pivô de objetos estáticos: centro da pegada no chão. Partes móveis e todos os quadros de uma animação compartilham canvas e pivô. Pivôs de dobradiça/eixo são registrados no catálogo e não são inferidos a cada quadro.
- Orientação-base de objetos alongados: eixo longitudinal horizontal, proa da barca em **+X**. Módulos retos de parede têm versões horizontal/vertical por rotação de 90°. Rotação de decoração não deve inverter inscrições ou dobradiças.
- Iluminação de leitura suave e neutra, com contato curto sob os objetos. Não embutir sombras longas, halos, cenário, chão ou luz colorida nos sprites transparentes. Sem iluminação solar direcional em cada peça.
- Formato de importação planejado: sRGB, filtragem bilinear, sem mipmaps, wrap Clamp nos objetos e Repeat apenas nos pisos repetíveis. Preservar alpha e dimensões de origem; não definir max-size que reduza uma folha silenciosamente. Nenhuma configuração dos assets atuais foi alterada aqui.
- Folhas de animação novas: no máximo oito colunas, grade de células idênticas, sem quadros cortados. Estados parado/aberto/fechado usam os extremos da sequência; o fechamento pode reutilizar a sequência invertida quando indicado no catálogo.
- Organizar os futuros arquivos em `Assets/Resources/Environment/Dungeon/{Floors,Architecture,Props,Sectors,Exits,Details}`. Ferramentas finais vão para `Assets/Resources/Items/Dungeon`. Esses diretórios serão criados quando houver assets; não são dependências existentes.
- Nomes seguem o ID do catálogo e uma variante explícita, por exemplo `CAT-F01_v01.png`. Arquivos de origem e prompts ficam em `Art/Catacumbas`, separados das versões de runtime.

O alvo inicial não introduz Shader Graph, novos materiais especiais, mapas normais ou um sistema de luz adicional. Produzir albedo + alpha compatíveis com o pipeline atual. Decidir compressão/atlas final a partir da inspeção dos assets produzidos e da avaliação prática, sem prometer desempenho nesta fase.

## 5. Reaproveitamento verificado no repositório

| Recurso | Decisão |
|---|---|
| `Resources/Monsters/Weaver/Walk.png` e `Web.png` | Reutilizar. Manter silhueta pálida, pivôs, dimensões, animações e colisor. |
| `Resources/Monsters/Stalker/Walk.png` e `ClawAttack.png` | Reutilizar a versão integrada. As pranchas conceituais antigas não substituem os atlases de runtime. |
| `Resources/Monsters/Shell/Walk.png` e `ArcAttack.png` | Reutilizar; preservar frente protegida, placas e leitura dos flancos. |
| `Resources/Environment/Chest/Open.png` e `Close.png` | Reutilizar o baú integrado, 20 quadros por folha, 256 px por quadro, pivô e dimensões atuais. Não redesenhar os seis baús da masmorra nesta subetapa. |
| `Environment/CoverCrate-v1.png` | Candidato para caixote medieval de madeira e ferro. Não substituir os sarcófagos por caixotes nem mudar cobertura existente para acomodar o sprite. |
| `Environment/AbandonedProps.png` | Mesa e cadeira são candidatos após conferência do recorte, proporção e alpha. Não importar o atlas inteiro como piso ou decoração de sala. Árvores, cama e armário metálico não entram no lote inicial das Catacumbas. |
| `Environment/Sawmill/Moss.png` e `DampPatch.png` | Candidatos a decalques, somente se escala, alpha e leitura combinarem com pedra; permanecem separados dos assets da serraria. |
| `PassageToolArt` e `ItemIconLibrary` | Placeholders atuais da manivela/chave continuam até a substituição dos dois ícones finais. Sem alterar suas regras de inventário. |

Os caminhos acima foram encontrados em disco; a inspeção de código confirma os carregadores e os usos. A prancha do baú e o atlas de móveis foram inspecionados visualmente. Reuso de candidatos não significa integração ou validação em execução. Documentos históricos que dizem “integração pendente” não prevalecem sobre os caminhos e carregadores atuais.

## 6. Leitura de combate e estados

- Prioridade visual: ator e perigo ativo → objeto interativo → cobertura → arquitetura → detalhe de piso. Não pintar teias ativas, projéteis, manchas de ácido ou indicadores de ataque em texturas estáticas.
- Aranha: reservar pedra média/escura junto aos encontros; teia decorativa fina e imóvel apenas em bordas. Não desenhar silhuetas de aranhas em relevos ou amontoados de ossos.
- Espreitador: usar uma faixa reconhecível de pedra ao redor de pilares/portas escuros. Não clarear o próprio monstro, adicionar olhos luminosos ou remover o efeito da visão existente.
- Couraçado: separar o basalto do corpo de pilares e sarcófagos pelo contorno e pelo piso. Decoração não cobre as bordas das placas nem os acessos aos flancos.
- Objetos que bloqueiam passagem mantêm base e contorno claros. Entulho pintado no chão não simula uma parede sólida. Decoração nova não cria colisores ou novos bloqueios de visão.
- Extração pendente: obstrução física representada por grade/corrente/encaixe vazio. Extração pronta: passagem visualmente livre ou ferramenta instalada. Nunca depender apenas de verde/vermelho.
- Escadaria aponta para uma abertura superior; poço mostra cordas, polia e contrapeso; barca aponta para a boca do canal. Desenhar a continuidade da fuga dentro da área visual disponível, sem expandir a área caminhável.
- Animações cosméticas obedecem aos estados replicados. Não mudar os 6/4 segundos de preparação, os 3 segundos de extração ou a disponibilidade compartilhada para caber na arte.
- Manivela e chave têm formas distintas e continuam identificadas pelo nome. Reflexos e brilhos de vela não imitam itens coletáveis.

## 7. Ordem de produção e critérios de entrega

O catálogo divide os assets pelas subetapas já combinadas: **3.2 pisos/água; 3.3 arquitetura; 3.4 sepulturas/coberturas; 3.5 identidade dos setores; 3.6 extrações; 3.7 detalhes/atmosfera; 3.8 revisão**. Os IDs permanecem estáveis mesmo se a arte ganhar versões novas. “Variante” significa desenho diferente; rotação e instância na cena não contam como novos desenhos. Os números de quadros são entregas de arte, não duração autoritativa.

Para cada lote: entregar fontes, PNGs, medidas/pivôs, nomes de variantes e correspondência com o ID; registrar o que foi produzido, reutilizado ou ficou pendente. Integrar somente os assets do lote solicitado e manter o restante em placeholder. Não tratar uma imagem conceitual ou esta prancha SVG como atlas final.

Na revisão estática, conferir transparência, consistência de vista, encaixe da pegada, extremos de animação, repetição do piso e ausência de elementos modernos. A avaliação prática do usuário vem primeiro para leitura sob fog, combate, toque, escalas de tela e desempenho. **Não foram iniciados Unity, testes automatizados, builds ou renders de validação.**
# Atualização do canal — 19/10/2026

O Canal dos Defuntos passa a um rio conectado às bordas leste e sul, com o embarcadouro no sudeste. A comporta abre uma ponte específica como atalho; a alternativa é uma galeria mais longa a oeste. Água permanente não é liberada pela chave. Consulte [geometria, apresentação e revisão estática](Canal/IMPLEMENTACAO.md) e a [planta atualizada](Canal/planta.svg). As plantas e medidas anteriores deste documento são históricas. Avaliação prática na Unity pendente.

## Ampliação da decoração narrativa — levantamento de 19/10/2026

A [etapa 1 de distribuição](DecoracaoNarrativa/ETAPA_1.md) registra as 24 salas, 28 ligações autoradas e três áreas do canal, com [planta](DecoracaoNarrativa/planta.svg) e [catálogo de zonas, peças e reservas](DecoracaoNarrativa/distribuicao.json). É planejamento, sem novas instâncias ou sprites integrados. As zonas de busca não são posições aprovadas; não substituem as reservas atuais nem a conferência dos bounds existentes.

Preservar a leve inclinação visual aprovada pelo usuário nas peças atuais, compatível com a câmera superior, sem mudar a projeção do mapa. O piloto seguinte cobre Galeria das urnas, Câmara dos contrapesos e Embarcadouro funerário antes de distribuir as novas composições pelo restante do mapa.

Atualização: [etapa 2 integrada](DecoracaoNarrativa/ETAPA_2.md), com cinco famílias novas e 11 peças autoradas nos três pilotos. Avaliação visual em jogo pendente; as demais salas ainda não receberam esses conjuntos.

[Etapa 3 concluída](DecoracaoNarrativa/ETAPA_3.md): seis famílias complementares produzidas, completando N01–N11. Catálogo unificado e galeria preparados; distribuição adicional pelo mapa ainda não executada.

[Etapa 4 integrada](DecoracaoNarrativa/ETAPA_4.md): distribuição autorada nas 24 salas, corredores e galeria do canal, com 77 grupos e alternativas de encaixe seguro. Contagem estática preferencial; avaliação de densidade e leitura em jogo pendente.

[Apresentação das faces de parede](CATACUMBAS_VISAO_FACES_PAREDES.md): a alvenaria exposta pode conservar suas cores e decorações até a borda posterior da faixa visual, mantendo o primeiro bloqueio para piso e alvos atrás dela. Implementação revisada estaticamente; avaliação visual em jogo pendente.

