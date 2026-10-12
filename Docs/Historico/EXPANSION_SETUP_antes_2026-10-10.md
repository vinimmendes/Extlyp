# Expansao do prototipo — mapa, inventario e atividades

## Para testar

Salve o projeto, abra Iniciar-Servidor.bat e aguarde a atualizacao automatica do servidor. Na Unity, abra Game e aperte Play. Caminhe ate o portao do acampamento e entre em 127.0.0.1. As tres salas continuam com cinco participantes cada.

Esta entrega altera o codigo e o protocolo para 6. Nao foi gerado APK nem executada uma partida completa. A verificacao realizada foi de compilacao dos scripts e menus do editor. Clientes compilados anteriormente precisam ser atualizados antes de conectar ao novo servidor.

## Controles

- WASD/setas ou joystick: movimentacao.
- Mouse/joystick direito e botao de tiro: mira e disparo.
- R ou botao da barra: recarga da pistola.
- 1 ou B na barra: bandagem.
- I ou botao Inventario: abrir a mochila.
- E ou Interagir: iniciar abertura de um bau proximo; perto da margem de um lago, iniciar pesca.
- Na grade, arraste entre slots ou entre mochila e bau. Arrastar para fora joga a pilha no chao.
- No acampamento, aproxime-se da estrutura e pressione E ou toque na interacao.
- Pesca: aguarde a fisgada, segure Espaco/botao para fisgar, depois alterne segurar e soltar para manter a tensao entre os extremos.
- Fogueira: escolha um peixe, controle os dois lados, vire com Espaco/botao e retire antes de queimar.

## Mapa

Escolha no portao entre Teste (30 x 46) e Grande (900 x 1380). Teste e a opcao inicial, preservando as duas casas, paredes centrais, quatro caixas e extracao do mapa anterior; tem um monstro e poucos pontos de loot/baus para testes rapidos. As mecanicas novas de inventario, municao e baus tambem funcionam nele.

Grande tem cinco abrigos de nascimento separados, ruinas, coberturas, lagos, recursos, baus e monstros distribuidos. Sete zonas de extracao reduzem trajetos impossiveis para o limite atual de cinco minutos. O HUD mostra distancia e direcao da extracao mais proxima. A navegacao usa janelas locais de 30 x 30 unidades em vez de uma grade cobrindo o mapa inteiro; monstros distantes dos participantes ficam parados.

Uma sala vazia assume o mapa escolhido pelo primeiro jogador. A busca pula salas que estejam esperando jogadores de outro mapa; os tres processos podem hospedar mapas diferentes simultaneamente. Servidor e clientes sincronizam a geometria antes de jogar. A escolha e lembrada no cliente.

A camera da expedicao foi afastada de 7 para 9 unidades ortograficas; o acampamento usa 9. O alcance de visao e tiro permanece igual. No mapa grande, o minimapa mostra terreno, pontos de interesse, abrigos, saidas e o jogador local; M ou toque amplia a visualizacao. Inimigos e loot escondidos nao sao revelados. O minimapa nao aparece no mapa de teste.

O terreno e uma composicao inicial por codigo, com formas e cores provisórias. A distribuicao dos pontos de interesse e deterministica, igual no servidor e clientes. A duracao de cinco minutos foi mantida porque um novo valor nao foi decidido.

## Pistola e municao

Pistola: carregador de 12, reserva inicial de 24, recarga de 1,5 segundo e caixas de 12 tiros. A reserva e formada por pilhas de municao na mochila. Uma pistola descartada conserva os tiros restantes do carregador no item. A arma deixa de disparar quando nao ha carregador ou reserva; bots seguem as mesmas regras. Dano e cadencia continuam nos valores anteriores.

Bancada: 3 metais + 1 polvora + 1 componente produzem 12 tiros. Componentes tambem podem ser adquiridos por troca na bancada: inicialmente 2 metais por unidade, valor provisório.

Reciclador: niveis 1/2/3 recuperam 10%/20%/30% do metal correspondente aos cartuchos disparados. Fracoes sao acumuladas; os metais recuperados entram na mochila ou caem no chao se ela estiver cheia. A eficiencia nao se aplica a polvora. Custos iniciais de melhoria: 10/20/30 metais, ainda sujeitos a balanceamento.

## Inventario e armazenamento

Mochila e bau seguro: nove slots cada. Todos os itens empilham ate 20; pistolas nao empilham. Peixes de tipos diferentes e alimentos de qualidade diferente usam pilhas separadas. Os icones usam abreviacoes, incluindo M, P e B. Baus incomuns e raros tambem podem fornecer uma pistola para substituir equipamento perdido.

A mochila e o equipamento escolhido para a expedição. O bau permanece no acampamento. Na morte ou desconexao durante a partida, a mochila cai no chao; o conteudo do bau fica protegido. Na extracao, a mochila retorna ao acampamento: o jogador escolhe o que guardar, levar novamente, preparar ou descartar. Nada e apagado porque o bau ficou cheio.

## Baus da expedicao

Comum/incomum/raro: 2/4/6 segundos para abrir. Dano, morte, fim da partida ou afastamento interrompem a abertura. Depois de abrir, a grade de nove slots permite retirar, guardar, rearranjar e descartar itens. O servidor confere distancia, estado e capacidade em cada transferencia. As recompensas aumentam conforme a raridade e incluem metal, componentes, municao, bandagens e polvora nos baus melhores.

## Investida Devastadora

Preparacao de mira: 3 segundos, provisório. O monstro mantem o mesmo alvo e acompanha sua posicao visivel ate iniciar o avanco, mesmo se ele sair do raio inicial de seis unidades. Se uma parede esconder o alvo, mantem a ultima direcao vista; nao acompanha alvos atraves de paredes. Morte, extracao ou desconexao do alvo cancela a preparacao. A direcao fica travada no inicio do avanco.

Uma faixa vermelha mostra a area prevista, com a largura do corpo e ate 20 unidades de comprimento, encurtada quando encontra uma parede. O indicador acompanha a mira e respeita a visao local. Pode ser desativado no componente Monster pela opcao Mostrar area da investida.

Investida: 40 unidades/s durante ate 0,5 segundo, 40 de dano em um impacto. Paredes interrompem e outros monstros nao consomem a habilidade nem bloqueiam fisicamente o avanco. Recuperacao vulneravel: 2 segundos. Intervalo: 8 segundos. Ajuste o tempo de mira depois de observar a esquiva.

## Monstros do mapa grande

O mapa grande recebe tres criaturas por ruina, alternando os sete tipos ativos, um Imitador no lugar de um bau a cada quatro ruinas e um monstro de investida a cada oito ruinas. Posicoes bloqueadas sao ajustadas; nenhum monstro nasce a menos de dez unidades dos abrigos iniciais. O mapa pequeno conserva somente o monstro de investida. As criaturas distantes de todos os participantes param a IA, com ativacao a 65 unidades.

| Tipo | Visual e comportamento inicial |
| --- | --- |
| Escutador | Circulo roxo com antenas; 110 de vida. Visao de apenas 1,5 unidade. Ouve passos, tiros, abertura de baus e itens descartados com alcance ampliado (limite 50). O som mais recente muda o destino e permite distrai-lo. |
| Carapaca | Hexagono cinza, placas frontais e traseira laranja; 220 de vida, velocidade 1,5 e dano 25. Recebe apenas 25% do dano de tiros no cone frontal de 120 graus; laterais e costas recebem dano integral. |
| Tecela | Corpo roxo com oito patas; 70 de vida. Prepara disparo por 0,8 segundo e lanca teia a cada seis segundos. Deixa ate tres armadilhas na regiao durante o combate. Teias reduzem a velocidade de humanos e bots em 50%, podem ser contornadas e sao destruidas por um tiro. Armadilhas duram 25 segundos; teias disparadas, 12. |
| Berrante | Corpo rosa com saco vocal que incha; 80 de vida. Prepara grito por dois segundos; qualquer dano interrompe. Ao completar, alerta monstros em 45 unidades para a ultima posicao detectada. Intervalo de 12 segundos; apos interrupcao, cinco. |
| Imitador | Mesmo desenho e texto BAU de um bau comum, sem olhos ou dentes enquanto escondido; 130 de vida. Interagir por E ou toque inicia transformacao de 0,75 segundo, revelando mandibula, dentes, olhos e seis patas. Tiros tambem revelam. Depois persegue e causa 30 de dano. Um tremor discreto ocasional denuncia o disfarce. |
| Corrosivo | Triangulo verde com boca amarela; 80 de vida. Prepara cuspe por 1,2 segundo, com intervalo de seis. Projetil causa 10 de dano e deixa poca de raio 2,3 por oito segundos; poca causa quatro de dano a cada meio segundo. Tenta manter distancia. |
| Espreitador | Losango azul escuro e olhos cianos; 90 de vida. Prioriza alvos isolados e tenta se aproximar pelas costas. Velocidade 4,6 fora da visao do alvo e 1,5 quando iluminado pelo cone de visao. Ataque pelas costas causa 30; demais, 15. |
| Devorador | Corpo vermelho com boca circular e dentes; 140 de vida. Busca cadaveres de jogadores e criaturas a ate 12 unidades e leva 2,5 segundos para comer. Dano interrompe a alimentacao e tiros causam dano dobrado durante ela. Cada cadaver consumido cura 45 e aumenta vida maxima em 25 e dano em quatro, ate cinco evolucoes. O visual cresce; a area fisica permanece constante. |

Teias, pocas de acido, projeteis e cadaveres pertencem a partida e continuam existindo se a criatura morrer. O servidor resolve as habilidades, blindagem, lentidao, dano e consumo exclusivo de cadaveres. Efeitos e aparencias respeitam a visao local. Cadaveres desaparecem apos 45 segundos e todos os efeitos sao removidos ao reiniciar a sala.

Valores sao provisorios para teste manual. Descartar um item da mochila produz ruido e pode distrair o Escutador. O sistema ainda usa formas geradas por codigo, sem sprites externos. Nao foi gerado APK nem executada partida automatica nesta mudanca.

## Inicio rapido da partida

No portao, marque Teste rapido (sem espera) e escolha de zero a quatro bots. Funciona nos dois mapas. A sala aceita um humano e inicia assim que ele termina de sincronizar, com a quantidade escolhida e sem esperar os 30 segundos. Com zero bots, voce pode testar apenas o monstro. Salas de teste rapido nao se misturam com a fila normal. Desmarcado, continua a regra de cinco participantes com preenchimento apos 30 segundos.

## Botao temporario de reinicio

Reiniciar servidor (temporario) aparece na parte inferior da tela do cliente Windows executado dentro do projeto, incluindo Play na Unity. Ele reinicia as tres salas locais, usa os scripts existentes e verifica o codigo atualizado antes de ligar novamente. O cliente sai da conexao e pode entrar outra vez depois da atualizacao. Nao existe chamada de rede permitindo reiniciar um servidor remoto. O botao nao aparece quando a conexao aponta para outro PC nem em Android.

O reinicio encerra as partidas e perde o progresso que ainda existe apenas na RAM. A opcao GameConfig.ShowTemporaryServerRestart permite retirar o botao depois. Erros do iniciador ficam em Logs/server-restart.log. Nesta alteracao apenas compilacao e sintaxe foram conferidas; o reinicio nao foi executado automaticamente.

## Pesca e fogueira

Pier reparado: 20 madeiras + 10 pregos. Pesca tem espera pela fisgada, janela para reagir, tensao e progresso da captura. Peixe comum, raro e grande usam dificuldades diferentes; podem aparecer metal ou componentes como captura incomum. Pescar entrega um item, sem conceder buff automaticamente. A mesma interacao e disponibilizada nas margens dos lagos da expedição.

A fogueira prepara peixes da mochila. Cada lado acumula calor separadamente; ultrapassar o limite queima, retirar cedo deixa cru. Peixe cru pode ser assado novamente. O preparo bom ou perfeito produz alimento; para receber Bem alimentado, selecione e coma o peixe assado na fogueira. Valores provisórios: +20 de vida maxima para preparo bom e +25 para perfeito, consumidos na proxima expedição. Cru e queimado nao concedem beneficio.

## Limites deste prototipo

Sem contas ou banco de dados: o progresso ainda esta na RAM compartilhada das salas. Acoes no acampamento sao simuladas localmente e confirmadas quando o jogador entra numa expedição; o servidor confere itens, receitas e custos. A validacao completa dos comandos dos minigames do acampamento permanece uma etapa futura; a pesca durante a expedição e simulada no servidor. Encerrar todas as salas perde o progresso dessa sessao.

A interface, a densidade do mapa, o desempenho no celular e o balanceamento precisam do teste manual. Nao houve teste de partidas nem de toque no celular nesta entrega, conforme solicitado.
