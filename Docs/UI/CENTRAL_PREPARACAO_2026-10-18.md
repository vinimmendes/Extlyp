# Central de preparação sem cabana

## Apresentação e acesso

A base normal e a base de testes passam a usar uma central uGUI horizontal. A preparação apresenta equipamento à esquerda, imagem existente da Floresta Velha ao centro e destino/conexão/Partir à direita. O conteúdo usa a área segura, altura virtual 600 e largura máxima 1200; a imagem central cede espaço em telas menores. Os temas Simples e Sombria conservam preferência e geometria compartilhadas.

As abas Preparação, Inventário, Personagem, Comerciante e Atividades permanecem disponíveis nas páginas comuns. Opções mantém os controles existentes. Informações da conta são preenchidas após sincronização; ações ficam bloqueadas durante pedidos pendentes. A central observa `CampSession.Changed`; estado da conexão, tema e área segura são observados separadamente.

Inventário abre equipamento, mochila e baú juntos. Personagem e Comerciante usam os apresentadores existentes com entradas diretas. Atividades abre bancada, cozinha e pesca sem proximidade física; receitas, custos, desbloqueio da pesca, minigames e confirmação pelo servidor permanecem.

`Partir` fica na Preparação. Busca, cancelamento, erro e nova tentativa aparecem no painel da direita. A seleção já existente entre Floresta Velha e Catacumbas foi preservada; a imagem central continua uma ambientação da floresta, não uma prévia das Catacumbas. Seleção de bots e área de testes dependem da autorização existente. Não foi criado um destino novo.

Voltar/Esc cancela primeiro seleção/confirmacão, fecha a lista de itens no chão ou a atividade e depois retorna à Preparação. Na Preparação abre o menu. Minigames conservam apresentação e controles próprios; seu encerramento retorna às atividades (ou à seleção da cozinha). O resultado da expedição mantém seu retorno por recarga de cena, iniciando a nova base em Preparação.

## Assets e integração

- `Assets/Resources/UI/Prefabs/BaseHub.prefab`: composição editável com controles separados, referências em `BaseHubView` e comportamento em `UguiBaseHub`.
- `Assets/Resources/UI/Prefabs/GroundItems.prefab`: lista rolável e modelo de linha reutilizado, integrada ao inventário. O foco por controle mantém a linha selecionada visível.
- `PlayerInterface` contém as duas novas referências. `UiScreenCoordinator` reconhece a base como proprietária da entrada; a HUD de combate mantém seu layout.
- `CampController` mantém sessão e atividades, mas só instancia cenário/avatar/colisores quando iniciado explicitamente com `-camp-preview`. Os assets e o código da cabana continuam preservados.
- `OpenBench`, `OpenKitchen` e `OpenFishing` validam contexto, sincronização, atividade e pedido pendente antes de encaminhar as ações existentes. Não há alteração de RPC, save, custos, persistência ou autoridade.

Itens largados são lidos de `CampSession.Progress.Ground`, incluindo registros anteriores à mudança. A lista envia `RecoverDrop` pelo ID estável; não concede itens localmente. Recuperar requer espaço para a pilha inteira e confirmação do servidor. Não há exclusão automática de itens.

`Tools/UI/Author-BaseHub.py` registra a autoria inicial dos dois novos prefabs sem abrir Unity. Reutiliza somente definições do autor YAML existente, sem executar a geração geral; recusa sobrescrever prefabs existentes. Não executar `MvpSetup.Build` para esta entrega. Edições posteriores devem preservar os prefabs autorados.

## Referências

- [Hunt: Showdown — melhorias de interface](https://www.huntshowdown.com/news/developer-insight-upcoming-ui-improvements): abas persistentes e comparação lado a lado.
- [Arena Breakout](https://www.arenabreakout.com/tutorial/): preparação de equipamento no ciclo de extração.
- [Darkwood](https://www.darkwoodgame.com/about/): atmosfera de sobrevivência florestal.
- [Bullet Echo](https://www.zeptolab.com/games/bullet-echo): referência tática complementar para celular.

Arte, ícones e fontes foram reaproveitados do projeto; não houve geração ou cópia de assets desses jogos.

## Verificação e roteiro prático

Revisão estática de chamadas, entradas, referências de prefabs/GUIDs e relações pai/filho. Nenhum teste automatizado, sessão Unity, compilação ou build foi executado. Isso não confirma aparência, importação nativa, rede ou interação em execução.

Validação inicial pelo usuário:

1. Entrar na base normal e de testes; verificar ausência da cabana caminhável, controles de movimento e ferramentas de teste na base normal.
2. Conferir as cinco abas, temas, opções e Voltar/Esc, incluindo seleção de item e confirmação de redistribuição/venda.
3. Equipar, transferir, comprar, vender, fabricar, melhorar reciclador, cozinhar/comer e pescar.
4. Descartar e recuperar itens; conferir registros antigos, mochila cheia, rolagem por toque e foco por controle.
5. Partir para destinos existentes, cancelar busca, reconectar e retornar pelos resultados sem perder progresso.
6. Conferir 16:9, celular horizontal alongado com recorte lateral e ultrawide nos dois temas. Avaliar também os painéis preexistentes de inventário/comércio e os minigames.
