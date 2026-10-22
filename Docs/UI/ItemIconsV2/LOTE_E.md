# Lote E — moeda e tesouros

Continuação autorizada pelo usuário em 19/10/2026. Seis sprites novos, mantendo o acabamento pintado medieval sombrio e a identidade dos itens existentes.

| Item | Identificação visual |
| --- | --- |
| Moeda antiga | Disco de bronze com borda gasta e relevo central |
| Relicário | Pequeno corpo de bronze, argola superior e recesso circular |
| Ídolo funerário | Figura de pedra austera em pedestal baixo |
| Sucata valiosa | Engrenagem de aço com eixo e encaixe de bronze |
| Objeto antigo | Relógio de bolso de bronze com mostrador marfim |
| Documento de coleção | Pergaminho envelhecido com selo de cera |

O relógio preserva a representação anterior de Objeto antigo, sem criar um novo item. A moeda é um tesouro de inventário, não uma alteração do saldo de moedas. Não foram adicionados textos legíveis, quantidades, preços ou molduras nas imagens; informações funcionais continuam na interface.

## Fontes e integração

Geração individual com a skill imagegen e ferramenta integrada; referência de acabamento no Elmo do Guardião piloto. Prompts completos em `Art/ItemIconsV2/<ItemId>-prompt.txt`; proveniência em `Art/ItemIconsV2/lote-e.json`. Originais preservados em `Art/ItemIconsV2/Originals/`, copiados sem edição de pixels para `Assets/Resources/Items/IconsV2/`.

A integração preenche somente InterfaceIcon no catálogo visual. WorldIcon continua vazio, preservando as representações no mundo. Não há alteração de regras, preços, economia, atributos, saves ou protocolo. Tamanhos, perfis, posições personalizadas, controles e placeholders dos slots vazios permanecem.

Importação: Sprite Single, alpha, Bilinear, Clamp, sem mipmaps ou compressão; limite 512 em Default/Standalone/Android. ASTC e retirada do fallback ficam posteriores à avaliação visual e à cobertura completa.

## Revisão estática

Seis PNGs inspecionados individualmente: RGBA de 1254×1254, alpha de 0 a 255 e nenhum pixel com alpha superior a 128 tocando a borda externa. Conferidas as referências e as configurações de importação; cópias de uso idênticas aos originais preservados. Catálogo com 59 IDs, 53 substituições de interface e zero de mundo; A/B/C/D/E completos.

Comparação SHA256 de 302 arquivos confirmou a preservação das 207 fontes C#, dos 47 sprites/metadados anteriores e dos padrões da HUD. Não foi repetida revisão semântica sem alterações de scripts. A inspeção de assets não comprova a aparência no aparelho.

## Avaliação prática

[Galeria de arte](../Previews/ItemIconsV2/index.html) em 32, 48, 64, 96 e 128 pixels CSS com fundos Simples/Sombria. Não simula Canvas, importação 512 ou interação Unity. Conferir principalmente moeda versus relógio, relicário versus medalhão e a leitura do documento e da sucata nos slots compactos; observar contadores, detalhes e arraste.

Sem Unity, testes automatizados do jogo ou builds. Próxima produção: lote F, seis equipamentos de treinamento.
