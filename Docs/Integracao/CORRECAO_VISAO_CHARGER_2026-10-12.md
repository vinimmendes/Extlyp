# Correção de regressões de visão e Charger — 12/10/2026

Problemas relatados em partida pelo usuário. Investigação estática identificou duas causas:

- PlayerVision.WallDistance criava uma cópia completa do SpriteRenderer na ordem 102, acima da escuridão 100. Um único raio habilitava a cópia inteira, sem recorte.
- Durante a preparação, Charger consultava PerceptionTarget (cone e raio inicial) para atualizar a mira. Na hora de avançar, revalidava CanStartCharge, incluindo distância mínima de 5: aproximar-se, sair do raio inicial ou perder a aquisição cancelava o ataque.

## Correção

VisionSurface.shader, em Resources, recorta cada fragmento da superfície pelo campo radial da lanterna e alcance do primeiro bloqueador naquela direção. Os 180 raios guardam limite da superfície do bloqueador e raio de iluminação; mantém a suavização nas bordas. A superfície continua visível sem revelar o sprite inteiro ou objetos atrás dela. O alcance da superfície usa saída pelos bounds do collider com margem de 0,15; é uma aproximação conservadora para o desenho, sem mudar colisões.

Charger mantém o alvo original durante a preparação. A cada 0,125 s, acompanha a posição quando há linha de visão e o alvo está dentro da área de perseguição; cone e raio inicial não limitam esse acompanhamento. Cobertura congela a última direção. Distância mínima e caminho livre permanecem condições para INICIAR a preparação, não para cancelá-la no fim. O avanço lança na direção preparada e suas colisões continuam resolvidas por CircleCast. Morte/invalidade do alvo e retorno por limite de perseguição continuam interrompendo. Velocidade 38, preparação 3 s e duração 0,5 s preservadas.

## Verificação proporcional

Unity 6000.0.84f1, cópia isolada Builds/ServerValidation, verificação gráfica VisionSurfaceChecks.Run: saída 0. Compilação dos scripts e shader passou; pixels confirmaram trecho iluminado visível, costas e lateral fora do recorte ocultas. PNG inspecionado: Logs/vision-surface-check.png. Regra real TryTrackChargeAim verificada com alvo diagonal além do raio inicial, alvo a 2 unidades e parede bloqueadora. Log: Logs/vision-surface-unity.log.

A primeira execução foi interrompida por cena inicial sem nome na cópia; o teste abre Game antes da cena aditiva. A execução bem-sucedida emitiu aviso de liberação de RenderTexture ainda vinculado à câmera; a limpeza do teste foi ajustada depois para desvincular a câmera, sem alterar suas verificações.

Não foi reproduzida uma investida completa com cliente/servidor nem uma cena real de ruína; não foi testado Android. Nenhum build foi gerado e nenhum servidor reiniciado/atualizado. Protocolo continua 17; cliente e servidor precisam incorporar as correções.
