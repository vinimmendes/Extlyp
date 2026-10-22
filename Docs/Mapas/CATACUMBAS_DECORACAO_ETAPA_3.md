# Decoração — etapa 3: quebra e sincronização

Continuação implementada: [etapa 4 — distribuição variável e loot](CATACUMBAS_DECORACAO_ETAPA_4.md). A descrição abaixo registra a seleção fixa e a ausência de loot da terceira etapa.

Urnas, caixotes e barris agora recebem tiros e ataques corpo a corpo nas Catacumbas. Um acerto válido quebra o objeto, troca a apresentação por destroços planos e desativa sua área de acerto. Não há Health, experiência, coleta, loot ou ruído adicional de quebra nesta etapa. As armas mantêm munição, alcance, arco, tempos, stamina e ruído existentes.

## Presença e autoridade

O servidor instancia 48 objetos após configurar o mapa: os dois primeiros candidatos de cada sala interna, oito nas entradas e oito nos corredores. Essa seleção é fixa nesta etapa para permitir avaliar apresentação e quebra. A etapa 4 substituirá a escolha fixa pela variação aprovada e acrescentará os 10% de chance de recursos comuns. O catálogo e as posições continuam os da etapa 2; arte com leve inclinação preservada.

DungeonBreakable é um NetworkBehaviour separado do sistema de vida. Não recebe RPC de quebra do cliente: os caminhos autoritativos de tiros e golpes confirmam acerto, alcance e obstáculos antes de chamar ServerBreak. A transição só acontece em partida Running, no servidor, em objeto ainda intacto. Acertos simultâneos encontram o estado já quebrado e não repetem a transição.

Área de acerto: BoxCollider2D trigger de 0,9 × 0,9, no layer Default, criada no Awake e habilitada apenas no servidor enquanto intacto. Não entra nas máscaras de parede/visão, não bloqueia jogadores e não altera navegação. O filtro de tiro reconhece explicitamente o trigger do quebrável e termina o projétil no primeiro objeto válido, preservando a precedência de paredes, atores e teias. Corpo a corpo verifica o ponto mais próximo, alcance, arco e WallMask; não usa a lista de Health nem concede XP. O limite atual de três alvos vivos do ataque permanece inalterado; quebráveis dentro do arco são tratados à parte.

## Apresentação e rede

Sincronizados CandidateId, instante da quebra e Broken. Prefab em Resources registrado tanto na lista persistente de prefabs quanto pelo bootstrap existente. NetworkObject transmite posição/rotação no spawn; objetos são estáticos, sem NetworkTransform ou Rigidbody. Protocolo de conexão incrementado de 36 para 37: cliente e servidor devem ser atualizados juntos. Formatos de save não mudaram.

Usa ServerVisibility existente: clientes recebem/ocultam os objetos conforme visão. Novos observadores e entradas tardias recebem o estado atual, inclusive destroços. Efeitos antigos não são reproduzidos ao reexibir o objeto. O efeito local de quebra usa três pequenos fragmentos por 0,3 s, sem colisão; sprites de runtime e fragmentos são liberados no despawn. O reset da sala já despawna os objetos de rede e limpa a presença da expedição anterior.

Sprite intacto em ordem -5; destroços em -7, no mundo antes da composição de escuridão. Fonte intacta com pegada de apresentação de 0,9 unidade; destroços de 1,05. Servidor com dispositivo gráfico Null mantém somente estado e trigger, sem carregar texturas ou criar fragmentos.

Prefab autorado incrementalmente por `Tools/Author-DungeonBreakablePrefab.py`, baseado apenas nas estruturas existentes de Transform e NetworkObject do projeto. Não executa MvpSetup.Build nem recria cenas ou outros prefabs.

## Revisão e aceite pendente

Revisão estática: sintaxe dos sete scripts alterados/criados; referências do prefab, registro e protocolo; filtro de tiros e golpes; estado único e liberação de recursos. Não foram iniciados Unity, testes automatizados ou builds. A análise sintática não valida compilação completa, importação nativa de prefab, geração de RPC/NetworkVariable ou comportamento em rede.

Avaliar com cliente e servidor atualizados: passagem através dos objetos; quebra com tiro e com cada arma corpo a corpo; rejeição de acerto através de paredes/coberturas; consumo normal de munição/stamina; ausência de XP e loot; dois jogadores acertando o mesmo objeto; ocultação pela escuridão; entrada tardia e retorno à visão preservando destroços; reinício de sala; escala e fragmentos no celular. Variação de posições e drops continuam pendentes da etapa 4.
