# Equipamentos — etapa 6: níveis, XP e pontos

Implementação em 16/10/2026. Revisão estática; validação prática pendente. Mantidos Unity 6000.0.84f1, NGO 2.13.3 e Transport 2.7.4.

## Progressão

- Nível inicial 1, máximo 20. Custo do nível L para o seguinte: `100 + 50 × (L − 1)`. Total necessário: 10.450 XP.
- Cada nível além do primeiro libera um ponto, totalizando 19. XP, nível e saldo de pontos são reconciliados no servidor; subir de nível não concede vida, dano ou velocidade automaticamente.
- Um ponto pode ser investido em Força, Destreza, Vigor, Percepção ou Vitalidade, até dez investimentos por atributo. Mantém o teto efetivo de 20 da etapa 5. Inteligência continua reservada.
- Investir somente na base, pelo comando de acampamento existente, com revisão, ID de ação e confirmação do servidor. Ações com saldo insuficiente, atributo inválido, expedição ativa, atividade bloqueante ou revisão repetida são recusadas. Não há distribuição otimista no cliente.
- Redistribuição paga, comércio e moeda continuam na etapa 9.

## Recompensas autoritativas

| Evento | XP provisório | Regra |
| --- | --- | --- |
| Monstro comum | 10 | Dividido entre participantes humanos elegíveis pelo dano válido dos últimos 15 s |
| Monstro de ameaça alta | 20 | Mesma divisão; categoria explícita no código |
| Primeiro setor de exploração | 5 | Uma vez por região e jogador, até 40 por expedição |
| Preparar extração nas Catacumbas | 25 | Somente quem conclui o mecanismo |
| Abrir atalho relevante | 15 | Somente quem conclui, sem repetir ao reabrir UI |
| Eliminar humano em PvP | 20 | Golpe final, até 60 por expedição; mesmo par de contas, em qualquer direção, uma vez a cada 24 h |

Ameaça alta: Charger, Shell, Mimic, Devourer e planta-mãe. Os demais arquétipos, bulbos, servos e Esporífero independente usam 10 XP. A classificação não deriva da vida atual, crescimento ou dificuldade. Destruir um objeto por despawn administrativo não gera recompensa de morte.

Tiros e melee passam a identificar o autor do dano no servidor. A contribuição considera somente dano realmente aplicado, sem inflar overkill. Entradas anteriores a 15 s e autores mortos, extraídos, desconectados ou sem expedição real ativa não participam da divisão. O arredondamento usa maiores restos e desempate por ID de rede, preservando a soma da recompensa. Cada morte tem uma identidade nova; o evento e as parcelas são gravados juntos na transação de persistência existente.

PvP não recompensa suicídio, assistência, ambiente sem autor ou bots. A restrição de 24 h fica no registro privado do servidor e sobrevive a reconexões e mudanças de sala; não expõe contas adversárias no snapshot do jogador. Não cria equipes nem altera o PvP existente. Essas restrições reduzem repetição entre duas contas, sem prometer impedir combinação entre várias contas.

Nas Catacumbas, entrar em uma sala registra seu setor autorado: oito setores permitem até 40 XP. Na Floresta grande, aproximar-se a seis unidades de um ponto de exploração registra seu setor, agrupando os quatro pontos de cada setor: seis setores permitem 30 XP. No mapa pequeno, os quatro pontos existentes permitem 20 XP. O limite de 40 é um teto, não uma recompensa garantida em todos os mapas. Não foi alterado o layout dos mapas.

## Saldo, resultado e recuperação

As recompensas são registradas como saldo provisório com chave de evento e ID da expedição. Não aumentam o XP confirmado antes do resultado. Extração confirma 120% do saldo; morte, tempo esgotado e desconexão durante partida confirmam 50%, arredondados para baixo. O XP confirmado anterior nunca é retirado. Ganhos além do nível 20 são descartados, sem moeda ou pontos adicionais.

O registro provisório satura em 20.900: esse saldo já alcança o teto vitalício mesmo sob a regra de perda de 50%; valores superiores não alterariam nenhum resultado possível. A interface limita a estimativa ao XP que ainda cabe no personagem.

Confirmação, mudança de estado da expedição e retorno/perda de itens ocorrem na mesma transação durável. Chamadas repetidas de extração/perda encontram a expedição já encerrada e não concedem XP novamente. O último ID de resultado e o ganho efetivo ficam disponíveis para apresentação. As identidades de expedição são geradas no servidor, não escolhidas pelo cliente.

Recuperação de uma expedição iniciada aplica somente metade do saldo persistido e encerra sua reserva; não devolve o equipamento inicial. Antes do início efetivo, rollback restaura mochila, equipamento e comida sem XP. Rollback explícito de falha ao iniciar também funciona se parte das reservas já tiver sido confirmada antes de uma falha de preparação. Não inventa recompensa para ações que não chegaram a ser registradas.

Falhas de persistência de uma recompensa são registradas em log e não interrompem o processamento do dano. Apenas o que tiver sido gravado participa da recuperação; não há promessa de reconstruir mortes ou objetivos não persistidos. O fluxo usa o armazenamento compartilhado existente, sem serviço online adicional.

Bots e laboratório não recebem progresso persistente, XP PvP ou moedas. A sessão de teste continua isolada do perfil normal. Não foi criado comando público para conceder XP ou pontos arbitrários.

## Interface

Abra a mochila e use **Personagem**. A ficha mostra nível, avanço até o próximo nível, pontos, atributos-base/efetivos e saldo provisório. Na base, os cinco botões de investimento ficam disponíveis com pontos e sincronização; durante a expedição, a ficha é de consulta. **Mochila** retorna ao inventário. O resultado da partida inclui XP efetivamente confirmado, nível e pontos.

Textos, toggle e botões foram autorados pontualmente no prefab `Inventory`, preservando os objetos existentes; o presenter não recria a hierarquia durante a partida. Temas Simples/Sombria e área segura continuam compartilhados. O resumo detalhado de loot/resultados continua na etapa 10.

## Persistência e compatibilidade

- Perfil: versão **2 → 3**, com saldo e chaves da expedição e último resultado.
- Documento compartilhado: versão **4 → 5**, incluindo histórico privado dos pares PvP.
- Protocolo: **26 → 27**, com estado de progressão replicado somente ao dono.
- Backup de migração usa `.pre-v5-<guid>.json`, preservando backups anteriores.

A atualização não redistribui armas de perfis já migrados para os slots A/B. Nível e pontos são derivados do XP e dos investimentos legítimos; investimentos inválidos ou acima do orçamento impedem carregar o perfil, preservando o arquivo para revisão. Não foram alterados perfis reais nem iniciados servidores nesta tarefa. Antes de atualizar a instalação, encerrar/recuperar expedições e atualizar salas e clientes juntos, como nas etapas anteriores.

## Revisão e limites

Revisão estática de custo de níveis, orçamento de pontos, atribuição de dano, rateio, chaves de evento, limites de exploração/PvP, resultados repetidos, desconexão, recuperação, rollback, snapshots privados e referências autoradas da interface. `git diff --check` sem erros nos arquivos rastreados verificados. Nenhum teste automatizado, compilação manual, sessão Unity ou build de validação executado, conforme AGENTS.md. Não está validado em execução.

A avaliação prática deve cobrir duas pessoas dividindo uma morte, extração/perda/reconexão, ganho cruzando múltiplos níveis, investimento concorrente/repetido, limite de dez pontos por atributo e de 20 níveis, mesmo par PvP em ambas as direções, exploração repetida e isolamento do laboratório. Não usar verificações antigas como evidência de compatibilidade sem adaptar suas expectativas de recuperação e versões de save.
