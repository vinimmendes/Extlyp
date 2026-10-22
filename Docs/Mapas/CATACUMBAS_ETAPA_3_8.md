# Catacumbas — etapa 3.8: revisão final

Revisão estática concluída em 2026-10-15. Os lotes de produção estão integrados no código; a avaliação em partida permanece pendente. Esta etapa não gerou imagens nem alterou pixels dos sprites existentes.

## Inventário entregue

| Lote | PNGs selecionados |
|---|---:|
| Pisos e água | 40 |
| Arquitetura | 32 |
| Sepulturas e coberturas | 28 |
| Identidade dos setores | 15 |
| Extrações e ferramentas | 23 |
| Detalhes e atmosfera | 37 |
| Total | 175 |

São 69 famílias e 189 regiões de sprites, incluindo os quadros das duas animações. Revisões de fonte preservadas, mas não selecionadas para integração, não entram nessa contagem.

- [Galeria do conjunto](../../Art/Catacumbas/Revisao/index.html): fontes existentes e recortes dos manifestos, com filtros e fundos de comparação.
- [Inventário de arquivos](CATACUMBAS_INVENTARIO_ARTE.json): dimensões, transparência, hashes e metadados por recurso.
- Preparação reproduzível: `Tools/Build-DungeonArtReview.py`, sem iniciar Unity ou simular o jogo.

No lote selecionado, o inventário não encontrou arquivos ausentes, diferenças de bytes entre fontes selecionadas e cópias de runtime, GUIDs repetidos ou regiões fora das texturas. Essa conferência não constitui auditoria global de GUIDs do projeto nem confirma a importação efetiva pela Unity.

## Revisão visual e de integração

A inspeção de amostras dos seis lotes mostrou continuidade de calcário, ferro escuro e madeira abandonada, com atmosfera inspirada em Darkwood e intenção de circulação e cobertura legíveis inspirada em Bullet Echo. Foram inspecionados piso, pilar, sarcófago, altar, escadaria selecionada e vela. A galeria reúne todo o conjunto para comparação, mas não foi validada em navegador nesta etapa.

A leitura dos montadores confirmou o uso das máscaras e posições existentes: piso e decoração sem novos colisores, cobertura sobre a pegada original e marcos nas superfícies reservadas. Detalhes usam distribuição esparsa, reservas de objetivos e casulos somente em nichos vazios. Os recortes da arquitetura preservam os vãos; os deslocamentos de barco e plataforma continuam ligados à apresentação existente.

As regras de extração continuam no servidor: escadaria com preparação de 6 s; poço e canal com preparação de 4 s; extração de 3 s. A manivela prepara o poço e a chave abre a passagem opcional da cisterna. Permanecem somente aranha, espreitador e couraçado. Nenhuma regra, geometria física ou composição de monstros foi alterada nesta revisão.

Ainda dependem de observação em partida: composição completa com névoa e iluminação, emendas dos pisos, leitura do combate, registro dos quadros animados e desempenho real em Android. Não houve medição de desempenho nem promessa de ganho.

## Correção pontual

`ItemIconLibrary.ApplyWorld` agora reutiliza o filho `ItemIcon` e seu `SpriteRenderer`, atualizando sprite, transformação e visibilidade. Antes, toda chamada criava outro filho. A chamada em `LootPickup.OnNetworkSpawn` e o tratamento de ocultação/reapresentação motivaram a correção de idempotência.

É uma constatação estática: o acúmulo durante um ciclo de visibilidade de rede não foi reproduzido em execução. A correção mantém o tamanho visual, o colisor de coleta e as regras de inventário e servidor existentes.

## Avaliação prática pendente

1. Percorrer entradas, salas e rotas alternativas; observar colisões das paredes, vãos e leitura das coberturas.
2. Enfrentar os três monstros sob névoa; comparar teias decorativas com a teia ativa da aranha.
3. Preparar e usar as três extrações, incluindo cancelamento e retomada. Quando disponível, observar clientes separados, entrada tardia e extrações simultâneas.
4. Conferir manivela no poço, chave na passagem da cisterna, ponte aberta/fechada e água. Observar pickups após ocultação/reapresentação de rede para verificar o visual único.
5. Observar escala e controles em Android horizontal, nos dois temas existentes. Se houver lentidão, registrar aparelho, cenário e medidas para investigação direcionada.

Por preferência do usuário, não foram iniciados testes automatizados, sessões Unity, builds ou renders de validação. A revisão encerra a produção e a conferência estática desta sequência de oito etapas; não declara o mapa validado em execução.
