# Equipamentos — etapa 2: slots e operações

Implementação no código em 15/10/2026; validação prática pendente. Preserva Unity 6000.0.84f1, NGO 2.13.3 e Transport 2.7.4.

## Entrega

- Seis slots: cabeça, torso, mãos, pés, amuleto e anel. Dois slots adicionais de arma: A/B. Mochila continua com nove espaços; peças equipadas não ocupam a mochila.
- Inventário uGUI existente ampliado pontualmente, com oito slots autorados no prefab `Inventory`, sem reconstrução da hierarquia durante o jogo. Os dois temas compartilham a composição, calculada na área segura existente.
- Ficha apresenta nível e atributos-base com contribuições do equipamento separadas. Selecionar um item permite consultar nome, raridade e bônus, comparando com uma peça equipada. Os efeitos de combate desses atributos continuam reservados para a etapa 5.
- Operações de equipar, guardar e substituir usam o servidor. Equipar troca a peça pelo item no mesmo espaço da mochila, permitindo substituir equipamento com mochila cheia. Para guardar sem substituição, escolher um espaço vazio. Slot incompatível é recusado.

## Uso da interface

1. Abra a mochila na base ou na expedição.
2. Selecione uma arma/peça da mochila e toque no slot compatível. Também é possível arrastar da mochila para o slot.
3. Para guardar uma peça equipada, toque nela sem uma peça da mochila selecionada, ou arraste para um espaço vazio da mochila.
4. Para trocar uma peça ocupada, arraste um item compatível para o mesmo slot. Recipientes não equipam diretamente: transferir primeiro para a mochila.

Na base, a troca é imediata e usa a revisão do perfil. Na expedição, dura dois segundos: movimento superior a 0,05 unidade, dano, disparo, fechamento da mochila ou alteração dos itens envolvidos cancela a operação. Não permite iniciar durante recarga, troca de arma, ferrolho, extração ou outra interação bloqueante. O inventário mostra a contagem de tempo. A operação compara os itens capturados no pedido com o estado atual no servidor e revalida antes de concluir.

Peças equipadas não são descartadas ao arrastar para fora da janela; primeiro devem ser guardadas na mochila. Não há mudança de vida máxima, cura ou stamina por equipamento nesta etapa.

## Armas e expedição

Somente A/B participam da alternância rápida. Armas na mochila são saque, sem disparo ou recarga direta. Munição de reserva continua na mochila; munição carregada permanece no exemplar equipado. Estado público de arma e tempos existentes de troca são preservados, sem expor o equipamento privado de outros participantes.

O início de expedição reserva mochila e equipamento juntos. Extração devolve ambos; rollback recupera os dois; morte derruba mochila e equipamento e limpa os slots. Perda de expedição remove a reserva correspondente. Recuperação básica considera também armas equipadas, evitando entregar outra pistola apenas porque a mochila está sem arma.

Bots e jogadores do laboratório inicializam armas nos mesmos slots. O laboratório mantém o comando existente de equipar arma, adaptado para A/B, e oferece seis novos itens provisórios no seletor de itens. Os IDs foram acrescentados ao fim do catálogo, preservando os anteriores. Cada peça provisória tem identidade, slot próprio, ícone geométrico e bônus informativo +1. Não foram adicionadas às tabelas de loot normal; as 18 peças definitivas continuam na etapa 7.

## Persistência e compatibilidade

- Perfil: `SchemaVersion` 1 → **2**.
- Documento compartilhado: versão 3 → **4**.
- Protocolo: 22 → **23**.

Perfis antigos passam pela migração anterior de identidade quando necessário e, depois, movem até duas armas da mochila para A/B em ordem dos espaços. Armas extras permanecem na mochila. Nível, XP, moeda, identidade, bônus, qualidade e munição não são rerrolados. A arma ativa de uma sessão antiga não é um campo persistido; na migração a primeira arma encontrada torna-se A. No novo formato, a arma ativa é salva com o conjunto equipado.

Backups anteriores à migração passam a usar `<cluster>.json.pre-v4-<guid>.json`, preservando os backups da etapa 1. Expedições legadas ativas não são migradas no meio de uma sala: manter o procedimento de encerrar/recuperar primeiro e atualizar todas as salas e clientes juntos. Nenhum servidor foi reiniciado, nenhum perfil real foi convertido e nenhum build foi criado nesta tarefa.

## Revisão e limites

Revisão estática de migração, reservas, transferências, perda/extração, referências à arma ativa, recarga, interface e IDs autorados no prefab. `git diff --check` sem erros de espaço nos arquivos verificados. Nenhum teste automatizado, compilação manual, sessão Unity ou build executado para validar a etapa.

Na avaliação prática, conferir mochila cheia, troca cancelada, atributos comparados, recarga/troca A/B, itens derrubados e retorno após extrair, além do inventário horizontal nos temas Simples/Sombria. Não usar os antigos checks de inventário como evidência desta versão: alguns ainda pressupõem armas utilizáveis na mochila e precisam ser adaptados quando testes forem solicitados.
