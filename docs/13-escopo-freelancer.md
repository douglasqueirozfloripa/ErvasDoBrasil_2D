# Escopo para o(a) freelancer Unity 2D


O protótipo já tem as regras, os dados, os testes e um mundo jogável com UI provisória (IMGUI). Entregas sugeridas, em ordem:

| # | Entrega | Critério de aceite |
|---|---|---|
| 1 | **Tilemap do mundo** (horta, vila, mata, casa da vó) substituindo o mapa único | colisões com cercas, árvores e casas; mesma posição das estações |
| 2 | **Animações** (Animator) da raizeira com a sprite sheet atual + ações (colher, pilar, regar) | 4 direções, idle, transições suaves |
| 3 | **UI final** em UI Toolkit ou uGUI: HUD, Bolsa, Livro (visual do mockup), Caldeirão, Feira | mantém as chamadas à `GameSession`; funciona com toque no iPhone e com mouse/teclado no Mac |
| 4 | **Menus na UI final**: menu inicial, Menu da partida e lista de jogos salvos (o save já existe: `SaveGame` + `SaveStore`) | mesmas chamadas do `GameManager` (`NewGame`, `LoadGame`, `SaveNow`, `BackToTitle`) |
| 5 | **Áudio**: SFX de colheita, caldeirão e espíritos (a trilha por ambiente já existe, ver [14-trilha-sonora.md](14-trilha-sonora.md)) | mixer com volume separado de música e efeitos (o volume geral do jogo e o mudo já existem: `GameAudio`, controle no rodapé) |
| 6 | **Diálogos** dos aldeões e da Dona Cida (`DialogueManager`) | textos em JSON |
| 7 | **Partículas e efeitos**: fumaça da defumação, brilho das auras, redemoinho do Saci | performance estável a 60 fps no simulador |

**Regras para PRs:** toda regra nova entra em `Scripts/Core` com teste em `Tests/EditMode`, e `./scripts/run-tests.sh` precisa passar.
