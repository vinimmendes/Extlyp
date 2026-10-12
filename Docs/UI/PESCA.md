# Pesca — movimento e apresentacao

Atualizacao em 2026-10-11: pesca exclusiva da cabana. A acao, interface e recompensas de pesca na expedicao foram desativadas; os lagos do mapa permanecem ambientais.

O painel da base e da expedicao compartilha `MinigameView.FishingPanel` e uma instancia de `FishingPresentation` por tela. Mantem os temas A/Simples e B/Sombria de `HudTheme`, area segura de `UiLayout` e uso horizontal no celular.

Referencia de composicao: [pesca de Stardew Valley](https://wiki.stardewvalley.net/Fishing): trilho vertical, peixe dentro da faixa controlavel e medidor de captura ao lado. As formas, paletas e silhueta usadas aqui sao proprias do projeto. Nao foram importados sprites nem alteradas as regras de recompensa/dificuldade para copiar a referencia.

## Comportamento

- Mouse/toque sao capturados no inicio do pressionamento dentro do mesmo retangulo usado para desenhar o botao. Arrastar para fora preserva o gesto ate soltar. Teclado e controle reutilizam `GameInput.Hold`.
- Pressionar/soltar envia imediatamente no Update; um refresh de 200 ms mantem o comando vivo. Abrir menus ou perder foco solta o comando. Continua existindo o timeout de entrada do servidor.
- A base avanca apenas a simulacao da pesca a cada Update do servidor. Publicacao periodica e acesso ao perfil continuam separados, a cada 100 ms, com envio adicional em transicoes de fase.
- A apresentacao avanca uma vez por quadro. Peixe e progresso convergem suavemente para os dados recebidos. A barra usa a mesma funcao de movimento do servidor com antecipacao local limitada e correcao suave; nao ha rollback nem replay completo de entradas.
- A antecipacao e limitada a 0,16 do trilho em relacao ao ultimo estado recebido. Depois de 350 ms sem mudanca dos valores observados, a barra converge para o ultimo estado. Isso e uma protecao visual, nao um detector de desconexao.
- Progresso, fisgada, especie, falha e recompensa continuam autoritativos. O estado visual nunca completa uma captura.
- A interface informa dentro/fora da faixa tambem por texto. A boia afunda com transicao curta; o sinal sonoro existente continua na fisgada.

## Compatibilidade e verificacao

A expedicao adiciona `FishingVelocity` ao estado de rede e a base adiciona `BarVelocity` ao snapshot. Atualizar cliente e servidor juntos; nao misturar executaveis antigos e novos.

Foi feita compilacao C# com Roslyn e referencias do projeto Unity 6000.0.84f1, sem erros. Isso nao executa os geradores de RPC nem substitui a compilacao/importacao da Unity.

`Tools/Audit/Run-FishingProbe.ps1` executa as classes reais `FishingGame` e `FishingPresentation` com adaptadores de matematica/input fora da engine. As 20 verificacoes cobrem edges de entrada, refresh, bloqueio por menu, movimento antecipado e estagnação a 30/60/120 passos por segundo, limites e janela de fisgada. Esses passos nao sao medicoes de FPS.

Ainda pendente em execucao Unity/aparelho: sensacao com latencia real, multitouch, perda de foco, cancelamento/reinicio, comportamento dos RPCs e inspecao visual dos dois temas em telas horizontais. Nenhum APK/player foi gerado e nenhum ganho de desempenho foi medido.
