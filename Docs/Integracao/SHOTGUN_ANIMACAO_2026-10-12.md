# Troca e pose de shotgun — 12/10/2026

Animação aprovada conectada a PlayerCharacterVisual. Ao mudar da pistola para a shotgun durante a partida, reproduz 32 frames a 24 fps (1,33 s). Outras origens usam somente os frames 11–32, sem mostrar uma pistola que não estava equipada. Personagem já nascendo com shotgun usa diretamente a pose, sem reproduzir uma troca fictícia.

Durante a transição, SpriteRenderer principal desenha o ciclo das pernas, e o filho WeaponSwitchUpperBody desenha tronco/braços/armas. Pernas só avançam por deslocamento real; parado usa frame 9. Quando termina, volta ao atlas completo Shotgun na mesma fase de caminhada. Morte, fim de partida, teleporte, desativação e troca para outra arma cancelam a transição. VisibilityTarget e DamageFlash continuam responsáveis pela visibilidade e pelo dano; o filho fica inativo fora da troca.

Assets em Resources/PlayerCharacter:

- Shotgun.png: pose e ciclo de caminhada rerenderizados com a empunhadura aprovada no Blender. GUID original preservado.
- SwitchShotgun.png: 32 frames só do tronco da transição.
- Legs.png: ciclo de pernas separado, usando o rig aprovado.

Todos são 2048 x 1024, 8 x 4 células de 256 pixels, alpha transparente, sem mipmaps e sem compressão. Cache compartilhado inclui os novos atlases.

Fontes em Prototypes/PlayerWalk3D: export_shotgun_runtime.py e package_shotgun_runtime.py. Modelo editável e prévia aprovados continuam em ShotgunSwitch. Exportação Blender concluída e pose final inspecionada visualmente. Revisão estática e git diff --check; sem testes Unity, builds ou reinício de servidor.

Integração de apresentação a partir do equipamento já replicado, sem novas variáveis de rede; protocolo permanece 17. A equipagem, cadência, dano, recarga e disponibilidade de disparo continuam com suas regras atuais: a duração visual não adiciona bloqueio de tiro no servidor. Não foi implementada animação inversa para guardar a shotgun, nem uma transição própria a partir do rifle.
