# Catacumbas — luz, enquadramento, visão e minimapa

## Pendência — mapa muito vazio

Registrada em 15/10/2026 por relato do usuário. Status: aberta, sem implementação nesta etapa.

- O mapa ainda está muito vazio.
- Adicionar mais decoração e detalhes ambientais.
- Reforçar a ambientação e a identidade dos espaços.
- Incluir mais desafios durante a exploração; tipos, distribuição e dificuldade ainda precisam ser definidos.

Na futura implementação, considerar Darkwood para atmosfera, abandono e detalhes ambientais, e Bullet Echo para legibilidade do combate, cobertura, circulação e rotas alternativas. Preservar a identidade própria de Cyber Extraction e evitar que a decoração prejudique a leitura ou bloqueie caminhos necessários.

Somente registro do retorno do usuário. Nenhum asset, layout ou regra de gameplay alterado; nenhum teste ou build executado nesta etapa.

## Pendência — velocidade relativa e novas mecânicas dos monstros

Registrada em 15/10/2026 a pedido do usuário. Status: aberta, sem implementação nesta etapa.

- No teste relatado, o protagonista é muito mais rápido que os monstros. Revisar a relação entre mobilidade do jogador e ameaça dos inimigos, em conjunto com a sincronização da animação e a futura corrida com stamina.
- Criar mecânicas adicionais para os monstros além das existentes, ampliando as decisões de combate e fuga. O pedido não se limita a aumentar sua velocidade.
- Retomar no planejamento os papéis de ataque à distância, ataque em área e outras ameaças complementares já solicitadas; arquétipos, habilidades e valores finais ainda precisam ser definidos.

Ver também [velocidade, animação e corrida do protagonista](../CORRIDA_PROTAGONISTA_PENDENTE.md). Somente registro: nenhum código, asset ou balanceamento alterado e nenhum teste ou build executado.

### Exemplos propostos pelo usuário — 15/10/2026

- **Aranha — preparar uma emboscada:** espalhar teias pela sala, esconder-se e esperar que o jogador se aproxime, em vez de depender somente de perseguição direta.
- **Aranha — cercar com teias:** possibilidade de substituir o lançamento único na direção do jogador por três teias distribuídas ao redor dele. É uma alternativa em discussão; distribuição, aviso, duração e espaço de fuga ainda precisam ser definidos.
- **Espreitador — mobilidade condicionada à visão:** esconder-se e ganhar velocidade de movimento enquanto estiver fora da visão do jogador; mover-se mais lentamente quando estiver sendo visto.

São exemplos para orientar o planejamento, não uma lista fechada de habilidades nem regras já implementadas. No multiplayer, ainda definir como a visão de vários jogadores afeta o Espreitador. A futura implementação deve manter as decisões no servidor e distinguir visão real (ângulo, alcance e obstáculos) da simples presença na tela. Nenhuma habilidade ou valor alterado neste registro.

## Decisão — Percepção e abertura da visão

Aprovada pelo usuário em 15/10/2026. Pendente de implementação.

- Nas Catacumbas, a visão inicial deve ter abertura total de **80°**, chegando a **110°** conforme o atributo **Percepção**.
- Percepção altera **somente a abertura**. O alcance permanece em **10 unidades**, e o raio próximo existente permanece em 2,4 unidades.
- O atributo não altera o zoom da câmera nem o cone de precisão dos ataques.
- A abertura individual deve ser usada tanto pela máscara que revela as cores quanto pelas consultas de visibilidade e replicação do servidor. Não basta mudar apenas o shader ou um valor global para todos os jogadores.
- Escala/pontos do atributo, curva de progressão e distribuição de pontos ainda serão definidos no planejamento dos atributos persistentes. Os limites angulares acima já estão aprovados.

Somente registro da decisão nesta etapa; o atributo Percepção ainda não foi implementado e o código atual não foi alterado por esta confirmação.

## Requisito — ponto de respawn aleatório

Registrado em 15/10/2026 a pedido do usuário. Implementado em 15/10/2026; validação prática pendente.

O ponto de respawn deve ser escolhido aleatoriamente entre os pontos de respawn existentes no mapa, em vez de manter um ponto fixo por jogador. A seleção deve ocorrer no servidor. O requisito trata da escolha do local; não define uma nova regra de retorno à partida após a morte.

Complemento do usuário em 15/10/2026: não pode nascer mais de um jogador no mesmo spawn. Sortear os pontos sem repetição na distribuição dos participantes da partida, incluindo bots que ocupem vagas de jogador. O servidor deve reservar cada ponto atribuído para impedir duplicações; não reutilizar um ponto para acomodar participantes além da quantidade de spawns disponíveis.

A aprovação de conexão agora sorteia uniformemente um ponto entre os IDs livres e reserva esse ID no servidor. O ID também determina a orientação e o retorno ao abrigo no laboratório. A restrição de um humano no modo de teste continua independente do ponto sorteado.

Os bots iniciais sorteiam sem reposição os pontos restantes após as reservas humanas. A recriação de bots pelo laboratório usa a mesma exclusão dos pontos humanos; se um ponto estiver fisicamente obstruído, não desloca o bot para outro abrigo que possa estar reservado.

Capacidades: Catacumbas com oito pontos/participantes, Floresta Velha com cinco e mapa pequeno de testes com dois. O mapa pequeno anteriormente acomodava cinco participantes por offsets em duas áreas; agora permite um humano e até um bot, cada um em um dos dois pontos existentes. As opções da interface derivam da capacidade do mapa.

Revisão estática dos caminhos de aprovação, criação/recriação de bots e retorno ao abrigo. Sem testes automatizados, sessão Unity ou build nesta alteração, conforme preferência do projeto. Conferir em partida diferentes pontos para entradas sucessivas, ausência de repetição com humanos e bots, nova distribuição após encerrar a sala e retorno ao abrigo sorteado no laboratório.

## Histórico de implementação

Implementação de 15/10/2026 para os quatro relatos desta conversa. A extração com manivela permanece confirmada pelo usuário; sua lógica não foi alterada. Referências mantidas: atmosfera escura de Darkwood e leitura espacial/de combate de Bullet Echo, sem copiar layouts.

## Alterações

- **Luz:** nas Catacumbas, `PlayerVision.RefreshLookup` usa a primeira face atingida, em vez da saída do colisor. Os blocos de pedra são agrupados horizontalmente por linha; suas faces de saída causavam extensões de luz diferentes em cada faixa de alvenaria. Isso também escurece a massa sólida da parede, em vez de revelar sua profundidade inteira. Outros mapas preservam o tratamento anterior.
- **Câmera:** `DungeonCameraSize` de 10 para 7,5; aproximação adicional de aproximadamente 33%, sem alterar floresta/base.
- **Campo de visão:** Catacumbas com alcance de 10 unidades e abertura de 110°; outros mapas continuam com 16 e 140°. Raio próximo de 2,4 preservado. `GameConfig` mantém a referência comum para apresentação e consultas de visão autoritativas. O material e o lookup acompanham mudanças de alcance/mapa.
- **Minimapa:** uGUI e IMGUI projetam os marcadores de extração na mesma coordenada do terreno, sem prendê-los à borda. Destinos fora do recorte local são consultados no mapa ampliado; a orientação de destino existente na HUD permanece. Os dois temas usam a mesma regra.

## Evidência e limites

Compilação dos scripts atuais com Roslyn e referências locais da Unity 6000.0.84f1 concluída com código de saída 0, sem diagnósticos. Saída isolada em `Logs/registered-fixes-2026-10-15`; nenhum assembly do Editor foi substituído. Isso não valida renderização, física, RPCs ou sensação de zoom. Não foi iniciado Play Mode, teste Unity, servidor ou build de jogador.

A inspeção estática identifica os caminhos responsáveis pelos deslocamentos do marcador e pelas faixas de luz. Ainda é necessário conferir o resultado visual no mesmo corredor, o alinhamento do ícone ao mover o jogador/abrir o mapa e a preferência de enquadramento. Cliente e servidor precisam usar o código atualizado para compartilhar os novos limites de visão.
