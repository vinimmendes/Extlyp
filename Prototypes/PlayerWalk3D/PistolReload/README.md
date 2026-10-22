# Protagonista — recarga da pistola

Status posterior em 12/10/2026: implementação autorizada e concluída no código/recursos. Tronco da recarga integrado, sincronizado com o servidor e combinado com pernas do runtime. Validação prática pendente; o adiamento abaixo é histórico. Detalhes em Docs/Integracao/ANIMACOES_IMPLEMENTADAS_2026-10-12.md.

Em 12/10/2026, o usuário orientou explicitamente manter esta animação pendente de implementação, junto às outras animações guardadas para integração posterior.

Animação construída no Blender 4.5 com o personagem rústico aprovado, sem substituir o modelo original, os sprites ativos ou a lógica de recarga. Status: pronta para avaliação; implementação no jogo pendente junto às outras animações.

Arquivo editável: protagonista-recarga-pistola.blend. Controle da pistola, carregadores retirado/novo e braços com keyframes editáveis. Modelo e roupas atuais preservados. Pernas fixadas na pose parada usada na troca para shotgun; uma futura integração deve combinar o tronco da recarga com as pernas do runtime quando o jogador se move.

Timeline de 1 a 37 em 24 fps: 36 intervalos, 1,5 segundo, corresponde ao valor padrão de GameplayDefinition.ReloadSeconds para a pistola. A integração futura deve usar a duração autoritativa real e tratar cancelamento/troca de arma; essa animação isolada não modifica essas regras.

- 1–3: mira inicial.
- 4–10: inclinar a arma e levar a mão de apoio ao carregador.
- 11–14: retirar o carregador.
- 15–18: guardar na cintura.
- 19–20: buscar outro carregador.
- 21–24: alinhar com a empunhadura.
- 25–28: encaixar.
- 29–37: voltar ao apoio com as duas mãos e à mira.

Troca genérica de carregador com retenção do anterior, sem animar munição individual ou descarte no chão. Não há manipulação do ferrolho; uma variante para arma completamente vazia pode ser criada separadamente.

Câmera superior do modelo aprovado, personagem voltado para +X. Renders reais Blender com alpha em frames/, 512 x 512. recarga-pistola.gif inclui pausas extras inicial/final apenas para avaliação. Atlas sprite-sheet-recarga-pistola.png, 2048 x 1280, oito colunas, células de 256, 37 frames e três células vazias. etapas-recarga-pistola.png apresenta fases principais.

Fontes: ../build_pistol_reload.py e ../package_pistol_reload.py. Renderização e inspeção dos artefatos Blender; nenhum teste, sessão de validação ou build Unity executado.
