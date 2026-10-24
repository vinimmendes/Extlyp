# Catacumbas — piloto do Ossário inferior

Revisão 3, de 23/10/2026. **Ideia: a ordem que começou a ceder.** Conceito pintado, montagem com módulos efetivos e integração restrita à R17. Avaliação prática pendente; a expansão continua aguardando retorno.

**Ideia ampliada:** a [revisão 4](IDEIA_V4_ACAMPAMENTO.md) acrescenta um acampamento abandonado relacionado ao recipiente fechado do sudoeste. Essa revisão é um conceito; o pacote ativo descrito abaixo continua sendo a revisão 3 com as urnas quebráveis agrupadas.

## Arte produzida

- Seis novas peças pintadas para esta ideia: nicho com três urnas assentadas, versão danificada, restos recolhidos, trilha de fragmentos, raízes finas e recipiente funerário de pedra aberto. O recipiente fechado anterior é reaproveitado. As 12 peças antigas continuam disponíveis, mas os objetos do acampamento, a poça e o esqueleto inteiro não integram esta composição.
- 188 módulos de parede: 47 vizinhanças normalizadas em quatro acabamentos (seco, úmido, rachado e raízes). A mesma geometria e a mesma faixa de borda são compartilhadas pelos estados. Rachaduras são superficiais; não abrem passagens.
- Piso de pedra contínuo, com desgaste mais discreto na circulação e poeira neutra junto às bases das paredes, dividido em 288 células editáveis. As manchas ovais de terra e raízes foram retiradas. O manifesto não coloca nenhuma célula de água rasa; os arquivos anteriores de água permanecem preservados.
- 15 instâncias decorativas autoradas e dois suportes sobre as coberturas baixas existentes. As urnas quebráveis da expedição continuam sendo os objetos antigos com suas regras e aparências existentes.

Estudo e peças foram pintados pela ferramenta integrada **image_gen**, com alpha solicitado nas folhas de objetos. Fontes e prompts completos em [Art/Catacumbas/ReworkPiloto](../../../Art/Catacumbas/ReworkPiloto), incluindo `prompts-ossuario-v3.json`. O empacotador separa peças, amostra materiais, compõe desgaste e relevo modular, extruda bordas de atlas e monta pranchas; não substitui a pintura por primitivas.

- [Prancha de acabamento](../../../Art/Catacumbas/ReworkPiloto/Prancha-acabamento.png).
- [Composição em resolução integral](../../../Art/Catacumbas/ReworkPiloto/OssarioInferior-composicao.png).
- [Amostras das 47 vizinhanças nos quatro estados](../../../Art/Catacumbas/ReworkPiloto/Paredes-47x4.png).
- [Catálogo e posições](catalogo.json), [receitas de composição](composicoes.json), [fichas das salas](SALAS.md).
- [Ideia e limites](IDEIA_V3.md), [conceito pintado](../../../Art/Catacumbas/ReworkPiloto/Conceito-pintado-v3.png), [conceito com módulos efetivos](../../../Art/Catacumbas/ReworkPiloto/Conceito-v3/Prancha-acabamento.png) e [comparação da composição](../../../Art/Catacumbas/ReworkPiloto/Comparacao-composicao-v3.png).
- [Exemplo do canto com urnas quebráveis](../../../Art/Catacumbas/ReworkPiloto/Urnas-quebraveis-canto.png) e [sala com duas urnas ilustrativas](../../../Art/Catacumbas/ReworkPiloto/OssarioInferior-com-quebraveis-exemplo.png). Essas duas imagens incluem uma combinação ilustrativa de posições; não reproduzem o sorteio de uma partida específica.

## Grupo de urnas quebráveis no canto sudeste

Por solicitação do usuário, os três candidatos existentes da sala (IDs 35, 36 e 37) foram aproximados nas células (27,37), (28,37) e (28,38), junto à parede e abaixo dos nichos. O grupo sugere recipientes que permaneceram guardados aguardando organização. Não foram adicionados simulacros decorativos de objetos quebráveis.

A regra existente continua colocando **duas urnas entre essas três posições por partida**. IDs, variantes, recompensa, estados de quebra e autoridade do servidor permanecem iguais. Só os candidatos da R17 mudaram; nenhuma posição das outras salas ou corredores foi alterada. A proximidade é uma exceção autorada à distribuição espaçada do gerador, preservada em `Tools/Author-DungeonBreakables.py`.

O catálogo real é `Assets/Resources/Environment/Dungeon/Breakables/BreakablesCatalog.json`. O empacotador do piloto apenas desenha duas dessas posições com os sprites reais para documentação; o jogo continua instanciando o prefab de rede existente. A alteração foi revisada nos dados e nas imagens, sem testes Unity ou builds.

## Da ideia à entrega

A ideia foi definida antes da arte nova: um ossário organizado cuja extremidade começou a ceder. O estudo pintado usou a vista superior e as duas bases existentes. A montagem executável foi preparada em `Conceito-v3` antes de substituir o pacote ativo, usando os mesmos recortes, escala, posições e ordem de desenho empregados pela integração.

Diferenças explícitas do estudo pintado: nichos mais rasos, inteiramente sobre as células sólidas; raízes e detritos menos extensos para preservar o candidato de quebrável no noroeste; nenhum novo recesso físico na parede. A imagem pintada orienta a cena; a prancha dos módulos efetivos é a referência da entrega. As urnas fazem parte da arte dos módulos de nicho, enquanto os módulos, detritos, raízes e recipientes permanecem selecionáveis separadamente.

A revisão preserva as paredes aprovadas na etapa anterior. Prancha, receita e empacotador da composição anterior estão em `Historico/v2`; a versão inicial permanece em `Historico/v1`.

## Revisão de legibilidade das paredes — 23/10/2026

Relato do usuário: difícil distinguir parede de chão. A inspeção da prancha e do empacotador mostrou alvenaria com valores próximos aos das lajes e uma borda de baixo relevo, fazendo a parede parecer outra faixa de piso.

Correção aplicada ao atlas do piloto: nova pintura de maciço frio e escuro, borda elevada mais larga, face rebaixada e sombra de contato. O relevo fica inteiramente dentro das mesmas células sólidas, preservando as 47 máscaras e os acessos. Piso, objetos e suas posições não foram redesenhados. As bases de cobertura baixa ganharam uma referência de arte própria para conservar sua aparência anterior e não herdar a apresentação de parede alta.

[Comparação antes/depois](../../../Art/Catacumbas/ReworkPiloto/Comparacao-paredes-v2.png). Fonte e prompt adicionais: `Fontes/MacicoParede-v2.png` e `prompt-paredes-v2.json`. Prancha, composição e atlas anteriores preservados em `Historico/v1`.

Arquivos de integração desta correção: empacotador, `CRP_WallAtlas.png`, novo `CRP_LowCoverStone.png`, manifesto e referência do suporte em `DungeonOssuaryRework.cs`. Revisão visual da prancha atualizada realizada; ainda sem execução Unity. O contraste sob fog e na tela do aparelho depende de nova avaliação prática.

**As pranchas são montagens estáticas dos sprites e da geometria autorada. Não são capturas nem reproduções completas da renderização Unity.** Não incluem fog, atores, loot, objetos quebráveis ou a arte antiga fora do piloto.

## Cena R17

Uso original: guarda de cinzas e restos já exumados. O abandono aparece como uma interrupção localizada da organização, sem um segundo tema de acampamento ou inundação.

- Norte e leste: nove módulos de nicho repetem uma organização funerária, interrompida pelas aberturas reais. As urnas estão assentadas nas cavidades e deixam de flutuar sobre as molduras.
- Noroeste: um módulo danificado termina essa sequência. Ossos, fragmentos e raízes finas se relacionam com esse dano; não formam ilhas independentes.
- Coberturas existentes: recipiente fechado no sudoeste e recipiente de pedra com restos recolhidos no nordeste. Ambos preservam as bases e pegadas de colisão.
- Piso: pedra contínua, desgaste no eixo de circulação e poeira de borda. Nenhuma água decorativa, fogueira, panela, pá ou equipamento de aventureiro.
- Reservas: eixo norte–sul, acesso oeste, loot, encontros e candidatos de objetos quebráveis permanecem livres de novas peças. Os volumes novos ocupam apenas pedra ou cobertura já bloqueadas; no piso livre há apenas vestígios baixos e planos.

As receitas registram pivô e deslocamentos locais em células. Permitem remontar cada grupo como ponto de partida de autoria; não constituem um novo pincel de grupos no editor. Cada peça integrada continua selecionável individualmente.

## Integração e arquivos

- `Assets/Scripts/Match/DungeonOssuaryRework.cs`: carregamento e recortes, pisos e paredes sem collider, suportes de coberturas e colocação decorativa com reservas. Exibe os sprites por `IMapSpritePalette` no editor visual existente.
- `Assets/Scripts/Match/DungeonMapGeometry.cs`: uma chamada após a decoração anterior, antes da aplicação dos ajustes salvos do editor.
- `Assets/Scripts/Match/MapBuilder.cs`: apenas a opção serializada `catacombOssuaryReworkPilot`, inicialmente ligada. As outras modificações locais nesse arquivo foram preservadas.
- `Assets/Resources/Environment/Dungeon/ReworkPilot/`: atlases ativos de piso e paredes, seis PNGs novos `CRP_UrnRecess`, `CRP_DamagedRecess`, `CRP_GatheredBones`, `CRP_StoneDust`, `CRP_FineRoots`, `CRP_BoneCasket`, manifesto e metadados. Os demais assets são preservados.
- `Tools/Author-CatacombReworkPilot.py`: empacotamento reproduzível das fontes e das pranchas. `--concept` escreve somente a montagem de estudo em `Art/.../Conceito-v3`; sem essa opção, atualiza o pacote ativo. Não inicia Unity ou testes.

Na revisão 3, a única extensão do leitor C# é `Piece.stretch`, opcional e falsa por padrão. É usada para acomodar o decalque de raízes à faixa fina da base da parede; os demais objetos mantêm proporção uniforme. As reservas continuam usando o envelope completo da peça.

O piloto usa célula de 1,06 u e sprites de 128 pixels por célula. Os atlases usam extrusão de dois pixels, Clamp, bilinear, sem mipmaps, sem redução de NPOT e limite de importação 4096. Não há alteração de materiais globais, shader de visão ou água funcional.

A ocultação anterior é local e acontece somente depois de carregar e montar os recursos. Mantém os objetos, caminhos e tiles antigos; desliga renderers decorativos contidos na área substituída e zera a cor dos tiles anteriores cobertos. Ajustes persistidos não são apagados. Falha de recurso mantém o cenário anterior; uma peça sem encaixe é omitida com aviso identificável. Nenhum dado autoritativo ou collider é modificado.

## Revisão realizada

- Leitura de Unity 6000.0.84f1, URP 17.0.4, NGO 2.13.3 e Transport 2.7.4; sem atualização.
- Revisão estática dos recortes, dimensões, atlas com bordas extrudadas, correspondência das 47 máscaras e posições autoradas.
- Inspeção visual do conceito pintado, da folha de peças novas e da montagem efetiva, preservando a leitura de parede anteriormente aprovada.
- Comparação estática dos envelopes decorativos com loot/encontros e reservas dos quebráveis: nenhum envelope cruza essas reservas na fonte atual. Isso não executa as regras Unity de colocação nem prova que todas as peças aparecerão.
- Revisão das alterações de código: sem novos RPCs, NetworkObjects, colisores, chamadas por quadro ou alteração de layout. Não se afirma ganho de desempenho; comportamento e custo em aparelho permanecem sem medição.

**Não foram iniciados Unity, Play, testes automatizados, compilação de scripts ou builds.** A importação, a execução de `Fits`, o comportamento do editor e a aparência sob fog ainda dependem do primeiro teste do usuário.

## Primeira avaliação prática pelo usuário

1. Abrir as Catacumbas no fluxo normal ou no editor visual já existente. Localizar `DungeonOssuaryRework_R17` na hierarquia e enquadrar com F. Centro da sala: **(-46,64; -21,20)** unidades.
2. Conferir escala dos nichos e recipientes ao lado do personagem, ligação dos vestígios ao dano, leitura das coberturas e ausência dos objetos antigos substituídos.
3. Percorrer os acessos norte, sul e oeste; observar quinas, transições e encontro com os pisos antigos. Conferir também sob o cone de visão/fog habitual.
4. Conferir se as peças da receita aparecem e se há avisos de omissão. A montagem estática não mostra os quebráveis, loot e monstros que coexistem no jogo.
5. Selecionar um tile e um objeto no editor, conferir as entradas `CRP_` na paleta e salvar um ajuste se desejar. A persistência dessa edição ainda não foi avaliada em execução nesta entrega.

Para rever a apresentação pré-piloto, desativar **Catacomb Ossuary Rework Pilot** no MapBuilder antes de uma nova geração do mapa. Isso não exclui os assets novos ou antigos; as revisões do piloto ficam registradas no histórico de arte.

## Limite da etapa

Pendente após retorno: ajustes do piloto; tilesets completos e reutilizáveis das seis famílias de chão, incluindo ornamentação, destruição e todos os tipos de transição; acabamento dedicado dos portais nas quatro orientações; pilares e relevos complementares; organização ampliada da paleta; aplicação e composição das outras 23 salas e corredores. As fichas dessas salas são intenção de autoria, não arte nova já aplicada.

Os 47 módulos por estado já existem como base técnica e amostras para o piloto; sua presença não significa aprovação visual do conjunto nem conclusão do rework inteiro.
