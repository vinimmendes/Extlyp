# Mensagem TransportShutdown — 11/10/2026

## Complemento após novo stack trace

O usuário relatou um aviso vindo diretamente de `NetworkConnectionManager.Shutdown` → `UnityTransport.DisconnectLocalClient` → callback `Disconnected`. A proteção anterior cobria somente chamadas feitas pela coroutine do bootstrap; o encerramento interno do NGO podia escapar dela.

Agora a callback também reconhece o evento tipado `NetworkTransport.DisconnectEvents.TransportShutdown`. Essa notificação de limpeza não gera novo aviso de falha nem sobrescreve um motivo real registrado antes. Eventos de fechamento remoto e recusas continuam visíveis. Isso cobre também encerramentos externos ao bootstrap, como saída do Play e descarregamento de cena.

Verificação atual na cópia isolada Unity 6000.0.84f1: **18 casos passaram**, incluindo shutdown interno com a flag do bootstrap desativada, falha remota real e shutdown posterior preservando a causa anterior. Log: `Logs/skills-implementation/SkillsValidation-NetworkTransitionChecks-Run-20261011-142855.log`. Sem sockets, clientes ou reinício das salas. O stack trace de shutdown não identifica por si só por que a sessão decidiu encerrar; não se apresenta esta correção como reprodução da conexão completa.

O levantamento e a primeira rodada de 12 verificações abaixo ficam como histórico.

Relato: captura do painel de conexão exibindo `TransportShutdown` / `NetworkConnectionManager was shutdown`, com parte do texto cortada.

## Evidências

- A captura confirma o erro técnico bruto e a mensagem cortada, mas não identifica sozinha o motivo da tentativa original ter terminado.
- Inspeção local: três processos do servidor ativos e portas UDP 7777, 7778 e 7779 abertas, confirmadas com consulta de processos/netstat. Não houve reinício das salas.
- NGO 2.13.3 local marca o transporte como `TransportShutdown` ao encerrar `NetworkManager`. A callback de desconexão também é usada no encerramento iniciado pelo próprio cliente.
- O bootstrap anterior escrevia toda callback local como falha; os encerramentos usados para trocar de modo/sala não eram distinguidos. A interface mostrava o motivo sem tratamento em uma área fixa de 60 unidades, com alinhamento vertical central.

## Correção

- `StopClientConnection` marca apenas os encerramentos intencionais até o fim do shutdown. Esses encerramentos limpam a sessão sem sobrescrever mensagem/motivo de erro ou solicitar recarregamento da cena.
- Troca de modo e de cabana para expedição mantém um estado de transição, bloqueando cliques concorrentes e recarregamento automático enquanto a troca ocorre.
- Falhas inesperadas preservam seu motivo original para a busca de salas e seguem visíveis; diagnóstico bruto também é registrado no log.
- `FriendlyReason` traduz TransportShutdown para uma mensagem curta e preserva as recusas estruturadas do servidor. Aplica-se também à mensagem antiga salva em PlayerPrefs.
- Área da mensagem alinhada no topo, fonte 17 e rolagem para textos maiores. Mesmos temas e coordenadas seguras existentes; sem novos controles de desenvolvimento em partidas.

## Verificação direcionada

Compilação e `NetworkTransitionChecks.Run` concluídos na Unity 6000.0.84f1 em cópia isolada: **12 verificações passaram**. Cobrem mensagem vazia/nula, tradução, motivos estruturados do servidor, callback de shutdown intencional e callback de falha real com o motivo consumido do connection manager do NGO.

Log atual: `Logs/skills-implementation/SkillsValidation-NetworkTransitionChecks-Run-20261011-131715.log`.

Não foram iniciados sockets/clientes, reiniciados servidores, feitos builds de entrega ou executadas suites completas. O teste direcionado não reproduz a entrada completa na partida nem confirma graficamente a rolagem. A causa operacional específica da tentativa da captura não foi reproduzida; a correção trata o manejo de shutdown e a apresentação comprovados no código/captura. A entrada na partida deve ser conferida no Play atualizado.
