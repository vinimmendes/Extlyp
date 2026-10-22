# Setup do pacote do Passo 2 (Unity 6)

Pre-requisito: projeto Unity 6 criado com o template **Universal 2D** (o mesmo do Passo 1).

1. Abra o projeto. Em Window > Package Manager > "+" > Add package by name, digite
   `com.unity.netcode.gameobjects` e instale a versao 2.x. Aguarde compilar.
2. Apague a pasta antiga `Assets/Scripts` (do Passo 1) e copie para o projeto as pastas
   `Assets/Scripts` e `Assets/Editor` deste pacote. Copie tambem `GDD.md`, `README.md` e esta pagina para a raiz.
3. Menu **Cyber Extraction > Montar MVP (prefabs + cena)**. Ele cria os prefabs, o material, a cena
   `Assets/Scenes/Game` e a coloca no Build Settings.
   O MatchManager agora e um prefab registrado e criado pelo host depois de iniciar a rede.
   Para atualizar uma cena antiga com o erro ServerSpawnSceneObjectsOnStartSweep, saia do Play,
   substitua `Assets/Editor/MvpSetup.cs` e `Assets/Scripts/UI/NetworkBootstrap.cs` pelas versoes
   deste pacote e execute novamente esse menu. Ele recria a cena Game e configura o novo prefab;
   somente copiar os scripts sem recriar a cena nao aplica essa correcao.
4. Teste sozinho: em `Assets/Scripts/Core/GameConfig.cs` troque `PlayersToStart = 2` por `1`.
   Abra a cena Game, aperte Play, clique em "Criar partida (Host)".
   WASD move, o mouse aponta o cone, botao esquerdo atira.
5. Volte `PlayersToStart` para `2` antes de testar com duas pessoas.
6. Android: File > Build Profiles > Android > Player Settings > Other Settings:
   Internet Access = Require. Instale o APK em dois celulares na mesma rede Wi-Fi.
   Um toca "Criar partida", o outro digita o IP mostrado na tela e toca "Entrar na partida".

## Ordem de testes (um por vez, so avance quando o anterior funcionar)
1. Sozinho (PlayersToStart=1): o jogador nasce na casa, anda, ve o cone e a escuridao.
2. Dois jogadores: os dois se veem quando estao no cone um do outro.
3. Tiro: as duas linhas abrem andando e fecham parado; o tiro tira vida do outro. Confirme que loot e area de extracao nao bloqueiam tiros, mas paredes e caixas bloqueiam.
   Android: joystick direito so mira; segure ATIRAR para disparar. Soltar o botao interrompe os disparos. No editor, mouse mira e botao esquerdo dispara.
4. Monstro: persegue, ataca e reage a tiros; contorna caixas, paredes centrais e entra pelas portas. Ao perder visao, procura a ultima posicao vista. Ao morrer, solta loot.
5. Loot e extracao: pegar loot, ficar 3 s na area verde, ver "Voce extraiu!".
6. Morte: loot cai no corpo; fim por tempo esgotado.

## Se algo der errado
Mande a mensagem de erro exata (aba Console do Unity). Pontos que mais podem precisar de ajuste:
- Se o menu "Montar MVP" der erro de compilacao, e quase certo uma diferenca de versao do pacote Netcode.
- Se os jogadores nao nascerem nas casas, e o NetworkTransform no modo Owner.
