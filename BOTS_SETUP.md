# Bots jogadores — IA v2 integrada

Atualização de 11/10/2026. Unity 6000.0.84f1, regras autoritativas no servidor e cinco participantes por sala. As vagas restantes são preenchidas com bots; a dificuldade padrão continua Mediano.

| Perfil | Exploração | Combate | Retorno |
| --- | --- | --- | --- |
| Iniciante | Conhecimento inicial menor, destinos menos estáveis | Atira andando, mira mais lenta e menos precisa | Margem de 5 segundos |
| Mediano | Conhece mais ruínas e mantém o plano | Para para mirar, rajadas de 4 e deslocamento lateral ocasional | Margem de 15 segundos |
| Experiente | Planeja a volta, evita nascimentos rivais no início | Rajadas de 3, cobertura e reposicionamento | Margem de 30 segundos |

Os perfis não ganham vida, dano, velocidade ou visão extras. Conhecimento de ruínas não revela inimigos, loot escondido ou estado de extrações. Bots recebem uma saída confirmada e descobrem outras pela visão, como os jogadores.

O bot calcula a viagem por rotas do mapa grande, considera lentidão e tempo de embarque, sai antecipadamente e aguarda fora da zona. Sua extração continua liberada apenas com 60 segundos restantes. Depois de decidir sair, mantém essa intenção, podendo recuar ou lutar diante de perigo próximo.

Loot observado permanece na memória até o local ser revisto. Só seleciona itens que cabem na mochila. Baús e armários fechados ainda não são explorados automaticamente. Sons não informam a identidade nem a posição atual de um inimigo escondido.

Ao travar, o bot tenta outro plano; pontos inacessíveis recebem uma pausa antes de nova tentativa. Animação, teias, cadência, recarga e origem dos disparos continuam usando os sistemas atuais.

## Inspeção e validação

Selecione Bot(Clone) no servidor para observar State e Difficulty. `botDifficulty` no MatchManager configura a dificuldade. O menu Cyber Extraction > Verificar regras dos bots contém as verificações importadas do pacote; elas não comprovam física, navegação nem rede em partida.

Nesta integração foi feita revisão estática. Nenhum teste automático, sessão Unity ou build foi executado. O usuário realiza a validação prática primeiro. Para usar a nova IA no executável, atualize o servidor pelo fluxo de SERVER_SETUP.md.

Detalhes das adaptações: Docs/Integracao/BOTS_IA_V2_2026-10-11.md.
