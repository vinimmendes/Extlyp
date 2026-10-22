# Monstros escolhidos e baú — animações Blender

Entrega em 12/10/2026: doze modelos originais construídos e animados individualmente no Blender 4.5, baseados nos visuais escolhidos pelo usuário. Geometria e materiais foram simplificados em relação às ilustrações; os GIFs vêm dos renders reais dos modelos, não de imagens IA deformadas.

[Galeria local](../../Prototypes/MonsterAnimationBatch/index.html) · [Catálogo dos clipes](../../Prototypes/MonsterAnimationBatch/CATALOGO.md)

| Modelo | Clipes | Arquivo editável |
| --- | --- | --- |
| Baú de viagem surrado | Open, Close | [Modelo](../../Prototypes/MonsterAnimationBatch/Chest/Chest.blend) |
| Imitador · Mordida rasteira | Walk, Reveal, Bite, Disguise | [Modelo](../../Prototypes/MonsterAnimationBatch/Mimic/Mimic.blend) |
| Charger · Ombros ossudos | Walk, Charge, Attack | [Modelo](../../Prototypes/MonsterAnimationBatch/Charger/Charger.blend) |
| Ouvinte · Escutador | Walk, Listen, Attack | [Modelo](../../Prototypes/MonsterAnimationBatch/Listener/Listener.blend) |
| Carapaça · Enrolado | Walk, Ram, Attack | [Modelo](../../Prototypes/MonsterAnimationBatch/Shell/Shell.blend) |
| Arauto · Costelas ressonantes | Walk, Shout, Attack | [Modelo](../../Prototypes/MonsterAnimationBatch/Herald/Herald.blend) |
| Devorador · Colhedor | Walk, Feed, Growth, Attack | [Modelo](../../Prototypes/MonsterAnimationBatch/Devourer/Devourer.blend) |
| Esporífero · Bolsas dorsais | Walk, Spores, Attack | [Modelo](../../Prototypes/MonsterAnimationBatch/Spore/Spore.blend) |
| Planta-mãe · Nó de raízes | Spawn, Breathe | [Modelo](../../Prototypes/MonsterAnimationBatch/Mother/Mother.blend) |
| Bulbo atacante | Mature | [Modelo](../../Prototypes/MonsterAnimationBatch/AttackBulb/AttackBulb.blend) |
| Bulbo curador | Heal, Breathe | [Modelo](../../Prototypes/MonsterAnimationBatch/HealingBulb/HealingBulb.blend) |
| Lacaio | Walk, Attack | [Modelo](../../Prototypes/MonsterAnimationBatch/Minion/Minion.blend) |

## Construção e revisão

Materiais procedurais e câmeras superior/perspectiva. Membros com dois ossos, keyframes FK e trajetória de apoio linear; retorno elevado separado. Os destinos respeitam o alcance mínimo/máximo dos membros. Controles separados para tampa, cabeça, orelha, costelas, pescoço, bolsas e valvas das plantas. Arquivos salvos na pose de repouso (501), com marcadores dos clipes.

Baú e Imitador usam a mesma construção. O disfarce esconde braços/dentes e utiliza o mesmo PNG fechado do baú comum, evitando até diferenças de ruído entre renderizações. A tampa gira na dobradiça traseira; o fechamento da mordida mantém espaço entre as pontas dos dentes.

Refinamentos após inspeção: laterais da tampa preenchidas, limite mínimo de alcance dos braços recolhidos, apoios do Charger durante preparação, concavidade/material da orelha do Ouvinte, arcos e pescoço suavizados, boca do Colhedor acima do chão, poros acima das bolsas, raízes afiladas e valvas abrindo para fora.

O bulbo atacante e o lacaio usam a mesma geometria-base, preservando continuidade no amadurecimento. A mãe fica estacionária; o curador pulsa a seiva. FX de broto gerado e emissão de cura são demonstrações opcionais, separáveis do corpo na futura exportação.

## Tempos e exportação

Timeline a 24 fps, caminhada 1–32 com fechamento do ciclo em 33. Charger: 3 s de preparação, 0,5 s de avanço, 2 s de recuperação. Arauto: 2 s de preparação, emissão/retorno em seguida. Esporífero: aproximadamente 1,2 s de inflação antes da contração. Alimentação do Colhedor inclui cerca de 2,5 s antes do crescimento/retorno. Amadurecimento do bulbo: 9 s. Ataques curtos precisam ser normalizados ao aviso/dano autoritativo na integração.

Clipes longos têm PNGs de avaliação amostrados a cada dois/seis frames; a animação editável mantém os keyframes em 24 fps. Os índices exatos estão em clips.json. Os GIFs respeitam esses intervalos e acrescentam pausas para avaliação; não são a cadência definitiva do jogo. Folhas de sprites são de avaliação, com células de 256 e páginas até 2048 x 2048. Na exportação de produção, renderizar todos os frames necessários, girar a frente para +X e configurar escala/camadas.

## Limites desta entrega

Modelos, clipes, PNGs, GIFs e catálogos conferidos; isso não valida execução Unity, física, rede ou importação de sprites. Nenhum teste/build/sessão de validação Unity executado. Assets ativos e regras do jogo não foram substituídos nesta etapa de animação Blender; integração destes novos modelos permanece para uma etapa posterior. Os modelos/animações antes integrados do protagonista, aranha, Corrosivo e Rastejante foram preservados.

## Couraçado — integração em 14/10/2026

O Couraçado (`MonsterKind.Shell`) agora usa o novo modelo aprovado nesta conversa, com caminhada v02 (dobra traseira corrigida) e corte frontal em arco. Este modelo substitui o desenho geométrico desse monstro; é uma produção posterior e distinta do “Carapaça · Enrolado” listado acima.

- Assets de execução: `Assets/Resources/Monsters/Shell/Walk.png` (32 frames) e `ArcAttack.png` (27 frames). Atlases RGBA 2048 × 1024, células 256, oito colunas, centro fixo e frente em +X.
- `MonsterAppearance` cria o visual animado nos clientes; mantém a representação geométrica como fallback se os atlases faltarem.
- `MonsterAnimationVisual` acompanha a distância percorrida. O corte entra no frame de impacto (índice 7) quando termina o aviso de melee do servidor; a parte final da varredura é mostrada antes de retomar a caminhada.
- A integração inicial usava 3 unidades percorridas por ciclo no Couraçado. Esse valor foi substituído pela calibração de passada abaixo em 15/10/2026.
- Saúde, colisão, movimento, dano, setor de ataque e resistência frontal permanecem nas regras existentes do servidor. O gesto das placas é visual, sem novos acertos contínuos durante a varredura.
- Apenas os atlases finais foram levados ao projeto. Modelos Blender, fontes de renderização, frames intermediários e previews continuam fora do repositório.

Código revisado por leitura; nenhuma sessão Unity, teste automatizado ou build executado nesta integração. A validação prática no jogo permanece pendente.

## Cadência por passada — 15/10/2026

`MonsterAnimationVisual` e `WeaverVisual` avançam a caminhada pela distância realmente exibida, dividida pelo comprimento do ciclo e pela escala mundial do visual. A velocidade configurada da IA não define o ritmo: desaceleração, colisão e interpolação de rede entram pelo deslocamento observado. A fase permanece nas pausas; o sprite de caminhada fica congelado quando o deslocamento cessa. Ataques continuam usando os tempos do servidor. Teleportes e desativação da partida reiniciam o acompanhamento.

Evidência estática: os valores anteriores (Corrosivo 1,5; Espreitador/aranha 1,8; Couraçado 3 unidades/ciclo) excediam a passada representada nos sprites. Valores atuais antes da escala do objeto:

| Monstro | Unidades/ciclo | Base da calibração |
| --- | --- | --- |
| Corrosivo | 1 | Pé recua 0,60 durante 60% do ciclo; enquadramento original preservado. |
| Espreitador | 0,667 | Pé recua 0,40 durante 60% do ciclo; enquadramento original preservado. |
| Aranha | 0,526 | Pata recua 0,40 durante 50% do ciclo; conversão do enquadramento 3,65 para sprite de 2,4 unidades. |
| Couraçado | aproximadamente 0,395 | Estimativa visual de 23 pixels de apoio em meio ciclo no atlas de células 256, com sprite de 2,2 unidades. Fonte atual externa ao repositório; exige ajuste fino no jogo. |

As três primeiras relações vêm dos scripts de autoria em `Prototypes/CorrosivePouch`, `Prototypes/StalkerHoodedV3` e `Prototypes/SpiderPale`. A suavização adicional da fase do Couraçado foi removida para não atrasar os pés em relação à posição já exibida. Não foram alterados velocidades, física, dano, rede ou assets. Os demais monstros ainda usam os visuais estáticos/procedurais existentes, sem clipe de caminhada integrado.

Revisão por leitura apenas, sem testes, execução Unity ou build. Conferir na validação prática patrulha, perseguição, paradas e retomada após ataques, tanto no host quanto em cliente remoto. A calibração não elimina limitações dos próprios sprites, como curvas, deslocamento lateral e discretização dos 32 frames.
