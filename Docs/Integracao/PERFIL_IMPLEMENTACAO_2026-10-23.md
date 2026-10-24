# Perfil, nome e arma preferida — implementação

Implementado no código em 23/10/2026. Revisão estática; sem execução de testes, Unity ou builds, conforme preferência do projeto. A avaliação prática permanece com o usuário.

## Interface e nome

A central da base possui a sexta aba Perfil, preservando Personagem e seus atributos. O prefab `Assets/Resources/UI/Prefabs/PlayerProfile.prefab` contém campo de nome, confirmação, arma preferida com ícone e seções Resumo, Expedições, Combate, Criaturas e Histórico. Usa a área segura horizontal e os temas Simples/Sombria. O conteúdo longo tem rolagem.

Nome pertence à conta autenticada existente. Primeira escolha gratuita; nomes repetidos permitidos. Normalização NFC, espaços internos condensados, de 3 a 20 elementos de texto, letras, acentos, números, hífen e sublinhado; controles e marcação recusados. Limite de transporte de 120 bytes UTF-8, com replicação FixedString128Bytes. Alteração gratuita a cada sete dias; confirmar o mesmo nome não renova o prazo. O servidor valida todas as regras e a revisão da ação. Expedição normal exige nome salvo; nenhum nome flutuante foi adicionado.

Na base de testes, nome e estatísticas oficiais continuam vindo da conta. Alterar o nome ali salva a conta real; inventário e atributos de treino continuam isolados. A progressão exibida durante testes corresponde ao perfil de treino.

## Contagem e armazenamento

- Abate no golpe fatal e assistências para os demais contribuintes dos últimos 15 segundos; humanos, bots e criaturas separados. Limites de XP não retiram abates/assistências.
- XP de criaturas mantém divisão proporcional ao dano elegível nos últimos 15 segundos. Espectadores recebem zero.
- Dano e cura registram apenas valores efetivos. Arma e origem são capturadas no ataque; a arma preferida soma todos os tipos de abate oficial, com desempate pelo mais recente.
- Início registrado na confirmação da expedição. Extração e XP confirmado são salvos na mesma transação dos itens. Resultados encerram uma única vez; desconexão é interrupção, não abandono voluntário. Uma morte já registrada prevalece sobre interrupção ou tempo esgotado.
- Histórico limitado às últimas 20 expedições, entregue somente ao dono. Totais agregados não são limitados ao histórico.
- Eventos têm ID, expedição e credencial da sala, com deduplicação e retentativa. Mortes e encerramentos provocam gravação imediata. Dano, cura e tempo consolidam em até dois segundos e antes do encerramento; falha abrupta pode perder os eventos desse intervalo. Não são reconstruídos dados ausentes.
- Laboratório, sandbox e treino não produzem estatísticas oficiais. Não há ranking ou perfil público.

Schema do perfil e envelope de armazenamento passam de 6 para 7. Migração preserva inventários, equipamento, identidade e progressão e inicia estatísticas vazias com data de início da coleta. O carregamento de disco recupera reservas/expedições pela política existente, antes da migração; não inventa histórico legado. Backup de migração segue o mecanismo existente.

Protocolo de rede: **44**. Inclui a identificação replicada de morte por humano: a tela mostra “Morto pelo player: Nome”. Cliente e todas as salas do servidor devem ser atualizados juntos; encerrar as salas antigas antes de iniciar as novas. Nenhum executável ou APK foi gerado nesta entrega.

## Roteiro para primeira avaliação prática

1. Conta antiga e nova: sincronizar, escolher nome, reconectar e conferir inventário/progresso preservados. Testar acentos, espaços, 3/20 caracteres, nome inválido e repetido entre contas.
2. Confirmar o mesmo nome e verificar que o prazo não muda; tentar alteração antes de sete dias e conferir recusa. Após o prazo, confirmar uma troca.
3. Dois jogadores causam dano e um apenas observa: conferir abate, assistência e XP proporcional, incluindo participante que recebe zero por limite/arredondamento.
4. Abater criaturas, humanos e bots com armas diferentes; conferir favorito e desempate por abate mais recente. Trocar arma após o ataque não deve reatribuir sua origem.
5. Morrer para cada espécie e para efeitos persistentes; conferir causa, estatísticas de combate preservadas e histórico.
6. Extrair e desconectar imediatamente; conferir um único resultado e itens/XP confirmados. Repetir pedidos de finalização não deve duplicar estatísticas.
7. Desconectar durante expedição, esgotar o tempo e reconectar após reinício: conferir os resultados e duração registrada, sem criar dados perdidos.
8. Jogar laboratório/treino e retornar à base normal: totais oficiais não mudam.
9. Conferir as cinco seções, rolagem, teclado do nome, controle/teclado, área segura e temas nos aparelhos horizontais usados pelo jogador.
