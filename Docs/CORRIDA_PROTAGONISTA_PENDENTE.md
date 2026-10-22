# Corrida do protagonista — pendência

## Atualização em 16/10/2026

Corrida e stamina implementadas na [etapa 3](Integracao/EQUIPAMENTOS_ETAPA_3_2026-10-16.md), com botão segurado/personalizável, Shift, valores iniciais do plano e bônus de regeneração por comida. Animação permanece vinculada à distância percorrida. Validação prática pendente; revisão de perseguição dos monstros e eventual ruído extra continuam em aberto. Os registros abaixo preservam o histórico anterior à implementação.

## Velocidade, animação e comparação com monstros

Registrada em 15/10/2026 por relato do usuário. Status: aberta, sem implementação nesta etapa.

- Ajustar a velocidade de deslocamento do protagonista em conjunto com sua animação, para que a cadência e o comprimento dos passos correspondam ao movimento no mapa.
- Segundo o teste do usuário, o protagonista atualmente é muito mais rápido que os monstros. Revisar essa diferença no balanceamento de caminhada, futura corrida e perseguição de cada arquétipo.
- Considerar a stamina já prevista ao definir a corrida e as possibilidades de fuga; valores finais ainda não foram definidos.
- Ampliar as mecânicas dos monstros além das atuais, conforme [pendência de encontros e ameaça](Mapas/CATACUMBAS_RELATOS_2026-10-15.md).

Registro baseado no relato do usuário, sem reprodução ou medição pelo agente. Nenhuma velocidade, animação ou regra alterada; nenhum teste ou build executado nesta etapa.

## Corrida e stamina — registro anterior

Registrada em 12/10/2026. Retomar a discussão hoje às 21h30 (America/Sao_Paulo), conforme pedido do usuário. Não iniciar a implementação automaticamente.

- Implementar corrida para o protagonista.
- Proposta a discutir: botão independente na HUD, acionado ao segurar, com tamanho e posição personalizáveis.
- Definir aumento de velocidade, duração/limitações e interação com combate.
- Avaliar aumento de ruído e atração de monstros; essa regra ainda não foi aprovada.
- Stamina é um requisito confirmado pelo usuário: a corrida deve consumir esse recurso. Definir capacidade, consumo, regeneração e apresentação na HUD.
- Avaliar um bônus do status Bem alimentado para stamina, conforme sugestão do usuário. Decidir se aumenta a capacidade, melhora a regeneração ou reduz o consumo; valores e efeito exato ainda não foram definidos.
- Preservar autoridade do servidor para movimento e regras de gameplay.

A implementação da corrida e a existência de stamina foram confirmadas. Os demais detalhes continuam como pontos de discussão. A implementação depende da retomada com o usuário.

O lembrete automático das 21h30 não foi criado: a chamada de agendamento foi recusada. O horário permanece apenas como registro nesta pendência.
