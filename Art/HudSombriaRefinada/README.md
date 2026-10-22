# Cyber Extraction — Sombria refinada

Produção de 19/10/2026. Referência permanente: `Docs/UI/DIRECAO_DE_ARTE_HUD.md` e HUD Sombria aprovada. Assets originais; sem cópia de interfaces de outros jogos.

## Arquivos

- `Sources/`: SVG editável dos 20 símbolos e das superfícies geométricas.
- `Exports/`: PNG separados com transparência. Button e Fire são originais raster gerados; os demais são exportações dos SVG.
- `manifest.json`: regiões e bordas, também em `Assets/Resources/UI/RefinedHud/Visuals.json`.
- `Assets/Resources/UI/RefinedHud/Atlas0.png`: superfícies; `Atlas1.png`: símbolos.
- `Docs/UI/Previews/HudSombriaRefinada/index.html`: composição interativa e família de ícones usando recortes dos atlas de produção.

As imagens de geração maiores ficam fora de Assets para não duplicar memória no jogo. Os estados usam a mesma superfície, tintas do Button e preenchimento radial. As fontes e os números permanecem em TMP. Painéis usam borda de 12 pixels no atlas, correspondente a 4 dp na apresentação; símbolos não são esticados em nove partes.

O exportador preserva os arquivos de geração e produz dois atlas RGBA32, 1024 × 1024, sem mipmaps, sem readback: 8 MiB nominais de textura. Padding de oito pixels nas bordas e dezesseis entre regiões. Não foi medido consumo em execução.

## Geração raster — ferramenta integrada imagegen

Sem CLI/API externa. Prompts usados, em inglês para manter o registro exato:

**Button.png**

> Create ONE production game UI sprite: an isolated perfectly front-facing circular dark olive leather/blackened bronze button BACKPLATE with a very restrained matte weathered copper bevel. Darkwood atmospheric survival aesthetic, refined and functional, not fantasy jewelry. Crisp circular silhouette, rim only 3 percent of diameter, center near black olive with extremely subtle material, soft shallow volume. No icon, no letters, no ornaments, no background, no cast shadow outside silhouette. Asset centered occupying 94% of square canvas. Genuinely transparent outside. Must remain clean at 48dp. This is the neutral secondary button base for Cyber Extraction mobile HUD, independent icon will be layered on it by Unity.

**Fire.png**

> Production UI sprite for original dark survival game Cyber Extraction, Sombria refined family. ONE circular primary FIRE button BACKPLATE, perfectly front facing, centered occupying 96% of square canvas. Genuine transparency outside circle. Dark olive-black leather center, restrained matte copper raised rim with warm bronze inner bevel and soft volume. Rim roughly 4 percent diameter, simple flawless circle, very subtle age texture, no ornaments, no engraving, no symbols, no letters, no text, no weapon, no glow, no external shadow. Center must remain dark for separate ivory icon/text overlay. Clean crisp silhouette readable at72logicalpixels. Copper rim more prominent than a secondary button but sober rather than shiny gold fantasy.

**cenario-preview.png — somente documentação, edição da captura fornecida**

> Edit target is the supplied screenshot. Produce a clean gameplay environment plate for a UI design preview. Remove ALL existing HUD: every bar, button, joystick, text, map, weapon panel, gear, clock, and tools panel. Remove the dark editor margins by filling the entire landscape canvas with the actual game world. Preserve the top-down stone catacomb walls, floors, wooden crate, central survivor, flashlight lighting, perspective, pixelated painterly game aesthetic and camera scale. Reconstruct only the small areas under the removed HUD with matching floor/walls. Do not redesign the level or character. Output full-width landscape roughly 2:1. No text, no UI, no border, no new actors. This plate is preview-only; no changes to gameplay art assets.

A arte de cenário reconstruída é ilustrativa e não é um asset de terreno nem uma captura atual da Unity. A cartografia das prévias HTML também é ilustrativa; em execução, a textura continua derivada da geografia real do mapa.

## Exportação e reprodução

`Tools/UI/Build-RefinedHudArt.cjs` exporta os SVG e empacota os atlas com Sharp já disponível no ambiente. `Tools/UI/Prepare-RefinedHudMetadata.py` prepara somente os metadados Unity, sem importar ou iniciar o Editor. `Tools/UI/Preview-RefinedHud.cjs` renderiza o documento local com o Edge em segundo plano, usando fontes Barlow e os recortes dos mesmos atlas.

Nenhum desses comandos inicia Unity, testes do jogo ou builds. O desenho é revisável, mas a qualidade no aparelho e o comportamento de toque ainda precisam da avaliação prática do usuário.
