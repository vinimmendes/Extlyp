# Avaliação dos monstros — 11/10/2026

## Alterações

- Charger: avanço de 40 para 38 unidades/s (5% menor). Preparação de 3 s, duração de 0,5 s, dano 40 e recuperação de 2 s preservados. O alcance resultante passa de 20 para 19 unidades; o indicador usa a mesma velocidade.
- `MonsterEffect` recebeu `[Serializable]`. A lista enviada em `ce-effects` usa JsonUtility: implementar INetworkSerializable não torna os campos serializáveis nesse outro transporte. Sem o atributo, teias, ácido, cadáveres e gritos não chegavam corretamente ao cliente.
- Teia: sprite original gerado por código, com 12 raios, anéis curvos e fundo transparente. Ácido: poça irregular com borda, manchas e bolhas. Sprites em cache, sob os personagens e sujeitos à visão existente; não revelam efeitos atrás de paredes.
- Projéteis de habilidades ignoram triggers, assim como a investida. Gatilhos de extração e interação não devem interceptar teia/ácido.

## Revisão das habilidades no código

| Monstro | Comportamento encontrado | Observação |
| --- | --- | --- |
| Charger | Prepara, acompanha a mira, avança, colide com cenário/jogador e recupera | Varredura circular; outros monstros não interceptam a investida. |
| Listener | Visão curta (1,5), alcance auditivo ampliado até 50, investiga ruído | Usa o mesmo evento de tiros e gritos. |
| Shell | 220 de vida, lento, recebe 25% do dano de projéteis pela frente | Flancos e costas recebem dano integral; tiros dos bots usam a mesma regra. |
| Weaver | Prepara disparo em 0,8 s, lança teia e deixa armadilhas | Teia reduz movimento à metade; destruída por tiro. Limite local de 3 armadilhas. |
| Herald | Prepara grito por 2 s, alerta monstros num raio de 45 | Dano interrompe a preparação. Grito visual também afetado pela falha de JSON. |
| Mimic | Imita baú, revela ao abrir ou receber tiro, prepara por 0,75 s | Interação exige proximidade, jogador vivo e caminho livre; seleção aleatória é feita no servidor. |
| Acid | Prepara por 1,2 s, projétil causa 10 e cria poça de raio 2,3 por 8 s | Poça causa 4 a cada 0,5 s; paredes impedem dano através delas. |
| Stalker | Prefere alvos isolados, aproxima pelas costas quando fora da luz | Luz reduz velocidade para 1,5; ataque pelas costas causa 30. |
| Devourer | Busca cadáver, alimenta por 2,5 s, cura 45 e cresce até 5 vezes | Cada crescimento soma 25 de vida máxima e 4 de dano; recebe dano duplo ao alimentar e pode ser interrompido. |
| Esporífero | Acumula dano de 25% da vida máxima e prepara nuvem por 1,2 s | Nuvem dura 8 s; morte cancela liberação. |
| Planta-mãe / bulbos | Até 3 atacantes e 2 curadores; maturação em 9 s; cura de 12 | Cura revalidada no impacto; destruir a mãe remove seus filhos. |

Esses comportamentos foram rastreados no código, não todos reproduzidos em partida. Não há evidência suficiente para afirmar que toda a IA funciona em execução, especialmente navegação, física, percepção e RPCs entre aparelhos.

## Verificação pontual

`MonsterAbilityChecks.Run` verifica round-trip JSON de todos os seis tipos de efeito, dimensões/transparência/contraste dos sprites, parâmetros reais do prefab do Charger e regras de limites/cura das plantas e limiares dos esporos. Gera relatório e PNGs em `Logs` da cópia isolada. Não recria cenas nem prefabs; não inicia partida e não gera builds.

Cliente e servidor precisam incorporar a alteração de serialização. O executável já instalado não é atualizado por esses testes.

Resultado executado: **18 verificações passaram**, Unity 6000.0.84f1, cópia `Builds/ServerValidation`, saída 0. Log: `Logs/monster-ability-unity.log`. Os testes detectaram e permitiram corrigir transparência excessiva no novo sprite e ausência da velocidade explícita no prefab. Arte exportada e inspecionada em `Logs/monster-Web.png` e `Logs/monster-AcidPool.png`. A execução sem gráficos não comprova renderização durante uma partida.
