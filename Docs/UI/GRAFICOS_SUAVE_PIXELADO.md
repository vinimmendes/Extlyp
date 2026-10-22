# Gráficos do cenário — Suave / Pixelado

Implementação de 19/10/2026 para corrigir o pixelado involuntário do cenário e preservar essa estética como opção local.

## Causa e correção

Na URP 17.0.4 instalada, o caminho RenderGraph do Renderer2D cria CameraSortingLayerTexture usando largura/altura multiplicadas por lightRenderTextureScale. O Renderer2D estava em 0,5. Downsampling.None seleciona filtro Point nessa cópia; VisionSurface usa essa textura para recompor a imagem inteira. Portanto “sem downsampling” não garantia captura na resolução nativa.

Renderer2D agora usa escala 1. Suave, padrão para aparelhos sem preferência, amostra essa captura sem quantização adicional. Pixelado quantiza somente a amostragem da cor em uma grade de aproximadamente metade da largura e altura, gerando blocos de dois pixels. A escolha preserva o preto e branco, o escurecimento, os alcances, os raycasts, a visibilidade de alvos e as regras autoritativas.

Ambos os modos mantêm a captura completa; Pixelado é uma opção estética, não uma promessa de economia de GPU. A escala 1 também amplia os buffers de luz/normal gerenciados por essa versão da URP. Não foi medido desempenho ou memória no aparelho. Não foram alterados os pacotes nem o código da URP em Library.

## Preferência e menu

Menu → Opções · HUD e gráficos → Gráficos do cenário: Suave / Pixelado. Botão alterna imediatamente e salva em PlayerPrefs `scene_graphics_mode_v1`, independente dos temas e layouts da HUD. Texto/menu continuam na resolução própria da interface. Disponível no menu uGUI da base/expedição e no menu legado; foco permanece no seletor ao alternar por controle.

SceneGraphics centraliza a preferência e emite Changed. PlayerVision aplica `_Pixelated` ao material local em OnEnable e nas mudanças, removendo a inscrição em OnDisable. Não há alteração de save, perfil do servidor ou protocolo.

## Verificação

Compilação C# de integração pelo compilador instalado da Unity, usando referências/defines existentes e saída isolada em Logs/scene-graphics-probe: sem erros. Não executa o Editor ou produz um player.

Prova focada `Tools/Audit/SceneGraphicsProbe.cs`, compilando SceneGraphics real com substitutos de PlayerPrefs: passaram padrão Suave, alternância/evento imediato, recusa de modo inválido, ausência de evento redundante, persistência de ambos os modos após recarga e recuperação de preferência inválida. Isso não valida o armazenamento nativo do aparelho.

Revisão estática do shader e das duas composições do menu: cinco ações dentro do painel, botões existentes do prefab suficientes, navegação por teclado/controle preservada. Sem sessão Unity, compilação de shader no Editor, captura A/B ou build do jogo. No aparelho, conferir cena parada/movimento, ambos os modos, ambos os temas e persistência após reabrir o jogo. A resolução limitada das artes originais continua sendo independente da captura do cenário.
