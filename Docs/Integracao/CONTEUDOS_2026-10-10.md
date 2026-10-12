# Integracao seletiva — armas, plantas, Esporifero e minigames

Conteudo importado de `feature/armas-monstro-planta` e `feature/esporifero`, preservando o codigo local posterior a main. Nenhuma branch foi mesclada integralmente. A cena Game, MvpSetup, engine e pacotes nao foram substituidos. Player e Bot receberam apenas o componente de contaminacao.

## Armas

- Pistola inicial preservada: capacidade e recarga usam o Gameplay.asset atual.
- Rifle de ferrolho: cinco tiros, dano 75, alcance 24, intervalo/ferrolho 1,35 s e recarga 3,2 s.
- Espingarda de dois canos: dois cartuchos, oito chumbos de dano 13 cada em curta distancia, queda de dano depois de tres unidades, alcance nove, intervalo 0,55 s e recarga 2,6 s.
- Q/2 ou botao da barra: trocar arma. R/3: recarregar. 1: bandagem. I: mochila.
- As armas e municoes aparecem no loot, incluindo ruinas proximas aos cinco abrigos iniciais. MO/AR indicam rifle/municao; SG/CA, espingarda/cartucho.
- Bancada: tres metais + uma polvora + um componente produzem 12 tiros de pistola, cinco de rifle ou quatro cartuchos. A receita da pistola continua usando o asset de balanceamento.
- Municao carregada acompanha o item ao mover, guardar, soltar e extrair. Trocar ou mover a arma cancela a recarga sem consumir reserva. Troca nao elimina o intervalo do ultimo tiro.
- O inventario permanece privado. Somente o tipo da arma equipada e replicado aos observadores para o visual. Disparos continuam usando as mensagens filtradas por visibilidade/distancia; a espingarda toca um som por cartucho, nao por chumbo.
- O WAV existente e usado como som provisório das armas; esta integracao nao inclui novos audios de rifle ou espingarda. O rifle nao amplia a visao nem adiciona mira telescopica.

## Novas criaturas

Somente o mapa grande recebe cinco colonias de plantas e cinco Esporiferos, procurando espaco perto dos abrigos. O mapa pequeno conserva o monstro de investida.

Planta-mae: 300 de vida, ate tres atacantes e dois curadores. Bulbos atacantes amadurecem em nove segundos; filhos perseguem em torno da mae. Curadores aplicam 12 de cura somente apos a chegada das particulas e revalidacao pelo servidor. Matar a mae remove os filhos e deixa duas polvoras. Os sprites e efeitos da branch foram preservados.

Esporifero: usa a locomocao basica compartilhada, sem investida e sem substituir seu sprite pelo visual geometrico. Cada 38 pontos de dano nao letal acumulado prepara uma descarga por 1,2 s. Nuvens de raio 2,8 duram oito segundos e persistem apos a morte. Vento inicial (0,65; 0,15); paredes bloqueiam propagacao e exposicao. Nos abrigos iniciais, a nuvem nasce sem deslocamento.

Contaminacao: cresce 20 por segundo na nuvem, recupera 15 por segundo fora; acima de 40 causa quatro de dano por segundo e tosse audivel pela IA. Sobreposicao nao multiplica o efeito. Humanos e bots usam as mesmas regras.

## Minigames aprovados

Pesca no acampamento e na expedicao: boia belisca a isca antes da mordida verdadeira. Tocar/apertar antes da mordida perde a captura. Quando a boia afundar e soar o aviso, pressione dentro da janela de 1,6 s. Depois, segure/solte para subir/descer a faixa verde e manter nela o peixe amarelo. Comuns se movem suavemente, raros mudam mais rapido e grandes fazem arrancadas. Progresso, premio e entrada sao resolvidos pelo servidor.

Cozinha: o indicador atravessa a barra em oito segundos. Parar na faixa laranja entrega preparo perfeito; na faixa maior, bom. Retirar muito cedo deixa cru; deixar passar queima. Espaco ou botao Parar e retirar. A qualidade e calculada pelo relogio do servidor, preservando os buffs existentes. Este modelo substitui virar os dois lados.

## Verificacao

- `Tools/Audit/Run-AuditProbe.ps1`: regras reais de inventario, receitas, sequencia e rejeicao de repeticao, fora da Unity.
- `ContentIntegrationChecks.Run`: importacao dos PNGs, recortes, prefabs/componentes/referencias, IDs de itens, receitas, limites das armas/esporos e simulacao dos minigames na Unity 6000.0.84f1.
- Cliente/servidor separados, fisica real das habilidades, interface em Play e Android precisam do teste manual. As verificacoes de regras e importacao nao substituem esses testes.

Protocolo 12. O .bat atualiza o servidor no proximo inicio; o cliente normal pode ser aberto no projeto. Nao foi gerado APK nesta entrega. Para conferir o conteudo, selecione mapa grande e teste rapido com zero bots.
