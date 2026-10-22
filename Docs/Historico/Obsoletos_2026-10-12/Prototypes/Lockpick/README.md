# Lockpick — prototipo isolado

Esta pasta fica fora de `Assets`, `Packages` e `ProjectSettings`. Nada foi importado, registrado ou ligado ao jogo.

- `Code/`: logica C# independente de engine, estados, ferramenta com desgaste, tentativa exclusiva e entradas sequenciadas. As notas de integracao descrevem as responsabilidades futuras do servidor.
- `Visual/lockpick-preview.html`: previa interativa que pode ser aberta diretamente em um navegador. Controles mouse/toque e teclado; mecanismo ilustrado, pinos, tensao, desgaste e som opcional.
- `Visual/VISUAL-DIRECTION.md`: direcao artistica e limites da demonstracao.

A gazua usa chance crescente de quebra por erro, sem contador de erros restantes na interface. Cancelar, retomar, acertar e mudar de fechadura preservam o desgaste da mesma ferramenta. Apenas uma gazua nova reinicia o risco. Os valores iniciais de probabilidade sao ajustaveis e ainda nao foram balanceados em execucao.

Os dois entregaveis sao independentes. A simulacao da pagina nao executa nem verifica o C#. Estado atual: rascunhos revisados estaticamente, sem compilacao, testes automatizados, Unity ou builds, conforme a preferencia do usuario. A validacao pratica inicial e manual.

Referencia de design: `Docs/LOCKPICK_REFERENCIAS_E_PROPOSTA_2026-10-10.md`. Este prototipo nao substitui a abertura atual dos baus nem transforma os moveis/portas do mapa em objetos interativos.
