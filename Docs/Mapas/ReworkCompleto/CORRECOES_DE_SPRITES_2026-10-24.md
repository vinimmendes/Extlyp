# Correções de apresentação — 24/10/2026

Relatos do usuário durante sua avaliação prática das Catacumbas. Estas correções tratam falhas de apresentação da versão atual; não encerram o objetivo de aproximar a montagem dos conceitos aprovados.

## Coberturas

O retângulo opaco atrás do caixão era o desenho provisório de `DungeonSarcophagus`, com a mesma ordem da base pintada. Após carregar a apresentação publicada, o jogo oculta somente esse SpriteRenderer; conserva objeto, camada e colisão.

## Extrações

O catálogo publicado já continha `DockPier` e `DockEdge`. A montagem do cais tentava registrar as mesmas chaves novamente, interrompendo a montagem de todas as extrações. `DungeonExtractionArt` agora reutiliza as peças publicadas e cria recortes somente quando ausentes. O usuário confirmou posteriormente que os sprites da escada reapareceram. A relação com o erro anterior é uma explicação provável; não houve captura do estado anterior da sessão do usuário para comprová-la.

## Apoios de vigília

Reproduzido em montagem isolada com o código da partida e o catálogo publicado:

- O nicho já incluía a moldura de pedra. A receita adicionava uma laje inteira comprimida sobre sua borda, criando a faixa sobreposta.
- O tecido com projeção frontal recebia a rotação do apoio e aparecia invertido ou deitado nas outras paredes.
- Os apoios norte de R16 e R20 recebiam dois tecidos praticamente na mesma posição. A cópia gerada é omitida quando o tecido original está presente.

`DungeonVotivePresentation` identifica exclusivamente essas peças das composições geradas antigas: omite a laje redundante e conserva a projeção frontal do tecido. A mesma correção é aplicada à cena editável quando sua apresentação é atualizada. A receita de produção também foi corrigida; os assets continuam disponíveis individualmente na paleta.

A compatibilidade em partida não modifica o JSON publicado, as posições, as colisões ou as regras do servidor. Salvar a cena editável preserva sua montagem corrigida; uma publicação posterior continua sujeita à comparação de fidelidade. As capturas originais da produção não foram substituídas por uma nova aprovação geral.

Verificação limitada à montagem e aos sprites relatados, numa cópia temporária do projeto, sem Play, conexão ou build. Isso não comprova comportamento de extração, física, visibilidade em partida ou desempenho.

Resultado da conferência final: 26 bordas redundantes omitidas, seis tecidos com projeção preservada, duas cópias de tecido omitidas e definição publicada inalterada. Foram capturadas as orientações norte, sul, leste e oeste. [Registro da verificação](../../../Art/Catacumbas/ReworkCompleto/Correcoes/Vigilia/diagnostico.txt) · [Apoio leste corrigido](../../../Art/Catacumbas/ReworkCompleto/Correcoes/Vigilia/R16_EastVigil_0.png) · [Apoio sul corrigido](../../../Art/Catacumbas/ReworkCompleto/Correcoes/Vigilia/R20_SouthVigil_0.png).

A correção da cena de autoria é uma migração aplicada uma vez, para não reaplicar poses sobre as edições seguintes e sobre Undo/Redo. Depois de salvar/publicar uma montagem já corrigida, a regra de compatibilidade antiga deixa de ser necessária.
