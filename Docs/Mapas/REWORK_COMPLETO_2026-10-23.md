# Catacumbas — expansão da direção visual aprovada

> Estado de implementação atualizado em [IMPLEMENTACAO_E_ESTADO.md](ReworkCompleto/IMPLEMENTACAO_E_ESTADO.md). O texto abaixo preserva o planejamento original da etapa de conceitos. As oito pranchas foram aprovadas posteriormente; editor, dados compartilhados, kit e montagens nativas foram produzidos e publicados localmente após a revisão estática das 24 salas. A primeira avaliação prática continua pendente do usuário.

## Autorização e direção

Em 23/10/2026 o usuário solicitou aplicar a ideia e o conceito ao restante do mapa. Esta solicitação libera a expansão além da R17. Aplicar a linguagem e o processo aprovados a composições próprias das 24 salas, corredores e margens; o acampamento abandonado continua específico do ossuário inferior.

Referências: Darkwood para matéria, abandono e narrativa ambiental; Bullet Echo para leitura de combate e circulação. Preservar Unity 6000.0.84f1/URP 2D, NGO/Transport, geometria e regras autoritativas. Sem MvpSetup.Build, sessões Unity, testes automatizados ou builds.

## Ideias por setor, antes da montagem

| Setor | Ideia que organiza as salas | Relações da composição |
|---|---|---|
| Vestíbulos | O primeiro contato com a função da ala | Marca funerária junto à parede, vestígios no seu pé e centro de entrada livre. Urnas, grades, raízes ou cera antecipam a ala correspondente. |
| Galeria dos nomes | Uma memória organizada que foi sendo apagada | Ritmo de nichos e placas, interrupções localizadas, lascas diretamente sob o dano e piso de passagem contínuo. |
| Ossários | Os restos ainda revelam a ordem de quem os guardou | Nichos preenchidos, recipientes nas bases existentes, raízes entrando por juntas. R17 acrescenta abrigo abandonado apoiado no recipiente sudoeste e urnas quebráveis no sudeste. |
| Claustro | O ritual cessou, mas a disposição de quem esperava permanece | Bancos e suportes de velas nas bordas, tecido e cera ligados a esses apoios, centro amplo e silencioso. |
| Cisternas | A água deixou registro de níveis e correntes | Pedra úmida e sedimentos em continuidade com margens e entradas de água; madeira encalhada acompanha a direção do escoamento. Sem poças demonstrativas nas alas secas. |
| Prisão | Os ocupantes se foram, o confinamento ainda pode ser lido | Grades em alvenaria já bloqueada, argolas e correntes relacionadas a elas, tigela e palha junto ao lugar de permanência. |
| Capela | Uma cerimônia interrompida por deterioração e saque | Altar partido sobre cobertura existente, desenho de piso aponta sua orientação; tecido e fragmentos próximos à origem, sem ocupar o eixo de combate. |
| Contrapesos | O trabalho parou antes de ficar pronto | Bancada, madeira preparada e ferragens pertencem à mesma operação; ferramentas e fragmentos ficam junto ao apoio, mecanismo real permanece reconhecível e livre. |

As fichas individuais partem de `ReworkPiloto/SALAS.md`. Gerar e apresentar uma galeria de conceitos executáveis com as peças efetivas antes de ativar o conjunto completo. Não depender de luzes ou geometria inexistentes no jogo.

## Implementação escolhida

- Novo pacote versionado separado do piloto e dos assets anteriores; o piloto permanece como opção de retorno.
- Paredes aprovadas com as 47 vizinhanças e quatro acabamentos, compartilhadas por todo o mapa. Soleiras e acabamentos seguem os vãos reais nas quatro orientações; nenhuma passagem visual nova.
- Seis famílias reutilizáveis de piso (lajes, ornamental, destruído, terra, pedra/terra e raízes), centros variados e transições por vizinhança. Um acabamento úmido adicional se limita às áreas coerentes com água. Usar camadas de terra/raízes relacionadas para evitar linhas de pedra artificiais entre elas.
- Composições autoradas com ocupação e reservas comuns: volumes sobre pedra/cobertura existente, vestígios baixos no piso, atores/interações/acessos livres. Não sortear decoração independentemente em cada cliente.
- Manter água funcional, margens, mecanismos e animações de extração existentes. Preservar os IDs e as regras das urnas quebráveis já agrupadas na R17.
- Empacotamento compartilhado de tiles e sprites, sem criar uma textura única de tamanho de sala para cada cômodo. Sem promessa de desempenho não medido.
- Leito do acampamento com dimensão próxima de 2,0 × 0,8 unidade, conferida contra a grade de 1,06 e o personagem; utensílios proporcionais. A cama grande do conceito não deve ser copiada literalmente.

## Etapas e evidência

1. Ideias e limites registrados neste documento.
2. Conceitos pintados por setor sobre bases de geometria real, apresentados antes de produzir o kit completo. [Ideias individuais das 24 salas](ReworkCompleto/IDEIAS_DAS_SALAS.md).
3. Após a direção visual, produzir fontes e kit modular; inspecionar recortes, alpha, atlas e proporções e apresentar montagens com as peças efetivas antes da ativação.
4. Integração de apresentação, preservação dos ajustes do editor e documentação de omissões/fallbacks.
5. Revisão estática e inspeção das imagens. Avaliação prática pelo usuário; não apresentar montagem estática como execução Unity.

Estado atual: ideias das 24 salas registradas; oito bases extraídas da geometria atual; oito conceitos pintados e revisados, reunidos na [galeria V1](ReworkCompleto/GALERIA_CONCEITOS_V1.md). Integração do restante do mapa ainda não iniciada. O reforço do usuário foi explícito: ideia, depois conceito. Esta entrega desenvolve essa etapa; não representa aprovação das novas salas nem validação em Unity.
