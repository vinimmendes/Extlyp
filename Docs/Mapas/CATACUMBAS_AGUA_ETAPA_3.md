# Água — etapa 3: anéis de goteira

Continuação: [etapa 4 — integração e controles](CATACUMBAS_AGUA_ETAPA_4.md). Implementação integrada; aprovação visual em execução pendente.

Adicionados quatro pontos cosméticos: dois na cisterna e dois no canal funerário. Cada gota produz um anel expansivo e outro interno mais fraco, com desaparecimento gradual em 1,65 s e raio máximo de 0,38 unidade. Cores e intensidade acompanham os brilhos oliva discretos da superfície; sem espuma branca, respingos altos ou novos sons.

Os pontos ficam em 25%/32% e 75%/68% da largura/altura de cada superfície retangular existente. Permanecem afastados das bordas e do eixo central. A margem tranquila também atenua os anéis. Como parte do próprio shader da água, não desenham sobre as pedras, degraus, barco ou passarela que já estão acima da superfície.

Intervalos de referência: cisterna 4,7 e 6,1 s; canal 5,3 e 7,4 s. Fases distintas e atraso determinístico entre 0 e 0,65 s variam o momento de cada gota sem usar UnityEngine.Random. Ciclos sempre maiores que a duração do efeito impedem sobreposição no mesmo ponto. Há no máximo quatro eventos simultâneos no mapa (até dois traços concêntricos por evento).

A reutilização ocorre em quatro slots fixos do shader, em vez de um pool de GameObjects. Não há Instantiate/Destroy por emissão, Update, RPC ou partículas. Os parâmetros são enviados uma vez durante a construção. O custo adicional é cálculo por fragmento nas superfícies; ainda não foi medido em dispositivo. O servidor sem dispositivo gráfico continua sem criar esses materiais.

Controles: Drip ring strength (0,045), Drip maximum radius (0,38), Drip A/B (posição mundial XY, intervalo Z, fase W). Intervalo zero desabilita o ponto. Ao aumentar o raio, revisar a distância das margens. A implementação pressupõe as duas superfícies retangulares atuais, conforme a etapa 2.

Revisão estática: sintaxe do script, correspondência dos parâmetros, limites de pontos, duração e ordem de desenho preservada. Não foram iniciados Unity, compilação de shader, testes automatizados ou builds. Legibilidade dos anéis, antialiasing no celular e oclusão pela visão aguardam avaliação prática. A etapa 4 de integração e acabamento continua pendente.
