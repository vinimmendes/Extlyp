# Animação do baú integrada

Reaproveitados Open e Close do baú de viagem em Prototypes/MonsterAnimationBatch/Chest. Os 20 quadros de cada clipe foram empacotados em atlas 2048 × 768, com células de 256, canvas e pivô fixos. Tools/Prepare-ChestAnimation.py permite regenerar os atlas sem renderizar novamente o Blender.

ChestAppearance usa a nova imagem fechada nos baús comuns e no disfarce do imitador. ChestAnimationVisual acompanha OpenState e OpenProgress replicados pelo servidor. A abertura acompanha o tempo atual de saque; interrupção reproduz Close a partir da pose correspondente. Sucesso da gazua reproduz Open a 24 fps. Ao aparecer já aberto por visibilidade/reconexão, o baú mostra diretamente a pose aberta. Sair da janela de inventário não fecha um baú que o servidor mantém aberto.

Mantidos fechadura, raridade, conteúdo, RPCs e duração autoritativa. O corpo fechado conserva 1,5 × 1,2 unidades; a tampa aberta estende o desenho, com amostragem de visibilidade ajustada. Armários, portas e baú seguro da cabana não recebem essa animação. O monstro revelado mantém a aparência atual; apenas seu disfarce foi alinhado ao baú normal. A cor de raridade não tinge a madeira nova.

Despawn desativa/remove a apresentação e suas assinaturas, evitando duplicação ao reexibir o baú. O servidor sem dispositivo gráfico mantém o fallback geométrico e não carrega os atlas.

Revisão estática do fluxo de abertura, cancelamento, gazua e reaparecimento. Sem testes automatizados, Unity ou builds, conforme preferência do projeto. Avaliação prática pendente: abertura normal, cancelamento parcial, gazua, outro cliente observando e saída/entrada do campo de visão.
