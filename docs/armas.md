# Novas armas para Extlyp

Pacote de alteracoes baseado na main, commit 40fb95aa3b0e6f6d6db96bc8772ae08ac169dafd.
Documento original das armas, mantido como referencia. Para instalar a branch atual e usar protocolo 8, siga armas-e-plantas.md.

## Instalar

1. Faça uma cópia de segurança do projeto atual.
2. Faça checkout da branch atual.
3. Abra o projeto com a versão Unity registrada em ProjectSettings/ProjectVersion.txt e aguarde a importação.
4. Execute Cyber Extraction > Verificar novas armas. Esse menu verifica 23 casos de catálogo, inventário e receitas; não exige alterar cenas/prefabs.
5. Recompile servidor e clientes juntos: o protocolo atual e 8 porque o estado replicado e os RPCs mudaram.

## Armas

| Arma | Capacidade | Dano | Intervalo | Recarga | Alcance |
|---|---:|---:|---:|---:|---:|
| Pistola | 12 | 20 | 0,35 s | 1,5 s | 10 |
| Rifle de ferrolho inspirado na Mosin | 5 | 75 | 1,35 s (ferrolho) | 3,2 s | 24 |
| Espingarda antiga de dois canos | 2 | 8 chumbos de até 13 | 0,55 s | 2,6 s | 9 |

A espingarda usa um cartucho por disparo (um cano por vez). Dano cheio até 3 unidades, caindo até cerca de 25% na distância máxima. As paredes interrompem cada chumbo. A Mosin fica mais precisa parada e perde precisão em movimento; seu alcance de tiro não aumenta a visão nem revela inimigos ocultos. A recarga do rifle repõe o carregador interno de uma vez ao terminar; não há animação de inserir cartucho individualmente. O ferrolho é representado pelo bloqueio temporário do disparo e pelo indicador na interface.

## Como usar

- Colete MO (rifle) ou SG (espingarda), e AR (munição de rifle) ou CA (cartucho), nos pontos de loot. A pistola continua sendo o equipamento inicial.
- Q ou 2: alterna entre as armas na mochila; no celular, toque no segundo botão da barra.
- R ou 3: recarrega; no celular, toque no terceiro botão. Arma vazia tenta recarregar ao disparar.
- 1: bandagem, como antes.
- Segurar ATIRAR/mouse repete os disparos respeitando cadência, ferrolho e recarga conferidos pelo servidor.
- HUD mostra arma, munição carregada, reserva, recarga e ferrolho.
- Cada arma preserva a munição carregada ao mover, largar ou extrair. Trocar/mover/largar durante recarga cancela a operação sem consumir munição. A troca não elimina o intervalo do último disparo.

No depósito há três receitas: 3 metais + 1 pólvora + 1 componente produzem 12 munições de pistola, 5 de rifle ou 4 cartuchos. A fabricação usa a fila existente de ações do acampamento, conferida pelo servidor ao entrar na expedição. Metais, pólvora e componentes também aparecem no loot. Sem espaço, a receita devolve os custos.

## Validação e limites

Verificações locais executadas: delimitadores do C#, referências a ItemId, preservação dos IDs antigos, contagem dos nomes/ícones e integridade do ZIP. Não havia Unity nem compilador C# no ambiente; não foi possível compilar ou executar a partida. O menu de 23 verificações foi incluído, mas precisa ser executado no Unity.

Teste manual pendente: host e cliente separado, pegar as duas armas, alternar, disparar, observar ferrolho, recarregar com reserva parcial, cancelar ao trocar/descartar, acertar paredes, comparar dano da espingarda perto/longe, morrer e recolher armas, extrair e entrar novamente. Conferir também HUD e botões em Android. Não foi gerado APK nem build do servidor.

As silhuetas são geométricas provisórias, com coronha de madeira e canos de metal, seguindo o estilo atual do protótipo. Não inclui arte final, sons, mira telescópica nem animação de abertura dos canos. Os bots continuam iniciando com pistola; o código de tiro e a cadência já suportam as novas armas quando equipadas. Os valores são iniciais para balanceamento.
