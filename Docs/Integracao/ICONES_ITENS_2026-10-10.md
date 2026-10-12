# Integração dos ícones de itens — 10/10/2026

Os 18 ícones aprovados estão referenciados por `Assets/Resources/ItemIcons.asset`, em ordem de `ItemId`. Os PNGs originais em `Assets/Art/Items` foram preservados. O importador usa Sprite/Single, alpha, filtro bilinear, sem mipmaps, tamanho máximo de 512 e sem compressão. `ItemIconSetup.Prepare` permite refazer a configuração sem reconstruir cenas ou prefabs.

## Locais integrados

- Mochila e baús: desenho, nome, quantidade, munição da arma e qualidade; mesmas áreas de arraste e transferência.
- Item arrastado: prévia do ícone e quantidade.
- Barra rápida: bandagem e arma equipada, mantendo atalhos, cooldown e condições dos botões.
- Bancada: ícones dos três tipos de munição nos botões de fabricação.
- Fogueira: ícones dos peixes na seleção para cozinhar/comer.
- Loot na expedição e itens largados no acampamento: sprite branco sem tintura sobre a arte original; baús preservados.

`ItemIconLibrary.ApplyWorld` adiciona um visual filho, ajusta seu tamanho e esconde o quadrado do pai. A escala, o collider e as regras de coleta do objeto pai não são alterados. Arte de inventário é reutilizada no chão, inclusive armas em perfil; não foi gerada outra perspectiva.

O catálogo armazena somente referências visuais. Inventário, progresso, receitas, quantidades e autoridade do servidor permanecem nos sistemas existentes. Ícone ausente mantém a abreviação na GUI ou o quadrado no mundo como fallback.

## Evidências do código atual

- Unity 6000.0.84f1: importação do catálogo com as 18 referências concluída, log `Logs/item-icons/prepare.log`.
- Compilação de scripts e build Windows da cena isolada de prévia concluídos.
- `ItemIconChecks.Run`: passou para os 18 mapeamentos, transparência, visual de loot e preservação de escala/raio/trigger de coleta; também verifica rejeição de identificador inválido.
- Log: `Logs/skills-implementation/SkillsValidation-ItemIconChecks-BuildPreview-20261010-144528.log`.
- Cena/build de prévia foram criados somente na cópia `Builds/SkillsValidation`; nenhuma cena normal foi recriada.

A execução gráfica das prévias em 1280×720 e 800×360 não foi autorizada. Não há capturas válidas deste código nem confirmação visual em Play Mode, clientes conectados ou Android. Isso não é apresentado como teste de multitouch, rede, APK ou desempenho. O executável normal do jogo não foi reconstruído por esta integração.

## Repetição das verificações

No Editor, usar `Cyber Extraction > Itens > Verificar icones` depois de `Preparar icones` se necessário. Para gerar a prévia descartável em uma cópia isolada:

```powershell
./Tools/Validate-Unity.ps1 -Method ItemIconChecks.BuildPreview -Graphics
```

O componente `ItemIconPreview` não está nas cenas normais. Ele renderiza os 18 itens pela interface real `InventoryWindow` e pelo helper de loot, com inventário transitório de teste. O build resultante fica em `Builds/SkillsValidation/Builds/ItemIconPreview/ItemIconPreview.exe`.
