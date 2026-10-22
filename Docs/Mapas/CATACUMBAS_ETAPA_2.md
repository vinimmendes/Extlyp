# Catacumbas dos Afogados — etapa 2

Direção da próxima fase: [arte 3.1](CATACUMBAS_DIRECAO_VISUAL.md) e [catálogo de sprites](CATACUMBAS_CATALOGO_SPRITES.md).

Implementação de 14/10/2026, sobre a estrutura da etapa 1. Mantidos Unity 6000.0.84f1, NGO 2.13.3, Transport 2.7.4, oito participantes, PvPvE e dez minutos. Arte do cenário e dos mecanismos permanece provisória.

## Encontros e saque

| Conteúdo inicial | Distribuição |
|---|---|
| 10 aranhas (`Weaver`) | Duas em cada ossário, duas no salão das águas, duas na cisterna dos lamentos, uma na galeria das urnas e uma nos contrapesos. |
| 5 espreitadores (`Stalker`) | Prisão, sala das correntes, nomes raspados, sacristia e galeria dos relevos. |
| 3 couraçados (`Shell`) | Contrapesos, capela e salão das águas. |
| 18 pontos de saque no chão | Distribuídos pelas alas, fora das antecâmaras iniciais; usam as chances e tabelas atuais de loot. |
| 6 baús de maior interesse | Salão das águas, contrapesos, pátio, capela, sacristia e cisterna dos lamentos. Raridade mínima 1 e chance de fechadura de 30%; conteúdos seguem as tabelas existentes. |

Os 24 pontos são oportunidades de saque; quantidade e presença dos itens continuam sujeitas às rolagens existentes. Não há reaparecimento de monstros ou ondas novas. Nenhum outro arquétipo, imitador ou planta é criado pela geração normal deste mapa. Ferramentas deliberadas do laboratório continuam disponíveis somente no ambiente de teste.

Cada encontro usa âncora autorada e procura uma posição livre dentro da própria sala, afastada das entradas, áreas de embarque, outros monstros e pontos de saque. Falta de prefab ou de espaço interrompe o início com erro e usa a reversão existente, em vez de iniciar silenciosamente com uma população incompleta. Habilidades, vida e dano dos três arquétipos não foram alterados.

## Três fugas preparáveis

Todas as saídas podem ser preparadas em qualquer partida. Uma preparação concluída permanece disponível para todos. A fuga é individual, sem vagas limitadas e sem consumir a saída.

| Saída | Preparação | Fuga |
|---|---|---|
| **1 — Escadaria do Cortejo** | Operar o cabrestante próximo à escada por **6 segundos**, sem item. | A grade desaparece; alcançar os degraus e permanecer na área de saída por **3 segundos**. |
| **2 — Poço dos Coveiros** | Recolher a **Manivela dos coveiros** no pedestal da Câmara dos Contrapesos e instalá-la no controle do ossário por **4 segundos**. | A plataforma permite a extração de **3 segundos**, com representação local de subida. |
| **3 — Canal dos Defuntos** | Soltar a corrente no controle do embarcadouro por **4 segundos**, sem item. | Embarcar por **3 segundos**; a barca tem representação local de partida pelo canal. |

A **Chave do canal**, encontrada no pedestal da Prisão dos Penitentes, abre em **4 segundos** a comporta/passarela da cisterna. O controle fica na margem leste do Salão das Águas. Esse cruzamento encurta o caminho entre prisão e canal; o contorno pela margem continua livre sem chave. A chave não é requisito da barca.

Use **E** ou o botão contextual para começar. É uma interação iniciada por clique: permanecer junto ao controle, sem se afastar mais de 0,4 unidade do ponto inicial. Outro clique cancela. Movimento, dano, abertura de uma janela que bloqueia gameplay, morte, perda do item ou invalidade da interação interrompem o progresso incompleto. O item só é consumido na conclusão. Armas e outras operações de inventário ficam indisponíveis durante a preparação; andar continua possível para interrompê-la.

Preparações emitem alertas de ruído pelo sistema atual, atraindo a atenção de bots e monstros dentro do alcance. Isso é ruído lógico de gameplay; efeitos sonoros finais não foram produzidos nesta etapa.

## Ferramentas temporárias e autoridade

- Manivela e chave ocupam um espaço de mochila cada, com limite de uma por pilha e pontuação zero. Usam coleta, descarte e transferência para recipientes existentes.
- Morrer derruba os itens pelas regras atuais. Na desconexão ou extração, uma ferramenta ainda não utilizada retorna ao pedestal quando não existe outra cópia acessível no mundo. Uma verificação periódica também recupera ferramentas perdidas em geometria inacessível.
- Ferramentas colocadas em recipientes acessíveis continuam lá; não são duplicadas pelo recuperador. Ferramentas já consumidas por uma preparação não são repostas.
- O depósito de extração remove esses dois tipos temporários antes de gravar a mochila na cabana. Eles não entram nas tabelas normais de loot nem exigem migração dos itens antigos.
- Progresso, posse, alcance, linha de interação, interrupção, consumo e ativação são verificados pelo servidor. A solicitação de um jogador usa RPC com propriedade exigida.
- A abertura da comporta é replicada, atualiza colisão e invalida caminhos/cartografia da cisterna. Não bloqueia nenhuma rota obrigatória.
- A extração exige saída preparada, participante vivo e sem outra interação, permanência completa na zona e proximidade física do embarque. O depósito continua usando a transação existente por expedição.
- Limpeza da sala cancela operações e impede a recuperação de ferramentas durante o descarte dos objetos da rodada.

O protocolo passa de **20 para 21** por causa dos novos estados replicados e tipos de item. Cliente e servidor precisam usar o código atualizado. Não foram criados novos prefabs de rede nem regeneradas cenas/prefabs existentes.

## Bots, interface e placeholders

Bots avaliam distâncias pelos corredores, consideram o tempo de preparação e escolhem entre saídas prontas ou preparáveis com os itens que possuem. Operam mecanismos, param enquanto trabalham e interrompem a operação para reagir a uma ameaça visível. Se estiverem levando a chave e escolherem o canal, podem abrir a passagem da cisterna antes de continuar. A regra existente que permite extração de bots apenas no último minuto permanece.

O minimapa permite selecionar as três saídas. Saídas prontas usam `+`; as demais mostram o número para relacionar escadaria, poço e canal. O relógio orienta o destino e informa a preparação pendente; o botão contextual mostra tempo, progresso, cancelamento ou item necessário. Ambos os temas e os controles existentes foram preservados, sem novos botões de desenvolvimento em partidas normais.

Manivela e chave têm silhuetas provisórias geradas por código. Grade, corrente, barca, plataforma e cruzamento da cisterna respondem ao estado da partida. As animações de transporte são apresentação local do jogador; a conclusão e a disponibilidade para outros participantes são decididas pelo servidor. A arte definitiva e a ambientação detalhada continuam na etapa 3.

## Área e evidência

| Geometria | Área útil conectada | Relação com o teste |
|---|---:|---:|
| Cruzamento da cisterna fechado | 7.070,75 u² | 6,046x |
| Cruzamento aberto | 7.117,00 u² | 6,086x |

Medição estática em grade de 0,25 unidade e raio de jogador de 0,40 unidade. O relatório do estado aberto está em `CATACUMBAS_AREA_CANAL_ABERTO.json`; a planta correspondente está em `CATACUMBAS_PLANTA_CANAL_ABERTO.svg`. A ferramenta de medição aceita `--canal-open` e preserva os arquivos históricos da etapa 1.

Foi feita revisão estática e medição matemática da geometria. **Não foram executados Unity, testes automatizados, builds ou sessões de validação.** Contagem efetiva dos encontros, física, bots, RPCs gerados, extrações simultâneas, apresentação em aparelhos e balanceamento aguardam a primeira avaliação prática do usuário. Nenhum desempenho ou resultado em execução está sendo apresentado como validado.
