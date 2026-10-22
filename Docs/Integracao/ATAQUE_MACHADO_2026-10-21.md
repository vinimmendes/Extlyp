# Ataque de machado — 21/10/2026

Implementado golpe diagonal descendente de duas mãos no personagem rústico atual,
com 26 quadros de tronco/braços, pernas independentes e arma separada.
Entrega e instruções de produção: `Prototypes/PlayerWalk3D/AxeAttack/README.md`.

## Integração

- `PlayerMeleeActionArt` substitui `PlayerSwordArt`, preservando seu GUID e os atlas/metadados
  das espadas. Cada ação declara atlas, metadados, quantidade de quadros e índice de impacto.
- Espadas mantêm 20 quadros e impacto no quadro 9. Machado usa 26 quadros e impacto no 11.
  Metadados antigos de espada recebem escala aparente 1, preservando sua transformação anterior.
- `AxeAttack_1.png` e `AxeGrip.json` ficam em `Resources/PlayerCharacter/Actions`.
- `PlayerCharacterVisual` usa o amostrador comum; o tronco do machado fica acima da arma
  para mostrar as mãos envolvendo o cabo. Outras ações mantêm sua ordem de desenho.
- `MeleeWeaponVisual` aplica posição, rotação e escala aparente, com ponto da mão direita
  em 50/128 do cabo e apoio em 22/128, conforme a geometria de produção.
- O servidor aplica caminhada a 60% durante todo o ataque de machado, multiplicando
  os modificadores existentes de equipamento, como já ocorre nas espadas.
- Mantidos preparação de 0,40 s e recuperação de 0,65 s, dano, alcance, stamina,
  setor de impacto, limite de alvos, obstrução por paredes, comandos e bloqueio de corrida.
- Apresentação segue os timestamps replicados existentes, sem RPC novo ou dano por animação.
  Clientes selecionam a fase corrente; cancelamento, morte, troca e fim da partida encerram a ação.
- Materiais, cor, visibilidade e arco do dono continuam vinculados aos sistemas existentes.
  Recursos ausentes permitem usar a apresentação anterior.

## Evidência e avaliação pendente

Poses e composição inspecionadas visualmente; mãos ajustadas para leitura fora da cabeça,
empunhadura em dois pontos do mesmo cabo e retorno à guarda. Revisão estática da integração
e da compatibilidade das espadas. Sem testes automatizados, sessões Unity ou builds,
conforme preferência do projeto; funcionamento no jogo e na rede ainda não validado.

Primeira avaliação pelo usuário: atacar parado e andando, mudar a mira na preparação,
repetir ataques, tentar correr, alternar entre espada e machado após o golpe, observar
outro jogador e conferir a visibilidade na neblina. Verificar redução de movimento,
impacto sincronizado, duas mãos no cabo e continuidade da animação das espadas.

## Revisão artística — recuo e empunhadura

Por solicitação do usuário, mãos deslocadas para baixo no cabo, afastando a mão direita
da lâmina. Preparação agora leva o machado para trás do ombro direito, com giro maior do
tronco e máximo recuo no quadro 8. Renders, atlas, metadados e ponto de empunhadura no
renderer atualizados juntos; tempos e regras mantidos. Prévia: `ataque-machado-recuo.gif`.
Revisão visual da prancha realizada; sem testes Unity ou builds.
