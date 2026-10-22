# Menus do conceito — implementação em componentes

Os quatro menus aprovados usam agora a mesma abordagem da entrada: imagens de ambiente separadas, painéis e molduras nativos, textos TextMeshPro editáveis e botões uGUI com estados visuais. A prancha de conceito não é usada como interface achatada.

## Apresentação

- Expedição: `ExpeditionForestBackdrop.png` como cenário da tela, painel quase opaco à direita, descrição com altura e largura explícitas no ScrollRect e ação Partir em destaque. Configuração de mapa/bots segue restrita ao contexto de teste. A entrada permanece com seus próprios cartões.
- Inventário: cenário da cabana separado do painel, duas grades de nove slots na base ou no saque e somente mochila quando apropriado. Ícones maiores, nome separado da quantidade, rótulo de quantidade próprio em cada slot e preenchimento uniforme. Arraste, transferência, pilhas, descarte e cancelamento continuam usando o controlador existente.
- Seleção de peixe: painel compacto que se adapta à quantidade de peixes disponíveis, preservando os nove slots de origem da mochila. São exibidos somente os itens elegíveis. Cada cartão guarda o índice original para chamar `ChooseFish`, sem reorganizar ou alterar o inventário. A grade cresce até três linhas quando necessário; voltar permanece uma ação independente.
- Bancada: três cartões com ícone, título, produção e custo derivados dos dados atuais; botões Fabricar independentes. Recursos mostram o snapshot da mochila e do baú. Reciclador, componente e voltar ficam na seção secundária. Confirmação e rejeição de receitas permanecem no servidor.

`UguiMenuStyle` aplica fundo, superfície e estados de botão. `HudFrameGraphic.MenuStyle` e `SlotStyle` desenham superfícies quase opacas e molduras finas; o tema Simples mantém a mesma geometria com sua paleta. `UguiCampPresenter` atualiza a preparação ao receber o snapshot ou mudanças de contexto, tamanho, tema e pendência.

## Asset novo

Arquivo: `Assets/Resources/UI/Art/ExpeditionForestBackdrop.png`. Gerado pela ferramenta integrada image_gen a partir da prancha aprovada. Prompt: reconstruir apenas a ilustração ambiental da expedição, em 16:9, ruínas de igreja e ponte de pedra na floresta fria, veículo abandonado, chão rochoso e vegetação; completar o espaço coberto pelo painel; sem textos, molduras, botões, pessoas ou outros quadrantes. A arte é ilustrativa e não cria ruínas ou estruturas no mapa jogável.

O cenário `EntryCabinBackground.png` e os ícones de itens existentes foram reutilizados. Não foram adicionadas receitas, moedas, capacidade de inventário ou regras. Minigames e ferramentas de teste continuam fora desta mudança.

Prompt completo usado na ferramenta integrada, com a prancha aprovada como referência:

> Extract/reconstruct the environmental artwork seen in the TOP LEFT quadrant of the concept board as a standalone full-width 16:9 background asset for an original survival game's expedition preparation screen. Only scene art: ancient abandoned stone church ruins and broken stone bridge deep in a cold misty overgrown forest, derelict rusty vehicle near bottom left, wet rocky foreground, painterly realistic horror woodland atmosphere, soft ivory grey sky and olive green foliage. Match the reference scene closely. Extend scenery naturally to fill the space previously covered by the right UI. No people. No text, UI, panels, border, buttons, title, symbols, collage or other quadrants. Keep the right half calmer and darker for separately overlaid real interface. Use Darkwood-like atmosphere without copying any game assets.

## Conferência

Inspeção estática das referências, relações de parent/child, três cartões de receita, quantidades independentes e preservação dos dez prefabs. Não houve execução Unity, build ou suíte automatizada.

Conferir primeiro: entrada e retorno; expedição normal/de teste; mochila e baú; saque e arraste para fora; escolha de peixe cuja origem não seja o primeiro slot; bancada, confirmação do servidor e recurso insuficiente; temas Sombria/Simples e proporções horizontais diferentes.

## Correção pontual da expedição

As capturas posteriores mostraram a descrição recortada horizontalmente. Causa identificada no prefab: a Image do Viewport usava `preserveAspect=true` com um sprite quadrado; a Mask recortava pela geometria quadrada da Image, não pelo retângulo disponível para o texto. A propriedade foi corrigida no componente existente, e o autor de prefabs também recebeu a opção explícita para evitar a regressão. O layout do Content é atualizado e a posição de rolagem volta ao início quando o conteúdo muda.

Título, descrição e background passam a seguir a seleção real de mapa. A Área de testes usa a ilustração separada `EntryTestWorkshop.png`, texto próprio e a indicação de recursos temporários; Floresta Velha restaura a ilustração da floresta e sua descrição. A preferência de mapa não foi alterada e os controles de teste continuam sujeitos ao contexto autorizado pelo servidor.

Verificação desta correção: leitura estática do componente de máscara, dos dois recursos de imagem e do caminho de atualização da seleção. Reimportação, rolagem e alternância em execução ainda não foram conferidas na Unity.

## Saque sobre o cenário em tempo real

No contexto `InventoryWindow.WorldMode`, o fundo ilustrado é desativado. O painel recebe 24% da opacidade do preenchimento e os slots 26%; a composição dos dois permite enxergar o cenário através da área de itens. O ajuste afeta somente os preenchimentos nativos: ícones, nomes, quantidades, bordas e botão Fechar mantêm a legibilidade. Ao voltar ao inventário da base, preenchimentos e fundo ilustrado são restaurados. Não foi adicionado escurecimento de tela inteira ou pausa da partida. A transferência e o descarte continuam usando as regras existentes.

Verificação desta mudança: revisão estática dos dois caminhos de apresentação e da aplicação de alpha apenas ao preenchimento. Visibilidade de monstros e contraste sobre o cenário ainda precisam de conferência prática na Unity.
