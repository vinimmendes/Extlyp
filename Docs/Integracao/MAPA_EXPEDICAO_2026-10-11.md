# Floresta Velha — implementação inicial do mapa e expedição

## Mapa e circulação

Mapa grande 400×600, partida de 600 segundos, cinco abrigos periféricos e nenhum nascimento na clareira. Seis setores: serraria, depósito, vilarejo, lago, fazenda e clareira. Quatro estruturas por setor, totalizando 24 ruínas. Formatos locais, orientação dos prédios e dos móveis variam com a semente; baús e armários acompanham a transformação das estruturas.

As posições locais dos prédios também variam dentro dos setores. O gerador reserva os eixos de circulação, as aproximações às extrações, o entorno dos bloqueios e os abrigos antes de selecionar quatro posições separadas. Pátios são abertos na mata ao redor das construções, evitando que a rotação faça paredes/móveis ocuparem corredores comuns. Marcos dos setores, nascimentos e extrações mantêm suas posições.

O anel externo e as ligações com a clareira têm dois caminhos por conexão; serraria–depósito e vilarejo–fazenda têm um terceiro. Total de 22 caminhos. Massas sólidas de mata, árvores existentes e escarpas delimitam áreas e circulação; água profunda no lago também impede movimento. Mantida a regra de visão: essas barreiras naturais são obstáculos físicos baixos; somente paredes, portas e armários grandes ocultam a visão.

Pelo menos um dos dois caminhos principais de cada conexão nasce livre. O outro pode estar bloqueado, ocupado por um bando, estreitado ou exigir uma ferramenta/chave. Terceiros caminhos são atalhos especiais. A condição é derivada da semente replicada; não é sorteada separadamente por clientes. Bandos usam monstros reais e podem ser derrotados. Bloqueios físicos comuns permanecem estáveis durante a partida.

Darkwood orienta a floresta, madeira gasta, abandono e interiores; Bullet Echo orienta entradas alternativas, circulação e espaço de manobra. Arte/layout próprios. Árvores e móveis usam os sprites existentes; veículos, barcos, rochas, cercas, cultivos e marcos ainda têm apresentação provisória por código.

## Extrações

Dez pontos fixos, quatro ativos por partida, distribuídos por pelo menos três setores. A seleção rejeita combinações sem retorno de até 360 unidades (90 segundos à velocidade base), usando o comprimento dos caminhos no grafo e aproximações aos setores. Isso é uma estimativa estrutural: não mede ainda cada desvio de mobília no percurso físico.

| Setor | Pontos |
| --- | --- |
| Serraria | Estrada das Toras; Caminhão Madeireiro |
| Depósito | Vagonete de Manutenção; Portão de Serviço |
| Vilarejo | Ponte Velha; Trilha dos Peregrinos |
| Lago | Barco do Ancoradouro; Canoa dos Juncos |
| Fazenda | Carroça do Portão; Passagem do Gado |

Transportes só aparecem quando a saída está ativa; saídas a pé mostram bloqueios quando inativas. O servidor recusa a contagem em pontos inativos. Extração mantém três segundos, com progresso individual e texto de embarque/passagem no HUD. Retirar um jogador não consome a saída nem remove o transporte para os demais.

Cada participante recebe uma saída ativa confirmada no início. As demais permanecem incertas até o servidor confirmar que seu marco entrou na visão do personagem. Conhecimento, destino escolhido e progresso são replicados somente ao dono do jogador. O minimapa mostra + aberta, × indisponível e ? incerta quando expandido; clicar/tocar num ponto escolhe o destino sem revelar seu estado. O HUD orienta para o destino escolhido, inicialmente a saída confirmada. Bots orientam o retorno pelas saídas que conhecem, usando navegação entre setores e A* local.

## Atalhos com ferramentas e chaves

Novos itens acrescentados ao final do enum, preservando identificadores existentes: alicate de corte, pé de cabra, serra manual, chave de serviço e chave do depósito. Podem aparecer como suprimentos no loot; ficam na mochila, caem com ela na morte e retornam à base quando extraídos. Ícones são silhuetas provisórias por código. A chave do depósito é usada somente em conexões ligadas ao depósito.

Sem a ferramenta, a passagem não anuncia sua solução. Com o item correto, a interação próxima oferece cortar, remover tábuas ou destrancar. O servidor verifica participante, estado da partida, distância, item e linha de interação. A ação inicial é instantânea e não consome/danifica o item; duração, ruído e durabilidade não foram impostos sem definição de balanceamento.

Uma abertura permanece compartilhada até o fim da partida; colisão, apresentação e navegação são atualizadas pelo mesmo estado replicado. Restos nas laterais indicam passagem aberta. Atalhos especiais não são usados como requisito para a rota comum de retorno. Comprar ferramentas na base e desbloquear extrações especiais por progresso persistente continuam etapas futuras.

## Compatibilidade e revisão

Protocolo de rede **16**, pela adição de estado de extrações, conhecimento individual e passagens. Cliente e servidor precisam usar a mesma versão; executáveis existentes não foram substituídos nesta entrega. Engine e pacotes de rede permanecem Unity 6000.0.84f1, NGO 2.13.3 e Transport 2.7.4. Não foi executado MvpSetup.Build nem reconstruída a cena.

Revisão estática de identificadores de itens, máscaras, transformação de móveis/baús, seleção de saídas, validação das interações e atualização da navegação. Não foram executados testes automatizados, Unity, clientes separados, builds ou medições de desempenho. A geometria é um primeiro blockout: legibilidade dos marcos, desvios físicos, custo de geração, tempo entre setores e dificuldade dos encontros precisam da avaliação prática do usuário antes de refinar arte e balanceamento.

Recipientes de rede se religam aos colisores fixos quando a geometria é reconstruída, cobrindo a chegada de mensagens de spawn antes da atualização da semente. Se o gerador não encontrar espaço reservado para quatro ruínas em um setor, ele sinaliza erro; a rotina de início da partida mantém a reversão existente do equipamento, em vez de iniciar com uma distribuição inválida.
