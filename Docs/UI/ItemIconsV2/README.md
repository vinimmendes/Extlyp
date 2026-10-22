# Ícones V2 — catálogo e lotes

Atualização: [itens iniciais](ITENS_INICIAIS.md) acrescentam besta de mão improvisada, espada enferrujada e virotes. Catálogo atual: 62 ícones; os números de 59 abaixo descrevem o lote anterior. As três artes novas seguem os tamanhos e layouts V2 existentes e têm referências de mundo explícitas.

Entrega de 19/10/2026: etapas 1 e 2 preparadas, três pilotos integrados e lotes [A de recursos/peixes](LOTE_A.md), [B de armas/munições](LOTE_B.md), [C de ferramentas/chaves/extração](LOTE_C.md), [D de equipamentos/acessórios](LOTE_D.md), [E de moeda/tesouros](LOTE_E.md) e [F de treinamento](LOTE_F.md) concluídos após autorização do usuário para continuar. São 59 de 59 ícones integrados. Compressão ASTC e retirada do fallback de interface permanecem posteriores à avaliação visual; resolvedores antigos de mundo são preservados. Geração ou autorização de continuação não equivalem a validação em execução.

## Catálogo e separação visual

O [catálogo de cobertura](catalogo.csv) relaciona todos os 59 ItemId, famílias de produção, fontes anteriores de interface/mundo e destino V2. `Assets/Resources/ItemVisualCatalog.asset` contém uma entrada explícita por ID. Todos possuem InterfaceIcon novo: A=12, B=9, C=8, D=18, E=6 e F=6. WorldIcon permanece vazio em todas as entradas.

`ItemIconLibrary.Get(ItemId)` consulta InterfaceIcon e recorre ao resolvedor anterior. `GetWorld(ItemId)` prioriza WorldIcon, arte ambiental específica e arte anterior válida. Após o relato de itens quadrados, treino e referências antigas ausentes passaram a recuperar o sprite completo da interface; consulte a [correção do fallback](CORRECAO_ITENS_MAPA.md). ApplyWorld usa GetWorld e conserva suas regras anteriores de escala/colisor. IDs, atributos, quantidade, economia, saves e rede não foram alterados. O catálogo é configuração compartilhada e não armazena estado por jogador.

Todos os consumidores que já usam Get recebem os pilotos, inclusive ItemIconGui, inventário, equipamentos, loja, recursos, receitas e arraste. O mostrador da arma consulta o novo catálogo, preservando os símbolos de tiro/ataque e demais controles. Itens sem substituição mantêm o símbolo atual nesse mostrador. Adicionados preserveAspect aos slots, arraste e imagens de atividades, sem alterar seus retângulos.

## Importação e fontes

- Originais individuais em `Art/ItemIconsV2/Originals/`, preservados sem modificar pixels. Prompts completos em `Art/ItemIconsV2/*-prompt.txt`.
- Sprites de uso em `Assets/Resources/Items/IconsV2/`: Single Sprite, alpha, Bilinear, Clamp, sem mipmaps, sem compressão, limite 512 nos perfis Default/Standalone/Android. Nada foi convertido para ASTC nesta entrega.
- Pilotos gerados com imagegen integrado: bandagem `exec-b6dc1119-23fc-4396-8cbb-76b86e176795.png`; pistola `exec-4adb75a1-1fb3-4a13-9c54-450d0d35c671.png`; elmo `exec-5c8d37ed-2767-4959-b02f-d59ee1641a33.png`. Originais de geração permanecem no diretório generated_images da sessão.
- O script `Tools/UI/Prepare-ItemIconsV2.py` documenta a autoria do catálogo, metadados e levantamento. Lê os manifestos `Art/ItemIconsV2/lote-*.json`, não modifica pixels e preserva recursos existentes. Preenche referências vazias sem substituir referências já editadas por outras.

## Geometria atual, preservada

`L` é o lado real do slot depois de descontar seu espaçamento. `S = UiLayout.Scale = max(0,1, altura segura em pixels / 600)`. Para um retângulo virtual `(w,h)`, seu tamanho físico é aproximadamente `(w × S,h × S)`, considerando Canvas/transform local sem escala adicional. PreserveAspect ajusta a imagem dentro desse retângulo; o tamanho do objeto depende também das margens transparentes.

| Contexto | Retângulo da imagem em unidades virtuais | Pixels |
| --- | --- | --- |
| Inventário, equipamento e saque amplo | x=0,08L; y a partir de 0,29L; 0,84L × 0,46L | multiplicar por S |
| Slots compactos | x=0,10L; y a partir de 0,12L; 0,80L × 0,73L | multiplicar por S |
| Loja ampla | x=0,12L; y a partir de 0,34L; 0,76L × 0,44L | multiplicar por S |
| Loja compacta | x=0,12L; y a partir de 0,12L; 0,76L × 0,70L | multiplicar por S |
| Recursos de atividades amplo/compacto | 42 × 42 / 40 × 40 | multiplicar por S |
| Receita ampla/compacta | (largura do cartão − 48) × 70 / 72 × 72 | multiplicar por S |
| Arraste | ícone 50 × 50, posição (4,3) no visual 80 × 60 | multiplicar por S |
| Mostrador de arma | 44 × 26 dp, convertido por HudMetrics.Dp | 44 × densidade; 26 × densidade |

Exemplo da base 16:9 sem recorte: largura virtual 1066,67, conteúdo 1034,67 × 432. Grade de quatro linhas usa célula de 67 e slot L=61. Ícone compacto: 48,8 × 44,53 unidades; em 1920×1080, S=1,8, corresponde a 87,84 × 80,15 pixels de caixa. Em 1280×720, S=1,2: 58,56 × 53,44 pixels. O layout amplo é usado apenas quando o critério atual de Compact é falso; não foi redefinido.

Para as grades comuns, célula=min(largura disponível/colunas, altura disponível/linhas), L=célula−6. Base mantém máximo de 1200 unidades de largura de conteúdo e altura 432; loja compacta conserva sua divisão 60/40; Personagem conserva Equipamentos/Atributos. As posições dos detalhes e nomes atuais não foram refeitas. Fontes 24 dos menus não são 24 dp; HudMetrics continua convertendo os tamanhos da HUD de combate separadamente.

## Avaliação do piloto

[Galeria em 32, 48, 64, 96 e 128 pixels](../Previews/ItemIconsV2/index.html). É um documento de arte: escala as imagens originais no navegador. Não simula a importação 512, filtragem, Canvas, toque ou renderização Unity. Observar a 100% de zoom, inclusive em monitor de alta densidade.

Na próxima avaliação prática: comparar os três itens em compacto/amplo, Simples/Sombria, equipamento, loja e arraste. Conferir contadores sobre a arte, nomes nos detalhes, último slot e mudança de resolução. Pistola no mapa deve continuar com a arte anterior; no mostrador deve receber a nova arte sem tingimento marfim. Slots vazios mantêm os placeholders e opacidades atuais. Não mudar posições personalizadas nem tamanhos dos controles para acomodar a imagem.

Produção dos 59 itens e [revisão final da integração](REVISAO_FINAL.md) concluídas. O mostrador legado Quickbar também passou a consultar o catálogo comum, no retângulo existente. Próximo passo: avaliação prática dos dois temas e dos layouts compacto/amplo no aparelho, seguida dos ajustes de arte necessários. Após aprovação sem compressão, avaliar ASTC 4×4 no Android e manter sem compressão os ícones com perda perceptível. Qualquer retirada de fallback de interface deve preservar os resolvedores usados pelo mundo. Não executar MvpSetup.Build.

## Evidência e limites

A entrega do piloto recebeu revisão semântica de 207 fontes C#: zero erros com as referências/defines existentes, sem emitir assemblies. Os lotes A a F alteraram somente imagens, referências visuais, documentação e ferramentas de preparação/inspeção. A revisão final acrescentou a integração pontual de Quickbar, revisada estaticamente sem compilar. Catálogo atual com 59 IDs distintos, 59 substituições de interface, zero substituições de mundo e lotes A/B/C/D/E/F com 12/9/8/18/6/6 entradas. As cópias de uso são idênticas aos originais preservados; importação limita cada sprite a 512. Revisão completa de transparência, dimensões e metadados pelo inspetor somente de leitura `Tools/UI/Inspect-ItemIconsV2Batch.py all`. Sem Unity, testes automatizados do jogo ou builds. A inspeção de PNGs e o documento HTML não comprovam o resultado no aparelho. Nenhuma medição de desempenho ou memória foi realizada.
