# Pesca — estudo de movimento v01

Prévia produzida no Blender 5.2.2 LTS, câmera ortográfica, 960 × 540, 96 quadros a 12 fps (8 segundos).

Sequência: espera → beliscada → mordida/submersão → disputa → retorno à espera. O retorno e os valores dos medidores são demonstrativos, sem simulação de gameplay. Esta é uma prova de movimento, não aprovação da HUD final, arte final ou validação Unity.

- `fishing-preview-v01.blend`: fonte editável; cena `Fishing-Preview-v01c`. A cena original foi preservada. As cenas vazias v01/v01b registram duas tentativas de configuração interrompidas antes de produzir objetos.
- `fishing-preview-v01.gif`: reprodução dos quadros realmente renderizados no Blender.
- `fishing-preview-poses.png`: quatro momentos do ciclo.
- `frames/`: quadros PNG originais.

Autoria: `Tools/Blender/FishingAnimationPreview.py`; montagem da prévia: `Tools/Blender/PackageFishingPreview.py`. Não executar novamente o autor na mesma sessão: ele recusa substituir a cena existente.

Proporções de estudo: peixe de aproximadamente 35 cm e boia de aproximadamente 5 cm, ampliados uniformemente para leitura no painel; não são assets dimensionados para o mapa. Água estilizada por material procedural; peixe, cauda, boia, linha e ondas são camadas separadas. Na integração, a linha e os sinais de gameplay precisam responder ao servidor, sem reproduzir o ciclo inteiro como um vídeo fixo.

Referências: direção Sombria de `Docs/UI/DIRECAO_DE_ARTE_HUD.md` e proposta de composição simplificada desta conversa. Não foram usados assets externos. Nenhuma alteração no minigame Unity e nenhum teste ou build Unity executado.
