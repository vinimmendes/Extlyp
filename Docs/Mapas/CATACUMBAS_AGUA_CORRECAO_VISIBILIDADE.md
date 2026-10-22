# Correção após avaliação: água pouco perceptível e textos da HUD

O usuário relatou texto corrompido ao abrir baú e ausência perceptível de movimento na água. A HUD continha literais UTF-8 interpretados previamente como outro encoding (baÃº, Â· e outros). Corrigidos os textos no UguiGameHud e no fluxo alternativo InventoryWindow, preservando UTF-8 e os rótulos aplicados pela Unity.

O log disponível de atualização do servidor registra compilação do shader DungeonWater sem erro. Isso confirma a compilação anterior, não a apresentação do cliente. A inspeção da implementação mostrou que a textura original não se movia: havia apenas modulação de luminosidade de 4,5% e uma fase principal com ciclo de aproximadamente 22 s. Essa combinação explica um efeito pouco perceptível sobre a textura já detalhada; não reproduz nem exclui outras causas em execução.

## Ajuste aplicado

Adicionado FlowTex independente com a textura repetível CAT-F07_v01, amostrada por coordenadas mundiais. O detalhe passa a se deslocar, com pequena deformação senoidal, misturado em 55% ao interior da superfície. Usa uma amostragem adicional por fragmento. Mantém UVs e alpha do sprite de base, evitando vazamento de atlas e deslocamento da silhueta; o efeito desaparece gradualmente junto às margens. A corrente adicional do canal continua respeitada. Piso, margens, degraus, passarela e máscaras físicas não foram alterados.

Valores iniciais revisados no shader e no material: WaveSpeed 0,85; Surface variation 0,12; Reflection strength 0,055; Drip ring strength 0,10; FlowSpeed 0,035 UV/s; FlowBlend 0,55. FlowScale é calculado pelo terreno como 1/(2 × CellSize). O material de configuração inclui a referência da textura, permitindo inspecionar o recurso; o terreno também a atribui explicitamente ao criar as cópias.

Cobertura continua sendo a água da cisterna e o canal funerário. Decalques de umidade/poças no piso não viraram novas superfícies animadas. O mapa deve ser reconstruído ao reentrar na expedição para receber as novas cópias do material; alterar apenas o asset durante uma sessão não atualiza os parâmetros já copiados.

## Verificação

Compilação C# executada com os response files da última atualização do servidor e os três scripts corrigidos, gerando saídas isoladas em Logs. Sem iniciar Unity, gerar player ou substituir servidor. Shader/material receberam revisão estática de parâmetros, referência de textura, sampler Repeat e integração. A nova versão do shader ainda precisa ser compilada/importada pela Unity e avaliada em cena; não houve validação visual em execução nem medição de desempenho.

Na próxima avaliação: reabrir a expedição no cliente atualizado, observar a cisterna/canal por alguns segundos e conferir movimento dos detalhes, margens fixas, goteiras e ocultação pela escuridão. Texto esperado: “Abrir baú · 4 s”, com tempo variável conforme o baú.
