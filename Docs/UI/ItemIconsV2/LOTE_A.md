# Lote A — recursos e peixes

Continuação autorizada pelo usuário após a entrega dos pilotos, em 19/10/2026. O lote A possui doze itens: a bandagem piloto preservada e onze sprites novos. A continuação não é registrada como confirmação de validação em execução.

## Conteúdo

| Item | Direção visual |
| --- | --- |
| Madeira | Três troncos curtos amarrados, com cortes claros e casca marrom |
| Pregos | Cinco pregos de ferro forjado, cabeças largas e hastes distintas |
| Metal | Três chapas de ferro sobrepostas, com oxidação discreta |
| Pólvora | Bolsa de couro aberta com o conteúdo escuro visível |
| Componentes | Estojos de latão vazios, com bocas abertas, sem pontas de projétil |
| Peixe comum | Corpo fino prateado/oliva, sem decoração de raridade |
| Peixe raro | Corpo mais alto, listras escuras e dorsal espinhosa |
| Peixe grande | Bagre alongado, cabeça larga e barbilhões |
| Peixe assado | Peixe inteiro dourado com cortes e carne clara |
| Peixe queimado | Silhueta inteira carbonizada, com destaques cinza para leitura |
| Peixe cru | Filé de carne clara, sem cabeça ou cauda |

Imagens originais geradas individualmente com imagegen integrado, usando a bandagem apenas como referência de iluminação e acabamento. Prompts completos em `Art/ItemIconsV2/<ItemId>-prompt.txt`; proveniência e caminhos selecionados em `Art/ItemIconsV2/lote-a.json`. Os originais ficam em `Art/ItemIconsV2/Originals/` e as cópias para importação em `Assets/Resources/Items/IconsV2/`. Não foram editados pixels por código.

## Integração

O catálogo passa de 3 para 14 substituições de interface: os três pilotos e os onze itens desta etapa. Lote A completo (12/12); lotes B e D já possuem um piloto cada. Restam 45 itens: B=8, C=8, D=17, E=6, F=6.

Apenas InterfaceIcon recebe as novas referências. Todos os 59 WorldIcon continuam vazios, preservando os resolvedores ambientais anteriores. Nenhuma definição de item, prefab de controles, HudMetrics, dado de layout ou regra de transação foi alterada nesta etapa.

Importação igual aos pilotos: Sprite Single, alpha, Bilinear, Clamp, sem mipmaps e sem compressão, limite 512 nos perfis Default/Standalone/Android. Não foi aplicada ASTC. Os sprites antigos e fallbacks continuam disponíveis.

O preparador de catálogo agora lê os manifestos `lote-*.json`, preenche somente entradas vazias e recusa substituir uma referência já editada por outra. Mantém os GUIDs dos recursos existentes e registra a cobertura real no CSV, sem reclassificar os três pilotos como os únicos itens ativos.

## Revisão e próxima avaliação

Inspeção individual dos PNGs e revisão estática dos IDs, referências, transparência, dimensões e configurações de importação. Sem alteração de C# nesta etapa, sem nova compilação semântica, sem Unity, testes automatizados ou builds.

Resultado: 59 IDs distintos, 14 referências de interface, zero substituições de mundo e lote A completo. Os onze PNGs novos são RGBA de 1254×1254, com transparência; suas cópias de importação são idênticas aos originais. Nenhum pixel com alpha superior a 128 toca a borda externa. Conferidas as três configurações de plataforma com limite 512 e compressão desativada, os GUIDs e os onze prompts. A tabela de cobertura mantém 45 itens pendentes.

[Galeria atualizada](../Previews/ItemIconsV2/index.html) com 32, 48, 64, 96 e 128 pixels CSS e os dois fundos. Ela não simula a importação 512 nem o Canvas Unity.

Na avaliação prática: comparar especialmente os três peixes capturados, assado/queimado/cru, metal/madeira e componentes/munição; verificar contadores, receitas, comerciante e arraste nos dois temas. Objetos no mapa devem continuar com sua apresentação anterior. A próxima família de produção é o lote B: oito armas/munições restantes, preservando a Pistola piloto.
