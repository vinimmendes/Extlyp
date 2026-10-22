# Visão — faces de parede em cor

## Mudança

Nas Catacumbas, a apresentação da visão passa a conservar a cor da faixa de alvenaria exposta e das decorações desenhadas sobre ela. O piso atrás da parede continua escurecido pelo primeiro impacto do raio, como antes. Não deslocar o colisor da parede para obter esse efeito.

## Causa identificada no código

`PlayerVision.RefreshLookup` usava a distância até a primeira face nas Catacumbas, pois os colisores são grandes segmentos de alvenaria: usar sua saída inteira revelava cunhas muito profundas no maciço. Como o shader aplicava esse mesmo corte a todos os pixels capturados, a própria parede ficava acinzentada/escurecida.

## Implementação

- O canal R do lookup mantém as distâncias originais para o piso. G registra uma distância cosmética para a face de parede; B continua identificando a superfície atingida.
- Uma textura estática 128×128 identifica somente alvenaria: células que não são piso-base, água ou cobertura baixa. A textura é criada uma vez por componente e destruída junto dele.
- Um percurso por células avança apenas na primeira faixa contínua de alvenaria após o impacto, até uma célula de profundidade normal à face. Esse limite corresponde ao contorno de parede desenhado, sem clarear o maciço de pedra que fica além dele.
- O percurso termina ao encontrar qualquer piso, água, cobertura baixa, limite do mapa ou limite de alcance. Não continua até a parede de uma segunda sala.
- `VisionSurface` usa a distância G somente quando o pixel pertence à alvenaria. Os demais pixels conservam R e o filtro anterior. O degradê da superfície começa em sua borda posterior; a guarda angular da alvenaria usa o menor alcance vizinho para evitar extrapolar quinas.
- O cone, o alcance radial, a opção Suave/Pixelado e a ocultação de alvos por `Physics2D.Linecast` permanecem com suas regras existentes. A mira e as consultas de combate não usam G nem a textura de alvenaria.

Arquivos: `Assets/Scripts/Players/PlayerVision.cs` e `Assets/Resources/VisionSurface.shader`. Não foram alterados colisores, geometria, atributos, rede ou saves.

## Revisão e limites

Revisados os caminhos de parede fina, alvenaria larga, quina, pilar, água, cobertura baixa e troca de mapa no código. O piso não recebe o prolongamento cosmético e os alvos continuam sujeitos ao primeiro bloqueio físico.

C# compilado isoladamente com referências atuais da Unity 6000.0.84f1, sem erros. Shader revisado estaticamente; não foi compilado/avaliado pelo Editor nesta entrega. Não foram iniciados Unity, testes automatizados ou builds.

Avaliação prática pendente: parede reta e suas decorações, corredor em L, pilar, parede fina entre salas e movimento junto às quinas; conferir ambos os modos gráficos. A cor fora do cone/alcance continua sujeita à apresentação de fog existente. Arte que exceder a faixa de uma célula continua parcialmente escurecida por segurança.

Correção após a captura enviada pelo usuário: a faixa inicial de três células clareava a alvenaria interna além da parede desenhada, formando a área iluminada abaixo/ao lado da borda marcada em vermelho. Reduzida para uma célula, mantendo a máscara exclusiva de alvenaria e o corte original do piso. Ajuste baseado na captura e na leitura do código; nova avaliação visual pendente.
