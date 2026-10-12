# Gazua, fechaduras e acampamento de teste — 10/10/2026

Implementado no codigo, com revisao estatica apenas. Nao foram executados testes automatizados, Unity, compilacao ou builds nesta etapa, por preferencia do usuario. Cliente e servidor precisam ser recompilados juntos: protocolo **14**.

## Teste rapido do lockpick

1. Ative **Laboratorio** ao entrar em uma expedicao.
2. Abra **Ferramentas [F2] > Fechaduras**.
3. Use **Kit de gazuas**: substitui a mochila temporaria por equipamento inicial, tres gazuas novas e bandagens.
4. Escolha **Bau / Armario / Gaveta / Porta** e **Simples / Comum / Reforcada**. Crie a fechadura perto do jogador.
5. Feche o painel, aproxime-se do objeto e use **E / Interagir**.
6. Selecione um pino com mouse/toque ou setas/direcional. Segure **Espaco / botao de pressao / A do controle** e solte quando o pino oscilar e tocar o pequeno clique.

O mundo continua em tempo real. Dano, afastamento, morte, desconexao ou cancelamento interrompem a tentativa. O painel bloqueia movimento, tiros e inventario enquanto a tentativa estiver ativa. No laboratorio, pode-se remover as fechaduras criadas e receber outro kit para repetir rapidamente.

Gazuas ocupam um slot individual. Os erros acumulados ficam em `ItemStack.Data`, preservados ao mover, guardar, deixar no chao e recuperar a ferramenta. Cada erro sorteia a quebra no servidor: 10%, 25%, 40%, 55%, 70%, 85%, depois 95%. Acertar, cancelar e mudar de fechadura nao renovam a ferramenta. Nao ha contador de erros restantes ou porcentagem no painel. Uma gazua quebrada e removida, e outra pode ser usada na mesma fechadura, cuja solucao permanece fixa.

Ao destrancar um recipiente, o inventario dele abre. A porta de teste libera a passagem e gira sua representacao. O estado destrancado pertence ao objeto e e compartilhado na partida. A primeira versao usa snapshots enviados somente ao jogador que esta tentando e tempo do servidor; ajustes de tolerancia para latencia ficam sujeitos ao teste pratico.

No mapa normal, baus comuns continuam com a abertura existente e oferecem uma gazua. Baus de raridade maior recebem fechadura. Armarios, gavetas e portas trancadas podem ser criados no laboratorio; os moveis decorativos previamente existentes nao foram substituidos automaticamente.

## Teste rapido do acampamento

No acampamento, use **Entrar no acampamento de teste**, no painel de ferramentas. O servidor precisa permitir testes (Editor, build de desenvolvimento ou `-allow-test`). Aguarde a reconexao e a sincronizacao.

O perfil temporario vem com madeira e pregos suficientes para reparar o pier, metais/polvora/componentes para a bancada, bandagens e peixes na mochila para cozinhar. E possivel testar o bau seguro, transferencias, reparo do pier, pesca, receitas, reciclador, compra de componentes, cozimento e refeicao, sem buscar recursos numa expedicao.

- **Repor recursos e peixes** cancela a atividade em andamento e substitui a mochila e o bau temporarios; preserva reparo do pier, nivel do reciclador e outros resultados desse teste.
- **Reiniciar teste (pier quebrado)** reinicia o perfil temporario e fornece recursos novamente.
- **Voltar ao acampamento real** reconecta e carrega o perfil persistente original.

Os recursos de teste vivem somente na memoria do servidor, por conexao. As acoes e atividades usam esse perfil temporario e nunca o salvam no repositorio persistente. Desconectar descarta o perfil de teste. Entrar em expedicao a partir dele ativa o laboratorio, impedindo que equipamentos temporarios sejam usados como equipamentos persistentes de uma expedicao normal.

## Sprite e integracao

Sprite original gerado com imagegen e preservado como PNG com alpha em `Assets/Resources/Items/Lockpick.png`. Icone disponivel no inventario e nos itens caidos. Identificador `Lockpick` acrescentado ao fim do enum para preservar os IDs antigos.

Prompt utilizado: um unico sprite de gazua de aco gasto, ponta curva, cabo curto de madeira escura com dois rebites, em diagonal, arte 2D pintada para inventario de survival horror, silhueta legivel em 48–64 px, fundo realmente transparente, sem texto, cenario, sombra externa ou outros objetos. Ferramenta integrada imagegen; nenhuma imagem existente foi sobrescrita.

Regras C# migradas do prototipo isolado para `Assets/Scripts/Items/Lockpicking`. A integracao de rede, objeto e desgaste fica em `LootLock`; a interface, em `LockpickWindow`. O prefab de loot existente foi reutilizado, evitando refazer cenas/prefabs ou mudar a engine/rede. O acampamento de teste tem um sinalizador proprio incluido no login assinado, permitido apenas em servidores de teste, e seus comandos especiais sao recusados no acampamento real.
