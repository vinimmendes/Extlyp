# Etapa 7 — chama da tocha

Entrega: `working-v3/warrior_v3-step07-flame.blend`, derivada da etapa 6 preservada.

## Alterações

- Três camadas principais mantêm sua topologia, pesos em `hand.L`, shape keys `Basis`/`Flicker` e Actions Heavy/Alert existentes.
- Contornos menos uniformes, com lobos amplos e inclinações diferentes por camada, mantendo o anel de fixação.
- Duas línguas secundárias menores com geometrias assimétricas e shape keys próprias. Seus valores de `Flicker` seguem a camada externa por drivers; não usam nova cadência ou novos keyframes.
- Gradientes de emissão distintos para laranja externo, corpo amarelo e núcleo marfim quente. O gradiente usa o atributo de altura `FireHeight`.
- Camadas internas deslocadas em direção à câmera de jogo e núcleo levemente alongado, para reduzir a ocultação pelas superfícies externas opacas. Fixação na tocha preservada.

As línguas menores diferem pela forma e pela escala da deformação, mas não têm movimentos temporais independentes: acompanham o `Flicker` externo. A troca de cadência é herdada desse vínculo.

Não foram alterados cabo, suporte da tocha, posição da mão esquerda, rig, caminhada, equipamentos, materiais das etapas anteriores, luzes ou câmeras. O reflexo quente no metal pertence ao ajuste de iluminação da etapa 8.

## Revisão e limites

Revisão estática no quadro 1 pelo viewport, em Blender 5.2.2 LTS. Ajustadas as camadas internas após observar ocultação do núcleo. Sem render novo, reprodução da animação, simulação, teste Unity ou build.

A leitura final do núcleo sob a iluminação de entrega pertence à etapa 8. Sobreposição das camadas, continuidade temporal, enquadramento e comportamento dos drivers ao longo do ciclo continuam pendentes da etapa 9.

Registro: `stage07-change-log.json`. Fonte: `Tools/Blender/WarriorV3Stage07.py`. Nenhum script do pacote original foi executado.
