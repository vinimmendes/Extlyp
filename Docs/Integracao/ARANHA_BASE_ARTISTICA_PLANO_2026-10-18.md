# Aranha pálida — conceito e plano da base artística

## Entrega e escopo

Conceito e planejamento solicitados em 18/10/2026. Proposta visual produzida; não representa aprovação do novo desenho nem implementação da remodelagem. Nenhum modelo Blender, atlas de runtime, collider, regra de combate ou parâmetro de rede foi alterado nesta tarefa.

Conceito atualizado: `Art/MonsterProposals/Aranha/aranha-palida-conceito-v3-dash-2026-10-18.png`. A versão v2 permanece preservada. Em 18/10/2026, o usuário definiu que a preparação do dash deve levantar as patas da frente.

![Aranha pálida — preparação com patas dianteiras erguidas](../../Art/MonsterProposals/Aranha/aranha-palida-conceito-v3-dash-2026-10-18.png)

Prancha gerada por imagem com `Prototypes/SpiderPale/Actions/actions-preview.png` como referência de identidade e poses. A vista três quartos comunica anatomia e material; a vista superior é a referência prioritária para a silhueta no jogo. Os estudos com borrão sugerem movimento, mas não serão copiados para os sprites. A contagem das patas, olhos e articulações precisa ser conferida no modelo, sem tomar uma imagem gerada como blueprint anatômico exato.

## Diagnóstico da base existente

Base da análise: fontes `build_spider.py`, `build_actions.py`, README e sprites já produzidos. A consulta ao Blender nesta etapa foi recusada; as observações abaixo não são uma nova inspeção do arquivo em execução.

- Abdômen e cefalotórax são derivados de esferas escaladas. O volume geral é legível, mas uniforme e com pouca transição entre as massas.
- Cada uma das oito patas usa segmentos cônicos e juntas esféricas. Essa construção é editável, porém evidencia peças encaixadas e pontas retas.
- O rig existente tem dois ossos por pata, `Femur` e `Tibia`, com poses gravadas por solução analítica; os empties são guias, não controles IK ativos.
- Manchas procedurais dão identidade ao abdômen, mas o ruído não substitui variação de forma, dobra da superfície e anatomia dos encaixes.
- As ações novas já diferenciam recolhimento e abertura das patas. A remodelagem precisa conservar esse espaço de movimento e a leitura das poses, em vez de acrescentar detalhes que se cruzem durante a animação.

## Direção visual proposta

**Identidade:** criatura pálida de ambiente úmido, pesada no abdômen, baixa e veloz nas pernas. Reconhecível como a mesma aranha aprovada, com acabamento mais orgânico e ameaçador.

Darkwood orienta atmosfera, desgaste e integração com pedra e abandono. Bullet Echo orienta leitura rápida da frente do inimigo, do ataque e das rotas de fuga. A anatomia, materiais e composição são próprios de Cyber Extraction.

### Silhueta e volumes

- Refinar o abdômen para uma forma de pera, com encontro frontal mais estreito e massa posterior mais ampla. Adicionar uma crista longitudinal rasa e poucas dobras grandes; evitar sulcos regulares de armadura.
- Rebaixar visualmente o centro do corpo e distribuir o apoio pelos pares traseiros. O peso deve parecer sustentado pelas patas, sem abdômen flutuando ou enterrado no piso.
- Fazer o cefalotórax escuro conectar-se ao abdômen por uma transição estreita e flexível, parcialmente protegida pela quitina.
- Diferenciar os quatro pares pela orientação e função: dianteiros expressivos na ameaça, centrais estabilizadores e traseiros propulsores. Preservar exatamente oito patas.
- Manter a frente reconhecível no sprite: cabeça escura, mandíbulas curvas compactas e distribuição das patas anteriores. Não aumentar a criatura para obter impacto visual.

### Patas e articulações

- Substituir a aparência de cones retos por seções afiladas com curvatura leve e mudanças suaves de espessura.
- Cobrir as juntas com abas de quitina sobrepostas; a membrana escura aparece principalmente na dobra. Evitar juntas que pareçam esferas expostas ou rolamentos.
- Criar uma ponta curta, curva e escura em cada pé, associada inicialmente ao osso inferior existente. Uma aparência de tarso adicional pode ser modelada sem acrescentar imediatamente um terceiro osso.
- Dar espessura suficiente às bases das patas para as poses de dash não parecerem quebradas. Controlar interpenetração entre raiz, fêmur e corpo.
- Espelhar a estrutura anatômica, mantendo pequenas assimetrias somente no desgaste. Não criar comprimentos diferentes que alterem passada ou apoio.

### Cabeça e detalhes

- Preservar oito olhos pretos, pequenos e sem emissão. Hierarquizar quatro principais e quatro menores para leitura do conjunto; não exigir oito pontos distintos quando a criatura estiver pequena na tela.
- Redesenhar as duas quelíceras com inserção sob a frente da cabeça e curva para dentro. Devem parecer capazes de morder, sem virar presas desproporcionais ou chifres.
- Acrescentar poucos pelos táteis perto de boca, articulações e pés apenas se sobreviverem à redução para 256 px. Preferir geometria simples para os pelos que modificam a silhueta, evitando sistemas densos de partículas.
- Não acrescentar crânio humano, olhos luminosos, neon, espinhos extensos ou sinais de uma habilidade que o jogo não possui. As pequenas saliências sugeridas pela prancha são opcionais e devem ser descartadas se prejudicarem a leitura.

### Materiais

Paleta inicial para ajuste sob a iluminação de exportação existente:

| Região | Direção | Cor-base aproximada |
| --- | --- | --- |
| Abdômen | Marfim envelhecido, sem branco puro | `#BCB5A0` |
| Manchas | Cinza oliva e terra seca, em massas irregulares | `#777667` / `#6D6450` |
| Cefalotórax | Carvão marrom | `#2D2923` |
| Juntas e pontas | Marrom quase preto | `#322E27` |
| Seda | Cinza marfim fosco | `#A9A58F` |

- Abdômen com variação ampla de pigmentação, pouco brilho e relevo sutil. Não transformar o noise em textura de pedra ou algodão.
- Patas um pouco mais duras e acetinadas; juntas mais escuras e foscas. Pequenas diferenças de rugosidade devem descrever materiais, sem exigir reflexo forte para ler a anatomia.
- Preservar o contraste entre abdômen, cabeça e pontas, inclusive quando o cenário perde saturação pela visão. Testar a leitura por valor, não apenas por cor.
- Exportar como albedo/iluminação e alpha dos sprites atuais. Não introduzir shader, mapa normal ou pipeline de iluminação adicional implicitamente.

## Movimento e apresentação

### Dash

- Preparação definida pelo usuário: erguer o primeiro par de patas acima da cabeça, com abertura lateral que forme uma silhueta ameaçadora. As outras seis patas permanecem apoiadas; as traseiras flexionam para preparar o impulso. Elevar levemente a frente do cefalotórax e conservar o peso sustentado atrás. Não acrescentar patas nem confundir quelíceras com membros erguidos.
- Durante os 0,7 s existentes, subir as patas, sustentar brevemente a ameaça e baixar o par dianteiro para a frente na transição ao avanço. Conferir que a elevação seja legível também pela câmera superior, não apenas em perfil; manter margens no canvas.
- Avanço: corpo baixo e alongado pela postura, patas posteriores empurrando e anteriores buscando apoio. Não esticar artificialmente o abdômen.
- Recuperação: apoio dianteiro, amortecimento curto e retorno à pose base. Sem deslizar os pés enquanto estão plantados.
- Usar a câmera real de exportação para julgar a pose. A imagem lateral da prancha não deve ser a única pose bem resolvida.

### Descida

- Recolher os oito membros junto ao corpo, com separação suficiente para não se fundirem numa massa de pontas.
- Mostrar tensão no ponto posterior de fixação do fio e abrir as patas em sequência até o apoio.
- Coordenar pose e amortecimento com o deslocamento de tela e o fio compostos pelo código. Não gravar um segundo deslocamento lateral/vertical no atlas que duplique esse movimento.

### Lançamento e caminhada

- Preservar a identificação da preparação do lançamento, diferenciando-a das patas dianteiras altas e abertas do dash. O lançamento enfatiza abdômen/produção de seda; não repetir a mesma silhueta de ameaça.
- Transferir a caminhada existente e revisar apoio de cada pé após mudar a malha. Não aceitar clipping ou salto de volume apenas porque os keyframes antigos continuam funcionando.
- Manter os tempos autoritativos: dash 0,7/0,25/0,6 s; descida 1,5 s; preparação do cerco 0,8 s. Mudança de regra é uma decisão separada.

### Teia de teto

- Evoluir os círculos e raios regulares para uma trama assimétrica, com centro denso, regiões abertas, fios de sustentação e pequenos trechos rompidos.
- Preservar uma silhueta legível como alvo destrutível. A textura não pode desaparecer sobre piso claro nem parecer um círculo de magia.
- A prancha sugere fixação ambiental no teto. Para o asset final, exportar a seda isolada com alpha, sem embutir pedra, teto, sombra longa ou arquitetura no sprite.
- Diferenciar teia de teto intacta dos avisos e das teias de lentidão no chão. Não integrar decoração estática que pareça um perigo ativo.

## Plano de trabalho no Blender

1. **Cópia de trabalho e inventário.** A partir de `Actions/aranha-palida-acoes.blend`, salvar um arquivo novo, por exemplo `ArtisticV2/aranha-palida-artistica-v2.blend`. Confirmar versão, rig, pesos, câmera, unidades e ações antes de alterar. Conservar as duas fontes anteriores.
2. **Proporção e silhueta.** Ajustar primeiro abdômen, cabeça e oito patas em material simples. Comparar vista superior, lateral e três quartos. A vista superior deve funcionar dentro do mesmo envelope de sprite e com frente +X.
3. **Malha e encaixes.** Refinar a topologia dos segmentos, inserir loops nas curvas e fechar as transições. Usar modificadores não destrutivos onde ajudarem, sem engrossar articulações ou perder a forma na deformação.
4. **Preservação do rig.** Primeiro tentar transferir pesos e manter os 16 ossos das patas. Se isso não resolver apoio ou articulação, documentar a necessidade de novos ossos antes de retargetear. Um rig novo não será presumido compatível com os clipes existentes.
5. **Materiais por massas.** Ajustar primeiro marfim/cabeça/juntas; depois manchas grandes, rugosidade e relevo. Acrescentar microdetalhes somente após a leitura em tamanho de jogo estar resolvida.
6. **Passagem das animações.** Revisar repouso, caminhada, preparação/liberação de teia, preparação/avanço/recuperação do dash e descida/pouso. Corrigir pesos e poses que revelem a origem em primitivas ou causem cruzamento de patas.
7. **Teia de teto.** Produzir a trama orgânica separada e assegurar que o centro sobreviva à redução da imagem. Manter as regras e a resistência atuais.
8. **Exportação candidata.** Gerar folhas novas em pasta de proposta, com o mesmo canvas, pivô, orientação e organização dos clipes. Produzir prévias de autoria para comparação; não substituir os atlases em uso nesta etapa.
9. **Escolha visual e integração.** Após a avaliação da proposta, promover os assets escolhidos em alteração própria e atualizar documentação de origem. Validação prática inicial na Unity continua a cargo do usuário, conforme AGENTS.md.

## Contrato de exportação e aceitação artística

- Runtime permanece 2D URP, com sprites RGBA em células de 256×256 e orientação +X.
- Manter pivô central, referência de frame de 2,4 unidades, 8 colunas e configuração de importação existente. Não alterar o transform raiz, o colisor ou os alcances de combate para caber uma silhueta maior.
- Preservar todos os quadros necessários: caminhada 32, teia 28, dash 32 e descida 32; a teia inicial usa imagem independente. Os segmentos do dash permanecem 12/6/14 quadros e são temporizados pelo estado de rede.
- Conservar pelo menos 16 px transparentes nas bordas em todas as poses. Não cortar pés ou fio para preencher o canvas.
- Inspecionar em 256 px e em reduções de 96/64 px: frente reconhecível, espaços entre as patas, ameaça da postura e distinção entre material claro/escuro. Essas inspeções são de produção artística; não validam gameplay.
- Conferir sobre pedra escura, pedra média e terra, com iluminação de exportação suave e neutra. Aumentar contraste na arte antes de propor halos ou luzes exclusivas.
- Oito patas e oito olhos no modelo; duas quelíceras, sem duplicação de apêndices. Ausência de junções abertas, facetas evidentes, clipping, saltos de pivô ou mudança involuntária de escala entre clipes.
- Sem alteração de dano, velocidade, vida, protocolo, população ou regras do teto/cerco como consequência desta remodelagem.

## Entregáveis previstos para a futura produção

Arquivo `.blend` adicional editável; malha refinada e pesos revisados; materiais organizados; ações compatíveis; atlas candidatos separados; prancha antes/depois na câmera de jogo e GIFs de autoria; instruções de exportação e relatório do que foi inspecionado.

Esta entrega contém somente a prancha conceitual e este plano. A remodelagem no Blender aguarda uma etapa própria.
