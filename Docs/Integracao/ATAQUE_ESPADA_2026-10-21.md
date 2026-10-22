# Ataque de espada do protagonista — 21/10/2026

Implementado corte lateral de uma mão no personagem rústico atual, com 20 quadros de
tronco/braços separados das pernas e arma separada presa à empunhadura exportada do Blender.
Fonte e prévias: `Prototypes/PlayerWalk3D/SwordAttack/README.md`.

## Comportamento

- Espada enferrujada e espada curta compartilham a ação, com seus tempos existentes.
- Caminhada a 60% durante preparação e recuperação, aplicada no servidor e multiplicada
  pelos modificadores de equipamento. Sem deslocamento automático; corrida continua bloqueada.
- Direção ajustável até o impacto e travada na recuperação, conforme regras existentes.
- Apresentação usa `MeleeStartedAt`, `MeleeImpactAt`, `MeleeEndsAt` e `MeleeItem`, sem RPC novo.
  Ao receber um golpe em andamento, o cliente seleciona a fase atual, sem reiniciar do quadro 1.
- Dano, stamina, cooldown, alcance, bloqueio por paredes, limite de alvos, comandos de ataque
  e disponibilidade de itens preservados. O arco de alcance do dono permanece ativo.
- Guarda e ataque herdam material, cor, camada e visibilidade do personagem; encerramento
  e morte removem a ação. Se os recursos faltarem, a apresentação anterior permanece disponível.

## Verificação e avaliação prática pendente

Código revisado estaticamente e poses renderizadas inspecionadas. Sem testes automatizados,
sessões Unity ou builds, conforme preferência do projeto; não validado em execução no jogo.

No primeiro teste do usuário: atacar parado e andando, ajustar a direção na preparação,
repetir golpes, tentar correr, trocar arma após o golpe e observar um segundo jogador.
Conferir empunhadura e visibilidade na neblina, redução de velocidade, impacto e retorno à guarda.
