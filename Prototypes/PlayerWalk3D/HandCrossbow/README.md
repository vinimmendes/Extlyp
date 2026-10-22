# Personagem com besta de mão

Produzido no Blender 5.2.2 LTS via MCP oficial. Skill `blender-animation` instalada de `RobLe3/cc-blender-skill`, caminho `plugin/skills/blender-animation`, a pedido do usuário.

Fonte preservada: `../VisualV1/personagem-rustico.blend`. Cena editável independente: `personagem-besta-de-mao.blend`. A pistola e os braços anteriores permanecem na cópia, ocultos tanto no render quanto no viewport. Sem alteração do arquivo original.

## Clipes

- Caminhada/mira: 32 quadros, mantém rig e deslocamento existentes das pernas. Besta na mão direita; braço livre em repouso.
- Caminhada descarregada: mesmos 32 quadros, corda solta e nenhum virote.
- Recarga: quadros 41–89, 49 amostras ao longo de 2 segundos. Baixa a arma, mão livre puxa a corda, busca e assenta um virote, retorna à mira.
- Disparo: quadros 97–104, 8 quadros a 24 fps. Soltura da corda, desaparecimento do virote e movimento curto de recuperação.

Modelagem própria com arco espelhado, trilho central, madeira, aço e couro nos materiais existentes. Versão tridimensional simplificada da silhueta aprovada do ícone; não é a textura do ícone colada sobre o personagem.

Render: câmera ortográfica existente 2,65, células RGBA 256×256, Cycles 16 amostras com denoise. Atlas em `Assets/Resources/PlayerCharacter/HandCrossbow.png`, `HandCrossbowEmpty.png`, `Actions/ReloadCrossbow_1.png` e `Actions/ShotCrossbow_1.png`. Mesma escala/pivô dos atlas anteriores; metadados derivados do perfil existente.

Geradores: `Tools/Blender/HandCrossbowAnimation.py` e `Tools/Blender/PackageHandCrossbow.py`. Não executam automaticamente Unity nem alteram outras animações. O construtor recusa sobrescrever a cena criada. GIFs `Walk.gif`, `EmptyWalk.gif`, `Reload.gif`, `Shot.gif`; contato `poses.png`.

## Integração

Pose visual 6 para a besta; índice 7 reservado ao atlas descarregado. Caminhada segue distância real; disparo segue `ShotSequence`; recarga segue `ReloadEndsAt`, normalizada à duração da arma. Pernas permanecem animadas sob a camada superior de ação. O sprite avulso anterior da besta na mão foi retirado para evitar duplicação; ícones de inventário e mundo continuam os aprovados.

Origem de disparo alinhada à posição da besta no modelo (0,96; -0,19), convertida pela escala existente. Sem alteração de dano, duração, consumo, saves ou protocolo. Equipamento/munição privados continuam privados: dono/servidor usam o carregador real; observadores inferem a aparência descarregada de disparos/recargas observados. Quem entra durante a sessão não recebe o carregador privado, portanto a aparência inicial remota assume a pose pronta até um evento conhecido.

Revisão visual dos renders e revisão estática de referências, dimensões, alpha e caminhos concluídas. A mensagem de retorno de um lote MCP usou lista em vez de dicionário; os 89 renders já estavam concluídos e foram conferidos no disco, sem repetir o lote. Mais 32 quadros descarregados foram produzidos separadamente.

Não foram iniciados Unity, testes automatizados de jogo ou builds. Encaixe, visibilidade, recarga cancelada e reprodução entre clientes ainda aguardam avaliação prática.
