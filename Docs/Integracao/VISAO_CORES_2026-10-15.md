# Campo de visão revela as cores — 15/10/2026

Implementado a pedido do usuário: durante a expedição, o cenário fora da visão local fica em preto e branco e mantém o escurecimento existente. O campo de visão revela as cores originais, com transição gradual nas bordas e oclusão pelas paredes. Alvos ocultos continuam seguindo as regras existentes; o efeito não revela inimigos escondidos.

O Renderer2D captura a camada de ordenação Default, em resolução integral, por Camera Sorting Layer Texture. A nova camada de ordenação VisionOverlay é desenhada depois da captura: `VisionSurface` combina a cor original e a luminância usando a mesma máscara angular e de obstáculos do fog. As linhas de mira são desenhadas após a composição. Não há cópia dos sprites ou modificação das texturas originais. A captura adiciona uma textura intermediária e uma cópia por câmera que usa esse renderizador; impacto em aparelhos ainda não medido.

A HUD em Canvas Overlay e o minimapa mantêm suas cores. Fora da partida, a camada de visão fica desativada conforme o ciclo existente. Não altera os limites de visão autoritativos do servidor nem as regras de combate. A pendência de serrilhado residual da sombra continua aberta; esta alteração trata da apresentação de cores.

Revisão estática do código e dos caminhos Camera Sorting Layer Texture da URP 17.0.4 instalada. Sem testes automatizados, execução Unity, renders de validação ou builds, conforme preferência do projeto. Na avaliação prática, conferir cores dentro da visão, monocromia fora e atrás das paredes, alinhamento da captura ao caminhar/girar, HUD colorida e retorno à base. Os checks gráficos anteriores de fog isolado não foram executados e precisam fornecer a captura de cenário e a ordenação VisionOverlay antes de validar esta composição.

## Teste solicitado pelo usuário — 15/10/2026

Executado Play Mode na Unity 6000.0.84f1, Direct3D 11, AMD Radeon Graphics. Host local de teste em loopback, cena Game real, Catacumbas e HUD uGUI. O probe editor-only `VisionGameplayCapture.Run` inicia uma sessão sem conta persistente e captura a visão apontada para leste e norte. Movimento por entrada e IA dos monstros foram desativados durante as capturas para manter o enquadramento; este teste não valida combate, cliente remoto ou desempenho.

A primeira captura revelou distorção vertical: a transformação de UV de fullscreen dependia de `_ScaleBiasRt`, que não estava inicializada nesse caminho do Renderer2D. Corrigido para coordenadas de raster normalizadas por `_ScreenParams`. Novo teste gerou cenário nítido, cores dentro do campo de visão, monocromia fora dele e HUD/minimapa coloridos nas duas direções. A compilação também identificou e permitiu corrigir o nome local `client` duplicado na alteração anterior de spawns do laboratório.

Capturas reais, sem edição:

- [Visão para leste](../Auditoria/Evidencias/visao-cores-leste-2026-10-15.png).
- [Visão para norte](../Auditoria/Evidencias/visao-cores-norte-2026-10-15.png).

Log: `Logs/vision-gameplay-editor.log`, marcador `VISION GAMEPLAY CAPTURE COMPLETE`. Não foi gerado build de jogador. A partida de teste foi encerrada ao concluir a captura. Serrilhado residual e texto persistente de soltar corrente continuam como pendências separadas.
