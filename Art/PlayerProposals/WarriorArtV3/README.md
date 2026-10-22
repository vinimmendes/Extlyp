# Guerreiro — preparação da arte v3

Etapas 1–8 executadas. Etapas 3–8 em arquivos incrementais separados: veja os registros `ETAPA_03_ARMADURA.md` até `ETAPA_08_ILUMINACAO.md`. Etapa 8 aguarda avaliação artística do usuário. A cópia inicial permanece sem alterações visuais.

## Arquivos

- `baseline-v2/warrior_v2_bundle-original.zip`: cópia integral do ZIP recebido, com SHA-256 conferido.
- `baseline-v2/warrior_v2.blend`: original extraído para comparação; preservar sem editar.
- `baseline-v2/`: imagens, atlas, quadros, scripts e relatórios fornecidos pelo outro chat. Scripts não executados. Skills internas continuam apenas no ZIP original, sem instalação.
- `working-v3/warrior_v3.blend`: cópia inicialmente idêntica ao `.blend` v2; aberta no Blender com execução de scripts desativada, sem salvar conversão de versão.
- `references/`: referência artística principal, protagonista original como apoio de identidade/câmera, comparação fornecida pelo pacote.
- `preservation-manifest.json`: hashes do ZIP original, da cópia e dos arquivos `.blend`.
- `baseline-scene-settings.json`: leitura direta de câmeras, luzes, materiais, rig, Actions, saída e gerenciamento de cor do arquivo recebido.
- `DIRECAO_VISUAL_V3.md`: alvo visual e critérios para as próximas etapas.

## Estado inicial registrado

Leitura com Blender 5.2.2 LTS pelo MCP oficial. O README recebido informa autoria em Blender 4.3.2; a abertura atual não comprova equivalência de renderização entre versões.

- Câmera ativa: `CAM-game-topdown`; três câmeras ortográficas no total.
- Três objetos de luz e 11 materiais.
- Rig: `ARM-warrior-3D`, 23 ossos; Action ativa `Walk_Heavy_36f_IK`.
- Intervalo 1–36, 30 fps; quadros de 640 × 640.
- 148 objetos de malha na leitura direta. O relatório recebido menciona 146; diferença apenas registrada, sem diagnóstico ou correção nesta etapa.
- O caminho de renderização gravado no arquivo é proveniente do ambiente do outro chat. Deve ser redirecionado à pasta v3 antes de qualquer futura renderização; não executar os scripts originais sem adaptar seus caminhos e revisar efeitos.

O registro é um inventário de preparação, não um teste de movimento, colisão, compatibilidade de Actions ou renderização. Relatórios em `baseline-v2/validation` são evidências históricas fornecidas, não verificações executadas aqui.

## Continuação

Arquivo atual: `working-v3/warrior_v3-step08-lighting.blend`. Comparação: `stage08-approval/comparacao-v2-v3-quadro01.png`. Após avaliação artística: etapa 9, revisão visual durante o ciclo; depois etapa 10, entrega. Manter esta base preservada e criar salvamentos incrementais na pasta de trabalho.

Nenhum script do pacote foi executado; não houve reprodução do ciclo, teste Unity ou build. A etapa 8 produziu os renders estáticos de apresentação descritos acima.
