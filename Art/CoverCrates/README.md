# Caixas de cobertura — arte original v1

Sprite criado com a ferramenta integrada ImageGen em 12/10/2026. Fonte preservada: `cover-crate-v1.png`. Asset de runtime: `Assets/Resources/Environment/CoverCrate-v1.png`.

Uma caixa de carga fechada de madeira envelhecida, vista ortográfica diretamente de cima, sem fundo, com tábuas, reforço diagonal, cantoneiras de ferro e musgo discreto. Atmosfera de abandono inspirada em Darkwood; silhueta e leitura de cobertura inspiradas em Bullet Echo. Arte original, sem copiar assets desses jogos. Não é um baú de saque.

`CoverCrateArt` aplica o sprite em um filho visual de `Crate1`–`Crate4`, `WorkshopCrate`, `StoreCrate`, `SupplyCrate` e `Cover`, criados por `MapBuilder.Block`. Mantém os colliders, dimensões, camadas, posições e função dos objetos existentes. Retângulos usam a mesma arte ajustada ao seu tamanho. Se a textura não estiver disponível, o visual anterior permanece. Processos batchmode não carregam a arte.

A transparência foi gerada pelo ImageGen. O arquivo PNG original foi copiado sem edição. O sprite recorta as margens transparentes por UV normalizada para aproximar a silhueta da área de colisão, inclusive quando o importador reduz a textura por plataforma.

## Prompt final usado

Create one production-ready original 2D environment sprite for Cyber Extraction, a top-down forest survival extraction game. Asset: a CLOSED wooden cargo crate used as solid combat cover, NOT a treasure chest. Strict orthographic directly overhead view (camera looking straight down at its square lid), square footprint, edges aligned horizontally/vertically, absolutely no isometric perspective or visible tall front face. One single crate centered with approximately 6% transparent padding, fills most of image. Aged weathered gray-brown oak planks, stout perimeter wooden rails and a diagonal reinforcement plank, dark iron corner straps and small rusty nail heads; tasteful cracks, scuffs, damp stains, subtly mossy seams. Hand-painted realistic game asset, nuanced subdued texture, woodland abandonment atmosphere inspired by Darkwood with readable strong cover silhouette inspired by Bullet Echo, original artwork. Neutral soft overhead ambient lighting, very restrained relief and depth within lid, no cast shadow outside footprint, no ground, no scene, no lettering, no symbols, no loot contents, no lock, no handles, no UI, no glow. True transparent background and clean alpha edges. At small in-game sizes retain clear plank divisions and silhouette; avoid excessive fine noisy texture. Output a single square sprite at high resolution.

Revisão visual da imagem e revisão estática da integração. Sem testes, builds ou sessão Unity nesta entrega.
