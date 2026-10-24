# Android 1.2.4 — 24/10/2026

Entrega do estado atual completo do projeto, autorizada pelo usuário. APK `com.cyberextraction.game`, versão 1.2.4, código Android 7, IL2CPP/ARM64, Unity 6000.0.84f1. Cliente e servidor usam protocolo **45**; versões anteriores são incompatíveis.

## Alterações incluídas

- Pesca com isca Minhoca, visitantes que investigam e podem recusar, fisgada manual e faixa de captura horizontal. Consumo autoritativo da isca na mordida. [Detalhes e parâmetros](../UI/PESCA_ISCA_E_FAIXA_HORIZONTAL.md).
- Estado atual dos mapas, ferramentas de autoria, perfil/estatísticas, inventário, interface e infraestrutura de servidores que estava no workspace. A publicação não representa nova aprovação da fidelidade visual das Catacumbas.
- Canais estável e desenvolvimento separados. Promoção da estável feita somente com as salas desligadas, preservando a versão anterior e os dados da base.

## Instalação e compatibilidade

Instale por cima do APK 1.2.3 publicado no GitHub, sem desinstalar. A assinatura precisa coincidir com a distribuição anterior para preservar a atualização e a identidade local. O APK aponta para o endereço Android configurado no build e para UDP 7777–7779. O IP configurado não comprova alcance por dados móveis.

Builds, hashes, assinatura e resultado da promoção são registrados nos artefatos da release. O APK temporário local de pacote `com.DefaultCompany.Myproject` é outra aplicação e não serve como referência de atualização do pacote distribuído.

## Limites

Compilação de entrega e verificações de integridade/assinatura não validam jogabilidade, toque, fidelidade da HUD, rede externa ou balanceamento da pesca. Não foram solicitadas suítes automatizadas ou Play Mode. A primeira avaliação prática continua com o usuário.
