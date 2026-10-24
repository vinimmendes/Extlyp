# Área de testes restrita à rede local — 22/10/2026

## Regra solicitada

O usuário confirmou que outras pessoas na sua rede também podem acessar os testes. A permissão é por origem de rede, sem cadastro de proprietário, senha compartilhada ou mudança da identidade do perfil.

## Implementação

- Na abertura normal, o cliente faz uma consulta assinada ao servidor antes de apresentar a escolha de ambiente. Essa consulta é recusada intencionalmente pela aprovação NGO com uma resposta de permissão; não cria perfil, lease, participante ou recursos de teste.
- Quando autorizado, o jogador pode escolher a base normal ou o acampamento de testes. Quando não autorizado ou sem resposta, o cliente segue para a base normal. Durante a consulta, a interface mostra que está verificando o acesso.
- O servidor decide pela origem real obtida em `UnityTransport.GetEndpoint`, na versão instalada do NGO 2.13.3. O IP declarado pelo cliente não concede permissão.
- São aceitos loopback e IPv4 privado que pertença à mesma sub-rede de uma interface Ethernet/Wi-Fi ativa do servidor, usando sua máscara real. Não basta o endereço começar com 192.168, 10 ou 172. IPv6 não loopback e origens desconhecidas são recusados nesta versão, consistente com o servidor atual escutando em IPv4.
- Conexões de teste são verificadas novamente na aprovação, independentemente da consulta inicial ou do menu. Comandos do laboratório e leitura/gravação do acampamento sandbox também exigem acesso local. Perder essa autorização não converte uma operação sandbox em alteração do perfil real.
- Builds de distribuição podem oferecer os testes aos clientes locais. Editor, development build ou `-allow-test` não dispensam a verificação de rede. O argumento `-disable-test` permite desativar a funcionalidade no servidor.
- A opção temporária de reiniciar o servidor conserva sua condição anterior de desenvolvimento e suas restrições de processo local.
- Catacumbas permanece como mapa inicial. Credenciais e progresso existentes não são alterados.

## Endereço público e roteamento

O Android está configurado para conectar no IP público do servidor. A consulta não compara esse destino com o IP do celular: o servidor avalia a origem que efetivamente recebeu. Em acesso pelo IP público dentro do Wi-Fi, isso depende do NAT loopback do roteador. Se a origem chegar como pública, o acesso aos testes será recusado; nesse caso, usar o endereço LAN do servidor para a conexão local. Não foi introduzida descoberta automática do servidor.

A regra pressupõe conexão UTP direta, como no projeto atual. Um proxy, VPN que apareça como Ethernet local ou roteador que substitua indistintamente a origem externa por um IP da LAN muda essa fronteira: o servidor só conhece a origem recebida. Não equivale a autenticação pessoal do proprietário.

## Compatibilidade e avaliação

O campo de consulta faz parte da assinatura do login. O protocolo compartilhado passou de 41 para 42: cliente e servidor precisam ser entregues juntos. Nenhum executável ou APK foi gerado, servidor reiniciado, teste automatizado ou sessão Unity iniciada nesta alteração. Foram revisados estaticamente os fluxos e as APIs nos fontes locais dos pacotes instalados.

Primeira avaliação prática: PC no servidor por loopback; celular no Wi-Fi por IP local; celular no mesmo Wi-Fi por IP público; outro aparelho na LAN; celular em dados móveis; servidor indisponível; tentativa externa de sandbox/QuickTest; laboratório local; retorno à base; sandbox sem alterar inventário real. Conferir a máscara do servidor se a rede tiver segmentos Wi-Fi isolados ou VLANs.
