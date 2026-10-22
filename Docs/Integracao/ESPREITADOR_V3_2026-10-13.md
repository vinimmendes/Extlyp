# Espreitador encapuzado V3 — integração

O Stalker usa agora o modelo encapuzado aprovado, substituindo o Rastejante. Fonte preservada em `Prototypes/StalkerHoodedV3/Animation`.

- `Assets/Resources/Monsters/Stalker/Walk.png`: caminhada superior, 32 quadros.
- `Assets/Resources/Monsters/Stalker/ClawAttack.png`: golpe de garras superior, 20 quadros; substitui `Bite.png`, mantendo o GUID do importador.
- Células de 256 pixels, oito colunas, transparência e frente +X. Escala corresponde ao enquadramento ortográfico de 3,2 unidades; pivô central fixo.
- Caminhada acompanha deslocamento real; parado usa o quadro 20 do golpe. Quadros 1–12 acompanham o aviso de ataque de 0,3 segundo; recuperação segue a 24 fps. Cancelamento do aviso interrompe o golpe visual.

Integração feita no construtor visual compartilhado: vale para expedição e laboratório, incluindo clientes remotos. Visibilidade, flash de dano, colisão, IA, vida e dano continuam usando as regras existentes, com combate autoritativo no servidor. O servidor sem dispositivo gráfico não carrega os atlas.

Revisão estática dos caminhos e configuração. Sem execução em Unity, testes automatizados ou builds, conforme preferência do usuário. Validação prática pendente: gerar Espreitador no laboratório, observar repouso/caminhada/garras, rotação, alcance aparente, luz/fog e observação por outro cliente.
