# Decoração narrativa — etapa 2: três pilotos

Implementação de 19/10/2026. Três composições ligadas à construção das Catacumbas; avaliação visual em jogo pendente. O restante do mapa permanece para as etapas seguintes.

## Onde encontrar

| Piloto | Local | Composição |
|---|---|---|
| R08 | Galeria das urnas, noroeste; centro em células (-24, 42) | Mortalha sobre sarcófago baixo existente, tampa deslocada na alvenaria a oeste e cerâmica no piso próximo. |
| R19 | Câmara dos contrapesos, noroeste do centro; centro (-28, 24) | Caixão inacabado sobre alvenaria ao norte, ferramentas no piso e corrente recolhida. A mesa funerária existente permanece. |
| R13 | Embarcadouro funerário, sudeste; centro (44, -48) | Madeira encalhada sobre sedimentos na margem leste do cais; outra ilha de sedimentos, umidade e cerâmica na parte oeste/sudoeste. |

As coordenadas abaixo são relativas ao centro do mapa, em células de 1,06 u, e representam o centro de cada envelope visual. Não são coordenadas de novos colliders.

| Peça | Centro (células) | Envelope antes da rotação | Rotação | Suporte |
|---|---|---|---|---|
| R08 mortalha | (-29, 39) | 1,7 × 0,8 | 0° | Cobertura baixa existente |
| R08 tampa | (-35, 46) | 1,6 × 1,6 | 0° | Alvenaria inacessível |
| R08 cerâmica | (-31,4, 45,5) | 0,9 × 0,8 | 0° | Piso livre |
| R19 caixão | (-36,5, 32,6) | 3 × 1 | 0° | Alvenaria inacessível |
| R19 ferramentas | (-34, 30,8) | 2 × 0,85 | 0° | Piso livre |
| R19 corrente | (-31, 31) | 0,7 × 0,7 | 0° | Piso livre |
| R13 sedimentos de margem | (52,8, -43,8) | 2,7 × 1 | 90° | Piso seco junto à margem |
| R13 madeira | (52,8, -43,8) | 2,2 × 0,6 | 90° | Piso seco; sobreposição intencional com sedimentos |
| R13 sedimentos oeste | (36, -54,4) | 3 × 1 | 0° | Piso seco |
| R13 umidade | (39, -54,5) | 1,4 × 0,8 | 0° | Piso seco |
| R13 cerâmica | (38, -51,5) | 0,8 × 0,8 | 0° | Piso seco |

## Arte produzida

Cinco famílias geradas com `image_gen` integrado, em RGBA transparente: mortalha N01, caixão N02, ferramentas N03, madeira N08 e sedimentos N09. A versão N09_v01 foi rejeitada por parecer uma estrutura de madeira; somente N09_v02 entra no runtime. A tentativa rejeitada permanece em Art para rastreabilidade.

- Fontes, prompts e registro da revisão: `Art/Catacumbas/Narrativa/`.
- Texturas selecionadas e manifesto: `Assets/Resources/Environment/Dungeon/Narrative/`.
- Reaproveitamentos por referência: CAT-P02_v01, CAT-P10_v01, CAT-D10_v01 e CAT-D08_v01; sem duplicar suas texturas. O sarcófago e a mesa existentes continuam sendo os suportes da composição.
- Originais preservados em resolução de geração. Importação das cinco peças com limite 2048, Bilinear, Clamp, sRGB, sem mipmaps/compressão e alpha preservado. Fontes de 2172 px podem ser reduzidas ao limite na importação; não é uma folha de animação. Recortes normalizados acompanham a escala importada.
- Enquadramento pelo limite do alpha, com escala uniforme para caber no envelope; sem esticar proporções. Leve inclinação e materiais seguem as peças existentes; não foi alterada a câmera.

## Integração e reservas

`DungeonNarrativePilot` carrega o manifesto local uma vez, após Props, SectorArt e ExtractionsArt e antes de Details. Usa os bounds dos renderizadores existentes para rejeitar sobreposições; Details encontra os novos renderizadores CAT e os inclui em sua ocupação. O grupo de madeira/sedimentos do cais é a única sobreposição nova intencional. Os sprites pertencem ao componente e são liberados na destruição do mapa. Servidor sem dispositivo gráfico não instancia os pilotos.

O encaixe verifica suporte por célula (piso, alvenaria ou cobertura baixa), aproximações de portas, água, spawns, extrações, mecanismos, pedestais, encontros, loot e todos os candidatos de quebráveis. Volumes altos ficam em alvenaria existente; a mortalha fica sobre cobertura já existente; ferramentas e fragmentos no piso são rasos.

A reserva ampla `CanalReserved` permanece nos distribuidores automáticos. Apenas as peças explicitamente vinculadas à sala 13 podem ser colocadas dentro dela, exigindo piso seco e envelope inteiramente contido na sala. A reserva da barca usa a varredura para sul, incluindo margem visual, além do raio de extração. A nova arte não cobre água ou a ponte.

Se uma peça não couber por mudança futura na geometria ou arte existente, ela será omitida com seu ID no aviso. Se faltar uma textura ou o manifesto for inválido, o grupo piloto é desativado e o cenário anterior permanece. Nenhum objeto existente é removido para forçar o encaixe.

Não foram acrescentados colliders, luzes, componentes de rede, cobertura, interações, drops ou progressão. Permanecem 48 quebráveis ativos, 64 candidatos e o balanceamento atual de loot. Não há alteração de saves ou protocolo.

## Revisão realizada e limite

- Inspeção visual dos cinco PNGs selecionados; alpha real e regiões não vazias conferidos.
- Levantamento das 11 âncoras por leitura da geometria atual: sem conflitos com suporte, água, aproximações de portas, 59 pontos protegidos ou 64 candidatos. Resultado em [pilotos-levantamento.json](pilotos-levantamento.json).
- Compilação C# isolada com referências atuais da Unity 6000.0.84f1, sem iniciar o Editor; nenhum erro de compilação.
- Revisão de caminhos Resources, GUIDs próprios, ordem de construção, destruição e preservação dos sistemas existentes.

Não foram iniciados Unity, testes automatizados, builds ou capturas de jogo. O levantamento não comprova a importação nativa, todos os bounds efetivos ou leitura sob fog. A avaliação prática inicial continua com o usuário em uma nova expedição.

## Avaliação dos pilotos

Conferir reconhecimento na câmera de jogo, densidade e contraste; verificar se o caixão e a tampa parecem apoiados, a mortalha não parece um item coletável e os detritos não parecem uma ponte. Conferir aproximação ao pedestal, coleta, combate e percurso da barca. Só expandir os conjuntos depois dos ajustes dessa avaliação.

O restante das famílias N04–N07 e N10–N11 não foi produzido nesta etapa. A distribuição em todas as salas e corredores também não foi executada.

Atualização posterior: as seis famílias complementares foram produzidas na [etapa 3](ETAPA_3.md), sem ampliar a distribuição dos pilotos.
