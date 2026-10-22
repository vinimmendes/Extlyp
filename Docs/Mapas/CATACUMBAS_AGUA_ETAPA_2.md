# Água — etapa 2: margens e correnteza

Continuação implementada: [etapa 3 — anéis de goteira](CATACUMBAS_AGUA_ETAPA_3.md). A descrição abaixo registra a segunda etapa.

O shader DungeonWater agora reduz gradualmente a variação de brilho perto da margem e escurece discretamente a faixa de contato com a pedra. A faixa tem 0,4 unidade de mundo; o brilho adicional desaparece na borda e a ondulação conserva apenas 15% de sua intensidade. A transparência e os UVs dos sprites continuam fixos, sem espuma branca ou deslocamento das bordas.

O cálculo usa a distância às bordas externas da superfície, sem reiniciar em cada tile. Na cisterna, DungeonTerrain calcula os limites a partir das células IsWater. A implementação corresponde ao único tanque retangular existente; se o mapa passar a ter água recortada ou bacias separadas, substituir esse limite retangular por uma máscara de distância por bacia. No canal, usa os limites do sprite após aplicar sua escala.

O canal recebe uma instância própria do material, com transporte do padrão de ondas no eixo X positivo a 0,10 unidade/s. A cisterna mantém movimento sem transporte adicional. Isso representa correnteza visual e não movimenta barco, jogadores, itens ou colisores. Os dois materiais pertencem ao terreno e são destruídos com ele.

Controles adicionais do shader: Quiet shore width determina a largura da faixa tranquila; Current contém direção XY e velocidade em Z. Water bounds contém mínimo XY e tamanho ZW em coordenadas mundiais. A corrente move apenas o padrão procedural; a textura de base permanece fixa.

Margens, degraus submersos e passarela conservam renderizadores, sprites e ordem de desenho anteriores. Nenhuma mudança de navegação, rede ou extração. Goteiras e anéis pertencem à etapa 3 e continuam pendentes.

Revisão estática: análise sintática C#, parâmetros entre shader e script, limites do tanque, material separado do canal e liberação dos recursos. Sem executar Unity, compilar shader, testes automatizados ou builds. A leitura final dos degraus/passarela, a corrente e o contraste sob escuridão aguardam avaliação prática; não houve medição de desempenho.
