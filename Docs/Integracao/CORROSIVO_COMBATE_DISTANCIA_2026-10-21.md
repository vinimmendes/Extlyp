# Corrosivo como inimigo de combate a distância

Implementação de 21/10/2026. Reutiliza o modelo, a bolsa de ácido, os atlas de caminhada/cuspe e os efeitos existentes. Nenhum novo RPC ou estado replicado.

- Procura manter 5–8 unidades do alvo observado; aproxima acima de 8 e recua abaixo de 5.
- Recuo a 75% da velocidade de perseguição, procurando corredor livre à frente ou lateralmente, sem atravessar paredes ou sair do território.
- Se encurralado, pode usar o ataque básico existente ou cuspir, conforme a distância e os intervalos.
- Preparação de 0,8 s, com direção fixada no início. A animação usa a mesma constante do servidor.
- Projétil viaja a 6 unidades/s por até 10 unidades na direção comprometida. Não acompanha o alvo nem termina automaticamente em sua antiga posição.
- Recuperação imóvel de 0,65 s após o disparo; mais 2 s até permitir outra preparação. O dano recebido permanece normal durante as duas fases.
- Colisão varrida do projétil, bloqueio por cobertura, dano direto (10), poça e dano periódico preservados pelo `MonsterWorld`.

Revisão estática e `git diff --check`, sem testes automatizados, sessão Unity ou build. Avaliação prática pendente: observar à distância, desviar durante a preparação, usar cobertura, pressionar com espada e encurralar perto de paredes. Não há comprovação de comportamento em execução nesta entrega.
