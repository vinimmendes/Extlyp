# Sprites de slots de equipamento vazios

Atlas original: `Assets/Resources/UI/Art/EmptyEquipmentSlots.png`, gerado com imagegen em 2026-10-18. PNG com transparência preservada, importação sem mipmaps e sem compressão, limite 1024. Fonte gerada preservada em `C:/Users/Vinni/.codex/generated_images/01a14ec1-3f32-7202-83b6-27ff99fb7d09/exec-bfc78a22-8bea-4db0-bcf6-025424d8c10b.png`.

Cartela 4×2. Linha superior: cabeça, torso, mãos, pés. Linha inferior: amuleto, anel, arma A e arma B. `EmptyEquipmentSlotArt` recorta e mantém em cache os oito sprites a partir do bitmap. Não desenha ícones proceduralmente e não usa artes de itens como substitutos.

`UguiInventorySlot.PresentEquipment` aplica cinza RGB 0,72 e opacidade 18% somente quando o slot está vazio. A mesma apresentação vale para Simples e Sombria. Os ícones dos itens presentes mantêm cores e opacidade normais; mochila e baú vazios continuam sem ícone. Rótulos e foco permanecem legíveis.

Prompt: Create ONE production UI sprite atlas for an atmospheric dark survival game. Transparent background, aspect ratio 2:1, exactly four columns and two rows of equally sized square cells. No text, labels, cell borders or background. Top row: hood/head armor, torso tunic armor, gloves, boots. Bottom row: pendant amulet, finger ring, pistol sidearm, short sword. Centered monochrome pale gray silhouettes with transparent spacing, restrained hand-painted survival inventory style, no color or glow. These indicate EMPTY equipment slots.

Revisão estática; sem iniciar Unity, testes automatizados ou builds. A legibilidade final em aparelho depende da avaliação prática.

## Arma A — besta de uma mão

O slot vazio de arma A usa agora o bitmap dedicado `Assets/Resources/UI/Art/EmptyEquipmentHandCrossbow.png`: besta leve de uma mão, cinza e com a mesma opacidade de 18%. A célula antiga da pistola no atlas não é usada. Os outros sete sprites permanecem idênticos.

Prompt da substituição: Single square transparent UI icon of a compact light one-handed crossbow, diagonal, visible transverse bow limbs and taut string, short bolt rail and small grip, no shoulder stock. Match monochrome neutral gray hand-painted shading of the original atlas. No text, border, glow or shadow. Transparent margin.
