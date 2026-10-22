# C16 — Aviso do melee de monstros e área de dano

Implementada e revisada estaticamente em 17/10/2026. **Sem compilação, testes, Unity ou builds.** Referência E3-03; aceite em execução pendente da T12.

## Causa

O aviso era uma elipse posicionada à frente do monstro. O impacto usava um setor de raio `attackRange` e abertura de 65° para cada lado. Pontos válidos nas laterais do setor ficavam fora da elipse. A geometria do aviso também não representava paredes.

## Alterações

- `Assets/Scripts/Enemies/MonsterBehaviour.cs`: abertura compartilhada de 130° e preparação mantida em 0,3 segundo. O impacto usa `MeleeReach`, registrado quando o ataque começa, em vez de reler um alcance que poderia mudar durante o aviso. Origem, direção e alvo seguem a regra existente.
- `MeleeWarningPoint` calcula os pontos do setor em 64 segmentos e recorta cada raio pela primeira parede usando a mesma máscara e função de linha do impacto. O pequeno ajuste externo do arco impede que a corda de um segmento encolha o raio visível em campo livre.
- Nas quinas, a malha continua sendo uma aproximação por amostras. Para não permitir dano fora dessa aproximação, o impacto também verifica o triângulo recortado correspondente à direção do alvo, com os mesmos pontos usados pelo desenho. A checagem original de linha direta continua obrigatória, evitando dano através de parede mesmo se uma interpolação visual atravessar uma quina. O alcance máximo original e a abertura permanecem limites obrigatórios. Em bordas de obstáculos, isso pode recusar um ponto antes atingível que esteja fora da malha amostrada.
- `Assets/Scripts/Enemies/MonsterAppearance.cs`: substitui o sprite elíptico por uma malha de setor, com cor/progresso existentes. Reaproveita arrays por monstro e material unlit existente, sem alterar assets. Atualiza somente quando o aviso é visível nas condições anteriores.
- `Assets/Scripts/Enemies/Monster.cs`: libera a malha criada em runtime ao destruir o monstro; hide/despawn continua desativando a apresentação existente.

Não foram alterados dano, cooldown, alvo único, janela de preparação, carga/telegraph de investida ou melee do protagonista. O trabalho é restrito ao melee comum identificado na auditoria. Nenhuma mudança de engine, rede, protocolo ou asset de jogo.

## Limites e revisão

Foram revisados estaticamente criação, ativação, cancelamento, impacto, constantes compartilhadas, recorte, limites dos arrays e liberação da malha. O material consultado é `Resources/UnlitSprite`; não houve validação de renderização na URP. As amostras podem mostrar pequenos excessos perto de quinas, mas a linha física ainda bloqueia o dano. Não há promessa de correspondência pixel a pixel, ganho de desempenho ou legibilidade sob latência. O desenho usa o estado/posição observados pelo cliente e o servidor mantém a autoridade do impacto.

Na T12, conferir material/renderização, alcance frontal e laterais em ±65°, incluindo o exemplo de 0,95 × alcance a 60°. Conferir pontos dentro/fora do setor, paredes retas/quinas/obstáculos estreitos, variação de orientação, transformação/escala do monstro e alcance registrado no início. Confirmar que alvo fora da malha não recebe dano e que a linha bloqueia ataques através de parede; observar possíveis excessos visuais. Verificar cancelamento, morte, hide/show, duas máquinas e latência na preparação de 0,3 segundo. Medir custo somente se houver necessidade concreta; nenhum teste foi executado nesta etapa.
