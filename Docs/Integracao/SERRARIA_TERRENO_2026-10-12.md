# Piloto do terreno da serraria

## Entrega

Região 128 × 128 centrada no setor Serraria, com oito unidades de transição. Solo em Tilemap de células de duas unidades; estradas em SpriteShape, derivadas dos segmentos e larguras de GroundPath. A união dos segmentos é traçada antes de gerar os contornos, evitando bordas internas nas bifurcações. A máscara comum controla folhagem/terra, umidade, ocupação do terreno aberto e transição do piloto. Preenchimento em coordenadas do mundo com período de oito unidades.

As superfícies originais continuam disponíveis. Dentro do piloto, as estradas antigas desaparecem gradualmente pelo material de apresentação; fora dele mantêm sua aparência. A ocupação da floresta usa OpenTerrain do construtor existente. Paredes, pisos, árvores, rede, navegação, colisões e larguras das rotas permanecem sob o código anterior.

Três texturas geradas por image_gen integrado, normalizadas para 1024 × 1024, com reparo determinístico de bordas. Originais e prompts em Art/SawmillTerrain. Os overlays discretos foram preparados por código: folhas, musgo, pedras pequenas, serragem, sulcos de roda e umidade. Limite de 48 grupos ambientais, 12 grupos de sulcos, mais serragem nas pilhas existentes. A distribuição evita estruturas, nascimentos, extrações e obstáculos. Toras usam a versão amarrada ajustada para 7 × 1,8 unidades.

Recursos carregados uma vez por construção, sem Update próprio. Objetos criados em runtime são liberados com a apresentação; shaders e texturas Resources não são destruídos pelo piloto. Servidor sem dispositivo gráfico não cria a apresentação. Se texturas/shader não estiverem disponíveis, o mapa anterior permanece. Se a construção lançar exceção, a apresentação parcial é removida e seus materiais originais restaurados.

APIs conferidas por leitura do pacote instalado SpriteShape 10.1.1. Stretch UV é um campo privado falso por padrão nessa versão; o código habilita worldSpaceUVs e define fillPixelsPerUnit=128. Não houve instalação/atualização de pacotes ou mudança na cena.

## Desativar

No Inspector do objeto que contém MapBuilder, desmarcar **Sawmill Terrain Pilot** antes de iniciar uma nova partida. A construção seguinte usa o visual antigo, incluindo os retângulos das toras. Não há botão de desenvolvimento na partida.

## Backup e restauração

Backup: Backups/Mapa/mapa-2026-10-12-175228.zip. Inclui estado real dos arquivos, mudanças locais, recursos novos já presentes antes desta tarefa, hashes SHA-256, lista de entradas ausentes e estado Git. O snapshot descompactado também permanece em Backups/Mapa/snapshot-2026-10-12-175228. O diff suplementar foi limitado aos arquivos do mapa porque o filtro Git LFS de armas fora do escopo falhou; o ZIP preserva os arquivos reais independentemente desse diff.

Para desfazer somente esta entrega:

1. Fechar a sessão de jogo. Preservar separadamente qualquer edição posterior nos arquivos que serão restaurados.
2. Extrair o ZIP em um diretório separado e copiar de volta **apenas** Assets/Scripts/Match/MapBuilder.cs e Assets/Scripts/Match/ForestMapGeometry.cs. Essas cópias incluem as alterações locais que já existiam antes desta tarefa; não usar git reset.
3. Remover os novos arquivos listados em Backups/Mapa/sawmill-changes-2026-10-12.json. O registro não inclui arquivos pré-existentes a serem removidos.
4. Nenhuma cena, configuração de pacote ou asset anterior precisa ser substituído. O backup completo é uma reserva adicional, não uma instrução de sobrescrever o projeto inteiro.

## Conferência e limites

Mosaicos 3 × 3 inspecionados visualmente. As bordas foram igualadas; a repetição ampla ainda pode ser percebida em mosaicos grandes, sendo atenuada pela mistura de umidade e pelos overlays. Não chamar isso de repetição invisível.

Art/SawmillTerrain/material-composition.jpg é uma composição offline usando os assets entregues e escala de 24 pixels por unidade. Demonstra materiais e toras, mas suas curvas são ilustrativas e não reproduzem o layout procedural da partida. Não é uma captura Unity e não valida SpriteShape, shader, sorting ou fog.

Revisão estática das APIs, ciclo de vida e integração. Não foram executados testes automatizados, sessões Unity, builds ou medições de desempenho. Avaliação prática pendente: console sem erros de shader/tesselação, estradas sem emendas, tilemap sem quadrados perceptíveis, entradas preservadas, fog, legibilidade no celular, saída/entrada da partida e visual antigo com a opção desativada. Antes de expandir, ajustar esse piloto com base no teste do usuário.
