# Refinamento após a migração para uGUI

A captura da seleção de ambiente mostrou uma regressão em relação a `ExpeditionMenu.OnGUI`: dois cartões com descrições haviam sido substituídos por duas linhas de botões. A composição anterior foi restaurada no prefab existente, com dois cartões editáveis, títulos, descrições e ações independentes. Os cartões são desenhados antes dos botões para não encobrir o desenho nem interceptar seus cliques.

O presenter ajusta a largura dos cartões à área segura. Títulos, descrições e ações recuperam a hierarquia de tamanhos da interface anterior. A tela da Floresta Velha mantém imagem à esquerda e preparação à direita; preparar mochila e partir voltaram a ficar lado a lado. Os controles de teste permanecem exclusivos do contexto de teste.

`HudTheme.Surface` compartilha o acabamento procedural anterior com os frames uGUI: textura discreta, cores distintas para botões e painéis, bordas finas e cantos próprios de cada tema. As quatro superfícies são armazenadas em cache por tema/função. As bordas circulares continuam usando a geometria nativa existente. Não houve alteração de layout personalizado, tamanho de inventário ou regras de jogo.

Verificação desta revisão: inspeção estática das referências e relações de hierarquia dos dez prefabs, incluindo as referências dos cartões e sua ordem de desenho. Não houve abertura de sessão Unity, build ou validação visual em execução. Conferir primeiro a seleção de ambiente, depois a preparação normal/de teste e os temas Sombria/Simples no jogo.
