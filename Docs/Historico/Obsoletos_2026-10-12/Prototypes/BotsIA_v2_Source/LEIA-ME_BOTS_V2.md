# Bots v2 - como aplicar

Copie por cima do projeto (mesmos caminhos):
- Assets/Scripts/Bots/BotDifficulty.cs
- Assets/Scripts/Bots/BotController.cs
- Assets/Editor/BotChecks.cs
- BOTS_SETUP.md

Nenhum outro arquivo foi alterado. A API usada por LootPickup, MatchManager, ExtractionZone e ServerSmokeTest
(BotController.InVision, BotDecision.CanExtract, BotDifficulty.Easy/Medium/Hard) continua igual.

Depois de recompilar:
1. Menu Cyber Extraction > Verificar regras dos bots (deve mostrar 76 verificacoes).
2. Teste cada nivel trocando `botDifficulty` no MatchManager do prefab (como antes), gere o servidor e observe.
3. Ajuste os numeros em BotProfile.For (BotDifficulty.cs). Cada campo tem comentario.

O que observar em jogo, por nivel:
- Rotas: o bot deve seguir para uma ruina, coletar os dois pontos e so entao partir para outra, sem zigue-zague.
- Extracao: perto do fim ele deve estar a ~10 m da zona e entrar quando faltarem 60 s.
- Luta: Iniciante atira andando e erra muito; Mediano para para atirar; Experiente atira em rajadas e se desloca entre elas.
