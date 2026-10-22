# Correção da perseguição da aranha — 12/10/2026

Usuário relatou que a aranha usava teia, mas não atacava o player. Evidência estática: StepArchetype compartilhava entre Acid e Weaver a regra de recuar quando attackRange < distância < 4. Com attackRange de 1,2, a Weaver invertia a direção precisamente ao se aproximar do ataque comum; MoveBehaviour também girava o corpo na direção do recuo, prejudicando sua aquisição por cone.

Correção pontual: recuo permanece somente para Acid. Weaver segue a posição observada do alvo depois de lançar a teia, sem consultar posição escondida; ao alcançar 1,2 unidade usa TryMelee/StepMelee existentes, preparação de 0,3 s, dano 15 e revalidação no impacto. A teia continua causando lentidão, não dano direto. Fuga, cobertura e alcance de perseguição ainda permitem evitar o ataque.

Preservados preparação/cooldown da teia, armadilhas, stagger de movimento, navegação, animação e regras dos demais arquétipos. Revisão estática do fluxo e git diff --check; sem execução Unity, builds ou atualização/reinício de servidor. O problema relatado não foi reproduzido numa sessão controlada nesta entrega; a correção do código ainda requer observação em partida. Servidor precisa incorporar a alteração para mudar a decisão da IA; protocolo permanece 19.
