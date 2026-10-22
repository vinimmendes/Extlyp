# C14 — Instrução do mecanismo somente no contexto

Implementada e revisada estaticamente em 17/10/2026. **Sem compilação, testes, Unity ou builds.** Referência E6-03 e relato da corrente; aceite visual pendente da T14.

## Causa e alteração

O aviso global do HUD usava `DungeonContent.ExitStatus` da saída selecionada, sem filtro de distância. Esse método devolvia instruções de operação com duração, inclusive “Erguer grade · 6 s” e “Soltar corrente · 4 s”. O botão contextual próximo ao mecanismo era outro caminho; escondê-lo não retirava o texto global.

- `Assets/Scripts/Match/DungeonContent.cs`: o estado global passou a indicar “Grade fechada” ou “Barca presa”. Mantidos “Pronta”, o requisito/localização da manivela e o estado não verificado. A orientação da saída selecionada continua disponível, sem repetir ação ou duração.
- `Assets/Scripts/Match/DungeonExpedition.cs`: o mecanismo de uma operação já ativa também precisa estar no alcance de interação de 2,3 unidades e com linha livre para aparecer no contexto. Isso evita reter a instrução enquanto a replicação do cancelamento ainda não chegou. Mecanismos já concluídos deixam de fornecer essa instrução.

`DungeonActionLabel` continua mostrando ação, duração/progresso e cancelamento no botão próximo ao mecanismo. uGUI e IMGUI usam o mesmo seletor contextual. `ExitKnowledge` também usa o estado global, portanto o HUD alternativo recebe o texto de estado consistente. Não foram alterados tempos, requisitos, extrações, layout, temas, atributos, autoridade, rede ou assets. Consultadas as regras existentes de UI/UX e direção de arte; as referências visuais foram preservadas.

## Revisão e pendência

Conferidos estaticamente os consumidores de `ExitStatus`, `ExitKnowledge`, `ContextDungeonAction` e `DungeonActionLabel`, incluindo aviso global e interação nos dois HUDs. Nenhuma captura ou observação em jogo foi produzida.

Na T14, aproximar e afastar dos mecanismos de grade/corrente, inclusive durante a operação e com atraso de replicação. A ação/duração deve desaparecer fora do alcance ou atrás de parede; o destino e seu estado geral podem continuar visíveis. Conferir manivela, atalho, saída já pronta e início/cancelamento legítimo. Validar os dois temas, uGUI e IMGUI, sem tratar esta revisão estática como aceite visual.
