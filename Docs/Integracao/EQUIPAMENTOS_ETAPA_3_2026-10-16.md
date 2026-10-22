# Equipamentos — etapa 3: stamina e corrida

Implementada em 16/10/2026. Revisão estática; validação prática pendente. Unity 6000.0.84f1, NGO 2.13.3 e Transport 2.7.4 preservados.

## Regras

| Parâmetro | Valor inicial |
| --- | --- |
| Caminhada | 4 unidades/s |
| Corrida | 5,4 unidades/s |
| Capacidade | 100 stamina |
| Consumo de corrida | 20/s |
| Mínimo para começar | 10 |
| Regeneração | 15/s |
| Espera após gasto | 1,5 s |
| Bem alimentado | +15% na regeneração (17,25/s) |

Correr exige movimento. Ao esgotar, volta a caminhar; soltar e segurar o comando novamente permite reiniciar quando houver pelo menos 10 stamina. Uma corrida já iniciada continua abaixo de 10 até esgotar ou ser interrompida. Novo início de expedição restaura o recurso. Lentidão existente multiplica a velocidade de caminhada e corrida igualmente.

Stamina, capacidade e velocidade são decididas pelo servidor. O cliente envia movimento, direção e intenção de correr; não envia valores de stamina ou velocidade. Sequência de entrada e timeout de 0,5 s são preservados. Entrada local e entrada recebida pelo servidor ficam separadas, inclusive no host.

Disparo encerra a corrida antes de resolver o ataque e impede retomada durante o cooldown. Recarga também encerra e bloqueia a corrida. Ferrolho, troca de arma/equipamento, extração, fechadura, pesca e mecanismo bloqueiam a corrida; morte e encerramento da partida interrompem movimento e gasto. Menus e perda de foco limpam a intenção local, com o timeout como proteção contra perda dos pacotes de soltura.

`ServerSpendStamina` disponibiliza gasto autoritativo para a etapa 4, recusando custos inválidos/insuficientes sem consumo. O período de preparação/recuperação informado bloqueia corrida e regeneração. Melee e efeitos de Vigor continuam nas etapas 4 e 5. Bem alimentado usa o bônus real recebido no início da expedição, sem inferir comida de um futuro aumento de vida por Vitalidade.

## Controles e apresentação

- PC: segurar Shift esquerdo/direito. Controle: segurar o clique do analógico esquerdo.
- Celular: botão CORRER independente, segurado junto do joystick. Não exige clicar repetidamente.
- Barra STAMINA no alto à esquerda, abaixo dos status; valor atual/máximo e preenchimento. Botão à direita do joystick, com símbolo original de corredor e destaque durante corrida.
- Ambos são elementos autorados no prefab `Hud`, personalizáveis por posição e tamanho no organizador, com os mesmos controles nos temas Simples e Sombria e área segura existente.
- Os IDs anteriores do enum e as 16 posições existentes são preservados; layouts locais antigos recebem os dois novos padrões por fallback, sem apagar personalizações.
- A animação existente avança pela distância realmente percorrida, portanto acelera com a corrida e acompanha a lentidão sem introduzir um ciclo por tempo separado.

## Compatibilidade e revisão

Protocolo **23 → 24** pelo novo estado de rede e parâmetro de corrida na entrada. Cliente e servidor precisam da mesma versão. Nenhum servidor foi reiniciado nem build criado.

Revisados estaticamente autoridade, limiares, recuperação, host/cliente, interação com disparo/recarga, captura/soltura do botão, campos do prefab e preservação do layout. Nenhum teste automatizado, compilação manual, sessão Unity ou build de validação executado, conforme AGENTS.md. Sem evidência de execução nesta entrega.

Na avaliação prática: segurar corrida até esgotar, recuperar e reiniciar; disparar/recarregar correndo; abrir menus e alternar foco; conferir host e cliente separado e multitouch nos dois temas. Ruído extra de corrida e rebalanceamento dos monstros permanecem fora desta etapa.
