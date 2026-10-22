# HUD da expedição — Sombria refinada

Implementação e entrega visual de 19/10/2026. A primeira avaliação é do usuário, no aparelho. Não foram iniciados Unity, testes automatizados do jogo ou builds.

## Pesquisa aplicada

Não há uma resolução física única de celular pequeno. A referência Android de 320 dp é útil para a menor composição, mas pixels, densidade e área segura precisam ser tratados separadamente. A recomendação é alvo de toque de pelo menos 48 × 48 dp; o desenho visível pode ser menor. Fontes: [layouts Android](https://developer.android.com/develop/ui/views/layout/responsive-adaptive-design-with-views) e [acessibilidade Android](https://developer.android.com/guide/topics/ui/accessibility/views/apps-views). Como comparação de plataforma, a [Apple](https://developer.apple.com/design/tips/) recomenda 44 × 44 pontos. Isto não acrescenta suporte a iOS.

O [DisplayMetrics](https://developer.android.com/reference/android/util/DisplayMetrics) fornece a densidade lógica do Android. A implementação não usa [Screen.dpi](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Screen-dpi.html) como medida física confiável. A densidade é consultada quando a geometria da tela muda, com correção para a resolução de renderização da Unity.

Referências de diversidade: [iPhone SE de 4,7 polegadas](https://support.apple.com/en-ie/111866), 1334 × 750; [Galaxy A01 Core](https://news.samsung.com/co/samsung-anuncia-integracion-entre-galaxy-a01-core-y-spotify-lite), 1480 × 720. São referências de telas diferentes, não presets de pixels para dimensionar o toque.

Referências artísticas e decisões de projeto:

| Referência | Aplicação nesta HUD |
| --- | --- |
| [Bullet Echo / ZeptoLab](https://www.zeptolab.com/games/bullet-echo) | Economia de informação e foco na leitura durante o combate; interpretação de design, sem copiar assets. |
| [Diablo Immortal / Blizzard](https://news.blizzard.com/en-us/article/23805083/making-a-game-for-everyonediablo-immortals-accessibility-features) | Conforto e reposicionamento dos controles; materialidade adaptada para ornamentação discreta. |
| Darkwood e Sombria aprovada | Atmosfera oliva escura, couro e cobre envelhecido, centro livre. |
| [Identificação de ícones / NN Group](https://www.nngroup.com/articles/icon-testing/) | Família com silhuetas distintas e poucos detalhes; identificação no tamanho final ainda será observada no aparelho. |

Skills consultadas no skills.sh: [game-ui-ux](https://www.skills.sh/gamedev-skills/awesome-gamedev-agent-skills/game-ui-ux), já instalada e aplicada; [create-game-assets](https://www.skills.sh/gamedev-skills/awesome-gamedev-agent-skills/create-game-assets), referência complementar para produção; [ui-ux-pro-max](https://www.skills.sh/nextlevelbuilder/ui-ux-pro-max-skill/ui-ux-pro-max), referência complementar de hierarquia/contraste, sem adotar sua arquitetura web. Nenhuma skill ou serviço foi instalado. Aplicadas também as skills locais ui-ugui, imagegen, unity-csharp-scripting e unity-input-system, adaptadas à versão real 6000.0.84f1.

## Arte e composições

- [Prévias interativas](Previews/HudSombriaRefinada/index.html): tamanho, tema, estado, recorte e áreas de toque.
- [Sombria em 640 × 360](Previews/HudSombriaRefinada/sombria-640x360.png), [720 × 360](Previews/HudSombriaRefinada/sombria-720x360.png) e [menor composição](Previews/HudSombriaRefinada/sombria-568x320.png).
- [Simples em 640 × 360](Previews/HudSombriaRefinada/simple-640x360.png).
- [Família de ícones](Previews/HudSombriaRefinada/familia-icones.png).
- [Prancha de direção artística](Previews/HudSombriaRefinada/direcao-artistica.png), composta com os mesmos assets.
- [Fontes editáveis, PNG separados e prompts](../../Art/HudSombriaRefinada/README.md).

As prévias são documentos de design renderizados com os sprites dos atlas, Barlow e geometria equivalente. Não comprovam o resultado do Canvas, a serialização ou a resposta de toque na Unity. O cenário foi reconstruído a partir da captura fornecida para retirar a HUD antiga; a cartografia do documento é ilustrativa.

Sombria usa superfícies de couro oliva escuro, volume suave, cobre fosco e símbolos marfim. Simples usa a mesma geometria, símbolos e controles, com superfícies limpas e destaque verde. Os símbolos são SVG editáveis exportados para PNG; as superfícies circulares Sombria são raster original gerado com imagegen integrado. Não há texto gravado nas imagens.

## Métricas e geometria

| Perfil | Seleção na área útil | Ações | Disparo | Movimento | Minimapa |
| --- | --- | ---: | ---: | ---: | ---: |
| Pequeno | largura < 640 ou altura < 360 dp | 48 | 72 | 96 | 88 × 80 |
| Padrão | demais celulares | 52 | 76 | 104 | 96 × 88 |
| Amplo | largura ≥ 800 e altura ≥ 480 dp | 56 | 88 | 112 | 112 × 104 |

`HudMetrics` converte dp para o espaço virtual existente: densidade / UiLayout.Scale. `UiLayout` e a escala global dos menus não foram alterados. Android usa DisplayMetrics; desktop simula altura de 360 dp nas Game Views maiores e mantém os tamanhos lógicos das vistas pequenas. No Editor, o menu **Cyber Extraction > HUD > Altura lógica** permite escolher 320/360/480 dp manualmente, preservando a proporção real da Game View. Configure a Game View na proporção desejada; o comando não inicia Play Mode.

Todos os controles interativos da HUD têm pelo menos 48 × 48 dp. Vida, stamina, relógio, status e mostrador de arma são informativos e não capturam toque fora do organizador. O desenho e os alvos partem de `HudLayout`; não há expansão invisível invadindo o controle vizinho.

A fileira Cura → Troca → Recarga → Mochila usa oito dp entre elementos e doze antes do disparo; bordas inferiores alinhadas a dezesseis dp da área segura. Mira fica acima do disparo no mesmo eixo. Arma fica oito dp acima da fileira. Vida/stamina compartilham largura e posição, com quatro **unidades virtuais** entre molduras, não quatro dp. Estados ficam abaixo do grupo. Não há nomes externos permanentes nos círculos.

Barlow Regular nos auxiliares, SemiBold nos valores principais: 16 dp vida/stamina, 22 dp relógio/munição, 14 dp auxiliares, 12 dp ATIRAR/ATACAR. Valores essenciais não usam autosizing ou reticências. O painel da arma cresce para contagens maiores, com ícone, carregador e munição na mochila; corpo a corpo oculta ambos os números; desarmado oculta o painel. Bandagens mostram quantidade com limite visual 99+ ou segundos; cura/recarga usam sobreposição radial com as durações existentes do gameplay.

## Minimap e mapa ampliado

Cartografia derivada dos mapas existentes: piso, parede e vazio recebem contrastes distintos. Floresta mantém rotas reais e reduz ruído decorativo. O canto mostra aproximadamente 64 unidades de mundo de largura, limitado ao mapa; norte permanece fixo, jogador usa seta e saídas usam porta e indicadores de conhecimento. Não há informação de inimigos.

A legenda permanente foi removida. O toque no minimapa abre o mapa completo, separado da engrenagem. Os marcadores permanecem geográficos. Para evitar áreas de toque sobrepostas em saídas próximas, o mapa ampliado oferece uma faixa horizontal rolável de botões de **120 × 48 dp**, com nomes completos, conhecimento e identificação da saída selecionada. Isso também atende as dez saídas da floresta. A faixa usa o pedido de seleção existente. O mapa completo mostra vento na floresta; legendas de setores só aparecem quando há largura suficiente para leitura.

## Preferências e integração

Os 18 IDs de HudControl permanecem; o ID antigo Map fica reservado e sem alvo. O botão Mapa não voltou. A engrenagem continua abrindo o menu e o minimapa continua abrindo o mapa.

As migrações cumulativas de alinhamento foram substituídas pelos padrões por perfil. Novas personalizações ficam em `hud_layout_refined_v2_0`, `_1`, `_2`, em unidades lógicas. A chave `hud_layout_v1` e os backups anteriores não são apagados nem sobrescritos. **Restaurar organização anterior**, no organizador, importa o layout antigo para edição; só **Salvar** confirma a nova configuração. Padrão limpa apenas as posições do perfil atual. Cancelar recupera o estado anterior, inclusive ao visitar mais de um perfil durante a edição. Os tamanhos mínimos continuam valendo no layout antigo importado; pode ser necessário reorganizar posições antigas que eram muito pequenas.

Nenhuma alteração em captura de dedos, cancelamento de gestos, ações de input, câmera, rede, persistência de gameplay ou cenas. As correções das fontes continuam em UiFontAssets; nenhum arquivo em PackageCache foi alterado. Os prefabs existentes são reutilizados; a apresentação é configurada no componente em execução, sem reconstrução global de prefabs/cena.

## Revisão desta entrega

Revisão estática do fluxo de dados, geometria, alvos e compatibilidade das APIs com as dependências instaladas; inspeção visual das prévias e dos atlas. Prévias cobrem os cinco tamanhos, dois temas, densidades 1×/2×/3× e recortes esquerda/direita. Há composições específicas de cura/recarga, contagens grandes, corpo a corpo, desarmado, interação e extração.

O orçamento nominal dos dois atlas é 8 MiB RGBA32, sem mipmaps e sem cópia para leitura em CPU. Fontes e cartografia ficam fora desse orçamento. As imagens de geração e PNG separados ficam fora de Assets, evitando duplicação em Resources. Não foi medido uso de memória/draw calls no aparelho.

## Roteiro para avaliação prática

1. Abrir a expedição no celular pequeno. Conferir identificação imediata dos oito símbolos, predomínio do disparo, leitura dos valores e centro livre sobre piso claro, sala escura e cenário carregado. Repetir Simples/Sombria.
2. Experimentar as referências 568 × 320, 640 × 360, 720 × 360, 800 × 360 e 800 × 480, com as densidades e áreas seguras reais; girar entre paisagem esquerda/direita. Observar margens, polegares e recortes. As PNG 2×/3× demonstram densidade de renderização, não substituem este passo.
3. Mover, correr, mirar e disparar com dedos simultâneos; arrastar para fora do botão e soltar; abrir menu/mochila enquanto segura um gesto. Confirmar cancelamento e ausência de comandos presos.
4. Equipar pistola, Mosin, escopeta, espada, machado e lança. Observar seus símbolos, números, munição zerada, recarga e troca. Desarmar: painel some. Corpo a corpo: só ícone. Não devem aparecer nomes ou máximo do carregador.
5. Curar com vida cheia/ferida, sem bandagens, em espera e com quantidade superior a 99. Conferir número, preenchimento e indisponibilidade, sem palavras truncadas. Observar estados de alimentação/esporos e interação contextual com nomes longos.
6. Entrar em extração e interromper; conferir relógio, seta, distância, barra e extensão contextual. Abrir/fechar mapa: canto é local, ampliado contém geografia completa. Percorrer a faixa de saídas, incluindo as dez da floresta, selecionar uma e confirmar destino/conhecimento/vento.
7. Abrir organizador, arrastar/redimensionar, alternar proporções/perfis, Salvar, reabrir e Cancelar. Conferir grupo vida/stamina, limite de toque, recusa de sobreposição durante arraste/redimensionamento, Padrão e restauração do layout anterior. Depois de importar o legado, revisar posições antes de salvar.

Critérios em execução ainda pendentes: nitidez percebida no aparelho, conforto real, física do multitouch, serialização/importação nativa dos sprites e ausência de erros do Console. Esta entrega não é apresentada como validada em Play Mode.
