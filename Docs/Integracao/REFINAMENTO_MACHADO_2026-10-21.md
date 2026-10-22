# Refinamento visual do machado — 21/10/2026

Substituída a arma provisória do ataque por 26 renders de um modelo com cabeça curva de um lado,
fio chanfrado, encaixe de ferro, cabo oval de madeira e couro escuro. Referência: ícone de inventário
existente, preservado. Fonte, renders e instruções em `Prototypes/PlayerWalk3D/AxeRefined/README.md`.

## Integração

- `PlayerMeleeActionArt` aceita atlas opcional da arma por ação e retorna corpo/arma usando
  o mesmo índice de quadro. O machado usa `AxeWeapon`, com enquadramento de 5,1 unidades por célula.
- `PlayerActionArt.Get` aceita enquadramento por atlas e carregamento opcional. Demais chamadas
  mantêm o padrão de 3,4 unidades e suas mensagens de erro. O cache distingue enquadramentos.
- `MeleeWeaponVisual` aplica os quadros renderizados com transformação local identidade;
  preserva material, cor, camada, neblina, arco de alcance e mãos à frente da arma.
- Se faltar o atlas opcional, permanece a apresentação anterior com `AxeGrip`. Espadas e lança
  não recebem atlas de arma novo nem alterações nas suas transformações.
- `ItemVisualCatalog` recebe somente o `WorldIcon` do machado. `InterfaceIcon` é preservado.
  O sprite de chão usa o caminho existente de apresentação do loot, sem alterar sua lógica.
- Animação do corpo, empunhadura revisada, velocidade de caminhada, timings, dano, stamina,
  alcance, RPCs e regras de combate não foram alterados.

## Evidência e avaliação pendente

Modelo e poses renderizadas inspecionados; cabeça, fio e cabo legíveis, mãos nos contatos
existentes e retorno à guarda. Código revisado estaticamente. Sem testes automatizados,
sessões Unity ou builds, conforme preferência do projeto; não validado em execução.

Na primeira avaliação prática pelo usuário: atacar parado/andando, observar outro jogador,
conferir mãos e arma durante recuo/impacto, neblina, aparência do machado no chão e permanência
do ícone de inventário. Conferir também a troca para espada e lança.
