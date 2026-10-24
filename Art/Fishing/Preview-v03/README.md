# Pesca v03 — peixe na faixa horizontal

Revisão solicitada pelo usuário: preservar o visual v02 e a orientação horizontal da barra; retornar à mecânica de manter o peixe dentro de uma faixa móvel. Segurar desloca a faixa para a direita; soltar desloca para a esquerda. O progresso aumenta dentro da faixa e diminui fora. O medidor de tensão, o desgaste da linha e as instruções de ceder/recolher foram retirados desta proposta.

Minhoca, lançamento, visitantes que investigam e recusam, mordida, fisgada manual e resultado continuam na composição. A dificuldade futura deve vir da largura da faixa e do comportamento do peixe, conforme os perfis atuais; os parâmetros de rompimento da linha propostos anteriormente deixam de se aplicar.

`fishing-preview-v03.blend` contém o storyboard inteiro em uma nova cena, preservando as versões anteriores. `fish-in-zone-v03.gif` mostra somente cerca de seis segundos da disputa revisada. `concept-somber.png` e `concept-simple.png` mostram o mesmo momento nos dois temas. Prévia produzida no Blender 5.2.2 LTS, com câmera ortográfica e quadros originais 1280 × 720.

A trajetória de acompanhamento e as falhas breves são dirigidas para demonstrar a apresentação. Não são validação de dificuldade nem uma partida executada em Unity. A mecânica original de faixa já permanece no código Unity: a proposta de tensão v02 nunca foi integrada. O loop novo e a minhoca ainda dependem da etapa de integração.

Autoria: `Tools/Blender/FishingPreviewV03.py`, reutilizando a composição v02 de forma versionada. Renderização: `RenderFishingPreviewV03.py`. Empacotamento: `PackageFishingPreviewV03.py`. Nenhuma alteração em `Assets`, teste automatizado, sessão Unity ou build nesta etapa.
