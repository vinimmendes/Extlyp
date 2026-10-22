# Animações Blender — integração em 12/10/2026

O usuário autorizou implementar todas as animações antes pendentes ao concluir as quatro trocas adicionais. Integração feita no código e nos recursos do projeto; ainda sem validação prática em Unity. Nenhum teste automatizado, sessão Unity ou build iniciado para validar. Nenhum servidor existente reiniciado.

## Protagonista e bots

- Seis direções de troca entre pistola, shotgun e rifle: preserva pistola → shotgun e acrescenta pistola → rifle, shotgun → pistola, rifle → pistola, shotgun → rifle e rifle → shotgun.
- Recargas de pistola, shotgun e rifle, sincronizadas com ReloadEndsAt e a duração atual da arma. Não acrescentam atrasos de combate nem mudam consumo de munição.
- Shotgun usa variante visual de um cartucho quando a quantidade reposta é um. Rifle usa o pente com a quantidade real de um a cinco tiros.
- Pernas renderizadas separadamente do tronco. Movimento avança por deslocamento real; parado retorna à pose parada. Troca/recarga do tronco pode tocar enquanto as pernas andam.
- Novas trocas cancelam a anterior. Recarga, disparo confirmado, morte, fim de partida e correções de posição interrompem a troca visual. O cancelamento autoritativo da recarga interrompe o clipe.
- Pose/caminhada do rifle atualizadas com a empunhadura do novo clipe, mantendo a escala e o GUID do atlas Mosin.

PlayerCharacterVisual e PlayerActionArt são compartilhados por jogadores e bots. Não leem o inventário privado dos outros clientes para escolher variantes. PlayerShooter replica ReloadRounds (quantidade efetivamente planejada para a recarga) e ShotSequence (disparos confirmados). Regras de recarga, acerto e disparo continuam no servidor. A quantidade é uma apresentação da ação visível, não o total da reserva.

## Monstros

- Rastejante substitui o visual geométrico do Stalker. Caminha somente quando há deslocamento; parado usa a pose de repouso. Preparação e extensão da mordida são ajustadas ao aviso autoritativo de 0,3 segundo; recuperação segue depois.
- Papo inchado substitui o visual geométrico do Corrosivo. Preparação/inflar usa AbilityEnds com 1,2 segundo. A contração/cuspe toca após a resolução; a caminhada pode retomar depois dos primeiros cinco frames de liberação. Parado completa a recuperação e volta ao repouso.
- Cancelamento antes do prazo evita animação falsa de liberação. Correções de posição, morte e fim de partida limpam o estado visual.
- VisibilityTarget e DamageFlash continuam responsáveis por visibilidade e cor. Colliders, navegação, dano e IA não foram alterados. Projétil e poça ácida existentes continuam separados dos sprites do corpo.

## Recursos e rede

Sprites vêm de renders reais dos arquivos .blend preservados em Prototypes. Atlas com células de 256 pixels; páginas no máximo 2048 x 2048. Camadas de tronco em Assets/Resources/PlayerCharacter/Actions. Monstros em Assets/Resources/Monsters/Stalker e Acid, rotacionados para frente +X. GUIDs existentes preservados; scripts/recursos novos têm .meta. Servidor sem dispositivo gráfico não carrega estes atlas.

Fontes reproduzíveis: Prototypes/PlayerWalk3D/export_pending_runtime.py e install_pending_assets.py. Variantes parciais do rifle exportam somente o trecho que contém o pente; restante compartilha o atlas principal. Shotgun de um cartucho tem modelo separado em ShotgunReloadSingle.

Protocolo atualizado de 17 para **18**, porque PlayerShooter ganhou campos de rede. NetworkBootstrap e o manifesto do build usam a mesma constante GameConfig.NetworkProtocol. Cliente e servidor devem ser atualizados juntos antes de testar conectados. Builds antigos e servidores em execução não foram modificados.

## Revisão realizada e validação pendente

Inspeção estática das ligações aos estados replicados e inspeção visual dos PNGs/GIFs Blender. Foi identificado e corrigido um problema do exportador: o início do render reaplicava a curva de hide_render e restaurava todas as munições nas variantes parciais do rifle. Agora somente as curvas de visibilidade das munições ausentes são silenciadas durante a exportação; os quatro atlas têm conteúdo distinto. Isso não confirma compilação Unity, RPCs gerados, importação nativa ou comportamento entre clientes.

Na validação prática inicial do usuário: experimentar as seis trocas e recargas parado/andando, uma/duas cargas de shotgun, um a cinco tiros de rifle, cancelamento por troca, disparo durante troca, bots e outro cliente observador; conferir mordida do Stalker e cuspe do Corrosivo sob a visão/fog e o flash de dano. A ciclagem do ferrolho a cada disparo não ganhou novo clipe nesta entrega: é ação diferente da recarga solicitada.
