# Decoração narrativa — etapa 3: conjuntos complementares

Seis famílias complementares produzidas, completando as 11 famílias N01–N11 do levantamento. Esta etapa entrega arte e catálogo, sem ampliar a distribuição além dos três pilotos da etapa 2.

## Peças produzidas

| ID | Peça | Envelope máximo (células) | Uso previsto |
|---|---|---|---|
| N04 | Padiola funerária | 3 × 1 | Área de trabalho dos coveiros, somente em recesso/alvenaria inacessível. |
| N05 | Incensário caído | 1 × 1 | Claustro e capela; cinzas frias, sem fogo, fumaça ou luz. |
| N06 | Tigela e utensílios de cela | 1 × 1 | Bordas da prisão; cerâmica e colher sem conteúdo ou interação. |
| N07 | Palha deteriorada | 2 × 1 | Decalque plano nas celas, em contraste e opacidade baixos. |
| N10 | Mochila rasgada decorativa | 1 × 1 | Expedição perdida; vazia, danificada e sem apresentação de pickup. |
| N11 | Acampamento apagado | 2 × 2 | Cinzas frias, tecido gasto e utensílio; sem fogueira funcional ou zona segura. |

Os envelopes são limites de enquadramento visual, não pegadas físicas. Ajustar escala uniformemente pelo limite do alpha; não esticar arte nem criar colisão para acomodá-la. Uma célula continua sendo 1,06 u.

## Catálogo e arquivos

- [Galeria das 11 famílias](../../../Art/Catacumbas/Narrativa/galeria.html): fundos claro, escuro e quadriculado; prévias de 64, 96 e 128 pixels. É uma galeria das fontes, não uma captura do jogo.
- `Assets/Resources/Environment/Dungeon/Narrative/NarrativeCatalog.json`: catálogo unificado com caminho Resources, recorte normalizado, envelope, suporte e opacidade sugerida de cada família. Preparado para a distribuição seguinte; nenhum carregador novo foi ativado nesta etapa.
- `Art/Catacumbas/Narrativa/prompts-etapa3-v1.json` e `selecao-etapa3-v1.json`: prompts completos e correspondência com as imagens de origem geradas pela ferramenta integrada `image_gen`.
- `Tools/Prepare-DungeonNarrativeCatalog.py`: preparo do catálogo, metadados, galeria e relatório estático sem editar os pixels dos PNGs.
- [Levantamento de arte](arte-etapa3-levantamento.json): dimensões, alpha, regiões úteis e referências das 11 famílias selecionadas.

Originais selecionados preservados em Art e cópias de runtime em Resources. Não sobrescrever os pilotos ou a tentativa N09_v01 rejeitada na etapa anterior. O catálogo aponta N09_v02.

## Direção e importação

Materiais pintados, madeira escura, ferro/bronze envelhecido, tecido bege acinzentado, terracota fosca e palha pouco saturada. Visão superior com leve inclinação, sem mudar a câmera ou criar uma cena isométrica. Sem texto, moldura, brilho de loot, personagem ou fundo retangular.

PNG RGBA com transparência real; recorte útil registrado no manifesto, sem reamostrar ou pintar os arquivos de origem. Manter o padrão do piloto: Bilinear, Clamp, sRGB, sem mipmaps, sem compressão e limite de importação 2048. Fontes acima desse limite podem reduzir na importação, mas permanecem preservadas em Art. O catálogo usa recortes normalizados compatíveis com a escala importada.

Palha e acampamento terão opacidade sugerida de 0,4 e 0,65, respectivamente, para não disputar leitura com atores e itens. Os valores são ponto inicial para a próxima integração, ainda sem avaliação sob a visão/fog do jogo. A aparência do cenário é compartilhada pelos dois temas da HUD.

## Limites e próxima etapa

Nenhuma instância adicional foi colocada no mapa nesta etapa. Os três pilotos, a geografia, os colliders, o percurso do barco e as reservas permanecem como estavam. Não há novos recipientes, drops, cobertura, luz, economia, protocolo ou dados de jogador.

Atualização posterior: a [etapa 4](ETAPA_4.md) acrescentou a distribuição autorada das famílias, mantendo os limites acima.

A etapa 4 distribuirá as composições nas salas e corredores previstos, usando os envelopes e as reservas do levantamento. Não transformar a mochila decorativa em item recuperável, o acampamento em ponto seguro ou a palha em obstáculo. Volumes como padiola e caixão ficam sobre áreas já inacessíveis.

## Revisão

Inspeção visual das seis peças produzidas e conferência estática de alpha, dimensões, caminhos, recortes e GUIDs do catálogo completo. Não foram iniciados Unity, testes automatizados ou builds. A leitura em câmera, a importação nativa e o desempenho continuam pendentes de avaliação prática.
