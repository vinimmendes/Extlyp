# Catacumbas — etapa 3.5: identidade dos setores

Lote CAT-S01 a CAT-S08 produzido e integrado: 15 imagens, com oito marcas de antecâmara e sete marcos internos. A identidade vem de materiais e formas da mesma construção medieval: inscrições raspadas, raízes, cera antiga, níveis de inundação, grades, altar partido e polias de madeira. Darkwood orienta matéria e abandono; Bullet Echo orienta a leitura do espaço e das rotas. Não são oito biomas separados.

## Aplicação

`DungeonSectorArt` cria apresentação local depois de pisos, arquitetura e sepulturas. Não modifica máscaras, colliders, navegação, combate, monstros, loot ou estado de rede.

- As oito entradas recebem relevos planos de 2 × 1 células, com motivos de sino, urna, nomes apagados, grade, raiz, velas mortas, cortejo e água. Ficam junto às bordas internas, afastadas do centro de spawn. Não são itens ou indicadores de extração. O marcador quadrado antigo de cada entrada é ocultado somente após a colocação do relevo correspondente e a conclusão da camada.
- Cada setor interno recebe um marco de 4 × 3 células (4,24 × 3,18 u), preferencialmente junto à sala que já dá nome ao setor. Se necessário, procura outra sala do mesmo setor. Em paredes laterais, gira a peça completa em 90 graus.
- Marcos maiores ocupam exclusivamente alvenaria já inacessível, adjacente ao piso da sala. A seleção exige todas as células bloqueadas e sem água/cobertura baixa, evitando interferência com sprites de arquitetura, mobiliário e outros marcos. Não posiciona altar, raízes ou polias no centro caminhável.
- Reservas ao redor de spawns, loot, mecanismos, ferramentas e extrações evitam disputar a leitura dos objetivos. As peças de polias são decoração estática, sem manivela ou controle interativo. A cisterna recebe marcas de inundação sobre pedra; a água e a ponte continuam sendo apresentadas pelos sistemas existentes.
- Se não houver encaixe, omite aquela instância e registra aviso; não desloca a geometria para acomodar arte. O nome textual do setor e sua posição permanecem inalterados.

O carregamento exige o lote completo antes de criar instâncias. Recurso ausente cancela somente a camada nova. Servidor sem dispositivo gráfico não a cria; os sprites gerados por `Sprite.Create` são liberados no descarte do mapa. Não há atualização por frame ou nova dependência.

## Fontes e integração

- Originais RGBA e prompts individuais: `Art/Catacumbas/Setores/{Fontes,Prompts}`.
- Manifesto de prompts e galeria: `Art/Catacumbas/Setores/{prompts-v1.json,index.html}`. A galeria mostra arte isolada, não captura da Unity.
- Recursos: `Assets/Resources/Environment/Dungeon/Sectors`, incluindo `SectorsArt.json` com os retângulos normalizados do corpo.
- Preparação: `Tools/Prepare-DungeonSectors.py`, derivada do empacotamento das sepulturas. Mede alpha e copia PNGs intactos, sem edição de pixels; preserva fontes e arquivos existentes diferentes.

Geração pela ferramenta imagegen integrada, uma chamada por imagem; prompts exatos preservados no projeto. Importação sRGB, bilinear, Clamp, sem mipmaps e sem compressão. O limite de importação considera o corpo recortado e a referência de 128 px por célula; margem transparente não amplia a pegada. APIs adaptadas à Unity 6000.0.84f1, sem alteração da engine, URP ou rede.

## Verificação e limites

Revisão estática do encaixe e da integração; inspeção visual das 15 imagens originais. Sem testes automatizados, sessão Unity ou build nesta entrega, conforme a preferência do projeto. Presença de cada instância, contraste sob fog, escala no Android e leitura dos marcos durante combate aguardam avaliação prática do usuário. Não há medição de desempenho nesta etapa.
