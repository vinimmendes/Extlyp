# Auditoria completa — etapa 1: preparação e mapeamento

Data: 2026-10-16T08:35:42.330974-03:00 · America/Sao_Paulo. Execução: `2026-10-16_083542_382c7a`.

**Etapa 1 concluída.** Inventário e cópia independente conferida por SHA-256; nenhum teste, compilação, Unity, build, limpeza, migração de save ou alteração de gameplay executado.

## Base registrada

- Branch `main`, HEAD `a80a701a471b7004ce37db864b346505f56ba429`. A base inclui o conteúdo físico atual, inclusive modificações e arquivos não rastreados; não é apenas uma cópia do commit.
- Unity 6000.0.84f1; NGO 2.13.3; Transport 2.7.4; URP 17.0.4; Input System 1.20.1.
- Protocolo 30, perfil 4, documento compartilhado 6.
- Cena habilitada: `Assets/Scenes/Game.unity`, com Input Actions registrado em EditorBuildSettings.
- 178 scripts de runtime e 25 scripts de editor. Os arquivos de editor não foram executados.

## Preservação e cópia isolada

- Projeto: [Projeto](C:/Users/Vinni/Documents/GitHub/Extlyp/Builds/Auditoria/2026-10-16_083542_382c7a/Projeto).
- Evidências: [pasta da execução](C:/Users/Vinni/Documents/GitHub/Extlyp/Logs/auditoria/2026-10-16_083542_382c7a).
- 5047 arquivos, 1.513 GiB lógicos; 873 rastreados e 4174 não rastreados/ignorados copiados.
- SHA-256 do inventário: `fcdf78077dca71119a05d701dad579149239ccedd107a3d16fe11c0ca32241cd`. Cada arquivo possui hash próprio e foi comparado com a cópia; estado Git permaneceu igual durante a captura.
- `git-working-tree.patch`, `git-index.patch` e os estados Git inicial/final estão nas evidências locais. Exclusões locais permanecem registradas no estado Git e não foram reintroduzidas na cópia.
- A cópia usa arquivos independentes, sem hardlinks. Foram excluídos `.git`, caches, builds anteriores, backups, diretórios de dados de usuário e nomes/extensões identificáveis de credenciais; isso não constitui varredura de segredos embutidos em fontes. A lista detalhada de exclusões consta em `inventory.json`.
- Não é backup dos saves reais, nem backup de toda a máquina. Arte, protótipos, histórico, ferramentas e fontes atuais estão incluídos nas raízes inventariadas; caches não são necessários para preservar essas fontes.
- O contexto futuro define cluster exclusivo, nomes de identidades separados e pasta própria para dados. Esses arquivos de identidade e saves ainda não foram criados. A porta proposta deve ser conferida antes do uso.
- O script de preparação faz parte da cópia. Este relatório e o plano administrativo não constituem validação do jogo; o relatório desta execução é produzido depois da captura.

| Raiz | Arquivos copiados | MiB lógicos |
| --- | ---: | ---: |
| .agents | 87 | 0.78 |
| .gitattributes | 1 | 0.00 |
| .gitignore | 1 | 0.00 |
| .vsconfig | 1 | 0.00 |
| AGENTS.md | 1 | 0.01 |
| Art | 608 | 548.78 |
| Assets | 1337 | 476.89 |
| BOTS_SETUP.md | 1 | 0.00 |
| CAMP_ITEMS.md | 1 | 0.00 |
| Docs | 155 | 10.54 |
| EXPANSION_SETUP.md | 1 | 0.01 |
| GDD.md | 1 | 0.01 |
| Iniciar-Servidor.bat | 1 | 0.00 |
| Packages | 2 | 0.02 |
| Parar-Servidor.bat | 1 | 0.00 |
| ProjectSettings | 27 | 0.06 |
| Prototypes | 2762 | 511.63 |
| README.md | 1 | 0.00 |
| Restart-Server.ps1 | 1 | 0.00 |
| SERVER_SETUP.md | 1 | 0.00 |
| Server-Console.ps1 | 1 | 0.00 |
| Server-SourceFingerprint.ps1 | 1 | 0.00 |
| Start-Server.ps1 | 1 | 0.00 |
| Stop-Server.ps1 | 1 | 0.00 |
| Tools | 51 | 0.26 |
| Update-Server.ps1 | 1 | 0.01 |

## Mapa dos sistemas

| Pasta em Assets/Scripts | Scripts |
| --- | ---: |
| `Bots` | 2 |
| `Camp` | 13 |
| `Combat` | 11 |
| `Core` | 9 |
| `Diagnostics` | 9 |
| `Enemies` | 19 |
| `Items` | 22 |
| `Match` | 31 |
| `Players` | 18 |
| `UI` | 44 |

- **Core:** configurações, atributos, progressão e serviços compartilhados.
- **Camp:** identidade, comunicação, transações, persistência, atividades e comércio. Fluxo: identidade → sessão autenticada → comando/revisão → repositório → snapshot.
- **Items:** pilhas, identidade, equipamento, armas, recuperação, catálogos e tabelas de loot. Resultados sorteados pertencem ao servidor; assets armazenam configuração.
- **Players/Combat:** entradas, movimento, visão, combate, saúde, coleta e recipientes. Fluxo: intenção → validação no servidor → estado replicado.
- **Match:** fila, mapas, spawns, interesse por cliente, objetivos, extrações e ciclo da expedição. Fronteira crítica: base → reserva/commit → partida → extração/perda/rollback.
- **Enemies/Bots:** percepção, navegação, habilidades, decisões e recompensas.
- **UI:** HUD, menus e presenters uGUI; IMGUI permanece em ferramentas/minigames. Recursos visuais incluem carregamento por strings e caminhos compostos.
- **Diagnostics/Editor/Tools:** telemetria, verificadores e automação. Atualidade e efeitos desses verificadores serão revisados antes dos testes agrupados.

O inventário enumera cada script por sistema. Este mapa identifica áreas e fronteiras; não afirma que as implementações estejam corretas.

## Histórico e cobertura seguinte

- Auditoria inicial de 09/10: 27 achados registrados para reavaliação. Nenhum foi marcado como resolvido ou recorrente nesta etapa.
- Implementação e correções de 10/10: evidências históricas, sem comprovação automática para o código atual.
- Revisão de obsolescência e limpeza de 12/10: referências para a etapa 4. Valores de espaço e recomendações antigas precisam de medição atual.
- Integrações das etapas 1–9 de equipamentos/atributos/loot: especificação e limites para confrontar na auditoria; não substituem testes.
- Todos os documentos históricos identificados, IDs dos achados anteriores e caminhos de ferramentas estão em `inventory.json`.

| Etapa | Situação | Entrega prevista |
| --- | --- | --- |
| 1 — Preparar e mapear | Concluída | Este relatório, inventário, hashes e cópia isolada |
| 2 — Dados, segurança e economia | Não iniciada | Revisão de autoridade, conservação, persistência, comércio e XP |
| 3 — Gameplay e interface | Não iniciada | Revisão de combate, IA, mapas, visão, animações e controles |
| 4 — Possíveis reduções | Não iniciada | Candidatos com dependências, confiança e benefício, sem apagar |
| 5 — Provas fora da Unity | Não iniciada | Casos C# agrupados usando regras reais |
| 6 — Unity e jogo integrado | Não iniciada | Uma abertura Unity e campanha com servidor/dois clientes reutilizados |
| 7 — Consolidar resultados | Não iniciada | Achados, cobertura, limitações e fila de correções/limpeza |

## Limites e retomada

A primeira tentativa (`Logs/auditoria/2026-10-16_083404_4ab734`) foi interrompida pelo bloqueio da sandbox ao filtro Git LFS, antes da cópia. Ela permanece como registro de tentativa, sem valor de baseline. A execução concluída usou permissão ampliada e não iniciou Unity, testes ou builds.

O editor instalado foi localizado no caminho do contexto de execução, com versão de arquivo `6000.0.84.7908207`; essa inspeção não abriu o programa. Para a futura validação, conferir conexão por loopback na cópia, disponibilidade de portas e flags de identidade/cluster/dados antes de lançar clientes ou servidor. Os scripts operacionais copiados não devem ser executados com destinos padrão sem essa conferência.

As únicas exclusões internas registradas foram o `.git` de um protótipo arquivado e um `__pycache__` de ferramenta. Pastas geradas e dados reais na raiz não fazem parte das raízes de fonte selecionadas. Nenhum arquivo original foi excluído.

O acompanhamento geral está em [Plano da auditoria completa](PLANO_AUDITORIA_COMPLETA_2026-10-16.md).

Nenhum binário antigo ou log anterior valida esta base. A cópia ainda não foi importada ou compilada. Referências, RPCs, física, UI, desempenho e Android não foram validados. Nenhum arquivo foi classificado como seguro para exclusão.

Na etapa 2, usar a base desta execução e seus hashes. Alterações posteriores devem ser registradas como uma nova revisão da base, sem misturar evidências de versões diferentes. Ferramentas de teste adicionadas posteriormente devem ser identificadas separadamente do conteúdo auditado. Saves normais e scripts de início do servidor real não devem ser usados pelos testes isolados.
