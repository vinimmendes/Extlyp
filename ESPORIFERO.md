# Esporífero

Criatura original para extlyp, com direção visual de horror florestal inspirada em Darkwood.
O PNG é o conceito apresentado na conversa, incluído como sprite de idle provisório.
Não é uma folha de animações: respiração e aviso usam escala e cor discretas em runtime.

## Como usar

Abra Assets/Scenes/Game.unity no Unity 6000.0.84f1. A cena e os prefabs já estão
ligados; não é necessário reconstruir o MVP. O servidor gera um Esporífero em (22, 18)
ao iniciar a partida, além do monstro existente. O menu Montar MVP também preserva
a integração ao recriar os assets.

Servidor e clientes devem usar esta branch: o protocolo foi elevado de 5 para 6,
pois jogadores e bots agora incluem um NetworkBehaviour de contaminação.
Recompile o servidor antes de testar com os clientes novos.

## Regras

- Vida: 150. Patrulha: 0,8; perseguição: 1,7; alcance corpo a corpo: 1,5.
- Cada 38 pontos de dano efetivo acumulado dispara um aviso de 1,2 s.
  Durante o aviso, a criatura para e suspende ataques. Dano recebido durante
  esse período não enfileira descargas. Um golpe letal cancela a preparação.
- Nuvem: raio de 2,8 unidades, duração de 8 s. É um objeto de rede independente,
  que persiste após a morte da criatura e desaparece ao encerrar a partida.
- Vento: configurado em MatchManager.sporeWind, inicialmente (0,65; 0,15)
  unidades por segundo, indicado no HUD pelo destino. A nuvem para ao atingir paredes;
  linha de visão impede contaminação através delas.
- Dentro das áreas de abrigo definidas por MapBuilder.IsSheltered, a descarga
  não recebe deslocamento pelo vento. Para novos interiores, estenda essa função.
  O mapa atual não tem um sistema genérico de volumes de ambientes fechados.
- Exposição: +20%/s dentro de uma ou mais nuvens, -15%/s fora delas.
  A partir de 40%, permanecer exposto causa 4 pontos de dano por segundo.
  A tosse emite ruído de alcance 8 a cada 2 s, perceptível pela IA existente.
  Sair da nuvem interrompe dano e tosse imediatamente; morte, extração e fim
  da partida zeram a exposição. Sobreposição não multiplica dano/exposição.
- Preparação, centros de nuvens e contaminação são replicados via NetworkVariable.
  Clientes não decidem dano, vento nem criação de nuvens.

## Verificação

Menu: Cyber Extraction > Verificar Esporifero. Também pode ser executado em batch:

```text
Unity -batchmode -nographics -projectPath <Extlyp> -executeMethod SporeChecks.Run -quit -logFile spore-checks.log
```

O menu cobre limites de dano/exposição, recuperação, abrigos, importação do sprite
e referências dos prefabs. Física e rede exigem estes cenários em Play Mode:

1. Host + cliente: causar 37 de dano não prepara; o próximo ponto prepara.
   Conferir o mesmo aviso e a mesma posição da nuvem nas duas janelas.
2. Acertar durante o aviso: não gerar uma fila de nuvens. Matar durante o aviso:
   nenhuma nova nuvem. Matar após a descarga: nuvem permanece até completar 8 s.
3. Ficar na nuvem por 2 s: alcançar 40%; continuar por mais 1 s: perder 4 de vida.
   Sair: dano e tosse param, exposição decai até zero.
4. Sobrepor duas nuvens: não dobrar exposição/dano. Colocar uma parede entre nuvem
   e jogador: não contaminar. Verificar drift bloqueado por paredes.
5. Gerar nuvem dentro de um abrigo: centro imóvel. Ao ar livre: deslocamento
   indicado pelo HUD. Distância e lado de origem do vento permitem evitar exposição.
6. Encerrar/resetar a sala: nenhuma nuvem/exposição remanescente.
   Testar um bot e um jogador humano para confirmar dano e ruído no servidor.
7. Conferir sprite girado para a direção de movimento, ocultação fora da visão
   e sinal de preparação em uma janela cliente.

Neste ambiente não há editor Unity nem compilador C#. A verificação local cobre
estrutura dos assets, referências e whitespace; os checks C# e cenários acima
precisam ser executados no Unity antes de integrar a branch à principal.
