# Seleção de ambiente — composição aprovada

## Revisão: componentes reais

A implementação abaixo foi substituída após o usuário rejeitar a imagem achatada com áreas transparentes de clique. A tela atual usa `EntryCabinBackground.png` exclusivamente como cenário, e `EntryNormalCabin.png`/`EntryTestWorkshop.png` nas duas imagens de cartão. Nenhuma dessas imagens contém o texto ou os botões da interface. A amostra original foi preservada como referência, mas seu GUID não é mais usado pelo prefab.

Os cartões, título, subtítulo, descrições, rodapé e dois botões são componentes independentes no prefab existente. Textos usam TextMeshPro/Barlow; molduras e desgaste são geometria nativa de `HudFrameGraphic.EntryStyle`. Cada Button tem ColorTint com estados normal, hover/seleção, pressionado e desabilitado, com transição de 0,09 s. Navegação explícita permite escolher os dois cartões pelo teclado/gamepad. As ações continuam usando `ChooseEnvironment` e a disponibilidade de teste continua regida por `GameConfig.AllowQuickTest`.

O conteúdo segue a composição de 1672 × 941 como unidades de referência e escala uniforme na área segura. O cenário cobre o fundo com recorte de proporção; os controles são centralizados e não são recortados. Os dois temas mantêm a geometria e recebem suas respectivas cores. A separação dos assets mantém a direção visual aprovada, sem afirmar identidade de pixels entre a ilustração original e as novas imagens.

Assets gerados pela ferramenta integrada image_gen, copiados para `Assets/Resources/UI/Art`:

- `EntryCabinBackground.png`: edição da referência para remover todos os cartões, imagens inseridas, molduras, botões e letras; recompor a madeira da cabana, preservando câmera, lampião quente à esquerda e janela com floresta fria à direita.
- `EntryNormalCabin.png`: cena independente horizontal 2:1 inspirada no cartão esquerdo; cabana de madeira gasta, lampião na varanda, floresta fria e enevoada; sem UI, molduras ou texto.
- `EntryTestWorkshop.png`: cena independente horizontal 2:1 inspirada no cartão direito; oficina improvisada sob lona, ferramentas, caixas e pequeno monitor; sem UI, molduras ou texto.

Validação desta revisão: inspeção estática da hierarquia e referências dos dez prefabs. Não foi iniciada uma sessão Unity. Conferir no jogo seleção, toque/pressionado, desabilitado, retorno à entrada e proporções horizontais diferentes.

## Histórico da primeira implementação — substituída

O usuário aprovou a amostra gerada e solicitou implementação fiel à imagem. A arte original foi copiada, sem alteração, para `Assets/Resources/UI/Art/EnvironmentSelectionApproved.png`. O arquivo tem 1672 × 941 pixels; a importação utiliza sRGB, sem mipmaps, sem compressão e sem redimensionamento para potência de dois.

Nesta tela, a composição inteira é uma camada visual RawImage ligada no prefab existente de expedição. Dois Buttons uGUI independentes recebem os cliques nos retângulos dos botões ilustrados. Seus handlers continuam chamando `NetworkBootstrap.ChooseEnvironment`, preservando as regras e separação de progresso. A imagem não recebe raycasts. O teste indisponível apresenta escurecimento no seu botão.

O conjunto usa escala uniforme e centralização na área segura. Mantém a proporção original e margens pretas nas proporções diferentes. Não recorta, estica ou reposiciona internamente a arte. Os alvos de clique usam a mesma transformação dos pixels de origem.

Os textos e molduras da amostra são parte desta composição raster aprovada. Isso preserva o desenho exato, mas alterações nesses textos ou localização exigirão uma nova versão da arte. A imagem não foi usada como fundo da HUD de combate nem da cabana. Os componentes e textos editáveis existentes continuam sendo usados nas telas seguintes; os cartões anteriores foram preservados no prefab.

Geração original: ferramenta integrada image_gen, categoria ui-mockup. Direção do prompt: tela horizontal de seleção, interior de cabana abandonada, lampião quente à esquerda, janela com floresta fria à direita; cartões translúcidos oliva, marfim e cobre fosco; títulos “ESCOLHA SEU AMBIENTE”, “CABANA NORMAL” e “ACAMPAMENTO DE TESTE”; botões “Entrar na cabana” e “Entrar para testar”. A implementação reutiliza a saída aprovada, sem nova geração.

Verificação: igualdade dos hashes entre a imagem original e a cópia do projeto; inspeção estática dos vínculos e hierarquia dos prefabs. Importação Unity, cliques e aparência em execução ainda precisam de validação prática pelo usuário.
