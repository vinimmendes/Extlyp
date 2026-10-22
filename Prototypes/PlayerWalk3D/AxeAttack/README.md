# Machado de duas mãos — protagonista atual

A apresentação final da arma está em `../AxeRefined/README.md`. Esta pasta preserva
a animação do corpo, os contatos de empunhadura e as versões da arma provisória.

Produzido em 21/10/2026 no Blender 5.2.2 LTS pelo MCP oficial, usando o modelo rústico
original em `../VisualV1/personagem-rustico.blend`, preservado sem alterações.

## Entrega

- `protagonista-ataque-machado.blend`: versão final editável, com o rig de caminhada original,
  tronco e braços animados, controles das duas empunhaduras e proxy do machado visível no viewport.
  O proxy não participa dos renders: a arma é uma camada separada no jogo.
- `Upper/001.png` a `026.png`: quadros RGBA de 256 × 256, sem pernas e sem arma.
- `AxeGrip.json`: posição da mão direita, rotação e escala aparente da arma por quadro.
  `leftX/leftY` registram a posição de apoio para referência de produção.
- `machado-arte-atual.png`: reprodução para a prévia das regras de desenho de
  `MeleeWeaponArt.Get(BattleAxe)`. A integração continua usando a arte existente do jogo.
- `ataque-machado.gif`: prévia composta de 1,05 s, com pernas em repouso.
- `poses-machado.png`: prancha de preparação, impacto, conclusão e retorno.

Quadros 1–10: preparação de 0,40 s; 11: impacto; 12–14: conclusão; 15–26: retorno.
Recuperação total de 0,65 s. Quadros 1 e 26 compartilham a guarda.
A mão direita usa o ponto 50/128 do cabo; a esquerda, 22/128. Os dois pontos são derivados
do mesmo eixo 3D, incluindo a inclinação que encurta o cabo visto de cima.

## Revisão solicitada pelo usuário

Empunhadura afastada da lâmina: mão direita no meio do cabo, esquerda próxima à ponta.
Preparação recua a arma para trás do ombro direito, com giro maior do tronco e máximo recuo
no quadro 8; quadros 9–11 aceleram até o impacto. Tempos e regras de combate preservados.
`ataque-machado-recuo.gif` apresenta esta revisão. A sessão anterior está preservada em
`antes-ajuste-recuo-empunhadura.blend`. Revisão visual realizada, sem validação Unity.

Skills adicionais de refinamento (`quality-refinement-autoloop` e `blender-skill-harmonizer`)
não estão instaladas; revisão realizada com as skills Blender locais, sem instalar dependências.

O tronco é desenhado acima da arma para que os dedos cubram o cabo. A apresentação da espada
mantém sua ordem anterior. Pernas continuam independentes do tronco e ligadas ao deslocamento.

## Reprodução da produção

`Tools/Blender/AxeAttackAnimation.py`: abrir o modelo rústico original pelo MCP e executar `build()`.
O comando recusa sobrescrever a entrega por padrão. `revision=True` é reservado a revisões explícitas
da ação produzida nesta tarefa, após preservar uma cópia incremental.
`Tools/Blender/PackageAxeAttack.py`: empacotar renders, metadados, GIF e prancha.

`sessao-espada-preservada.blend` preserva a sessão que estava aberta, com alterações não salvas,
antes da troca de arquivo. `*-v1.blend` e `*-v2.blend` preservam etapas intermediárias do machado.

Revisão visual realizada nas poses renderizadas; código revisado estaticamente. Não foram
executados testes automatizados, sessões Unity ou builds. A prévia não valida combate ou rede.
