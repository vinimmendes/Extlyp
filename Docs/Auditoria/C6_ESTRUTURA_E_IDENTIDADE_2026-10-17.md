# C6 — Estrutura dos inventários e identidade dos itens

Implementada e revisada estaticamente em 17/10/2026. **Não testada nem compilada.** Referência: E2-05. Quantidades e limites de munição permanecem na C7.

## Causa identificada por leitura

O validador percorria os slots sem exigir o tamanho da grade nem verificar todos os IDs de item. Equipamentos de treinamento fora dos slots equipados podiam passar sem identidade. Na carga de disco, `Ensure` e a recuperação da expedição rodavam antes da validação: estruturas ausentes podiam ser preenchidas, e a reserva substituía a mochila antes de seu conteúdo original ser conferido. A desserialização também podia normalizar campos ausentes ou estreitar valores do enum para byte.

## Alterações

- `CampProfileMigration.cs`: valida estrutura antes de `Ensure` e da migração; exige grades de 9 slots e equipamento de 8 slots, IDs presentes no catálogo, lista de chão válida e identificadores de chão não vazios/não repetidos por perfil. Arma ativa admite somente os slots 6/7 ou -1, já utilizado quando não há arma. Expedição ativa exige ID e reserva coerentes com a versão.
- Armas atuais e todos os equipamentos ocupados precisam de identidade. A verificação global já existente de identidades repetidas continua abrangendo todos os proprietários vivos, inclusive suas reservas ainda não confirmadas.
- Reservas confirmadas são conferidas com um conjunto separado de identidades por reserva. Isso verifica duplicações internas sem confundir a fotografia histórica com a posse atual de outro participante.
- `SharedCampMemory.cs`: confere contêineres, arrays, entradas e IDs no JSON original antes do `JsonUtility`. Valida a coleção desserializada antes de recuperar a expedição, além da validação final já existente. Estado de expedição fora de 0–2 é recusado antes da conversão para byte.
- `CampProgress.cs`: confere a estrutura recebida por `Save` antes da cópia por JSON.

## Política para dados históricos

- Mantida a migração já existente de armas sem identidade em perfis anteriores à versão 4. Essa tolerância ocorre apenas na leitura anterior à migração; não autoriza armas atuais sem identidade nem troca identidades duplicadas por outras novas.
- Equipamento ausente em perfil anterior à versão 2 pode continuar sendo criado pela migração existente. Isso não autoriza preencher slots ausentes ou redimensionar arrays existentes.
- Mochila, baú e lista de chão ausentes, arrays nulos/com tamanho errado, IDs desconhecidos e estruturas ambíguas são recusados. Não há truncamento, descarte, reconstrução de itens nem mudança de versão do perfil.
- A escolha/preservação do candidato de disco segue a C5: candidato inválido não é aceito parcialmente; a recuperação pelo backup continua condicionada à validação completa. Nenhum save real foi aberto ou alterado nesta etapa.

## Verificação pendente

Revisados estaticamente os caminhos de criação, migração, reserva/commit, recuperação e gravação, os produtores de IDs de chão e o sentinela de arma ativa. Não foram executados testes, Unity, builds ou servidores/clientes, conforme a preferência do usuário.

Na T5, verificar rejeição de grades de 8/10 slots, equipamento de tamanho errado, slots nulos, IDs negativos/desconhecidos/fora de byte, chão inválido ou repetido, armas/equipamentos sem identidade e identidades repetidas entre perfis. Conferir migração de armas antigas e ausência histórica de equipamento; reserva confirmada pode compartilhar identidade com a posse atual, mas não conter duplicações internas. Na T4/T5, confirmar rejeição antes da recuperação e preservação dos arquivos quando principal e backup falham. Logs históricos não validam este código.
