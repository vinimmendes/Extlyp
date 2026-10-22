# Revisão do loot dos baús — histórico e implementação

Atualização em 16/10/2026: a etapa 8 do plano de equipamentos/loot implementa presença de 100% e uma peça garantida para recipientes valiosos/fechados, com 3–4 pilhas no total. Recipientes comuns mantêm presença de 90%. O conteúdo passa a ser sorteado uma vez por objeto no servidor, separado da qualidade de abertura. Consulte [Etapa 8](Integracao/EQUIPAMENTOS_ETAPA_8_2026-10-16.md).

Evidência estática: o sorteador anterior aplicava uma chance de presença ao conjunto, inclusive aos baús de qualidade alta, permitindo conteúdo vazio. Isso não comprova que a ocorrência relatada teve essa causa; apresentação, importação e sincronização ainda aguardam validação prática. Não foram executados testes ou sessões Unity nesta entrega.

## Registro original de 12/10/2026

Registrada em 12/10/2026. O usuário relatou ter aberto um baú raro sem itens. Não houve reprodução ou investigação nesta etapa.

- Investigar se o baú raro realmente foi gerado vazio ou se houve falha na apresentação, sincronização ou transferência do conteúdo.
- Revisar as probabilidades atuais de geração por raridade e condição de trancado.
- Requisito confirmado: baús trancados devem ter maior chance de conter loot que baús equivalentes destrancados, compensando o custo e o esforço de abertura.
- Definir se baús raros/trancados podem ficar vazios ou devem ter algum conteúdo mínimo garantido. O usuário ainda não determinou uma garantia de loot.
- Definir o bônus de probabilidade e avaliar sua relação com raridade e qualidade/quantidade, sem assumir que maior chance implica recompensa garantida.
- Manter a geração e concessão de itens autoritativas no servidor, sem alterar o inventário para corrigir apenas a aparência.

Pendência para retomada posterior; nenhuma probabilidade, recompensa ou regra foi alterada neste registro.
