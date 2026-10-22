# Ícones V2 — revisão final da integração

Revisão estática de 19/10/2026, após os seis lotes de produção. Os 59 itens possuem sprite de interface individual. Aparência em execução e compressão Android ainda aguardam avaliação prática.

## Cobertura conferida

| Apresentação | Caminho visual |
| --- | --- |
| Mochila, baú, equipamento e saque uGUI | UguiInventorySlot → ItemIconLibrary.Get |
| Mercadorias | UguiInventoryPresenter.Commerce → ItemIconLibrary.Get |
| Arraste | UguiInventoryPresenter → ItemIconLibrary.Get |
| Preparativos e recursos da base | UguiBaseHub → ItemIconLibrary.Get |
| Recursos e receitas das atividades | UguiCampPresenter → ItemIconLibrary.Get |
| Interfaces IMGUI de itens | ItemIconGui → ItemIconLibrary.Get |
| Mostrador de arma uGUI | ItemVisualCatalog.Interface, sem alterar ícones de ação |
| Mostrador de arma legado | Quickbar → ItemIconGui → ItemIconLibrary.Get |
| Objetos no mapa | ItemIconLibrary.GetWorld e resolvedores anteriores |

Na revisão foi encontrado um consumidor restante da arte antiga: Quickbar desenhava armas corpo a corpo por MeleeWeaponArt e armas de fogo por HudArt. Agora usa ItemIconGui no mesmo retângulo 34×34 existente. Não foram alterados hitboxes, nomes, valores, perfis, preferências ou controles de combate. Os símbolos funcionais do disparo, inclusive o caminho legado de TouchControls, continuam separados das imagens de inventário.

## Inspeção do catálogo completo

Comando de inspeção de arquivos, sem engine ou execução de regras do jogo:

```text
python Tools/UI/Inspect-ItemIconsV2Batch.py all
```

Resultado: 59 IDs sequenciais, 59 referências de interface com GUIDs distintos, zero substituições de mundo. Para todos os itens foram conferidos arquivo original, cópia de uso idêntica, prompt, metadados, referência correspondente, RGBA, dimensões de pelo menos 1024 e alpha de 0 a 255. Nenhuma região com alpha superior a 128 toca as bordas externas. Isso verifica enquadramento, não garante leitura perceptiva nos menores tamanhos.

Todos conservam Sprite Single, alpha, Bilinear, Clamp, mipmaps desativados, limite 512 em Default/Standalone/Android e compressão desativada. O modo `all` usa os originais do projeto e funciona sem o cache externo da geração. Inspeções por lote continuam disponíveis.

## O que ainda depende da avaliação prática

1. Conferir todos os itens em compacto/amplo e Simples/Sombria, especialmente munições, ferramentas alongadas e modelos próximos de equipamento.
2. Verificar contadores, seleção, comparação, arraste, loja, receitas e mostrador de arma no jogo.
3. Conferir celular horizontal pequeno, recorte lateral, 16:9, tela alongada, ultrawide e desktop de alta densidade.
4. Confirmar que os itens de treinamento continuam exclusivos do teste e que os objetos no mapa mantêm sua arte anterior.
5. Após aprovação sem compressão, aplicar e avaliar ASTC 4×4 no Android, preservando exceções sem compressão quando houver perda perceptível.

O fallback de interface permanece defensivo até essa avaliação. Não se deve apagar GetLegacy ou as artes procedurais indiscriminadamente: GetWorld ainda depende delas para preservar a representação física. Remover essa dependência exigiria uma migração de arte do mundo que não faz parte da renovação dos ícones.

Não foram iniciados Unity, testes automatizados do jogo ou builds. A mudança pontual de Quickbar recebeu leitura estática do fluxo e do uso do helper existente; não foi compilada ou validada em execução. Nenhum ganho de desempenho ou memória foi medido.
