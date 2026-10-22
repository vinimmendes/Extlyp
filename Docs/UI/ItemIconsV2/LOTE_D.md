# Lote D — equipamentos e acessórios

Continuação da produção autorizada pelo usuário em 19/10/2026. O lote contém 18 modelos: o Elmo do Guardião piloto preservado e 17 sprites novos. A geração segue o piloto como referência de acabamento; a autorização para continuar não equivale a validação visual em execução.

| Região | Modelos e diferenças visuais |
| --- | --- |
| Cabeça | Capuz do Vigia em tecido oliva, Elmo do Guardião em aço, Máscara do Atirador em couro |
| Torso | Gibão do Batedor acolchoado, Cota do Combatente de malha, Couraça do Guardião de placas |
| Mãos | Luvas do Atirador ajustadas, Manoplas do Combatente articuladas, Luvas do Explorador com punhos longos e reforços |
| Pés | Botas do Andarilho com cadarços, Botas do Duelista com cano dobrado, Grevas do Guardião em aço |
| Amuleto | Medalhão do Olho redondo, Talismã do Fôlego em losango, Presa do Combatente curva |
| Anel | Mão Firme com engaste em losango, Pulso Forte com placa retangular, Persistência com engaste em escudo |

Objetos vazios, sem personagem, mãos ou pernas dentro das peças. Materiais envelhecidos, luz marfim discreta e leve inclinação; sem moldura, números, nomes, preços ou raridade gravados na imagem. A raridade continua sendo informação da interface. As diferenças de silhueta permitem distinguir os modelos sem depender somente da cor.

## Fontes e integração

Sprites individuais gerados com a skill imagegen e ferramenta integrada. Prompts completos em `Art/ItemIconsV2/<ItemId>-prompt.txt`, proveniência em `Art/ItemIconsV2/lote-d.json`, referência de estilo em `Art/ItemIconsV2/Originals/GuardianHelm.png`. Originais preservados em `Art/ItemIconsV2/Originals/`; cópias de uso em `Assets/Resources/Items/IconsV2/`, sem edição de pixels por código.

A integração preenche InterfaceIcon no catálogo visual existente. Não troca WorldIcon, as definições dos equipamentos ou seus atributos. IDs, exemplares, economia, saves e protocolo são preservados. Também permanecem os placeholders dos slots vazios, as métricas, os tamanhos e as preferências de organização da HUD.

Importação: Sprite Single, transparência, Bilinear, Clamp, sem mipmaps nem compressão, limite 512 em Default/Standalone/Android. ASTC e retirada do fallback continuam posteriores à avaliação visual e à cobertura completa.

## Revisão estática

Os 17 PNGs finais foram inspecionados individualmente: RGBA de 1254×1254, alpha de 0 a 255, sem pixels com alpha superior a 128 tocando a borda externa. Cópias de uso idênticas aos originais preservados; metadados e GUIDs conferidos. O catálogo tem 59 IDs, 47 substituições de interface e zero de mundo. A/B/C/D completos; restam E=6 e F=6.

Na revisão visual, a primeira máscara foi corrigida para se distinguir do elmo; medalhão e presa receberam correção de enquadramento dos cordões. As fontes iniciais e os prompts das correções estão registrados no manifesto, sem edição de pixels por código.

Comparação SHA256 de 268 arquivos confirmou a preservação das 207 fontes C#, dos 30 sprites/metadados anteriores e dos padrões da HUD. Não foi repetida a revisão semântica sem mudanças de scripts. Nenhuma validação em execução foi realizada.

## Avaliação prática

[Galeria de arte](../Previews/ItemIconsV2/index.html) em caixas de 32, 48, 64, 96 e 128 pixels CSS, nos fundos Simples/Sombria. A galeria não simula Canvas ou importação Unity. No aparelho, conferir principalmente a distinção entre os três pares de luvas, botas, amuletos e anéis; seleção, comparação e arraste; contadores e margens nos layouts compacto/amplo. Equipamentos equipados continuam exclusivos da aba Personagem.

Sem iniciar Unity, testes automatizados do jogo ou builds. Próximo lote: E, seis moedas/tesouros; depois F, seis equipamentos de treinamento.
