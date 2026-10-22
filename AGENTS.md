# Cyber Extraction — orientacoes do projeto

## Contexto

- Unity **6000.0.84f1**, C#, URP 2D, Android e cliente/servidor Windows.
- Rede existente: Netcode for GameObjects **2.13.3**, Unity Transport **2.7.4**.
- Consulte `Packages/manifest.json` e `ProjectSettings/ProjectVersion.txt` antes de usar exemplos de APIs.
- As skills externas miram Unity 6.3; adapte exemplos a 6000.0 sem atualizar a engine ou trocar a rede implicitamente.
- Preserve alteracoes locais e assets existentes. `MvpSetup.Build` recria cena/prefabs; prefira mudancas pontuais em um projeto ja montado.

## Referencias permanentes para mapas

- Por orientacao do usuario, considere **Darkwood** e **Bullet Echo** em toda criacao, alteracao ou revisao de mapas.
- Use Darkwood como referencia de atmosfera, floresta, abandono, interiores e detalhes ambientais.
- Use Bullet Echo como referencia de legibilidade do combate, cobertura, linhas de visao, circulacao e rotas alternativas.
- Combine essas referencias com as regras e a identidade de Cyber Extraction; mantenha arte e layouts proprios.

## Referencias permanentes para HUD e arte de interface

- Dois temas aprovados: **A — Simples** e **B — Sombria**, selecionaveis em Menu > Opcoes e salvos por aparelho.
- Consulte `Docs/UI/DIRECAO_DE_ARTE_HUD.md` antes de criar imagens, botoes, icones, paineis ou alterar o layout. A referencia aprovada da Sombria esta em `Docs/UI/Referencias/hud-sombria-aprovada.png`.
- Use a mesma estrutura e regras para os dois temas; adapte apresentacao com `HudTheme` e coordenadas pela area segura de `UiLayout`.
- Celular funciona exclusivamente na horizontal, permitindo paisagem esquerda/direita. Nao criar variantes verticais.
- Mochila e bau seguro juntos somente na base. Na expedicao, mochila individual e janela de saque separada.
- Laboratorio e ferramentas de teste pertencem ao modo de testes; nao adicionar botoes de desenvolvimento a partidas normais.

## Skills aplicadas

Leia a skill correspondente ao assunto antes de implementar ou revisar. Nao e necessario carregar todas em cada tarefa.

| Assunto | Referencia local |
| --- | --- |
| HUD, menus, inventario, safe area | `.agents/skills/game-ui-ux/SKILL.md` |
| Desempenho e alocacoes | `.agents/skills/performance-optimization/SKILL.md` |
| Scripts C#, componentes e ciclo de vida | `.agents/skills/unity-csharp-scripting/SKILL.md` |
| Controles e Input Actions | `.agents/skills/unity-input-system/SKILL.md` |
| Bots, monstros e navegacao | `.agents/skills/game-ai/SKILL.md` |
| Mapa, rotas, ritmo e aprendizado | `.agents/skills/level-design/SKILL.md` |
| APK, builds Windows e automacao | `.agents/skills/unity-build-pipeline/SKILL.md` |
| Dados de balanceamento e eventos | `.agents/skills/unity-scriptableobjects/SKILL.md` |
| Bugs e comportamento inesperado | `.agents/skills/systematic-debugging/SKILL.md` |
| Orientacao e coordenacao de tarefas Blender | `.agents/skills/text-to-blender/SKILL.md` |
| Planejamento e etapas de producao no Blender | `.agents/skills/blender-pro-workflow/SKILL.md` |
| Modelagem, malhas e modificadores no Blender | `.agents/skills/blender-modeling/SKILL.md` |
| Animacao, keyframes, F-curves e NLA no Blender | `.agents/skills/blender-animation/SKILL.md` |
| Exportacao de assets do Blender para Unity | `.agents/skills/blender-export/SKILL.md` |

## Uso obrigatorio das skills Blender

- Em toda tarefa de criacao, alteracao ou revisao de assets no Blender, leia primeiro `text-to-blender` e as skills instaladas correspondentes ao trabalho. Use `blender-pro-workflow` para planejar trabalhos com varias etapas, `blender-modeling` para geometria e modificadores e `blender-export` antes de exportar assets.
- Antes de controlar Blender, consulte `.agents/skills/text-to-blender/references/mcp-backends.md`, confira a versao real instalada e os recursos disponiveis. Adapte receitas a essa versao; nao instale ou atualize Blender, add-ons ou MCP implicitamente.
- Preserve cenas, arquivos `.blend`, assets e alteracoes locais existentes. A leitura das skills nao autoriza sobrescrever trabalho existente nem alterar a arquitetura Unity/URP 2D do projeto.
- Para assets destinados ao jogo, aplique as referencias permanentes de mapas e interface conforme o assunto e adapte a exportacao ao pipeline existente, inclusive sprites renderizados quando aplicavel.
- As instrucoes do usuario e as preferencias de verificacao deste arquivo prevalecem sobre receitas externas de testes, renders de validacao, builds e delegacao. Se uma skill referenciada adicional nao estiver instalada, registre a limitacao e use as quatro disponiveis conforme o escopo; nao instale dependencias adicionais implicitamente.
- Origem das quatro skills: `RobLe3/cc-blender-skill`, pasta `plugin/skills`, descobertas no skills.sh e instaladas localmente em `.agents/skills`.

## Evidencias e verificacao

- **Preferencia do usuario:** por padrao, implemente e revise o codigo sem iniciar testes automatizados, sessoes de validacao Unity ou builds feitos apenas para testar. O usuario faz a validacao pratica primeiro. Execute esses testes somente quando ele relatar algo errado no proprio teste ou solicitar testes explicitamente.
- Quando houver esse acionamento, limite a verificacao ao problema relatado e suas dependencias diretas; evite repetir suites completas, builds e capturas sem uma necessidade concreta. Builds solicitados para entrega continuam autorizados.
- Ao entregar uma alteracao sem testes executados, informe isso de forma breve e nao a apresente como validada em execucao. As orientacoes de verificacao abaixo se aplicam quando os testes forem acionados conforme essa preferencia.
- Referencia da auditoria inicial: `Docs/Auditoria/AUDITORIA_SKILLS_2026-10-09.md`.
- Diferencie problema reproduzido, evidencia estatica, hipotese e proposta de produto.
- Correcoes devem atacar a causa identificada e incluir uma verificacao proporcional ao risco.
- Para inventario, receitas e politica de bots, `Tools/Audit/Run-AuditProbe.ps1` executa provas das regras C# reais fora da Unity. Os casos de vulnerabilidade confirmam o comportamento atual: quando corrigidos, devem ser convertidos em expectativas de rejeicao.
- Essas provas nao validam Unity, serializacao nativa, fisica, multitouch, RPCs gerados, desempenho ou APK.
- Para rede, combate e interface, use verificacoes em Unity e clientes separados/aparelhos conforme a mudanca. Nao trate logs antigos como validacao do codigo atual.
- Em desempenho, registre hardware, cenario e medidas antes/depois; nao prometa ganho com base apenas em leitura de codigo.

## Limites de arquitetura

- Regras da expedicao permanecem autoritativas no servidor.
- ScriptableObjects podem armazenar configuracao; estado por jogador e progresso persistente precisam de armazenamento separado.
- Adotar servicos online adicionais da Unity e uma decisao de arquitetura separada da manutencao do servidor atual por IP.
