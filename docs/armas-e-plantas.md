# Extlyp — planta-mãe, bulbos, efeitos e armas

Esta branch inclui as novas armas e o encontro de plantas, com cura na chegada das partículas. A main foi preservada. Não há APK nem build de servidor incluído.
Base: vinimmendes/Extlyp, main, commit 40fb95aa3b0e6f6d6db96bc8772ae08ac169dafd. Os arquivos de integração já preservam as novas armas.

## Instalação

1. Faça checkout desta branch em uma cópia do projeto.
2. Aguarde o download dos arquivos e assets.
3. Abra no Unity 6000.0.84f1 e aguarde a importação. Confira erros no Console.
4. Fora do Play, execute **Cyber Extraction > Plantas > Preparar sprites e prefab**. Esse comando configura a importação do PNG e grava o prefab de rede da planta; não recria a cena Game.
5. Execute **Cyber Extraction > Plantas > Verificar regras e recursos** (29 verificações), e **Cyber Extraction > Verificar novas armas** (23 verificações).
6. Recompile servidor e clientes juntos. O protocolo agora é **8**, por causa do novo prefab de rede e do estado sincronizado. Clientes antigos não devem ser usados com esse servidor.

O prefab está na branch em Assets/Resources/Plants/PlantCreature.prefab. O NetworkBootstrap carrega e registra esse recurso antes de iniciar a conexão. Um único prefab representa mãe, bulbos e monstrinho; a fase é replicada pelo servidor. Não exige posicionar objetos ou registrar prefabs manualmente nas cenas.

## Encontro

Há uma colônia perto de cada um dos cinco abrigos, aproximadamente 16 unidades à direita e 16 abaixo do ponto inicial, procurando espaço livre. O monstro original continua no mapa. A mãe é imóvel e gera bulbos quando detecta um jogador ou bot em um raio de 12 unidades, sem parede entre eles.

| Entidade | Vida | Comportamento |
|---|---:|---|
| Planta-mãe | 300 | Gera um bulbo a cada 6 segundos enquanto há um alvo próximo; primeira tentativa após 2 segundos |
| Bulbo atacante | 30 | Amadurece em 9 segundos; pode ser destruído antes disso |
| Monstrinho-planta | 35 | Persegue alvos vistos, contorna obstáculos, causa 10 de dano a cada 1,2 segundo |
| Bulbo de cura | 40 | Recupera 12 de vida da mãe a cada 2,5 segundos, até 6 unidades de distância e com caminho visual livre |

O limite por mãe é de três atacantes (bulbos e monstrinhos somados) e dois curadores. Os curadores não ressuscitam nem curam além da vida máxima. A transformação mantém o mesmo objeto de rede e conserva a proporção de vida restante do bulbo. O monstrinho usa visão bloqueada por paredes, investiga a última posição vista por 3 segundos e retorna à mãe; os alvos são limitados a um raio de 14 unidades em torno dela.

Todos podem receber dano das armas. Destruir um bulbo impede sua função, mas a mãe pode gerar outro após o intervalo e respeitando os limites. Matar a mãe remove seus filhos e deixa duas pólvoras como loot. Os bulbos e filhos não geram loot adicional.

## Sprites e sinais visuais aprovados

O PNG é exatamente a última imagem aprovada, sem redesenho: mãe, bulbo atacante, monstrinho e bulbo de cura. O recorte de cada sprite é feito em runtime; as três criaturas menores usam as vistas de cima aprovadas. Não redimensione a imagem 1306×1205, pois os retângulos de recorte são fixos.

- **Cura na chegada:** o bulbo pulsa em dourado, partículas viajam até a mãe e ela recebe um brilho dourado breve. O envio começa apenas para uma mãe ferida. A cura de 12 pontos é aplicada somente quando a última partícula chega, cerca de 0,83 segundo depois do envio; o brilho dourado da mãe só começa após a confirmação dessa cura pelo servidor. Alcance, paredes, vínculo e vida são conferidos novamente na chegada. Se o bulbo ou a mãe morrerem, ou a cura deixar de ser válida, não há cura nem brilho. Há um halo dourado fraco em repouso para distinguir o tipo do bulbo.
- **Amadurecimento:** pulso e halo roxos aceleram, rachaduras luminosas aparecem a partir de 45% do progresso e o bulbo treme nos últimos 25%. A transformação termina com uma explosão curta de folhas e a troca para o sprite do monstrinho.
- Os efeitos respeitam o cone de visão, a escuridão e as paredes. Partículas de cura só aparecem quando ambos os extremos estão visíveis; cada partícula também verifica sua posição. Isso evita que o efeito indique uma mãe ou bulbo ocultos.
- Efeitos são cosméticos no cliente. Cura, dano, geração, transformação e movimento dos filhos são decididos no servidor.

## Armas

O pacote conserva a pistola e adiciona o rifle de ferrolho inspirado na Mosin e a espingarda de dois canos. Q/2 alterna armas; R/3 recarrega; os botões correspondentes funcionam no celular. Detalhes do pacote anterior estão em armas.md. Balanceamento das plantas fica em PlantRules.cs e das armas em WeaponCatalog.cs.

## Verificação realizada e pendências

As verificações locais concluídas estão em validacao-estatica.txt: delimitadores lexicais, YAML e referências do prefab, GUIDs, integridade do PNG aprovado, limites dos quatro recortes, registro do prefab, protocolo e integridade do ZIP. A revisão estática também confere que a cura fica no ramo de conclusão do voo e que o brilho usa o horário confirmado da chegada.

**Não foi possível compilar ou executar no Unity**, pois o ambiente não possui Unity nem compilador C#. As verificações dos menus foram incluídas, mas não foram executadas. O prefab foi preparado a partir do formato do prefab do monstro existente; execute o comando Preparar sprites e prefab para gravá-lo pelo editor antes de gerar builds. Não foi gerado APK nem servidor executável.

Teste em Play com servidor e cliente separados: aproximar-se da mãe, observar dois tipos de bulbo, ferir a mãe e conferir o fluxo dourado/limite de vida, destruir um curador durante o voo e confirmar que a cura pendente é cancelada, destruir um atacante antes de nascer, observar os 9 segundos de amadurecimento, testar perseguição/ataque e paredes, matar a mãe e conferir remoção dos filhos, terminar e reiniciar a partida sem sobras. Conferir também que sprites, rachaduras e partículas somem fora da visão e verificar desempenho/legibilidade no Android.

Os números são iniciais para balanceamento. Os sprites são estáticos com animação por escala, deslocamento e partículas; não há folhas quadro a quadro nem áudio novo.
