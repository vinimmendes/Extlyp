# Revisão da composição da HUD compacta

## Navegação: engrenagem e toque no minimapa

Por orientação do usuário, o botão Mapa foi retirado da HUD e da seleção do organizador; tocar no minimapa continua abrindo/fechando o mapa. O ID antigo e suas preferências permanecem serializados para compatibilidade, mas sua área de toque é vazia. Foi removida também a região invisível do botão antigo que `Minimap.ContainsScreenPoint` ainda considerava bloqueadora de comandos.

Menu passa a uma engrenagem vetorial, sem rótulo permanente, abrindo o mesmo menu existente. O círculo tem mínimo de 76 unidades virtuais e 44 pixels de lado para toque, conforme a escala da área segura; desenho e alvo usam a mesma caixa, sem uma ampliação invisível ao redor. A posição inicial fica à esquerda do minimapa, com intervalo de 16 unidades e o topo alinhado. A resolução da geometria impede que o botão editado invada o minimapa nas disposições usuais. O mapa ampliado usa uma área abaixo da engrenagem para não interceptar seu toque.

O reposicionamento é aplicado uma vez pela revisão `hud_menu_gear_alignment_v1`, com cópia do layout anterior em `hud_layout_before_menu_gear_v1`. Tamanhos, posições e regras dos demais controles são preservados; Menu continua editável no organizador. Restaurar apenas Menu ou o padrão geral usa a nova engrenagem. As prévias e a Ajuda foram atualizadas. Revisão estática, sem Unity, testes automatizados ou builds; resposta ao toque no aparelho permanece para avaliação do usuário.

## Alinhamento do conjunto de combate após pesquisa de referências

Referências visuais consultadas: [Bullet Echo — capturas da HUD](https://bullet-echo.en.uptodown.com/android), [Brawl Stars — capturas de combate](https://fr.aptoide.com/editorial/brawl-stars) e [página oficial de Brawl Stars](https://supercell.com/en/games/brawlstars/). A adaptação abaixo é uma decisão para Cyber Extraction: agrupar ações secundárias perto do disparo, preservar a hierarquia de tamanhos e manter o centro livre, sem copiar assets ou transportar as mecânicas desses jogos.

A evidência estática para o afastamento na captura é a mistura de âncoras: cura/troca/recarga seguem o centro, mochila segue a direita. O ajuste anterior preservava esse intervalo e só tratava colisões em uma fileira já próxima da mesma altura; não definia uma composição comum com o disparo.

Agora cura, troca, recarga e mochila ficam nessa ordem, em uma fileira com a mesma linha de base do botão de disparo, intervalos de 16 unidades e afastamento de 16 até ele. Arma/munição fica centralizada acima da fileira, com intervalo de 12 unidades; a mira fica no eixo vertical do disparo, 16 unidades acima. Os seis elementos alinhados passam a compartilhar a âncora inferior direita, mantendo distâncias em 16:9, 18:9 e 20:9. A posição e o tamanho do disparo são a referência existente, sem alterar o joystick ou os demais grupos.

O alinhamento solicitado é aplicado uma única vez na primeira composição compacta pela revisão `hud_combat_alignment_v3`, preservando os tamanhos personalizados. A configuração anterior é guardada em `hud_layout_before_combat_alignment_v3`; não há limpeza de PlayerPrefs. O organizador continua permitindo ajustes individuais depois disso. Restaurar Padrão reaplica essa composição em memória, sem salvar antes de o usuário confirmar. A verificação permanece estática e pelas prévias; nenhuma sessão Unity, teste automatizado ou build foi iniciado.

## Histórico das alterações anteriores

Segundo refinamento após a captura do usuário: mostrador de arma compacto de 144 × 68 unidades (antes, mínimo de 200 × 96), com ícone de 36 e os dois números. Cura, troca, recarga e mochila passam de 50 para um mínimo de 76 unidades de diâmetro no modo compacto, mantendo seus centros como referência. Uma fileira horizontal existente recebe espaço mínimo de 12 unidades entre círculos, com proteção da área de disparo quando há espaço; controles reposicionados em alturas distintas continuam independentes. Preferências armazenadas não são apagadas ou regravadas por esse ajuste.

Os quatro ícones agora usam `HudActionIconGraphic`, uma subclasse de Image com traçados vetoriais gerados diretamente em VertexHelper e suavização nas bordas. As molduras circulares já eram geometria nativa. Não há ampliação dos bitmaps antigos nem pacote adicional; os sprites existentes permanecem guardados. O prefab recebe apenas a troca de script dos quatro componentes de ícone e seu símbolo, preservando IDs, tintas e ligações de botão. `Tools/UI/Vectorize-HudActions.py` registra essa edição pontual. Revisão estática e prévias atualizadas; sem executar Unity, testes ou builds. Aparência e toque em execução aguardam avaliação do usuário.

Atualização após avaliação do usuário: o painel da arma agora exibe somente o ícone, a munição atual em destaque e a quantidade na mochila abaixo, sem nome, capacidade máxima ou rótulo Reserva, nos dois temas e layouts. Corpo a corpo mostra somente o ícone, sem números de munição fictícios ou atributos textuais. Desarmado não exibe um painel vazio. As prévias foram atualizadas para essa apresentação. Sem Unity, testes ou builds nesta alteração. As descrições de nome/capacidade/dano abaixo registram a primeira entrega, substituída por esta simplificação.

Implementação em 18/10/2026, restrita à HUD da expedição. Revisão estática e desenhos de composição; nenhuma sessão Unity, teste automatizado ou build executado.

## Prévia antes da integração

[Galeria em tamanho real](Previews/HudCompacta/index.html): 640 × 360, 1280 × 720 e 800 × 360 (20:9), nos temas Simples e Sombria. Os arquivos PNG conservam essas dimensões. A galeria também contém estudos de corpo a corpo, desarmado, cura em espera/recarga e extração.

Os desenhos usam Barlow e as posições padrão do projeto, com as novas dimensões dos painéis. Ícones e fundo são esquemáticos, não são novos assets do jogo nem capturas da Unity. O modo compacto está explicitamente representado também em 20:9, como em um dispositivo móvel. Preferências pessoais e recortes físicos precisam da avaliação no aparelho. Gerador: `Tools/UI/Preview-CompactHud.py`; não modifica prefabs, cenas ou PlayerPrefs.

## Composição implementada

- `HudLayout.Bounds` deriva stamina da posição da vida: mesma largura/altura e intervalo exato de 4 unidades virtuais entre molduras. Na altura física 360, são 2,4 pixels antes do arredondamento.
- Estados ativos ficam abaixo da stamina, com intervalo de 8 unidades. Arrastar esses estados acompanha a posição do grupo; o tamanho continua próprio. Vida e stamina compartilham arraste, tamanho, destaque de seleção e restauração no organizador compacto.
- Preferências existentes continuam armazenadas sem migração ou limpeza. A posição antiga independente da stamina permanece disponível no layout amplo. Desfazer/Cancelar continua restaurando os dados originais.
- Os painéis informativos usam suas dimensões reais em unidades virtuais, sem compensar a fonte pelo inverso da escala do controle. Relógio: pelo menos 180 × 76; arma: 200 × 96; interação: 280 × 56; minimapa: 178 × 160. Mapa/menu comportam o texto de 20 unidades. São reservas por conteúdo, não um aumento geral de fonte.
- Vida/stamina: nome Barlow Regular e valor SemiBold de 22, acima da barra. Relógio/carregador: SemiBold de 30. Auxiliares: Regular de 20. Valores essenciais não usam reticências. A superfície e o toque continuam derivados de `HudLayout` e `UiLayout`.
- Movimento, corrida, troca, recarga e mochila mantêm os ícones, sem nomes externos. Cura mostra quantidade dentro do botão (`99+` acima de 99) ou segundos de espera. Disparo conserva ATIRAR/ATACAR dentro do círculo. Indisponibilidade mantém o tratamento visual existente; nomes completos continuam na Ajuda e no organizador.
- `CaptionBackdrop` e sua criação dinâmica foram removidos. Notas contextuais ampliam a própria superfície do relógio apenas enquanto existem; a barra de extração fica na borda inferior dessa extensão. A proximidade exigida para mensagens dos mecanismos não mudou.
- Orientação e legenda do minimapa mantêm áreas separadas do atlas. Interação mostra o ícone e a ação completa somente no contexto já existente.

## Investigação do painel de arma vazio

Evidência estática: Title, Value e Detail estavam ativos e vinculados no prefab `Hud.prefab`. O preenchimento já recebia `PlayerShooter.Weapon`, `Magazine` e `Reserve`. A apresentação anterior aumentava a fonte inversamente à escala visual, sobre caixas de largura antiga, com overflow Ellipsis. Isso permite que conteúdo válido deixe de caber, sem indicar perda dos dados da arma. Não houve reprodução em execução nesta revisão.

A integração remove essa compensação e as reticências, usa o painel ampliado e assegura a ativação dos textos ao aplicar a geometria. Nome, carregador e reserva ficam em linhas próprias. A fonte física SemiBold usa peso Regular no componente para evitar uma segunda seleção de peso. As correções anteriores de tabelas de pesos/fallbacks em `UiFontAssets` permanecem intactas. Sem arma, a apresentação mostra SEM ARMA e um traço, oculta o ícone de pistola e não inventa carregador/reserva. Corpo a corpo mantém nome real, dano e custo de stamina. Nenhuma alteração em inventário, combate, rede ou PackageCache.

## Revisão e avaliação prática pendente

Foram revisados os caminhos de arma de fogo, corpo a corpo, desarmado, recarga/ferrolho, quantidade/espera da cura, estados ativos, notas contextuais, progresso de extração e mapa. Também foram lidos os caminhos de arraste, redimensionamento, restauração e cancelamento do organizador. As prévias foram inspecionadas visualmente; isso não valida renderização TMP, toque, material SDF ou fluxo de rede.

Avaliação no jogo: alternar os dois temas e proporções, abrir/fechar o mapa, usar armas de fogo e corpo a corpo, ficar desarmado, recarregar, curar com espera e contagens grandes, aproximar-se de objetos e extrair. Reposicionar/redimensionar vida e stamina por ambas as barras, salvar e cancelar. Conferir recortes à esquerda/direita e personalizações já salvas. Aceitação visual em execução permanece pendente da avaliação do usuário.
