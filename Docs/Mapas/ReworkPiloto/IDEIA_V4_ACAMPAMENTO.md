# Ossário inferior — a ordem que cedeu, o abrigo que ficou

Revisão da ideia por solicitação do usuário: acrescentar **um acampamento abandonado** à sala já composta. Esta etapa desenvolve a ideia e seu conceito visual; não altera o pacote ativo do mapa.

## Referência escolhida pelo usuário

O usuário escolheu explicitamente a imagem `Imagem do ChatGPT 23 de out. de 2026, 12_31_30.png`, preservada em `Art/Catacumbas/ReworkPiloto/Referencia-escolhida-v4.png`, como base visual desta ideia. Preservar o ritmo e a profundidade aparente dos nichos, o detalhe das raízes e do dano noroeste, a continuidade do piso e as duas bases funerárias dessa imagem. A revisão visual acrescenta apenas o abrigo abandonado no sudoeste e o pequeno grupo de urnas no sudeste.

Esta é a referência visual escolhida, não uma aprovação de simplificações da montagem anterior. Na tradução futura para o jogo, diferenças necessárias de geometria ou escala devem aparecer na montagem executável e ser explicitadas antes de substituir essa composição.

## Camadas de história

1. **Uso funerário:** a sequência de nichos norte/leste, os recipientes e as urnas ainda guardadas identificam um ossuário organizado para receber restos exumados.
2. **Deterioração:** a extremidade noroeste começou a ceder. Raízes e fragmentos continuam ligados a essa ruptura, como na composição anterior.
3. **Ocupação posterior:** alguém encontrou a ala já abandonada e improvisou um abrigo junto ao recipiente fechado do sudoeste. Escolheu uma proteção existente e um acesso próximo, sem reorganizar toda a sala.
4. **Abandono do abrigo:** não há fogo vivo, ocupante ou equipamento pronto para uso. Marcas de permanência e poucos pertences gastos mostram que o abrigo também ficou para trás. Não definir uma causa de saída que a cena não consiga sustentar.

## Como o acampamento pertence ao lugar

O recipiente funerário fechado é a âncora do abrigo, em vez de existir um núcleo de acampamento separado dele. A área de descanso ocupa o piso logo ao sul da base; um tecido baixo pode se apoiar na borda da pedra. Os sinais de preparo de comida ficam perto, afastados da área de dormir e diretamente sobre as lajes. Os pertences se encostam ao apoio existente, em vez de formar uma exibição de itens.

Um leito de tecido gasto, cinzas frias com sinais de fuligem, utensílio usado e uma bolsa abandonada são candidatos visuais para comunicar descanso, preparo e saída. Não são uma lista obrigatória: a montagem pode reduzir ou substituir peças mantendo essas relações. Não acrescentar pá, corda, esqueleto ou outro objeto apenas por terem aparecido em exemplos anteriores.

Marcas discretas de uso ligam o acesso oeste ao abrigo, contornando a base. O piso continua sendo a mesma pedra. Nenhuma mancha oval de terra ou círculo de material separa o acampamento do ossuário. O centro livre, os nichos e a falha noroeste continuam participando da leitura da sala.

## Limites de execução

- Apoio na geometria existente: base sudoeste nas células (14–15,40–41), abrigo proposto no piso imediatamente ao sul, contido na faixa aproximada x=12–16,5 / y=36,5–40. Coordenadas são envelope de conceito, não autorização para ocupar reservas.
- Manter os acessos oeste e sul, loot, encontros e o grupo de urnas quebráveis do sudeste. Nenhum collider, cobertura, luz, loot ou interação nova para representar o abrigo.
- Mesma vista superior, parede aprovada e escala do piloto. Tecido, cinzas e vestígios baixos podem ser sprites com alpha. Apoios sobre o recipiente não mudam sua pegada física.
- Sem barraca volumosa, novas paredes, luz volumétrica ou detalhes impossíveis de reproduzir no pipeline 2D. O conceito não promete fogo animado, fumaça ou iluminação nova.
- O estudo deve preservar a composição do ossuário e acrescentar a ocupação no sudoeste. Antes de aplicar ao jogo, traduzir essa relação para módulos efetivos e revisar seus envelopes, conforme o fluxo registrado em AGENTS.md.

## Entrega desta revisão

Retorno do usuário: a ideia foi considerada executável e a composição recebeu avaliação positiva, com ressalva explícita de que a cama está desproporcional. Manter a composição e reduzir o leito antes da integração, usando a escala do personagem e a grade de 1,06 unidade como referências. Conferir também a relação com o recipiente funerário, as urnas, a bolsa e a panela; não copiar literalmente as dimensões do leito do conceito. Esse retorno não registra validação em execução nem autoriza expansão para outras salas.

Ideia e [conceito visual sobre a imagem escolhida](../../../Art/Catacumbas/ReworkPiloto/Conceito-ossuario-acampamento-urnas-v4.png). Estudo editado pela ferramenta integrada image_gen; prompt e origem em `Art/Catacumbas/ReworkPiloto/prompt-conceito-selecionado-v4.json`.

A imagem acrescenta o leito gasto e a bolsa junto ao recipiente sudoeste, cinzas frias e panela nas lajes próximas e duas urnas no canto sudeste, mantendo a leitura funerária da referência. É um conceito pintado, não uma captura ou montagem de sprites já integrados. A composição ativa continua sendo a revisão 3 com as urnas quebráveis agrupadas. Nenhum teste, sessão Unity ou build foi iniciado.
