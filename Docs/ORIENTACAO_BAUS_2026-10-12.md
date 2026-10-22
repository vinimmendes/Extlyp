# Orientação dos baús

Baús comuns de loot calculam sua orientação no servidor durante o spawn, depois de inicializar o tipo da instalação. A direção é replicada por `LootPickup.ChestFacing`; clientes que entram depois ou voltam a enxergar o objeto recebem a mesma orientação. O transform é orientado antes de construir a aparência, mantendo a orientação usada pelas verificações existentes de visibilidade.

Uma parede ou cobertura sólida próxima, nas camadas de obstáculos existentes, é considerada apoio. O baú vira para a direção que se afasta do ponto mais próximo desse apoio. O alcance corresponde à meia profundidade do baú (0,6 unidade) mais 0,2 de tolerância; obstáculos do outro lado de um corredor não são tratados como apoio. Triggers, os colliders do próprio baú e corpos móveis não definem sua orientação. Em empate entre apoios, a seleção usa uma ordem estável. Quando não há apoio, a frente fica para o sul.

Armários, gabinetes e portas preservam suas orientações autoradas. Não houve mudança em posições de spawn, conteúdo de loot, duração de abertura ou regras de interação. Cena e prefabs não foram reconstruídos.

Revisão estática: apoio ao norte → frente sul; ao sul → norte; a leste → oeste; a oeste → leste; sem apoio → sul. Não foram executados testes Unity ou clientes separados. Conferir esses casos na próxima validação prática, inclusive uma parede inclinada e a entrada posterior de um cliente.
