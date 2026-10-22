# Pendências de interface

## Texto de soltar a corrente permanece ao se afastar

Registrada em 15/10/2026 por relato do usuário nas Catacumbas. Correção adicional aplicada em 17/10/2026; conferência visual em partida pendente.

- O texto “3 · Soltar corrente · 4 s” não desaparece da tela, mesmo quando o jogador se afasta do objetivo de soltar a corrente.
- Comportamento esperado: a instrução contextual deve desaparecer ao sair da proximidade do mecanismo; eventual orientação persistente de destino deve ser distinguida da indicação de interação próxima.
- Evidência fornecida pelo usuário; causa ainda não investigada e comportamento não reproduzido pelo agente.

![Texto de soltar a corrente persistente](../Auditoria/Evidencias/texto-soltar-corrente-persistente-2026-10-15.png)

A captura posterior “1 · Grade fechada” confirmou que a orientação da saída selecionada continuava permanente na HUD uGUI. O aviso de estado e de ferramentas agora aparece somente a até 2,3 unidades do mecanismo ou da saída selecionada e é limpo ao se afastar. O texto de extração em andamento permanece durante a extração. Revisão estática; sem sessão Unity ou build nesta correção.

## Símbolo da extração patinando no minimapa

Registrada em 15/10/2026 por relato do usuário. Correção aplicada em 15/10/2026; conferência visual em partida pendente.

- O símbolo da extração fica “patinando” (deslizando) no minimapa.
- Comportamento esperado: o marcador deve acompanhar corretamente a posição da extração na representação do mapa, sem deslocamentos indevidos em relação ao cenário.
- Evidência estática: os caminhos uGUI e IMGUI limitavam a posição do marcador às bordas do painel, separando-o da projeção do terreno. A limitação foi removida; extrações fora do recorte local aparecem no mapa ampliado, sem um ícone geográfico deslocado sobre outra sala.

Compilação C# concluída sem erros. Sem validação visual em Unity; detalhes em [correções dos relatos](../Mapas/CATACUMBAS_RELATOS_2026-10-15.md).

## Câmera ainda distante

Registrada em 15/10/2026 por relato do usuário. Ajuste aplicado em 15/10/2026; avaliação do enquadramento em partida pendente.

- A câmera ainda está longe demais durante o jogo.
- Aproximar o enquadramento para que personagem, monstros e detalhes do cenário fiquem maiores e mais legíveis.
- Tamanho ortográfico das Catacumbas reduzido de 10 para 7,5: elementos aproximadamente 33% maiores na mesma resolução. Demais mapas mantêm seus valores.

Compilação C# concluída sem erros; sem sessão Unity ou build de jogador.

## Diminuir o campo de visão neste mapa

Registrada em 15/10/2026 a pedido do usuário. Ajuste aplicado às Catacumbas em 15/10/2026; avaliação em partida pendente.

- Diminuir o campo de visão no mapa mostrado na captura do problema de iluminação, preservada em [evidência de 15/10/2026](../Auditoria/Evidencias/luz-parede-faixas-2026-10-15.png).
- Catacumbas: alcance de 16 para 10 unidades e abertura de 140° para 110°. Percepção próxima permanece em 2,4 unidades. Outros mapas mantêm 16/140°.
- Os valores são compartilhados pela luz, apresentação dos alvos e consultas de visão usadas pelo servidor/bots. A aproximação da câmera é o ajuste separado descrito acima.

Compilação C# concluída sem erros; sem sessão Unity ou build de jogador.

## Mochila na expedição — visibilidade do cenário

Registrada em 12/10/2026. Implementação autorizada e aplicada posteriormente: a mochila individual da expedição agora compartilha a transparência do saque. Conferência visual no jogo ainda pendente.

- Aplicar à mochila individual da expedição a mesma apresentação do menu de saque: cenário real em movimento ao fundo, sem a imagem ilustrada da cabana.
- Painel e preenchimento dos slots semitransparentes, preservando legibilidade de ícones, nomes, quantidades e bordas.
- Permitir observar a aproximação de monstros enquanto consulta ou organiza a mochila, sem pausar a partida.
- Manter os nove slots e as regras atuais de organização e descarte; não mostrar o baú seguro na expedição.
- Preservar o fundo e a apresentação próprios do inventário da base.
- Na implementação, distinguir contexto de expedição de `WorldMode`, que atualmente identifica somente o saque de recipiente.

## Tela de resultados e loot extraído

Registrada em 12/10/2026 por pedido do usuário. Apenas pendência, sem lembrete ou implementação imediata.

- Refinar a janela de resultados, seguindo a família visual dos menus aprovados: tema Sombria/Simples, textos editáveis, molduras discretas e botões reais em uGUI.
- Substituir o título técnico “RESULTS” por uma apresentação adequada ao resultado da partida.
- Avaliar um resumo do loot que o jogador conseguiu extrair, com ícones, nomes e quantidades dos itens efetivamente confirmados pelo servidor.
- Diferenciar extração concluída, morte e tempo esgotado. Não apresentar a mochila perdida como loot extraído.
- Preservar as regras de retorno à cabana, persistência e o encerramento da partida; considerar o tempo disponível para consultar os resultados.

Retomar o conceito e confirmar o escopo com o usuário quando esta melhoria entrar em execução. A sugestão de mostrar loot ainda precisa ser definida; não representa uma nova regra de recompensa.
