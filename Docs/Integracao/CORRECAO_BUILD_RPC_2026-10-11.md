# Correção da compilação do servidor

O log da atualização reportou um erro do IL postprocessor do Netcode: o método marcado com `[ServerRpc]` em `ExtractionKnowledge.cs` se chamava `SelectExitRpc`. A API exige o sufixo `ServerRpc`. O método e seu chamador foram renomeados para `SelectExitServerRpc`, preservando as regras da seleção de destino.

Verificação limitada ao erro relatado: atualização do servidor com `Update-Server.ps1`. A primeira repetição dentro da sandbox caiu no Mono ao iniciar o socket interno de BuildReportRestService; a repetição fora da sandbox foi autorizada e terminou com código 0, instalando o novo servidor. Manifesto confirma protocolo 16. Nenhum APK foi gerado; a compilação não valida a jogabilidade, os caminhos ou o balanceamento do mapa.
