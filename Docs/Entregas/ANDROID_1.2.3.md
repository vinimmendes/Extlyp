# Android 1.2.3 — 22/10/2026

[Baixar APK 1.2.3](https://github.com/vinimmendes/Extlyp/releases/download/v1.2.3/CyberExtraction-1.2.3.apk).

Corrige o armazenamento de identidade por celular e oferece separação confirmada quando dois aparelhos usam a mesma chave.

## Testar em dois celulares

1. Instale a atualização por cima do aplicativo atual em ambos os celulares; não desinstale. Deixe pelo menos 2 GB livres durante a instalação.
2. No primeiro celular, continue com o perfil atual.
3. No segundo, caso apareça “Seu acampamento já está conectado”, toque **Criar perfil neste celular** e depois **Confirmar novo perfil**. Esse aparelho começará um perfil novo; o anterior permanece salvo e sua credencial é arquivada localmente. **Usar perfil atual** tenta reconectar sem trocar a identidade.
4. Confira se os dois conseguem entrar na base e manter progresso separado após reiniciar.
5. Para entrar na mesma expedição multiplayer, escolham **Cabana normal**, o mesmo destino e iniciem a busca dentro da janela de 30 segundos. O ambiente de testes mantém a regra anterior de partida individual.

## Entrega e limites

- Android 6 ou superior, IL2CPP/ARM64, Unity 6000.0.84f1.
- Versão 1.2.3, código Android 6, protocolo 41. Não exige mudar o protocolo do servidor.
- Chave migrada sem mudar o perfil existente para o diretório privado sem backup do Android.
- Bloqueio de sessão duplicada mantido. O novo perfil é criado somente após confirmação.
- Inclui todas as opções e categorias da [versão 1.2.2](ANDROID_1.2.2.md).
- Compilação e assinatura conferidas; assinatura igual à versão anterior e verificação compatível com Android 9.
- SHA-256: `D372E1BC77227E4CC7E5716F1F01C1C0729939530C1CC17EE5DEEC3442828317`.
- 13 verificações focadas da identidade passaram, com falha esperada reproduzida na baseline. [Escopo da correção e das provas](../Integracao/IDENTIDADE_POR_CELULAR_2026-10-22.md).
- Ainda falta validar JNI/armazenamento Android real, o fluxo visual de confirmação e dois celulares conectados. A origem da cópia da identidade nos aparelhos não foi confirmada.
