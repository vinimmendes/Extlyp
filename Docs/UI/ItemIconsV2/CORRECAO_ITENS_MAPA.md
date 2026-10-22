# Itens quadrados no mapa — correção do fallback

Relato do usuário: itens comuns e de treinamento aparecem como quadrados no mapa.

Evidência estática: GetWorld recorria somente à arte anterior. Para os seis equipamentos de treino, GetLegacy retornava explicitamente SpriteFactory.Square. Para itens comuns cuja referência antiga não resolvesse, o retorno era null e ApplyWorld deixava o quadrado do prefab visível. Os arquivos antigos de itens contêm tabelas de sub-sprites; a falha concreta de carregamento desses arquivos não foi reproduzida na Unity nesta revisão.

GetWorld agora mantém a prioridade de WorldIcon explícito, arte ambiental das catacumbas e arte anterior válida. Para treino ou referência antiga ausente, recupera o sprite completo do catálogo novo. A recuperação não muda escala de coleta, colisor, identidade, rede ou interface. Faltas de referências comuns emitem um aviso por item; ausência das duas fontes emite erro identificando o item.

Prova focada em `Tools/Audit/WorldItemVisualProbe.cs`: compila a fonte real ItemIconLibrary contra substitutos das APIs de apresentação. Passaram os casos de arte antiga válida, referência comum ausente, seis itens de treinamento, prioridade de WorldIcon, arte das catacumbas, arma procedural, ID inválido e aviso sem repetição. A prova não exercita importação, serialização ou renderização Unity.

Inspeção dos 59 sprites V2 também passou. Nenhuma sessão Unity ou build do jogo foi iniciada. Ainda é necessário conferir o resultado no mapa; se os itens comuns persistirem como quadrados apesar do aviso de recuperação, investigar o carregamento e o material em execução, sem atribuir esse comportamento a uma causa ainda não observada.
