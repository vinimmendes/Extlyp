# Interação e comporta — 19/10/2026

Relatos do usuário: botão de interação no centro; após baixar a comporta, nada acontece.

## Posição do botão

HudLayout usava o fallback central para Interaction, acima da fileira de ações. Após correção da interpretação conforme orientação do usuário, o padrão fica centralizado na parte inferior da área segura, com margem inferior de 16 dp e caixa 200×48 dp. A proposta intermediária à esquerda foi substituída. Preservados os demais controles e as posições personalizadas; quem salvou uma posição específica para Interação mantém essa posição e pode restaurar o padrão desse elemento no organizador.

Conferência aritmética em 568×320, 640×360, 720×360 e 960×480 lógicos: centro horizontal em metade da largura e borda inferior a 16 dp da área segura. Não equivale a teste de renderização, toque ou layout personalizado.

## Comporta nas catacumbas

O fluxo atual diferencia três saídas de extração e uma comporta de atalho. A ação de mecanismo 3 consome a chave, ativa DungeonShortcut e libera a passagem pela água; não cria uma quarta extração. A zona de extração não existe na comporta. Esse comportamento foi identificado no código, sem reproduzir a sessão do usuário.

A ação passou a dizer “Liberar atalho da comporta”. Após abrir, perto do mecanismo e sem outra interação prioritária, a HUD informa “Atalho aberto · saídas no mapa”, sem oferecer uma nova ação. Escadaria do Cortejo, Poço dos Coveiros e Canal dos Defuntos mantêm seus mecanismos e condições existentes. A mudança corrige a comunicação; não transforma a comporta em saída nem comprova que os triggers das três saídas funcionaram na sessão relatada.

Revisão estática e de posicionamento, sem iniciar Unity ou builds. Nenhuma alteração de rede, saves ou regras autoritativas da extração.
