# Ruínas, visão e feedback de dano

## Alterações implementadas

- Visão de lanterna: alcance 16, abertura 140 graus, percepção próxima 2.4 e câmera da expedição com semialtura 15. Bordas continuam graduais. Valores iniciais para avaliação prática, não equivalência exata de escala com a composição HTML.
- Apenas paredes, portas em posição de bloqueio e armários grandes ocultam a visão. Árvores, caixas, cobertura baixa, mesas, camas, bancadas e armários pequenos preservam colisão física, mas não ocultam a visão.
- A malha de escuridão para na primeira superfície opaca. Cópias visuais somente das superfícies atingidas pelos raios são apresentadas acima da escuridão, permitindo ver a parede/armário sem revelar jogadores, loot ou terreno atrás. A apresentação revela o objeto atingido inteiro, não recorta sua textura pixel a pixel. Objetos não atingidos continuam sujeitos à escuridão.
- Armários grandes e pequenos das ruínas passam a ser recipientes de rede usando LootPickup: abertura, saque e fechaduras existentes. Chance inicial de estarem trancados: 30%. Conteúdo usa a mesma regra de baús (85% de presença de loot, permitindo recipientes vazios). Não são substituídos por imitadores.
- Armário grande recebe arte própria fechada/aberta em `Resources/Environment/WardrobeStates.png`; a abertura não remove o corpo do móvel nem seu bloqueio de visão. Armário pequeno mantém a arte existente, com indicação visual de abertura.
- Colisão desses móveis permanece no mapa mesmo quando seu recipiente de rede não está sendo observado. FurnitureObstacle liga a geometria à apresentação do recipiente quando ele entra na observação, sem duplicar o colisor das ruínas.
- Rotação dos móveis varia deterministicamente com posição/semente: camas horizontais/verticais, mesas e bancadas com posições horizontais, verticais ou diagonais. Armários variam orientação sem mudar a posição do corpo.
- Um dos quatro modelos locais de ruína tem perímetro octogonal, aproximando uma casa arredondada; demais modelos preservados. Essa alteração local não implementa ainda a reorganização macro do mapa grande.
- Health instala DamageFlash para jogadores, bots e monstros com vida. Redução da vida replicada aciona sobreposição vermelha que desaparece em 0.24 s. Cura não aciona flash; morte e cores originais não são sobrescritas. Efeito permanece subordinado à visibilidade do personagem.

## Verificação

Revisão estática de máscaras, geração determinística, ciclo de vida dos eventos e ligação com o saque existente. `git diff --check` não apontou problemas de espaço nas alterações revisadas. Não foram executados Unity, testes automatizados ou builds; alinhamento da nova arte, bordas da iluminação, colisões dos móveis rotacionados e comportamento em clientes separados ainda precisam da avaliação prática do usuário.

Usar cliente e servidor atualizados juntos, pois a geração dos obstáculos e as regras de percepção mudaram. Não foram reconstruídos cenas/prefabs por MvpSetup.Build.
