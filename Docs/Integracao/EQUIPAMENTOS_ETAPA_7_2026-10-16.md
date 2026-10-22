# Equipamentos — etapa 7

Implementação em 16/10/2026. Revisão estática realizada; importação dos assets, apresentação e comportamento em jogo aguardam a avaliação prática do usuário. Não foram executados testes, compilação, Play Mode ou builds.

## Catálogo entregue

Os IDs 35–52 acrescentam 18 modelos, preservando todos os IDs anteriores. As definições editáveis ficam em `Assets/Resources/Items/Equipment/Definitions`, com categoria, slot, nome, atributo principal e duas opções de secundário. `EquipmentCatalog` verifica o conjunto antes de disponibilizá-lo. Os assets são configuração compartilhada; os exemplares continuam no estado do servidor e no perfil.

| Slot | Modelo | Principal | Secundários possíveis |
| --- | --- | --- | --- |
| Cabeça | Capuz do Vigia | Percepção | Destreza / Vigor |
| Cabeça | Elmo do Guardião | Vitalidade | Força / Vigor |
| Cabeça | Máscara do Atirador | Destreza | Percepção / Vitalidade |
| Torso | Gibão do Batedor | Vigor | Destreza / Percepção |
| Torso | Cota do Combatente | Força | Vitalidade / Vigor |
| Torso | Couraça do Guardião | Vitalidade | Força / Percepção |
| Mãos | Luvas do Atirador | Destreza | Percepção / Vigor |
| Mãos | Manoplas do Combatente | Força | Vigor / Vitalidade |
| Mãos | Luvas do Explorador | Percepção | Destreza / Vitalidade |
| Pés | Botas do Andarilho | Vigor | Percepção / Destreza |
| Pés | Botas do Duelista | Destreza | Força / Vigor |
| Pés | Grevas do Guardião | Vitalidade | Força / Vigor |
| Amuleto | Medalhão do Olho | Percepção | Destreza / Vitalidade |
| Amuleto | Talismã do Fôlego | Vigor | Força / Vitalidade |
| Amuleto | Presa do Combatente | Força | Vigor / Percepção |
| Anel | Anel da Mão Firme | Destreza | Percepção / Vigor |
| Anel | Anel do Pulso Forte | Força | Vitalidade / Destreza |
| Anel | Anel da Persistência | Vitalidade | Vigor / Percepção |

## Exemplares e proteção

| Raridade | Principal | Secundário | Cabeça | Torso |
| --- | --- | --- | --- | --- |
| Comum | +1 ou +2 | Nenhum | 2% | 4% |
| Incomum | +2 ou +3 | +1 | 3% | 6% |
| Raro | +3 ou +4 | +1 ou +2 | 4% | 8% |

Valores inteiros uniformes; cada secundário elegível tem 50% de chance. A criação no servidor gera uma identidade e grava os bônus em `ItemInstance`. `EquipmentCatalog.Create` aceita a raridade explicitamente; o construtor habitual de um item novo cria uma peça comum. Transferência, troca, coleta, descarte e serialização usam os metadados existentes, sem sortear novamente. Equipamentos não empilham.

A entrada no inventário, o equipamento e a validação de propriedade do perfil rejeitam exemplares dessas peças sem identidade ou com bônus incompatíveis com modelo/raridade. Dados inválidos não são corrigidos com uma nova rolagem. Os seis itens `Training*` continuam legados, sem conversão aleatória e sem proteção implícita.

Cabeça e torso somam proteção física, até 12% neste catálogo, com teto sistêmico de 15%. O servidor aplica a redução antes de registrar dano, contribuição de XP e morte. O dano final é arredondado ao inteiro mais próximo, com mínimo de 1 por impacto; ataques pequenos podem manter o mesmo valor após arredondar. Disparos, golpes melee e ataques físicos dos monstros usam a redução. Esporos são classificados como veneno; projétil e poça de ácido como dano ambiental. Esses dois tipos ignoram proteção. Atributos de Vitalidade continuam aumentando a vida separadamente.

Não foram acrescentados velocidade de movimento, Inteligência, crítico, durabilidade, requisitos de nível, conjuntos ou multiplicador de dano de armas por raridade.

## Apresentação e laboratório

`EquipmentArt` desenha 18 silhuetas originais em código, com famílias de cabeça, torso, luvas, botas, amuletos e anéis. A mesma imagem aparece no inventário e no saque do chão. Cada `ItemDefinition.Icon` pode substituir sua silhueta futuramente. As formas usam aço, couro, tecido e marfim foscos conforme a direção visual existente; esta etapa não cria vestimenta no corpo do personagem.

Os slots mostram borda e texto por raridade, além das letras C/I/R. A descrição escreve Comum/Incomum/Raro por extenso, identifica principal e secundário e informa proteção física. A comparação mostra valores efetivos antes/depois, excedentes acima de 20 e diferença de proteção. A ficha apresenta a proteção total. A estrutura continua compartilhada pelos temas Simples/Sombria.

Para avaliação manual: no ambiente de testes, abra F2 > Equipar, escolha uma peça na lista, selecione Comum/Incomum/Raro e use Entregar item. A lista tem rolagem e altura proporcional ao catálogo. Com a mochila cheia, o mesmo exemplar sorteado cai no chão. O comando de raridade é validado pelo servidor e permanece restrito ao laboratório; os itens são temporários e não concedem progresso normal.

## Compatibilidade e próxima etapa

Protocolo de rede **28**: atualizar cliente e servidor juntos. Perfil continua no schema 3 e documento compartilhado na versão 5: identidade, raridade, origem e bônus já tinham campos persistentes; nenhuma conversão de perfil é necessária nesta etapa. Foram preservados cenas, prefabs e assets existentes.

`Tools/Items/create_equipment_definitions.py` registra o autoramento inicial dos assets e preserva arquivos/GUIDs existentes quando invocado novamente. Não executa Unity nem recria mapas.

As peças ainda não entram no loot normal. **Próxima etapa: 8**, tabelas por fonte/mapa/risco, distribuição de equipamentos e remodelagem do loot. Preços, comércio e redistribuição ficam na etapa 9. Esta entrega não declara validação em execução.
