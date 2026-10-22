# Stagger de movimentação — 12/10/2026

Impactos que realmente reduzem vida pausam a movimentação comum dos monstros por 0,1 s, controlados no servidor por Health.OnDamagedServer. Dano ignorado por invulnerabilidade, dano zero e golpe fatal não iniciam stagger. Impactos renovam o prazo para agora + 0,1, sem somar duração por pellet.

Somente MoveBehaviour recebe a pausa; FixedUpdate, preparação, golpes em andamento e avanço continuam. Investida ativa (ChargeState=2) mantém velocidade e duração. Nenhum temporizador, alvo ou estado de habilidade é alterado pelo stagger. A pausa não conta como falta de progresso na navegação.

Minions da planta também recebem pausa somente na movimentação; crescimento/cura da mãe/bulbos e ataques mantêm suas regras. As interrupções por dano já existentes para Herald/Devourer são regras anteriores independentes, não foram adicionadas ou removidas pelo stagger.

Sem variáveis novas de rede; os clientes observam a posição replicada. Protocolo 17 preservado. Revisão estática e git diff --check; sem testes Unity ou builds, conforme preferência do usuário.
