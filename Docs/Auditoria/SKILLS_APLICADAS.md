# Skills aplicadas ao Cyber Extraction

Instaladas em **09/10/2026**, apenas neste projeto, em `.agents/skills/`.
As instrucoes foram lidas e usadas na auditoria. A descoberta automatica pelo agente ocorre na proxima interacao.

| Skill | Origem | Uso nesta auditoria |
| --- | --- | --- |
| game-ui-ux | [skills.sh](https://www.skills.sh/gamedev-skills/awesome-gamedev-agent-skills/game-ui-ux) | Layout, toque, safe area, overlays e atualizacao do HUD |
| performance-optimization | [skills.sh](https://www.skills.sh/gamedev-skills/awesome-gamedev-agent-skills/performance-optimization) | Candidatos a medicao, alocacoes, IA, memoria e budgets |
| unity-csharp-scripting | [skills.sh](https://www.skills.sh/gamedev-skills/awesome-gamedev-agent-skills/unity-csharp-scripting) | Ciclo de vida, dependencias e liberacao de recursos |
| unity-input-system | [skills.sh](https://www.skills.sh/gamedev-skills/awesome-gamedev-agent-skills/unity-input-system) | Dispositivos, Input Actions e separacao entre jogo e UI |
| systematic-debugging | [skills.sh](https://www.skills.sh/obra/superpowers/systematic-debugging) | Rastreamento das causas e provas reproduziveis |
| game-ai | [skills.sh](https://www.skills.sh/gamedev-skills/awesome-gamedev-agent-skills/game-ai) | Percepcao, politica, mochila, rotas e extracao |
| level-design | [skills.sh](https://www.skills.sh/gamedev-skills/awesome-gamedev-agent-skills/level-design) | Escala, caminhos, tempo e apresentacao das mecanicas |
| unity-build-pipeline | [skills.sh](https://www.skills.sh/gamedev-skills/awesome-gamedev-agent-skills/unity-build-pipeline) | Compilacao, resultados, versoes, perfis e scripts operacionais |
| unity-scriptableobjects | [skills.sh](https://www.skills.sh/gamedev-skills/awesome-gamedev-agent-skills/unity-scriptableobjects) | Configuracoes editaveis e eventos sem estado global por jogador |

Os oito pacotes de jogos vieram de [gamedev-skills/awesome-gamedev-agent-skills](https://github.com/gamedev-skills/awesome-gamedev-agent-skills), nos caminhos `skills/disciplines/` e `skills/unity/`. O diagnostico veio de [obra/superpowers](https://github.com/obra/superpowers/tree/main/skills/systematic-debugging).

O download utilizou a referencia `main`. Os hashes SHA-256 dos SKILL.md e dos arquivos auditados estao em `source-evidence.json`; eles identificam o conteudo local efetivamente utilizado, sem afirmar que foi fixado um commit remoto.

As duas sugestoes condicionais da pesquisa anterior, `ui-ugui` e `setup-multiplayer-services`, nao foram instaladas nesta etapa: a primeira serve a uma futura implementacao com Canvas; a segunda depende da decisao de adotar Multiplayer Services. Sua ausencia nao impede auditar o codigo e a rede existentes.

## Compatibilidade

O projeto esta em Unity **6000.0.84f1**, enquanto as skills de Unity usam **6000.3** como referencia. A auditoria trabalhou sobre os fontes e pacotes locais. Nao houve migracao de engine, Netcode ou estrutura de interface.

`AGENTS.md` vincula os assuntos aos arquivos das skills para uso continuo no projeto. Leia somente as referencias pertinentes a tarefa. Regras externas devem ser interpretadas no contexto do projeto e das instrucoes do usuario.
