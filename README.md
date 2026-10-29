# Ervas do Brasil 2D

RPG cozy top-down em **Unity 2D** sobre o poder das plantas brasileiras, chás, ervas e do nosso folclore. Você é uma raizeira que herdou o sítio da avó: prepara a terra, planta, colhe, seca, pila, faz chás, garrafadas e elixires, cura a si mesma (inclusive a fissura do cigarro) e os aldeões. À noite, protege a casa das assombrações com espada-de-são-jorge, arruda, guiné e pimenteira, **sem violência**.

**Plataformas:** iPhone (Simulador iOS) · macOS · **Engine:** Unity 6.3 LTS (C# 9, .NET Standard 2.1) · **Câmera:** top-down · **Combate:** passivo · **Testes:** 239 unitários + 38 de interface (NUnit, com teste de desempenho) · **Lint:** Roslyn + analisadores .NET e Unity

---

## ⚡ Início rápido
```bash
cd ~/Dev/ErvasDoBrasil_2D
./scripts/setup-mac.sh          # Unity Hub + Unity 6.3 LTS + módulo iOS (uma vez)
# → abra o Unity Hub, faça login, ative a licença Personal e adicione esta pasta

./scripts/lint.sh               # análise estática do C# (lint), sem abrir a Unity
./scripts/run-tests.sh          # testes unitários do core
./scripts/run-ui-tests.sh       # testes de interface: abre o jogo e uma jogadora simulada joga
./scripts/build-ios-sim.sh      # compila e abre no Simulador do iPhone
./scripts/build-macos.sh        # gera e abre o app do macOS (release; --dev para build de desenvolvimento)
./scripts/check-security.sh     # confere que o app não abre porta nem conexão de rede
```
No editor: menu **Ervas do Brasil ▸ Criar ou abrir cena principal** ▸ **Play**.

| Plataforma | Controles |
|---|---|
| macOS / editor | WASD ou setas · **E** ação · **I** bolsa · **B** livro · **H** histórico · **M** liga/desliga o som · **Esc** menu / fecha janela · clique no HUD recolhe |
| iPhone | arraste à esquerda para andar · botão grande = ação · botões Bolsa, Livro, Histórico e Menu |

**Histórico:** o botão **Histórico (H)**, ao lado da Bolsa, guarda os últimos 60 avisos da partida (os que aparecem no canto da tela e somem em 4 s), do mais novo para o mais antigo, com o dia e a hora do jogo. Subir de nível e a dica das plantas liberadas aparecem em destaque dourado; avisos repetidos em seguida viram uma linha só ("3 vezes"). O selo vermelho no botão conta os avisos novos desde a última vez que ele foi aberto, e o histórico vai junto no jogo salvo. A regra fica em `Core/MessageLog.cs` (a `GameSession` registra cada aviso) e a janela em `UI/HistoryWindow.cs`.

**Som:** no rodapé, à direita, ficam o botão de mudo (o alto-falante, ou a tecla **M**) e o volume do jogo, com a porcentagem ao lado. O controle muda só o som do jogo, sem mexer no volume do Mac ou do iPhone, e a escolha fica gravada para a próxima vez. Ele aparece também no menu inicial e com as janelas abertas.

O jogo abre no **menu inicial** (Continuar · Novo jogo · Jogos salvos · **Bastidores do desenvolvimento** · **Sair do jogo**, que fecha o app). Salve pelo **Menu** ou dormindo na cama da Casa da Vó. O Sair responde na hora: a tela escurece com "Fechando o jogo...", o botão vira "Saindo..." (um segundo clique não faz nada) e o app fecha no frame seguinte. Ele fica só no menu inicial: no meio da partida, volte pelo Menu ▸ Menu inicial (o que não foi salvo se perde). No iPhone não há Sair, porque quem fecha o app é o sistema.

---

## 🎬 Bastidores do desenvolvimento (dentro do jogo)
O jogo traz uma janela que explica como ele foi feito. Ela abre pelo botão **Bastidores do desenvolvimento** no menu inicial ou no **Menu** da partida (Esc). São quatro abas:

| Aba | O que mostra |
|---|---|
| **Como foi feito** | ferramentas, o mundo montado em código, as camadas Core × Unity, dados em JSON, arte e música geradas, IMGUI, testes, lint e builds |
| **Ciclo de vida** | do ícone ao primeiro frame, o loop de cada frame (animado: Entrada → `Update` → `LateUpdate` → Desenho → `OnGUI`), troca de cena, segundo plano e saída |
| **Unity × Web** | 13 comparações lado a lado entre um sistema web e este jogo |
| **Ao vivo** | frame atual, FPS, quantas vezes o `OnGUI` rodou no último frame, objetos na cena, onde ficam os saves e o log, e o **diário do ciclo de vida** com os eventos reais desta execução |

O diário é o `LifecycleLog` (`Assets/Scripts/World/LifecycleLog.cs`). Cada evento também vai para o Console da Unity e para o `Player.log` com o prefixo `[Ciclo de vida]`. Para ver o diário mudar: troque de janela (Cmd+Tab) e volte, ou carregue um jogo salvo, e reabra a aba **Ao vivo**.

O texto deste README e o da janela contam a mesma história. O código da janela está em `Assets/Scripts/UI/DevWindow.cs`.

---

## 🛠️ Como o jogo foi construído

### Ferramentas
| Para quê | Ferramenta |
|---|---|
| Engine e linguagem | Unity 6.3 LTS (`6000.3.26f1`) com C# 9 sobre .NET Standard 2.1 |
| Código | escrito em parceria com o Claude Code (IA) |
| Arte | `tools/arte`: Python + Pillow desenham cada sprite no estilo "Quintal Minimalista" |
| Música | `tools/musica`: Python + numpy + ffmpeg compõem as faixas de dia, feira e noite |
| Testes | Unity Test Framework (NUnit), modo EditMode |
| Lint | Roslyn + analisadores do .NET (CA), regras de estilo (IDE), Microsoft.Unity.Analyzers (UNT) e `dotnet format` |
| Builds | `scripts/*.sh` chamam a Unity em modo batch (`-batchmode -executeMethod`), sem abrir o editor |

### Uma cena vazia, um mundo feito em código
`Main.unity` só tem a câmera e o `GameManager`. No `Awake()`, o `GameManager` lê `plants.json` e `recipes.json`, cria a `GameSession` e monta tudo com `new GameObject()` + `AddComponent()`: mapa, casa, canteiros, feira, aldeões, ervas da mata, música, jogadora e câmera. Não há prefabs nem objetos posicionados à mão, então o mundo inteiro está no código e no controle de versão. Um `[RuntimeInitializeOnLoadMethod]` cria o `GameManager` se a cena não tiver um, por isso dá para dar Play em qualquer cena.

### Duas camadas: regras × motor
```
Entrada (teclado/toque) ─▶ MonoBehaviours (camada Game) ─▶ GameSession (Core, C# puro) ─▶ eventos ─▶ HUD, janelas, sprites, música
     InputCompat             PlayerController, FarmPlot,        energia, males, horta,          Message / NewDay
                             Villager, GameUI...                receitas, economia, save
```
- **`ErvasDoBrasil.Core`** é C# puro: o asmdef tem `noEngineReferences: true`, então nada ali pode usar `UnityEngine`. Toda regra do jogo mora aqui e é testada sem abrir a Unity. É a mesma ideia de separar as regras de negócio da API num back-end.
- **`ErvasDoBrasil.Game`** (MonoBehaviours) só lê a entrada, chama a `GameSession` e desenha o resultado.
- **`ErvasDoBrasil.Editor`** cuida da importação da arte, da cena e dos builds. **`ErvasDoBrasil.Tests.EditMode`** tem os testes.

### Dados, arte e interface
- **Dados em JSON** (`Assets/Resources/Data`): para incluir uma planta, basta editar o JSON. `GameDatabase.Validate()` e os testes conferem ids, ingredientes e níveis.
- **Arte desacoplada:** o `ArtPipeline` importa os PNGs e gera a `ArtLibrary` (nome → sprite). Se um sprite faltar, o jogo desenha uma forma colorida no lugar e continua jogável.
- **Interface em IMGUI:** todas as janelas são desenhadas no `OnGUI()` com `GUILayout`, sem montar nada na cena. A UI usa uma tela virtual de 720 de altura escalada pela `GUI.matrix`, e a `Screen.safeArea` desvia do notch do iPhone. Na versão final, dá para migrar para UI Toolkit mantendo as chamadas à `GameSession`.

### Build e plataformas
- **macOS:** `./scripts/build-macos.sh` gera `Builds/macOS/ErvasDoBrasil.app` com o backend **Mono** (compilação JIT, build rápido).
- **Release por padrão:** o build de desenvolvimento (Profiler, console de erros na tela) abre a porta 55000 e anuncia o IP do computador na rede local, por isso só sai com `--dev`. Um portão de segurança roda antes de todo build ([Segurança](#-segurança)).
- **iPhone:** `./scripts/build-ios-sim.sh` gera um projeto Xcode com **IL2CPP** (o C# vira C++ e é compilado antes, AOT) e abre no Simulador.
- A mesma base de código atende teclado no Mac e joystick virtual no toque (`InputCompat`).
- **Ícone:** `tools/arte/icone.py` gera duas versões. `app_icon.png` é o quadrado opaco que o **iOS** recorta sozinho. `app_icon_macos.png` já vem com o formato do **macOS**, porque o Mac não recorta: um "squircle" de 824 px centrado na tela de 1024, com cantos contínuos e sombra suave, igual aos outros ícones do Dock. O `BuildTools` usa a segunda só no build do Mac.

---

## 🔄 Ciclo de vida do app

### 1. Do ícone ao primeiro frame
| # | Momento | Gancho da Unity | O que este jogo faz |
|---|---|---|---|
| 1 | o sistema abre o executável e a Unity sobe o runtime C# | `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` | `LifecycleLog` zera o diário |
| 2 | antes de carregar a cena | `BeforeSceneLoad` | anota no diário |
| 3 | a primeira cena da build (`Main.unity`) é carregada e cada componente nasce | `Awake()` | `GameManager` lê os JSON, cria a `GameSession`, monta o mundo e abre o menu inicial |
| 4 | o componente é ligado | `OnEnable()` | canteiros, ervas e espíritos entram em listas estáticas (`Interactable.All`, `Spirit.All`) |
| 5 | depois da cena | `AfterSceneLoad` | `GameManager.Boot()` cria um `GameManager` se a cena não tiver um |
| 6 | logo antes do primeiro `Update` | `Start()` | anota no diário |

Este é o diário real de uma abertura do app no Mac (`Player.log`). Os passos 1 a 5 acontecem no frame 0, e o primeiro frame de verdade (frame 1) roda `Start` → `Update` → `LateUpdate` → `OnGUI`, nessa ordem:
```
frame 0 · SubsystemRegistration: a Unity subiu o runtime C#
frame 0 · BeforeSceneLoad: a primeira cena vai ser carregada
frame 0 · GameManager.Awake(): lê os JSON, cria a GameSession e monta o mundo
frame 0 ·   mundo montado: 36 objetos interativos se registraram no OnEnable()
frame 0 · GameManager.Boot() [AfterSceneLoad]: garante um GameManager na cena
frame 0 · OnApplicationPause(false): o app está rodando em primeiro plano
frame 0 · OnApplicationFocus(true): o app ganhou o foco
frame 1 · GameManager.Start(): roda uma vez, logo antes do primeiro Update()
frame 1 · GameUI.Update(): primeiro frame desta cena
frame 1 · CameraFollow.LateUpdate(): primeira vez nesta cena, depois de todos os Update()
frame 1 · GameUI.OnGUI(): primeira vez, depois que o frame foi desenhado
```

### 2. A cada frame (~60 vezes por segundo)
```
 Entrada ──▶ Update() ──▶ LateUpdate() ──▶ Desenho ──▶ OnGUI() ──▶ (próximo frame)
```
| Etapa | Método | O que este jogo faz |
|---|---|---|
| Entrada | (Unity) | lê teclado, mouse e toque; o código pergunta "a tecla E foi apertada neste frame?" (`InputCompat.Pressed`) |
| Lógica | `Update()` | `GameManager` avança o relógio com `Session.Tick(Time.deltaTime)` e faz nascer espíritos; `PlayerController` move a raizeira; cada `Spirit` persegue ou foge da aura |
| Depois da lógica | `LateUpdate()` | `CameraFollow` segue a jogadora depois que todos andaram, sem tremer |
| Desenho | (Unity) | a câmera renderiza os sprites, ordenados pela altura (`SpriteFactory.YOrder`) |
| Interface | `OnGUI()` | `GameUI` desenha HUD e janelas; roda **várias vezes por frame**: um evento de Layout, um de Repaint e um para cada clique ou tecla |

- Tudo que se move é multiplicado por `Time.deltaTime`, então a velocidade é a mesma a 30 ou a 120 fps.
- `FixedUpdate()` serve para física em passo fixo, e este jogo não usa física.
- **Pausar sem parar o loop:** com o Menu, os jogos salvos, os Bastidores ou as telas de abertura e de fim abertos (`GameUI.PausesWorld`), `GameManager.Update()` retorna cedo e o relógio do jogo para, mas o loop continua (na aba **Ao vivo** o contador de frames segue subindo). Bolsa, Livro, Caldeirão, Varal, Pilão, Feira, plantio e aldeões **não pausam**: o tempo corre (o relógio aparece no canto da janela) e a raizeira só não anda.

### 3. Trocar de cena = recarregar a página
Carregar um jogo salvo, começar outro ou voltar ao menu inicial chama `SceneManager.LoadScene`. Todos os objetos recebem `OnDisable()` e `OnDestroy()`, a cena volta do zero e o ciclo recomeça no `Awake()`. Só os campos `static` sobrevivem: é por eles (`GameManager.pendingLoad`) que o pedido "carregue este save" atravessa a recarga, como o `sessionStorage` de uma página web. No diário aparece `GameManager.OnDestroy()` → `GameManager.Awake()` → `SceneManager.sceneLoaded` → `Start()`.

### 4. Segundo plano e saída
| Evento | Quando | Observação |
|---|---|---|
| `OnApplicationFocus(false/true)` | o Mac troca de janela | aparece no diário |
| `OnApplicationPause(true/false)` | o iPhone manda o app para o segundo plano e de volta; no Mac, também quando a janela perde o foco | depois do `true`, o iPhone pode encerrar o processo sem outro aviso. No Mac o *Run In Background* está **ligado**: sem foco o loop continua e a música não corta ao trocar de janela ou minimizar. Para a partida não seguir sozinha, `GameUI.OnApplicationFocus(false)` abre o **Menu**, que pausa o relógio |
| `OnApplicationQuit()` | o app fecha: botão **Sair do jogo** do menu inicial (`Application.Quit()`) ou Cmd+Q | último gancho; aparece no `Player.log` |

Hoje o jogo salva pelo Menu ou ao dormir. Salvar também no `OnApplicationPause(true)` seria o próximo passo para não perder progresso no iPhone.

---

## 🌐 Unity × sistema web
Para quem vem da web, estas são as diferenças que mais mudam o jeito de pensar:

| Tema | Na web | Neste jogo (Unity) |
|---|---|---|
| **Quem manda no tempo** | orientado a eventos: o servidor responde a uma requisição e fica parado; o navegador só redesenha quando algo muda | **game loop**: ~60 vezes por segundo o jogo roda `Update()` e redesenha a tela inteira, mesmo com a jogadora parada |
| **Onde fica o estado** | servidor sem estado; dados no banco, sessão em cookie ou token; no front, estado do React e URL | tudo na memória do processo (`GameSession`); sem servidor nem banco: o save é um JSON em `persistentDataPath` |
| **Interface** | DOM + CSS em **modo retido**: você descreve a árvore e o navegador a guarda; clique vira evento `onClick` | IMGUI em **modo imediato**: a cada `OnGUI()` o código desenha cada botão de novo, e `GUI.Button()` devolve `true` no frame do clique |
| **Entrada** | eventos empurrados para handlers (`addEventListener`) | **polling**: a cada frame o código pergunta se a tecla foi apertada (`Input.GetKeyDown`) |
| **Tempo e animação** | `setTimeout`, `setInterval`, `requestAnimationFrame`, transições de CSS | `Time.deltaTime` em cada `Update()`; um dia do jogo são 240 s de frames somados, e ele para quando o Menu abre |
| **Layout e telas** | px, flexbox, media queries; o navegador calcula o layout | mundo em unidades com câmera ortográfica; UI numa tela virtual de 720 de altura escalada; `safeArea` desvia do notch |
| **Arquivos e assets** | imagens baixadas por HTTP sob demanda, com cache e CDN | assets importados no editor (PNG vira sprite, OGG vira `AudioClip`) e empacotados no app; `Resources.Load` lê do pacote, sem rede |
| **Entrega** | deploy no servidor; o usuário abre a URL e já tem a versão nova; rollback no servidor | um binário por plataforma (`.app`, projeto Xcode); cada mudança pede build novo e reinstalação; na App Store, revisão da Apple |
| **Linguagem e runtime** | JavaScript/TypeScript no navegador; Node, Java, Go no servidor | C# 9 sobre .NET Standard 2.1; Mono (JIT) no Mac, IL2CPP (AOT, sem gerar código em tempo de execução) no iPhone |
| **Concorrência** | muitos usuários ao mesmo tempo; async/await, filas, o banco cuida dos conflitos | um jogador e um processo; a API da Unity só pode ser chamada na thread principal |
| **Testes e análise estática** | unitários (Jest, JUnit), testes de API, E2E no navegador (Playwright, Cypress); ESLint | NUnit no Core (EditMode, sem abrir o jogo); PlayMode testa cenas; o E2E costuma ser o playtest; lint com analisadores .NET + `dotnet format` |
| **Depuração e logs** | DevTools: console, aba Network, inspetor do DOM; logs centralizados do servidor | Console, Inspector e Profiler do editor com o jogo rodando; no Mac, `~/Library/Logs/Douglas Queiroz/Ervas do Brasil/Player.log` |
| **Segurança** | autenticação, CORS, validação de entrada, injeção de SQL, rate limit contra DDoS | offline e sem login: não há API para atacar nem requisição para vazar dados. O lint proíbe APIs de rede, o build sai sem porta aberta e o save é validado como entrada não confiável. O jogador pode editar o próprio save, o que é aceitável num jogo single-player |

---

## 🔒 Segurança
O jogo é **offline**: não tem servidor, API nem login e não faz requisições. Não existe API para sofrer DDoS nem requisição por onde dados possam vazar, e cinco camadas garantem que continue assim:

| Camada | O que faz |
|---|---|
| **Lint** (`RS0030`) | `tools/lint/BannedSymbols.txt` proíbe no código do jogo `HttpClient`, sockets, `UnityWebRequest`, `Application.OpenURL`, `Process` e `BinaryFormatter` |
| **Portão antes do build** | `Editor/SecurityGate.cs` reprova qualquer build com pacote de rede ou telemetria, HTTP sem criptografia, serviços da Unity ligados (Analytics, Ads, IAP, relatório de crash) ou build de desenvolvimento não pedido |
| **Build release** | por padrão o app sai sem a conexão do Profiler. Até 29/10 todo build era de desenvolvimento e escutava na **porta 55000**, anunciando IP e nome do Mac na rede local |
| **Conferência do app** | depois do build, o `boot.config` do app não pode ter `player-connection`; `./scripts/check-security.sh` abre o jogo e confere com `lsof` que nenhum socket foi aberto |
| **Save como entrada não confiável** | arquivo de até 1 MB, id sem `/` nem `..`, números sem `NaN` nem estouro, listas e nomes com limite: um save adulterado ou baixado da internet não trava o jogo |

Na tela (aba **Ao vivo**) e nos avisos do save, a pasta pessoal aparece como `~`, para uma captura de tela não revelar o nome da conta. Detalhes, como verificar e o que fazer se um dia o jogo tiver servidor: [docs/15-seguranca.md](docs/15-seguranca.md).

---

## 🧹 Análise estática (lint do C#)
Os testes **executam** o código. O lint **lê** o código sem executar e procura bugs prováveis, desperdício e estilo fora do padrão.

```bash
./scripts/lint.sh          # verifica: código de saída 1 se houver qualquer aviso ou formatação fora do padrão
./scripts/lint.sh --fix    # corrige o que dá automaticamente (formatação, usings, estilo) e verifica de novo
```
Saída quando está tudo certo:
```
▶ 1/2 Analisadores (CA, IDE, UNT, RS) nos 5 assemblies: .NET SDK 10.0.402, DLLs da Unity 6000.3.26f1...
✅ Nenhum aviso.
▶ 2/2 Formatação e estilo (dotnet format, regras do .editorconfig)...
✅ Tudo no padrão.
✅ Lint OK
```

**Pré-requisitos:** o .NET SDK (`brew install --cask dotnet-sdk`), a Unity instalada (o lint usa as DLLs `UnityEngine`/`UnityEditor` dela) e a pasta `Library/PackageCache`, criada na primeira vez que a Unity abre o projeto ou roda `./scripts/run-tests.sh`. O lint leva uns 6 segundos e, ao contrário dos testes e dos builds, **pode rodar com o editor da Unity aberto**.

### Como funciona
A Unity gera arquivos `.csproj` só para a IDE (e o `.gitignore` os ignora). Por isso o lint tem seus próprios projetos .NET em `tools/lint/`, **um por asmdef**, que compilam os mesmos arquivos de `Assets/`:

| Projeto de lint | Espelha | Referências |
|---|---|---|
| `ErvasDoBrasil.Core.csproj` | `Scripts/Core` | nenhuma (C# puro, como o asmdef exige) |
| `ErvasDoBrasil.Game.csproj` | `Scripts/*` | Core + DLLs `UnityEngine.*` |
| `ErvasDoBrasil.Editor.csproj` | `Scripts/Editor` | Core + Game + DLLs `UnityEditor.*` |
| `ErvasDoBrasil.Tests.EditMode.csproj` | `Tests/EditMode` | Core + UnityEngine + NUnit do `Library/PackageCache` |

`tools/lint/Directory.Build.props` fixa **.NET Standard 2.1 e C# 9**, iguais aos da Unity 6, para o lint não sugerir recursos que a Unity não compila. A saída vai para `Builds/lint`. O script roda dois passos:
1. **`dotnet build -warnaserror`** com quatro famílias de regras:
   - **CA** (analisadores do .NET, modo *Recommended*): cultura em textos, desempenho, uso correto de APIs.
   - **IDE** (estilo): `using` desnecessário, membro ou parâmetro sem uso, valor atribuído e nunca lido.
   - **UNT** ([Microsoft.Unity.Analyzers](https://github.com/microsoft/Microsoft.Unity.Analyzers)): erros típicos de Unity, como `?.`/`??` em objetos da Unity, `GetComponent` que aloca e operações de asset no carregamento. Esse pacote também evita falsos alarmes: ele sabe que `Awake`, `Update` e companhia são chamados pela Unity e não acusa esses métodos como "nunca usados".
   - **RS0030** ([BannedApiAnalyzers](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md)), só no Core e no Game: APIs proibidas listadas em `tools/lint/BannedSymbols.txt`, como rede, processos externos e `BinaryFormatter` ([Segurança](#-segurança)).
2. **`dotnet format --verify-no-changes`**: formatação (chaves em linha própria, indentação de 4 espaços, fim de linha LF) e estilo, conforme o `.editorconfig`.

### Regras (`.editorconfig` na raiz)
O `.editorconfig` também vale no VS Code, no Rider e no Visual Studio. Ficam desligadas, de propósito:
| Regra | Por quê |
|---|---|
| CA1051 (campos públicos) | a Unity e o `JsonUtility` só serializam **campos**; `PlantData` e os componentes precisam deles |
| CA1707 (sublinhado em nomes) | testes se chamam `Metodo_cenario_resultado`, e os métodos `CI_*` são chamados pelo `-executeMethod` dos scripts |
| CA1822 (marcar como `static`) | vira só sugestão: os auxiliares da `GameUI` ficam de instância por ser uma classe parcial de UI |
| CA1708 em `GameData.cs` | o campo `aura` espelha o JSON e a propriedade `Aura` dá a versão tipada |
| CA1861 nos testes | arrays literais deixam os cenários legíveis; desempenho não importa ali |

### O que o lint encontrou (e foi corrigido) na primeira rodada
- `StartsWith`/`ToLower` dependentes do idioma do sistema → `StringComparison.Ordinal` e `ToLowerInvariant` (CA1310, CA1304, CA1311).
- `Inventory.TryParseKey` sem checar o retorno em 6 lugares → agora pula a chave inválida (CA1806).
- `??` entre sprites da Unity, que ignora o "null" de objeto destruído → `if (s == null)` (UNT0007).
- `GetComponent` para checar se o componente existe → `TryGetComponent`, que não aloca (UNT0026).
- `AssetDatabase` usado dentro do construtor estático do `ArtLibraryBuilder` → movido para um método chamado no `delayCall` (UNT0031).
- Arrays criados a cada `OnGUI()` na janela Bastidores → campos `static readonly` (CA1861).
- Variáveis `out` nunca lidas → descarte `out _` (IDE0059); estáticos públicos mutáveis → propriedades (CA2211); `nameof`, `CultureInfo.InvariantCulture`, `Array.Empty` e outros ajustes pequenos.

---

## 📚 Documentação
| # | Arquivo | Conteúdo |
|---|---|---|
| 01 | [Visão geral](docs/01-visao-geral.md) | pilares, decisões, o que já funciona e o que falta |
| 02 | [Enredo e roteiro](docs/02-enredo-e-roteiro.md) | premissa, personagens, os 3 atos, temas |
| 03 | [Estrutura do projeto](docs/03-estrutura-do-projeto.md) | pastas, assemblies, arquitetura Core × Unity |
| 04 | [Ambiente e setup](docs/04-ambiente-e-setup.md) | instalar a Unity no Mac, licença, problemas comuns |
| 05 | [Rodar no editor (desktop)](docs/05-rodar-no-editor-desktop.md) | Play, controles, testar toque, iteração |
| 06 | [Rodar no macOS](docs/06-rodar-no-macos.md) | build do `.app` |
| 07 | [Rodar no iPhone (simulador)](docs/07-rodar-no-iphone-simulador.md) | build Xcode, simulador, controles de toque |
| 08 | [Testes](docs/08-testes.md) | como rodar, o que cobre, como escrever novos, lint |
| 09 | [Mecânicas](docs/09-mecanicas.md) | energia, sanidade, aura, 15 males, economia |
| 10 | [Plantas e receitas](docs/10-plantas-e-receitas.md) | as 23 plantas, o fluxo de preparo, receitas compostas, auras |
| 11 | [Assombrações](docs/11-assombracoes.md) | Alma da Lamparina, Visagem da Neblina, Saci, Olho-Gordo, Cuca e Bernúncia; noites seguras |
| 12 | [Arte](docs/12-arte.md) | estilo "Quintal Minimalista", gerador de arte, convenções |
| 13 | [Escopo do freelancer](docs/13-escopo-freelancer.md) | próximas entregas e critérios de aceite |
| 14 | [Trilha sonora](docs/14-trilha-sonora.md) | faixas por ambiente, trilha original, licenças |
| 15 | [Segurança](docs/15-seguranca.md) | superfície de ataque, lint de APIs proibidas, portão do build, saves não confiáveis, como verificar, e se um dia tiver servidor |

---

## 🗺️ Mapa rápido do código
```
Assets/Scripts/Core      ⭐ regras puras (GameSession, PlayerStatus, Progression, SaveGame...) e 100% testáveis
Assets/Scripts/*         Unity: mundo, jogadora, espíritos, UI, Livro, Bastidores (DevWindow) e LifecycleLog
Assets/Scripts/Editor    pipeline de arte, cena, builds iOS/macOS e portão de segurança (SecurityGate)
Assets/Resources/Data    plants.json · recipes.json
Assets/Art               sprites originais (gerados por tools/arte)
Assets/Icon              ícone do app: a Vó benzedeira (tools/arte/icone.py)
Assets/Audio             trilha original (gerada por tools/musica)
Assets/Tests/EditMode    testes NUnit
scripts/                 setup, lint, testes e builds pelo terminal
tools/                   gerador de arte, compositor da trilha (Python) e projetos do lint (tools/lint)
.editorconfig            estilo e regras do lint, também lidos pelas IDEs
```

---

> ⚠️ Conteúdo educativo sobre usos tradicionais e cultura popular. Não substitui orientação médica. Arruda, guiné e comigo-ninguém-pode são tóxicas se ingeridas, e o jogo as usa apenas como proteção simbólica. Para parar de fumar, o SUS oferece tratamento gratuito.
