# Ajustes da cabana após comparação visual — 11/10/2026

Relato do usuário: fogão/bancada excessivamente planos, personagem pequeno e parecendo caminhar sobre a parede. Capturas comparadas ao conceito aprovado.

## Causas identificadas

- Fogão v2 e bancada quebrada v2 retiravam o corpo dos objetos para mostrar somente suas superfícies superiores. Isso contrariava a aparência inclinada do conceito.
- `CabinArt.Place` aplicava escalas X/Y distintas. O player era forçado a um quadrado de 1,15 × 1,15 apesar da silhueta original alongada, e outros móveis também podiam sofrer deformação.
- Os colliders das paredes cobriam linhas finas perto do topo pintado, deixando parte da faixa visível da parede transitável.

## Correções

- Fogão restaurado a partir de `Assets/Art/Cabin/wood-stove-topdown-v1.png`, com frente e corpo visíveis, na cópia de runtime `Assets/Resources/Cabin/Stove.png`. GUID e importador da cópia preservados.
- Nova bancada `Assets/Art/Cabin/ammo-bench-broken-high-angle-v3.png`, gerada com image_gen integrado a partir da primeira bancada e do conceito fornecido pelo usuário. Inclinação alta moderada, mantendo estrutura, pernas e dano; runtime usa `BenchBrokenAngled.png`.
- Sprites mantêm proporção com escala uniforme por padrão. Apenas a base do mapa usa as dimensões exatas do layout.
- Player com altura máxima de 1,85 unidades, antes 1,15; largura acompanha a proporção original. Collider circular de raio 0,65 no pawn local da cabana. Nenhuma mudança no player de combate/rede.
- Cama reposicionada e reduzida a 4 unidades de comprimento; bancada deslocada para dentro, preservando espaço junto da porta.
- Colisões das paredes ocupam as faixas completas, até o encontro com o chão. Retângulos em coordenadas da imagem `CabinBase` 1672×941: fundo (286,90)–(858,174), esquerda (286,90)–(340,560), direita (808,90)–(858,560), frente esquerda (286,475)–(553,558), frente direita (654,475)–(858,558). A passagem entre x=553 e x=654 continua aberta.
- Originais e versões anteriores preservados. A base do cenário não foi redesenhada. As novas mecânicas continuam para outra etapa; rótulos fixos continuam removidos no código atual.

## Verificação direcionada

Após o relato, `CabinContactChecks.Run` executado em cópia isolada Unity 6000.0.84f1: **8 verificações passaram**. Simulação física 2D de contato com lados/fundo/frente das paredes, contato externo à parede direita, passagem da porta com o collider maior, escala uniforme e dimensões do player.

Log: `Logs/skills-implementation/SkillsValidation-CabinContactChecks-Run-20261011-134103.log`. Nenhum cliente, servidor ou build foi iniciado. Essa prova não renderiza a cena nem valida a aparência final no aparelho; a conferência visual continua a cargo do usuário no Play atualizado.

## Prompt da bancada

Use case: precise-object-edit. Image 1 is the ORIGINAL broken workbench sprite to EDIT; image 2 is the cabin game concept as CAMERA/STYLE reference. Adjust ONLY the viewing inclination of image 1 to better match furniture in image 2. Keep the full THREE-DIMENSIONAL WOODEN BENCH, NOT just a detached tabletop. Use a high overhead camera approximately 70–75 degrees above the ground, about 15–20 degrees away from vertical, similar to the cabin reference. The tabletop is dominant but the FRONT APRON, SHORT FORESHORTENED LEGS and supporting cross brace are STILL CLEARLY VISIBLE. Shorten their visible projection compared with original, so it no longer looks like a frontal elevation. Do not erase those structural parts. Preserve the same rectangular wide horizontal footprint, split aged boards, missing right-hand top planks and collapsed corner, rusty vise at left, same hammer and two empty brass cases, coarse gritty wood texture, earthy brown and muted moss. Still a broken unusable ammo workbench. Parallel mostly-horizontal tabletop edges, slight natural overhead depth, no dramatic isometric diagonals. Individual game sprite isolated with TRUE transparent alpha background and no floor, ground patch or brown rectangular backdrop; also transparent through missing boards. No cast shadow outside object, no new tools, no text, no human. Full bench entirely contained with generous transparent margins. This is a modest camera-angle correction of existing bench, not a redesign.

