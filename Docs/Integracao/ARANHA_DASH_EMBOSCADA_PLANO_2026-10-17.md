# Aranha — dash, teia de teto e cerco

## Escopo solicitado

Solicitado pelo usuário em 17/10/2026:

- Adicionar ataque de dash e sua animação.
- A aranha nasce apresentada como uma teia. Destruir essa teia faz a aranha descer do teto, com animação própria.
- Implementar teias ao redor do jogador.

Status: implementação e novos assets concluídos em 18/10/2026 após restabelecer o Blender MCP. Decisões finais, parâmetros e limites de verificação estão em [Aranha — dash e emboscada](ARANHA_DASH_EMBOSCADA_2026-10-17.md). O restante deste documento preserva o planejamento inicial. Não foi iniciada sessão Unity, teste ou build nesta tarefa.

## Fontes existentes

- Modelo aprovado: `Prototypes/SpiderPale/aranha-palida.blend`.
- Autoria e empacotamento: `build_spider.py`, `package_spider.py` e `package_runtime.py` nessa pasta.
- Atlases atuais: `Assets/Resources/Monsters/Weaver/Walk.png` e `Web.png`.
- IA: `Monster.cs`, `MonsterBehaviour.cs` e `MonsterArchetypes.cs`.
- Efeitos autoritativos: `MonsterWorld.cs`.
- Apresentação: `WeaverVisual.cs`.
- Protocolo atual encontrado: 34. Novos campos replicados exigirão compatibilizar cliente e servidor e atualizar o protocolo na implementação.

Preservar modelo, materiais, caminhada e lançamento já aprovados. Salvar novas ações em arquivo Blender adicional, sem sobrescrever a fonte. Manter sprites 2D no pipeline URP existente; não migrar para personagem 3D em runtime.

## Entrada pelo teto

Estados previstos: teia intacta → descida → recuperação do pouso → combate.

- A apresentação inicial é uma teia legível e destrutível, sem aranha caminhando no chão.
- O gatilho solicitado é a destruição da teia, não a simples aproximação do jogador.
- A teia possui resistência própria; quebrá-la não mata a aranha, não entrega loot e não concede XP de morte.
- O servidor resolve o dano, dispara a transição uma única vez e controla quando a aranha pode agir.
- Enquanto desce, a aranha não anda, ataca nem causa dano por contato. A disponibilidade para receber dano deve ser explicitada na implementação e apresentada visualmente.
- Descida por fio com pernas recolhidas, abertura das pernas, contato com o chão e recuperação. Evitar parecer um simples aumento de escala do sprite.
- Clientes que entram ou voltam a enxergar durante a descida usam o tempo replicado; não reiniciam a animação.
- Colisões e ocupação do chão precisam acompanhar o estado de entrada; a teia não pode funcionar como uma parede invisível.

## Dash

Sequência prevista: preparação visível → avanço curto em direção fixa → recuperação.

- Antecipação com corpo baixo e pernas comprimidas; avanço com pose própria; recuperação com apoio das pernas no piso.
- A direção é travada antes do avanço. Não corrigir a trajetória para seguir o jogador durante o dash.
- O servidor testa paredes, obstáculos e corpos ao longo do deslocamento, evitando atravessar cobertura ou acertar através de paredes.
- Cada dash causa no máximo um impacto por vítima; evitar dano repetido por quadro ou colisão.
- Um erro abre uma janela para contra-ataque. Não combinar mordida e dash no mesmo impacto.
- Cancelar estados pendentes ao morrer, despawnar ou encerrar a partida. Definir também a interação com retorno ao território e stagger.

Ponto de partida proposto, ainda não validado: preparação de 0,7 s, avanço de até 3 unidades em aproximadamente 0,25 s, recuperação de 0,6 s e intervalo de 5 s. Começar sem aumentar o dano atual de 15. Esses números são parâmetros de ajuste, não decisões finais aprovadas.

## Teias ao redor do jogador

- Partir de três projéteis formando uma ferradura, mantendo abertura transitável para fuga.
- Projetar a distribuição a partir de posição observada pelo servidor; não acompanhar um alvo escondido continuamente.
- Validar alcance, trajeto, piso e cobertura de cada posição. Não colocar a abertura dentro de parede, móvel ou região inacessível.
- Avisar antes de ativar as áreas. Priorizar contraste no chão e som localizado, sem cobrir a HUD ou exigir leitura de texto.
- Preservar destruição das teias por ataque e lentidão sem acúmulo entre aranhas.
- Prever limite de teias ativas por encontro e coordenação dos lançamentos para impedir sobreposição que elimine todas as saídas.
- Decidir na implementação como o cerco substitui o disparo único e se as armadilhas atuais continuam necessárias; não somar todos os ataques automaticamente.

Ponto de partida proposto: reaproveitar os 0,8 s de preparação, projéteis com tempo de viagem e área ativa de 6–8 s. Abertura e espaçamento devem considerar o raio real do jogador e o zoom aproximado atual.

## Ordem de produção

1. Restabelecer a conexão Blender MCP e inspecionar versão, cena, rig e câmera antes de editar.
2. Criar cópia de trabalho do modelo aprovado e produzir dash, descida e teia intacta/destruída como assets adicionais.
3. Exportar pelo pipeline existente e inspecionar visualmente os sprites de produção. Skills extras de animação/render/harmonização não estão no catálogo local; usar as quatro skills Blender disponíveis, sem instalar dependências.
4. Integrar estados, dano à teia, dash e cerco com autoridade no servidor.
5. Sincronizar apresentação, atualizar compatibilidade de protocolo e documentar regras finais.
6. Revisar estaticamente; a validação prática inicial cabe ao usuário, conforme AGENTS.md. Não iniciar testes ou builds apenas para validar sem novo acionamento.

## Critérios para avaliação prática

- Todas as aranhas previstas aparecem inicialmente como teia; quebrar uma libera somente sua aranha e não concede recompensa antecipada.
- A descida tem início e fim claros e não gera ataques antes do pouso.
- Dash pode ser antecipado e desviado; cobertura segura continua segura e não há múltiplos danos pelo mesmo avanço.
- O cerco deixa uma saída real, não aplica lentidão acumulativa e suas teias podem ser destruídas.
- Várias aranhas aumentam a pressão sem criar controle contínuo inevitável.
- Cliente separado e host veem o mesmo estado; reexibição não repete descida nem ataques históricos.

Referências permanentes: Darkwood para atmosfera e ameaça ambiental; Bullet Echo para cobertura, leitura do combate e rotas alternativas. Arte e comportamento próprios de Cyber Extraction.
