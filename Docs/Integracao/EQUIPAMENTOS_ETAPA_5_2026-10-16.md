# Equipamentos — etapa 5: efeitos dos atributos

Implementação em 16/10/2026. Revisão estática, sem validação em execução. Mantidos Unity 6000.0.84f1, NGO 2.13.3 e Transport 2.7.4.

## Regras aplicadas

Todos os atributos começam em 1. O servidor lê os investimentos do perfil ao iniciar a expedição e soma os bônus dos seis slots de proteção/acessórios. Armas A/B conservam suas características próprias e não fornecem bônus de atributos nesta versão. Cada investimento é limitado a 0–10 para o cálculo; o total efetivo fica entre 1 e 20. Somas usam `long` antes de limitar, evitando overflow com metadados fora dos intervalos normais. Valores persistidos não são reescritos por essa limitação.

| Atributo | Efeito por ponto acima de 1 | Limite efetivo 20 |
| --- | --- | --- |
| Força | +2% de dano melee | +38% |
| Destreza | −1,5% na dispersão de apontamento | −28,5% |
| Vigor | +3 stamina máxima | 157 |
| Vitalidade | +1 vida máxima, antes da comida | 119 |
| Percepção | Abertura normalizada do campo de visão | Catacumbas 80°–110°; Floresta 110°–140° |
| Inteligência | Reservada | Valor efetivo 1, sem efeito ou investimento disponível |

O dano melee é arredondado para inteiro no impacto e na HUD com a mesma regra. Um bônus pequeno pode não alterar o dano inteiro de uma arma até o próximo limiar de arredondamento. Não altera alcance, arco, cadência ou custo de stamina.

Vigor não altera velocidade, regeneração, consumo ou custo dos golpes. Vitalidade soma o bônus existente de comida. Ao equipar, guardar ou substituir peças, vida/stamina atuais são preservadas em valor absoluto e apenas limitadas ao novo máximo: aumentar capacidade não cura nem recupera energia; reduzir e aumentar repetidamente não restaura o valor perdido. Começar uma expedição e usar restauração explícita no laboratório continuam preenchendo os recursos. Os bloqueios de equipamento durante combate e outras ações permanecem.

## Mira e espingarda

Pistola e rifle multiplicam o cone calculado pela arma e pelo movimento pelo fator de Destreza. A HUD usa a velocidade normalizada autoritativa replicada pelo servidor, com a mesma fórmula de dispersão usada pelo disparo; permanece a latência normal de apresentação da rede.

Na espingarda, os 7° de meio-arco estrutural existentes em repouso são preservados. A diferença entre o cone em movimento e esse padrão é o erro de apontamento, reduzido por Destreza. Cada tiro sorteia um erro de mira comum à rajada; cada pellet acrescenta o seu espalhamento estrutural. As linhas de mira mostram o limite da soma. Isso separa o padrão dos pellets da precisão sem eliminar a penalidade de movimento. A distribuição passa a ser essa composição, em vez de um sorteio independente de todo o cone por pellet; precisa de avaliação prática de balanceamento.

Destreza não altera cone melee, dano de tiro, alcance, recarga ou cadência.

## Visão individual

- Catacumbas: 80° no atributo 1, chegando a 110° no 20; alcance permanece 10 unidades.
- Floresta e ambiente de teste: 110° a 140°; alcance permanece 16 unidades.
- Raio próximo permanece 2,4 unidades. Câmera, audição e bloqueio por paredes não são alterados.
- Neblina e cores, alvos locais, visibilidade de rede de entidades e baús, efeitos, apresentação visual de tiros, inspeção de saídas e os indicadores existentes usam a visão do observador.
- A reação dos monstros a estarem iluminados usa o atributo do participante que os observa. A visão própria dos monstros mantém as regras de cada arquétipo.
- Bots usam a visão individual do próprio inventário e os mesmos limites. O laboratório continua com investimentos temporários em zero, sem importar progresso do perfil real.

O servidor publica somente a abertura derivada necessária aos observadores; investimentos e equipamentos continuam privados do dono. Não existe RPC para o cliente definir um atributo efetivo, velocidade de mira, capacidade ou abertura.

## Interface e disponibilidade

A ficha do inventário mostra base, contribuição das peças, valor efetivo e excedente acima de 20. Comparar uma peça mostra a mudança do valor efetivo e o excedente sem efeito. Inteligência aparece como futura. A composição foi ajustada pontualmente para três linhas de atributos; os dois temas e área segura existentes são preservados. O painel de melee mostra o dano já modificado por Força.

Bem alimentado agora possui um estado explícito replicado ao dono. A HUD não deduz comida da vida máxima, evitando que Vitalidade faça aparecer um bônus de alimentação inexistente. O bônus de regeneração de stamina por comida permanece em 15%.

As peças provisórias do laboratório já permitem observar os efeitos ao equipar: cabeça/amuleto dão Percepção, torso Vitalidade, mãos Força, pés Vigor e anel Destreza. As 18 peças definitivas entram na etapa 7. Não foram adicionados distribuição de pontos, níveis, XP ou botões sem função: isso começa na etapa 6.

## Compatibilidade e revisão

Protocolo **25 → 26** pelos novos campos replicados de atributos, alimentação, abertura e velocidade usada na mira. Cliente e servidor precisam da mesma versão. O formato persistente permanece igual; nenhum perfil real foi migrado, nenhum servidor reiniciado e nenhum build criado.

Revisão estática de fórmulas/limites, inicialização da expedição, mudança de peças, conservação dos recursos atuais, dano, dispersão, consumidores de visão e interface. `git diff --check` sem erros nos arquivos rastreados verificados. Nenhum teste automatizado, compilação manual, sessão Unity ou build de validação foi iniciado, conforme AGENTS.md. A entrega não está validada em execução.

Para avaliação prática: equipar e retirar as peças provisórias, comparar vida/stamina sem recuperação gratuita, observar abertura e paredes, comparar mira parada/andando e a espingarda, conferir dano melee e estados de comida. Verificar host, cliente separado e celular. Tetos de atributos e investimentos requerem dados controlados do perfil quando os testes forem solicitados; não foi criado um comando público de alteração de atributos.
