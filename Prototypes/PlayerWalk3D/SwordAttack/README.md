# Ataque de espada — protagonista atual

Produzido em 21/10/2026 no Blender 5.2.2 LTS pelo MCP oficial, a partir de
`../VisualV1/personagem-rustico.blend`, preservado sem alterações.

- `protagonista-ataque-espada.blend`: fonte editável, incluindo rig de caminhada original,
  controles animados de tronco, braços e empunhadura. A espada permanece uma camada Unity separada.
- `Upper/001.png` a `020.png`: tronco e braços transparentes de 256 × 256, com câmera original.
- `SwordGrip.json`: coordenadas locais Unity da mão e direção da lâmina por quadro.
- `ataque-espada.gif`: composição de apresentação com pernas em repouso e a espada enferrujada
  existente, usando os mesmos pivôs e transformações da integração. Duração de 0,80 s.
- `poses-espada.png`: preparação, impacto, conclusão e retorno à guarda.

Quadros 1–8: preparação; 9: impacto; 10–12: conclusão; 13–20: retorno.
Quadros 1 e 20 têm a mesma guarda. Os tempos reais vêm dos timestamps do servidor,
com 0,30/0,50 s para a enferrujada e 0,25/0,45 s para a curta.

Produção: `Tools/Blender/SwordAttackAnimation.py` via MCP na cena original; empacotamento:
`Tools/Blender/PackageSwordAttack.py`. Não executar `build()` sobre esta entrega existente.
Atlas e metadados exportados para `Assets/Resources/PlayerCharacter/Actions`.

Revisão: poses renderizadas e composição inspecionadas; sem execução Unity, testes automatizados
ou builds. A prévia ilustra a apresentação local e não demonstra sincronização de rede.
