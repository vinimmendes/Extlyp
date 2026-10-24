# Servidor estavel e desenvolvimento — 24/10/2026

O executavel atual em `Builds/Server` passa a ser a versao estavel. Iniciar ou reiniciar esse canal nao compila nem instala o codigo em edicao. O projeto Unity continua sendo a fonte de desenvolvimento; nao e necessario manter duas copias manuais dos fontes.

| Item | Estavel | Desenvolvimento |
| --- | --- | --- |
| Console | `Iniciar-Servidor.bat` | `Iniciar-Servidor-Desenvolvimento.bat` |
| Executavel | `Builds/Server/CyberExtractionServer.exe` | `Builds/ServerDevelopment/CyberExtractionServer.exe` |
| Portas UDP | 7777–7779 | 17777–17779 |
| Logs das salas | `Logs/server*.log` | `Logs/Development/server*.log` |
| Dados | Pasta `Camps` ja utilizada em persistentDataPath | `Builds/ServerDevelopmentData` |
| Atualizacao | Promocao explicita, com salas desligadas | Compilacao ao iniciar/reiniciar se os fontes mudaram |

Os canais usam identidades de cluster distintas, inclusive para memoria compartilhada e locks. A identidade da estavel foi preservada: os perfis existentes continuam no mesmo local. Desenvolvimento inicia com perfis separados; nao copia nem promove progresso. Preserve tambem a pasta de dados de desenvolvimento ao limpar builds. A escolha Cabana normal/Acampamento de teste dentro do jogo e outra funcionalidade, independente destes canais de executaveis.

## Trabalhar e jogar

1. Use o console estavel, opcao 1, para manter a versao atual disponivel. Fechar o console deixa as salas rodando; opcao 2 encerra somente o canal daquele console.
2. Faca alteracoes no projeto normalmente. Para experimenta-las, abra o console de desenvolvimento e use 1. A compilacao utiliza a copia existente `Builds/ServerValidation`, sem fechar a Unity principal. Falha na compilacao preserva o executavel anterior. Compilar/atualizar desenvolvimento pode encerrar suas partidas de desenvolvimento; nao encerra as estaveis.
3. Fora do Play, selecione **Cyber Extraction > Servidor > Editor conecta no desenvolvimento (17777)**. A selecao e local a este projeto/aparelho e vale no proximo Play; nao altera o asset de conexao nem os clientes distribuidos. O menu equivalente para estavel restaura 7777. Argumento `-port` explicito continua prevalecendo.
4. Um cliente Windows compativel pode usar `CyberExtraction.exe -port 17777`. O cliente deve ter o mesmo protocolo e conteudo publicado que o servidor escolhido. Preserve a distribuicao do cliente que acompanha a estavel; o cliente em desenvolvimento pode deixar de ser compativel com ela. O APK continua apontando para a porta configurada em seu build; nao foi criada uma selecao Android de canais nesta alteracao.

O botao temporario de reinicio dentro do jogo fica disponivel somente em conexao local nas portas de desenvolvimento, mantendo as demais restricoes existentes. Nao reinicia a estavel. `Restart-Server.ps1` sem argumentos reinicia a mesma versao estavel, sem atualizacao; com `-Channel Development`, prepara e reinicia desenvolvimento.

## Publicar uma versao aprovada

1. Execute `Update-Server.ps1` para preparar o candidato, ou inicie desenvolvimento, e avalie-o com cliente compativel. O atualizador gera `source.sha256`. O menu Unity de gerar servidor escreve somente na pasta de desenvolvimento; sozinho nao produz esse carimbo do atualizador.
2. Planeje a interrupcao e encerre a estavel pelo seu console. Resolva partidas em andamento e preserve backup dos dados antes de alteracoes de schema. A publicacao de executavel nao converte dados antecipadamente nem garante reversibilidade de migracoes realizadas pelo jogo.
3. Execute `powershell -NoProfile -ExecutionPolicy Bypass -File .\Publish-Server.ps1`. O comando recusa troca enquanto a estavel esta ligada, prepara uma copia completa e preserva a pasta anterior em `Builds/ServerReleases/<identificador>-previous`. Nao compila, encerra nem inicia processos. Execute atualizacao e publicacao sequencialmente.
4. Inicie a estavel e distribua o cliente correspondente, incluindo o mesmo conteudo publicado das Catacumbas. Promover nao modifica os fontes nem os dados de desenvolvimento.

Para voltar ao executavel anterior, com a estavel desligada, preserve a pasta atual e restaure integralmente a pasta anterior arquivada para `Builds/Server`. Se a nova versao ja migrou perfis, avalie e restaure o backup de dados compativel antes de abrir a versao antiga; nao sobreponha perfis reais com os de desenvolvimento.

## Investigacao da demora ao iniciar — 24/10/2026

Na maquina AMD Ryzen 5 5600G, Windows 11, a leitura completa dos 14.673 arquivos (0,88 GiB) para o fingerprint levou 9,97 s em PowerShell 7 e 23,73 s numa medicao em Windows PowerShell 5.1, usado pelos atalhos. Os valores sao amostras sob a carga existente, sem controle de cache de disco. A verificacao completa de conteudo foi preservada. Uma alternativa de leitura via stream foi descartada apos piorar a medicao no Windows PowerShell e nao comprovar equivalencia durante edicoes concorrentes.

O reinicio de desenvolvimento executava o atualizador e depois iniciava as salas por um caminho que executava o atualizador novamente. Essa segunda verificacao foi retirada com `-SkipUpdate` no caminho interno de reinicio. O inicio normal de desenvolvimento continua conferindo os fontes e compilando quando necessario, portanto esse custo ainda existe. Sincronizar a copia tambem compara hashes; nao foi alterado sem uma medicao especifica dessa etapa.

Na abertura estavel registrada em 24/10, o processo da sala 1 nasceu as 10:13:27.747 e confirmou prontidao as 10:13:34.748; sala 2 confirmou as 10:13:35.958; sala 3 as 10:13:37.272. Foram aproximadamente 9,52 s no conjunto. O primeiro processo registrou 5,840 s carregando assemblies, os seguintes 0,174 e 0,170 s. Esses logs comprovam a espera sequencial anterior, nao uma medicao de desempenho do iniciador novo.

Agora o iniciador abre todas as salas ausentes antes de esperar pela prontidao, com limite de 30 s individual. O console informa os tempos das salas, da conferencia e o total. O ganho real da abertura paralela deve ser medido na proxima inicializacao do usuario; concorrencia de disco/CPU pode afetar o resultado.

`powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Audit/Run-ServerStartupProbe.ps1` verifica o fluxo com processos simulados: abertura paralela, todas as salas ja abertas, apenas algumas abertas, identidade divergente, processo encerrado e timeout. Essa prova nao mede a Unity nem inicia executaveis de servidor. A investigacao nao interrompeu as salas existentes, nao gerou builds e nao iniciou Unity. Diferencie esta verificacao pontual do escopo original abaixo.

## Escopo da entrega original

Scripts, destinos de build, selecao de porta no Editor e documentacao foram revisados estaticamente. Nao foram gerados executaveis/APKs, executados testes automatizados, iniciadas sessoes Unity nem reiniciadas salas nesta entrega. A primeira compilacao e avaliacao pratica dos dois canais ainda estao pendentes. Esta separacao nao instala servico Windows, inicio automatico apos reinicializacao ou supervisao de quedas.
