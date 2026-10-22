# Modelos e animações escolhidos — Blender

Pedido do usuário em 12/10/2026: animar todos os visuais escolhidos, com cuidado e um por vez, incluindo o baú abrindo. Os modelos são construções 3D originais que aproximam as referências escolhidas; as ilustrações geradas não são usadas para fingir frames animados. Geometria e materiais são simplificados e procedurais, com controles editáveis e ossos de membros.

Ordem: Chest (baú de viagem surrado), Mimic (Mordida rasteira), Charger (Ombros ossudos), Listener (Escutador), Shell (Enrolado), Herald (Costelas ressonantes), Devourer (Colhedor), Spore (Bolsas dorsais), Mother/AttackBulb/HealingBulb/Minion (família Nó de raízes).

Cada pasta concluída contém arquivo .blend editável com o mesmo nome da pasta, idle.png, perspective.png, PNGs dos clipes, GIFs, folhas de sprites para avaliação, contact.png e clips.json. Os scripts comuns em common.py usam FK gravado a partir de trajetórias analíticas; não dependem de IK com pole solto. Câmeras, materiais, pose de repouso e intervalos de animação ficam no .blend. Modelos ficam na pose parada ao salvar.

- Caminhada: 1–32, frame 33 fecha o ciclo; a perna/mão percorre a fase de apoio em uma direção e retorna na fase elevada.
- Habilidades: intervalos explícitos em clips.json e marcadores no Blender. Alguns clipes longos têm renders a cada dois/seis frames para avaliação; a timeline editável mantém keyframes a 24 fps. Duração do GIF acompanha os números dos frames, com pausas extras para inspeção nos extremos.
- Pose parada: 501. Disfarce fechado do Imitador: 601, mesmo modelo/câmera do baú comum. Dentes e braços ocultos; nenhum indício externo.
- Frente do modelo +X, prévias superiores com frente para cima. Uma integração futura deve exportar a orientação/camadas e escala apropriadas.
- FX de geração de bulbo e viagem da cura são apenas demonstrações opcionais; efeitos do jogo continuam separados. Nomes FX_ permitem ocultá-los ao exportar o corpo.

Fontes: build_chest.py, build_creature.py, build_plants.py e package_preview.py. Renderização e inspeção visual de artefatos Blender, sem testes/builds/sessões de validação Unity. Nesta etapa não substitui assets ativos nem modifica gameplay, chance de Imitador, loot ou rede.
