# Cyber Extraction

Jogo de extracao multiplayer 2D top-down para Android, feito em Unity 6000.0.84f1. Servidor autoritativo Windows, cinco participantes por partida e ate tres salas.

## Estado atual — 10/10/2026

- Protocolo **11**: atualize cliente e servidor juntos.
- O acampamento carrega o perfil no servidor. Pesca, cozimento, receitas e transferencias sao confirmados online.
- Identidade por chave do aparelho; conhecer o ID do perfil nao permite entrar nele. Nao ha conta com recuperacao de senha.
- Progresso e salvo no servidor, com arquivo atomico e backup; morte perde a mochila, o bau permanece protegido.
- Objetos ocultos e inventarios privados sao filtrados na replicacao.
- Bots escolhem itens que cabem na mochila e planejam o trajeto de volta. So concluem extracao no ultimo minuto.
- Interface usa coordenadas de referencia dentro da area segura da tela. Mira silenciosa e botao de tiro separado foram mantidos.

## Usar e validar

Abra `Assets/Scenes/Game.unity`. Inicie o servidor com `Iniciar-Servidor.bat`; em versões de teste, escolha Cabana normal ou Acampamento de teste. O portão abre o menu Floresta Velha. A conexão usa a configuração interna `Assets/Resources/ServerConnectionSettings.asset`, inicialmente `127.0.0.1:7777`. Veja [SERVER_SETUP.md](SERVER_SETUP.md) e [o menu de expedição](Docs/UI/MENU_EXPEDICAO_FLORESTA_VELHA.md).

As definicoes da pistola e das receitas ficam em `Assets/Resources/Gameplay.asset`. Os valores padrao de dano, velocidade, cinco minutos de partida e custos originais foram preservados.

- Design e historico: [GDD.md](GDD.md), [EXPANSION_SETUP.md](EXPANSION_SETUP.md).
- Auditoria original: [27 achados](Docs/Auditoria/AUDITORIA_SKILLS_2026-10-09.md).
- Mudancas, testes e impacto: [implementacao](Docs/Auditoria/IMPLEMENTACAO_2026-10-10.md).
- Unity em copia isolada: `Tools/Validate-Unity.ps1`.
- Integracao de rede isolada: `Tools/Run-SkillsSmoke.ps1`.
- Regras fora da engine: `Tools/Audit/Run-AuditProbe.ps1`.

Execucao real em Android, medidas sustentadas de GPU/FPS e playtests de balanceamento ainda precisam de aparelhos e jogadores. A assinatura de identidade nao substitui protecao de transporte nem representa um sistema completo de anti-cheat.
