# Aranha Pálida no jogo — 12/10/2026

Visual aprovado aplicado ao arquétipo Weaver. MonsterAppearance cria o sprite animado no cliente; o servidor dedicado não carrega esses atlases. A geometria roxa anterior fica apenas como fallback se os assets estiverem ausentes, com erro de carregamento no log.

Assets originais Blender transformados por package_runtime.py: Resources/Monsters/Weaver/Walk.png (32 frames) e Web.png (28). Cabeça orientada para +X, combinando com Monster/NetworkTransform; grade 8 x 4 de 256 pixels, alpha transparente, sem mipmaps/compressão. Cache compartilhado entre aranhas. Sprite de 2,4 unidades locais, corpo/collider original intacto.

- Caminhada avança por distância real, ciclo de 1,8 unidade de deslocamento. Rotação sozinha não movimenta pernas. Correções/teleportes não contam como caminhada.
- Parada usa a pose de repouso do Blender (Web frame 128). Pequena tolerância de 0,1 s para snapshots remotos; corpo dinâmico parado encerra caminhada imediatamente.
- AbilityState=1 reproduz os frames de preparação 101–119 conforme AbilityEnds e ServerTime, durante os 0,8 s existentes.
- Ao terminar, reproduz retorno 120–128 se estiver parada; se voltar a se mover, prioriza caminhada. Teia/projétil continuam no MonsterWorld, sem efeito desenhado duas vezes.
- VisibilityTarget continua controlando enabled; DamageFlash continua colorindo o sprite. O controlador de animação não força visibilidade ou tint.

Nenhuma alteração em vida, velocidade, colisão, alcance, dano, cooldown, navegação ou RPC. Protocolo permanece 17. Cliente precisa incorporar os assets/código; servidor instalado não foi atualizado nesta entrega.

Empacotamento concluído e referências/código revisados estaticamente, com git diff --check. Sem testes Unity, builds ou reinício de servidor, conforme preferência do usuário. Animação em partida e Android ainda dependem de validação prática.
