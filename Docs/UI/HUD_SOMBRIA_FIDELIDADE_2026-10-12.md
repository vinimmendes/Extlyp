# HUD Sombria — aproximação da referência aprovada

Objetivo solicitado: aproximar a HUD da imagem aprovada em pelo menos 95%, priorizando qualidade. Esta entrega implementa a composição e o tratamento visual; não atribui uma porcentagem de fidelidade comprovada sem uma captura atual da Unity.

## Implementação

- Vida horizontal e status ativos em caixas separadas, com símbolos de alimento e esporos.
- Relógio com dois dígitos por campo, painel compacto e seta apontando ao destino selecionado. Incerteza e progresso de extração continuam visíveis quando necessários.
- Mapa e Menu como botões separados, com símbolos próprios.
- Minimap local com recorte proporcional da geografia, massas gráficas de floresta, rotas reais, jogador direcional, saídas conhecidas/incertas/indisponíveis, norte e vento da configuração existente. O terreno é cartografia estilizada, não captura da cena nem indicação de obstáculos individuais.
- Mapa completo preserva todas as saídas, seleção de destino e legenda. Aberto, bloqueia comandos locais; partidas online continuam. Voltar/Esc fecha o mapa antes de abrir o menu.
- Painel da arma com silhueta, munição em destaque e reserva abaixo.
- Cura, troca, recarga e mochila com símbolos próprios e rótulos abaixo.
- Movimento e mira visíveis em repouso na expedição. Arraste continua relativo ao ponto inicial do dedo; tamanho e posição visuais vêm da configuração individual. A base mantém seu controle flutuante.
- Disparo dedicado maior, símbolo de projétil e aro de cobre.
- Interação contextual com símbolo, duração real baseada na raridade e progresso replicado pelo servidor. Portas, fechaduras e passagens não recebem tempo fictício.
- Bordas discretamente gastas, transparência, silhuetas tintáveis e anéis suavizados gerados em código. A referência aprovada foi preservada.
- Fonte Barlow Condensed Regular e SemiBold incluída com sua licença SIL OFL. Origem: https://github.com/google/fonts/tree/main/ofl/barlowcondensed. Nenhuma fonte do Windows foi redistribuída.

## Controles independentes

`HudControl` identifica cada botão, painel e controle. Cada entrada tem seu próprio `HudPlacement`, contendo âncora na área segura, deslocamento e tamanho. Cura, troca, recarga e mochila não derivam sua posição de um retângulo de grupo.

`HudLayout.Bounds(control)` determina o desenho; `HitBounds(control)` mantém alvo de toque mínimo de 44 pixels; `ScreenBounds(control)` converte a mesma geometria para o Input System. `SetPlacement(control, placement)` e `ResetPlacement(control)` permitem alteração isolada sem mover os outros elementos. Valores inválidos são rejeitados, retângulos visuais e alvos de toque ficam na área segura, e controles circulares preservam sua proporção. As molduras são geradas na proporção de cada painel, com cache limitado, para preservar espessura e desgaste ao redimensionar.

O editor de personalização está disponível em Menu > Opções > Organizar botões e tamanhos. Posições e tamanhos são salvos por aparelho, compartilhados entre temas e PC/celular. Os controles podem ser arrastados e redimensionados individualmente; a prévia informa que os valores são exemplos. As preferências de tema continuam salvas por aparelho.

## Dimensões principais no celular

Altura virtual de 600. Largura e margens externas dependem da área segura. O tema Simples compartilha a geometria e usa a própria paleta.

| Elemento | Tamanho padrão virtual |
| --- | --- |
| Vida | 230 × 42 |
| Bem alimentado / Esporos | 112 × 25 / 109 × 25 |
| Tempo | 132 × 58 |
| Mapa / Menu | 96 × 26 / 86 × 26 |
| Minimap | 178 × 160 |
| Arma | 144 × 65 |
| Ações secundárias | 50 × 50, ampliadas quando necessário ao alvo mínimo de toque |
| Movimento | 160 × 160 |
| Mira | 72 × 72 |
| Disparo | 110 × 110 |
| Interação | 140 × 38 |

## Limites da comparação

A vida permanece verde musgo fora do estado de perigo, conforme `DIRECAO_DE_ARTE_HUD.md`; a imagem conceitual mostra vermelho. O vento é apresentado a partir da configuração real, portanto não é forçado a NE. O mapa usa a geografia existente, não copia o cenário conceitual. Status, disponibilidade de ações e informações de extração dependem do estado real da partida.

`Art/HudSombria/hud-sombria-composicao.png` é uma prévia de autoria com valores ilustrativos, fonte incluída e silhuetas extraídas de `HudArt.cs`. Não representa a renderização nativa IMGUI, o granulado, a composição sobre o cenário ou o minimapa em execução. `render_composition.py` gera essa imagem para revisão artística; não é um teste de gameplay.

Revisão estática realizada. Sem compilação, testes automáticos, builds, sessões Unity ou validação em aparelho. A confirmação da meta de 95% permanece dependente da primeira validação prática do usuário e da comparação com uma captura da HUD Sombria durante a expedição.

## Segunda passagem de acabamento

Após o pedido de maior capricho na arte e nas bordas:

- Molduras com cantos suavizados, abrasões localizadas, variação discreta de pátina e traço interno quase imperceptível. O desgaste é calculado por dimensão, sem esticar marcas de um quadrado para um painel horizontal.
- Aros em resolução de 512 × 512 com borda suavizada, luz de contorno fosca e perda irregular de tinta. O disparo recebe um arco complementar parcial, com interrupções intencionais.
- Silhuetas em resolução de 256 × 256. Mochila com fecho, bolso e costuras; cartuchos com base e gravação lateral; baú com ferragens, tábuas e fechadura; pistola com slide, miras e empunhadura; cura com cantos e detalhes do estojo; projétil com ranhuras do estojo.
- Gravações reduzem a opacidade dentro da silhueta, mantendo o desenho legível. Textura de pigmento discreta no Sombria; o Simples mantém seus ícones limpos e sua paleta.
- Prévia anterior preservada. Nova composição: `Art/HudSombria/hud-sombria-composicao-v2.png`. Detalhes ampliados: `Art/HudSombria/hud-sombria-detalhes-v2.png`.

As prévias ilustram geometria, paleta e intenção artística. O cálculo do ruído na prévia Python aproxima o acabamento procedural da Unity; não constitui prova da renderização nativa. Não foram executados testes ou builds.

## Correção das bordas após a captura ingame

Na captura dos botões pequenos, os contornos perdiam definição. A textura fixa de 512 pixels tinha aro secundário com 5 pixels: em um botão de 50 pixels, a espessura resultava em aproximadamente 0,49 pixel. A redução com filtro bilinear e as abrasões podiam deixar o contorno pontilhado.

`HudArt.Circle` agora usa o tamanho efetivo após `GUI.matrix`, gera uma textura adequada à escala de desenho e mantém aro secundário de 1,35 pixel e aro de disparo de 1,85 pixel. O desgaste afeta discretamente o pigmento, sem deformar o contorno. O cache de aros é limitado a 32 entradas. Posições, alvos de toque e legendas foram preservados.

Verificação focada executada na Unity 6000.0.84f1 com dispositivo gráfico: `HudBorderChecks.Run` gerou as texturas reais e as reduziu na GPU para 32, 50 e 78 pixels. Os 64 pontos angulares de cada tamanho mantiveram luminância visível acima do limite de 0,12; menor valor observado: 0,1298437. Compilação e execução concluídas sem erro, código de saída 0.

Evidências: `Logs/hud-border-check.json` e `Logs/hud-border-unity.log`. Esse resultado valida a continuidade dos aros após redução gráfica; não valida uma partida completa, contraste sobre todos os cenários ou Android.

## Layout único e organização pelo usuário

- Removidas as posições exclusivas do PC: arma/munição fica acima das ações, junto à mira, e o disparo está visível no PC também. Movimento, mira, quatro ações e disparo compartilham a disposição do celular. Atalhos de teclado continuam funcionando; os rótulos principais são os mesmos.
- Rotação dos ícones passa a compor translação/rotação na matriz local, mantendo o centro sob a escala da área segura. A seta de extração usa suavização angular de 0,16 segundo em `Update`, em vez de atualizar seu movimento a cada evento `OnGUI`.
- Interação contextual tem posição padrão ao centro inferior, 14 unidades acima da borda segura. Pode ser reposicionada no editor pelo usuário.
- `HudLayoutEditor` mostra todos os elementos, mesmo status inativos e botões que não estariam habilitados. A prévia tem conteúdo ilustrativo para organizar o layout sem executar ações do jogo.
- Arrastar seleciona e move um elemento; ‹/› selecionam elementos sobrepostos; o controle de tamanho varia de 60% a 200%. Botões circulares mantêm a proporção. Texto e arte acompanham o redimensionamento dos painéis principais.
- Ocultar ajustes libera a área central para posicionar elementos. Mostrar ajustes recupera o painel. Restaurar este elemento afeta apenas a seleção; Padrão restaura a composição inicial; Cancelar/Voltar/Esc desfazem a sessão; Salvar persiste o layout em `PlayerPrefs`, chave `hud_layout_v1`.
- No Editor da Unity, Salvar também exporta `Logs/hud-layout-custom.json`. Esse arquivo registra o layout escolhido para uma próxima incorporação como padrão distribuído no jogo. Salvar no editor de HUD ainda não altera o padrão de todos os aparelhos.
- O editor bloqueia comandos locais durante a organização. A partida online continua. Não altera regras de servidor, prefabs ou a cena salva.

Revisão estática desta alteração; os resultados do teste anterior de bordas não validam o novo editor de organização ou o novo layout. Sem nova sessão Unity, build ou validação em aparelho nesta entrega.
