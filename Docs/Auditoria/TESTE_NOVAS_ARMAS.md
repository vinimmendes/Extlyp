# Teste focado — botão de ataque, besta e espada enferrujada

Executado a pedido do usuário: primeiro prova de regras em C#, depois Unity 6000.0.84f1 em Play Mode. Não foi feito build de jogador nem utilizada conta ou arquivo de progresso real.

## Código

`Tools/Audit/NewWeaponsRulesProbe.cs` compila os arquivos reais `ItemCatalog.cs` e `WeaponCatalog.cs`, com substitutos mínimos de matemática Unity e colaboradores de domínio. **14 verificações passaram**: classificação da besta/munição, capacidade 1, recarga 2 s, intervalo 1 s, dano 30, queda de dano/limite de alcance, dispersão em movimento, dano 16/stamina 15 da espada, preparação/recuperação, arco/alcance e rejeição dos seis IDs retirados. Essa prova não executa entrada, rede ou física.

Evidência: `Logs/new-weapons-test/rules.txt`.

## Unity

`Assets/Editor/NewWeaponsGameplayProbe.cs`, entrada explícita `NewWeaponsGameplayProbe.Run`, abre a cena existente sem salvá-la e cria um host loopback temporário na porta 19387. A aprovação de conta é desviada apenas pelo componente de teste dentro do Editor; nenhum caminho de persistência é usado. Atores são colocados fora do mapa para isolar a física das armas. O teste encerra o Editor após gravar o resultado.

Eventos simulados passam pelo raycast da uGUI e pelos handlers de arraste/pressionamento/soltura, `TouchControls`, `PlayerController`, RPC e lógica autoritativa do host. Não é uma chamada direta a `ServerFire` para comprovar o botão.

**37 verificações passaram** na execução final:

- Kit com besta descarregada, espada enferrujada e cinco virotes.
- Mira por arraste na HUD; raycast na posição exibida do botão; pressionar/soltar atualiza `FireHeld`.
- Botão da besta vazia inicia recarga; um virote passa da reserva para a arma.
- Disparo consome um virote e atinge alvo nativo da física: vida 500 → 470.
- Segurar brevemente o botão não duplica o disparo; sem munição não há tiro/recarga fictícios.
- Espada inicia preparação e consome stamina, aplica exatamente 16 de dano uma vez, recusa alvo a 2 unidades e recusa ataque com stamina zero.

## Investigação e correção

A primeira execução falhou no acerto porque o mouse do Editor podia alterar a direção da mira da fixture entre os comandos. O teste passou a manter um arraste de mira para leste pela HUD; a trajetória foi registrada em `shot-state.txt`. Não houve correção de dano, trajetória ou alcance baseada nessa falha de preparação.

Na revisão do estado do botão, foi constatado que a condição visual exigia munição carregada, embora o caminho real `ServerFire` inicie a recarga quando existe reserva. O botão ficava acinzentado, mas o handler de ponteiro ainda recebia o comando. `UguiGameHud` agora mantém a ação visualmente habilitada quando há munição carregada **ou reserva**. A execução final inclui uma asserção específica desse estado e repetiu os demais cenários.

Evidências finais: `Logs/new-weapons-test/unity-result.txt`, `unity-checks.txt`, `editor.log`, `shot-state.txt`, `sword-hud.png`. Tentativas anteriores preservadas em `unity-attempt1.txt`, `unity-attempt2.txt` e logs correspondentes.

## Limites

Teste com eventos de ponteiro simulados no Editor Windows e host local, não com cliente separado, rede com latência ou aparelho Android físico. Não comprova todos os controles físicos, multitouch de aparelho, acerto através de paredes, cancelamento de recarga ou desempenho. Screenshot conferida: HUD de ataque visível e rótulo ATACAR com a espada. O fundo preto é deliberado por usar uma área fora do mapa na fixture.
