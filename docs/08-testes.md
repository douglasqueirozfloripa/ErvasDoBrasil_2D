# Testes automatizados e análise estática

O core do jogo (`Assets/Scripts/Core`) é C# puro e é coberto por **testes unitários NUnit** no **Unity Test Framework** (modo EditMode).

## Rodar
```bash
./scripts/run-tests.sh
```
Saída:
```
Total: 239  ✅ 239  ❌ 0  ⏭ 0
```
O XML (formato NUnit) fica em `Builds/TestResults-EditMode.xml` e o log em `Builds/unity-tests.log`. O código de saída é 0 quando tudo passa, então serve para CI.

Pela Unity: **Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All**.

## O que é testado
| Arquivo | Cobre |
|---|---|
| `InventoryAndDatabaseTests.cs` | Bolsa (chaves, quantidades, exceções, sorteio do Saci) · catálogo, preparos simples gerados, `Validate()`, parse de males |
| `PlayerStatusTests.cs` | Doses (fissura = 3), velocidade por mal, gripe, azia, sanidade/ansiedade, noite × dia, crises de fissura (com tranco na tela; em espera até o dia seguinte), aviso de energia perdida (só fora das ações), amuleto, mau-olhado × aura × pimenteira, preços, revelar |
| `LoopTests.cs` | Relógio (18 h, meia-noite, dormir) · canteiro (preparar, regar, crescer, secar no novo dia, colheita com sorte e com mau-olhado, auras) · varal e pilão |
| `CraftingAndEffectsTests.cs` | Caldeirão (forma dos ingredientes, queimadura, energia) · efeitos (boldo, camomila, arruda, guaraná, banho, amuleto, defumação) · economia · regras de proteção · fichas das assombrações · regras da trilha |
| `ChaFrescaOuSecaTests.cs` | Playtest de 29/10 (save com 4 mulungus secos e o chá travado em "Faltam ingredientes"): o preparo simples gerado pede a erva **fresca ou seca** · o chá sai com a erva seca · gasta a fresca antes da seca · sem o bastante não tira nada da bolsa · pó não serve para o chá, e o elixir continua pedindo pó (e camomila fresca) · forma desconhecida no JSON vira fresca |
| `GameSessionTests.cs` | Fluxos completos: kit inicial, primeira crise de fissura só depois do primeiro plantio (ou no dia 2, sem plantar), assombração/Cuca/Bernúncia avisam quanto de energia tiraram, horta → Livro, secar → pilar → elixir → usar, feira, aldeões, sono, insônia, noite em claro, males do amanhecer, Saci, Alma, vitória |
| `SaveAndProgressionTests.cs` | Salvar → JSON → carregar devolve a mesma partida; salvo antes do primeiro plantio, a fissura continua em espera (save antigo: crises como antes); dados que sumiram dos JSONs são ignorados · níveis e travas de feira/plantio/mata · experiência · mulungu do kit já no Livro · ansiedade não volta logo após a cura · ansiosa recupera pela metade · doses espaçam as crises de fissura · dormir acalma |
| `ShovelTests.cs` | Pá: grade da horta e `Snap`, comprar (uma vez, com dinheiro), cavar (sem pá, casa ocupada, sem energia, limite de 24), canteiro cavado já preparado e com aura de Espada-de-São-Jorge, **cavado só aceita planta de proteção** (sem gastar semente nem energia, com aviso) e a horta da vó aceita qualquer uma, planta de proteção é a que faz efeito na área, desfazer só cavado e vazio, salvar e carregar os cavados no lugar, save antigo sem pá |
| `PlaytestGarapuvuTests.cs` | Correções do playtest de 28/10: tirar do varal sem perder (fresca ou seca), vender tudo pelo valor anunciado, comprar de 5 em 5 até o dinheiro acabar, barra de XP de 0 a 1 em cada nível, noites seguras, Cuca só de madrugada e Bernúncia a partir do dia 5, quem espanta quem, Cuca dá cansaço, Bernúncia engole o canteiro (terra continua preparada) e a jogadora |
| `SecurityTests.cs` | Segurança ([docs/15](15-seguranca.md)): save com `NaN`/infinito, números gigantes (sem estouro na próxima soma), listas enormes (limite de 500 e o mesmo limite da pá) e nome enorme; id de save que tentaria sair da pasta (`../`, `/`, `\`, `:`); regra que acha pacote de rede inclusive como dependência; `manifest.json` e `packages-lock.json` reais sem pacote de rede; `boot.config` com a porta do Profiler; Player Settings sem HTTP nem serviços da Unity |
| `SoundSettingsTests.cs` | Volume do jogo e mudo: começa em 80%, o mudo guarda o volume e desmutar volta a ele, mexer no volume tira do mudo, volume preso entre 0 e 1, desmutar com o volume no zero volta ao padrão |
| `FrameRateTests.cs` | Régua do teste de desempenho (`Core/FrameRate.cs`): 60 fps constantes = excelente · FPS alto com um quadro de 50 ms engasga e cai para regular (o 1% low entrega) · dois engasgos em 15 s = bom · 30 fps estáveis = regular · 15 fps = ruim · oscilar entre rápido e lento pesa mais que a média · limites do alvo de 60 (55/45, 45/30, 30/15 fps e 0/2/5 engasgos), proporcionais a outro alvo · 29,99 vale como 30 · lista vazia ou valores inválidos = ruim sem estourar · resumo com a nota e os números |
| `PedidosDaVilaTests.cs` | Pedidos da vila valem uma semana (iguais do dia 1 ao 7, novos no dia 8, com aviso) · atender os três traz pedidos novos na manhã seguinte · atender só alguns mantém os pedidos · dormir também conta os dias · a semana volta com o jogo salvo · save antigo ou com dia no futuro conta a partir do dia carregado · save sem pedidos recebe pedidos no dia seguinte · cada aldeão tem a sua arte (`Village.Arte`) |
| `MessageLogTests.cs` | Histórico dos avisos: do mais novo para o mais antigo com dia e hora · repetido em seguida vira uma linha com a contagem · guarda só os 60 últimos · abrir zera os não lidos · texto vazio não entra e enorme é cortado · subir de nível entra em destaque com as plantas liberadas · `Avisar` registra e vai para o canto da tela · novo jogo começa limpo · volta com o jogo salvo sem contar como novo · save antigo abre vazio · save adulterado não estoura |
| `DataFileTests.cs` | Valida os **JSON reais** do jogo: nenhum id quebrado, todo mal tem cura, plantas do briefing presentes, textos do Livro completos, níveis válidos com o kit inicial no nível 1, mulungu cura fissura e ansiedade |

## Como os testes ficam determinísticos
Toda aleatoriedade passa por `IRandom`. Nos testes, o `FixedRandom` devolve valores roteirizados, por exemplo:
```csharp
var r = plot.Harvest(status, new FixedRandom().Ints(3).Values(0.1f)); // 3 ervas + semente
```
O `TestData` monta um catálogo pequeno e previsível, independente do JSON do jogo.

## Escrevendo um teste novo
1. Coloque a regra em `Scripts/Core` (nada de `UnityEngine` ali; o asmdef impede).
2. Crie o teste em `Assets/Tests/EditMode/` com `[Test]` / `[TestCase]`.
3. Rode `./scripts/run-tests.sh`.

> Os testes do core também foram executados fora da Unity durante a criação do projeto (Mono + NUnit mínimo): 113/113 passaram. Os 5 de `DataFileTests` dependem da Unity (`JsonUtility`), e os mesmos JSONs foram validados com `GameDatabase.Validate()` sem nenhum erro.

## Testes de interface (PlayMode)
```bash
./scripts/run-ui-tests.sh
```
A Unity gera um **app de teste do macOS** com o jogo e os testes de `Assets/Tests/PlayMode` e abre esse app. Uma janela do jogo aparece por alguns minutos enquanto uma **jogadora simulada** joga pelo mesmo caminho da entrada real: anda com o joystick virtual (o do iPhone), aperta E, I, B e Esc e clica nos botões da tela pelo texto. Cada passo confere o estado do jogo, e os passos importantes tiram uma foto da tela.

- Resultado em `Builds/TestResults-PlayMode.xml`, log em `Builds/unity-ui-tests.log` e fotos em `Builds/ui-tests/<teste>/NN-passo.png`.
- Como o `run-tests.sh` e o build, não roda com o editor da Unity aberto neste projeto. Não precisa de permissão de Acessibilidade: nada controla o mouse do Mac.
- Pela Unity: **Test Runner ▸ PlayMode ▸ Run All** roda os mesmos testes na aba Game do editor.

| Arquivo | Cobre |
|---|---|
| `MenuEJogosSalvosTests.cs` | Menu inicial sem saves (sem Continuar) e Bastidores · salvar pelo Menu, voltar ao menu inicial e Continuar (mesmo canteiro, mesma posição) · **apagar a partida antiga pela lista mantém a do Continuar**, com Desfazer · Carregar pela lista abre a partida escolhida · **Sair do jogo** (`GameUI.BotaoSair`) responde na hora ("Fechando o jogo...", botão "Saindo..." desabilitado), pede para fechar o app uma vez só, também com Continuar e Jogos salvos na tela, e não aparece no Menu da partida |
| `HortaEMataTests.cs` | Novo jogo → preparar, plantar e regar ("Regar [E]") · a planta cresce (relógio acelerado) e é colhida, com página do Livro e experiência · coleta na mata: erva do nível 1 vem para a bolsa, erva trancada não |
| `PreparosTests.cs` | Chá de mulungu no Caldeirão, usado pela Bolsa, conta uma dose da fissura · varal seca a erva, Recolher secas, Pilão vira pó · **mulungu seco também faz o chá** (seca todo o mulungu do kit no varal, recolhe e o Caldeirão ▸ Preparar sai com a erva seca) |
| `FissuraTests.cs` | Jogo novo com o relógio acelerado: sem plantar, nenhuma crise e energia 100 · preparar a terra não pisca a barra · depois do primeiro plantio vem a crise, e a barra de energia pisca com "−15" (`GameUI.PiscandoEnergia`, `GameUI.EnergiaPerdidaNaBarra`) · perdas em seguida somam no mesmo "−N" (Cuca + Bernúncia = −35), a barra para de piscar em 2 s e a próxima perda começa do zero · salvar antes de plantar, voltar ao menu e Continuar mantém o começo calmo |
| `FeiraEAldeoesTests.cs` | Comprar a semente liberada; a do nível 5 fica trancada · vender uma erva · entregar o remédio cura o aldeão e paga a recompensa |
| `AldeoesTests.cs` | Cada aldeão aparece com a arte do seu nome (o Tonhão da Venda no lugar onde antes saía de senhora), com foto da vila e da janela que diz até quando vale o pedido · quando os pedidos trocam, a arte acompanha o novo aldeão |
| `CasaEJanelasTests.cs` | Bolsa (I), Livro (B, aba Males, X) e Menu (botão) abrem e fecham · cama: de dia não dorme; à noite dorme, passa para o dia 2 e salva · no topo do mapa a raizeira inteira cabe na tela |
| `PaTests.cs` | Feira ▸ Comprar ▸ pá · parada ao lado da porta de casa, aparece a moldura e "Cavar canteiro aqui"; E cava um canteiro já preparado, onde só a Espada-de-São-Jorge (proteção) tem "Plantar", as outras sementes ficam em "Só na horta", e ela é plantada e regada · sem pá não há moldura · canteiro cavado e vazio some com "Desfazer este canteiro" · a mesma semente de boldo: no cavado fica em "Só na horta" (canteiro vazio, semente na bolsa) e na horta da vó é plantada |
| `SomTests.cs` | Botão de mudo do rodapé (`GameUI.BotaoMudo`, que mostra só o ícone) zera o `AudioListener.volume` e o segundo clique volta ao volume de antes · tecla M faz o mesmo · o controle aparece no menu inicial e com o Menu da partida aberto · **arrastar a barra** (`GameUI.BarraVolume`) muda o volume e a porcentagem · no mudo, puxar a barra liga o som de novo · barra no zero silencia e o botão devolve o volume padrão · no mudo a trilha continua tocando, só sem volume · volume e mudo escolhidos voltam quando o jogo abre de novo (o `GameManager.Awake` lê o `PlayerPrefs`) |
| `HistoricoTests.cs` | Subir de nível fica no Histórico em destaque, com a dica das plantas liberadas · o selo do botão conta os avisos novos e zera ao abrir · `GameUI.BotaoHistorico` e a tecla H abrem e fecham · o aviso "Jogo salvo." (que nasce na Unity) também fica · o histórico volta com o jogo salvo e ganha o "Jogo carregado" |
| `DesempenhoTests.cs` | **Teste de desempenho**: 1 s de aquecimento e 15 s andando pelos quatro cantos do mapa (casa, horta, feira e mata) com o tempo de cada quadro anotado; `FrameRateReport` dá a nota **ruim / regular / bom / excelente** pelo FPS médio, pelo 1% low e pelos engasgos (> 50 ms). A nota sai no terminal (📊), na foto (aviso no canto da tela) e na mensagem do teste; **ruim reprova** |
| `NivelTests.cs` | Subir do nível 1 ao 2 começa a festa; 5 fotos ao longo dela (faixa com o número antigo, o número murchando, o novo brotando, florada e fogos, balões) · a festa termina sozinha e não abre janela |

### Teste de desempenho (FPS)
O `DesempenhoTests` joga 15 s no app de teste e dá uma nota. O FPS médio sozinho engana: 119 quadros rápidos e um de 50 ms dão um número alto no contador, mas o olho vê a engasgada. Por isso a régua (`Core/FrameRate.cs`, `FrameRateReport.Avaliar`) olha três números, com o alvo de 60 fps do jogo (`Application.targetFrameRate`):

| Nota | FPS médio | 1% low (FPS do 1% de quadros mais lentos) | Engasgos (quadros acima de 50 ms) |
|---|---|---|---|
| **Excelente** | ≥ 55 | ≥ 45 | nenhum |
| **Bom** | ≥ 45 | ≥ 30 | até 2 |
| **Regular** | ≥ 30 | ≥ 15 | até 5 |
| **Ruim** | o resto | | |

Saída no terminal:
```
  ✅ Quinze_segundos_andando_pelo_sitio_ganham_nota_de_fluidez (16.3 s)
     📊 EXCELENTE · 60 fps de média · 1% low 56 fps · pior quadro 20 ms · 0 engasgos (> 50 ms) · 900 quadros em 15.0 s
```
Só **ruim** reprova; as outras notas ficam registradas no terminal, no XML (saída do teste) e na foto do passo, que mostra a nota no canto da tela.

A **bateria inteira** também ganha nota. O `AmostradorDeQuadros` (PlayMode) nasce no primeiro teste, sobrevive às trocas de cena e anota cada quadro até o último teste; no fim de cada teste ele regrava `desempenho.txt` (copiado para `Builds/ui-tests/`), e o script mostra a versão final:
```
📊 Bateria inteira: 37 testes · 2 min 11 s · 7525 quadros
  jogo (sem as trocas de cena e as fotos): EXCELENTE · 60 fps de média · 1% low 50 fps · pior quadro 49 ms · 0 engasgos (> 50 ms) · 7273 quadros em 121.4 s
  tudo (com as trocas de cena e as fotos): RUIM · 57 fps de média · 1% low 11 fps · pior quadro 134 ms · 55 engasgos (> 50 ms) · 7525 quadros em 131.2 s
```
A linha **jogo** é a que vale: tira os 3 quadros de cada troca de cena (o `Awake` monta o mundo inteiro) e os quadros que pagam cada foto (`ReadPixels` + PNG em C#), que são trabalho do teste, não do jogo. A linha **tudo** mostra o custo desse trabalho: cada uma das 54 fotos é um engasgo de uns 100 ms. Essa nota não reprova nada. O app de teste é um build de desenvolvimento (precisa da conexão para devolver os resultados), então o jogo em release tende a ir igual ou melhor. Para medir à mão, a aba **Ao vivo** dos Bastidores mostra o FPS e os ms por frame.

### Como a jogadora simulada funciona
- **`JogadorSimulado.cs`** é a base: abre a cena `Main`, aponta `SaveStore.Dir` para uma pasta só dos testes (os saves de verdade não são tocados), grava o som em chaves só dos testes (`GameAudio.Chave = "ui-tests.som"`, apagadas no começo e no fim: o volume do jogo de verdade não muda e cada teste começa no padrão), troca o fechar do app por uma anotação (`GameManager.AoSair`: o **Sair do jogo** liga `PediuParaSair` em vez de fechar o app de teste no meio da rodada) e oferece `Clicar("texto")`, `Arrastar(GameUI.BarraVolume, 0.3f)`, `Apertar(GameKey.Livro)`, `AndarAte(ponto)`, `IrAte(objeto)`, `Abrir(estação, janela)`, `Acelerar(condição)` (via `Time.timeScale`), `SalvarPeloMenu()`, `VoltarAoMenuInicial()` e `Foto("nome")`.
- **Cliques:** todo botão da IMGUI é desenhado por `Button(...)` ou `ListButton(...)` (`UI/UiAutomation.cs`). `GameUI.Click("Apagar", 1)` pede um clique no segundo botão "Apagar" da tela, e `Click("Plantar")` no primeiro habilitado. O clique só acontece se o botão for desenhado e estiver habilitado, e o evento vira `Used`, como no clique do mouse. **Botão novo precisa usar `Button`/`ListButton`**, senão os testes não conseguem clicar nele. Do mesmo jeito, o controle deslizante passa por `Slider(..., nome)`: `GameUI.Arrastar("Volume", 0.3f)` põe o pino em 30% só se a barra estiver desenhada e habilitada.
- **Por que um app, e não o modo batch:** sem placa de vídeo (`-batchmode` no editor), a Unity não chama o `OnGUI` nem desenha a tela, então não haveria botões para clicar nem fotos.
- **Fotos:** o projeto não inclui o módulo ImageConversion da Unity (o `EncodeToPNG`), então `Png.cs` grava o PNG.

Para um teste novo, herde de `JogadorSimulado`, escreva um `[UnityTest]` que comece em `ComecarPartida()` e confira o estado do jogo (`Sessao`, `Tela.Current`) depois de cada ação.

## Análise estática (lint)
Os testes executam o código; o lint lê o código sem executar.
```bash
./scripts/lint.sh          # falha (código 1) com qualquer aviso ou formatação fora do padrão
./scripts/lint.sh --fix    # aplica as correções automáticas e verifica de novo
```
Ele compila os 5 assemblies pelos projetos de `tools/lint` (um por asmdef, .NET Standard 2.1 e C# 9 como a Unity) com os analisadores do .NET (CA), as regras de estilo (IDE) e o Microsoft.Unity.Analyzers (UNT) e, no Core e no Game, as APIs proibidas de `tools/lint/BannedSymbols.txt` (RS0030: rede, processos externos e `BinaryFormatter`; ver [Segurança](15-seguranca.md)), e depois roda `dotnet format --verify-no-changes`. As regras ficam no `.editorconfig` da raiz. Precisa do .NET SDK (`brew install --cask dotnet-sdk`) e roda em ~6 s, mesmo com o editor da Unity aberto. Detalhes, regras desligadas e o que ele já encontrou: [README › Análise estática](../README.md#-análise-estática-lint-do-c).

Ordem sugerida antes de entregar: `./scripts/lint.sh && ./scripts/run-tests.sh && ./scripts/run-ui-tests.sh && ./scripts/build-macos.sh && ./scripts/check-security.sh`.
