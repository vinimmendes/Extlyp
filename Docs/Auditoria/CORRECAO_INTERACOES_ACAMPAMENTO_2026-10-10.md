# Interacoes do acampamento — 10/10/2026

Problema reproduzido no Unity 6000.0.84f1: interagir com o bau sem sincronizacao abria a tela de expedicao. A mesma condicao em `CampController.Interact` atendia todas as estacoes: tanto a falta de sincronizacao quanto um comando pendente definiam `ExpeditionPanelOpen=true` antes de verificar o objeto proximo.

A correcao identifica primeiro a estacao. Somente o portao abre a tela de expedicao/conexao. As outras estacoes exibem uma mensagem de espera, sem abrir outra tela nem executar a atividade, quando o perfil nao foi sincronizado ou a acao anterior ainda esta pendente. Com o perfil pronto, as rotas de bau, pier, bancada e fogueira continuam as existentes. A mensagem tambem e atribuida a `CampSession.Message`, de onde a interface realmente le o aviso.

Verificacao: `CampInteractionChecks.Run` falhou no codigo anterior com `bau desconectado nao abre expedicao`. Depois da correcao, passaram 20 verificacoes nativas dos componentes no editor Unity, cobrindo as quatro estacoes sem sincronizacao/com comando pendente, o portao sem sincronizacao e as telas locais de bau/fogueira/bancada sincronizadas. Resultado: `Docs/Auditoria/Evidencias/camp-interaction-checks.json`.

Cliente Windows gerado por `CampInteractionChecks.BuildClient`, na copia isolada `Builds/SkillsValidation/Builds/Client`. Codigo compilado confere com o arquivo atual. Protocolo permanece 13; nenhuma regra de servidor, economia, cena ou prefab foi alterada. Este teste verifica o despacho real das interacoes no editor; nao e uma prova de multitouch nem uma nova validacao completa das atividades online.
