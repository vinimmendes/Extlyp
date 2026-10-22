# C5 — recuperação segura pelo backup

Situação: **implementada e revisada estaticamente; não testada**. Escopo E2-03. Nenhum teste, Unity, compilação, build, instalação ou sessão de jogo iniciada; nenhum save real lido, migrado ou alterado pela tarefa.

## Causa

O fallback anterior cobria somente leitura/parsing, deixando validação e migração fora da escolha do candidato. Na auditoria, JsonUtility também converteu Records:null em lista vazia. Um documento rejeitável podia virar um checkpoint vazio e substituir o backup válido.

## Alterações

**SharedCampMemory.cs:**

- Confere a estrutura original do JSON antes de desserializar com JsonUtility: versão explícita, Records como array, sessão preenchida, Progress como objeto e propriedades sem duplicação. Records ausente/nulo não vira lista vazia válida. Uma lista explicitamente vazia continua permitida; não se restaura um backup antigo só por conter mais registros.
- Cada candidato passa por validação de formato, sessões únicas, recuperação de expedição/migração aplicável e validação de propriedade existente antes de ser escolhido. As novas regras de formato dos inventários e quantidades continuam para C6/C7.
- Recusa documentos/perfis de versão futura sem tentar voltar automaticamente a um backup mais antigo. Migrações suportadas continuam com preservação do documento original.
- Se o principal não for utilizável, preserva seus bytes e tenta o backup completo. Antes de restaurar, também preserva o backup utilizado; a substituição do principal nessa recuperação não grava o principal inválido por cima de .bak.
- Principal ausente com backup existente também permite recuperação. Só considera armazenamento novo quando **ambos os caminhos retornam arquivo não encontrado**. Falhas de acesso, leitura ou validação não inicializam silenciosamente um conjunto vazio.
- Ambos inválidos/indisponíveis produzem erro com as duas causas, sem executar a operação econômica nem publicar um checkpoint vazio. Falha ao preservar um arquivo também interrompe a recuperação antes de sobrescrevê-lo.
- Arquivos de preservação usam nomes derivados de hash, conferência de conteúdo e gravação temporária com flush antes de promoção. Repetir a mesma falha não cria cópias idênticas continuamente; nenhum arquivo original é apagado por essa preservação.

**Packages/manifest.json e packages-lock.json:** Newtonsoft JSON 3.2.2, que já estava instalado como dependência transitiva, foi declarado como dependência direta. A estrutura é conferida com JObject/JsonLoadSettings; o formato de gravação e a desserialização dos objetos continuam com JsonUtility. O overload e a opção de propriedades duplicadas foram conferidos na documentação XML instalada. Nenhuma versão foi atualizada e nenhum gerenciador de pacotes foi executado.

As alterações anteriores de posse/token e recuperação de leases foram preservadas. Versões de Unity, NGO e Transport continuam as mesmas.

## Limites e verificação futura

A política de backup foi alterada no carregamento do armazenamento; não constitui monitoramento contínuo de edições externas dos arquivos. Os mesmos validadores são usados para o documento compartilhado, que continua recusando estado inválido sem o transformar em vazio. A validação integral dos campos de inventário depende também das próximas etapas C6/C7.

Arquivos arquivados servem para diagnóstico/recuperação manual; não há busca automática por qualquer arquivo histórico. Uma versão futura é um bloqueio de compatibilidade, não um sinal para restaurar silenciosamente dados antigos. Não houve promessa de ganho de desempenho ou redução de disco.

T4 deverá usar diretório isolado e verificar separadamente: principal válido; JSON inválido; Records nulo/ausente; registro incompleto/duplicado; migração ou validação recusada; principal ausente com backup válido; ambos ausentes; ambos inválidos; acesso negado; versões futuras de documento e perfil; preservação dos bytes; .bak válido após restauração; falha durante preservação/escrita; repetição sem duplicar arquivos; migração legada com expedição ativa. Conferir JsonUtility nativo e reinício real, não apenas adaptadores.

O histórico da auditoria permanece intacto e não valida este código novo. Próxima etapa: **C6 — estrutura e identidade dos itens**, sem testes ou builds.
