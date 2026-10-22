# Preferência de mira no toque — 22/10/2026

Menu > Opções > Jogabilidade oferece **Mira ao andar**, nos dois temas e nos menus uGUI e de fallback.

- **Acompanhar movimento**: padrão original para aparelhos sem preferência. Sem gesto de mira e sem disparo mantido, andar orienta o personagem na direção do movimento.
- **Manter direção**: andar e soltar o controle direito preservam a última direção escolhida na mira.

O gesto no controle de mira continua tendo prioridade nos dois modos. O disparo permanece separado. A escolha tem efeito ao voltar ao jogo e é salva por aparelho em PlayerPrefs (`touch_aim_follow_movement_v1`), sem alterar mouse, controle físico, layout personalizado, temas ou autoridade do servidor.

A ajuda descreve o modo selecionado. As opções são separadas em Jogabilidade e Gráficos, com navegação por categorias. Jogabilidade usa duas colunas no modo compacto, com Voltar em uma linha própria. O prefab existente dispõe de oito botões e atende todas as páginas sem cópias adicionais. Navegação direcional acompanha a disposição de cada página.

## Trocar Mira e Atirar

Opção independente de Mira ao andar, desativada por padrão e salva por aparelho em `touch_swap_aim_fire_v1`. Ativar troca as áreas completas dos dois controles (posição, tamanho e alvo de toque), inclusive em layouts personalizados e na prévia do PC. A mira assume a área de disparo; o disparo assume a área de mira. Ícones e funções acompanham seus controles.

A troca é aplicada na consulta da geometria, sem regravar os perfis. Desativar recupera a disposição anterior. Arrastar, redimensionar, restaurar um elemento, salvar e cancelar no organizador trabalham com a área exibida; carregar perfis mantém a identificação original das áreas para evitar inversão duplicada. Os temas usam a mesma geometria.

Conferir na avaliação prática: alternar a troca nos dois temas, tocar e arrastar nos locais novos, manter movimento e mira simultâneos, comparar com Mira ao andar nos dois modos, reiniciar o aplicativo e editar/salvar/cancelar um perfil com a troca ativada. Sem novo APK ou execução nesta entrega.

## Tiro automático no toque

Opção independente, desativada por padrão e salva por aparelho em `touch_auto_fire_v1`. Quando ativada, solicita um tiro comum se um alvo vivo e visível interceptar a linha central da mira dentro do alcance da arma. Funciona com a direção mantida, sem exigir gesto contínuo; não gira o personagem nem busca alvos fora da linha. Usa as regras de visão existentes e ignora monstros disfarçados/ocultos.

Somente armas de projéteis carregadas. Corpo a corpo permanece manual; recarga automática tem preferência independente. O disparo manual segue disponível. Menus, morte, pesca, abertura de fechadura, ações de dungeon, troca de arma/equipamento, recarga e ferrolho impedem a solicitação automática. A primeira superfície sólida/quebrável bloqueia a seleção de alvos atrás dela; o trecho até a boca da arma também é conferido.

A decisão é local ao dono em controles de toque e usa `RequestFire` existente. Dano, munição, dispersão e cadência continuam no servidor, sem novo RPC ou protocolo. Consulta física limitada a cada 80 ms quando a cadência permite; buffer reutilizado de 64 impactos, com recusa conservadora se lotar. A seleção verifica o impacto mais próximo sem depender da ordem dos resultados. API conferida na documentação XML instalada da Unity 6000.0.84f1 (`Physics2D.Raycast` com `ContactFilter2D` e array de resultados).

Revisão estática apenas. Avaliar no aparelho: ativado/desativado, alvo entrando/saindo da linha com mira mantida, obstáculo alto/baixo e quebrável, alvo morto/disfarçado, alcance, munição vazia, recarga, ferrolho, troca de arma, menu, corrida e persistência após reiniciar. Comparar layouts e temas. Sem testes automatizados, Unity ou novo APK nesta entrega.

## Mirar em área livre

Opção independente, desativada por padrão e salva por aparelho em `touch_aim_anywhere_v1`. Na expedição, permite iniciar a mira por arraste em qualquer área livre dentro da área segura, à esquerda ou à direita. A origem é o primeiro contato do dedo, mantendo o mesmo comportamento direcional e zona morta do controle dedicado. O botão de mira continua funcionando.

O uGUI mantém a leitura dos botões por ponteiro. Somente toques iniciados fora da interface e dos controles de movimento/mira/disparo/corrida podem iniciar o novo gesto. IDs do Touchscreen ficam separados dos IDs do EventSystem. O primeiro dedo de mira conserva a função até soltar, cancelar ou perder foco; atravessar um botão não transfere a função. Menu, inventário, mapa, organizador e demais bloqueios de gameplay cancelam o gesto, sem reaproveitar um dedo que já estava pressionado ao voltar. Caminhada na cabana e controles de mouse/gamepad permanecem no fluxo existente.

Compatível com os dois temas, troca de Mira/Atirar, layouts personalizados, Mira ao andar e Tiro automático no toque. A ajuda acompanha a preferência. Na categoria Jogabilidade, controles compactos continuam com alvos de 64 unidades e Voltar cabe dentro do painel.

Revisão estática do fluxo de captura/soltura e da geometria dos menus. Sem testes, sessão Unity ou APK novo nesta alteração. Avaliar no celular: arraste livre em ambos os lados, movimento e mira com dois dedos, toques em cada botão, gesto atravessando controles, dois dedos tentando mirar, soltura/cancelamento, menu/foco, reinício do aplicativo e ambas as orientações horizontais.

## Recarga automática no toque

Opção independente, desativada por padrão e salva por aparelho em `touch_auto_reload_v1`. Quando ativada, solicita recarga assim que o carregador da arma equipada estiver vazio e existir munição compatível na mochila, sem exigir alvo na mira ou toque em Atirar/Recarregar. Não completa carregadores parcialmente usados. Funciona também com Tiro automático desativado; recarga manual continua disponível para antecipar a recarga.

Apenas o dono em controles de toque solicita a ação. Menus, perda de foco, morte, pesca, fechaduras, ações de dungeon, troca de arma/equipamento, armas corpo a corpo, recarga e ferrolho impedem novas solicitações. Intervalo mínimo de 0,5 s entre pedidos automáticos evita envio por frame enquanto a resposta do servidor chega. Usa `RequestReload`/`ServerReload` existentes: duração, consumo, capacidade, identidade da arma e interrupção de corrida continuam sob as regras atuais do servidor. O comportamento anterior de solicitar recarga ao apertar Atirar com a arma vazia é preservado, inclusive com a nova preferência desligada.

A ajuda acompanha a preferência. A categoria Jogabilidade mostra as cinco preferências de toque, Organizar botões e tamanhos e Voltar às categorias, sem ultrapassar a área segura.

Revisão estática; sem testes, Unity ou novo APK. Avaliar no aparelho: último tiro com/sem reserva, reserva de tipo errado, carregador parcialmente usado, munição recolhida com arma vazia, ferrolho, troca de arma, menu, morte, recarga manual, Tiro automático ligado/desligado e persistência após reiniciar.

## Categorias do menu

Opções abre a seleção entre **Jogabilidade** e **Gráficos**.

- Jogabilidade: Mira ao andar, Trocar Mira e Atirar, Tiro automático no toque, Mirar em área livre, Recarga automática no toque e Organizar botões e tamanhos.
- Gráficos: temas Simples/Sombria e aparência Suave/Pixelado do cenário.

Voltar às categorias e o comando Voltar do Android/teclado/controle retornam primeiro à seleção de categorias; daí, Voltar retorna ao menu principal. Todas as chaves de preferências são preservadas. A organização é compartilhada pelos temas e pelos menus uGUI/fallback.

Revisão estática de contagem de botões, foco, navegação, retorno e limites dos painéis. Sem testes, Unity ou novo APK. Conferir no aparelho: ambas as categorias, alteração de cada preferência, voltar por botão e Android, navegação por teclado/controle e ambas as orientações horizontais.

Revisão estática do fluxo e geometria; sem testes automatizados, sessão Unity ou novo APK. Na avaliação prática, comparar ambos os modos andando, mirando, soltando a mira e atirando; reabrir o aplicativo para conferir persistência e comparar Simples/Sombria nas duas orientações horizontais.
