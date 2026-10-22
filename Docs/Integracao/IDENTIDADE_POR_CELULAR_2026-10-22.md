# Identidade separada por celular — 22/10/2026

## Problema e evidência

O usuário relatou que o segundo celular recebe **Seu acampamento já está conectado**. Essa mensagem vem da rejeição `DUPLICATE` quando a mesma identidade já tem uma sessão. Não é evidência de uma sala normal limitada a um jogador.

`CampIdentity` usa uma chave RSA local e deriva o perfil da chave pública. A versão anterior guardava a chave privada em `PlayerPrefs`, que no Android usa preferências potencialmente copiadas por backup/restauração. O projeto não excluía essa credencial dos backups. Copiar a chave reproduz uma identidade igual; a origem da duplicação nos celulares do usuário ainda não foi confirmada.

## Correção

- Android migra a chave existente para `Context.getNoBackupFilesDir()/camp-identity-v2.json`. A migração mantém a identidade e o progresso atual e só remove o valor legado de PlayerPrefs após gravar o arquivo.
- Arquivos nesse diretório são excluídos de backup/transferência pelas regras do Android: [Auto Backup](https://developer.android.com/identity/data/autobackup).
- Ao receber `DUPLICATE`, a base oferece **Criar perfil neste celular**. O primeiro toque mostra a explicação; **Confirmar novo perfil** arquiva a credencial anterior, gera uma nova chave RSA e reconecta. **Usar perfil atual** tenta reconectar sem trocar a chave.
- O perfil anterior permanece no servidor, sem transferir ou duplicar itens para o novo perfil. A credencial anterior fica em `identity-archive` dentro do diretório privado sem backup do Android, para recuperação técnica; não é exportada nem registrada nos logs.
- Não há troca automática da identidade apenas por uma rejeição: uma sessão legítima pendente também pode gerar `DUPLICATE`.
- A gravação da nova chave usa arquivo temporário e substituição. Uma falha não troca a chave em memória; o arquivo anterior e seu arquivo de recuperação permanecem disponíveis.
- Windows e argumentos de identidade das ferramentas preservam o fluxo existente. O servidor mantém autenticação e bloqueio de sessão duplicada. Não muda o protocolo ou exige serviço online adicional.

## Verificação focada

`Tools/Audit/Run-CampIdentityDeviceProbe.ps1` compila a regra C# real com adaptadores explícitos de Android/PlayerPrefs/JsonUtility e do DTO de login.

A baseline `2d3a57c` falhou na expectativa de remoção da chave das preferências copiáveis. O código corrigido passou 13 expectativas: migração sem perder perfil, armazenamento sem backup, persistência após reinício, reprodução da duplicação por chave copiada, nova identidade após confirmação, arquivo da chave anterior, assinatura válida, perfil novo em outro armazenamento e preservação de identidade em falha de escrita.

Isso não valida backup real do Android, serialização Unity, JNI, layout, transporte ou dois celulares conectados. A avaliação prática deve manter o primeiro celular no perfil atual e usar Criar perfil neste celular somente no segundo, confirmando inventários/progresso separados e reconexão após reiniciar ambos. O modo de testes continua com sua regra atual de um jogador por partida; usar o modo normal para comparar a mesma expedição multiplayer.
