# Catacumbas — correções após o primeiro teste

## Relatos e alterações

1. **Câmera distante:** tamanho ortográfico específico da masmorra reduzido de 15 para 10. O enquadramento mostra dois terços da altura anterior e amplia os elementos em 1,5 vez, na mesma resolução. A preferência ainda precisa de avaliação em partida.
2. **Espaços pretos nos sprites:** as células de cobertura eram retiradas da máscara de piso e não recebiam textura sob as partes transparentes dos sarcófagos. `DungeonTerrain` agora pinta piso também sob coberturas, preservando suas colisões. No poço, o fundo preto de 4 × 4 u aparecia pelas margens transparentes da moldura; sua pegada passou para 2,8 × 2,8 u, dentro da estrutura. O interior do poço continua escuro por representar uma abertura real.
3. **Manivela sem ação aparente:** o texto fotografado correspondia ao estado de ferramenta ausente, não a uma interação concluída. O código exige a manivela encontrada na Câmara dos Contrapesos antes de permitir a instalação no poço. O aviso agora diz explicitamente “Falta manivela · busque nos Contrapesos”. A chave recebeu a mesma clareza. Não foi confirmado defeito na execução do mecanismo; preparação e consumo continuam autoritativos no servidor.
4. **Decorações soltas sobre preto:** a arquitetura desenhava apenas a primeira camada de contorno, deixando o restante da alvenaria quase preto; algumas decorações ocupam duas ou três células de profundidade. Além disso, o contorno e o preenchimento anterior disputavam a ordem -5. Agora todas as células sólidas recebem uma base opaca de pedra reutilizando textura existente, abaixo do contorno e das decorações; o preenchimento antigo fica abaixo dessas camadas. Nenhum colisor ou máscara de navegação foi alterado.

A extração pelo barco foi relatada como funcional pelo usuário antes dessas alterações. Sua lógica não foi modificada.

## Verificação e limites

Foram lidos os caminhos de interação, requisito, consumo e preparação da manivela, além dos montadores e das máscaras de piso/cobertura/parede. Inspecionada a fonte RGBA da moldura do poço e amostras de transparência das peças envolvidas. Sprites e arquivos fonte preservados; nenhuma imagem gerada ou editada.

A compilação C# com o compilador e referências locais da Unity 6000.0.84f1 terminou com código 0, sem diagnósticos na execução final. Saída separada em `Logs/dungeon-fixes-2026-10-15`, sem substituir assemblies da Unity. Isso verifica compilação, não renderização, importação ou funcionamento dos RPCs em execução.

Não foram iniciados Unity, builds de jogador ou suítes automatizadas. Na próxima avaliação prática, conferir o novo zoom, bordas dos sarcófagos e do poço e paredes atrás das decorações.

## Retorno do usuário — 15/10/2026

O usuário confirmou: “Extração da manivela funciona”. A extração do poço com manivela fica registrada como funcional no teste prático relatado pelo usuário; a pendência de confirmação desse funcionamento está encerrada. Não houve nova execução de testes pelo agente nem alteração de código neste registro. O relato não encerra as pendências visuais de iluminação, câmera/campo de visão ou do símbolo da extração deslizando no minimapa.
