# Canal dos Defuntos — rio, cais e comporta

Implementação de 19/10/2026 dos planos aprovados: tornar o atalho da comporta útil e colocar a extração da barca em um curso de água que sai do mapa. Referências: abandono e materiais de Darkwood; circulação, cobertura e alternativas de Bullet Echo. Arte existente reutilizada, sem substituir os originais.

## Geografia e circulação

O Embarcadouro Funerário foi deslocado para o sudeste, com cais seco a oeste do rio. O curso entra pela borda leste, entre y=-28 e -20 células, e se conecta ao trecho sul, entre x=54 e 62, que alcança a borda inferior. A barca fica em (56,-50), dentro desse trecho. Todas as coordenadas abaixo são células relativas ao centro do mapa; multiplicar por 1,06 para unidades de mundo.

A ponte cobre x=42..46 e y=-28..-20. O mecanismo fica na margem norte, em (47,-18), acessível antes da travessia. Com chave, a ponte conecta a chegada ao acesso sul do embarcadouro. Sem chave, uma galeria a oeste, x=24..30, leva ao cais pela faixa sul. A antiga volta curta ao redor da cisterna e os corredores automáticos que anulavam a escolha foram removidos.

As entradas Cortejo e Canal foram deslocadas, respectivamente, para (16,-56) e (52,-16), mantendo oito entradas e evitando começar diretamente no novo cais. As demais seis entradas, a chave na Prisão e as outras duas saídas mantêm suas posições.

Coberturas baixas autoradas protegem trechos da chegada e da aproximação sul. A galeria longa tem cobertura parcial e dois encontros Weaver redistribuídos do salão, sem aumentar o total de inimigos. A disponibilidade da chave não torna a barca obrigatoriamente inacessível: a rota sem chave permanece.

## Estado e autoridade

Somente as 32 células da ponte ficam caminháveis após abrir a comporta. Os 504 blocos de água permanente, descontados ponte e convés, continuam bloqueados. Os colisores da ponte são separados dos do rio durante a construção. A navegação usa essa mesma distinção, invalida suas rotas na mudança de estado e o mapa mostra água e ponte aberta com cores distintas.

Preservados consumo da chave, interação de quatro segundos, 15 XP do objetivo, sincronização de DungeonShortcut e as três saídas existentes. A corrente da barca continua sendo uma preparação independente; o mecanismo está no cais, em (52,-50). Abrir a comporta informa “Passagem para o embarcadouro liberada”. Sem mudanças de saves ou protocolo.

## Apresentação

Os tiles e materiais existentes formam dois trechos de água: corrente para oeste na entrada e para sul na saída. A arquitetura existente fornece o arco sobre o rio junto à borda sul, com apoios nas margens. O cais e a ponte reaproveitam as peças de extração anteriores.

O convés ocupa quatro por duas células e tem um trigger retangular menor, alinhado à barca. A embarcação permanece atracada durante toda a contagem de extração e só inicia a partida visual após a confirmação do servidor, seguindo para o sul. A animação não desloca o trigger nem permite que o jogador caminhe rio afora. As representações móveis continuam locais, como no sistema anterior.

Decoração automática é reservada fora das novas aproximações e da faixa de partida. Nove candidatos de quebráveis afetados foram reposicionados: IDs 6, 7, 23, 24, 25, 27, 56, 59 e 62. Mantidos 64 IDs, famílias, variantes, quantidades e distribuição por sala/entrada/corredor.

## Evidência estática e limites

[Planta esquemática](planta.svg) e [levantamento de células](levantamento.json) produzidos por `Tools/Survey-CanalGeometry.py`. Esse levantamento interpreta as formas autoradas; não executa Unity, regras de partida, física ou testes automatizados do jogo.

Entre o mecanismo e a barca: 85,86 unidades com a comporta fechada e 47,70 aberta, redução de 44,4%. A distância é uma estimativa ortogonal de centros de células, sem combate, curvas de movimento, custo de buscar a chave ou tempo da interação. As oito entradas possuem conexão pela rota fechada nessa planta. Todos os 64 candidatos permanecem em chão seco; nenhum foi deixado na água. O script antigo de levantamento de área também passou a consultar as novas máscaras, sem manter a antiga cisterna.

Compilação C# com referências e defines existentes da Unity 6000.0.84f1 concluída sem erros, com saída isolada em Logs/canal-integration. Sem iniciar o Editor, testes automatizados ou builds do jogo. Não foi executada avaliação gráfica ou de colisões nativas.

Na primeira avaliação prática, conferir ambas as rotas, aproximação do mecanismo pelos dois lados, navegação dos inimigos, ausência de passagem pelas bordas, sincronização da ponte, proteção da água permanente, embarque sobre o convés e partida sob o arco. As novas posições exigem clientes e servidor com a mesma versão e reinício da expedição; uma partida já montada não reconstrói sua geografia automaticamente.
