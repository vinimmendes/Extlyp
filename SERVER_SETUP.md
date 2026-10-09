# Servidor no seu PC

Alteracoes de acampamento e itens estao em CAMP_ITEMS.md. O codigo atual usa protocolo 4; nao misture um servidor compilado com este codigo com clientes anteriores. Novos APKs sao gerados somente quando solicitados.

Unity Netcode. Ate tres salas simultaneas, uma por processo, com cinco participantes cada. O primeiro humano de cada sala inicia uma espera de 30 segundos; cinco humanos iniciam imediatamente. Ao vencer o prazo, as vagas restantes recebem bots medios. Novas conexoes durante a partida sao encaminhadas pelo cliente para outra sala disponivel. Bots so extraem no ultimo minuto.

Iniciar-Servidor.bat abre as salas nas portas UDP 7777, 7778 e 7779. O cliente recebe apenas o endereco do PC e procura uma vaga entre essas portas. O numero da sala aparece no HUD. Quando todas estao ocupadas, o cliente avisa para tentar novamente. Parar-Servidor.bat encerra as tres instancias.

A partida termina quando nao resta nenhum humano vivo e nao extraido, mesmo que os bots continuem vivos. Se nao ha nenhum cliente conectado, a sala e limpa imediatamente para a proxima busca. Humanos conectados que morreram ou extrairam podem ver os resultados por ate 15 segundos.

## Gerar e iniciar

1. Salve as alteracoes do projeto na Unity. Nao e necessario recriar a cena Game.
2. De dois cliques em Iniciar-Servidor.bat na pasta do projeto.
3. O iniciador compara o conteudo de Assets, Packages e ProjectSettings com a ultima compilacao. Se mudou, gera e instala o servidor atualizado antes de iniciar. Se nada mudou, inicia diretamente. Isso funciona com o projeto aberto na Unity: a compilacao usa uma copia separada e reutiliza o cache.
4. Abra a cena Game na Unity e aperte Play para usar o cliente atual do projeto. No acampamento, caminhe ate o portao e entre no servidor usando 127.0.0.1 quando estiver no mesmo PC.
5. Consulte Logs/server.log, Logs/server-room-2.log e Logs/server-room-3.log para acompanhar cada sala. Erros de compilacao ficam em Logs/server-update.log.

A atualizacao automatica compila apenas o servidor Windows. Nao gera APK nem executa partidas ou verificacoes completas. As verificacoes continuam disponiveis nos menus para quando forem solicitadas. A Unity na versao indicada em ProjectSettings/ProjectVersion.txt precisa estar instalada no caminho padrao do Unity Hub.

O programa usa uma compilacao Windows comum executada sem graficos (`-batchmode -nographics -server`), pois o modulo opcional Windows Dedicated Server nao esta instalado. O PC executa a simulacao sem jogador local. Uma compilacao otimizada com o modulo Dedicated Server pode ser adicionada depois.

Para parar, de dois cliques em Parar-Servidor.bat. Fechar a janela do iniciador deixa as salas funcionando. Salas ja ligadas nao sao duplicadas; salas faltantes sao iniciadas. Mantenha os dois arquivos .bat, os scripts .ps1 e a pasta Builds/Server nesta estrutura. A opcao `-Port` define a porta base e `-Rooms` permite iniciar de uma a tres salas; o cliente normalmente procura nas tres portas a partir de 7777.

Se o servidor estiver rodando com codigo antigo, ele so e encerrado depois que a nova compilacao termina com sucesso. A nova versao sera iniciada em seguida. Se a compilacao falhar, o iniciador informa o arquivo de log e preserva a versao anterior, sem iniciar silenciosamente uma versao desatualizada. Neste prototipo, reiniciar o servidor perde o progresso guardado apenas em memoria.

## Testar

- APK: execute Cyber Extraction > Servidor > Gerar APK Android para teste. O arquivo fica em Builds/Android/CyberExtraction.apk. Transfira para um celular Android de 64 bits e abra para instalar, autorizando o aplicativo que abre o arquivo quando o Android solicitar. Com o celular no mesmo Wi-Fi do PC, informe o IPv4 local do servidor e toque em Iniciar partida. Nao precisa conectar o celular a Unity para instalar o APK manualmente.
- Mesmo PC: gere o cliente Windows pelo menu Servidor, abra Builds/Client/CyberExtraction.exe e informe 127.0.0.1. Clique em Iniciar partida.
- Celular no mesmo Wi-Fi: gere novamente o aplicativo Android com a cena Game e informe o IPv4 local do PC (consulte `ipconfig`). Libere a porta UDP 7777 do servidor no firewall do Windows.
- Um humano deve ver a contagem de 30 segundos e entrar numa partida com 4 bots. Cinco humanos devem iniciar imediatamente, sem bots. Teste movimentos, tiros, morte, extracao e desconexoes.
- Ao terminar, os resultados ficam visiveis por 15 segundos. O servidor desconecta os clientes, limpa a partida e abre uma nova sala. O cliente volta ao menu e pode iniciar outra busca.
- Na Unity, Iniciar servidor de teste abre um servidor sem jogador local. Use um cliente separado para entrar.

## Internet

Reserve o IPv4 local do PC no roteador. Encaminhe UDP 7777 a 7779 para esse endereco e permita essas portas no firewall. Os jogadores usam o IP publico ou um nome DNS apontando para ele. Teste com o celular em dados moveis. CGNAT pode impedir o acesso direto: nesse caso, solicite IPv4 publico ao provedor ou escolha uma solucao de tunel/relay compativel com UDP. A configuracao do roteador/firewall nao e alterada pelos scripts do projeto.

## Limites atuais

Ate tres partidas, com no maximo 15 participantes ao todo. O acampamento acompanha o jogador entre as salas pela RAM compartilhada; nao ha banco de dados, e encerrar todas as salas perde o progresso. Uma mesma identificacao de acampamento nao pode ocupar duas salas simultaneamente. Ainda nao existe fila aguardando vaga, autenticacao ou reconexao durante partida. O movimento e simulado no servidor, sem previsao local, portanto a sensacao de atraso deve ser avaliada em testes reais.

## Validacao realizada

Compilacao Windows na Unity 6000.0.84f1, 65 verificacoes de sala/prefabs e 50 das regras dos bots passaram. Dez clientes locais iniciaram uma partida com zero bots, com movimento replicado em todos. A mesma instancia do servidor abriu outra sala e iniciou uma partida com um humano e nove bots apos a espera. Estes testes nao medem latencia pela internet nem desempenho de celulares.

APK Android ARM64 gerado em Builds/Android/CyberExtraction.apk. Assinatura, alinhamento e permissao INTERNET verificados; requer Android 6.0 ou superior em aparelho de 64 bits. A execucao no celular ainda precisa ser testada no aparelho.
