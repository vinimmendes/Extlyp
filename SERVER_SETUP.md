# Servidor e cliente — estado atual em 16/10/2026

## Compatibilidade

Unity 6000.0.84f1; Netcode for GameObjects 2.13.3; Transport 2.7.4; protocolo **30**. Clientes antigos nao devem conectar ao servidor novo. O servidor continua sendo um Player Windows executado com `-batchmode -nographics -server`; nao exige o modulo Dedicated Server. Cliente e servidor precisam ser recompilados juntos. A etapa 9 acrescenta comércio, redistribuição e origem da munição de recuperação, com schema de perfil 4 e documento compartilhado 6. Resolver expedições ativas antes da migração; o mecanismo existente preserva backup do documento anterior. Nesta entrega houve somente revisão estática, sem testes, novos builds, alteração de saves reais ou reinício do servidor. Detalhes em [Comércio — etapa 9](Docs/Integracao/EQUIPAMENTOS_ETAPA_9_2026-10-16.md).

## Iniciar

1. Salve os arquivos, abra `Iniciar-Servidor.bat` e digite `1` para iniciar.
2. O atualizador compila em copia separada se o fingerprint mudou; so troca o executavel apos sucesso.
3. O iniciador aguarda a confirmacao de prontidao de cada sala. Portas padrao: UDP 7777, 7778 e 7779.
4. Abra Game na Unity ou o cliente compilado. Em versões com testes habilitados, escolha **Cabana normal** ou **Acampamento de teste**; nas demais, a cabana normal conecta diretamente. O portão abre a expedição **Floresta Velha**. Endereço e porta são internos, em `Assets/Resources/ServerConnectionSettings.asset`: `address` mantém o PC/Editor em `127.0.0.1`; `androidAddress` define o IPv4 usado pelo Android (vazio utiliza `address`). Porta base `7777`, com busca nas três salas. O cliente não usa a preferência antiga `server_ip`. Consulte a preparação do acesso externo abaixo.
5. Logs ficam em `Logs/server*.log`; compilacao em `Logs/server-update.log`.

O console de `Iniciar-Servidor.bat` mostra `1 - abre`, `2 - fecha` e `3 - reinicia`; digite o número e pressione Enter. Abrir o menu não inicia as salas automaticamente. Fechar a janela mantém as salas ligadas. `Parar-Servidor.bat` também encerra as salas; `Restart-Server.ps1` continua disponível para o painel de desenvolvimento. O reinício prepara a compilação antes de interromper a versão em execução, mas encerra as partidas em andamento.

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

Preparacao para dados moveis em 22/10/2026:

- Preencha `androidAddress` com o IPv4 da WAN/Internet do modem. Para teste no Wi-Fi local, use o IPv4 LAN do PC. Um endereco privado ou CGNAT na WAN pode impedir acesso externo mesmo com os encaminhamentos salvos.
- O PC/Editor continua usando `address`, evitando depender de NAT loopback para jogar no proprio servidor.
- As regras configuradas nesta rede sao: modem G-240W-C UDP 7777 a 7779 para a WAN do D-Link (`192.168.1.147`); D-Link com tres regras individuais UDP 7777, 7778 e 7779 para o PC (`192.168.100.3`). Preserve esses enderecos com reservas DHCP nos respectivos aparelhos.
- O servidor escuta em `0.0.0.0`. O firewall do Windows precisa permitir UDP nessas portas para `Builds/Server/CyberExtractionServer.exe`; a existencia da regra nao comprova acesso externo.
- Gere um APK novo em **Cyber Extraction > Servidor > Gerar APK Android para teste**. O gerador exige IPv4 sem loopback, usa ARM64/IL2CPP e inclui permissao de internet. A orientacao permanece paisagem esquerda/direita. Nao desinstale o aplicativo antigo para atualizar: preserve a chave de identidade e o progresso.
- Inicie as salas com `Iniciar-Servidor.bat`. Instale o APK, desligue o Wi-Fi do celular e entre pelos dados moveis. O APK antigo de 13/10 nao representa o codigo atual. Compare a versao/protocolo do servidor e do cliente quando houver recusa de conexao.
- Se o IP publico mudar, atualize `androidAddress` e gere/instale outro APK. Nao ha descoberta automatica nem atualizacao de IP por servico externo.

O encaminhamento salvo nos paineis ainda precisa ser confirmado pelo teste real no celular. Preparar configuracao ou compilar nao valida rede externa, CGNAT, multitouch ou jogabilidade.

Acesso externo ainda depende de roteador/firewall, IPv4 publico ou solucao UDP adequada. A prova de identidade nao cifra o transporte nem autentica por si so o servidor para o cliente. Antes de uma abertura publica, configurar/provar protecao de transporte, limites de carga e recuperacao de identidade.

Continuam faltando testes em Android fisico, redes reais, carga prolongada nas tres salas, latencia/perda e balanceamento. Veja a evidencia e o escopo exato em `Docs/Auditoria/IMPLEMENTACAO_2026-10-10.md`.
