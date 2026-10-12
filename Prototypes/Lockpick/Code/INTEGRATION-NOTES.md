# Logica independente de lockpick

Rascunho C# sem dependencia de Unity. Nao importado no jogo, nao compilado e nao executado nesta etapa.

`LockpickSession` e uma instancia por fechadura no servidor. `LockpickTool` e a referencia canonica da gazua no inventario do jogador. A semente da fechadura e persistida no servidor para nao mudar ao cancelar/reabrir. Um jogador por tentativa; cada tentativa recebe um token e aceita comandos sequenciados. O tempo e a distancia sao valores calculados pelo servidor, nunca argumentos aceitos diretamente de uma mensagem do cliente.

Fluxo futuro do adaptador:

1. Validar ator, estado vivo, partida ativa, distancia, parede, ferramenta e permissao sobre o objeto. Impedir mais de uma tentativa ativa por jogador e reservar a gazua canonica durante a tentativa; a exclusividade desta classe e por fechadura, nao entre todas as fechaduras do mundo.
2. Chamar `TryBegin`, guardar o token no lado do jogador e enviar apenas `ViewFor` ao dono.
3. Ao pressionar/soltar, receber identificador de tentativa e sequencia; o servidor acrescenta seu proprio tempo e a checagem atual de alcance.
4. Chamar `Tick` para avancar e cancelar por dano, deslocamento, distancia ou desconexao.
5. Atualizar a interface com a visao do dono e usar os sinais para som, oscilacao, erro e quebra.
6. Em `Unlocked`, replicar o estado compartilhado da fechadura. Destrancar nao abre automaticamente o inventario nem move a porta.
7. Persistir os erros acumulados e o estado de quebra da gazua canonica; cancelar, acertar ou trocar de fechadura nao restaura seu desgaste. Sortear uma unica vez por erro, no servidor, usando um fluxo aleatorio independente da semente da fechadura. A ferramenta e removida do inventario apenas quando quebrar. Chaves e arrombamento pertencem ao adaptador e ainda nao foram implementados.

Estados terminais privados (cancelamento/quebra) chegam ao jogador pelo sinal. O adaptador deve reter o vinculo entre ator e token ate enviar a notificacao final, pois a sessao ja libera a posse ao emitir o sinal terminal. Um observador sem posse recebe somente o estado publico trancado/destrancado. O adaptador precisa validar as entradas, limitar frequencia, tratar latencia com tolerancia limitada e fazer o commit atomico de gazua/desbloqueio; este rascunho nao constitui um protocolo de rede pronto nem uma garantia contra automacao.

Valores iniciais: 2/3/4 pinos, janela de encaixe de 0,10/0,075/0,055 da profundidade, pressao de 0,28 por segundo, sobrecarga apos 0,30 s alem da janela. Probabilidade de quebra por erro com a mesma gazua: 10%, 25%, 40%, 55%, 70%, 85% e, a partir do setimo, 95%. Valores configuraveis e ainda nao balanceados. Nao ha numero fixo de erros permitido: pode quebrar no primeiro ou sobreviver a varios. Progresso dos pinos e reiniciado no cancelamento; solucao e erros acumulados permanecem. Apenas uma gazua nova comeca sem desgaste.

Nao exibir erros acumulados, erros restantes ou porcentagem de quebra no HUD. `LockpickView` nao transmite contagens nem a probabilidade; esses dados pertencem a ferramenta no servidor. A interface comunica a falha por som e movimento e avisa quando a gazua efetivamente quebrar.

O HTML na pasta `Visual` e uma simulacao local independente para discutir a experiencia. Ele nao executa estas classes e nao valida a logica C#. Sua durabilidade e sua semente sao demonstrativas, sem salvamento ou integracao com inventario/rede.
