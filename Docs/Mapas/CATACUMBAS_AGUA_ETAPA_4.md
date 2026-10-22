# Água — etapa 4: integração e controles de acabamento

A implementação das quatro etapas está integrada. A aprovação visual e o ajuste fino em PC/Android permanecem dependentes da avaliação prática do usuário; não foram iniciados Unity, testes automatizados ou builds.

## Ajustes persistentes

Selecionar `Assets/Resources/DungeonWaterAppearance.mat` no Project e ajustar no Inspector. O terreno cria duas cópias desse material ao montar o mapa; as cópias recebem limites, pontos de goteira e correnteza do canal. O asset original não é modificado em execução. Alterações no asset serão usadas na próxima montagem do mapa. Não editar o material de runtime esperando que suas mudanças sejam persistidas.

| Controle no material | Valor inicial | Ajuste |
| --- | --- | --- |
| Wave scale | 1,4 | Frequência espacial das ondas |
| Wave speed | 0,28 | Velocidade de ondulação |
| Surface variation | 0,045 | Variação da luminosidade da água |
| Reflection strength | 0,025 | Intensidade do brilho ilustrativo |
| Quiet shore width | 0,4 | Faixa de água tranquila junto à margem, em unidades do mundo |
| Drip ring strength | 0,045 | Intensidade dos anéis; zero os oculta |
| Drip maximum radius | 0,38 | Raio máximo dos anéis; limitado também pela distância à margem |

Water bounds e Drip A/B são parâmetros calculados pelo terreno. A corrente do canal é configurada em DungeonTerrain (eixo X, velocidade 0,10). Se o material de configuração não existir ou apontar para outro shader, o terreno utiliza os padrões do shader; se o shader não for suportado, preserva a água estática.

## Integração revisada

- Cisterna: Tilemap Water em Default, ordem -8; margens e degraus em -7. Passarela em -6. Anéis são parte da superfície, sem renderizador adicional.
- Canal: superfície em Default, ordem -7. Mantida a apresentação existente do barco e dos elementos de extração.
- PlayerVision captura Default antes da camada VisionOverlay, onde a escuridão é composta. Renderer2D mantém Camera Sorting Layers Texture habilitada. O shader da água usa Universal2D, sem passe de sobreposição posterior à visão.
- Sprite UVs, transparência, colisores, máscara de navegação e regras de extração preservados. Recursos próprios destruídos com o terreno; nenhum recurso visual criado em dispositivo gráfico Null.
- Anéis recebem atenuação por cobertura de pixel para evitar pontos intensos quando muito pequenos. O raio também é limitado pela distância dos pontos às margens. Não há partículas, emissões de objetos, Update ou RPC adicional.

## Avaliação prática pendente

1. Observar cisterna e canal parado e caminhando: movimento discreto, sem emenda adicional entre tiles ou brilho excessivo.
2. Aproximar-se e afastar-se das margens: anéis não ultrapassam a água; degraus e passarela permanecem legíveis.
3. Abrir e fechar a passarela; acompanhar o barco e a extração, conferindo a ordem de desenho.
4. Virar o personagem e deixar água fora da visão: efeitos precisam sofrer a mesma escuridão da superfície.
5. Conferir no Android horizontal e na escala real da câmera: legibilidade, estabilidade dos anéis e custo no dispositivo. Nenhum ganho ou orçamento de desempenho foi medido nesta entrega.

Revisão realizada: análise sintática C#, referências do shader/material, parâmetros, caminho de composição e ordem de desenho. Isso não equivale a compilação de shader, importação nativa do material ou validação em execução. A suavização por margens continua específica das duas superfícies retangulares atuais; novas bacias ou recortes precisam de máscara própria.
