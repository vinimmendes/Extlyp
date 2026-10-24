# Pesca — conceito e storyboard v02

## Estado desta entrega

Etapa visual anterior à integração, conforme o plano solicitado em 24/10/2026. Precisa de avaliação do usuário antes de finalizar assets e alterar o minigame Unity. A versão v01 e as demais cenas foram preservadas.

Fonte: `fishing-preview-v02.blend`, cena `Fishing-Preview-v02`, Blender 5.2.2 LTS com EEVEE. Câmera ortográfica; 216 quadros a 12 fps; duração de 18 segundos. PNGs originais em 1280 × 720; GIF para avaliação em 960 × 540. Fontes instaladas Segoe UI/Segoe UI Bold foram empacotadas no arquivo Blender.

## O que avaliar

Escolha de isca → lançamento → espera → visitante se aproxima e investiga → recusa e saída → nova espera → segundo visitante → mordida e fisgada manual ilustrada → disputa → captura e novo lançamento manual.

- Um exemplo com minhoca selecionada: estoque ilustrativo passa de 3 para 2 na mordida. Não muda na recusa.
- Molduras finas, cobre fosco, botão com relevo, ícone de carretel, marcações no medidor, seleção da isca e card de resultado com nome revelado ao final.
- Peixe de corpo deformável e cauda animada; boia com flutuação, mergulho e deslocamento; anzol com minhoca. Água, linha e ondas são elementos independentes.
- Temas Sombria e Simples comparados no mesmo quadro e com as mesmas posições.

## Limites práticos

O storyboard é uma sequência dirigida para apresentar os dois ramos. Não contém probabilidades executáveis, consumo persistente, entrada do jogador, física ou autoridade de servidor. Os medidores ilustram estados, não validam dificuldade. A janela de fisgada é abreviada porque o jogador demonstrativo responde antes de 1,6 s; a disputa rare ilustrada é curta e não substitui o balanceamento final de 7–11 s. O GIF se repete como reprodução de mídia; a proposta de gameplay não relança automaticamente.

Os modelos são originais e estilizados para leitura em painel: peixe tem cerca de 6–8 vezes o comprimento visual da boia. Não equivalem à escala física do mapa. A HUD é uma composição de referência para montagem futura em `HudTheme`/`UiLayout`; não é um prefab e não deve ser aplicada como fundo único.

Nenhum arquivo em `Assets`, nenhuma tabela de loot e nenhum serviço estável foram alterados. Nenhuma sessão Unity, teste automatizado ou build de validação foi iniciado.

## Reprodução e referências

- Autoria: `Tools/Blender/FishingPreviewV02.py`. Recusa substituir arquivo ou cena existente.
- Render: `Tools/Blender/RenderFishingPreviewV02.py`, em processo Blender separado carregando o `.blend`. A renderização reaproveita quadros já presentes; para uma revisão, criar outra pasta/versionar antes de renderizar, evitando quadros antigos.
- Empacotamento e verificação estrutural da mídia: `Tools/Blender/PackageFishingPreviewV02.py`.
- `timeline.json`: transições do storyboard; `preview-manifest.json`: dimensões, duração e quadros do GIF entregue.
- Direção de arte: `Docs/UI/DIRECAO_DE_ARTE_HUD.md` e composição simplificada discutida neste chat. Atmosfera sombria e detalhes discretos seguem a referência Darkwood; clareza de silhueta e espaço de leitura seguem a referência Bullet Echo, com arte própria.
- Não foi usada imagem gerada por IA neste lote; o conceito estático e a animação são renderizações da mesma cena Blender.

## Trabalho posterior à avaliação

Finalizar e exportar boia/peixe em sprites; implementar `Worm` no catálogo e loot comum; configuração de pesca; estados e sorteios autoritativos; consumo persistente na mordida; fisgada; disputa de tensão; apresentação responsiva e resultado confirmado pelo servidor. Os parâmetros e regras permanecem os do plano aprovado para implementação, sem tratá-los como já implementados por esta prévia.
