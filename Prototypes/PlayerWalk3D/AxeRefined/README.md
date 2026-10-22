# Machado refinado — 21/10/2026

Modelo produzido no Blender 5.2.2 LTS pelo MCP oficial, a partir da animação revisada
em `../AxeAttack/protagonista-ataque-machado.blend`. Arquivos anteriores preservados.
Referência artística: `Assets/Resources/Items/IconsV2/BattleAxe.png`, também empacotada no `.blend`.

## Fonte e renders

- `protagonista-machado-refinado.blend`: personagem, ação aprovada e modelo editável do machado.
  Os quatro objetos `GEO-refined-axe-*` são o cabo oval, couro, cabeça curva chanfrada e encaixe de ferro.
  O cabo mantém os mesmos pontos de contato das duas mãos: direita em 50/128 e esquerda em 22/128.
- `machado-detalhe.png`: apresentação isolada em 1024 × 1024.
- `machado-chao.png`: sprite RGBA em 512 × 512, exportado para o item no mundo.
- `Weapon/001.png` a `026.png`: arma isolada na transformação 3D da empunhadura, em 256 × 256.
- `ataque-machado-refinado.gif`: composição dos atlas usados no jogo, com 0,40 s de preparação
  e 0,65 s de recuperação, pernas em repouso e mãos à frente do cabo.
- `poses-machado-refinado.png`: prancha; `leitura-tamanho-jogo.png`: comparação pequena em dois fundos.
- `manifest.json`: enquadramento, escala, quantidade de quadros e referência do sprite de mundo.

## Enquadramento e integração

O corpo mantém o enquadramento de 3,4 unidades por célula de 256 pixels. A arma usa 5,1 unidades
por célula, ou 50,196078 pixels por unidade, com pivô central. A câmera da arma tem a mesma posição
e direção da câmera do personagem e um enquadramento maior para dar margem ao movimento.
O atlas `AxeWeapon_1.png` tem 2048 × 1024, 8 colunas e 26 quadros.

As posições da empunhadura já estão renderizadas nas células. A integração usa posição local zero,
rotação identidade e escala um, selecionando o mesmo quadro do corpo pelo relógio existente de rede.
Não reaplicar os valores de `AxeGrip.json` ao atlas da arma. Os metadados antigos permanecem para
a apresentação anterior caso o atlas opcional falte. Espadas e lança mantêm suas apresentações.

`ItemVisualCatalog.WorldIcon` do machado aponta para `Items/Weapons/BattleAxeWorld.png`.
O ícone de inventário original e o comportamento de combate foram preservados.

## Produção

`Tools/Blender/RefineAxeModel.py`: executar `build()` sobre a animação revisada; recusa
sobrescrever a fonte refinada. `render_frames(first,last)` exporta a arma e restaura visibilidade
e câmera. `render_isolated(hero=True/False)` usa cópias temporárias desvinculadas da animação
para os renders isolados, preservando o personagem e seus controles.

`Tools/Blender/PackageRefinedAxe.py`: empacota somente a nova arte da arma e do chão, gera previews
com os atlas reais e mantém os GUIDs existentes. Não altera o catálogo automaticamente.
O `.blend` inclui a cena atual preservada ao iniciar o refinamento; `machado-refinado-v1.blend`
guarda a primeira iteração de geometria e materiais.

## Revisão

Silhueta, lâmina curva, fio, cabo, encaixe e composição inspecionados nos renders. Empunhadura,
recuo e proporções conferidos na prancha. Integração revisada estaticamente; sem testes
automatizados, sessões Unity ou builds. A prévia não valida rede, combate ou escala prática no mapa.
