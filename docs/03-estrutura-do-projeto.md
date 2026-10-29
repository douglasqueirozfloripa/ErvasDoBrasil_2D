# Estrutura do projeto

```
ErvasDoBrasil_2D/
├── Assets/
│   ├── Art/
│   │   ├── Characters/      # raizeira, aldeões e assombrações
│   │   ├── Environment/     # mapa do sítio + casa, caldeirão, varal, pilão, feira
│   │   ├── Plants/          # plant_<id>_0..4 (estágios de crescimento)
│   │   └── UI/Icons/        # ícones dos 28 preparos
│   ├── Audio/Resources/Music/ # trilha_dia · trilha_feira · trilha_noite (.ogg)
│   ├── Icon/                # app_icon.png (iOS) e app_icon_macos.png (Mac, squircle): a Vó benzedeira, fora de Art/ para não virar sprite
│   ├── Prefabs/             # (vazio) o protótipo cria tudo em código
│   ├── Resources/Data/      # plants.json · recipes.json
│   ├── Scenes/              # Main.unity (criada pelo menu "Ervas do Brasil")
│   ├── ScriptableObjects/
│   │   └── Resources/       # ArtLibrary.asset (gerada automaticamente)
│   ├── Scripts/
│   │   ├── Core/            # ⭐ regras puras (asmdef ErvasDoBrasil.Core, sem UnityEngine)
│   │   ├── Farming/         # FarmPlot.cs, WildHerb.cs
│   │   ├── Inventory/       # InventoryWindow.cs
│   │   ├── Crafting/        # CraftingWindows.cs (Caldeirão, Varal, Pilão)
│   │   ├── Player/          # PlayerController.cs, InputCompat.cs
│   │   ├── Spirits/         # Spirit.cs, ProtectionAura.cs
│   │   ├── UI/              # GameUI.cs, BookWindow.cs, ShopWindows.cs, MenuWindows.cs, DevWindow.cs (Bastidores), UIStyles.cs, UiAutomation.cs (botões clicáveis pelos testes)
│   │   ├── World/           # GameManager.cs, SaveStore.cs, MusicManager.cs, GameAudio.cs, ArtLibrary.cs, SpriteFactory.cs, CameraFollow.cs, Interactable.cs, LifecycleLog.cs, Privacy.cs
│   │   └── Editor/          # ArtPipeline.cs, BuildTools.cs, SecurityGate.cs (asmdef só-Editor)
│   └── Tests/
│       ├── EditMode/        # testes unitários NUnit do Core
│       └── PlayMode/        # testes de interface: uma jogadora simulada joga o app (./scripts/run-ui-tests.sh)
├── Packages/manifest.json
├── ProjectSettings/ProjectVersion.txt
├── scripts/                 # setup-mac · lint · run-tests · run-ui-tests · build-ios-sim · build-macos · check-security · unity-env
├── tools/
│   ├── arte/                # gerador da arte original (Python + Pillow)
│   ├── lint/                # projetos .NET que espelham os asmdefs, só para a análise estática (./scripts/lint.sh)
│   └── musica/              # compositor da trilha original (Python + numpy + ffmpeg)
├── .editorconfig            # estilo e regras do lint (também lido pelas IDEs)
└── docs/                    # esta documentação
```

## Assemblies
| Assembly | Pasta | Depende de | Para quê |
|---|---|---|---|
| `ErvasDoBrasil.Core` | Scripts/Core | — (sem engine) | Regras do jogo |
| `ErvasDoBrasil.Game` | Scripts/* | Core | MonoBehaviours, UI, mundo |
| `ErvasDoBrasil.Editor` | Scripts/Editor | Core, Game | Importação de arte, cena, builds |
| `ErvasDoBrasil.Tests.EditMode` | Tests/EditMode | Core + NUnit | Testes unitários |
| `ErvasDoBrasil.Tests.PlayMode` | Tests/PlayMode | Core, Game + NUnit | Testes de interface (o jogo inteiro rodando) |

## Core (C# puro)
| Arquivo | Responsabilidade |
|---|---|
| `GameSession.cs` | **Fachada**: horta, coleta, preparo, economia, aldeões, espíritos, tempo, vitória |
| `PlayerStatus.cs` | Energia, sanidade, aura, males por dose e bênçãos (amuleto, sorte, revelar, velocidade) |
| `Ailments.cs` | Os 15 males, com penalidade e número de doses |
| `GameData.cs` | `PlantData`, `RecipeData`, `Ingredient`, `StatBonus` (formato do JSON) |
| `GameDatabase.cs` | Catálogo, geração dos preparos simples e `Validate()` dos dados |
| `Inventory.cs` | Bolsa (`erva:`, `seca:`, `po:`, `semente:`, `prod:`) |
| `Farming.cs` | Canteiro: bruto → preparado → plantado → pronto, com rega diária; `FarmLayout`, a grade onde ficam a horta e os canteiros cavados com a pá |
| `Processing.cs` | Varal de secagem e pilão |
| `Crafting.cs` | Caldeirão (ingredientes por forma e risco de queimadura) |
| `ItemEffects.cs` | Efeito de usar um produto (cura, amuleto, banho, banir, revelar, sorte) |
| `Economy.cs` | Preços e modificadores |
| `Spirits.cs` | Regras de proteção (qual aura barra qual espírito) e spawn |
| `SpiritLore.cs` | Fichas das assombrações para o Livro |
| `Village.cs` | Pedidos dos aldeões |
| `Herbarium.cs` | Páginas descobertas e males do amanhecer |
| `Progression.cs` | Nível de raizeira: experiência, níveis e quais plantas cada nível libera |
| `SaveGame.cs` | `SaveData` (formato do jogo salvo) e conversão `GameSession` ⇄ `SaveData` |
| `DayCycle.cs` | Relógio, noite e sono |
| `MusicRules.cs` | Qual faixa toca em cada ambiente |
| `Random.cs` | `IRandom`, para que os testes sejam determinísticos |

## Como as camadas conversam
```
Input (teclado/toque) ─▶ PlayerController ─▶ Interactable.Interact()
                                              │
UI (janelas IMGUI) ───────────────────────────┼─▶ GameSession (Core) ─▶ PlayerStatus / Inventory / ...
                                              │          │
GameManager.Update ─▶ Session.Tick(dt) ◀──────┘          └─ eventos Message/NewDay ─▶ GameUI.Toast
```

## Jogo salvo
`SaveGame.Capture(session)` transforma a partida em `SaveData`; `SaveStore` (Unity) grava em JSON, um arquivo por partida, em `Application.persistentDataPath/saves` (no Mac: `~/Library/Application Support/Douglas Queiroz/Ervas do Brasil/saves`). Para carregar, o `GameManager` recarrega a cena, monta um mundo novo e chama `SaveGame.Restore` numa sessão limpa, então nenhum estado da partida anterior sobra. O menu inicial, o menu da partida e a lista de saves ficam em `UI/MenuWindows.cs`.

## Ordem de desenho
`SpriteFactory.YOrder(y)` coloca na frente quem está mais embaixo na tela (vai de cerca de −1200 a +1200 no mapa de 24 de altura). As camadas de chão (mapa, mata, terra dos canteiros, anel da aura) usam `SpriteFactory.ChaoOrder` (−32000) e somas pequenas, sempre abaixo de qualquer `YOrder`.

## Arte desacoplada
`ArtLibrary.Get("plant_boldo_2")` devolve o sprite se ele existir. Se não existir, o jogo usa uma forma colorida de reserva. Por isso dá para jogar sem arte nenhuma e trocar a arte depois sem mexer em código. Veja [12-arte.md](12-arte.md).

## Bastidores e ciclo de vida
`UI/DevWindow.cs` desenha a janela **Bastidores do desenvolvimento** (menu inicial e Menu da partida): como o jogo foi feito, o ciclo de vida do app, Unity × Web e um painel ao vivo. O painel lê o `World/LifecycleLog.cs`, um diário estático (sobrevive à recarga da cena) que o `GameManager`, a `CameraFollow` e a `GameUI` alimentam nos ganchos `Awake`, `Start`, `Update`, `LateUpdate`, `OnGUI`, `OnApplicationFocus`, `OnApplicationPause` e `OnDestroy`. Cada evento também vai para o log com o prefixo `[Ciclo de vida]`. A explicação completa está no [README](../README.md#-ciclo-de-vida-do-app).
