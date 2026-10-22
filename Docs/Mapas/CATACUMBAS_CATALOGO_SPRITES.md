# Catacumbas — catálogo de sprites

Catálogo da etapa 3.1. IDs e metadados completos em [JSON](CATACUMBAS_CATALOGO_SPRITES.json); regras de produção em [Direção visual](CATACUMBAS_DIRECAO_VISUAL.md).

**Produzir** é pendência de arte; **reutilizar** é recurso atual preservado; **candidato** exige conferir encaixe antes de integração. Dimensões são a pegada visual de referência, em unidades do jogo; não autorizam novos colisores. Medida “preservar/UI” mantém a escala existente ou identifica um ícone.

## 3.1 — Reaproveitamento

| ID | Asset | Desenhos × quadros | Pegada (u) | Estado | Observações |
|---|---|---:|---|---|---|
| CAT-R01 | Aranha integrada | Existente | Preservar/UI | reutilizar | Manter frames, escala, orientação, collider e animações. |
| CAT-R02 | Espreitador integrado | Existente | Preservar/UI | reutilizar | Usar a versão de runtime atual; não substituir por proposta antiga. |
| CAT-R03 | Couraçado integrado | Existente | Preservar/UI | reutilizar | Preservar placas, frente protegida e flancos. |
| CAT-R04 | Baú animado integrado | Existente | 1.5 × 1.2 | reutilizar | 20 quadros por folha; preservar pivôs e leitor atual. |
| CAT-R05 | Caixote de cobertura | Existente | 2 × 2 | candidato | Apenas madeira/ferro em área compatível; não substituir sarcófagos. |
| CAT-R06 | Mesa e cadeira do atlas antigo | Existente | Preservar/UI | candidato | Conferir recortes, alpha e proporção; não reutilizar armário metálico. |
| CAT-R07 | Musgo e umidade da serraria | Existente | Preservar/UI | candidato | Conferir aparência sobre pedra antes de adotar; não mudar assets da serraria. |

## 3.2 — Pisos e água

| ID | Asset | Desenhos × quadros | Pegada (u) | Estado | Observações |
|---|---|---:|---|---|---|
| CAT-F01 | Calcário seco | 4 × 1 | 1.06 × 1.06 | produzido/integrado | Repetível e opaco; sem cobertura ou perigo ativo pintado. |
| CAT-F02 | Pedra úmida | 4 × 1 | 1.06 × 1.06 | produzido/integrado | Repetível e opaco; sem cobertura ou perigo ativo pintado. |
| CAT-F03 | Lajes quebradas | 3 × 1 | 1.06 × 1.06 | produzido/integrado | Repetível e opaco; sem cobertura ou perigo ativo pintado. |
| CAT-F04 | Terra com raízes | 3 × 1 | 1.06 × 1.06 | produzido/integrado | Repetível e opaco; sem cobertura ou perigo ativo pintado. |
| CAT-F05 | Piso do ossário | 3 × 1 | 1.06 × 1.06 | produzido/integrado | Repetível e opaco; sem cobertura ou perigo ativo pintado. |
| CAT-F06 | Transições entre pisos | 8 × 1 | 1.06 × 1.06 | produzido/integrado | Faixas e cantos; alpha; combinar bases sem aumentar relevo. |
| CAT-F07 | Água profunda | 2 × 1 | 2.12 × 2.12 | produzido/integrado | Superfície repetível; duas variações estáticas; movimento discreto por apresentação futura. |
| CAT-F08 | Margens de água | 8 × 1 | 1.06 × 1.06 | produzido/integrado | Reta, cantos e terminações; preservar fronteira física. |
| CAT-F09 | Marcas de inundação | 3 × 1 | 2.12 × 1.06 | produzido/integrado | Decalques de parede; sem colisão nova. |
| CAT-F10 | Degrau submerso | 2 × 1 | 2.12 × 1.06 | produzido/integrado | Decoração fora da passagem; não anuncia ponte acessível. |

## 3.3 — Arquitetura

| ID | Asset | Desenhos × quadros | Pegada (u) | Estado | Observações |
|---|---|---:|---|---|---|
| CAT-A01 | Parede reta | 3 × 1 | 1.06 × 1.06 | produzido/integrado | Encaixe visual no maciço existente; não estreitar corredor nem acrescentar collider. |
| CAT-A02 | Canto externo | 3 × 1 | 1.06 × 1.06 | produzido/integrado | Encaixe visual no maciço existente; não estreitar corredor nem acrescentar collider. |
| CAT-A03 | Canto interno | 3 × 1 | 1.06 × 1.06 | produzido/integrado | Encaixe visual no maciço existente; não estreitar corredor nem acrescentar collider. |
| CAT-A04 | Terminação de parede | 2 × 1 | 1.06 × 1.06 | produzido/integrado | Encaixe visual no maciço existente; não estreitar corredor nem acrescentar collider. |
| CAT-A05 | Parede quebrada | 3 × 1 | 2.12 × 1.06 | produzido/integrado | Encaixe visual no maciço existente; não estreitar corredor nem acrescentar collider. |
| CAT-A06 | Arco de passagem principal | 2 × 1 | 6.36 × 1.06 | produzido/integrado | Encaixe visual no maciço existente; não estreitar corredor nem acrescentar collider. |
| CAT-A07 | Pilar de pedra | 3 × 1 | 2.12 × 2.12 | produzido/integrado | Encaixe visual no maciço existente; não estreitar corredor nem acrescentar collider. |
| CAT-A08 | Moldura de nicho | 3 × 1 | 2.12 × 1.06 | produzido/integrado | Encaixe visual no maciço existente; não estreitar corredor nem acrescentar collider. |
| CAT-A09 | Painel de inscrições raspadas | 3 × 1 | 2.12 × 1.06 | produzido/integrado | Encaixe visual no maciço existente; não estreitar corredor nem acrescentar collider. |
| CAT-A10 | Entrada funerária e soleira | 2 × 1 | 4.24 × 1.06 | produzido/integrado | Encaixe visual no maciço existente; não estreitar corredor nem acrescentar collider. |
| CAT-A11 | Grade de cela | 3 × 1 | 2.12 × 1.06 | produzido/integrado | Barras, grade danificada e grade aberta encostada; decoração sobre arquitetura existente. |
| CAT-A12 | Porta medieval de cela | 2 × 1 | 2.12 × 1.06 | produzido/integrado | Aberta/fechada como estados visuais; dobradiça fixa e mesma pegada. Não criar interação ou bloqueio novo. |

## 3.4 — Sepulturas e coberturas

| ID | Asset | Desenhos × quadros | Pegada (u) | Estado | Observações |
|---|---|---:|---|---|---|
| CAT-P01 | Sarcófago baixo | 3 × 1 | 2.12 × 2.12 | produzido/integrado | Preservar pegada e cobertura baixa existentes. |
| CAT-P02 | Tampa de sarcófago | 3 × 1 | 2.12 × 2.12 | produzido/integrado | Mesmo pivô da base; repouso fechado ou tampa decorativa fora de rotas. |
| CAT-P03 | Lápide | 3 × 1 | 1.06 × 1.06 | produzido/integrado | Decoração em nichos; não criar cobertura nova. |
| CAT-P04 | Nicho com ossos | 3 × 1 | 2.12 × 1.06 | produzido/integrado | Ossos dentro da moldura A08; evitar tapete claro perto da aranha. |
| CAT-P05 | Urna funerária | 3 × 1 | 1.06 × 1.06 | produzido/integrado | Silhueta distinta de item coletável. |
| CAT-P06 | Banco tombado | 2 × 1 | 2.12 × 1.06 | produzido/integrado | Usar apenas sobre pegada já reservada ou como decoração não bloqueante. |
| CAT-P07 | Mesa funerária | 2 × 1 | 3.18 × 1.06 | produzido/integrado | Ferro forjado e madeira; sem utensílios modernos. |
| CAT-P08 | Bloco de pedra caído | 3 × 1 | 2.12 × 2.12 | produzido/integrado | Ajustar à cobertura existente; cantos legíveis. |
| CAT-P09 | Entulho de cobertura | 3 × 1 | 2.12 × 2.12 | produzido/integrado | Concentrado sobre bloqueio existente; não esconder circulação. |
| CAT-P10 | Cerâmica funerária quebrada | 3 × 1 | 1.06 × 1.06 | produzido/integrado | Decalque decorativo não bloqueante. |

## 3.5 — Marcos dos setores

| ID | Asset | Desenhos × quadros | Pegada (u) | Estado | Observações |
|---|---|---:|---|---|---|
| CAT-S01 | Marco: Vestíbulos dos Enlutados | 8 × 1 | 2.12 × 1.06 | produzido/integrado | Oito marcas de entrada, uma por antecâmara; mesma família de soleira. |
| CAT-S02 | Marco: Galeria dos Nomes Raspados | 1 × 1 | 4.24 × 3.18 | produzido/integrado | Faixas de nomes apagados. Peça visual sobre área não caminhável, sem novos bloqueios. |
| CAT-S03 | Marco: Ossário das Raízes | 1 × 1 | 4.24 × 3.18 | produzido/integrado | Raízes rompendo juntas de pedra. Peça visual sobre área não caminhável, sem novos bloqueios. |
| CAT-S04 | Marco: Claustro das Velas Mortas | 1 × 1 | 4.24 × 3.18 | produzido/integrado | Bancos laterais e pedra exposta. Peça visual sobre área não caminhável, sem novos bloqueios. |
| CAT-S05 | Marco: Cisternas do Pranto | 1 × 1 | 4.24 × 3.18 | produzido/integrado | Margens claras e marcas de inundação. Peça visual sobre área não caminhável, sem novos bloqueios. |
| CAT-S06 | Marco: Prisão dos Penitentes | 1 × 1 | 4.24 × 3.18 | produzido/integrado | Argolas, grades e correntes. Peça visual sobre área não caminhável, sem novos bloqueios. |
| CAT-S07 | Marco: Capela Invertida | 1 × 1 | 4.24 × 3.18 | produzido/integrado | Altar quebrado e relevo funerário. Peça visual sobre área não caminhável, sem novos bloqueios. |
| CAT-S08 | Marco: Câmara dos Contrapesos | 1 × 1 | 4.24 × 3.18 | produzido/integrado | Polias grandes e feixes de correntes. Peça visual sobre área não caminhável, sem novos bloqueios. |

## 3.6 — Extrações e mecanismos

| ID | Asset | Desenhos × quadros | Pegada (u) | Estado | Observações |
|---|---|---:|---|---|---|
| CAT-E01 | Escadaria e abertura superior | 1 × 1 | 3 × 3 | produzido/integrado | Continuidade visual para a superfície; degraus já representados. |
| CAT-E02 | Grade do cortejo | 1 × 8 | 3 × 0.3 | produzido/integrado | 8 quadros incluindo fechada/aberta; mesma pegada e eixo; reversível. |
| CAT-E03 | Cabrestante da escadaria | 1 × 1 | 0.8 × 0.8 | produzido/integrado | Base com manivela separada; estados derivados da grade. |
| CAT-E04 | Borda do poço | 1 × 1 | 4 × 4 | produzido/integrado | Abertura + aro; transparente no centro para compor com a plataforma. |
| CAT-E05 | Plataforma dos coveiros | 1 × 1 | 2.5 × 2.5 | produzido/integrado | Movimento pelo transform existente, sem novo atlas de subida. |
| CAT-E06 | Guincho e polia do poço | 1 × 1 | 0.8 × 0.8 | produzido/integrado | Encaixe de manivela vazio/ocupado via sobreposição E13. |
| CAT-E07 | Cordas e contrapesos | 3 × 1 | 1.06 × 3.18 | produzido/integrado | Peças separadas para compor o poço; fora dos atores. |
| CAT-E08 | Embarcadouro | 1 × 1 | 6 × 3 | produzido/integrado | Margem acessível em madeira e pedra; manter zona de embarque. |
| CAT-E09 | Barca funerária | 1 × 1 | 3 × 1.5 | produzido/integrado | Proa +X; partida pelo transform existente; sem motor. |
| CAT-E10 | Corrente da barca | 2 × 1 | 2 × 0.16 | produzido/integrado | Presa/solta; mesma origem e orientação. |
| CAT-E11 | Comporta e travamento | 1 × 8 | 0.8 × 0.8 | produzido/integrado | 8 quadros incluindo trancada/liberada; não muda tempo do servidor. |
| CAT-E12 | Passarela da cisterna | 2 × 1 | 4.24 × 8.48 | produzido/integrado | Recolhida/aberta; respeitar máscara de água e cruzamento atuais. |
| CAT-E13 | Manivela física | 1 × 1 | 0.8 × 0.8 | produzido/integrado | Peça legível no pedestal/guincho; ícone separado E15. |
| CAT-E14 | Chave física do canal | 1 × 1 | 0.8 × 0.8 | produzido/integrado | Chave grande de ferro; distinta da manivela; ícone separado E16. |
| CAT-E15 | Ícone da manivela | 1 × 1 | Preservar/UI | produzido/integrado | Fonte RGBA integral; importada com limite 128×128; mesmo significado nos dois temas. |
| CAT-E16 | Ícone da chave do canal | 1 × 1 | Preservar/UI | produzido/integrado | Fonte RGBA integral; importada com limite 128×128; mesmo significado nos dois temas. |
| CAT-E17 | Relevos de orientação das fugas | 3 × 1 | 1.06 × 1.06 | produzido/integrado | Escadaria, poço e barca; sem letras rasterizadas. |

## 3.7 — Detalhes e atmosfera

| ID | Asset | Desenhos × quadros | Pegada (u) | Estado | Observações |
|---|---|---:|---|---|---|
| CAT-D01 | Raízes de parede | 4 × 1 | 2.12 × 2.12 | produzido/integrado | Grandes bifurcações; silhueta orgânica distinta de pernas. |
| CAT-D02 | Raízes de piso | 3 × 1 | 2.12 × 1.06 | produzido/integrado | Decalque raso nas bordas; não simula obstáculo alto. |
| CAT-D03 | Musgo | 3 × 1 | 1.06 × 1.06 | produzido/integrado | Decalque irregular, sem cor luminosa. |
| CAT-D04 | Teias decorativas | 4 × 1 | 2.12 × 2.12 | produzido/integrado | Finas, estáticas, nas quinas; distintas de teia ativa. |
| CAT-D05 | Casulos decorativos | 2 × 1 | 1.06 × 1.06 | produzido/integrado | Pequenos, em nichos; sem volume de criatura viva. |
| CAT-D06 | Ossos esparsos | 3 × 1 | 1.06 × 1.06 | produzido/integrado | Sem silhueta de aranha ou brilho de loot. |
| CAT-D07 | Velas e suportes | 3 × 1 | 1.06 × 1.06 | produzido/integrado | Apagadas/acendidas/consumidas; brilho separado, sem halo no sprite. |
| CAT-D08 | Poças e umidade | 3 × 1 | 1.06 × 1.06 | produzido/integrado | Decalque de contraste baixo; não parece perigo ativo. |
| CAT-D09 | Pegadas e arrasto | 3 × 1 | 2.12 × 1.06 | produzido/integrado | Narrativa fixa; não indica posição real de jogadores. |
| CAT-D10 | Correntes e argolas | 3 × 1 | 1.06 × 1.06 | produzido/integrado | Ferro forjado; peças de borda, sem fios elétricos. |
| CAT-D11 | Tecido e estandartes | 3 × 1 | 1.06 × 2.12 | produzido/integrado | Sem símbolos de equipes; não encobre atores. |
| CAT-D12 | Cera, argamassa e detritos | 3 × 1 | 1.06 × 1.06 | produzido/integrado | Detalhe agrupado em pequenas ilhas; sem poluição do caminho. |

## Contagem e prioridade

**69 famílias novas**, **175 desenhos/sequências** e **189 imagens/quadros planejados**, sem contar recursos existentes, rotações ou instâncias. São estimativas fechadas para este lote; reaproveitamentos confirmados podem reduzir o trabalho e devem atualizar o manifesto.

P0: base modular, coberturas essenciais e extrações. P1: identidade e variações. P2: detalhes secundários e candidatos. A subetapa 3.8 revisa o conjunto e não adiciona outro lote de sprites por padrão.

Dimensões de marcos de setor são envelopes máximos de composição decorativa; a integração deve usar superfícies já não caminháveis. As animações E02/E11 têm oito quadros totais por sequência, incluindo os extremos; não são oito quadros para cada estado separado.

Os nomes de variantes seguem v01, v02 etc. Cada variante animada pode ser uma folha; o total de quadros não significa igual número de arquivos PNG.

Atualização 3.2: 40 variantes de pisos/água produzidas e integradas. Fontes e prompts em `Art/Catacumbas/Pisos`; registro em `CATACUMBAS_ETAPA_3_2.md`. Sem validação em execução.

Atualização 3.3: 32 variantes de arquitetura produzidas e integradas. CAT-A06: vão de 4,24 u com apoios nas paredes laterais existentes, pegada visual total de 6,36 u. Sem alterar colisão. Registro em `CATACUMBAS_ETAPA_3_3.md`.

Atualizacao 3.4: 28 variantes de sepulturas e coberturas produzidas e integradas. Detalhes e limites em [CATACUMBAS_ETAPA_3_4.md](CATACUMBAS_ETAPA_3_4.md); sem validacao em execucao.

Atualizacao 3.5: 15 imagens de identidade dos setores produzidas e integradas. Detalhes e limites em [CATACUMBAS_ETAPA_3_5.md](CATACUMBAS_ETAPA_3_5.md); sem validacao em execucao.

Atualizacao 3.6: 23 PNGs selecionados, 37 regioes incluindo duas animacoes de oito quadros, integrados nas extracoes e ferramentas. Cais em L por recorte; passarela recolhida na margem sul. Detalhes em [CATACUMBAS_ETAPA_3_6.md](CATACUMBAS_ETAPA_3_6.md); sem validacao em execucao.

Atualizacao 3.7: 37 variantes de detalhes e atmosfera produzidas e integradas. Montagem esparsa nas bordas, com casulos somente em nichos vazios dos ossarios. Detalhes em [CATACUMBAS_ETAPA_3_7.md](CATACUMBAS_ETAPA_3_7.md). Os lotes de producao 3.2 a 3.7 estao integrados; revisao 3.8 e avaliacao em execucao pendentes.


Atualização 3.8: produção e revisão estática concluídas, com 175 PNGs selecionados e 189 regiões de sprites. Inventário sem inconsistências de arquivos no lote selecionado; galeria consolidada em `Art/Catacumbas/Revisao/index.html`. Correção pontual para reutilizar o visual dos pickups. [Relatório e avaliação prática pendente](CATACUMBAS_ETAPA_3_8.md). Sem testes, Unity ou builds nesta etapa.
