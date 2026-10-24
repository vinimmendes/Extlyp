# Aba de perfil do jogador — planejamento

Status: planejamento histórico, substituído pelo plano aprovado e pela [implementação de 23/10/2026](PERFIL_IMPLEMENTACAO_2026-10-23.md). Decisões pendentes abaixo foram fechadas nessa entrega.

## Escopo e decisões

Criar uma aba Perfil na central da base para consultar o nome, a progressão, os resultados de expedição, os monstros eliminados e as causas de morte. O perfil pertence à identidade autenticada existente. Nomes podem se repetir e terão alteração com limite ou custo ainda pendente de definição.

Regra confirmada pelo usuário: o XP de um monstro deve ser concedido somente a quem participou da morte dele. Proximidade, estar na mesma partida ou pertencer ao mesmo grupo não bastam.

No código atual, `Health.Experience` considera dano efetivamente causado por participantes elegíveis nos últimos 15 segundos, divide o XP proporcionalmente e distribui os restos inteiros. O planejamento mantém esse critério existente; não altera implicitamente a janela nem cria XP individual integral para todos. Quem não contribuiu recebe zero. Monstro sem contribuinte elegível não distribui XP. O valor obtido na morte continua sujeito às regras existentes de confirmação da expedição.

## Apresentação proposta

Uma única aba, em paisagem, com seções internas Resumo, Expedições, Combate e Criaturas. Cabeçalho com nome, nível e progresso de XP; acesso à alteração de nome quando permitido. Os temas Simples e Sombria compartilham estrutura, área segura e navegação. Usar uGUI/TMP e prefabs existentes, sem refazer a ficha de atributos ou o inventário.

### Resumo

- Nome confirmado, nível, XP atual e progresso até o próximo nível, respeitando o nível máximo.
- Expedições iniciadas, extrações com vida e taxa de extração.
- Mortes, criaturas eliminadas e abates de jogadores humanos.
- Tempo em expedição e maior sequência de extrações com vida.
- Indicação de quando a coleta de estatísticas começou.

### Expedições

- Total iniciado e resultados separados: extração com vida, morte, tempo esgotado e interrupção por desconexão/recuperação técnica.
- Resultados ainda em andamento separados dos concluídos.
- Taxa de extração: extrações / resultados encerrados, incluindo interrupções de expedições efetivamente iniciadas. Mostrar também os totais que compõem a taxa; sem resultados, exibir “—”.
- Sequência atual e recorde de extrações; qualquer encerramento sem extração interrompe a sequência. Falha antes de iniciar não conta como expedição nem quebra a sequência.
- Tempo total efetivamente ativo, duração média e maior duração. Não contar tempo offline, espera de conexão ou permanência na base.
- Distribuição por mapa e saída utilizada, quando houver extração confirmada.
- Histórico limitado das últimas 20 expedições: data do servidor, mapa, resultado, duração, abates, assistências, causa de morte e XP confirmado. O limite é proposta de produto.

Interrupção não deve ser apresentada como abandono voluntário: queda de rede não comprova intenção. Se o servidor reiniciar sem evidência suficiente, usar resultado técnico/desconhecido; nunca inventar monstro causador da morte.

### Combate

- Criaturas eliminadas, assistências e participações em eliminações, em contadores distintos.
- Abates de humanos separados dos abates de bots.
- Mortes por criatura, humano, bot, ambiente e causa desconhecida.
- Dano efetivo causado e recebido, separando criaturas e participantes; cura efetiva recebida. Não contar excesso de dano sobre vida restante nem cura desperdiçada.
- XP obtido em combate separado do XP efetivamente confirmado no resultado. Não apresentar soma histórica como XP disponível para subir de nível.

Proposta de atribuição: o golpe fatal recebe um abate; outros contribuintes elegíveis da janela de dano recebem assistência. Participações representam a união dos dois. Não duplicar um abate para cada pessoa que recebeu XP. Participação com dano válido pode resultar em zero XP após arredondamento; continua sendo participação. A aprovação dessa semântica de estatísticas fica separada da regra de XP já confirmada.

Estatísticas reais de combate não devem depender de limites de recompensa: uma eliminação válida continua registrada mesmo quando não concede mais XP, inclusive por proteção contra repetição de PvP. Preservar essas proteções, sem transformá-las em regras artificiais de contagem.

### Criaturas

Tabela por espécie: nome, eliminações, assistências, participações e mortes causadas ao jogador. Incluir os arquétipos de `Monster`, Esporíferos e as variantes relevantes de `PlantCreature`, com identificadores estáveis, independentes do texto traduzido.

Registrar a espécie no momento do evento, inclusive inimigos que mudam de forma. Mortes por veneno, projétil ou efeito persistente devem conservar a origem mesmo após o agressor desaparecer. Se não existir informação confiável, usar desconhecido. Não tentar reconstruir espécie a partir de `KillerCreature`, que hoje é apenas texto e também contém nomes de humanos e bots.

Proposta visual: lista textual completa com zeros na primeira entrega. Ilustrações, silhuetas bloqueadas e revelação por encontro pertencem a uma futura decisão de bestiário; não criar uma regra de descoberta implicitamente.

## Persistência e autoridade

- Manter estatísticas por conta no perfil persistente do servidor, com schema próprio, versão de migração e data inicial de coleta. Não usar PlayerPrefs como fonte dos totais.
- Perfis existentes começam com contadores zero e indicação de coleta recente. Não deduzir histórico de mortes, abates ou extrações a partir do nível, inventário ou XP atual.
- Utilizar contadores de 64 bits, validação de valores e limites de histórico. Totais agregados não dependem de manter uma lista ilimitada de eventos.
- Registrar eventos exclusivamente no servidor a partir de dano, morte e transições reais da expedição. O cliente consulta snapshots; não envia contadores ou nomes de monstros para contabilização.
- Cada evento precisa de identificação para impedir contagem duplicada em callbacks, repetição de mensagens, retentativas e reconexões. O mesmo resultado terminal não pode virar extração e morte.
- Registrar a extração na transação que efetivamente salva itens/XP (`Bank`), não no início do temporizador nem na animação. Registrar a partida iniciada após confirmação de entrada (`Commit`), não na reserva de equipamentos.
- Finalizar resultados com causa explícita. Hoje `Lose`, recuperação de sessão e encerramento de XP convergem para caminhos genéricos; ampliar os contratos para preservar morte, tempo esgotado e interrupção antes de limpar a expedição.
- Persistir eventos de combate com estratégia de fila/lote e repetição segura, sem gravar o perfil inteiro a cada ponto de dano. Coordenar eventos de XP e estatísticas para resistir a falhas e à perda de posse da sessão. A janela exata de durabilidade de métricas acumuladas precisa ficar explícita na implementação.
- Abates e mortes efetivamente ocorridos permanecem nas estatísticas mesmo se o jogador não extrair. A penalidade de XP não apaga o histórico do combate.
- Excluir laboratório, sandbox, treino e eventos de depuração dos totais oficiais. Bots encontrados em expedições normais usam sua categoria própria.
- Apenas o dono consulta o histórico completo na primeira versão. Outros jogadores recebem somente o nome já previsto; perfil público, ranking e inspeção de terceiros não fazem parte desta entrega.

## Pontos de integração identificados

- `CampModels`, `CampProfileMigration` e `CampProgress`: dados, cópia, migração, validação e transações de resultado.
- `Health` e `Health.Experience`: dano efetivo, contribuições, evento único de morte e atribuição estruturada. Separar estatísticas da concessão de XP; cobrir também plantas e fontes ambientais.
- `PlayerInventory`, encerramento da partida e recuperação de sessão: resultado explícito por participante, inclusive extração já confirmada seguida de desconexão.
- `CampNetwork` / transferência de snapshots: distribuir o perfil sem exceder limites de payload. Páginas de histórico ou snapshots específicos se necessários, sem replicar o histórico a todos na expedição.
- Central da base e ficha existentes: botão/aba Perfil, filtros e listas com rolagem, estados de carregamento, vazio e falha. Consultar a referência de ScrollRect da skill uGUI antes de implementar listas.

## Ordem proposta

1. Fechar as regras propostas de abate/assistência, taxa de extração e duração do histórico, junto às decisões pendentes de nomes.
2. Implementar schema e migração dos dados de perfil; tipar fontes de dano e resultados de expedição.
3. Registrar início, encerramento e mortes/abates com persistência idempotente; integrar os contribuidores de XP sem ampliar os beneficiários.
4. Implementar a aba com Resumo, Expedições, Combate e Criaturas, incluindo o nome escolhido e os estados de sincronização.
5. Depois da primeira avaliação, ampliar economia e equipamento: valor realmente extraído, gastos/ganhos no comerciante, fabricação e uso por arma, cada um com definição própria. Não chamar preço de catálogo de lucro nem movimento entre mochila e baú de loot coletado.

## Avaliação prevista

Revisão estática antes da entrega; não iniciar testes, Unity ou builds apenas para validar por padrão, conforme AGENTS.md. Quando o usuário solicitar ou relatar problema, verificar o caso e dependências diretas.

Roteiro de avaliação prática: perfil novo e migrado; dois jogadores contribuindo com danos diferentes e um terceiro apenas próximo (somente os dois contribuintes recebem XP); golpe fatal e assistência; dano fora da janela; planta/Esporífero; veneno após despawn; abate sem XP por limite; extração seguida de desconexão; morte seguida de callback repetido; tempo esgotado; reserva cancelada; queda de rede/servidor; reinício e persistência; laboratório sem afetar totais; dois aparelhos com nomes iguais e perfis independentes. Conferir a aba nos dois temas e com teclado Android em paisagem.
