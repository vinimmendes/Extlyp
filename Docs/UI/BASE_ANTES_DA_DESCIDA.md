# Base — Antes da descida

Implementação do conceito aprovado para o menu da base. A ilustração do destino agora preenche o fundo; a Preparação usa painéis compactos, navegação persistente com sublinhado e uma única ação principal preenchida: Partir. Referência visual arquivada em [conceito aprovado](Referencias/base-antes-da-descida-conceito.png). A imagem conceitual não é um atlas nem um painel clicável; ilustrações, textos, ícones e controles continuam independentes.

## Composição

- Cabeçalho: identificação BASE/BASE · TESTE, marca tipográfica, recursos com ícones e rótulos separados dos números e Opções. Valores não são botões.
- Cinco abas mantidas: Preparação, Inventário, Personagem, Comerciante e Atividades. Aba ativa recebe sublinhado de destaque e tipografia semibold; foco por teclado/controle recebe contorno claro. Hover e foco são tratados por eventos.
- Preparativos: capacidade ocupada, barra de slots, bandagens, munição reserva e somente status ativo de alimentação disponível na base. Organizar mochila e Personagem são secundários e neutros. Nenhum equipamento equipado aparece nessa página.
- Destino: arte existente de Floresta Velha/Catacumbas/área de testes, com recorte proporcional. Título sobre uma área escurecida com bordas suaves, desenhada com geometria de Canvas. Sem ampliar sprites do avatar.
- Expedição: Alterar destino usa a seleção já existente, estado de conexão e aviso de perda. Partir, busca, cancelamento, falha e nova tentativa permanecem no painel direito. Bots aparecem somente no ambiente de teste.
- Rodapé aparece quando há mensagem contextual do servidor, sem mensagem fictícia permanente de sucesso.

Mochila, recursos e conexão são apresentados em controles separados; abandonado o bloco único de texto da Preparação. Capacidade e contadores seguem os snapshots e CampSession.Changed, sem previsão local de inventário. Campo legado de equipamentos e seus ícones permanecem serializados para compatibilidade do prefab, mas ficam ocultos.

## Responsividade e temas

Altura virtual 600 pela UiLayout; conteúdo limitado a 1200 unidades com 16 de margem lateral. Preparação mantém 432 de altura a partir de y=116; cabeçalho/abas ficam acima. Conteúdo principal usa aproximadamente 24/46/30 com intervalos de 12 e mínimos laterais de 240/280. Quando a área central fica menor que 260, o título passa ao painel direito. O fundo se estende além da área segura até as bordas físicas da tela; controles permanecem dentro dela. Não há variante vertical.

Os dois temas compartilham layout e funcionamento. Sombria usa oliva quase preto, marfim e cobre fosco; Simples usa superfícies verdes limpas. Painéis de leitura quase opacos e molduras reduzidas a pequenos detalhes; sem granulado sobre letras. Só Partir recebe preenchimento principal na Preparação. A transição de entrada continua em aproximadamente 150 ms, sem bloquear comandos.

Textos de conteúdo em 18–20; títulos 24–32; marca/legendas secundárias em 14–16. Barlow Condensed Regular/SemiBold existentes preservadas. Botões principais e abas com altura mínima de 48. Slots mantêm sua apresentação própria e indicadores de raridade; a linguagem de superfície neutra é aplicada aos painéis e controles das páginas comuns, sem reformular os sistemas de itens.

## Integração

BaseHubView recebe referências independentes para cabeçalho, recursos, mochila, suprimentos, status e conexão. UguiBaseHub mantém os callbacks e o fluxo de navegação anterior. HudFrameGraphic recebe um estilo opcional de base, desativado por padrão nas outras interfaces. BaseMenuStyle aplica esse estilo ao escopo da base, preservando slots/círculos/barras; ao voltar ao inventário da expedição, o estilo é removido. BaseMenuFocus desenha o foco e hover sem depender apenas da cor.

O inventário não seleciona mais o botão de fechar oculto ao ser aberto dentro da base: mantém o foco válido da navegação ou usa o primeiro controle de conteúdo. As atividades mantêm seus apresentadores e o fluxo de retorno existente. Prefab BaseHub editado incrementalmente por `Tools/UI/Reform-Base-Cinematic.py`, preservando IDs antigos e a referência de PlayerInterface. Não executado MvpSetup.Build.

Sem alteração das capacidades (12/16), equipamento exclusivamente em Personagem, catálogo/estoques do comerciante, comandos de rede, protocolos, saves ou separação normal/teste. Nenhuma skill adicional foi instalada. Aplicadas game-ui-ux, ui-ugui e unity-csharp-scripting; a arte de conceito gerada anteriormente serve apenas como referência.

## Pesquisa aplicada

- [Crytek — revisão de interface de Hunt](https://www.huntshowdown.com/news/developer-insight-upcoming-ui-improvements): navegação persistente e redução de submenus.
- [NN/g — princípios visuais](https://www.nngroup.com/articles/principles-visual-design/): hierarquia, escala e agrupamento.
- [Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/full-list/): leitura, foco, ações identificáveis e controles espaçados.
- [W3C — contraste de texto](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html): meta de 4,5:1 como critério de leitura, sem alegar certificação.

## Revisão e avaliação pendente

Análise semântica de 199 fontes C# com referências/defines existentes, sem emitir assemblies e sem iniciar Unity. Sem erros. Revisão estática do prefab: referências locais completas, arrays 3/2 de recursos/suprimentos e hierarquia pai/filho consistente. Cálculo das cores principais, com fundo branco no caso desfavorável de composição dos painéis: texto corrente 12,73:1/14,47:1 (Sombria/Simples), texto secundário 9,20:1/10,26:1, texto do botão principal 7,91:1/9,57:1. Esses números não medem a renderização, antialiasing ou estados desabilitados.

Não foram iniciados testes automatizados, sessões Unity ou builds. Não há captura de execução desta entrega. A avaliação prática inicial permanece com o usuário:

1. Conferir Preparação em ambos os temas: destino reconhecível, Organizar mochila neutro e Partir como ação principal.
2. Conferir contadores e barra após equipar, transferir, comprar, vender e consumir; confirmar equipamento somente em Personagem.
3. Trocar destino e verificar fundo/título, inclusive área de testes somente no ambiente correto.
4. Navegar por mouse, toque, teclado e controle; conferir foco e retorno de inventário, confirmações e atividades.
5. Conferir busca/cancelamento/falha/tentar novamente, sincronização e solicitações pendentes.
6. Avaliar 16:9, celular horizontal com recorte, janela estreita e ultrawide: títulos sem sobreposição, botões acessíveis e fundo sem distorção.

A aparência final, recortes, textos longos e interação em dispositivo ainda não foram validados em execução.
