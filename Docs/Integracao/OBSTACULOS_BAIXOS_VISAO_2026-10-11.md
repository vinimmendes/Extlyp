# Obstáculos baixos e visão

Causa identificada por leitura do código: `MapDressing.Furniture` atribuía a camada Walls a todos os móveis, incluindo camas, mesas e bancadas. A mesma máscara era usada pela colisão, pelo desenho da escuridão e pela visibilidade autoritativa.

Camas, mesas e bancadas agora usam a camada 9, LowObstacles. `WallMask` inclui Walls e LowObstacles para preservar colisões, navegação, posicionamento de loot e bloqueio físico de ataques existentes. `VisionMask` inclui somente Walls. Armários e estruturas altas continuam bloqueando visão.

Visão local, observação de rede, percepção visual dos bots e apresentação de efeitos/avisos de monstros ignoram obstáculos baixos. As linhas de mira continuam representando os obstáculos físicos dos ataques. A checagem de visibilidade de fechaduras especiais usa VisionMask; interação com a fechadura mantém sua checagem física.

Revisão estática realizada. Não foram executados Unity, builds ou testes automatizados; o comportamento em execução ainda precisa ser avaliado no jogo.
