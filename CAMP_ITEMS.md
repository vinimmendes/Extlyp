# Acampamento e itens — prototipo

Estado atual: acampamento online, persistencia e minigames autoritativos no servidor. Veja SERVER_SETUP.md.

Historico — esta etapa foi ampliada: regras atuais de mochila/baus com nove slots, pistola, fabricacao, Reciclador e minigames estao em EXPANSION_SETUP.md. A pesca nao concede mais buff automaticamente; e necessario preparar e comer o peixe. As secoes abaixo registram a etapa anterior.

## Fluxo

O cliente abre em um acampamento pessoal. Caminhe com WASD/setas no PC ou com o joystick esquerdo no celular. Aproxime-se de uma estrutura e pressione E ou toque no botao de interacao.

- Portao: abre o endereco do servidor e a busca por expedicao.
- Deposito: mostra materiais guardados e permite escolher ate tres bandagens para levar.
- Pier: custa 20 madeiras e 10 pregos para reparar. Depois permite pescar durante cinco segundos.
- Bem alimentado: oferece 20 de vida maxima adicional na proxima expedicao. Nao acumula e e consumido quando a partida realmente inicia.

## Materiais e bandagens

| Inicial | Item | Uso |
|---|---|---|
| M | Madeira | Reparar estruturas |
| P | Pregos | Reparar estruturas |
| B | Bandagem | Recuperar 25 de vida |

O mapa tem 12 pontos de loot. As caixas de materiais contem seis madeiras ou quatro pregos; os dois pontos nas casas contem duas bandagens. A coleta ocorre ao encostar. O monstro deixa pregos ao morrer.

A barra de acesso rapido tem a bandagem no primeiro espaco. Use a tecla 1 ou toque no B. O servidor confere vida, quantidade, estado da partida e intervalo de tres segundos. Vida cheia nao consome bandagem. A cura nunca ultrapassa a vida maxima. Os bots tambem podem usar bandagens coletadas.

Ao morrer ou desconectar durante a expedicao, os itens carregados caem no chao. Apenas uma extracao concluida envia os itens ao deposito. O bot nao recebe um acampamento nem gera progresso para humanos ao extrair. O botao Voltar ao acampamento permite retornar depois de extrair, morrer ou terminar o tempo.

## Estado atual

Ainda nao ha contas nem banco de dados. Cada instalacao usa uma identificacao anonima de prototipo. As tres salas compartilham os acampamentos em memoria, para que o deposito e o pier acompanhem o jogador ao trocar de sala. Encerrar todas as salas perde os materiais e reparos. O cliente conserva o estado enquanto o jogo permanece aberto e atualiza os dados ao conectar novamente. Esse identificador nao substitui autenticacao de conta.

Os pedidos de reparo, pesca e equipamento sao conferidos no servidor ao entrar na expedicao. O cliente nao envia quantidades para adicionar ao deposito. Os materiais creditados vem da mochila controlada pelo servidor.

O protocolo atual e a versao 4: servidor e clientes precisam ser compilados com esta mesma versao. O APK anteriormente entregue continua com a versao anterior. APKs novos serao gerados somente quando solicitados; nao foi gerado um APK nesta alteracao.

## Verificacao

As verificacoes do menu Cyber Extraction > Verificar acampamento e itens cobrem custos, quantidade insuficiente, pedidos repetidos, limite de equipamento e consumo do buff. Testes com servidor e cliente separados verificaram cura, consumo, cooldown, morte e drops, extracao sem credito duplicado, reparo, pesca e uma segunda expedicao com equipamento e buff sincronizados. Movimentacao local e interacao com o portao tambem foram verificadas.

Valores e geometria sao iniciais, sem sprites finais. O proximo teste manual deve conferir legibilidade de M/P/B e toque na barra no celular, quando um APK novo for solicitado.
