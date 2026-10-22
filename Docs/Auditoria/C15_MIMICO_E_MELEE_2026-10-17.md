# C15 — Mímico revelado ao receber melee

Implementada e revisada estaticamente em 17/10/2026. **Sem compilação, testes, Unity ou builds.** Referência E3-02; verificação em execução pendente da T11.

## Causa e correção

O golpe melee já validava alvo, alcance, arco, parede e deduplicação por entidade, aplicando `Health.ServerTakeDamage` uma vez. A revelação do mímico estava em `Monster.ProjectileDamage`, por onde o melee não passa. O evento de dano recebido estava ligado somente à reação de stagger.

- `Assets/Scripts/Enemies/MonsterArchetypes.cs`: extraída `RevealFromDamage`, compartilhada pelo caminho de projétil e pela reação ao dano efetivo. Exige servidor, entidade spawned, dano positivo e mímico ainda disfarçado. A transição existente `Reveal` continua cuidando do estado, prazo e colisão.
- `Assets/Scripts/Enemies/MonsterBehaviour.cs`: o callback já assinado em `Health.OnDamagedServer` aciona essa reação antes da saída para dano letal e depois mantém as regras originais de stagger. Melee e outros danos positivos recebidos pela Health deixam de ignorar o disfarce.

A revelação muda imediatamente o estado que define `Disguised`, portanto pellets/eventos subsequentes não reiniciam a transição. Não há chamada extra de dano, novo evento de Health, nova assinatura, multiplicação de loot/XP ou passagem do melee pela redução frontal da carapaça. Nenhuma alteração de arma, stamina, geometria de ataque ou rede.

O projétil mantém sua revelação antecipada e alerta existentes; a função compartilhada agora recusa dano não positivo. O evento posterior de Health não revela novamente um mímico já em transformação. Em golpe letal, a reação acontece antes do evento de morte/despawn; isso não promete que o cliente verá uma animação de transformação antes de o inimigo desaparecer.

## Revisão e pendências

Revisados estaticamente o golpe melee, o evento de Health, assinatura/remoção do handler do monstro, cálculo de projétil e estados da transformação. Consultadas as skills de investigação, C# e AI. Nenhum teste ou observação prática foi executado; balanceamento, prefabs e assets foram preservados.

Na T11, atingir um mímico disfarçado com melee válido e conferir dano único, início da revelação e reação após o prazo existente. Repetir durante a transformação, com golpe letal e com vários colliders; não deve reiniciar transformação nem duplicar XP/loot. Golpes fora de alcance/arco ou bloqueados por parede não devem revelar. Conferir tiro/pellets, abertura manual do disfarce e monstro comum/carapaça sem regressão. Não tratar esta revisão estática como aceite em jogo.
