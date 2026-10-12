# Chance de baús imitadores

Relato do usuário: todos os baús encontrados pareciam ser imitadores; foi solicitada uma chance de aproximadamente 10%.

Evidência estática: a geração de baús no mapa grande substituía todo quarto ponto (`i % 4 == 0`) por um imitador. Isso fixava os encontros nas mesmas posições, inclusive no primeiro ponto, com aproximadamente 25% dos pontos selecionados. Não foi reproduzida uma taxa global de 100%.

Alteração: cada ponto natural de baú, nos mapas Teste e Grande, recebe um sorteio independente por partida no servidor. `GameplayDefinition.MimicChestChancePercent` e `Assets/Resources/Gameplay.asset` definem 10%. Sorteio inteiro de 0 a 99: valores 0–9 selecionam imitador; demais valores geram baú normal. Não há quantidade mínima nem proporção exata garantida em cada partida. A seleção não se repete ao tentar abrir o baú.

Se não houver prefab de monstro ou não houver posição segura para criá-lo, gera-se um baú normal. Baús de treino e imitadores criados explicitamente pelo laboratório mantêm seu comportamento; o baú seguro da base também permanece normal.

Revisão estática dos caminhos de geração, limites 0–100 e retorno de sucesso da criação do monstro. Não foram executados testes automatizados, sessões Unity ou builds nesta alteração.
