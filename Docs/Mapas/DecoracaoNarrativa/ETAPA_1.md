# Decoração narrativa — etapa 1: distribuição

Levantamento de 19/10/2026. **Planejamento concluído; nenhuma composição nova integrada.**

Arquivos: [planta](planta.svg), [catálogo com coordenadas e reservas](distribuicao.json). Regeneração: `Tools/Plan-DungeonDressing.py`.

## Leitura e limites

A planta usa a geometria atual do canal. R00–R23 são os índices reais das salas. C00–C27 são ligações autoradas; CA01–CA03 representam a galeria, ponte e rio acrescentados pelo canal. Ligações podem compartilhar piso: não contar cada linha como corredor físico independente.

Faixas cobre delimitam áreas iniciais de busca junto às paredes, e podem intersectar reservas: não autorizam instanciar a composição inteira ali. Coordenadas finais, orientação e bounds de cada peça serão definidos nos pilotos. Envelopes máximos propostos: 2×1 células nas entradas, 4×3 nas salas; reduzir ou omitir se não couber.

A geometria é um levantamento estático em células. Não inclui bounds de todos os sprites, nem comprova leitura sob fog ou física. Não reduzir reservas para cumprir metas.

## Distribuição por sala

| ID | Sala | Composição principal | Secundária opcional | Área de busca | Piloto |
|---|---|---|---|---|---|
| R00 | Entrada do sino | Memorial apagado | — | lateral oeste | — |
| R01 | Entrada das urnas | Sepultura violada | — | lateral leste | — |
| R02 | Entrada dos nomes | Memorial apagado | — | lateral oeste | — |
| R03 | Entrada das grades | Cela esquecida | — | lateral leste | — |
| R04 | Entrada das raízes | Ossário retomado por raízes | — | lateral oeste | — |
| R05 | Entrada das velas | Ritual interrompido | — | lateral leste | — |
| R06 | Entrada do cortejo | Memorial apagado | — | lateral oeste | — |
| R07 | Entrada do canal | Margem após a enchente | — | lateral leste | — |
| R08 | Galeria das urnas | Sepultura violada | Colapso antigo | lateral oeste | Sim |
| R09 | Nomes raspados | Memorial apagado | — | lateral leste | — |
| R10 | Celas dos penitentes | Cela esquecida | Memorial apagado | lateral oeste | — |
| R11 | Sala das correntes | Cela esquecida | — | lateral leste | — |
| R12 | Salão das águas | Margem após a enchente | Colapso antigo | lateral oeste | — |
| R13 | Embarcadouro funerário | Margem após a enchente | — | lateral oeste | Sim |
| R14 | Escadaria do cortejo | Memorial apagado | — | lateral oeste | — |
| R15 | Galeria dos relevos | Memorial apagado | — | lateral leste | — |
| R16 | Claustro das velas | Ritual interrompido | Memorial apagado | lateral oeste | — |
| R17 | Ossário inferior | Ossário retomado por raízes | Sepultura violada | lateral leste | — |
| R18 | Ossário das raízes | Ossário retomado por raízes | Sepultura violada | lateral oeste | — |
| R19 | Câmara dos contrapesos | Trabalho dos coveiros | Colapso antigo | lateral leste | Sim |
| R20 | Pátio das vigílias | Ritual interrompido | Expedição perdida | lateral oeste | — |
| R21 | Capela invertida | Ritual interrompido | Memorial apagado | lateral leste | — |
| R22 | Sacristia partida | Sepultura violada | Expedição perdida | lateral oeste | — |
| R23 | Cisterna dos lamentos | Margem após a enchente | Colapso antigo | lateral leste | — |

As secundárias são alternativas de enriquecimento, não cotas. R20 e R22 são as únicas candidatas a expedição perdida; evitar repetir o mesmo conjunto. Entradas mantêm centros livres e não anunciam saídas ou loot inexistentes.

## Pilotos e reaproveitamento

- **R08:** compor sobre sarcófago/cobertura já existente ou recesso. Tampa, cerâmica e tecido existentes; produzir mortalha N01. Não criar outro volume atravessável no chão livre.
- **R19:** mesa e correntes existentes; produzir caixão N02 e ferramentas N03. Reservar pedestal da manivela, loot e os dois encontros; padiola N04 somente depois do piloto.
- **R13:** marcas de inundação, umidade e cerâmica existentes; produzir madeira N08 e sedimentos N09. Intervenção manual na margem oeste; a faixa proposta NÃO contorna a proteção de `CanalReserved`.

## Famílias existentes e lacunas

| Composição | Famílias existentes a reaproveitar | Arte nova |
|---|---|---|
| Memorial apagado | CAT-A09, CAT-P03, CAT-D07, CAT-D12 | Nenhuma obrigatória |
| Sepultura violada | CAT-P01, CAT-P02, CAT-P10, CAT-D11 | N01 |
| Ossário retomado por raízes | CAT-A08, CAT-P04, CAT-D01, CAT-D02, CAT-D06 | Nenhuma obrigatória |
| Ritual interrompido | CAT-P06, CAT-D07, CAT-D11, CAT-D12 | N05 |
| Cela esquecida | CAT-A11, CAT-A12, CAT-D09, CAT-D10 | N06, N07 |
| Trabalho dos coveiros | CAT-P07, CAT-D10 | N02, N03, N04 |
| Margem após a enchente | CAT-F09, CAT-D03, CAT-D08, CAT-P10 | N08, N09 |
| Expedição perdida | CAT-D09, CAT-D11 | N10, N11 |
| Colapso antigo | CAT-A05, CAT-P08, CAT-P09, CAT-D12 | Nenhuma obrigatória |

IDs CAT referem-se ao catálogo atual, com arquivos verificados em Resources. Reaproveitamento técnico não substitui inspeção visual dos recortes e das variantes antes do piloto.

## Produção adicional proposta

| ID de planejamento | Peça | Prioridade | Envelope em células | Regra |
|---|---|---|---|---|
| N01 | Mortalha rasgada | piloto | 2×1 | Pano funerário plano; não usar silhueta de cadáver ou monstro. |
| N02 | Caixão inacabado | piloto | 3×1 | Volume em recesso ou bloqueio existente; distinto de baú. |
| N03 | Ferramentas de coveiro | piloto | 2×1 | Pá e ferramentas em conjunto; sem brilho de pickup. |
| N04 | Padiola funerária | posterior | 3×1 | Compor área de serviço; carro quebrado fica como variante posterior. |
| N05 | Incensário caído | posterior | 1×1 | Sem fumaça ou luz nova. |
| N06 | Tigela e utensílios de cela | posterior | 1×1 | Cerâmica fosca; sem estado de saque. |
| N07 | Palha deteriorada | posterior | 2×1 | Decalque de baixo contraste. |
| N08 | Madeira encharcada encalhada | piloto | 3×1 | Fragmentos junto à margem; não sugerir ponte. |
| N09 | Sedimentos e detritos de margem | piloto | 3×1 | Decalque, sem ocultar o limite físico da água. |
| N10 | Mochila rasgada decorativa | posterior | 1×1 | Vazia e danificada, distinta dos pickups. |
| N11 | Acampamento apagado | posterior | 2×2 | Cinzas e utensílios; sem fogo, loot ou interação nova. |

N01–N11 são IDs de planejamento, ainda sem sprites ou caminhos runtime. O piloto requer cinco famílias novas (N01, N02, N03, N08 e N09). Preservar pintura e leve inclinação aprovadas, sem introduzir câmera isométrica; volumes coerentes com a projeção superior existente. Não girar inscrições espelhadas.

## Corredores

| ID | Ligação / área | Família | Meta opcional |
|---|---|---|---|
| C00 | R00 ↔ R18 | Ossário retomado por raízes | 1 |
| C01 | R01 ↔ R08 | Sepultura violada | 1 |
| C02 | R02 ↔ R10 | Cela esquecida | 1 |
| C03 | R03 ↔ R11 | Cela esquecida | 1 |
| C04 | R04 ↔ R17 | Ossário retomado por raízes | 1 |
| C05 | R05 ↔ R16 | Ritual interrompido | 1 |
| C06 | R06 ↔ R14 | Memorial apagado | 1 |
| C07 | R07 ↔ R12 | Margem após a enchente | 1 |
| C08 | R08 ↔ R09 | Sepultura violada | 2 |
| C09 | R09 ↔ R10 | Memorial apagado | 2 |
| C10 | R10 ↔ R11 | Cela esquecida | 2 |
| C11 | R11 ↔ R12 | Cela esquecida | 2 |
| C12 | R14 ↔ R15 | Memorial apagado | 2 |
| C13 | R15 ↔ R16 | Memorial apagado | 2 |
| C14 | R16 ↔ R17 | Ritual interrompido | 2 |
| C15 | R17 ↔ R18 | Ossário retomado por raízes | 2 |
| C16 | R18 ↔ R19 | Ossário retomado por raízes | 2 |
| C17 | R19 ↔ R08 | Trabalho dos coveiros | 2 |
| C18 | R19 ↔ R20 | Trabalho dos coveiros | 2 |
| C19 | R20 ↔ R21 | Ritual interrompido | 2 |
| C20 | R21 ↔ R12 | Ritual interrompido | 2 |
| C21 | R21 ↔ R22 | Ritual interrompido | 2 |
| C22 | R22 ↔ R09 | Sepultura violada | 2 |
| C23 | R22 ↔ R10 | Sepultura violada | 2 |
| C24 | R20 ↔ R23 | Ritual interrompido | 2 |
| C25 | R23 ↔ R15 | Margem após a enchente | 2 |
| C26 | R20 ↔ R16 | Ritual interrompido | 2 |
| C27 | R18 ↔ R20 | Ossário retomado por raízes | 2 |
| CA01 | Galeria longa do canal | Margem após a enchente | 2 |
| CA02 | Ponte da comporta | Margem após a enchente | 0 |
| CA03 | Rio e saída da barca | Margem após a enchente | 0 |

Nos corredores, adaptar a família a detalhes rasos de 1–2 células nas bordas: placas, cerâmica, cera, sedimentos ou raízes; não inserir caixões/móveis altos na passagem. Sem conjuntos nos cruzamentos e acessos às portas. Excluir os trechos dentro das salas para não duplicar sua decoração. CA01 admite somente projeto manual lateral; CA02/CA03 permanecem livres de grupos no trajeto.

## Reservas registradas

- 59 pontos: 8 spawns, 3 extrações, 4 mecanismos, 2 pedestais, 24 pontos de loot e 18 encontros. Raios extraídos da política de `DungeonDetails`: 3 / 5 / 2,8 / 3 / 2,8 / 2 unidades, respectivamente.
- Preservar os 64 candidatos de quebráveis, inclusive os não ativados na partida: pegada 0,9 u + folga total de 0,2 u. Permanecem 48 objetos ativos e chance de loot de 10%; não alterar balanceamento.
- Proteger aproximações de portas (regra `AtDoorApproach`), cruzamentos, coberturas/pilares e bounds dos renderizadores já colocados. Esses elementos exigem conferência por peça, não aparecem todos como círculos na planta.
- `CanalArea` e `CanalDetour` continuam reservados à colocação automática. A nova decoração de margens deverá ter âncoras próprias e checagens explícitas; não remover `CanalReserved` globalmente.
- Barca: proa e percurso para sul, deslocamento de 16 células, envelope visual aproximado 2×4 células após rotação. Reservar toda a varredura e conferir bounds reais. O sweep horizontal antigo em `DungeonDetails.Reserved` é legado redundante da reserva ampla; não reutilizá-lo como modelo da nova rota.
- Nenhum collider, novo bloqueio de visão, interação, luz ou cobertura será criado para acomodar decoração. Volumes somente em áreas já bloqueadas/recessos; piso livre recebe decalques planos.

## Integração prevista, sem executar nesta etapa

Coordenar `DungeonDetails`, `DungeonProps` e `DungeonSectorArt` com um registro comum de ocupação, incluindo composições autoradas antes do preenchimento opcional. Usar IDs estáveis, sem novos NetworkObjects cosméticos ou sorteio divergente por cliente. Falta de espaço omite a peça; falha de asset continua diagnosticável. Os marcos atuais continuam; substituições pontuais devem retirar apenas duplicatas identificadas.

## Evidência e próximo passo

Conferidos por leitura: geometria atual, ligações, pontos de conteúdo, candidatos de quebráveis, famílias e caminhos de assets. Gerados planta e JSON fora da Unity. Nenhum teste automatizado, Unity, build ou alteração de runtime foi iniciado.

A etapa 2 montará os três pilotos, fixará âncoras individuais e permitirá avaliar escala, densidade, encaixe e legibilidade antes de replicar o restante. Referências: Darkwood (atmosfera), Bullet Echo (combate), catacumbas de Roma e Paris (organização funerária).

