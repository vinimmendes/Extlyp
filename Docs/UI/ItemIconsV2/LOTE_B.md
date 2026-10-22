# Lote B — armas e munições

Continuação da produção autorizada pelo usuário em 19/10/2026. O lote B possui nove itens: a Pistola piloto preservada e oito novos sprites. A continuação não equivale a validação da aparência no aparelho.

## Conteúdo

| Item | Identificação visual |
| --- | --- |
| Munição de pistola | Cartuchos curtos com estojo largo e ponta arredondada |
| Munição de rifle | Cartuchos longos, com gargalo estreito e ponta aguda |
| Cartucho de espingarda | Cilindros grossos vermelhos, base de latão e topo plano |
| Rifle de ferrolho | Coronha longa de madeira, cano estreito e ferrolho, sem luneta |
| Espingarda de dois canos | Dois canos paralelos e coronha de madeira, sem mecanismo de bombeamento |
| Espada curta | Lâmina reta de dois gumes, guarda simples e punho curto |
| Machado | Uma cabeça de corte larga e cabo de madeira com empunhadura de couro |
| Lança | Haste comprida com ponta de aço em forma de folha |

O acabamento segue o piloto de Pistola: metal envelhecido, madeira/couro/latão discretos e iluminação neutra pelo alto à esquerda. A linguagem medieval sombria não transforma as armas existentes em mosquetes, bestas ou armas novas. A besta dos slots vazios permanece separada dos itens reais. Não há nomes, números, munição atual, raridade ou molduras gravados nas imagens.

Cada asset foi gerado individualmente com imagegen integrado. Prompts completos em `Art/ItemIconsV2/<ItemId>-prompt.txt`; proveniência em `Art/ItemIconsV2/lote-b.json`. Originais preservados em `Art/ItemIconsV2/Originals/`; cópias de importação em `Assets/Resources/Items/IconsV2/`, sem edição de pixels por código.

## Integração e preservação

Catálogo com 59 IDs e 22 substituições de interface: A=12/12, B=9/9 e o Elmo do Guardião piloto em D. Restam 37 itens: C=8, D=17, E=6 e F=6.

Somente InterfaceIcon é preenchido. Os 59 WorldIcon continuam vazios; objetos no mapa, armas seguradas e representações ambientais seguem seus resolvedores anteriores. O mostrador de arma já consulta o catálogo V2; por isso recebe os novos rifles, espingarda e armas corpo a corpo sem mudanças nos seus retângulos. Os símbolos funcionais de tiro/ataque não são substituídos.

Importação igual aos lotes anteriores: Sprite Single, alpha, Bilinear, Clamp, sem mipmaps e sem compressão; limite 512 em Default/Standalone/Android. Não foi aplicada ASTC nem removido o fallback antigo. Não mudaram IDs, valores, capacidade, receitas, estoque, perfil, rede ou saves.

Os sprites anteriores, seus metadados e as fontes C# são preservados. O preparador preenche somente referências vazias e mantém os GUIDs existentes. A tabela de cobertura passa a mostrar os nomes de apresentação corretos das três armas corpo a corpo, sem alterar os nomes no jogo.

## Revisão e aceite prático

Os oito PNGs foram inspecionados individualmente; referências, dimensões, transparência, metadados e cobertura foram conferidos. Todos são RGBA de 1254×1254, têm alpha de 0 a 255 e nenhum pixel com alpha superior a 128 toca a borda externa. As cópias de uso são idênticas aos originais preservados. Conferidos 59 IDs distintos, 22 referências de interface e zero substituições de mundo; lotes A e B completos.

Comparação de hashes de 236 arquivos confirmou a preservação das fontes C#, dos 14 sprites/metadados anteriores e dos padrões da HUD. Não foram iniciados Unity, testes automatizados ou builds; a revisão semântica não foi repetida sem mudanças C#.

[Galeria atualizada](../Previews/ItemIconsV2/index.html) com caixas de 32, 48, 64, 96 e 128 pixels CSS, fundos Simples/Sombria e as armas no início. A galeria não reproduz a importação 512, Canvas ou resposta de toque.

Na avaliação prática, conferir distinção entre as três munições e os componentes vazios; as duas armas longas; espada, machado e lança. Observar especialmente pontas, canos e hastes dentro dos slots, contadores, seleção, arraste e mostrador de arma. O botão de ataque deve conservar seu símbolo funcional. Próximo lote: C, com oito ferramentas/chaves/itens de extração.
