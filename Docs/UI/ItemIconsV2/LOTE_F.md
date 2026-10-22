# Lote F — equipamentos de treinamento

Continuação da produção autorizada em 19/10/2026. Seis sprites de treino originais, seguindo o acabamento pintado medieval sombrio com materiais humildes e silhuetas próprias. A arte não adiciona propriedades ou funções aos itens.

| ItemId | Identificação visual |
| --- | --- |
| TrainingHead | Touca acolchoada de tecido cru com abas curtas |
| TrainingTorso | Colete acolchoado sem mangas e fechos simples |
| TrainingHands | Par de luvas acolchoadas com compartimento arredondado para dedos |
| TrainingFeet | Par de calçados baixos de couro e tecido |
| TrainingAmulet | Pingente circular de madeira com furo e alça curta |
| TrainingRing | Aro simples de madeira sem engaste |

As peças de treino se distinguem dos equipamentos comuns por construção e forma: não recebem as placas, pedras ou emblemas dos modelos anteriores. Mantidos os identificadores e nomes reais do jogo, sem inventar modelos adicionais. Estes itens continuam pertencendo ao ambiente de teste; nenhuma regra de disponibilidade foi alterada.

## Fontes e integração

Sprites individuais gerados pela ferramenta integrada com a skill imagegen. Referência de acabamento: `Art/ItemIconsV2/Originals/ScoutJerkin.png`. Prompts completos em `Art/ItemIconsV2/<ItemId>-prompt.txt`; fontes registradas em `Art/ItemIconsV2/lote-f.json`. Originais em `Art/ItemIconsV2/Originals/`, copiados sem edição de pixels para `Assets/Resources/Items/IconsV2/`.

Somente InterfaceIcon é preenchido. Preservados WorldIcon, fontes C#, definições, regras, saves e protocolo. Geometria da HUD, perfis, personalizações, placeholders e opacidades dos slots vazios permanecem.

Importação: Sprite Single, transparência, Bilinear, Clamp, sem mipmaps nem compressão; limite 512 em Default/Standalone/Android. A aprovação visual sem compressão precede qualquer ASTC.

## Revisão estática

Seis PNGs finais inspecionados individualmente: RGBA de 1254×1254, alpha de 0 a 255, sem pixels com alpha superior a 128 tocando a borda externa. Conferidas cópias idênticas aos originais, GUIDs e parâmetros de importação. Catálogo com 59 IDs e 59 referências de interface, zero substituições de mundo; todos os lotes completos.

Comparação SHA256 de 314 arquivos confirmou a preservação das 207 fontes C#, dos 53 sprites/metadados anteriores e dos padrões da HUD. Revisão semântica não foi repetida sem alterações C#.

## Avaliação prática e fechamento

[Galeria de arte](../Previews/ItemIconsV2/index.html) em 32, 48, 64, 96 e 128 pixels CSS, com fundos Simples/Sombria. A galeria não representa Canvas ou importação Unity. Conferir as peças no ambiente de teste, seleção/arraste, contadores e detalhes em compacto/amplo. Confirmar que não entram na vitrine normal ou nas partidas normais.

A conclusão dos lotes de produção não substitui a avaliação prática. Compressão Android, avaliação em aparelhos e retirada de fallbacks de interface continuam posteriores; os resolvedores antigos de mundo precisam permanecer para preservar a arte ambiental. Sem Unity, testes automatizados do jogo ou builds nesta entrega.
