# Cyber Extraction

Jogo de extracao multiplayer 2D top-down para Android, feito em Unity 6000.0.84f1. Servidor autoritativo Windows, cinco participantes por partida e ate tres salas.

## Baixar o APK

[Baixar Cyber Extraction 1.2.3 para Android](https://github.com/vinimmendes/Extlyp/releases/download/v1.2.3/CyberExtraction-1.2.3.apk) · [Versões e notas de entrega](https://github.com/vinimmendes/Extlyp/releases).

Android 6 ou superior, ARM64. Instale por cima da versão anterior, sem desinstalar, para preservar a identidade do aparelho. Deixe pelo menos 2 GB livres durante a instalação. O repositório e seus downloads são privados: entre no GitHub com uma conta que tenha acesso.

Opções agora tem as categorias **Jogabilidade** e **Gráficos**. Em Jogabilidade, cada aparelho pode escolher mira ao andar, trocar Mira/Atirar, tiro automático, mira por arraste em área livre e recarga automática. Veja [as regras dos controles](Docs/UI/CONTROLES_TOQUE_OPCOES_2026-10-22.md) e [os dados da entrega 1.2.3](Docs/Entregas/ANDROID_1.2.3.md).

A versão 1.2.3 protege a identidade contra backup do Android. Se o segundo celular mostrar “Seu acampamento já está conectado”, use **Criar perfil neste celular > Confirmar novo perfil** somente nele. Ele começará um perfil próprio; o perfil anterior continua salvo. O primeiro celular pode continuar com seu perfil atual.

## Estado atual — 22/10/2026

- Protocolo **41**: cliente e servidor precisam usar o mesmo protocolo. Inclui recarga parcial e bloqueio de tiro durante a troca de arma.
- Mapa grande de 400 x 600, expedicoes de 10 minutos, extrações variáveis e caminhos com bloqueios/ferramentas.
- O acampamento carrega o perfil no servidor. Pesca, cozimento, receitas e transferencias sao confirmados online.
- Identidade por chave do aparelho; conhecer o ID do perfil nao permite entrar nele. Nao ha conta com recuperacao de senha.
- Progresso e salvo no servidor, com arquivo atomico e backup; morte perde a mochila, o bau permanece protegido.
- Objetos ocultos e inventarios privados sao filtrados na replicacao.
- Bots escolhem itens que cabem na mochila e planejam o trajeto de volta. So concluem extracao no ultimo minuto.
- Interface usa coordenadas de referencia dentro da area segura da tela. Mira silenciosa e botao de tiro separado foram mantidos.

## Usar e validar

Abra `Assets/Scenes/Game.unity`. Inicie o servidor com `Iniciar-Servidor.bat`; em versões de teste, escolha Cabana normal ou Acampamento de teste. Na base, use a preparação para entrar na expedição. A conexão usa `Assets/Resources/ServerConnectionSettings.asset`: `address` atende PC/Editor e `androidAddress` define o endereço do APK. Veja [SERVER_SETUP.md](SERVER_SETUP.md) e [a central da base](Docs/UI/CENTRAL_PREPARACAO_2026-10-18.md).

As definicoes da pistola e das receitas ficam em `Assets/Resources/Gameplay.asset`. A partida dura 10 minutos; regras de tempo e visão ficam em `GameConfig.cs`. Pesca pertence exclusivamente à cabana.

- Design e historico: [GDD.md](GDD.md), [EXPANSION_SETUP.md](EXPANSION_SETUP.md).
- Auditoria original: [27 achados](Docs/Auditoria/AUDITORIA_SKILLS_2026-10-09.md).
- Mudancas, testes e impacto: [implementacao](Docs/Auditoria/IMPLEMENTACAO_2026-10-10.md).
- Unity em copia isolada: `Tools/Validate-Unity.ps1`.
- Integracao de rede isolada: `Tools/Run-SkillsSmoke.ps1`.
- Regras fora da engine: `Tools/Audit/Run-AuditProbe.ps1`.

Instalação e conexão por dados móveis foram confirmadas pelo usuário em um Galaxy A20 SM-A205G com Android 9, no APK anterior às novas opções. O APK 1.2.3 teve compilação e assinatura conferidas. A correção de identidade passou 13 verificações focadas fora da Unity, com adaptadores explícitos; identidade e conexão em dois celulares ainda aguardam teste real. Medidas sustentadas de GPU/FPS e playtests de balanceamento continuam pendentes. A assinatura de identidade nao substitui protecao de transporte nem representa um sistema completo de anti-cheat.

## Clonar o projeto

Instale Git LFS antes de clonar e execute `git lfs pull` na pasta do projeto para obter artes, sons e arquivos Blender. Abra com Unity **6000.0.84f1**, mantendo os pacotes em `Packages/manifest.json`. Caches da Unity, logs, backups locais, credenciais e builds não fazem parte do código versionado; o APK é distribuído pelas Releases.
