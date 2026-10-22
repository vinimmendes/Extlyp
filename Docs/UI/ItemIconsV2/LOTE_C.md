# Lote C — ferramentas, chaves e extração

Continuação autorizada em 19/10/2026. Oito sprites individuais, mantendo a linguagem pintada medieval sombria, materiais envelhecidos e leve inclinação dos lotes anteriores.

| Item | Identificação visual |
| --- | --- |
| Gazua | Haste fina com ponta em gancho e empunhadura de couro |
| Alicate de corte | Duas empunhaduras de madeira e mandíbulas articuladas |
| Pé de cabra | Barra de ferro com garra curva e ponta achatada |
| Serra manual | Lâmina larga dentada e cabo fechado de madeira |
| Chave de serviço | Latão envelhecido e argola redonda |
| Chave do depósito | Ferro frio e argola quadrada |
| Manivela dos coveiros | Eixo deslocado com encaixe e manopla de madeira |
| Chave do canal | Argola grande, haste grossa e dentes profundos de ferro |

Geração individual com a skill imagegen e ferramenta integrada. Referência de acabamento: Pistola piloto. A manivela e a chave do canal conservam as características principais dos sprites anteriores das catacumbas. Prompts em `Art/ItemIconsV2/<ItemId>-prompt.txt`; fontes registradas em `Art/ItemIconsV2/lote-c.json`. Originais em `Art/ItemIconsV2/Originals/`, copiados sem edição de pixels para `Assets/Resources/Items/IconsV2/`.

Somente as referências InterfaceIcon são preenchidas. WorldIcon permanece vazio: os objetos de extração, representações no mapa e suas escalas continuam usando as fontes anteriores. Slots vazios, controles, métricas da HUD, preferências e geometria dos menus são preservados. Não há alterações de regras, identificadores, saves ou protocolo.

Importação: Sprite Single, transparência, Bilinear, Clamp, sem mipmaps e sem compressão; limite 512 em Default/Standalone/Android. ASTC continua pendente de avaliação visual. O fallback continua disponível para os itens ainda não renovados.

## Revisão estática

Os oito sprites foram inspecionados individualmente: RGBA de 1254×1254, alpha de 0 a 255, sem pixels com alpha superior a 128 tocando as bordas externas. Conferidos metadados de importação e igualdade das cópias com os originais. O catálogo mantém 59 IDs, 30 referências de interface e zero substituições de mundo. A/B/C completos; restam D=17, E=6 e F=6.

Comparação SHA256 de 252 arquivos confirmou a preservação das 207 fontes C#, dos 22 sprites/metadados anteriores e dos padrões da HUD. O inspetor `Tools/UI/Inspect-ItemIconsV2Batch.py` é somente de leitura e não inicia a engine.

## Avaliação prática

[Galeria](../Previews/ItemIconsV2/index.html) com caixas de 32, 48, 64, 96 e 128 pixels CSS e fundos Simples/Sombria. Não representa uma captura ou a filtragem da Unity. Conferir especialmente as três chaves, as pontas da gazua e do pé de cabra, os dentes da serra e o encaixe da manivela. Observar seleção, arraste e contadores no aparelho.

Não foram iniciados Unity, testes automatizados do jogo ou builds. Próximo lote: D, com 17 equipamentos e acessórios restantes; o Elmo do Guardião já integra o piloto.
