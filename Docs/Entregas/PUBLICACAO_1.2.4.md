# Publicação 1.2.4 — 24/10/2026

Entrega solicitada do estado completo do projeto. Revisão dos binários: `d0da772806d4178af4f71fa93bae97c4754a67d7`, árvore `b56fdd8a78d4d1f3eae37180e81a24b8aee272fb`, idêntica à revisão `bfa59082` usada no APK. Alterações posteriores em andamento não integram os binários publicados.

## Servidor estável

Promovido em 24/10/2026 às 14:43 (Brasília), versão 1.2.4, protocolo 45. Três salas sinalizaram prontidão em UDP 7777, 7778 e 7779; os processos foram conferidos após a promoção. Oito perfis antes e depois, com as mesmas identidades. Desenvolvimento permaneceu separado.

- Backup de dados: `Backups/Stable-before-1.2.4/Camps`.
- SHA-256 do JSON preservado: `7714AD7A530F952F9CA094302A107BB76B3FC2FD220BD738A54F809C1C1C383A`.
- Executável anterior preservado em `Builds/ServerReleases/20261024-174315-a459b0d4-previous`.
- Candidato congelado: `Builds/Releases/1.2.4/ServerCandidate`.
- Evidência local: `Builds/Releases/1.2.4/stable-promotion.json`.

O candidato isolado foi comparado com 14.692 arquivos da revisão Git, incluindo conteúdo LFS: nenhuma divergência de código/assets ou metadados importados; somente PlayerSettings configurado para o build. `Tools/Verify-ReleaseSource.py` registra essas categorias separadamente. `Publish-Server.ps1 -CandidateDirectory` permite promover esse candidato sem depender das edições simultâneas no diretório de desenvolvimento.

## Android e GitHub

[Release v1.2.4](https://github.com/vinimmendes/Extlyp/releases/tag/v1.2.4) · [APK](https://github.com/vinimmendes/Extlyp/releases/download/v1.2.4/CyberExtraction-1.2.4.apk)

APK `com.cyberextraction.game`, versão 1.2.4, versionCode 7, ARM64, 517.017.375 bytes. SHA-256: `0FF9D434D15FDF1E04995011ABA7A166BE700C42AD753B36D8F2D644AF0016EA`.

Assinatura conferida para Android 9 e comparada com a versão 1.2.3 publicada. Certificado SHA-256: `515189bd9d464cd5317a6baf976bd8c77d986ef2e7e44ed913e3f6d98033bf7b`. Instalar sobre a versão anterior, sem desinstalar. Clientes anteriores ao protocolo 45 precisam ser atualizados.

Compilações de entrega, integridade, assinatura e prontidão local foram conferidas. Não houve teste no aparelho, de jogabilidade ou de conexão externa; não foram executadas suítes automatizadas ou Play Mode. A avaliação prática permanece com o usuário.
