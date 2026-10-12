# Servidor e cliente — estado atual em 11/10/2026

## Compatibilidade

Unity 6000.0.84f1; Netcode for GameObjects 2.13.3; Transport 2.7.4; protocolo **16**. Clientes antigos nao devem conectar ao servidor novo. O servidor continua sendo um Player Windows executado com `-batchmode -nographics -server`; nao exige o modulo Dedicated Server. O mapa e a expedicao agora incluem extrações variáveis e passagens compartilhadas; cliente e servidor precisam ser recompilados juntos. Nesta entrega houve apenas revisão estática, sem novos builds ou reinício do servidor. Detalhes em `Docs/Integracao/MAPA_EXPEDICAO_2026-10-11.md`.

## Iniciar

1. Salve os arquivos e abra `Iniciar-Servidor.bat`.
2. O atualizador compila em copia separada se o fingerprint mudou; so troca o executavel apos sucesso.
3. O iniciador aguarda a confirmacao de prontidao de cada sala. Portas padrao: UDP 7777, 7778 e 7779.
4. Abra Game na Unity ou o cliente compilado. Em versões com testes habilitados, escolha **Cabana normal** ou **Acampamento de teste**; nas demais, a cabana normal conecta diretamente. O portão abre a expedição **Floresta Velha**. Endereço e porta são internos, em `Assets/Resources/ServerConnectionSettings.asset`: inicialmente `127.0.0.1`, porta base `7777`, com busca nas três salas. O cliente não usa a preferência antiga `server_ip`. Acesso pela rede local/externa permanece para outra etapa.
5. Logs ficam em `Logs/server*.log`; compilacao em `Logs/server-update.log`.

`Parar-Servidor.bat` encerra as salas. `Restart-Server.ps1` agora prepara a compilacao antes de interromper a versao em execucao. Esse reinicio ainda encerra as partidas; nao e uma atualizacao sem interrupcao.

## Progresso e identidade

O servidor salva perfis em `Application.persistentDataPath/Camps/<cluster>.json`, com backup `.bak`. `-camp-data <diretorio>` permite um diretorio explicito, usado pelos testes isolados. Preserve essa pasta ao mover o servidor.

O cliente salva a chave de identidade no aparelho. Seu perfil e vinculado a essa chave, e pedidos de login sao assinados. Preservar a chave e necessario para acessar o mesmo perfil. O prototipo anterior possuia somente um GUID e progresso temporario na RAM; nao ha migracao automatica que consiga comprovar a titularidade desses perfis antigos.

Pesca/cozimento rodam no servidor. Operacoes tem sequencia monotona e respostas de confirmacao. A mochila e reservada antes do inicio, confirmada na partida e contabilizada uma vez ao extrair. Morte/desconexao durante a partida continua perdendo a mochila. Na recuperacao depois de queda completa do servidor, uma reserva ainda nao liquidada devolve o equipamento inicial, sem inventar o loot que nao foi salvo.

## Teste e release

Menus de build geram Development por padrao. `-release` gera build sem esses recursos; modo rapido e reinicio temporario ficam restritos a desenvolvimento ou habilitacao explicita de teste no servidor (`-allow-test`).

Versao padrao: 1.2 / Android code 3. `CE_BUILD_VERSION` e `CE_ANDROID_VERSION_CODE` permitem versionamento da entrega. Release Android exige `CE_KEYSTORE_PATH`, `CE_KEYSTORE_ALIAS`, `CE_KEYSTORE_PASSWORD`, `CE_KEY_PASSWORD`, definidos fora do codigo. Nao registre esses valores em logs.

## Verificacao

`Tools/Validate-Unity.ps1 -Build` cria uma copia em `Builds/SkillsValidation`, roda checks e gera cliente/servidor nessa copia. `Tools/Run-SkillsSmoke.ps1` usa porta, identidade e arquivos de teste novos; nao reinicia as salas normais. Sua fixture desativa dano de IA para isolar transacoes e rede. Testes de combate competitivo continuam separados.

Use `-metrics` em testes para registrar counters disponiveis e eventos em `Logs/gameplay-metrics.jsonl`. Os dados nao incluem chaves ou conteudo de inventarios. A coleta e opt-in e adiciona custo de instrumentacao.

## Internet e limites

Acesso externo ainda depende de roteador/firewall, IPv4 publico ou solucao UDP adequada. Nao foram alterados firewall, roteador ou hospedagem. A prova de identidade nao cifra o transporte nem autentica por si so o servidor para o cliente. Antes de uma abertura publica, configurar/provar protecao de transporte, limites de carga e recuperacao de identidade.

Continuam faltando testes em Android fisico, redes reais, carga prolongada nas tres salas, latencia/perda e balanceamento. Veja a evidencia e o escopo exato em `Docs/Auditoria/IMPLEMENTACAO_2026-10-10.md`.
