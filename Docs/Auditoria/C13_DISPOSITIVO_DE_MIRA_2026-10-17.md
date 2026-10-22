# C13 — Prioridade do dispositivo de mira

Implementada e revisada estaticamente em 17/10/2026. **Sem compilação, testes, Unity ou builds.** Referência E3-01; verificação prática pendente da T9.

## Causa

`PlayerController.ReadInput` aplicava o analógico e depois sempre recalculava a direção pelo cursor fora do HUD. Um mouse parado continuava sobrescrevendo o controle. A posição desse cursor também participava do bloqueio do disparo sem distinguir sua origem.

## Correção

- `Assets/Scripts/Players/PlayerController.cs`: mantém o dispositivo de mira ativo. Deflexão do analógico acima do limiar existente de 0,25, alteração acumulada do vetor superior a 0,02 ou pressão válida do gatilho assumem a mira pelo controle. Mouse parado não a sobrescreve; soltar o analógico conserva a direção.
- Movimento acumulado de mouse de pelo menos um pixel fora do HUD, ou clique de disparo fora dele, retoma a mira pelo cursor. Movimentos pequenos são acumulados em vez de descartados a cada frame. Quando o mouse é a fonte ativa, a direção continua acompanhando o ponto do cursor no mundo, inclusive ao mover personagem/câmera.
- Em atividade simultânea no mesmo frame, clique de mouse válido tem precedência; depois atividade do analógico/gatilho; depois movimento de mouse. Dispositivo conectado ou valor estacionário, isoladamente, não retomam a prioridade.
- Toque conserva o caminho existente: joystick de mira, orientação por movimento quando permitida e disparo separado. Ao sair do toque, cursor parado não substitui sua direção; atividade real de mouse/controle pode assumir novamente.
- Bloqueio de gameplay/foco invalida a amostra anterior, evitando interpretar deslocamentos do cursor feitos no menu como movimento novo no primeiro frame de retorno. Controle já defletido pode assumir no retorno. Estado de seleção é reiniciado no spawn; troca/conexão de dispositivos não compara coordenadas de dispositivos diferentes.
- `Assets/Scripts/Players/GameInput.cs`: expõe o estado habilitado do mapa de gameplay e a origem de disparo informada por `fire.activeControl`. Disparo resolvido para gamepad não é bloqueado pela posição do cursor no HUD. O disparo de mouse continua respeitando o HUD e a exceção existente do botão visual. Mantida a supressão de disparo já existente ao reativar gameplay.

Não foram alterados movimento, stamina, dispersão, atributos, autoridade do servidor, bindings, protocolo ou assets. Aplicadas as skills de C# e Input System, adaptadas à Unity 6000.0.84f1 e Input System 1.20.1 instalados. Dispositivos simultâneos usam a resolução existente da ação de disparo; esta etapa não introduz controle local para vários jogadores.

## Revisão e pendências

Revisados estaticamente o caminho de mouse/controle, toque, bloqueio de interface, foco, spawn e origem da ação Fire. Conferidas as APIs no fonte local do Input System. Nenhuma observação em jogo foi feita.

Na T9, verificar mouse conectado/parado com analógico em uso; soltar analógico; mover mouse lentamente e clicar para retomar; alternar novamente ao controle; disparar com gatilho enquanto o cursor está sobre HUD; preservar bloqueio do clique no HUD e botão visual. Conferir drift abaixo dos limiares, atividade simultânea, dispositivo desconectado/reconectado, menu/foco e retorno sem disparo involuntário. Conferir toque e a transição posterior para mouse/controle, inclusive usando movimento para orientar a visão. Aceite funcional continua pendente.
