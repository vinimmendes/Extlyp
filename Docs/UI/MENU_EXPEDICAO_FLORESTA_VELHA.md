# Menu de expedição — Floresta Velha

Implementação de 11/10/2026. Referências: preparação de partida do [Hunt: Showdown](https://www.huntshowdown.com/player-guide/en_US) e composição atmosférica do [menu de Darkwood](https://www.mobygames.com/game/82245/darkwood/screenshots/). Interface própria, seguindo `DIRECAO_DE_ARTE_HUD.md`.

`ExpeditionMenu` apresenta a escolha inicial e o menu do portão; `NetworkBootstrap` continua responsável por aprovação, conexão e busca nas três salas. O painel horizontal tem até 960 × 460 unidades virtuais, centralizado na área segura, com apresentação pelos temas Simples/Sombria de `HudTheme`. A imagem reutiliza `Docs/Auditoria/Evidencias/MapaLaboratorio/floresta.png`, sem geração nova ou alteração da captura original.

- Cabana normal: somente Floresta Velha, mochila individual, Partir e Voltar à cabana. Mapa grande e modo normal são impostos também no pedido de conexão; o servidor recusa mapa pequeno fora de testes.
- Acampamento de teste: Área de testes ou Floresta Velha — teste, seletor de bots e ferramentas temporárias. Disponibilidade exige confirmação `CampSnapshot.TestMode` na base ou `MatchManager.Sandbox` na expedição, além da autorização existente do servidor.
- Recursos, reposição, reset, laboratório, ajuda para F2 e reinício local do servidor ficam limitados ao ambiente de teste. A persistência real e os perfis temporários continuam separados pelos mecanismos existentes; não há migração de perfis ou alteração do formato das mensagens.
- Preparar mochila fecha o menu e abre somente a mochila. Ao fechar essa janela, o menu retorna. A conexão bloqueia novos pedidos; a busca oferece Cancelar e falhas oferecem Tentar novamente/Voltar.
- A escolha de ambiente é mantida durante recarregamentos e reconexões na mesma execução. Sair pelo menu limpa a escolha. Editor, Development Build ou `-allow-test` habilitam a escolha inicial; versões sem essa autorização entram na cabana normal diretamente.

`Assets/Resources/ServerConnectionSettings.asset` configura endereço e porta base: `127.0.0.1`, `7777`; as salas seguintes usam `7778`/`7779`. A preferência `server_ip` não é lida nem escrita. Preferências antigas de mapa/laboratório não influenciam partidas normais; escolhas de teste usam somente `test_expedition_map` e `test_quick_bots`. Endereço externo/local remoto, descoberta, serviços online e roteador ficam adiados.

Revisão estática realizada. Nenhum teste automatizado, sessão Unity ou build foi iniciado nesta etapa. A validação prática inicial pertence ao usuário: escolha dos ambientes, atalhos, mochila/retorno, cancelamento/nova tentativa, temas, área segura em celular horizontal e navegação por teclado/gamepad. A captura reutilizada é uma evidência antiga do cenário, não uma validação visual desta implementação.
